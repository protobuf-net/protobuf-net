#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.BuildTools.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.Generators.Abstractions;
using ProtoBuf.Internal.RoslynUtils;
using ProtoBuf.Reflection;

namespace ProtoBuf.Generators
{
    /// <summary>
    /// Generates ProtoUnion-classes implementation
    /// </summary>
    public sealed class ProtoUnionGenerator : GeneratorBase
    {
        public override void Execute(GeneratorExecutionContext context)
        {
            Startup(context);
            var unionClassesToGenerate = GetUnionClassesToGenerate(context.Compilation, context.CancellationToken);
            if (!unionClassesToGenerate.Any())
            {
                Log("No classes marked with ProtoUnion attribute found, skipping protoUnionGenerator");
            }
            
            foreach (var unionClass in unionClassesToGenerate)
            {
                var codeFile = BuildCodeFile(unionClass);
                if (codeFile is null) continue;
                
                context.AddSource(codeFile.Name, SourceText.From(codeFile.Text, Encoding.UTF8));
            }
        }

        CodeFile? BuildCodeFile(ClassDeclarationSyntax classDeclarationSyntax)
        {
            return new CodeFile("", "");
        }
        
        private ClassDeclarationSyntax[] GetUnionClassesToGenerate(Compilation compilation, CancellationToken cancellationToken)
        {
            return compilation.SyntaxTrees
                .SelectMany(t => t.GetRoot(cancellationToken).DescendantNodes())
                .OfType<ClassDeclarationSyntax>()
                    // taking only classes with `partial` modifier
                .Where(syntax => syntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
                    // only classes which have at least one `ProtoUnion` attribute
                .Where(classDeclaration => classDeclaration.ContainsAttribute(compilation, typeof(ProtoUnionAttribute)))
                .ToArray();
        }
    }
}
