using BuildToolsUnitTests.CodeFixes.Abstractions;
using Microsoft.CodeAnalysis.Testing;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.CodeFixes
{
    public class NullableMemberInitializedCodeFixProviderTests : CodeFixProviderTestsBase<NullableMemberInitializedCodeFixProvider>
    {
        private readonly DiagnosticResult[] _standardExpectedDiagnostics = new[] {
            new DiagnosticResult(DataContractAnalyzer.MissingCompatibilityLevel)
        };

        private static string Wrap(string body) => @"#nullable enable
using ProtoBuf;
using System.ComponentModel;
" + body;

        [Theory]
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1)] public int? {|PBN0029:Timeout|} { get; set; } = 30; }",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public int? Timeout { get; set; } }")]
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1)] public int? {|PBN0029:Timeout|} = 30; }",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public int? Timeout; }")]
        // with [NullWrappedValue] the initializer costs a zero as well, and goes the same way
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1), NullWrappedValue] public bool? {|PBN0029:Enabled|} { get; set; } = true; }",
            "[ProtoContract] public class Foo { [ProtoMember(1), NullWrappedValue] public bool? Enabled { get; set; } }")]
        // what followed the `;` stays where it was
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1)] public int? {|PBN0029:Timeout|} { get; set; } = 30; // seconds\n}",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public int? Timeout { get; set; } // seconds\n}")]
        // ...and so does a comment before the `=`
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1)] public int? {|PBN0029:Timeout|} /* seconds */ = 30; }",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public int? Timeout /* seconds */; }")]
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1)] public int? {|PBN0029:Timeout|} { get; set; } /* seconds */ = 30; }",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public int? Timeout { get; set; } /* seconds */ }")]
        // a declared default stops its own value being written, so it goes too - or it would come back null
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1), DefaultValue(30)] public int? {|PBN0029:Timeout|} { get; set; } = 30; }",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public int? Timeout { get; set; } }")]
        [InlineData(
            "[ProtoContract] public class Foo { [ProtoMember(1)][DefaultValue(30)] public int? {|PBN0029:Timeout|} = 30; }",
            "[ProtoContract] public class Foo { [ProtoMember(1)] public int? Timeout; }")]
        public Task RemovesInitializer(string source, string expected)
            => RunCodeFixTestAsync<DataContractAnalyzer>(Wrap(source), Wrap(expected),
                codeActionEquivalenceKey: NullableMemberInitializedCodeFixProvider.RemoveInitializerKey,
                standardExpectedDiagnostics: _standardExpectedDiagnostics);
    }
}
