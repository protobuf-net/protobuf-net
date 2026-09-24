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

        // an initializer that only restates the type's own default - `= default`, `= null`, and the
        // `= default!` / `= null!` forms that nullable-reference-types makes routine - changes nothing
        // about what goes on the wire, so it must not trigger the IsRequired nag
        [Theory]
        [InlineData("string", "default")]
        [InlineData("string", "default!")]
        [InlineData("string", "null")]
        [InlineData("string", "null!")]
        [InlineData("string", "(string)null")]
        [InlineData("object", "default")]
        [InlineData("object", "null")]
        public async Task DoesNotReportShouldDeclareIsRequiredForImplicitDefaults(string type, string value)
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

            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldDeclareIsRequired));
        }

        // [DefaultValue("abc")] on a member left at null is a contract that loses data: protobuf-net
        // omits a member equal to its declared default, so a sender holding "abc" writes nothing and
        // the receiver - which has no initializer to fall back on - ends up with null. The two sides
        // disagree, which is exactly what PBN0021 is for; it must not be mistaken for the IsRequired nag
        [Theory]
        [InlineData("string", "\"abc\"", "null")]
        [InlineData("string", "\"abc\"", "null!")]
        [InlineData("string", "\"abc\"", "default")]
        [InlineData("string", "\"abc\"", "default!")]
        [InlineData("string", "\"abc\"", "(string)null")]
        public async Task ReportsShouldUpdateDefaultForNullInitializer(string type, string attributeValue, string propertyValue)
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

            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldDeclareIsRequired));

            var diags = diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldUpdateDefault).ToList();
            Assert.All(diags, diag => Assert.Equal(DiagnosticSeverity.Warning, diag.Severity));
            Assert.Equal(2, diags.Count);
        }

        // ...but when both sides say "default", they agree, and there is nothing to report
        [Theory]
        [InlineData("string", "null", "null")]
        [InlineData("string", "null", "default!")]
        [InlineData("string", "(string)null", "default")]
        [InlineData("object", "null", "null")]
        public async Task DoesNotReportWhenAttributeAndInitializerAreBothNull(string type, string attributeValue, string propertyValue)
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

            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldUpdateDefault
                || x.Descriptor == DataContractAnalyzer.ShouldDeclareDefault
                || x.Descriptor == DataContractAnalyzer.ShouldDeclareIsRequired));
        }

        // A member equal to its declared default is not written, and deserialization only assigns
        // fields that are present - so with nothing to restore the value, the two ends disagree
        // about what "absent" means and the sender's value is lost
        [Theory]
        [InlineData("string", "\"abc\"")]
        [InlineData("int", "5")]
        [InlineData("bool", "true")]
        [InlineData("double", "2.5")]
        [InlineData("DayOfWeek", "DayOfWeek.Monday")]
        public async Task ReportsDeclaredDefaultCannotRoundTrip(string type, string attributeValue)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract]
                public class Foo {{
                    [ProtoMember(1), DefaultValue({attributeValue})] public {type} FieldBar;
                    [ProtoMember(2), DefaultValue({attributeValue})] public {type} PropertyBar {{ get; set; }}
                }}
            ");

            var diags = diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultCannotRoundTrip).ToList();
            Assert.All(diags, diag => Assert.Equal(DiagnosticSeverity.Warning, diag.Severity));
            Assert.Equal(2, diags.Count);
        }

        // the shapes where something *does* restore the value, or where the declared default is not
        // load-bearing in the first place; every one of these is a pattern protogen emits or a
        // deliberate opt-out, and reporting on any of them would be noise
        [Theory]
        // the correct pairing: an initializer restores it
        [InlineData(@"[ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; } = ""abc"";")]
        // ...and protogen writes that assignment into a constructor instead
        [InlineData(@"[ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; }
                      public Foo() { Bar = ""abc""; }")]
        [InlineData(@"[ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; }
                      public Foo() { this.Bar = ""abc""; }")]
        // explicit presence: this is protogen's proto2-optional shape, [DefaultValue("")] and all
        [InlineData(@"[ProtoMember(1), DefaultValue("""")] public string Bar { get; set; }
                      public bool ShouldSerializeBar() => Bar != null;")]
        [InlineData(@"[ProtoMember(1), DefaultValue("""")] public string Bar { get; set; }
                      public bool BarSpecified { get; set; }")]
        // a null declared default means "no declared default" at all
        [InlineData(@"[ProtoMember(1), DefaultValue(null)] public string Bar { get; set; }")]
        // not a kind whose declared default we can reason about
        [InlineData(@"[ProtoMember(1), DefaultValue(5)] public int? Bar { get; set; }")]
        // a declared default equal to the type's own default needs nothing to restore it - the CLR
        // already hands it over. This is the shape protogen emits for a proto2 enum field whose
        // default is the zero member, and CustomOptions.cs in this repo is full of it
        [InlineData(@"public enum Kind { None = 0, Other = 1 }
                      [ProtoMember(1), DefaultValue(Kind.None)] public Kind Bar { get; set; }")]
        [InlineData(@"[ProtoMember(1), DefaultValue(0)] public int Bar { get; set; }")]
        [InlineData(@"[ProtoMember(1), DefaultValue(false)] public bool Bar { get; set; }")]
        [InlineData(@"[ProtoMember(1), DefaultValue("""")] public string Bar { get; set; }")]
        public async Task DoesNotReportDeclaredDefaultCannotRoundTrip(string body)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract]
                public class Foo {{
                    {body}
                }}
            ");

            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultCannotRoundTrip));
        }

        // under SkipConstructor neither a constructor nor a field initializer runs, so the fix this
        // diagnostic implies would not fix anything - it stays quiet rather than give bad advice
        [Fact]
        public async Task DoesNotReportDeclaredDefaultCannotRoundTripUnderSkipConstructor()
        {
            var diagnostics = await AnalyzeAsync(@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract(SkipConstructor = true)]
                public class Foo {
                    [ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; }
                }
            ");

            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultCannotRoundTrip));
        }

        // ValueMember.BuildSerializer only reaches the DefaultValueDecorator for a non-repeated,
        // non-required member, so in these two shapes the declared default is simply inert
        [Theory]
        [InlineData(@"[ProtoMember(1, IsRequired = true), DefaultValue(""abc"")] public string Bar { get; set; } = ""abc"";")]
        [InlineData(@"[ProtoMember(1), DefaultValue(5)] public System.Collections.Generic.List<int> Bar { get; set; } = new();")]
        [InlineData(@"[ProtoMember(1), DefaultValue(5)] public int[] Bar { get; set; }")]
        public async Task ReportsDeclaredDefaultIgnored(string body)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract]
                public class Foo {{
                    {body}
                }}
            ");

            var diag = Assert.Single(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultIgnored));
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
        }

        [Theory]
        // bytes are a scalar to protobuf-net, not a repeated member, so the default is applied
        [InlineData(@"[ProtoMember(1), DefaultValue(5)] public byte[] Bar { get; set; }")]
        [InlineData(@"[ProtoMember(1), DefaultValue(5)] public System.ReadOnlyMemory<byte> Bar { get; set; }")]
        // a string is IEnumerable but is a scalar
        [InlineData(@"[ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; } = ""abc"";")]
        // ShouldSerialize overriding the declared default is protogen's intended composition
        [InlineData(@"[ProtoMember(1), DefaultValue("""")] public string Bar { get; set; }
                      public bool ShouldSerializeBar() => Bar != null;")]
        // a null declared default is not a declared default
        [InlineData(@"[ProtoMember(1, IsRequired = true), DefaultValue(null)] public string Bar { get; set; }")]
        public async Task DoesNotReportDeclaredDefaultIgnored(string body)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract]
                public class Foo {{
                    {body}
                }}
            ");

            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultIgnored));
        }

        // the whole shape protogen emits for a proto2 optional field with a default, which must be
        // clean: it is the single largest body of code these two diagnostics could have shouted at
        [Fact]
        public async Task ProtogenPresenceTrackingShapeIsClean()
        {
            var diagnostics = await AnalyzeAsync(@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract]
                public class Foo {
                    [ProtoMember(1)]
                    [DefaultValue("""")]
                    public string Name
                    {
                        get => __pbn__Name ?? """";
                        set => __pbn__Name = value;
                    }
                    public bool ShouldSerializeName() => __pbn__Name != null;
                    public void ResetName() => __pbn__Name = null;
                    private string __pbn__Name;
                }
            ");

            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultCannotRoundTrip
                || x.Descriptor == DataContractAnalyzer.DeclaredDefaultIgnored));
        }

        // lifted verbatim from CustomOptions.cs, which protogen generated: a proto2 enum field whose
        // declared default is the zero member, carrying no initializer because it does not need one
        [Fact]
        public async Task ProtogenZeroEnumDefaultShapeIsClean()
        {
            var diagnostics = await AnalyzeAsync(@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                public enum MessageKind { None = 0, Service = 1 }

                [ProtoContract]
                public partial class ProtogenMessageOptions {
                    [ProtoMember(5, Name = @""messageKind"")]
                    [DefaultValue(MessageKind.None)]
                    public MessageKind MessageKind { get; set; }
                }
            ");

            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultCannotRoundTrip
                || x.Descriptor == DataContractAnalyzer.DeclaredDefaultIgnored));
        }

        // SkipConstructor deserializes via GetUninitializedObject, so neither the constructor nor a
        // field initializer runs - and a declared default is only restored by one of those. There
        // is no way to write the member that makes it round-trip, so the pairing itself is reported
        [Theory]
        [InlineData(@"[ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; } = ""abc"";")]
        [InlineData(@"[ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; }")]
        [InlineData(@"[ProtoMember(1), DefaultValue(5)] public int Bar { get; set; } = 5;")]
        [InlineData(@"[ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; }
                      public Foo() { Bar = ""abc""; }")]
        public async Task ReportsDeclaredDefaultUnderSkipConstructor(string body)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract(SkipConstructor = true)]
                public class Foo {{
                    {body}
                }}
            ");

            var diag = Assert.Single(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultUnderSkipConstructor));
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Contains("'Bar'", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        // one report per contract, not per member, and it names every member it covers
        [Fact]
        public async Task ReportsDeclaredDefaultUnderSkipConstructorOncePerType()
        {
            var diagnostics = await AnalyzeAsync(@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract(SkipConstructor = true)]
                public class Foo {
                    [ProtoMember(1), DefaultValue(""abc"")] public string First { get; set; } = ""abc"";
                    [ProtoMember(2), DefaultValue(5)] public int Second { get; set; } = 5;
                    [ProtoMember(3)] public string Untouched { get; set; }
                }
            ");

            var diag = Assert.Single(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultUnderSkipConstructor));
            var message = diag.GetMessage(CultureInfo.InvariantCulture);
            Assert.Contains("'First'", message);
            Assert.Contains("'Second'", message);
            Assert.DoesNotContain("Untouched", message);
        }

        // the report points at `SkipConstructor = true`, which is the thing to decide about
        [Fact]
        public async Task ReportsDeclaredDefaultUnderSkipConstructorAtTheOption()
        {
            var diagnostics = await AnalyzeAsync(@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract(SkipConstructor = true)]
                public class Foo {
                    [ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; } = ""abc"";
                }
            ");

            var diag = Assert.Single(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultUnderSkipConstructor));
            var span = diag.Location.SourceSpan;
            var text = (await diag.Location.SourceTree!.GetTextAsync()).ToString().Substring(span.Start, span.Length);
            Assert.Equal("SkipConstructor = true", text);
        }

        [Theory]
        // no SkipConstructor, no problem
        [InlineData(@"[ProtoContract]", @"[ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; } = ""abc"";")]
        [InlineData(@"[ProtoContract(SkipConstructor = false)]", @"[ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; } = ""abc"";")]
        // nothing declares a default
        [InlineData(@"[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1)] public string Bar { get; set; }")]
        // explicit presence replaces the declared-default guard, so the value still reaches the wire
        [InlineData(@"[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; }
                      public bool ShouldSerializeBar() => Bar != null;")]
        [InlineData(@"[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; }
                      public bool BarSpecified { get; set; }")]
        // a default equal to the type's own default is supplied by the CLR, constructor or not
        [InlineData(@"[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1), DefaultValue(0)] public int Bar { get; set; }")]
        [InlineData(@"[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1), DefaultValue(null)] public string Bar { get; set; }")]
        // a default that is never applied cannot be lost - that is PBN0025's business
        [InlineData(@"[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1, IsRequired = true), DefaultValue(""abc"")] public string Bar { get; set; }")]
        [InlineData(@"[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1), DefaultValue(5)] public System.Collections.Generic.List<int> Bar { get; set; } = new();")]
        public async Task DoesNotReportDeclaredDefaultUnderSkipConstructor(string contract, string body)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                {contract}
                public class Foo {{
                    {body}
                }}
            ");

            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultUnderSkipConstructor));
        }

        // PBN0024 stands down under SkipConstructor - its advice would not work there - so the two
        // never both fire on the same member
        [Fact]
        public async Task SkipConstructorReportSupersedesTheRoundTripNag()
        {
            var diagnostics = await AnalyzeAsync(@"
                using ProtoBuf;
                using System;
                using System.ComponentModel;

                [ProtoContract(SkipConstructor = true)]
                public class Foo {
                    [ProtoMember(1), DefaultValue(""abc"")] public string Bar { get; set; }
                }
            ");

            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultCannotRoundTrip));
            Assert.Single(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.DeclaredDefaultUnderSkipConstructor));
        }

        // a collection member has no wire presence to force - an empty collection writes nothing,
        // and IsRequired is only observable for value-type scalars - so initializing one (the
        // standard pattern, including getter-only) must not trigger the IsRequired nag
        [Theory]
        [InlineData("System.Collections.Generic.List<int>", "new()")]
        [InlineData("System.Collections.Generic.Dictionary<int, string>", "new()")]
        [InlineData("System.Collections.Generic.IList<int>", "new System.Collections.Generic.List<int>()")]
        [InlineData("int[]", "new int[0]")]
        public async Task DoesNotReportShouldDeclareIsRequiredForCollections(string type, string value)
        {
            var diagnostics = await AnalyzeAsync($@"
                using ProtoBuf;
                using System;

                [ProtoContract]
                public class Foo {{
                    [ProtoMember(1)] public {type} FieldBar = {value};
                    [ProtoMember(2)] public {type} PropertyBar {{ get; set; }} = {value};
                    [ProtoMember(3)] public {type} GetterOnlyBar {{ get; }} = {value};
                }}
            ");

            Assert.Empty(diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.ShouldDeclareIsRequired));
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
            Assert.DoesNotContain(diagnostics, x => x.Descriptor == DataContractAnalyzer.ShouldDeclareDefault);
        }
    }
}

