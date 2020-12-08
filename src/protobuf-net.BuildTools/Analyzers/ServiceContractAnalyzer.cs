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
