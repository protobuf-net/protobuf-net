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
                return new GrpcContractCandidate(null, One(GrpcDiagnosticKind.NotAServiceContract, Where(iface), iface.ToDisplayString()));
            }

            // a contract has to carry one of the two markers, or the runtime would not bind it either
            if (!HasAttribute(iface, ServiceAttributeName) && !HasAttribute(iface, ServiceContractAttributeName))
            {
                return new GrpcContractCandidate(null, One(GrpcDiagnosticKind.NotAServiceContract, Where(iface), iface.ToDisplayString()));
            }

            // nested and generic contracts are both about the *shape of the generated type*, not any
            // one operation, so they take the whole interface out
            if (iface.ContainingType is not null)
            {
                return new GrpcContractCandidate(null, One(
                    GrpcDiagnosticKind.InterfaceMustNotBeNested, Where(iface), iface.Name, iface.ContainingType.ToDisplayString()));
            }

            // Open versus closed, not generic versus not - the same line the serializer generator draws.
            // A closed construction is an ordinary contract: Roslyn hands us its members already
            // substituted, so IBox<Request> and IBox<Reply> are simply two contracts that share a
            // definition. An open one has nowhere to put the type parameter, since the proxy and the
            // provider are non-generic types.
            if (IsOpenGeneric(iface))
            {
                return new GrpcContractCandidate(null, One(GrpcDiagnosticKind.GenericInterfaceNotSupported, Where(iface), iface.Name));
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
                        GrpcDiagnosticKind.UnsupportedBaseInterface, Where(iface), iface.Name, inherited.ToDisplayString()));
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

                    if (TryParseOperation(method, contract, out var operation, out var reason) && operation is not null)
                    {
                        operations.Add(operation);
                    }
                    else
                    {
                        unsupported = true;
                        diagnostics.Add(new DiagnosticInfo(
                            GrpcDiagnosticKind.UnsupportedMethodShape, Where(method), contract.ToDisplayString(), method.Name, reason));
                    }
                }
            }

            // No recognised operations at all is most likely a marker interface rather than a contract -
            // but "most likely" is not a reason to say nothing, since the consumer named it explicitly.
            // Only reported where nothing was *refused* either: a contract with an unsupported member
            // already has PBN4002, which says more.
            if (operations.Count == 0 && !unsupported)
            {
                diagnostics.Add(new DiagnosticInfo(GrpcDiagnosticKind.NoOperationsFound,
                    Where(iface), iface.ToDisplayString(), iface.Name));
            }
            if (unsupported || operations.Count == 0)
            {
                return new GrpcContractCandidate(null, diagnostics.ToImmutable());
            }

            // The implementation must actually implement the contract, or the generated provider would
            // not compile; saying so here is much clearer than the CS0311 the consumer would get.
            if (implementation is not null && !ImplementsContract(implementation, iface))
            {
                return new GrpcContractCandidate(null, One(GrpcDiagnosticKind.ImplementationDoesNotImplement,
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
        /// <param name="reason">
        /// On failure, a sentence fragment naming what was wrong, for <c>PBN4002</c>. Every exit
        /// carries one: "this signature is not supported" tells a consumer nothing they can act on,
        /// and there are six quite different ways to reach it.
        /// </param>
        private static bool TryParseOperation(IMethodSymbol method, INamedTypeSymbol declaringInterface,
            out GrpcOperationModel? model, out string reason)
        {
            model = null;
            reason = "";
            if (method.IsGenericMethod)
            {
                reason = "it is generic, so there is no one request or response type to build a Method<,> from";
                return false;
            }
            if (method.MethodKind != Microsoft.CodeAnalysis.MethodKind.Ordinary) return false;
            if (method.Parameters.Length > 3)
            {
                reason = $"it takes {method.Parameters.Length} parameters; the supported patterns are "
                    + "(), (context), (request) and (request, context)";
                return false;
            }

            var returnInfo = CategorizeReturn(method.ReturnType);
            if (returnInfo is null)
            {
                reason = $"its return type '{Display(method.ReturnType)}' is a shape only the runtime proxy "
                    + "handles - Stream, IObservable<T> and Grpc.Core's own call types are reshaped at run time";
                return false;
            }
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
                    if (second is not null)
                    {
                        reason = $"'{second.Name}' follows the context parameter '{first.Name}', and nothing may";
                        return false;
                    }
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
                    if (firstKind == ArgKind.AsyncEnumerable && (element is null || IsRuntimeOnlyPayload(element)))
                    {
                        reason = $"the element type of request stream '{first.Name}' is a shape only the runtime "
                            + "proxy handles - Stream, IObservable<T> and Grpc.Core's own call types are reshaped at run time";
                        return false;
                    }

                    requestShape = firstKind == ArgKind.AsyncEnumerable ? GrpcArgShape.AsyncEnumerable : GrpcArgShape.Data;
                    requestType = Display(element ?? first.Type);
                    voidRequest = false;
                    if (second is null)
                    {
                        context = GrpcContextKind.None;
                    }
                    else
                    {
                        if (CategorizeArg(second.Type) != ArgKind.Context)
                        {
                            reason = $"'{second.Name}' follows the request but is not a CallContext or CancellationToken";
                            return false;
                        }
                        if (method.Parameters.Length > 2)
                        {
                            reason = $"'{method.Parameters[2].Name}' follows the context parameter, and nothing may";
                            return false;
                        }
                        context = MapContext(second.Type);
                    }
                }
                else
                {
                    reason = $"parameter '{first.Name}' has type '{Display(first.Type)}', which is "
                        + (IsType(first.Type, "Grpc.Core", "ServerCallContext") || IsType(first.Type, "Grpc.Core", "CallOptions")
                            ? "a server-side type rather than something a client contract can carry"
                            : "a shape only the runtime proxy handles - Stream, IObservable<T> and Grpc.Core's own "
                                + "call types are reshaped at run time");
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

            if (kind == GrpcMethodKind.DuplexStreaming && responseShape != GrpcResultShape.AsyncEnumerable)
            {
                reason = "a streaming request has to be answered by a streaming response or a single value, "
                    + $"and '{Display(method.ReturnType)}' is neither";
                return false;
            }
            if (kind == GrpcMethodKind.ClientStreaming && responseShape == GrpcResultShape.AsyncEnumerable)
            {
                reason = "a streaming request answered by a stream is a duplex call, which this return "
                    + "type does not express";
                return false;
            }

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
        /// An open generic type, or one closed over a type parameter - either way, not something a
        /// non-generic proxy type can be emitted for. <c>typeof(IBox&lt;&gt;)</c> arrives as an
        /// <em>unbound</em> symbol whose type arguments are not type parameters, so it needs its own
        /// test alongside the recursive one.
        /// </summary>
        private static bool IsOpenGeneric(INamedTypeSymbol type)
        {
            if (type.IsUnboundGenericType) return true;
            foreach (var argument in type.TypeArguments)
            {
                if (ContainsTypeParameter(argument)) return true;
            }
            return false;
        }

        private static bool ContainsTypeParameter(ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.TypeParameter) return true;
            if (type is IArrayTypeSymbol array) return ContainsTypeParameter(array.ElementType);
            if (type is INamedTypeSymbol named)
            {
                foreach (var argument in named.TypeArguments)
                {
                    if (ContainsTypeParameter(argument)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// The logical service name: an explicit <c>Name</c> if given, else the default that the
        /// runtime <c>ServiceBinder.GetDefaultName</c> would produce.
        /// </summary>
        /// <remarks>
        /// This has to agree with the runtime exactly, character for character: it is the name on the
        /// wire, so a generated client that computes it differently from a reflection-bound server
        /// simply does not find the service - and the failure is an unimplemented-method error at
        /// call time, not anything that points here.
        /// </remarks>
        private static string GetServiceName(INamedTypeSymbol iface)
        {
            foreach (var attribute in iface.GetAttributes())
            {
                var name = attribute.AttributeClass?.Name;
                if (name != "ServiceAttribute" && name != "ServiceContractAttribute") continue;

                string? explicitName = null;
                foreach (var named in attribute.NamedArguments)
                {
                    if (named.Key == "Name" && named.Value.Value is string namedValue && !string.IsNullOrWhiteSpace(namedValue))
                    {
                        explicitName = namedValue;
                    }
                }
                if (explicitName is null
                    && attribute.ConstructorArguments.Length >= 1
                    && attribute.ConstructorArguments[0].Value is string ctorName
                    && !string.IsNullOrWhiteSpace(ctorName))
                {
                    explicitName = ctorName;
                }
                if (explicitName is null) continue;

                // On a generic contract an explicit name is a *format string*, filled with the same
                // parts the default name would have used: ServiceBinder does string.Format(name, parts).
                if (!iface.IsGenericType) return explicitName;
                try
                {
                    return string.Format(explicitName, GetGenericParts(iface));
                }
                catch (FormatException)
                {
                    // a malformed template throws at bind time in the runtime too; emitting the raw
                    // string keeps the generator from being the thing that falls over
                    return explicitName;
                }
            }

            return GetDefaultServiceName(iface);
        }

        /// <summary>
        /// A port of <c>ServiceBinder.GetDefaultName(Type)</c>.
        /// </summary>
        /// <remarks>
        /// Two of its behaviours look like slips and are deliberately reproduced, because the name is
        /// the wire contract and diverging would rename the service silently:
        /// <list type="bullet">
        /// <item>the leading "I" is stripped by a bare <c>StartsWith("I")</c>, with no test that what
        /// follows is upper-case - so an interface called <c>Item</c> binds as <c>tem</c>;</item>
        /// <item>the namespace is concatenated unconditionally, and <c>Type.Namespace</c> is
        /// <c>null</c> in the global namespace - so a contract there binds with a leading dot.</item>
        /// </list>
        /// </remarks>
        private static string GetDefaultServiceName(INamedTypeSymbol iface)
        {
            var trimmed = iface.Name;
            if (trimmed.StartsWith("I", StringComparison.Ordinal)) trimmed = trimmed.Substring(1);

            var serviceName = Namespace(iface) + "." + trimmed;
            if (iface.IsGenericType)
            {
                serviceName = serviceName + "_" + string.Join("_", GetGenericParts(iface));
            }
            return serviceName;
        }

        /// <summary>
        /// The per-argument names a generic contract's service name is built from, per
        /// <c>ServiceBinder.GetGenericParts</c> - which asks <c>GetDataContractName</c>, so an
        /// explicit contract name wins over the type's own.
        /// </summary>
        private static object[] GetGenericParts(INamedTypeSymbol iface)
        {
            var arguments = iface.TypeArguments;
            var parts = new object[arguments.Length];
            for (int i = 0; i < parts.Length; i++) parts[i] = GetDataContractName(arguments[i]);
            return parts;
        }

        private static string GetDataContractName(ITypeSymbol type)
        {
            // ProtoContract first, then DataContract - the order the runtime asks in
            return TryGetName("ProtoBuf.ProtoContractAttribute")
                ?? TryGetName("System.Runtime.Serialization.DataContractAttribute")
                // the *metadata* name, so an argument that is itself generic contributes its arity
                // exactly as Type.Name would ("List`1")
                ?? type.MetadataName;

            string? TryGetName(string attributeName)
            {
                foreach (var attribute in type.GetAttributes())
                {
                    if (attribute.AttributeClass?.ToDisplayString() != attributeName) continue;
                    foreach (var named in attribute.NamedArguments)
                    {
                        if (named.Key == "Name" && named.Value.Value is string value && !string.IsNullOrWhiteSpace(value))
                        {
                            return value;
                        }
                    }
                }
                return null;
            }
        }

        private static string Namespace(INamedTypeSymbol iface)
            => iface.ContainingNamespace.IsGlobalNamespace ? "" : iface.ContainingNamespace.ToDisplayString();

        /// <summary>
        /// Render the fully-qualified interface name as a single identifier, for use in generated type names.
        /// </summary>
        private static string SanitizeTypeName(INamedTypeSymbol iface)
        {
            // Built from the *constructed* name, so that IBox<Request> and IBox<Reply> get distinct
            // proxy and provider types rather than colliding on "IBox". For a non-generic contract
            // this is character-for-character what the namespace-plus-name form produced.
            var raw = iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "");
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
