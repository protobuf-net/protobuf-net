using BuildToolsUnitTests.CodeFixes.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests.CodeFixes
{
    public class ShouldDeclareDefaultCodeFixProviderTests : CodeFixProviderTestsBase<ShouldDeclareDefaultCodeFixProvider>
    {
        private readonly DiagnosticResult[] _standardExpectedDiagnostics = new[] {
            new DiagnosticResult(DataContractAnalyzer.MissingCompatibilityLevel)
        };

        [Theory]
        [InlineData("int", "-2")]
        public async Task CodeFixValidate_ShouldDeclareDefault_NonProtoMemberAttributeExists(
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
        [InlineData("int", "-2")]
        public async Task CodeFixValidate_ShouldDeclareDefault_AnotherCustomAttributeExists(
            string propertyType, string propertyDefaultValue)
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

            await RunCodeFixTestAsync<DataContractAnalyzer>(
                sourceCode,
                expectedCode,
                diagnosticResult,
                standardExpectedDiagnostics: _standardExpectedDiagnostics);
        }

        [Theory]
        [InlineData("int", "-2")]
        public async Task CodeFixValidate_ShouldDeclareDefault_UsingDirectiveAlreadyExists(
            string propertyType, string propertyDefaultValue)
        {
            var sourceCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

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
}}";

            var diagnosticResult = PrepareDiagnosticResult(
                DataContractAnalyzer.ShouldDeclareDefault,
                9, 6, 9, 20,
                propertyDefaultValue);

            await RunCodeFixTestAsync<DataContractAnalyzer>(
                sourceCode,
                expectedCode,
                diagnosticResult,
                standardExpectedDiagnostics: _standardExpectedDiagnostics);
        }
        
        [Theory]
        [InlineData("bool", "true")]
        [InlineData("DayOfWeek", "DayOfWeek.Monday")]
        [InlineData("char", "'x'")]
        [InlineData("sbyte", "1")]
        [InlineData("byte", "0x2")]
        [InlineData("short", "0b0000_0011")]
        [InlineData("ushort", "4")]
        [InlineData("int", "-2")]
        [InlineData("uint", "6u")]
        [InlineData("long", "1234567890123456789L")]
        [InlineData("ulong", "6758493021UL")]
        [InlineData("float", "2.71828f")]
        [InlineData("double", "3.14159265")]
        [InlineData("nint", "1")]
        [InlineData("nuint", "2")]
        public async Task CodeFixValidate_ShouldDeclareDefault_ReportsDiagnosticClassicExample(
            string propertyType, string propertyDefaultValue)
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
}}";
            
            var diagnosticResult = PrepareDiagnosticResult(
                DataContractAnalyzer.ShouldDeclareDefault,
                8, 6, 8, 20,
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
