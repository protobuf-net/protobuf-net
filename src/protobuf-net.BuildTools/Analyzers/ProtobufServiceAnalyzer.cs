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
    public class ProtoBufServiceAnalyzer : DiagnosticAnalyzer, ILoggingAnalyzer
    {
        private event Action<string>? Log;
        event Action<string>? ILoggingAnalyzer.Log
        {
            add => this.Log += value;
            remove => this.Log -= value;
        }

        internal static readonly DiagnosticDescriptor InvalidDataParameter = new(
            id: "PBN2001",
            title: nameof(ProtoBufServiceAnalyzer) + "." + nameof(InvalidDataParameter),
            messageFormat: "The data parameter must be Void, a reference-type data contract, or an async sequence of the same.",
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

        private static readonly ImmutableArray<DiagnosticDescriptor> s_SupportedDiagnostics = Utils.GetDeclared(typeof(ProtoBufServiceAnalyzer));

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
                    serviceName = attrib.TryGetStringByName("Name", out string tmp) ? tmp : type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
                    break;
                }
            }
            if (serviceName is null) return;

            Log?.Invoke($"name is {serviceName}");
        }
    }
}
