#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Immutable;
using System.Threading;

namespace ProtoBuf.BuildTools.Analyzers
{
    /// <summary>
    /// Inspects service contracts for common code-first gRPC configuration errors
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ServiceContractAnalyzer : DiagnosticAnalyzer, ILoggingAnalyzer
    {
        private event Action<string>? Log;
        event Action<string>? ILoggingAnalyzer.Log
        {
            add => this.Log += value;
            remove => this.Log -= value;
        }

        internal static readonly DiagnosticDescriptor InvalidMemberKind = new(
            id: "PBN2001",
            title: nameof(ServiceContractAnalyzer) + "." + nameof(InvalidMemberKind),
            messageFormat: "This member is not a method; only methods are supported for gRPC services.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidPayloadType = new(
            id: "PBN2002",
            title: nameof(ServiceContractAnalyzer) + "." + nameof(InvalidPayloadType),
            messageFormat: "The data parameter of a gRPC method must currently be Void, a reference-type data contract, or an async sequence of the same.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidReturnType = new(
            id: "PBN2003",
            title: nameof(ServiceContractAnalyzer) + "." + nameof(InvalidReturnType),
            messageFormat: "The return value of a gRPC method must currently be Void, a reference-type data contract, or an task / async sequence of the same.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor GenericMethod = new(
            id: "PBN2004",
            title: nameof(ServiceContractAnalyzer) + "." + nameof(GenericMethod),
            messageFormat: "The gRPC method can not be generic.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor GenericService = new(
            id: "PBN2005",
            title: nameof(ServiceContractAnalyzer) + "." + nameof(GenericService),
            messageFormat: "The gRPC service can not be generic.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidContextType = new(
            id: "PBN2006",
            title: nameof(ServiceContractAnalyzer) + "." + nameof(InvalidContextType),
            messageFormat: "The context parameter of a gRPC method must be CallContext or CancellationToken.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidParameters = new(
            id: "PBN2007",
            title: nameof(ServiceContractAnalyzer) + "." + nameof(InvalidParameters),
            messageFormat: "Invalid signature; gRPC methods expect a single optional payload and a single optional context.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor PossiblyNotSerializable = new(
            id: "PBN2008",
            title: nameof(ServiceContractAnalyzer) + "." + nameof(PossiblyNotSerializable),
            messageFormat: "gRPC methods require inputs/outputs that can be marshalled with gRPC; this type *may* be usable with gRPC, but it could not be verified.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor PreferAsync = new(
            id: "PBN2009",
            title: nameof(ServiceContractAnalyzer) + "." + nameof(PreferAsync),
            messageFormat: "gRPC methods should be async when possible.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor StreamingSyncMethod = new(
            id: "PBN2010",
            title: nameof(ServiceContractAnalyzer) + "." + nameof(StreamingSyncMethod),
            messageFormat: "gRPC methods that take streaming parameters cannot be synchronous.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly ImmutableArray<SyntaxKind> s_syntaxKinds =
            ImmutableArray.Create(SyntaxKind.InterfaceDeclaration);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext ctx)
        {
            ctx.EnableConcurrentExecution();
            ctx.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze);
            ctx.RegisterSyntaxNodeAction(context => ConsiderPossibleServiceType(context), s_syntaxKinds);
        }

        private static readonly ImmutableArray<DiagnosticDescriptor> s_SupportedDiagnostics = Utils.GetDeclared(typeof(ServiceContractAnalyzer));

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => s_SupportedDiagnostics;

        private void ConsiderPossibleServiceType(SyntaxNodeAnalysisContext context)
        {
            if (context.ContainingSymbol is not INamedTypeSymbol type) return;

            var attribs = type.GetAttributes();
            string? serviceName = null;
            foreach (var attrib in attribs)
            {
                var ac = attrib.AttributeClass;
                if (ac?.Name == "ServiceAttribute" && ac.InNamespace("ProtoBuf","Grpc","Configuration"))
                {
                    attrib.TryGetStringByName("Name", out serviceName);
                    if (string.IsNullOrWhiteSpace(serviceName)) serviceName = GetDefaultName(type);
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(serviceName)) return;

            Log?.Invoke($"Service detected: '{serviceName}'");

            if (type.IsGenericType)
            {
                context.ReportDiagnostic(Diagnostic.Create(GenericService, Utils.PickLocation(ref context, type)));
            }

            foreach (var member in type.GetMembers())
            {
                if (member is not IMethodSymbol method)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidMemberKind, Utils.PickLocation(ref context, member)));
                    continue;
                }

