using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis;
using System.Composition;
using System.Collections.Immutable;
using System.Threading.Tasks;
using ProtoBuf.BuildTools.Analyzers;

namespace ProtoBuf.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ShouldDeclareDefaultCodeFixProvider)), Shared]
    public class ShouldUpdateDefaultValueCodeFixProvider : CodeFixProvider
    {
        const string CodeFixTitle = "Add [DefaultValue] attribute";
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DataContractAnalyzer.ShouldUpdateDefault.Id);

        /// <summary>
        /// Key of for a <see cref="KeyValuePair{TKey, TValue}"/> of diagnostic properties,
        /// containing <see cref="DefaultValueAttribute"/> constructor value to be inserted into code
        /// </summary>
        public const string DefaultValueDiagnosticArgKey = "DefaultValueDiagnosticArg";

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            return Task.CompletedTask;
        }
    }
}
