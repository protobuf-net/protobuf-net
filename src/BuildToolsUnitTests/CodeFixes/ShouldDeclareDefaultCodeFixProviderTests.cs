using BuildToolsUnitTests.CodeFixes.Abstractions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using ProtoBuf.BuildTools.Analyzers;
using System.Threading.Tasks;
using ProtoBuf.CodeFixes.DefaultValue;
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
using System.ComponentModel;

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
    [ProtoMember(1), DefaultValue(({propertyType}){propertyDefaultValue})]
    [Custom]
    public {propertyType} Bar {{ get; set; }} = {propertyDefaultValue};
}}

public class CustomAttribute : Attribute {{ }}";

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
    [ProtoMember(1), DefaultValue(({propertyType}){propertyDefaultValue})]
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
        [InlineData("decimal", "2.1", "2.1m")]
        [InlineData("sbyte", "1", "1")]
        [InlineData("uint", "6", "6u")]
        [InlineData("ulong", "6758493021", "6758493021UL")]
        [InlineData("ushort", "4", "4")]
        public async Task CodeFixValidate_ShouldDeclareDefault_ReportsDiagnostic_LongSyntax(string propertyType, string attributeValue, string propertyDefaultValue)
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

            // note: System.ComponentModel is added as part of code-fix
            // and this behavior is tested in separate class, since "usingDirective" addition produces
            // wrong line endings, which fail in roslyn codeFix test
            // https://github.com/dotnet/roslyn/issues/62976
            var expectedCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1), DefaultValue(typeof({propertyType}), ""{attributeValue}"")]
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
        [InlineData("DayOfWeek", "DayOfWeek.Monday", false)]
        [InlineData("char", "'x'")]
        [InlineData("byte", "0x2")]
        [InlineData("short", "0b0000_0011")]
        [InlineData("int", "-2")]
        [InlineData("long", "1234567890123456789L")]
        [InlineData("float", "2.71828f")]
        [InlineData("double", "3.14159265")]
        public async Task CodeFixValidate_ShouldDeclareDefault_ReportsDiagnostic_ShortSyntax(
            string propertyType, string propertyDefaultValue, bool isCasted = true)
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

            // note: System.ComponentModel is added as part of code-fix
            // and this behavior is tested in separate class, since "usingDirective" addition produces
            // wrong line endings, which fail in roslyn codeFix test
            // https://github.com/dotnet/roslyn/issues/62976

            var castExpression = isCasted ? $"({propertyType})" : string.Empty;
            
            var expectedCode = $@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo
{{
    [ProtoMember(1), DefaultValue({castExpression}{propertyDefaultValue})]
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
