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
using ProtoBuf.Internal.Models;
using ProtoBuf.Internal.ProtoUnion;
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
            if (!TryDetectCodeGenerator(context, out var codeGenerator, out var langVersion))
            {
                Log("Failed to find suitable codeGenerator");
                return;
            }
            if (codeGenerator is not CSharpCodeGenerator cSharpCodeGenerator)
            {
                Log("Non CSharp code gen is not supported for ProtoUnion");
                return;
            }
            
            var unionClassesToGenerate = GetUnionClassesToGenerate(context.Compilation, context.CancellationToken);
            if (!unionClassesToGenerate.Any())
            {
                Log($"No classes marked with {nameof(ProtoUnionAttribute)} found, skipping {nameof(ProtoUnionGenerator)}");
                return;
            }

            var descriptors = new List<ProtoUnionClassDescriptor>();
            foreach (var unionClass in unionClassesToGenerate)
            {
                if (!IsValidForGeneration(ref context, unionClass)) continue;
                var descriptor = BuildProtoUnionClassDescriptor(context, unionClass);
                if (descriptor is not null) descriptors.Add(descriptor);
            }

            if (!descriptors.Any())
            {
                Log($"No '{nameof(ProtoUnionClassDescriptor)}' were built successfully, no protoUnions to generate");
                return;
            }

            foreach (var file in cSharpCodeGenerator.Generate(descriptors))
            {
                context.AddSource(file.Name, SourceText.From(file.Text, Encoding.UTF8));
            }
        }

        ProtoUnionClassDescriptor? BuildProtoUnionClassDescriptor(GeneratorExecutionContext context, ClassDeclarationSyntax classSyntax)
        {
            var protoUnionFields = GetProtoUnionFields(context.Compilation, classSyntax);
            if (!protoUnionFields.Any())
            {
                Log($"No protoUnionFields parsed, codeFile generation is stopped for {classSyntax}");
                return null;
            }

            return new ProtoUnionClassDescriptor
            {
                // todo add class name and other stuff here
                UnionFields = protoUnionFields
            };
        }

        private ISet<ProtoUnionField> GetProtoUnionFields(Compilation compilation, ClassDeclarationSyntax classSyntax)
        {
            var attributes = classSyntax.GetAttributeSyntaxesOfType(typeof(ProtoUnionAttribute));
            var protoUnionFields = new HashSet<ProtoUnionField>();
            foreach (var attributeSyntax in attributes)
            {
                if (!ProtoUnionField.TryCreate(compilation, attributeSyntax, out var field))
                {
                    Log($"Failed to parse protoUnionField from {attributeSyntax.ToFullString()}");
                    continue;
                }

                protoUnionFields.Add(field);
            }

            return protoUnionFields;
        }
        
        private static bool IsValidForGeneration(ref GeneratorExecutionContext context, ClassDeclarationSyntax classSyntax)
        {
            if (!classSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.PartialKeyword)))
            {
                // context.ReportDiagnostic(Diagnostic.Create());
                return false;
            }

            return true;
        }
        
        private static ClassDeclarationSyntax[] GetUnionClassesToGenerate(Compilation compilation, CancellationToken cancellationToken)
        {
            return compilation.SyntaxTrees
                .SelectMany(t => t.GetRoot(cancellationToken).DescendantNodes())
                .OfType<ClassDeclarationSyntax>()
                .Where(classDeclaration => classDeclaration.ContainsAttribute(compilation, typeof(ProtoUnionAttribute)))
                .ToArray();
        }
    }
}
