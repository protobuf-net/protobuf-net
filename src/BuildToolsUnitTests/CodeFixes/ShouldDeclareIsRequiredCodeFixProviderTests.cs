using BuildToolsUnitTests.CodeFixes.Abstractions;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.CodeFixes
{
    public class ShouldDeclareIsRequiredCodeFixProviderTests : CodeFixProviderTestsBase<ShouldDeclareIsRequiredValueCodeFixProvider>
    {
        private readonly DiagnosticResult[] _standardExpectedDiagnostics = new[] {
            new DiagnosticResult(DataContractAnalyzer.MissingCompatibilityLevel)
        };

        [Theory]
        [InlineData("string", "GetString()", "public static string GetString() => \"my-const\";")]
        [InlineData("nint", "1")]
        [InlineData("nuint", "1")]
        public async Task CodeFixValidate_ShouldDeclareIsRequired_ClassicExample(
            string propertyType, string propertyValue, string? additionalClassCSharpCode = null)
        {
            var sourceCode = $@"
using ProtoBuf;
using System;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1)]
    public {propertyType} Bar {{ get; set; }} = {propertyValue};

    {(!string.IsNullOrEmpty(additionalClassCSharpCode) ? additionalClassCSharpCode : string.Empty)}
}}";

            var expectedCode = $@"
using ProtoBuf;
using System;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1, IsRequired = true)]
    public {propertyType} Bar {{ get; set; }} = {propertyValue};

    {(!string.IsNullOrEmpty(additionalClassCSharpCode) ? additionalClassCSharpCode : string.Empty)}
}}";

            var diagnosticResult = PrepareDiagnosticResult(
                DataContractAnalyzer.ShouldDeclareIsRequired,
                8, 6, 8, 20,
                propertyValue);

            await RunCodeFixTestAsync<DataContractAnalyzer>(
                sourceCode,
                expectedCode,
                diagnosticResult,
                standardExpectedDiagnostics: _standardExpectedDiagnostics);
        }
        
        [Theory]
        [InlineData("string", "GetString()", "public static string GetString() => \"my-const\";")]
        public async Task CodeFixValidate_ShouldDeclareIsRequired_WhenIsRequiredExistsButIsFalse(
            string propertyType, string propertyValue, string? additionalClassCSharpCode = null)
        {
            var sourceCode = $@"
using ProtoBuf;
using System;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1, IsRequired = false)]
    public {propertyType} Bar {{ get; set; }} = {propertyValue};

    {(!string.IsNullOrEmpty(additionalClassCSharpCode) ? additionalClassCSharpCode : string.Empty)}
}}";

            var expectedCode = $@"
using ProtoBuf;
using System;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1, IsRequired = true)]
    public {propertyType} Bar {{ get; set; }} = {propertyValue};

    {(!string.IsNullOrEmpty(additionalClassCSharpCode) ? additionalClassCSharpCode : string.Empty)}
}}";

            var diagnosticResult = PrepareDiagnosticResult(
                DataContractAnalyzer.ShouldDeclareIsRequired,
                8, 6, 8, 40,
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
