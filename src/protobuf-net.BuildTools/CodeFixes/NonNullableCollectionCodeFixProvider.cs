#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
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
    /// inside a wrapper message, which existing payloads and other protobuf implementations do not
    /// have - and a lightbulb is exactly where that is easy to accept without reading about it.
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
            if (root is null) return;

            foreach (var diagnostic in context.Diagnostics)
            {
                // the diagnostic sits on the member's name: a property, a field's declarator, or a
                // positional record parameter
                var declaration = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent;
                if (declaration is not (PropertyDeclarationSyntax or VariableDeclaratorSyntax or ParameterSyntax)) continue;
                if (DeclaredType(declaration) is not { } type || type is NullableTypeSyntax) continue;
                var name = root.FindToken(diagnostic.Location.SourceSpan.Start).ValueText;

                context.RegisterCodeFix(CodeAction.Create(
                    title: $"Declare '{name}' nullable",
                    createChangedDocument: _ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(type,
                        SyntaxFactory.NullableType(type.WithoutTrivia()).WithTriviaFrom(type)))),
                    equivalenceKey: NullableKey), diagnostic);

                if (Flag(diagnostic, DataContractContext.CollectionLeftNullConstructedKey)
                    && declaration is PropertyDeclarationSyntax or VariableDeclaratorSyntax)
                {
                    var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
                    if (model?.GetTypeInfo(type, context.CancellationToken).Type is { } typeSymbol
                        && EmptyValue(typeSymbol, model, type.SpanStart) is { } empty)
                    {
                        context.RegisterCodeFix(CodeAction.Create(
                            title: $"Initialize '{name}' to {empty}",
                            createChangedDocument: _ => Task.FromResult(context.Document.WithSyntaxRoot(
                                root.ReplaceNode(declaration, WithInitializer(declaration, SyntaxFactory.ParseExpression(empty))
                                    .WithAdditionalAnnotations(Formatter.Annotation)))),
                            equivalenceKey: InitializeKey), diagnostic);
                    }
                }

                if (Flag(diagnostic, DataContractContext.CollectionLeftNullSerializedKey)
                    && FindProtoMember(declaration) is { } protoMember)
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

        // only what can be written without guessing: an empty one-dimensional array, or a class with
        // a public parameterless constructor. An interface or an immutable type would need a
        // concrete type chosen for it, and a wrong guess is worse than no offer
        private static string? EmptyValue(ITypeSymbol type, SemanticModel model, int position) => type switch
        {
            IArrayTypeSymbol { Rank: 1, ElementType: not IArrayTypeSymbol } array
                => $"new {array.ElementType.ToMinimalDisplayString(model, position)}[0]",
            INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false } named
                when named.InstanceConstructors.Any(ctor => ctor.Parameters.IsEmpty && ctor.DeclaredAccessibility == Accessibility.Public)
                => $"new {named.ToMinimalDisplayString(model, position)}()",
            _ => null,
        };

        // replaces a spelled-out `= null!` as readily as it adds one where there is none; spacing is
        // left to the formatter, via the annotation the caller adds
        private static SyntaxNode WithInitializer(SyntaxNode declaration, ExpressionSyntax value)
        {
            var initializer = SyntaxFactory.EqualsValueClause(value);
            return declaration switch
            {
                PropertyDeclarationSyntax { Initializer: { } existing } property
                    => property.WithInitializer(existing.WithValue(value.WithTriviaFrom(existing.Value))),
                PropertyDeclarationSyntax property
                    => property.WithAccessorList(property.AccessorList!.WithoutTrailingTrivia())
                        .WithInitializer(initializer)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)
                            .WithTrailingTrivia(property.AccessorList!.GetTrailingTrivia())),
                VariableDeclaratorSyntax { Initializer: { } existing } variable
                    => variable.WithInitializer(existing.WithValue(value.WithTriviaFrom(existing.Value))),
                VariableDeclaratorSyntax variable
                    => variable.WithInitializer(initializer),
                _ => declaration,
            };
        }

        // the [ProtoMember] on the member itself - on the declaration, or on a record parameter
        // under `property:` - so the new attribute lands in the same list, with the same target
        private static AttributeSyntax? FindProtoMember(SyntaxNode declaration)
        {
            var lists = declaration switch
            {
                PropertyDeclarationSyntax property => property.AttributeLists,
                VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax field } => field.AttributeLists,
                ParameterSyntax parameter => parameter.AttributeLists,
                _ => default,
            };
            return lists.SelectMany(list => list.Attributes)
                .FirstOrDefault(attrib => RightmostName(attrib.Name) is "ProtoMember" or "ProtoMemberAttribute");
        }

        private static string RightmostName(NameSyntax name) => name switch
        {
            QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax alias => alias.Name.Identifier.ValueText,
            SimpleNameSyntax simple => simple.Identifier.ValueText,
            _ => "",
        };

        // spelled the way [ProtoMember] is, so `ProtoBuf.ProtoMember` gets `ProtoBuf.NullWrappedCollection`
        // and a bare one relies on the same using directive; lands in the same list, which keeps a
        // record parameter's `property:` target
        private static AttributeListSyntax AddNullWrappedCollection(AttributeSyntax protoMember)
        {
            var list = (AttributeListSyntax)protoMember.Parent!;
            var spelled = protoMember.Name.ToString();
            var prefix = spelled.Substring(0, spelled.Length - RightmostName(protoMember.Name).Length);
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName(prefix + "NullWrappedCollection"));
            return list.WithAttributes(list.Attributes.Insert(list.Attributes.IndexOf(protoMember) + 1, attribute));
        }
    }
}
