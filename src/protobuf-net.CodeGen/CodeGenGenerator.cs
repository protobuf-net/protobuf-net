using CodeGeneration.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoBuf.CodeGen
{
    internal class CodeGenGenerator : ICodeGenerator
    {
        public CodeGenGenerator(AttributeData attributeData)
        {
            if (attributeData == null) throw new ArgumentNullException(nameof(attributeData));
        }

        public Task<SyntaxList<MemberDeclarationSyntax>> GenerateAsync(MemberDeclarationSyntax applyTo, CSharpCompilation compilation, IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            var results = SyntaxFactory.List<MemberDeclarationSyntax>();
            // Our generator is applied to any class that our attribute is applied to.
            var applyToClass = (ClassDeclarationSyntax)applyTo;
            var applyToTypeName = SyntaxFactory.IdentifierName(applyToClass.Identifier);
            var serializer = SyntaxFactory.ClassDeclaration(applyToClass.Identifier + "Serializer");
            var model = compilation.GetSemanticModel(applyTo.SyntaxTree);

            var fullSerializerInterfaceName =
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.AliasQualifiedName(SyntaxFactory.IdentifierName("global"), SyntaxFactory.IdentifierName(nameof(ProtoBuf))),
                    SyntaxFactory.GenericName(SyntaxFactory.Identifier(compilation.GetTypeByMetadataName(typeof(ISerializer<>).FullName).Name),
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(applyToTypeName))));

            serializer = serializer.AddBaseListTypes(SyntaxFactory.SimpleBaseType(fullSerializerInterfaceName));

            var readerIdentifier = SyntaxFactory.Identifier("reader");
            var writerIdentifier = SyntaxFactory.Identifier("writer");
            var valueIdentifier = SyntaxFactory.Identifier("value");
            var read = SyntaxFactory
                .MethodDeclaration(SyntaxFactory.IdentifierName("void"), nameof(ISerializer<object>.Read))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                // .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(fullSerializerInterfaceName))
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.ParseTypeName($"global::{nameof(ProtoBuf)}.{nameof(ProtoReader)}"), readerIdentifier, null),
                    SyntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)), applyToTypeName, valueIdentifier, null)
                ).WithBody(SyntaxFactory.Block());

            var write = SyntaxFactory
                .MethodDeclaration(SyntaxFactory.IdentifierName("void"), nameof(ISerializer<object>.Write))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                // .WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(fullSerializerInterfaceName))
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), default(SyntaxTokenList), SyntaxFactory.ParseTypeName($"global::{nameof(ProtoBuf)}.{nameof(ProtoWriter)}"), writerIdentifier, null),
                    SyntaxFactory.Parameter(default(SyntaxList<AttributeListSyntax>), SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.RefKeyword)), applyToTypeName, valueIdentifier, null)
                ).WithBody(SyntaxFactory.Block());
            serializer = serializer.AddMembers(read, write);

            results = results.Add(serializer);

            //foreach (var member in applyToClass.Members)
            //{
            //    switch(member)
            //    {
            //        case FieldDeclarationSyntax field:
            //            var symbol = GetSingleFieldSymbol(field, model);
            //            results.Add(SyntaxFactory.ClassDeclaration(symbol.Name)); 
            //            break;
            //    }
            //}


            return Task.FromResult<SyntaxList<MemberDeclarationSyntax>>(results);
        }

        // derived from https://gist.github.com/frankbryce/a4ee2bf799ab3878ae91
        public static TypeSyntax GetTypeSyntax(SyntaxToken identifier, params TypeSyntax[] arguments)
        {
            return
                SyntaxFactory.GenericName(
                    identifier,
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SeparatedList(
                            arguments.Select(
                                x =>
                                {
                                    if (x is GenericNameSyntax)
                                    {
                                        var gen_x = x as GenericNameSyntax;
                                        return
                                            GetTypeSyntax(
                                                gen_x.Identifier,
                                                gen_x.TypeArgumentList.Arguments.ToArray()
                                            );
                                    }
                                    else
                                    {
                                        return x;
                                    }
                                }
                            )
                        )
                    )
                );
        }

        private ISymbol GetSingleFieldSymbol(FieldDeclarationSyntax field, SemanticModel model)
        {
            var iter = field.Declaration.Variables.GetEnumerator();
            if (!iter.MoveNext()) return null;
            var fieldSymbol = model.GetDeclaredSymbol(iter.Current);
            if (iter.MoveNext()) return null;
            return fieldSymbol;
        }
    }
}