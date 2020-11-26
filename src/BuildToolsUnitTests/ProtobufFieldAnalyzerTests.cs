using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests
{
    public class ProtobufFieldAnalyzerTests : AnalyzerTestBase<ProtobufFieldAnalyzer>
    {
        public ProtobufFieldAnalyzerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [Fact]
        public async Task DoesntReportOnUnrelatedCode()
        {
            var diagnostics = await AnalyzeAsync(@"
public class Foo
{
    public void Bar() {}
}");
            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(18999)]
        [InlineData(20000)]
        [InlineData(536870911)]
        // The smallest field number you can specify is 1, and the largest is 229 - 1, or 536,870,911. You also cannot use the numbers 19000 through 19999
        // from https://developers.google.com/protocol-buffers/docs/proto3#assigning_field_numbers
        public async Task DoesntReportOnLegalDto(int fieldNumber)
        {
            var diagnostics = await AnalyzeAsync($@"
using ProtoBuf;
[ProtoContract]
public class Foo
{{
    [ProtoMember({fieldNumber})]
    public int Bar {{get;set;}}
}}");
            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(42)]
        [InlineData(18999)]
        [InlineData(20000)]
        [InlineData(536870911)]
        // The smallest field number you can specify is 1, and the largest is 229 - 1, or 536,870,911. You also cannot use the numbers 19000 through 19999
        // from https://developers.google.com/protocol-buffers/docs/proto3#assigning_field_numbers
        public async Task DoesntReportOnLegalDto_Partial(int fieldNumber)
        {
            var diagnostics = await AnalyzeAsync($@"
using ProtoBuf;
[ProtoContract]
[ProtoPartialMember({fieldNumber}, nameof(Bar))]
public class Foo
{{
    public int Bar {{get;set;}}
}}");
            Assert.Empty(diagnostics);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-42)]
        [InlineData(19000, true)]
        [InlineData(19500, true)]
        [InlineData(19999, true)]
        [InlineData(536870912)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        // The smallest field number you can specify is 1, and the largest is 229 - 1, or 536,870,911. You also cannot use the numbers 19000 through 19999
        // from https://developers.google.com/protocol-buffers/docs/proto3#assigning_field_numbers
        public async Task ReportsOnIllegalDto(int fieldNumber, bool warningOnly = false)
        {
            var diagnostics = await AnalyzeAsync($@"
using ProtoBuf;
[ProtoContract]
public class Foo
{{
    [ProtoMember({fieldNumber})]
    public int Bar {{get;set;}}
}}");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == ProtobufFieldAnalyzer.InvalidFieldNumber);
            Assert.Equal(warningOnly ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The specified field number {fieldNumber} is invalid; the valid range is 1-536870911, omitting 19000-19999.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsOnIllegalPartialName()
        {
            var diagnostics = await AnalyzeAsync($@"
using ProtoBuf;
[ProtoContract]
[ProtoPartialMember(42, ""Bar"")]
public class Foo
{{
    public int Blap {{get;set;}}
}}");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == ProtobufFieldAnalyzer.MemberNotFound);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal("The specified type member 'Bar' could not be resolved.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-42)]
        [InlineData(19000, true)]
        [InlineData(19500, true)]
        [InlineData(19999, true)]
        [InlineData(536870912)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        // The smallest field number you can specify is 1, and the largest is 229 - 1, or 536,870,911. You also cannot use the numbers 19000 through 19999
        // from https://developers.google.com/protocol-buffers/docs/proto3#assigning_field_numbers
        public async Task ReportsOnIllegalDto_Partial(int fieldNumber, bool warningOnly = false)
        {
            var diagnostics = await AnalyzeAsync($@"
using ProtoBuf;
[ProtoContract]
[ProtoPartialMember({fieldNumber}, nameof(Bar))]
public class Foo
{{
    public int Bar {{get;set;}}
}}");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == ProtobufFieldAnalyzer.InvalidFieldNumber);
            Assert.Equal(warningOnly ? DiagnosticSeverity.Warning : DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The specified field number {fieldNumber} is invalid; the valid range is 1-536870911, omitting 19000-19999.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsOnIllegalConst()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
[ProtoContract]
public class Foo
{
    private const int FieldNumber = -42;
    [ProtoMember(FieldNumber)]
    public int Bar {get;set;}
}");
            var diag = Assert.Single(diagnostics, x => x.Descriptor == ProtobufFieldAnalyzer.InvalidFieldNumber);
            Assert.Equal(DiagnosticSeverity.Error, diag.Severity);
            Assert.Equal($"The specified field number -42 is invalid; the valid range is 1-536870911, omitting 19000-19999.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

    }
}
