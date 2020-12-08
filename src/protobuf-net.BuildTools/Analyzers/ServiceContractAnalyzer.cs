#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Immutable;

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
            messageFormat: "The member '{0}' is not a method; only methods are supported for services.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidDataParameter = new(
            id: "PBN2002",
            title: nameof(ServiceContractAnalyzer) + "." + nameof(InvalidDataParameter),
            messageFormat: "The data parameter must currently be Void, a reference-type data contract, or an async sequence of the same.",
            category: Literals.CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidReturnValue = new(
            id: "PBN2003",
            title: nameof(ServiceContractAnalyzer) + "." + nameof(InvalidReturnValue),
            messageFormat: "The return value must currently be Void, a reference-type data contract, or an task / async sequence of the same.",
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
            if (context.ContainingSymbol is not ITypeSymbol type) return;

            var attribs = type.GetAttributes();
            string? serviceName = null;
            foreach (var attrib in attribs)
            {
                var ac = attrib.AttributeClass;
                if (ac?.Name == "ServiceAttribute" && ac.InProtoBufGrpcConfigurationNamespace())
                {
                    attrib.TryGetStringByName("Name", out serviceName);
                    if (string.IsNullOrWhiteSpace(serviceName)) serviceName = GetDefaultName(type);
                    break;
                }
            }
            if (string.IsNullOrWhiteSpace(serviceName)) return;

            Log?.Invoke($"Service detected: '{serviceName}'");

            foreach (var member in type.GetMembers())
            {
                if (member is not IMethodSymbol method)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidMemberKind, Utils.PickLocation(ref context, member), member.Name));
                    continue;
                }

                if (method.ReturnType is not INamedTypeSymbol ret)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidReturnValue, Utils.PickLocation(ref context, member)));
                    continue;
                }
                var retFlags = ResolveDataType(ref ret);
                if ((retFlags & DataTypeFlags.InvalidType) != 0)
                {
                    context.ReportDiagnostic(Diagnostic.Create(InvalidReturnValue, Utils.PickLocation(ref context, member)));
                    continue;
                }

                Log?.Invoke($"Return value resolved as '{ret.ToDisplayString()}' with features: {retFlags}");


            }
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

        static DataTypeFlags ResolveDataType(ref INamedTypeSymbol symbol)
        {
            if (symbol.SpecialType == SpecialType.System_Void) return DataTypeFlags.IsEmpty;
            
            // if the compiler recognises it; it isn't a good thing!
            if (symbol.SpecialType != SpecialType.None)
                return DataTypeFlags.InvalidType;

            if (symbol.IsValueType)
            {
                if (symbol.Name == "ValueTask" && symbol.InSystemThreadingTasksNamespace())
                {
                    if (symbol.IsGenericType)
                        return ResolveSimpleSingleGenericParameterKind(ref symbol) | DataTypeFlags.IsAsync | DataTypeFlags.IsValueTask;
                    return DataTypeFlags.IsEmpty | DataTypeFlags.IsAsync | DataTypeFlags.IsValueTask;
                }

                return DataTypeFlags.InvalidType; // don't allow most value-types
            }

            if (symbol.Name == "Task" && symbol.InSystemThreadingTasksNamespace())
            {
                if (symbol.IsGenericType)
                    return ResolveSimpleSingleGenericParameterKind(ref symbol) | DataTypeFlags.IsAsync;
                return DataTypeFlags.IsEmpty | DataTypeFlags.IsAsync;
            }

            if (symbol.Name == "IAsyncEnumerable" && symbol.InSystemCollectionsGenericNamespace())
            {
                if (symbol.IsGenericType)
                    return ResolveSimpleSingleGenericParameterKind(ref symbol) | DataTypeFlags.IsAsync | DataTypeFlags.IsStream;
            }
            // TODO: identify streaming and async

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
