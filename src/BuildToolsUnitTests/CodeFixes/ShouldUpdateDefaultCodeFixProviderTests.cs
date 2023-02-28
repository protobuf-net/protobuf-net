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
        [InlineData("int", "-2", "-5")]
        public async Task CodeFixValidate_ShouldUpdateDefault_ClassicExample(
            string propertyType, string attributeDefaultValue, string propertyDefaultValue)
        {
            var sourceCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1), DefaultValue({attributeDefaultValue})]
    public {propertyType} Bar {{ get; set; }} = {propertyDefaultValue};
}}";

            var expectedCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1), DefaultValue({propertyDefaultValue})]
    public {propertyType} Bar {{ get; set; }} = {propertyDefaultValue};
}}";

            var diagnosticResult = PrepareDiagnosticResult(
                DataContractAnalyzer.ShouldUpdateDefault,
                9, 6, 9, 20,
                propertyDefaultValue);

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
