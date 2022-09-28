using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.Internal.CodeGen;
using ProtoBuf.Reflection.Internal.CodeGen;
using System.Collections.Generic;
using System.Linq;

namespace ProtoBuf.BuildTools.Analyzers;

[Generator(LanguageNames.CSharp)]
public sealed class DataContractGenerator : ISourceGenerator
{
    void ISourceGenerator.Initialize(GeneratorInitializationContext context) { }

    void ISourceGenerator.Execute(GeneratorExecutionContext context)
    {
        var parser = new CodeGenSemanticModelParser();
        foreach (var tree in context.Compilation.SyntaxTrees)
        {
            parser.Parse(context.Compilation, tree);
        }
        var parsedFromCode = parser.Process();
        if (parsedFromCode.Files.Count == 0) return;
        foreach (var codeFile in CodeGenCSharpCodeGenerator.Default.Generate(parsedFromCode))
        {
            context.AddSource(codeFile.Name, codeFile.Text);
        }
    }
}
