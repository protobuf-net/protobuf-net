using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace BuildToolsUnitTests.CodeFixes.Abstractions
{
    public abstract class CodeFixProviderTestsBase
    {
        protected async Task RunCodeFixTestAsync<TDiagnosticAnalyzer, TCodeFixProvider>(
            string sourceCode,
            string expectedCode,
            DiagnosticDescriptor diagnosticDescriptor)
                where TDiagnosticAnalyzer : DiagnosticAnalyzer, new()
                where TCodeFixProvider : CodeFixProvider, new()
        {
            var codeFixTest = new CSharpCodeFixTest<TDiagnosticAnalyzer, TCodeFixProvider, XUnitVerifier>
            {
                TestState = { Sources = { sourceCode } },
                FixedState = { Sources = { expectedCode } }
            };

            codeFixTest.TestState.AdditionalReferences.AddRange(MetadataReferenceHelpers.ProtoBufReferences);
            codeFixTest.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult());

            await codeFixTest.RunAsync();
        }
    }
}
