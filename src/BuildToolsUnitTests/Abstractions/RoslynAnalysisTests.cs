using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BuildToolsUnitTests.Abstractions
{
    public abstract class RoslynAnalysisTests
    {
        protected CompilationUnitSyntax BuildCompilationUnitSyntax(string programText)
        {
            var tree = CSharpSyntaxTree.ParseText(programText);
            return tree.GetCompilationUnitRoot();
        }
    }
}