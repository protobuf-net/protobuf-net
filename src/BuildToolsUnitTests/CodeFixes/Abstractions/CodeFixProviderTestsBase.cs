using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Reflection;
using System.Linq;
using System.Runtime.Versioning;
using ProtoBuf.BuildTools.Analyzers;

namespace BuildToolsUnitTests.CodeFixes.Abstractions
{
    public abstract class CodeFixProviderTestsBase
    {
        static TargetFrameworkAttribute CurrentRunningAssemblyTargetFramework 
            => (TargetFrameworkAttribute)Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
                .Single();

        protected async Task RunCodeFixTestAsync<TDiagnosticAnalyzer, TCodeFixProvider>(
            string sourceCode,
            string expectedCode,
            DiagnosticResult? diagnosticResult = null,
            params DiagnosticResult[] standardExpectedDiagnostics)
                where TDiagnosticAnalyzer : DiagnosticAnalyzer, new()
                where TCodeFixProvider : CodeFixProvider, new()
        {
            var codeFixTest = BuildCSharpCodeFixTest<TDiagnosticAnalyzer, TCodeFixProvider>(sourceCode, expectedCode);

            if (diagnosticResult is not null)
            {
                // expect a diagnostic in sourceCode
                codeFixTest.TestState.ExpectedDiagnostics.Add(diagnosticResult.Value);
            }

            // we expect some standard diagnostics in both of code compilations
            AddStandardDiagnostics(codeFixTest.TestState, standardExpectedDiagnostics);
            AddStandardDiagnostics(codeFixTest.FixedState, standardExpectedDiagnostics);

            await codeFixTest.RunAsync();
        }

        CSharpCodeFixTest<TDiagnosticAnalyzer, TCodeFixProvider, XUnitVerifier> BuildCSharpCodeFixTest<TDiagnosticAnalyzer, TCodeFixProvider>(string sourceCode, string expectedCode)
            where TDiagnosticAnalyzer : DiagnosticAnalyzer, new()
            where TCodeFixProvider : CodeFixProvider, new()
        {
            var codeFixTest = new CSharpCodeFixTest<TDiagnosticAnalyzer, TCodeFixProvider, XUnitVerifier>
            {
                ReferenceAssemblies = new ReferenceAssemblies(CurrentRunningAssemblyTargetFramework.FrameworkDisplayName!),
                TestState = { Sources = { sourceCode }, OutputKind = OutputKind.DynamicallyLinkedLibrary },
                FixedState = { Sources = { expectedCode }, OutputKind = OutputKind.DynamicallyLinkedLibrary }
            };

            //codeFixTest.TestState.OutputKind = OutputKind.DynamicallyLinkedLibrary;
            codeFixTest.SolutionTransforms.Add((solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                project = project!.WithCompilationOptions(project.CompilationOptions!.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));
                return solution;
            });

            AddAdditionalReferences(codeFixTest.TestState);
            AddAdditionalReferences(codeFixTest.FixedState);            

            return codeFixTest;
        }

        private static void AddAdditionalReferences(SolutionState solutionState)
        {
            solutionState.AdditionalReferences.AddRange(MetadataReferenceHelpers.ProtoBufReferences);
            solutionState.AdditionalReferences.AddRange(MetadataReferenceHelpers.WellKnownReferences);
        }

        private static void AddStandardDiagnostics(SolutionState solutionState, params DiagnosticResult[] diagnosticResults)
        {
            if (diagnosticResults is null || diagnosticResults.Length <= 0) return;
            foreach (var diagnosticResult in diagnosticResults)
            {
                solutionState.ExpectedDiagnostics.Add(diagnosticResult);
            }
        }
    }
}