                if (method.IsGenericMethod)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GenericMethod, Utils.PickLocation(ref context, method)));
                }

                var p = method.Parameters;
                (INamedTypeSymbol? Type, DataTypeFlags Flags) payload;
                ContextKind contextKind;
                switch (p.Length)
                {
                    case 0:
                        contextKind = ContextKind.None;
                        payload = (null, DataTypeFlags.IsEmpty);
                        break;
                    case 1:
                        // single arg could be payload or context
                        if (TryResolveContextKind(p[0].Type, out contextKind))
                        {
                            AssertContext(p[0]);
                            payload = (null, DataTypeFlags.IsEmpty);
                        }
                        else
                        {
                            contextKind = ContextKind.None;
                            payload = ResolveDataType(p[0].Type);
                            AssertPayload(p[0]);
                        }
                        break;
                    case 2:
                        // first arg is expected to be the payload, second arg is expected to be the context
                        payload = ResolveDataType(p[0].Type);
                        AssertPayload(p[0]);
                        if (TryResolveContextKind(p[1].Type, out contextKind))
                        {
                            AssertContext(p[1]);
                        }
                        else
                        {
                            contextKind = ContextKind.None;
                            context.ReportDiagnostic(Diagnostic.Create(InvalidParameters, Utils.PickLocation(ref context, member)));
                        }
                        
                        break;
                    default:
                        payload = (null, DataTypeFlags.InvalidType);
                        contextKind = ContextKind.Invalid;
                        context.ReportDiagnostic(Diagnostic.Create(InvalidParameters, Utils.PickLocation(ref context, member)));
                        break;
                }

                var ret = ResolveDataType(method.ReturnType);
                if ((ret.Flags & DataTypeFlags.InvalidType) != 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidReturnType, Utils.PickLocation(ref context, method)));
                }
                else if ((ret.Flags & (DataTypeFlags.DataContract | DataTypeFlags.IsEmpty)) == 0
                    && !SymbolEqualityComparer.Default.Equals(payload.Type, ret.Type)) // don't double report
                {
                    context.ReportDiagnostic(Diagnostic.Create(PossiblyNotSerializable, Utils.PickLocation(ref context, method)));
                }
                else if ((ret.Flags & DataTypeFlags.IsAsync) == 0)
                {
                    if ((payload.Flags & DataTypeFlags.IsStream) != 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(StreamingSyncMethod, Utils.PickLocation(ref context, method)));
                    }
                    else
                    {
                        context.ReportDiagnostic(Diagnostic.Create(PreferAsync, Utils.PickLocation(ref context, method)));
                    }
                }

                Log?.Invoke($"Send '{payload.Type?.ToDisplayString()}' ({payload.Flags}), receive '{ret.Type?.ToDisplayString()}' ({ret.Flags}), context: {contextKind}");

                Location? PickLocation(IParameterSymbol parameter)
                {
                    if (!parameter.Locations.IsDefaultOrEmpty) return parameter.Locations[0];
                    return Utils.PickLocation(ref context, method);
                }
                void AssertContext(IParameterSymbol parameter)
                {
                    if (contextKind == ContextKind.Invalid)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InvalidContextType, PickLocation(parameter)));
                    }
                }
                void AssertPayload(IParameterSymbol parameter)
                {
                    if ((payload.Flags & DataTypeFlags.InvalidType) != 0
                        || (payload.Flags & (DataTypeFlags.IsAsync | DataTypeFlags.IsStream)) == DataTypeFlags.IsAsync) // [Value]Task parameter
                    {
                        context.ReportDiagnostic(Diagnostic.Create(InvalidPayloadType, PickLocation(parameter)));
                    }
                    else if ((payload.Flags & (DataTypeFlags.DataContract | DataTypeFlags.IsEmpty)) == 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(PossiblyNotSerializable, PickLocation(parameter)));
                    }
                }
            }

        }

        enum ContextKind
        {
            None,
            CallContext,
            CancellationToken,
            Invalid,
        }

        [Flags]
        enum DataTypeFlags
        {
            None = 0,
            DataContract = 1 << 0,
            InvalidType = 1 << 1,
            IsAsync = 1 << 2,
            IsStream = 1 << 3,
            IsEmpty = 1 << 4,
            IsValueTask = 1 << 8,
        }

        static bool TryResolveContextKind(ITypeSymbol symbol, out ContextKind kind)
        {
            if (symbol is not INamedTypeSymbol named)
            {
                kind = ContextKind.Invalid;
                return false;
            }
            if (named.Name == nameof(CancellationToken) && named.InNamespace("System", "Threading"))
            {
                kind = ContextKind.CancellationToken;
                return true;
            }
            if (named.Name == "CallContext" && named.InNamespace("ProtoBuf", "Grpc"))
            {
                kind = ContextKind.CancellationToken;
                return true;
            }
            if ((named.Name == "ServerCallContext" || named .Name == "CallOptions") && named.InNamespace("Grpc","Core"))
            {
                kind = ContextKind.Invalid; // we don't support these; use CallContext instead
                return true; // but we recognised it!
            }

            kind = ContextKind.None;
            return false;
        }

        static (INamedTypeSymbol? Type, DataTypeFlags Flags) ResolveDataType(ITypeSymbol symbol)
        {
            if (symbol is INamedTypeSymbol named)
            {
                var flags = ResolveDataType(ref named);
                return (named, flags);
            }
            return (null, DataTypeFlags.InvalidType);
        }
        static DataTypeFlags ResolveDataType(ref INamedTypeSymbol symbol)
        {
            if (symbol.SpecialType == SpecialType.System_Void) return DataTypeFlags.IsEmpty;

            // if the compiler recognises it; it isn't a good thing!
            if (symbol.SpecialType != SpecialType.None)
                return DataTypeFlags.InvalidType;

            if (TryResolveContextKind(symbol, out _))
                return DataTypeFlags.InvalidType; // if it is a context kind, it isn't a payload!

            if (symbol.IsValueType)
            {
                if (symbol.Name == "ValueTask" && symbol.InNamespace("System", "Threading", "Tasks"))
                {
                    if (symbol.IsGenericType)
                        return ResolveSimpleSingleGenericParameterKind(ref symbol) | DataTypeFlags.IsAsync | DataTypeFlags.IsValueTask;
                    return DataTypeFlags.IsEmpty | DataTypeFlags.IsAsync | DataTypeFlags.IsValueTask;
                }

                return DataTypeFlags.InvalidType; // don't allow most value-types
            }

            if (symbol.Name == "Task" && symbol.InNamespace("System","Threading","Tasks"))
            {
                if (symbol.IsGenericType)
                    return ResolveSimpleSingleGenericParameterKind(ref symbol) | DataTypeFlags.IsAsync;
                return DataTypeFlags.IsEmpty | DataTypeFlags.IsAsync;
            }

            if (symbol.Name == "IAsyncEnumerable" && symbol.InNamespace("System", "Collections", "Generic"))
            {
                if (symbol.IsGenericType)
                    return ResolveSimpleSingleGenericParameterKind(ref symbol) | DataTypeFlags.IsAsync | DataTypeFlags.IsStream;
            }

            foreach (var attrib in symbol.GetAttributes())
            {
                var ac = attrib.AttributeClass;
                if (ac?.Name == nameof(ProtoContractAttribute) && ac.InProtoBufNamespace())
                    return DataTypeFlags.DataContract;
            }
            return DataTypeFlags.None;
        }

        static DataTypeFlags ResolveSimpleSingleGenericParameterKind(ref INamedTypeSymbol symbol)
        {
            if (symbol.TypeArguments.Length != 1)
                return DataTypeFlags.InvalidType;

            if (symbol.TypeArguments[0] is not INamedTypeSymbol inner)
                return DataTypeFlags.InvalidType;

            var flags = ResolveDataType(ref inner);

            switch (flags)
            {
                case DataTypeFlags.None:
                case DataTypeFlags.DataContract:
                    symbol = inner;
                    return flags; // fine
                default:
                    return DataTypeFlags.InvalidType; // the first T needs to be simple; can't be Task<Task<Foo>> etc

            }
        }

        static string GetDefaultName(ITypeSymbol contractType)
        {   // ported from protobuf-net.Grpc
            var serviceName = contractType.Name;

            if (contractType.TypeKind == TypeKind.Interface && serviceName.StartsWith("I")) serviceName = serviceName.Substring(1); // IFoo => Foo
            serviceName = contractType.ContainingNamespace.Qualified(serviceName);
            serviceName = serviceName.Replace('+', '.'); // nested types

            //int cut;
            //if (contractType.IsGenericType && (cut = serviceName.IndexOf('`')) >= 0)
            //{
            //    var parts = GetGenericParts(contractType);
            //    serviceName = serviceName.Substring(0, cut)
            //        + "_" + string.Join("_", parts);
            //}

            return serviceName ?? "";
        }
    }
}
