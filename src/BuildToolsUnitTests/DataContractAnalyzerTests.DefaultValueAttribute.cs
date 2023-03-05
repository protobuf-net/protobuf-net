using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests
{
    public partial class ProtobufFieldAnalyzerTests
    {
        [Fact]
        public async Task ReportsShouldDeclareIsRequired()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
using System;
[ProtoContract]
public class Foo {
    [ProtoMember(1)] public decimal TestDecimal {get;set;} = 1.618033m;
}
");
            var diags = diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldDeclareIsRequired).ToList();
            Assert.All(diags, diag => Assert.Equal(DiagnosticSeverity.Warning, diag.Severity));
            Assert.Collection(diags.Select(diag => diag.GetMessage(CultureInfo.InvariantCulture)),
                msg => Assert.Equal("Field 'TestDecimal' should use [ProtoMember(..., IsRequired=true)] to ensure its value is passed since it's initialized to a non-default value.", msg)
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
        [InlineData("nint", "1")]
        [InlineData("nuint", "2")]
        [InlineData("decimal", "1.618033m", false)]
        [InlineData("string", "string.Empty", false)]
        [InlineData("string", "\"\"", false)]
        public async Task ReportsShouldDeclareDefault(string type, string value, bool shouldReportDiagnostic = true)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;

                [ProtoContract]
                public class Foo {{ 
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
                msg => Assert.Equal($"Field 'FieldBar' should use [DefaultValue({value})] to ensure its value is sent since it's initialized to a non-default value.", msg),
                msg => Assert.Equal($"Field 'PropertyBar' should use [DefaultValue({value})] to ensure its value is sent since it's initialized to a non-default value.", msg)
            );
        }

        [Fact]
        public async Task ReportsShouldUpdateDefault()
        {
            var diagnostics = await AnalyzeAsync(@"
using ProtoBuf;
using System;
using System.ComponentModel;

[ProtoContract]
public class Foo {
    [ProtoMember(1), DefaultValue(false)] public bool FieldDefaultTrue = true;
    [ProtoMember(2), DefaultValue(false)] public bool PropertyDefaultTrue {get;set;} = true;
    [ProtoMember(3), DefaultValue(DayOfWeek.Tuesday)] public DayOfWeek FieldDefaultMonday = DayOfWeek.Monday;
    [ProtoMember(4), DefaultValue(DayOfWeek.Tuesday)] public DayOfWeek PropertyDefaultMonday {get;set;} = DayOfWeek.Monday;
    [ProtoMember(5), DefaultValue('Y')] public char TestChar {get;set;} = 'X';
    [ProtoMember(6), DefaultValue(2)] public sbyte TestSByte {get;set;} = 1;
    [ProtoMember(7), DefaultValue(0x1)] public byte TestByte {get;set;} = 0x2;
    [ProtoMember(8), DefaultValue(0b0000_0010)] public short TestInt16 {get;set;} = 0b0000_0011;
    [ProtoMember(9), DefaultValue(3)] public ushort TestUInt16 {get;set;} = 4;
    [ProtoMember(10), DefaultValue(1)] public int TestInt32 {get;set;} = -5;
    [ProtoMember(11), DefaultValue(5u)] public uint TestUInt32 {get;set;} = 6u;
    [ProtoMember(12), DefaultValue(123456789012345678L)] public long TestInt64 {get;set;} = 1234567890123456789L;
    [ProtoMember(13), DefaultValue(675849302UL)] public ulong TestUInt64 {get;set;} = 6758493021UL;
    // decimal's default is not a const expression     [ProtoMember(14), DefaultValue(!build time error!)] public decimal TestDecimal {get;set;} = 1.618033m;
    [ProtoMember(15), DefaultValue(2.6f)] public float TestSingle {get;set;} = 2.71828f;
    [ProtoMember(16), DefaultValue(3.1400)] public double TestDouble {get;set;} = 3.14159265;
    [ProtoMember(17), DefaultValue(2)] public nint TestIntPtr {get;set;} = 1;
    [ProtoMember(18), DefaultValue(1)] public nuint TestUIntPtr {get;set;} = 2;
}
");
            var diags = diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldUpdateDefault).ToList();
            Assert.All(diags, diag => Assert.Equal(DiagnosticSeverity.Warning, diag.Severity));
            Assert.Collection(diags.Select(diag => diag.GetMessage(CultureInfo.InvariantCulture)),
                msg => Assert.Equal("Field 'FieldDefaultTrue' should update [DefaultValue(true)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'PropertyDefaultTrue' should update [DefaultValue(true)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'FieldDefaultMonday' should update [DefaultValue(DayOfWeek.Monday)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'PropertyDefaultMonday' should update [DefaultValue(DayOfWeek.Monday)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestChar' should update [DefaultValue('X')] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestSByte' should update [DefaultValue(1)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestByte' should update [DefaultValue(0x2)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestInt16' should update [DefaultValue(0b0000_0011)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestUInt16' should update [DefaultValue(4)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestInt32' should update [DefaultValue(-5)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestUInt32' should update [DefaultValue(6u)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestInt64' should update [DefaultValue(1234567890123456789L)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestUInt64' should update [DefaultValue(6758493021UL)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestSingle' should update [DefaultValue(2.71828f)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestDouble' should update [DefaultValue(3.14159265)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestIntPtr' should update [DefaultValue(1)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg),
                msg => Assert.Equal("Field 'TestUIntPtr' should update [DefaultValue(2)] attribute usage to ensure the same value is being assigned to both 'type member' and '[DefaultValue]' attribute.", msg)
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
    [ProtoMember(18)] public decimal TestDecimal {get;set;} = 1.618033m; // is not a const expression, so no diagnostic
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

