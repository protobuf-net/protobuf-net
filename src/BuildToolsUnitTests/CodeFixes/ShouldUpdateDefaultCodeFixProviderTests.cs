using BuildToolsUnitTests.CodeFixes.Abstractions;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.CodeFixes
{
    public class ShouldUpdateDefaultCodeFixProviderTests : CodeFixProviderTestsBase<ShouldUpdateDefaultValueCodeFixProvider>
    {
        private readonly DiagnosticResult[] _standardExpectedDiagnostics = new[] {
            new DiagnosticResult(DataContractAnalyzer.MissingCompatibilityLevel)
        };

        [Theory]
        [InlineData("int", "-2")]
        public async Task CodeFixValidate_ShouldUpdateDefault_NonProtoMemberAttributeExists(
            string propertyType, string propertyDefaultValue)
        {
            var sourceCode = $@"
using ProtoBuf;
using System;

[ProtoContract]
public class Foo
{{
    public {propertyType} Bar {{ get; set; }} = {propertyDefaultValue};
}}
";

            var expectedCode = $@"
using ProtoBuf;
using System;

[ProtoContract]
public class Foo
{{
    public {propertyType} Bar {{ get; set; }} = {propertyDefaultValue};
}}
";

            await RunCodeFixTestAsync<DataContractAnalyzer>(
                sourceCode,
                expectedCode,
                diagnosticResult: null, // no diagnostic expected!
                standardExpectedDiagnostics: _standardExpectedDiagnostics);
        }
        
        [Theory]
        [InlineData("decimal", "2.1", "2.2", "2.2m")]
        public async Task CodeFixValidate_ShouldUpdateDefault_LongSyntax(
            string propertyType, string attributeBeforeValue, string attributeAfterValue, string propertyValue)
        {
            var sourceCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1), DefaultValue(typeof({propertyValue}), ""{attributeBeforeValue}"")]
    public {propertyType} Bar {{ get; set; }} = {propertyValue};
}}";

            var expectedCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1), DefaultValue(typeof({propertyValue}), ""{attributeAfterValue}"")]
    public {propertyType} Bar {{ get; set; }} = {propertyValue};
}}";

            var diagnosticResult = PrepareDiagnosticResult(
                DataContractAnalyzer.ShouldUpdateDefault,
                9, 22, 9, $"DefaultValue(typeof({propertyValue}), {attributeBeforeValue})]".Length + 22 - 2,
                propertyValue);

            await RunCodeFixTestAsync<DataContractAnalyzer>(
                sourceCode,
                expectedCode,
                diagnosticResult,
                standardExpectedDiagnostics: _standardExpectedDiagnostics);
        }

        [Theory]
        [InlineData("bool", "true", "false")]
        [InlineData("DayOfWeek", "DayOfWeek.Monday", "DayOfWeek.Tuesday")]
        [InlineData("char", "'x'", "'y'")]
        [InlineData("sbyte", "1", "2")]
        [InlineData("byte", "0x2", "0b_0011")]
        [InlineData("short", "0b0000_0011", "0x5")]
        [InlineData("ushort", "4", "5")]
        [InlineData("int", "-2", "-1")]
        [InlineData("uint", "6u", "5u")]
        [InlineData("long", "1234567890123456789L", "123")]
        [InlineData("ulong", "6758493021UL", "124")]
        [InlineData("float", "2.71828f", "2.1f")]
        [InlineData("double", "3.14159265", "3.14")]
        [InlineData("nint", "1", "2")]
        [InlineData("nuint", "2", "1")]
        [InlineData("string", "\"hello\"", "\"hello world!\"")]
        public async Task CodeFixValidate_ShouldUpdateDefault_ShortSyntax(
            string propertyType, string attributeValue, string propertyValue)
        {
            var sourceCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1), DefaultValue({attributeValue})]
    public {propertyType} Bar {{ get; set; }} = {propertyValue};
}}";

            var expectedCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1), DefaultValue({propertyValue})]
    public {propertyType} Bar {{ get; set; }} = {propertyValue};
}}";

            var diagnosticResult = PrepareDiagnosticResult(
                DataContractAnalyzer.ShouldUpdateDefault,
                9, 22, 9, $"[DefaultValue({attributeValue})]".Length + 22 - 2,
                propertyValue);

            await RunCodeFixTestAsync<DataContractAnalyzer>(
                sourceCode,
                expectedCode,
                diagnosticResult,
                standardExpectedDiagnostics: _standardExpectedDiagnostics);
        }

        static DiagnosticResult PrepareDiagnosticResult(
            DiagnosticDescriptor diagnosticDescriptor,
            int startLine, int startColumn, int endLine, int endColumn,
            string propertyDefaultValue)
        {
            return new DiagnosticResult(diagnosticDescriptor)
                .WithSpan(startLine, startColumn, endLine, endColumn)
                .WithArguments("Bar", propertyDefaultValue);
        }
    }
}
