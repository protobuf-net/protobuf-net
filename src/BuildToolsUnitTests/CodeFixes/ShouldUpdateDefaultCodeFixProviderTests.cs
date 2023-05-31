using BuildToolsUnitTests.CodeFixes.Abstractions;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using System.Threading.Tasks;
using ProtoBuf.CodeFixes.DefaultValue;
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
        [InlineData("decimal", "2", "2.2", "2.2m")]
        [InlineData("sbyte", "1", "2", "2")]
        [InlineData("uint", "6", "5", "5u")]
        [InlineData("ulong", "6758493021U", "124", "124")]
        [InlineData("ushort", "4", "5", "5")]
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
    [ProtoMember(1), DefaultValue(typeof({propertyType}), ""{attributeBeforeValue}"")]
    public {propertyType} Bar {{ get; set; }} = {propertyValue};
}}";

            var expectedCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1), DefaultValue(typeof({propertyType}), ""{attributeAfterValue}"")]
    public {propertyType} Bar {{ get; set; }} = {propertyValue};
}}";

            var diagnosticResult = PrepareDiagnosticResult(
                DataContractAnalyzer.ShouldUpdateDefault,
                9, 22, 9, @$"DefaultValue(typeof({propertyType}), ""{attributeBeforeValue}"")]".Length + 22 - 1,
                propertyValue);

            await RunCodeFixTestAsync<DataContractAnalyzer>(
                sourceCode,
                expectedCode,
                diagnosticResult,
                standardExpectedDiagnostics: _standardExpectedDiagnostics);
        }

        [Theory]
        [InlineData("bool", "true", "false")]
        [InlineData("DayOfWeek", "DayOfWeek.Monday", "DayOfWeek.Tuesday", false)]
        [InlineData("char", "'x'", "'y'")]
        [InlineData("byte", "0x2", "0b_0011")]
        [InlineData("short", "0b0000_0011", "0x5")]
        [InlineData("int", "-2", "-1")]
        [InlineData("long", "1234567890123456789L", "123")]
        [InlineData("float", "2.71828f", "2.1f")]
        [InlineData("double", "3.14159265", "3.14")]
        [InlineData("string", "\"hello\"", "\"hello world!\"")]
        public async Task CodeFixValidate_ShouldUpdateDefault_ShortSyntax(
            string propertyType, string attributeValue, string propertyValue, bool isCasted = true)
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

            var castExpression = isCasted ? $"({propertyType})" : string.Empty;

            var expectedCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1), DefaultValue({castExpression}{propertyValue})]
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
