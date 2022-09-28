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
    void ISourceGenerator.Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(static () => new DataContractSyntaxReceiver());
    }

    void ISourceGenerator.Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxReceiver is not DataContractSyntaxReceiver rec || rec.IsEmpty) return;
        rec.Execute(in context);
    }

    sealed class DataContractSyntaxReceiver : ISyntaxReceiver
    {
        public bool IsEmpty => _types.Count == 0;
        private readonly HashSet<TypeDeclarationSyntax> _types = new();

        public DataContractSyntaxReceiver() { }

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // when scanning the syntax tree, we'll keep things simple and gather
            // everything that looks likely
            if (syntaxNode is TypeDeclarationSyntax type // is a class
                && type.Modifiers.Any(SyntaxKind.PartialKeyword) // only look at partials
                && HasAttribute(type, "ProtoContract"))
            {
                _types.Add(type);
            }

            static bool HasAttribute(TypeDeclarationSyntax type, string name)
            {
                foreach (var lists in type.AttributeLists)
                {
                    foreach (var attrib in lists.Attributes)
                    {
                        var text = attrib.Name.ToString();
                        if (text == name) return true;
                        if (text.Length >= name.Length + 1
                            && text.EndsWith(name)
                            && text[text.Length - name.Length - 1] == '.') return true;

                        if (text.Length >= name.Length + 10
                            && text.EndsWith("Attribute")
                            && text[text.Length - name.Length - 10] == '.'
                            && text.LastIndexOf(name) == text.Length - 9) return true;
                    }
                }
                return false;
            }
        }

        internal void Execute(in GeneratorExecutionContext context)
        {
            var parser = new CodeGenSemanticModelParser();
            foreach (var type in _types)
            {
                var symbol = context.Compilation.GetSemanticModel(type.SyntaxTree).GetDeclaredSymbol(type, context.CancellationToken);
                parser.Parse(symbol);
            }
            var parsedFromCode = parser.Process();
            if (parsedFromCode.Files.Count == 0) return;
            foreach (var codeFile in CodeGenCSharpCodeGenerator.Default.Generate(parsedFromCode))
            {
                context.AddSource(codeFile.Name, codeFile.Text);
            }
        }
    }
}
