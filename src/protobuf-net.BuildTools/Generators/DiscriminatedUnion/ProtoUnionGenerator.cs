#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.Generators.Abstractions;
using ProtoBuf.Internal.ProtoUnion;
using ProtoBuf.Internal.RoslynUtils;
using ProtoBuf.Reflection;

namespace ProtoBuf.Generators.DiscriminatedUnion
{
    /// <summary>
    /// Generates implementation of the classes marked with `ProtoUnion`
    /// </summary>
    public sealed partial class ProtoUnionGenerator : GeneratorBase
    {
        public override void Execute(GeneratorExecutionContext context)
        {
            Startup(context);
            if (!TryDetectCodeGenerator(context, out var codeGenerator, out var langVersion))
            {
                Log("Failed to find suitable codeGenerator");
                return;
            }
            if (codeGenerator is not CSharpCodeGenerator cSharpCodeGenerator)
            {
                Log("Non-CSharp code gen is not supported for ProtoUnion");
                return;
            }
            
            var unionClassesToGenerate = GetUnionClassesToGenerate(context.Compilation, context.CancellationToken);
            if (!unionClassesToGenerate.Any())
            {
                Log($"No classes marked with ProtoUnion attribute found, skipping {nameof(ProtoUnionGenerator)}");
                return;
            }

            var descriptors = new List<ProtoUnionFileDescriptor>();
            foreach (var unionClass in unionClassesToGenerate)
            {
                if (!IsValidForGeneration(ref context, unionClass)) continue;
                var classDescriptors = BuildProtoUnionFileDescriptors(ref context, unionClass);
                if (classDescriptors?.Any() == true) descriptors.AddRange(classDescriptors);
            }

            if (!descriptors.Any())
            {
                Log($"No '{nameof(ProtoUnionFileDescriptor)}' were built successfully, no protoUnions to generate");
                return;
            }

            foreach (var file in cSharpCodeGenerator.Generate(descriptors))
            {
                context.AddSource(file.Name, SourceText.From(file.Text, Encoding.UTF8));
            }
        }
        
        /// <returns>Returns true, if class is valid for generation of DiscriminatedUnion, false otherwise.</returns>
        /// <remarks>Not all of the validations are happening here, to not parse fully data here.</remarks>
        private static bool IsValidForGeneration(ref GeneratorExecutionContext context, ClassDeclarationSyntax classSyntax)
        {
            var isValid = true;
            var syntaxTree = classSyntax.SyntaxTree;

            if (!classSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DataContractAnalyzer.DiscriminatedUnionShouldBePartial,
                    location: Location.Create(syntaxTree, classSyntax.Identifier.Span))
                );
                
                isValid  = false;
            }

            return isValid;
        }
        
        private static ClassDeclarationSyntax[] GetUnionClassesToGenerate(Compilation compilation, CancellationToken cancellationToken)
        {
            return compilation.SyntaxTrees
                .SelectMany(t => t.GetRoot(cancellationToken).DescendantNodes())
                .OfType<ClassDeclarationSyntax>()
                .Where(classDeclaration => classDeclaration.ContainsAttribute(compilation, typeof(ProtoUnionAttribute<>)))
                .ToArray();
        }
    }
}
