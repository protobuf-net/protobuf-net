using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests
{
    public partial class ProtobufFieldAnalyzerTests
    {
        [Theory]
        [InlineData("string", "GetString();", "public string GetString() => \"some-value\";")]
        [InlineData("nint", "1")]
        [InlineData("nuint", "1")]
        public async Task ReportsShouldDeclareIsRequired(string type, string value, string? additionalClassCSharpCode = null, bool shouldReportDiagnostic = true)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;

                [ProtoContract]
                public class Foo {{ 
                    [ProtoMember(1)] public {type} FieldBar = {value};
                    [ProtoMember(2)] public {type} PropertyBar {{ get; set; }} = {value};

                    {(!string.IsNullOrEmpty(additionalClassCSharpCode) ? additionalClassCSharpCode : string.Empty)}
                }}
            ");
            
            var diags = diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldDeclareIsRequired).ToList();
            if (!shouldReportDiagnostic)
            {
                Assert.Empty(diags);
                return;
            }

            Assert.All(diags, diag => Assert.Equal(DiagnosticSeverity.Warning, diag.Severity));
            Assert.Collection(diags.Select(diag => diag.GetMessage(CultureInfo.InvariantCulture)),
                msg => Assert.Equal(string.Format(DataContractAnalyzer.ShouldDeclareIsRequired.MessageFormat.ToString(), "FieldBar"), msg),
                msg => Assert.Equal(string.Format(DataContractAnalyzer.ShouldDeclareIsRequired.MessageFormat.ToString(), "PropertyBar"), msg)
            );
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
        [InlineData("decimal", "1.618033m")]
        [InlineData("string", "\"my-constant\"")]
        [InlineData("string", "string.Empty", false)]
        [InlineData("string", "\"\"", false)]
        [InlineData("string", "MyConst", false, "const string MyConst = \"hello\"")]
        public async Task ReportsShouldDeclareDefault_ShortSyntax(
            string type, 
            string value, 
            bool shouldReportDiagnostic = true,
            string? additionalClassCSharpCode = null)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;              

                [ProtoContract]
                public class Foo {{
                    {(!string.IsNullOrEmpty(additionalClassCSharpCode) ? additionalClassCSharpCode : string.Empty)}
 
                    [ProtoMember(1)] public {type} FieldBar = {value};
                    [ProtoMember(2)] public {type} PropertyBar {{ get; set; }} = {value};
                }}            
            ");
            
            var diags = diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldDeclareDefault).ToList();
            if (!shouldReportDiagnostic)
            {
                Assert.Empty(diags);
                return;
            }

            Assert.All(diags, diag => Assert.Equal(DiagnosticSeverity.Warning, diag.Severity));
            Assert.Collection(diags.Select(diag => diag.GetMessage(CultureInfo.InvariantCulture)),
                msg => Assert.Equal(string.Format(DataContractAnalyzer.ShouldDeclareDefault.MessageFormat.ToString(), "FieldBar", value), msg),
                msg => Assert.Equal(string.Format(DataContractAnalyzer.ShouldDeclareDefault.MessageFormat.ToString(), "PropertyBar", value), msg)
            );
        }

        [Theory]
        [InlineData("bool", "false", "true")]
        [InlineData("DayOfWeek", "DayOfWeek.Tuesday", "DayOfWeek.Monday")]
        [InlineData("char", "'Y'", "'X'")]
        [InlineData("sbyte", "2", "1")]
        [InlineData("byte", "0x1", "0x2")]
        [InlineData("short", "0b0000_0010", "0b0000_0011")]
        [InlineData("ushort", "3", "4")]
        [InlineData("int", "1", "-5")]
        [InlineData("uint", "5u", "6u")]
        [InlineData("long", "123456789012345678L", "1")]
        [InlineData("ulong", "675849302UL", "123")]
        [InlineData("float", "2.6f", "2.1")]
        [InlineData("double", "3.14", "3.14159265")]
        public async Task ReportsShouldUpdateDefault_ShortSyntax(string type, string attributeValue, string propertyValue, bool shouldReportDiagnostic = true)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract]
                public class Foo {{ 
                    [ProtoMember(1), DefaultValue({attributeValue})] public {type} FieldBar = {propertyValue};
                    [ProtoMember(2), DefaultValue({attributeValue})] public {type} PropertyBar {{ get; set; }} = {propertyValue};
                }}            
            ");
            
            var diags = diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldUpdateDefault).ToList();
            if (!shouldReportDiagnostic)
            {
                Assert.Empty(diags);
                return;
            }

            Assert.All(diags, diag => Assert.Equal(DiagnosticSeverity.Warning, diag.Severity));
            Assert.Collection(diags.Select(diag => diag.GetMessage(CultureInfo.InvariantCulture)),
                msg => Assert.Equal(string.Format(DataContractAnalyzer.ShouldUpdateDefault.MessageFormat.ToString(), "FieldBar", propertyValue), msg),
                msg => Assert.Equal(string.Format(DataContractAnalyzer.ShouldUpdateDefault.MessageFormat.ToString(), "PropertyBar", propertyValue), msg)
            );
        }
        
        [Theory]
        [InlineData("DayOfWeek", "DayOfWeek.Tuesday", "DayOfWeek.Monday")]
        [InlineData("char", "Y", "'X'")]
        [InlineData("byte", "0x1", "0x2")]
        [InlineData("short", "0b0000_0010", "0b0000_0011")]
        [InlineData("uint", "5u", "6u")]
        [InlineData("long", "123456789012345678L", "1")] // syntax is non valid for [DefaultValue(typeof(), "...")]
        [InlineData("ulong", "675849302UL", "123")]
        [InlineData("float", "2.6f", "2.1")] // syntax is non valid for [DefaultValue(typeof(), "...")]
        public async Task ReportsShouldUpdateDefault_LongSyntax_InvalidSyntax(string type, string attributeValue, string propertyValue, bool shouldReportDiagnostic = true)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract]
                public class Foo {{ 
                    [ProtoMember(1), DefaultValue(typeof({type}), ""{attributeValue}"")] public {type} FieldBar = {propertyValue};
                    [ProtoMember(2), DefaultValue(typeof({type}), ""{attributeValue}"")] public {type} PropertyBar {{ get; set; }} = {propertyValue};
                }}            
            ");
            
            var diags = diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldUpdateDefault).ToList();
            if (!shouldReportDiagnostic)
            {
                Assert.Empty(diags);
                return;
            }

            Assert.All(diags, diag => Assert.Equal(DiagnosticSeverity.Warning, diag.Severity));
            Assert.Collection(diags.Select(diag => diag.GetMessage(CultureInfo.InvariantCulture)),
                msg => Assert.Equal(string.Format(DataContractAnalyzer.ShouldUpdateDefault.MessageFormat.ToString(), "FieldBar", propertyValue), msg),
                msg => Assert.Equal(string.Format(DataContractAnalyzer.ShouldUpdateDefault.MessageFormat.ToString(), "PropertyBar", propertyValue), msg)
            );
        }
        
        [Theory]
        [InlineData("bool", "false", "true")]
        [InlineData("DayOfWeek", "Tuesday", "DayOfWeek.Monday")]
        [InlineData("char", "Y", "'X'")]
        [InlineData("sbyte", "2", "1")]
        [InlineData("byte", "0x1", "0x2")]
        [InlineData("ushort", "3", "4")]
        [InlineData("int", "1", "-5")]
        [InlineData("uint", "5", "6u")]
        [InlineData("long", "123456789012345678", "1")]
        [InlineData("ulong", "675849302U", "123")]
        [InlineData("float", "2.6", "2.1")]
        [InlineData("double", "3.14", "3.14159265")]
        public async Task ReportsShouldUpdateDefault_LongSyntax_ValidAttributeSyntax(string type, string attributeValue, string propertyValue, bool shouldReportDiagnostic = true)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract]
                public class Foo {{ 
                    [ProtoMember(1), DefaultValue(typeof({type}), ""{attributeValue}"")] public {type} FieldBar = {propertyValue};
                    [ProtoMember(2), DefaultValue(typeof({type}), ""{attributeValue}"")] public {type} PropertyBar {{ get; set; }} = {propertyValue};
                }}            
            ");
            
            var diags = diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldUpdateDefault).ToList();
            if (!shouldReportDiagnostic)
            {
                Assert.Empty(diags);
                return;
            }

            Assert.All(diags, diag => Assert.Equal(DiagnosticSeverity.Warning, diag.Severity));
            Assert.Collection(diags.Select(diag => diag.GetMessage(CultureInfo.InvariantCulture)),
                msg => Assert.Equal(string.Format(DataContractAnalyzer.ShouldUpdateDefault.MessageFormat.ToString(), "FieldBar", propertyValue), msg),
                msg => Assert.Equal(string.Format(DataContractAnalyzer.ShouldUpdateDefault.MessageFormat.ToString(), "PropertyBar", propertyValue), msg)
            );
        }

        [Fact]
        public async Task DoesNotReportShouldDeclareOrShouldUpdateDefault()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
