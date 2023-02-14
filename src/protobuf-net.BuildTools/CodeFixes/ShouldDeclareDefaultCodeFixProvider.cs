using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using ProtoBuf.BuildTools.Analyzers;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

namespace ProtoBuf.CodeFixes
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ShouldDeclareDefaultCodeFixProvider)), Shared]
    public class ShouldDeclareDefaultCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DataContractAnalyzer.ShouldDeclareDefault.Id);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var a = context;

            throw new NotImplementedException();
        }
    }
}
