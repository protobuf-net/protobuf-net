#nullable enable
// The contract/operation shape classification in this file is adapted, close to verbatim, from
// https://github.com/protobuf-net/protobuf-net/pull/1255 by Victor Irzak (@virzak), which moved it
// from https://github.com/protobuf-net/protobuf-net.Grpc/pull/364. It mirrors the runtime's
// ContractOperation rules, and three bugs found during that port are baked in here: explicit
// interface implementations must name the *declaring* interface, only [SubService] bases are bound,
// and IObservable<T> / Stream / Grpc.Core's own call types must be rejected rather than treated as
// ordinary payloads.
//
// What is NOT from there is how this is reached: the source PR triggered on the service interface
// itself, where this is driven from typeof(...) seeds on a consumer-declared model - see
// GrpcProxyGenerator.Parse.cs.
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal.Grpc;
using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace ProtoBuf.BuildTools.Generators
{
    public sealed partial class GrpcProxyGenerator
    {
        private static readonly SymbolDisplayFormat s_fullyQualified = SymbolDisplayFormat.FullyQualifiedFormat
            .WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
                | SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static string Display(ITypeSymbol type) => type.ToDisplayString(s_fullyQualified);

        /// <summary>
        /// Reduce a candidate service contract to an emittable model, or to the diagnostics explaining
        /// why it was left to the runtime path.
        /// </summary>
        private static GrpcContractCandidate ParseContract(INamedTypeSymbol iface,
            INamedTypeSymbol? implementation, CancellationToken cancellationToken)
        {
            if (iface.TypeKind != TypeKind.Interface)
            {
                return new GrpcContractCandidate(null, One(NotAServiceContract, Where(iface), iface.ToDisplayString()));
            }

            // a contract has to carry one of the two markers, or the runtime would not bind it either
            if (!HasAttribute(iface, ServiceAttributeName) && !HasAttribute(iface, ServiceContractAttributeName))
            {
                return new GrpcContractCandidate(null, One(NotAServiceContract, Where(iface), iface.ToDisplayString()));
            }

            // nested and generic contracts are both about the *shape of the generated type*, not any
            // one operation, so they take the whole interface out
            if (iface.ContainingType is not null)
            {
                return new GrpcContractCandidate(null, One(
                    InterfaceMustNotBeNested, Where(iface), iface.Name, iface.ContainingType.ToDisplayString()));
            }

            if (iface.IsGenericType)
            {
                return new GrpcContractCandidate(null, One(GenericInterfaceNotSupported, Where(iface), iface.Name));
            }

            // Emit only when EVERY operation on the interface (and its bases) is in a shape we handle:
            // the runtime path covers a wider set (IObservable, Stream, ...) than we generate, so a
            // proxy with missing implementations would regress behaviour rather than improve it.
            var operations = ImmutableArray.CreateBuilder<GrpcOperationModel>();
            var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();
            var unsupported = false;

            // Inherited interfaces follow the runtime's rules exactly, because the wire format depends
            // on them: a [SubService] base is bound under *this* contract's service name, IDisposable
            // and IAsyncDisposable get no-op implementations and are never bound, and anything else
            // takes the contract out - the runtime emits a throwing stub for those members and binds
            // nothing, which is not something worth reproducing at build time.
            var contracts = ImmutableArray.CreateBuilder<INamedTypeSymbol>();
            contracts.Add(iface);
            var disposable = false;
            var asyncDisposable = false;

            foreach (var inherited in iface.AllInterfaces)
            {
                if (HasAttribute(inherited, SubServiceAttributeName))
                {
                    contracts.Add(inherited);
                }
                else if (IsType(inherited, "System", "IDisposable"))
                {
                    disposable = true;
                }
                else if (IsType(inherited, "System", "IAsyncDisposable"))
                {
                    asyncDisposable = true;
                }
                else if (DeclaresOperations(inherited))
                {
                    diagnostics.Add(new DiagnosticInfo(
                        UnsupportedBaseInterface, Where(iface), iface.Name, inherited.ToDisplayString()));
                    return new GrpcContractCandidate(null, diagnostics.ToImmutable());
                }
            }

            foreach (var contract in contracts)
            {
                foreach (var member in contract.GetMembers())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (member is not IMethodSymbol method) continue;
                    if (method.MethodKind != Microsoft.CodeAnalysis.MethodKind.Ordinary) continue;
                    if (method.IsStatic) continue;

                    if (TryParseOperation(method, contract, out var operation) && operation is not null)
                    {
                        operations.Add(operation);
                    }
                    else
                    {
                        unsupported = true;
                        diagnostics.Add(new DiagnosticInfo(
                            UnsupportedMethodShape, Where(method), contract.ToDisplayString(), method.Name));
                    }
                }
            }

            // no recognised operations at all is most likely a marker interface, not a contract
            if (unsupported || operations.Count == 0)
            {
                return new GrpcContractCandidate(null, diagnostics.ToImmutable());
            }

            // The implementation must actually implement the contract, or the generated provider would
            // not compile; saying so here is much clearer than the CS0311 the consumer would get.
            if (implementation is not null && !ImplementsContract(implementation, iface))
            {
                return new GrpcContractCandidate(null, One(ImplementationDoesNotImplement,
                    Where(implementation), implementation.ToDisplayString(), iface.ToDisplayString()));
            }

            var sanitized = SanitizeTypeName(iface);
            var model = new GrpcInterfaceModel(
                interfaceFullName: iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                serviceName: GetServiceName(iface),
                proxyTypeName: sanitized + "_ClientProxy",
                providerTypeName: sanitized + "_ServerBindings",
                implementationTypeFullName: implementation?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                operations: operations.ToImmutable(),
                implementsDisposable: disposable,
                implementsAsyncDisposable: asyncDisposable);

            return new GrpcContractCandidate(model, diagnostics.ToImmutable());
        }

        private static Location? Where(ISymbol symbol)
        {
            foreach (var location in symbol.Locations)
            {
                if (location.IsInSource) return location;
            }
            return null;
        }

        private static bool ImplementsContract(INamedTypeSymbol implementation, INamedTypeSymbol contract)
        {
            foreach (var iface in implementation.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface, contract)) return true;
            }
            return false;
        }

        private static bool DeclaresOperations(INamedTypeSymbol iface)
        {
            foreach (var member in iface.GetMembers())
            {
                if (member is IMethodSymbol { IsStatic: false } method
                    && method.MethodKind == Microsoft.CodeAnalysis.MethodKind.Ordinary)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Recognise one operation, mirroring the runtime <c>ContractOperation.TryIdentifySignature</c>
        /// rules for the subset of shapes we emit.
        /// </summary>
        private static bool TryParseOperation(IMethodSymbol method, INamedTypeSymbol declaringInterface, out GrpcOperationModel? model)
        {
            model = null;
            if (method.IsGenericMethod) return false;
            if (method.MethodKind != Microsoft.CodeAnalysis.MethodKind.Ordinary) return false;
            if (method.Parameters.Length > 3) return false;

            var returnInfo = CategorizeReturn(method.ReturnType);
            if (returnInfo is null) return false;
            var (responseShape, impliedKind, responseType, voidResponse) = returnInfo.Value;

            GrpcArgShape requestShape;
            string requestType;
            bool voidRequest;
            GrpcContextKind context;

            var first = method.Parameters.Length >= 1 ? method.Parameters[0] : null;
            var second = method.Parameters.Length >= 2 ? method.Parameters[1] : null;

            // the supported parameter patterns are (), (context), (request) and (request, context)
            if (first is null)
            {
                requestShape = GrpcArgShape.Void;
                requestType = EmptyTypeName;
                voidRequest = true;
                context = GrpcContextKind.None;
            }
            else
            {
                var firstKind = CategorizeArg(first.Type);
                if (firstKind == ArgKind.Context)
                {
                    if (second is not null) return false; // nothing may follow the context
                    requestShape = GrpcArgShape.Void;
                    requestType = EmptyTypeName;
                    voidRequest = true;
                    context = first.Type.SpecialType == SpecialType.System_Object
                        ? GrpcContextKind.None
                        : MapContext(first.Type);
                }
                else if (firstKind == ArgKind.Data || firstKind == ArgKind.AsyncEnumerable)
                {
                    var element = firstKind == ArgKind.AsyncEnumerable ? GetElementType(first.Type) : null;
                    if (firstKind == ArgKind.AsyncEnumerable && (element is null || IsRuntimeOnlyPayload(element))) return false;

                    requestShape = firstKind == ArgKind.AsyncEnumerable ? GrpcArgShape.AsyncEnumerable : GrpcArgShape.Data;
                    requestType = Display(element ?? first.Type);
                    voidRequest = false;
                    if (second is null)
                    {
                        context = GrpcContextKind.None;
                    }
                    else
                    {
                        if (CategorizeArg(second.Type) != ArgKind.Context) return false;
                        if (method.Parameters.Length > 2) return false;
                        context = MapContext(second.Type);
                    }
                }
                else
                {
                    return false;
                }
            }

            // request and response streaming combine into the final method kind
            var kind = impliedKind switch
            {
                GrpcMethodKind.ServerStreaming => requestShape == GrpcArgShape.AsyncEnumerable ? GrpcMethodKind.DuplexStreaming : GrpcMethodKind.ServerStreaming,
                GrpcMethodKind.Unary => requestShape == GrpcArgShape.AsyncEnumerable ? GrpcMethodKind.ClientStreaming : GrpcMethodKind.Unary,
                _ => impliedKind,
            };

            if (kind == GrpcMethodKind.DuplexStreaming && responseShape != GrpcResultShape.AsyncEnumerable) return false;
            if (kind == GrpcMethodKind.ClientStreaming && responseShape == GrpcResultShape.AsyncEnumerable) return false;

            var parameters = ImmutableArray.CreateBuilder<GrpcParameterModel>(method.Parameters.Length);
            foreach (var parameter in method.Parameters)
            {
                parameters.Add(new GrpcParameterModel(parameter.Name, Display(parameter.Type)));
            }

            model = new GrpcOperationModel(
                operationName: TryGetOperationName(method) ?? StripAsyncSuffix(method.Name),
                methodName: method.Name,
                declaringInterfaceFullName: declaringInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                kind: kind,
                context: context,
                requestShape: requestShape,
                responseShape: responseShape,
                requestTypeFullName: requestType,
                responseTypeFullName: responseType,
                voidRequest: voidRequest,
                voidResponse: voidResponse,
                returnTypeDisplay: Display(method.ReturnType),
                parameters: parameters.MoveToImmutable());
            return true;
        }

        /// <summary>
        /// The logical service name: an explicit <c>Name</c> if given, else the default that the
        /// runtime <c>ServiceBinder.GetDefaultName</c> would produce.
        /// </summary>
        private static string GetServiceName(INamedTypeSymbol iface)
        {
            foreach (var attribute in iface.GetAttributes())
            {
                var name = attribute.AttributeClass?.Name;
                if (name != "ServiceAttribute" && name != "ServiceContractAttribute") continue;

                foreach (var named in attribute.NamedArguments)
                {
                    if (named.Key == "Name" && named.Value.Value is string explicitName && !string.IsNullOrWhiteSpace(explicitName))
                    {
                        return explicitName;
                    }
                }
                if (attribute.ConstructorArguments.Length >= 1
                    && attribute.ConstructorArguments[0].Value is string ctorName
                    && !string.IsNullOrWhiteSpace(ctorName))
                {
                    return ctorName;
                }
            }

            var trimmed = iface.Name;
            if (trimmed.Length > 1 && trimmed[0] == 'I' && char.IsUpper(trimmed[1])) trimmed = trimmed.Substring(1);
            var ns = Namespace(iface);
            return ns.Length == 0 ? trimmed : ns + "." + trimmed;
        }

        private static string Namespace(INamedTypeSymbol iface)
            => iface.ContainingNamespace.IsGlobalNamespace ? "" : iface.ContainingNamespace.ToDisplayString();

        /// <summary>
        /// Render the fully-qualified interface name as a single identifier, for use in generated type names.
        /// </summary>
        private static string SanitizeTypeName(INamedTypeSymbol iface)
        {
            var ns = Namespace(iface);
            var raw = ns.Length == 0 ? iface.Name : ns + "." + iface.Name;
            var sb = new StringBuilder(raw.Length);
            foreach (var c in raw)
            {
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }
            return sb.ToString();
        }

        private static string? TryGetOperationName(IMethodSymbol method)
        {
            foreach (var attribute in method.GetAttributes())
            {
                var name = attribute.AttributeClass?.Name;
                if (name != "OperationAttribute" && name != "OperationContractAttribute") continue;

                foreach (var named in attribute.NamedArguments)
                {
                    if (named.Key == "Name" && named.Value.Value is string explicitName && !string.IsNullOrWhiteSpace(explicitName))
                    {
                        return explicitName;
                    }
                }
                if (attribute.ConstructorArguments.Length == 1
                    && attribute.ConstructorArguments[0].Value is string ctorName
                    && !string.IsNullOrWhiteSpace(ctorName))
                {
                    return ctorName;
                }
            }
            return null;
        }

        private static string StripAsyncSuffix(string name)
            => name.Length > 5 && name.EndsWith("Async", StringComparison.Ordinal)
                ? name.Substring(0, name.Length - 5)
                : name;

        private enum ArgKind { Data, Context, AsyncEnumerable, Unsupported }

        private static ArgKind CategorizeArg(ITypeSymbol type)
        {
            if (IsCallContext(type) || IsCancellationToken(type)) return ArgKind.Context;
            if (IsAsyncEnumerable(type)) return ArgKind.AsyncEnumerable;
            // server-side and raw-gRPC shapes belong to the runtime path
            if (IsType(type, "Grpc.Core", "ServerCallContext") || IsType(type, "Grpc.Core", "CallOptions")) return ArgKind.Unsupported;
            if (IsRuntimeOnlyPayload(type)) return ArgKind.Unsupported;
            return ArgKind.Data;
        }

        /// <summary>
        /// Shapes that the runtime path recognises but this generator does not emit.
        /// </summary>
        /// <remarks>
        /// These must be rejected explicitly rather than falling through to "an ordinary payload":
        /// emitting a proxy that asks for a marshaller for, say, <c>IObservable&lt;T&gt;</c> would turn
        /// a contract that works today into a bind-time failure at startup.
        /// </remarks>
        private static bool IsRuntimeOnlyPayload(ITypeSymbol type)
        {
            if (type is INamedTypeSymbol named && named.IsGenericType && IsType(named.ConstructedFrom, "System", "IObservable"))
            {
                return true;
            }

            // Grpc.Core's own call types: AsyncUnaryCall<>, AsyncServerStreamingCall<>, ...
            if (type.ContainingNamespace?.ToDisplayString() == "Grpc.Core"
                && type.Name.StartsWith("Async", StringComparison.Ordinal)
                && type.Name.EndsWith("Call", StringComparison.Ordinal))
            {
                return true;
            }

            for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
            {
                if (IsType(current, "System.IO", "Stream")) return true;
            }

            return false;
        }

        private static GrpcContextKind MapContext(ITypeSymbol type)
        {
            if (IsCallContext(type)) return GrpcContextKind.CallContext;
            if (IsCancellationToken(type)) return GrpcContextKind.CancellationToken;
            return GrpcContextKind.None;
        }

        private static (GrpcResultShape Shape, GrpcMethodKind ImpliedKind, string DataType, bool Void)? CategorizeReturn(ITypeSymbol returnType)
        {
            if (returnType.SpecialType == SpecialType.System_Void)
            {
                return (GrpcResultShape.Sync, GrpcMethodKind.Unary, EmptyTypeName, true);
            }

            if (returnType is INamedTypeSymbol named)
            {
                if (!named.IsGenericType)
                {
                    if (IsType(named, "System.Threading.Tasks", "Task")) return (GrpcResultShape.Task, GrpcMethodKind.Unary, EmptyTypeName, true);
                    if (IsType(named, "System.Threading.Tasks", "ValueTask")) return (GrpcResultShape.ValueTask, GrpcMethodKind.Unary, EmptyTypeName, true);
                }
                else
                {
                    var definition = named.ConstructedFrom;
                    var payload = named.TypeArguments[0];

                    // Task<Stream>, IAsyncEnumerable<IObservable<T>>, ... are all runtime-path shapes
                    if (IsRuntimeOnlyPayload(payload)) return null;

                    if (IsType(definition, "System.Threading.Tasks", "Task")) return (GrpcResultShape.Task, GrpcMethodKind.Unary, Display(payload), false);
                    if (IsType(definition, "System.Threading.Tasks", "ValueTask")) return (GrpcResultShape.ValueTask, GrpcMethodKind.Unary, Display(payload), false);
                    if (IsType(definition, "System.Collections.Generic", "IAsyncEnumerable")) return (GrpcResultShape.AsyncEnumerable, GrpcMethodKind.ServerStreaming, Display(payload), false);
                }
            }

            // a bare synchronous return value
            if (IsRuntimeOnlyPayload(returnType)) return null;
            return (GrpcResultShape.Sync, GrpcMethodKind.Unary, Display(returnType), false);
        }

        private static ITypeSymbol? GetElementType(ITypeSymbol type)
            => type is INamedTypeSymbol named && named.IsGenericType && named.TypeArguments.Length == 1
                ? named.TypeArguments[0]
                : null;

        private static bool IsAsyncEnumerable(ITypeSymbol type)
            => type is INamedTypeSymbol named && named.IsGenericType
                && IsType(named.ConstructedFrom, "System.Collections.Generic", "IAsyncEnumerable");

        private static bool IsCallContext(ITypeSymbol type) => IsType(type, "ProtoBuf.Grpc", "CallContext");

        private static bool IsCancellationToken(ITypeSymbol type) => IsType(type, "System.Threading", "CancellationToken");

        private static bool IsType(ITypeSymbol type, string ns, string name)
            => type.Name == name && type.ContainingNamespace?.ToDisplayString() == ns;
    }
}
