#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.BuildTools.Internal;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoBuf.CodeFixes
{
    /// <summary>
    /// Fixes PBN0029, a nullable member whose initializer turns a null into a value on
    /// deserialize, by removing the initializer - and a <c>[DefaultValue]</c> with it, which would
    /// otherwise stop its own value being written, so that it came back null instead.
    /// </summary>
    /// <remarks>
    /// The other remedy - resetting the member to null in a <c>[ProtoBeforeDeserialization]</c>
    /// callback, which keeps the initializer for instances built in code - is left to the author:
    /// it means finding or naming a method, and in a hierarchy it only works on the root.
    /// </remarks>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullableMemberInitializedCodeFixProvider)), Shared]
    public class NullableMemberInitializedCodeFixProvider : CodeFixProvider
    {
        internal const string RemoveInitializerKey = "NullableMemberInitialized.RemoveInitializer";

        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DataContractAnalyzer.NullableMemberInitialized.Id);

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
                // the diagnostic sits on the member's name: a property, or a field's declarator
                var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
                var declaration = token.Parent;
                if (declaration is not (PropertyDeclarationSyntax { Initializer: not null } or VariableDeclaratorSyntax { Initializer: not null })) continue;

                var declaredDefault = diagnostic.Properties.TryGetValue(DataContractContext.NullableMemberDeclaredDefaultKey, out var flag) && flag == "true"
                    ? FindDefaultValue(declaration, model, context.CancellationToken)
                    : null;

                context.RegisterCodeFix(CodeAction.Create(
                    title: declaredDefault is null
                        ? $"Remove the initializer from '{token.ValueText}'"
                        : $"Remove the initializer and [DefaultValue] from '{token.ValueText}'",
                    createChangedDocument: cancellationToken => RemoveAsync(context.Document, declaration, declaredDefault, cancellationToken),
                    equivalenceKey: RemoveInitializerKey), diagnostic);
            }
        }

        // the attribute goes first, so that the declaration it sits in is replaced with it removed;
        // an attribute alone in its list takes the list with it
        private static async Task<Document> RemoveAsync(Document document, SyntaxNode declaration, AttributeSyntax? declaredDefault, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            if (declaredDefault is not null) editor.RemoveNode(declaredDefault);
            editor.ReplaceNode(declaration, (current, _) => NonNullableMemberCodeFixProvider.WithoutInitializer(current));
            return editor.GetChangedDocument();
        }

        // resolved through the semantic model, as the analyzer does, so an alias or a full name is
        // found as readily as `DefaultValue`
        private static AttributeSyntax? FindDefaultValue(SyntaxNode declaration, SemanticModel model, CancellationToken cancellationToken)
        {
            var lists = declaration switch
            {
                PropertyDeclarationSyntax property => property.AttributeLists,
                VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax field } => field.AttributeLists,
                _ => default,
            };
            return lists.SelectMany(list => list.Attributes).FirstOrDefault(attrib
                => model.GetSymbolInfo(attrib, cancellationToken).Symbol?.ContainingType is { Name: nameof(DefaultValueAttribute) } type
                    && type.InNamespace(nameof(System), nameof(System.ComponentModel)));
        }
    }
}
