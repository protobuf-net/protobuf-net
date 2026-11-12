using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests
{
    // PBN0028's neighbours: the same question - what does construction leave in a member that the
    // payload does not carry - asked of a nullable member (PBN0029) and of a non-nullable one that
    // is not a collection (PBN0030). Every "reported" shape was probed against RuntimeTypeModel.
    public partial class ProtobufFieldAnalyzerTests
    {
        private const string NullMemberTypes = @"
public enum Color { Red, Green, Blue }
[ProtoContract] public class Msg { [ProtoMember(1)] public int X { get; set; } }
public class FooSerializer { }
public struct Money { public static implicit operator Money(string? text) => default; }
[ProtoContract] public class Bag : System.Collections.IEnumerable { public System.Collections.IEnumerator GetEnumerator() => null!; }";

        private async Task<List<Diagnostic>> DiagnosticsAsync(DiagnosticDescriptor descriptor, string source)
            => (await AnalyzeAsync(source)).Where(x => x.Descriptor == descriptor).ToList();

        private static string NullMemberSource(string contract, string body, string nullable = "#nullable enable", string kind = "class", string bases = "") => $@"
{nullable}
using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;
{contract}
public {kind} Foo{bases} {{
    {body}
}}
" + NullMemberTypes;

        // PBN0029: null is not written, so the receiver keeps whatever the initializer gave it
        [Theory]
        [InlineData("[ProtoMember(1)] public int? V { get; set; } = 5;")]
        [InlineData("[ProtoMember(1)] public int? V = 5;")]
        [InlineData("[ProtoMember(1)] public bool? V { get; set; } = true;")]
        [InlineData("[ProtoMember(1)] public Color? V { get; set; } = Color.Blue;")]
        [InlineData("[ProtoMember(1)] public string? V { get; set; } = \"x\";")]
        [InlineData("[ProtoMember(1)] public Msg? V { get; set; } = new();")]
        // null-wrapped, a collection tells null from empty - and then the initializer loses the null
        [InlineData("[ProtoMember(1), NullWrappedCollection] public List<int>? V { get; set; } = new();")]
        // neither changes what an absent field reads as
        [InlineData("[ProtoMember(1), DefaultValue(5)] public int? V { get; set; } = 5;")]
        [InlineData("[ProtoMember(1, IsRequired = true)] public int? V { get; set; } = 5;")]
        // ShouldSerialize only decides the write: a null is still not written (probed)
        [InlineData("[ProtoMember(1)] public int? V { get; set; } = 5; public bool ShouldSerializeV() => V != 5;")]
        // a zero, not a null, whatever it looks like (probed)
        [InlineData("[ProtoMember(1)] public int? V { get; set; } = default(int);")]
        [InlineData("[ProtoMember(1)] public int? V { get; set; } = (int)default;")]
        [InlineData("[ProtoMember(1)] public Money? V { get; set; } = (Money)null;")]
        // an after-callback runs once the fields are read, too late to tell a null that was sent
        [InlineData(@"[ProtoMember(1)] public int? V { get; set; } = 5;
                      [ProtoAfterDeserialization] public void After() { if (V > 10) V = 10; }")]
        // a callback that resets something else does not count
        [InlineData(@"[ProtoMember(1)] public int? V { get; set; } = 5;
                      public int Other { get; set; }
                      [ProtoBeforeDeserialization] public void Reset() => Other = 0;")]
        public async Task ReportsNullableMemberWithInitializer(string body)
        {
            var diag = Assert.Single(await DiagnosticsAsync(DataContractAnalyzer.NullableMemberInitialized,
                NullMemberSource("[ProtoContract]", body)));
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.StartsWith("'V' is nullable, but its initializer gives it a value; null is not written, so a null comes back as that value",
                diag.GetMessage(CultureInfo.InvariantCulture));

            Assert.Equal("V", diag.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(diag.Location.SourceSpan));
        }

        [Fact]
        public async Task ReportsNullableMemberWithInitializerInFull()
        {
            var diag = Assert.Single(await DiagnosticsAsync(DataContractAnalyzer.NullableMemberInitialized,
                NullMemberSource("[ProtoContract]", "[ProtoMember(1)] public int? Timeout { get; set; } = 30;")));
            Assert.Equal("'Timeout' is nullable, but its initializer gives it a value; null is not written, so a null comes back as that value. "
                + "To fix: remove the initializer, or reset it to null in a [ProtoBeforeDeserialization] callback.",
                diag.GetMessage(CultureInfo.InvariantCulture));
        }

        // Nullable<T> is nullable whatever the context: it needs no annotation to promise null
        [Fact]
        public async Task ReportsNullableValueTypeWithoutNullableContext()
        {
            Assert.Single(await DiagnosticsAsync(DataContractAnalyzer.NullableMemberInitialized,
                NullMemberSource("[ProtoContract]", "[ProtoMember(1)] public int? V { get; set; } = 5;", nullable: "#nullable disable")));
        }

        // the message offers only what works: a declared default stops its own value being written,
        // so it has to go as well, and a callback cannot assign a getter-only or init-only member
        [Theory]
        [InlineData("[ProtoMember(1), DefaultValue(5)] public int? V { get; set; } = 5;",
            "remove the initializer and the [DefaultValue], or remove the [DefaultValue] and reset it to null in a [ProtoBeforeDeserialization] callback")]
        [InlineData("[ProtoMember(1)] public int? V { get; } = 5;", "remove the initializer")]
        [InlineData("[ProtoMember(1)] public int? V { get; init; } = 5;", "remove the initializer")]
        [InlineData("[ProtoMember(1), DefaultValue(\"x\")] public string? V { get; } = \"x\";", "remove the initializer and the [DefaultValue]")]
        public async Task SuggestsOnlyNullableFixesThatWork(string body, string fixes)
        {
            var diag = Assert.Single(await DiagnosticsAsync(DataContractAnalyzer.NullableMemberInitialized,
                NullMemberSource("[ProtoContract]", body) + IsExternalInit));
            Assert.EndsWith(" To fix: " + fixes + ".", diag.GetMessage(CultureInfo.InvariantCulture));
            Assert.Equal(body.Contains("DefaultValue"), diag.Properties.ContainsKey("DeclaredDefault"));
        }

        // SkipConstructor belongs to the concrete type being read (probed): inert on an abstract base,
        // and on a sub-type it strips the base as well. So the initializer runs - and turns a null into
        // its value - wherever some concrete type is constructed as usual
        [Theory]
        // an abstract base on its own: whatever derives from it is out of sight, and assumed ordinary
        [InlineData("[ProtoContract(SkipConstructor = true)]", "abstract class", "", true)]
        [InlineData("[ProtoContract(SkipConstructor = true)]", "abstract class", "[ProtoContract]", true)]
        [InlineData("[ProtoContract(SkipConstructor = true)]", "abstract class", "[ProtoContract(SkipConstructor = true)]", false)]
        // the base itself is constructed, even if the sub-type is not
        [InlineData("[ProtoContract]", "class", "[ProtoContract(SkipConstructor = true)]", true)]
        // ...and the other way round
        [InlineData("[ProtoContract(SkipConstructor = true)]", "class", "[ProtoContract]", true)]
        [InlineData("[ProtoContract(SkipConstructor = true)]", "class", "[ProtoContract(SkipConstructor = true)]", false)]
        public async Task ReportsNullableMemberWhereSomeTypeIsConstructed(string contract, string kind, string subContract, bool reported)
        {
            var include = subContract.Length == 0 ? "" : "\n[ProtoInclude(10, typeof(Derived))]";
            var derived = subContract.Length == 0 ? "" : subContract + "\npublic class Derived : Foo { }";
            var diags = await DiagnosticsAsync(DataContractAnalyzer.NullableMemberInitialized,
                NullMemberSource(contract + include, "[ProtoMember(1)] public int? V { get; set; } = 5;", kind: kind) + derived);
            Assert.Equal(reported ? 1 : 0, diags.Count);
        }

        // a type whose constructors cannot be answered for - here a `: this(...)` chain - stands the
        // collection check down, but must not stand down the members after it
        [Fact]
        public async Task ReportsNullableMemberAfterAnUnanswerableCollection()
        {
            var diagnostics = await AnalyzeAsync(NullMemberSource("[ProtoContract]", @"[ProtoMember(1)] public List<int> Items { get; set; }
                      [ProtoMember(2)] public int? V { get; set; } = 5;
                      public Foo() : this(0) { }
                      public Foo(int x) { Items = new(); }"));
            Assert.DoesNotContain(diagnostics, x => x.Descriptor == DataContractAnalyzer.NonNullableCollectionLeftNull);
            Assert.Contains("'V'", Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.NullableMemberInitialized)
                .GetMessage(CultureInfo.InvariantCulture));
        }

        // a partial type is visited once per declaration, and each member must be reported once, by
        // the part that declares it
        [Fact]
        public async Task ReportsNullMembersOnceAcrossPartialDeclarations()
        {
            var diagnostics = await AnalyzeMultiFileAsync(new List<string>
            {
@"
#nullable enable
using ProtoBuf;
[ProtoContract(SkipConstructor = true), ProtoInclude(10, typeof(Derived))]
public partial class Foo
{
    [ProtoMember(1)] public string Name { get; set; } = """";
}
[ProtoContract]
public class Derived : Foo { }",
@"
#nullable enable
using ProtoBuf;
public partial class Foo
{
    [ProtoMember(2)] public int? V { get; set; } = 5;
}",
            });

            Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.NonNullableMemberLeftNull);
            Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.NullableMemberInitialized);
        }

        // inside a [NullWrappedValue] wrapper the value is an ordinary field, so a zero or false is
        // left out and the wrapper goes out empty - which leaves the initializer's value in place.
        // An enum's zero is written explicitly, and a string is written even empty (both probed)
        [Theory]
        [InlineData("int? V { get; set; } = 5;", true)]
        [InlineData("bool? V { get; set; } = true;", true)]
        [InlineData("double? V { get; set; } = 1.5;", true)]
        [InlineData("nint? V { get; set; } = 5;", true)]
        [InlineData("Color? V { get; set; } = Color.Blue;", false)]
        [InlineData("string? V { get; set; } = \"x\";", false)]
        public async Task SaysWhenNullWrappingLosesZeroToo(string member, bool zeroLost)
        {
            var diag = Assert.Single(await DiagnosticsAsync(DataContractAnalyzer.NullableMemberInitialized,
                NullMemberSource("[ProtoContract]", "[ProtoMember(1), NullWrappedValue] public " + member)));
            var clause = ", as does a zero or false, which [NullWrappedValue] writes as an empty wrapper.";
            Assert.Equal(zeroLost, diag.GetMessage(CultureInfo.InvariantCulture).Contains(clause));
        }

        [Theory]
        // no initializer, or one that restates null
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public int? V { get; set; }")]
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public int? V { get; set; } = null;")]
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public int? V { get; set; } = default;")]
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public string? V { get; set; } = null!;")]
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public int? V { get; set; } = (int?)null;")]
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public int? V { get; set; } = default(int?);")]
        // not nullable: losing a value there is PBN0020/PBN0022's business
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public int V { get; set; } = 5;")]
        // not written at all, so nothing is lost on the way
        [InlineData("[ProtoContract]", "public int? V { get; set; } = 5;")]
        // presence is managed explicitly, so the receiver can tell "absent" apart
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public int? V { get; set; } = 5; public bool VSpecified { get; set; }")]
        // the documented remedy that keeps the initializer for instances built in code
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public int? V { get; set; } = 5; [ProtoBeforeDeserialization] public void Reset() => V = null;")]
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public int? V { get; set; } = 5; [System.Runtime.Serialization.OnDeserializing] public void Reset(System.Runtime.Serialization.StreamingContext _) => V = null;")]
        // without null-wrapping a collection writes null and empty alike, so one comes back as the
        // other with or without the initializer; that is not the initializer's doing
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public List<int>? V { get; set; } = new();")]
        // no initializer runs on deserialize, so the null survives (probed)
        [InlineData("[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public int? V { get; set; } = 5;")]
        [InlineData("[ProtoContract(Surrogate = typeof(Msg))]", "[ProtoMember(1)] public int? V { get; set; } = 5;")]
        [InlineData("[ProtoContract(Serializer = typeof(FooSerializer))]", "[ProtoMember(1)] public int? V { get; set; } = 5;")]
        // a type protobuf-net treats as a collection never has its members read (probed)
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public int? V { get; set; } = 5; public IEnumerator<int> GetEnumerator() => null!; System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null!;", "class", " : IEnumerable<int>")]
        public async Task DoesNotReportNullableMember(string contract, string body, string kind = "class", string bases = "")
        {
            Assert.Empty(await DiagnosticsAsync(DataContractAnalyzer.NullableMemberInitialized, NullMemberSource(contract, body, kind: kind, bases: bases)));
        }

        [Fact]
        public async Task DoesNotReportNullableMemberOnStructContract()
        {
            Assert.Empty(await DiagnosticsAsync(DataContractAnalyzer.NullableMemberInitialized,
                NullMemberSource("[ProtoContract]", "[ProtoMember(1)] public int? V = 5; public Foo() { }", kind: "struct")));
        }

        // IsRequired changes nothing on the wire for a nullable member - it is written whenever it has
        // a value, zero included - and alongside [NullWrappedValue] protobuf-net refuses the model.
        // A declared default is worse than useless there: it stops that value being written, so the
        // fixes PBN0029 offers would lose it. What such a member loses is null, which is PBN0029's
        [Theory]
        [InlineData("[ProtoMember(1)] public int? V { get; set; } = 5;")]
        [InlineData("[ProtoMember(1), NullWrappedValue] public int? V { get; set; } = 5;")]
        [InlineData("[ProtoMember(1)] public System.DateTime? V { get; set; } = new System.DateTime(2000, 1, 1);")]
        [InlineData("[ProtoMember(1)] public Msg? V { get; set; } = new();")]
        [InlineData("[ProtoMember(1)] public string? V { get; set; } = \"x\";")]
        [InlineData("[ProtoMember(1)] public Color? V { get; set; } = Color.Blue;")]
        public async Task DoesNotSuggestIsRequiredOrDefaultForNullableMember(string body)
        {
            var diagnostics = await AnalyzeAsync(NullMemberSource("[ProtoContract]", body));
            Assert.DoesNotContain(diagnostics, x => x.Descriptor == DataContractAnalyzer.ShouldDeclareIsRequired
                || x.Descriptor == DataContractAnalyzer.ShouldDeclareDefault);
            Assert.Single(diagnostics, x => x.Descriptor == DataContractAnalyzer.NullableMemberInitialized);
        }

        // ...while an oblivious one still gets the nag it always did
        [Fact]
        public async Task StillSuggestsDefaultForObliviousMember()
        {
            Assert.Single(await DiagnosticsAsync(DataContractAnalyzer.ShouldDeclareDefault,
                NullMemberSource("[ProtoContract]", "[ProtoMember(1)] public string V { get; set; } = \"x\";", nullable: "#nullable disable")));
        }

        // PBN0030: a non-nullable member that is not a collection, where no initializer runs. A
        // serialized one is null whenever the payload lacks it - written before the member existed,
        // or by a sender that omits "" - and one that is not serialized, every time
        [Theory]
        [InlineData("[ProtoMember(1)] public string Value { get; set; } = \"\";")]
        [InlineData("[ProtoMember(1)] public required string Value { get; set; }")]
        [InlineData("[ProtoMember(1)] public Msg Value { get; set; } = new();")]
        [InlineData("[ProtoMember(1)] public byte[] Value { get; set; } = new byte[0];")]
        [InlineData("[ProtoMember(1)] public string Value { get; private set; } = \"\";")]
        // a contract implementing only the non-generic IEnumerable is a message, not a collection
        [InlineData("[ProtoMember(1)] public Bag Value { get; set; } = new();")]
        // a callback cannot assign these, so it is not offered
        [InlineData("public readonly object Value = new();", "declare it nullable")]
        [InlineData("public string Value { get; } = \"x\";", "declare it nullable")]
        [InlineData("[ProtoMember(1)] public string Value { get; init; } = \"x\";", "declare it nullable")]
        public async Task ReportsNonNullableMemberUnderSkipConstructor(string body, string fixes = "declare it nullable, or restore it in a deserialization callback")
        {
            var diag = Assert.Single(await DiagnosticsAsync(DataContractAnalyzer.NonNullableMemberLeftNull,
                NullMemberSource("[ProtoContract(SkipConstructor = true)]", body) + IsExternalInit));
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            Assert.Equal("'Value' is non-nullable, but SkipConstructor means no constructor or initializer runs on deserialize; "
                + (body.Contains("ProtoMember") ? "it is null whenever the payload does not carry it" : "it is not serialized, so it is null after every deserialize")
                + ". To fix: " + fixes + ".",
                diag.GetMessage(CultureInfo.InvariantCulture));
        }

        // a sub-type's SkipConstructor strips the members it inherits too (probed) - including from a
        // base that sets it as well, where the base's own setting is inert
        [Theory]
        [InlineData("[ProtoContract]", "class")]
        [InlineData("[ProtoContract(SkipConstructor = true)]", "abstract class")]
        public async Task ReportsNonNullableBaseMemberWhenASubTypeSkipsConstructor(string contract, string kind)
        {
            var diag = Assert.Single(await DiagnosticsAsync(DataContractAnalyzer.NonNullableMemberLeftNull,
                NullMemberSource(contract + "\n[ProtoInclude(10, typeof(Derived))]", "[ProtoMember(1)] public string Value { get; set; } = \"\";", kind: kind)
                    + "[ProtoContract(SkipConstructor = true)]\npublic class Derived : Foo { }"));
            Assert.StartsWith("'Value' is non-nullable, but SkipConstructor on the sub-type 'Derived' means no constructor or initializer runs when one is deserialized;",
                diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsNonNullableMemberOnStructContract()
        {
            var diag = Assert.Single(await DiagnosticsAsync(DataContractAnalyzer.NonNullableMemberLeftNull,
                NullMemberSource("[ProtoContract]", "[ProtoMember(1)] public string Value;", kind: "struct")));
            Assert.StartsWith("'Value' is non-nullable, but a struct contract is never constructed on deserialize", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Fact]
        public async Task ReportsNonNullablePositionalRecordMember()
        {
            var diag = Assert.Single(await DiagnosticsAsync(DataContractAnalyzer.NonNullableMemberLeftNull, @"
#nullable enable
using ProtoBuf;
[ProtoContract(SkipConstructor = true)]
public record Person([property: ProtoMember(1)] string Name);
" + IsExternalInit));
            Assert.Equal("Name", diag.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(diag.Location.SourceSpan));
            // init-only, so a callback could not restore it
            Assert.EndsWith(" To fix: declare it nullable.", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Theory]
        // constructed normally: only an absent field leaves it null, and CS8618 has had its say
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public string Value { get; set; } = null!;")]
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public required string Value { get; set; }")]
        [InlineData("[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public string Value { get; set; } = \"\";", "#nullable enable", "abstract class")]
        // it has promised nothing, or cannot be null
        [InlineData("[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public string? Value { get; set; }")]
        [InlineData("[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public string Value { get; set; } = \"\";", "#nullable disable")]
        [InlineData("[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public int Value { get; set; } = 5;")]
        // restored where it matters
        [InlineData("[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1)] public string Value { get; set; } = """";
                      [ProtoAfterDeserialization] public void After() => Value ??= """";")]
        // a collection is PBN0028's
        [InlineData("[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public List<int> Value { get; set; } = new();")]
        [InlineData("[ProtoContract(SkipConstructor = true, Surrogate = typeof(Msg))]", "[ProtoMember(1)] public string Value { get; set; } = \"\";")]
        [InlineData("[ProtoContract(SkipConstructor = true, Serializer = typeof(FooSerializer))]", "[ProtoMember(1)] public string Value { get; set; } = \"\";")]
        // ...and on a struct, a callback restores it as well as on a class (probed)
        [InlineData("[ProtoContract]", @"[ProtoMember(1)] public string Value;
                      [ProtoAfterDeserialization] public void After() => Value ??= """";", "#nullable enable", "struct")]
        public async Task DoesNotReportNonNullableMember(string contract, string body, string nullable = "#nullable enable", string kind = "class")
        {
            Assert.Empty(await DiagnosticsAsync(DataContractAnalyzer.NonNullableMemberLeftNull, NullMemberSource(contract, body, nullable, kind)));
        }
    }
}
