#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.BuildTools.Internal;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoBuf.CodeFixes
{
    /// <summary>
    /// Offers the remedies for PBN0027, a non-nullable collection that nothing initializes on
    /// deserialize: declare it nullable, initialize it, or mark it <c>[NullWrappedCollection]</c>.
    /// </summary>
    /// <remarks>
    /// Each is offered only where it works, which the analyzer decides and passes along: an
    /// initializer does nothing under <c>SkipConstructor</c> or on a struct contract, since no
    /// constructor runs; and null-wrapping does nothing on a member that is never written. The
    /// null-wrapping title says it changes the wire format, because it does - the collection moves
    /// inside a wrapper message, which existing payloads and the old schema do not have - and a
    /// lightbulb is exactly where that is easy to accept without reading about it.
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NonNullableCollectionCodeFixProvider)), Shared]
    public class NonNullableCollectionCodeFixProvider : CodeFixProvider
    {
        internal const string NullableKey = "PBN0027.Nullable", InitializeKey = "PBN0027.Initialize", NullWrapKey = "PBN0027.NullWrap";

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DataContractAnalyzer.NonNullableCollectionLeftNull.Id);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (root is null || model is null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                // the diagnostic sits on the member's name: a property, a field's declarator, or a
                // positional record parameter
                var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
                var declaration = token.Parent;
                if (declaration is not (PropertyDeclarationSyntax or VariableDeclaratorSyntax or ParameterSyntax)) continue;
                if (DeclaredType(declaration) is not { } type || type is NullableTypeSyntax) continue;
                var name = token.ValueText;

                // the type is shared by every declarator in `List<int> a = new(), b;`, and only one
                // of them was reported
                if (declaration is not VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Variables.Count: > 1 } })
                {
                    context.RegisterCodeFix(CodeAction.Create(
                        title: $"Declare '{name}' nullable",
                        createChangedDocument: _ => Task.FromResult(context.Document.WithSyntaxRoot(
                            DeclareNullable(root, declaration, type))),
                        equivalenceKey: NullableKey), diagnostic);
                }

                if (Flag(diagnostic, DataContractContext.CollectionLeftNullConstructedKey)
                    && declaration is PropertyDeclarationSyntax or VariableDeclaratorSyntax
                    && model.GetTypeInfo(type, context.CancellationToken).Type is { } typeSymbol
                    && EmptyValue(typeSymbol, model, type.SpanStart) is { } empty)
                {
                    context.RegisterCodeFix(CodeAction.Create(
                        title: $"Initialize '{name}' to {empty}",
                        createChangedDocument: _ => Task.FromResult(context.Document.WithSyntaxRoot(
                            root.ReplaceNode(declaration, WithInitializer(declaration, SyntaxFactory.ParseExpression(empty))
                                .WithAdditionalAnnotations(Formatter.Annotation)))),
                        equivalenceKey: InitializeKey), diagnostic);
                }

                if (Flag(diagnostic, DataContractContext.CollectionLeftNullHasProtoMemberKey)
                    && FindProtoMember(declaration, model, context.CancellationToken) is { } protoMember)
                {
                    context.RegisterCodeFix(CodeAction.Create(
                        title: $"Mark '{name}' [NullWrappedCollection] (changes the wire format)",
                        createChangedDocument: _ => Task.FromResult(context.Document.WithSyntaxRoot(
                            root.ReplaceNode(protoMember.Parent!, AddNullWrappedCollection(protoMember)))),
                        equivalenceKey: NullWrapKey), diagnostic);
                }
            }
        }

        private static bool Flag(Diagnostic diagnostic, string key)
            => diagnostic.Properties.TryGetValue(key, out var value) && value == "true";

        private static TypeSyntax? DeclaredType(SyntaxNode declaration) => declaration switch
        {
            PropertyDeclarationSyntax property => property.Type,
            VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax variables } => variables.Type,
            ParameterSyntax parameter => parameter.Type,
            _ => null,
        };

        // `T` becomes `T?`, and a `= null!` or `= default!` that only existed to silence CS8618 goes,
        // since it no longer silences anything
        private static SyntaxNode DeclareNullable(SyntaxNode root, SyntaxNode declaration, TypeSyntax type)
        {
            var nullable = SyntaxFactory.NullableType(type.WithoutTrivia()).WithTriviaFrom(type);
            switch (declaration)
            {
                // a field's type belongs to the enclosing declaration, not to the declarator
                case VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax variables } variable:
                    var declarator = variable.Initializer is { } assigned && IsSpelledOutNull(assigned.Value)
                        ? variable.WithInitializer(null).WithIdentifier(variable.Identifier.WithoutTrivia())
                        : variable;
                    return root.ReplaceNode(variables, variables.ReplaceNode(variable, declarator).WithType(nullable));
                case PropertyDeclarationSyntax { Initializer.Value: var value, AccessorList: { } accessors } property when IsSpelledOutNull(value):
                    return root.ReplaceNode(property, property.WithType(nullable).WithInitializer(null).WithSemicolonToken(default)
                        .WithAccessorList(accessors.WithTrailingTrivia(property.SemicolonToken.TrailingTrivia)));
                default:
                    return root.ReplaceNode(type, nullable);
            }
        }

        private static bool IsSpelledOutNull(ExpressionSyntax value)
        {
            while (value is PostfixUnaryExpressionSyntax postfix && postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            {
                value = postfix.Operand;
            }
            return value is DefaultExpressionSyntax
                || value.IsKind(SyntaxKind.NullLiteralExpression)
                || value.IsKind(SyntaxKind.DefaultLiteralExpression);
        }

        // only what can be written without guessing: an empty one-dimensional array, or a class with
        // a public parameterless constructor. An interface or an immutable type would need a
        // concrete type chosen for it, and a wrong guess is worse than no offer. Type names are the
        // minimal spelling valid at the member, and `System.Array` is left to the simplifier, which
        // shortens it where System is imported (it will not strip a `global::` short of that)
        private static string? EmptyValue(ITypeSymbol type, SemanticModel model, int position)
        {
            switch (type)
            {
                case IArrayTypeSymbol { Rank: 1 } array:
                    return $"System.Array.Empty<{array.ElementType.ToMinimalDisplayString(model, position)}>()";
                case INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false } named
                    when named.InstanceConstructors.Any(ctor => ctor.Parameters.IsEmpty && ctor.DeclaredAccessibility == Accessibility.Public):
                    return model.SyntaxTree.Options is CSharpParseOptions { LanguageVersion: >= LanguageVersion.CSharp9 }
                        ? "new()"
                        : $"new {named.ToMinimalDisplayString(model, position)}()";
                default:
                    return null;
            }
        }

        // replaces a spelled-out `= null!` as readily as it adds one where there is none; spacing is
        // left to the formatter, via the annotation the caller adds, and names to the simplifier -
        // annotated last, since re-triviaing the value builds a new node
        private static SyntaxNode WithInitializer(SyntaxNode declaration, ExpressionSyntax value)
        {
            var initializer = SyntaxFactory.EqualsValueClause(value.WithAdditionalAnnotations(Simplifier.Annotation));
            return declaration switch
            {
                PropertyDeclarationSyntax { Initializer: { } existing } property
                    => property.WithInitializer(existing.WithValue(value.WithTriviaFrom(existing.Value).WithAdditionalAnnotations(Simplifier.Annotation))),
                PropertyDeclarationSyntax property
                    => property.WithAccessorList(property.AccessorList!.WithoutTrailingTrivia())
                        .WithInitializer(initializer)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                            .WithTrailingTrivia(property.AccessorList!.GetTrailingTrivia())),
                VariableDeclaratorSyntax { Initializer: { } existing } variable
                    => variable.WithInitializer(existing.WithValue(value.WithTriviaFrom(existing.Value).WithAdditionalAnnotations(Simplifier.Annotation))),
                VariableDeclaratorSyntax variable
                    => variable.WithInitializer(initializer),
                _ => declaration,
            };
        }

        // the [ProtoMember] on the member itself - on the declaration, or on a record parameter
        // under `property:` - so the new attribute lands in the same list, with the same target.
        // Resolved through the semantic model, which is what sees through `using PM = ...`
        private static AttributeSyntax? FindProtoMember(SyntaxNode declaration, SemanticModel model, CancellationToken cancellationToken)
        {
            var lists = declaration switch
            {
                PropertyDeclarationSyntax property => property.AttributeLists,
                VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax field } => field.AttributeLists,
                ParameterSyntax parameter => parameter.AttributeLists,
                _ => default,
            };
            return lists.SelectMany(list => list.Attributes).FirstOrDefault(attrib
                => model.GetSymbolInfo(attrib, cancellationToken).Symbol?.ContainingType is { Name: nameof(ProtoMemberAttribute) } type
                    && type.InProtoBufNamespace());
        }

        // written fully qualified and left to the simplifier, so that it reads `NullWrappedCollection`
        // where ProtoBuf is imported and stays valid where it is not - however [ProtoMember] was spelled
        private static AttributeListSyntax AddNullWrappedCollection(AttributeSyntax protoMember)
        {
            var list = (AttributeListSyntax)protoMember.Parent!;
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName("global::ProtoBuf.NullWrappedCollection"))
                .WithAdditionalAnnotations(Simplifier.Annotation);
            return list.WithAttributes(list.Attributes.Insert(list.Attributes.IndexOf(protoMember) + 1, attribute));
        }
    }
}
