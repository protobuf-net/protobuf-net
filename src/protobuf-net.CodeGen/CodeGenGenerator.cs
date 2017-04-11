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
            var serializer = SyntaxFactory.ClassDeclaration(applyToClass.Identifier + "Serializer");
            var iSerializertype = compilation.GetTypeByMetadataName(typeof(ISerializer<>).FullName);
            var model = compilation.GetSemanticModel(applyTo.SyntaxTree);

            var typeArgs = SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SeparatedList(new TypeSyntax[] { SyntaxFactory.IdentifierName(applyToClass.Identifier) }));

            serializer = serializer.AddBaseListTypes(SyntaxFactory.SimpleBaseType(
                SyntaxFactory.GenericName(SyntaxFactory.Identifier(iSerializertype.Name), typeArgs)));

            var read = SyntaxFactory.MethodDeclaration(SyntaxFactory.IdentifierName("void"), "Read");
            read = read.AddParameterListParameters(SyntaxFactory.Parameter(
                default(SyntaxList<AttributeListSyntax>), SyntaxFactory. default(SyntaxTokenList), SyntaxFactory.IdentifierName("ProtoReader"), SyntaxFactory.Identifier("reader"), null));
            read = read.WithBody(SyntaxFactory.Block());
            serializer = serializer.AddMembers(read);

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