using System;
using System.ComponentModel;
[ProtoContract]
public class Foo {
    [ProtoMember(1)] public bool FieldDefaultFalse;
    [ProtoMember(2), System.ComponentModel.DefaultValue(true)] public bool FieldDefaultTrue = true;
    [ProtoMember(3)] public bool PropertyDefaultFalse {get;set;}
    [ProtoMember(4, IsRequired = true)] public bool PropertyDefaultTrue {get;set;} = true;
    [ProtoMember(5)] public DayOfWeek FieldDefaultSunday;
    [ProtoMember(6), DefaultValue(DayOfWeek.Monday)] public DayOfWeek FieldDefaultMonday = DayOfWeek.Monday;
    [ProtoMember(7)] public DayOfWeek PropertyDefaultSunday {get;set;}
    [ProtoMember(8)] public DayOfWeek PropertyDefaultMonday {get;set;} = DayOfWeek.Monday;
    public bool ShouldSerializePropertyDefaultMonday() => true;
    [ProtoMember(9), DefaultValue('X')] public char TestChar {get;set;} = 'X';
    [ProtoMember(10), DefaultValue(1)] public sbyte TestSByte {get;set;} = 1;
    [ProtoMember(11), DefaultValue(0x2)] public byte TestByte {get;set;} = 0x2;
    [ProtoMember(12), DefaultValue(0b0000_0011)] public short TestInt16 {get;set;} = 0b0000_0011;
    [ProtoMember(13), DefaultValue(4)] public ushort TestUInt16 {get;set;} = 4;
    [ProtoMember(14), DefaultValue(-5)] public int TestInt32 {get;set;} = -5;
    [ProtoMember(15), DefaultValue(6u)] public uint TestUInt32 {get;set;} = 6u;
    [ProtoMember(16), DefaultValue(1234567890123456789L)] public long TestInt64 {get;set;} = 1234567890123456789L;
    [ProtoMember(17), DefaultValue(6758493021UL)] public ulong TestUInt64 {get;set;} = 6758493021UL;
    [ProtoMember(18), DefaultValue(typeof(decimal), ""1.618033m"")] public decimal TestDecimal {get;set;} = 1.618033m; // is not a const expression, so no diagnostic
    [ProtoMember(19), DefaultValue(2.71828f)] public float TestSingle {get;set;} = 2.71828f;
    [ProtoMember(20), DefaultValue(3.14159265)] public double TestDouble {get;set;} = 3.14159265;
    [ProtoMember(21), DefaultValue(1)] public nint TestIntPtr {get;set;} = 1;
    [ProtoMember(22), DefaultValue(2)] public nuint TestUIntPtr {get;set;} = 2;
    [ProtoMember(23)] public char Test0Char {get;set;}
    [ProtoMember(24)] public sbyte Test0SByte {get;set;}
    [ProtoMember(25)] public byte Test0Byte {get;set;}
    [ProtoMember(26)] public short Test0Int16 {get;set;}
    [ProtoMember(27)] public ushort Test0UInt16 {get;set;}
    [ProtoMember(28)] public int Test0Int32 {get;set;}
    [ProtoMember(29)] public uint Test0UInt32 {get;set;}
    [ProtoMember(30)] public long Test0Int64 {get;set;}
    [ProtoMember(31)] public ulong Test0UInt64 {get;set;}
    [ProtoMember(32)] public decimal Test0Decimal {get;set;}
    [ProtoMember(33)] public float Test0Single {get;set;}
    [ProtoMember(34)] public double Test0Double {get;set;}
    [ProtoMember(35)] public nint Test0IntPtr {get;set;}
    [ProtoMember(36)] public nuint Test0UIntPtr {get;set;}
}
");
            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldDeclareDefault));
        }
    }
}

