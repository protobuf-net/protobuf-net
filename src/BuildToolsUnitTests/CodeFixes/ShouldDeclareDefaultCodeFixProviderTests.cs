using BuildToolsUnitTests.CodeFixes.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.CodeFixes;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.CodeFixes
{
    public class ShouldDeclareDefaultCodeFixProviderTests : CodeFixProviderTestsBase
    {
        [Theory]
        [InlineData("int", "-2")]
        public async Task CodeFixValidate_ShouldDeclareDefault_NonProtoMemberAttributeExists(string propertyType, string propertyDefaultValue)
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

            // System.ComponentModel is added as part of code-fix
            var expectedCode = $@"
using ProtoBuf;
using System;

[ProtoContract]
public class Foo
{{
    public {propertyType} Bar {{ get; set; }} = {propertyDefaultValue};
}}
";

            await RunCodeFixTestAsync<DataContractAnalyzer, ShouldDeclareDefaultCodeFixProvider>(
                sourceCode,
                expectedCode,
                diagnosticResult: null,
                standardExpectedDiagnostics: new[] { new DiagnosticResult(DataContractAnalyzer.MissingCompatibilityLevel) });
        }

        [Theory]
        [InlineData("int", "-2")]
        public async Task CodeFixValidate_ShouldDeclareDefault_AnotherCustomAttributeExists(string propertyType, string propertyDefaultValue)
        {
            var sourceCode = $@"
using ProtoBuf; 
using System;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1)]
    [Custom]
    public {propertyType} Bar {{ get; set; }} = {propertyDefaultValue};
}}

public class CustomAttribute : Attribute {{ }}";

            // System.ComponentModel is added as part of code-fix
            var expectedCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1), DefaultValue({propertyDefaultValue})]
    [Custom]
    public {propertyType} Bar {{ get; set; }} = {propertyDefaultValue};
}}

public class CustomAttribute : Attribute {{ }}";

            var diagnosticResult = PrepareDiagnosticResult(
                DataContractAnalyzer.ShouldDeclareDefault,
                8, 6, 8, 20,
                propertyDefaultValue);

            await RunCodeFixTestAsync<DataContractAnalyzer, ShouldDeclareDefaultCodeFixProvider>(
                sourceCode,
                expectedCode,
                diagnosticResult,
                standardExpectedDiagnostics: new[] { new DiagnosticResult(DataContractAnalyzer.MissingCompatibilityLevel) });
        }

        [Theory]
        [InlineData("int", "-2")]
        public async Task CodeFixValidate_ShouldDeclareDefault_ClassicExample(string propertyType, string propertyDefaultValue)
        {
            var sourceCode = $@"
using ProtoBuf; 
using System;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1)]
    public {propertyType} Bar {{ get; set; }} = {propertyDefaultValue};
}}";

            // System.ComponentModel is added as part of code-fix
            var expectedCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1), DefaultValue({propertyDefaultValue})]
    public {propertyType} Bar {{ get; set; }} = {propertyDefaultValue};
}}
";

            var diagnosticResult = PrepareDiagnosticResult(
                DataContractAnalyzer.ShouldDeclareDefault,
                8, 6, 8, 20,
                propertyDefaultValue);

            await RunCodeFixTestAsync<DataContractAnalyzer, ShouldDeclareDefaultCodeFixProvider>(
                sourceCode, 
                expectedCode, 
                diagnosticResult,
                standardExpectedDiagnostics: new[] { new DiagnosticResult(DataContractAnalyzer.MissingCompatibilityLevel) });
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
