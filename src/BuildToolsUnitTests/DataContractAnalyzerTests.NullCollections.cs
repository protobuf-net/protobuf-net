using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Analyzers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BuildToolsUnitTests
{
    // PBN0027: an empty collection is normally not written at all, so on read the member keeps
    // whatever construction left there - and for a non-nullable collection that nothing initializes,
    // that is a null the compiler believes cannot happen. Every "reported" shape below was probed
    // against RuntimeTypeModel and comes back null from an empty collection; every "not reported"
    // one either comes back empty or has promised nothing.
    public partial class ProtobufFieldAnalyzerTests
    {
        // positional records need the init-only marker, which the netstandard reference set lacks
        private const string IsExternalInit = @"
namespace System.Runtime.CompilerServices
{
    public class IsExternalInit {}
}";

        private async Task<List<Diagnostic>> NullCollectionDiagnosticsAsync(string source)
            => (await AnalyzeAsync(source)).Where(x => x.Descriptor == DataContractAnalyzer.NonNullableCollectionLeftNull).ToList();

        // the shape that motivated the rule: no [ProtoMember], so nothing ever writes it, and
        // SkipConstructor means the primary constructor never runs either - it is null every time
        [Fact]
        public async Task ReportsPositionalRecordUnderSkipConstructor()
        {
            var diags = await NullCollectionDiagnosticsAsync(@"
#nullable enable
using ProtoBuf;
[ProtoContract(SkipConstructor = true)]
public record TestRecord(string[] Array);
" + IsExternalInit);

            var diag = Assert.Single(diags);
            Assert.Equal(DiagnosticSeverity.Warning, diag.Severity);
            var message = diag.GetMessage(CultureInfo.InvariantCulture);
            Assert.StartsWith("'Array' is a non-nullable collection, but SkipConstructor means no constructor or initializer runs on deserialize;", message);

            var span = diag.Location.SourceSpan;
            var text = (await diag.Location.SourceTree!.GetTextAsync()).ToString().Substring(span.Start, span.Length);
            Assert.Equal("Array", text);
        }

        [Theory]
        // an initializer is compiled into the constructor, so it does not run either
        [InlineData("[ProtoMember(1)] public List<int> Items { get; set; } = new();")]
        [InlineData("[ProtoMember(1)] public List<int> Items { get; } = new();")]
        [InlineData("[ProtoMember(1)] public List<int> Items { get; init; } = new();")]
        [InlineData("[ProtoMember(1)] public int[] Items = new int[0];")]
        [InlineData("[ProtoMember(1)] public Dictionary<int, string> Items { get; set; } = new();")]
        [InlineData("[ProtoMember(1)] public IList<int> Items { get; set; } = new List<int>();")]
        [InlineData("[ProtoMember(1)] public HashSet<string> Items { get; set; } = new();")]
        [InlineData("[ProtoMember(1, IsPacked = true)] public int[] Items { get; set; } = new int[0];")]
        // nor does the constructor
        [InlineData("[ProtoMember(1)] public List<int> Items { get; set; } public Foo() { Items = new(); }")]
        // not part of the contract at all: nothing ever assigns it
        [InlineData("public List<int> Items { get; set; } = new();")]
        [InlineData("private readonly Dictionary<int, int> Items = new();")]
        [InlineData("[ProtoIgnore] public List<int> Items { get; set; } = new();")]
        // a callback that assigns something else does not count
        [InlineData(@"[ProtoMember(1)] public List<int> Items { get; set; } = new();
                      public int Other { get; set; }
                      [ProtoAfterDeserialization] public void After() => Other = 1;")]
        public async Task ReportsUnderSkipConstructor(string body)
        {
            var diags = await NullCollectionDiagnosticsAsync($@"
#nullable enable
using ProtoBuf;
using System.Collections.Generic;
[ProtoContract(SkipConstructor = true)]
public class Foo {{
    {body}
}}
" + IsExternalInit);

            var diag = Assert.Single(diags);
            Assert.StartsWith("'Items' is a non-nullable collection, but SkipConstructor means", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        // a struct contract is read into `default` and never constructed, SkipConstructor or not -
        // so neither its initializers nor its explicit parameterless constructor run (probed)
        [Theory]
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public List<int> Items = new(); public Foo() { }")]
        [InlineData("[ProtoContract]", "[ProtoMember(1)] public List<int> Items; public Foo() { Items = new(); }")]
        [InlineData("[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public List<int> Items { get; set; }")]
        public async Task ReportsOnStructContract(string contract, string body)
        {
            var diags = await NullCollectionDiagnosticsAsync($@"
#nullable enable
using ProtoBuf;
using System.Collections.Generic;
{contract}
public struct Foo {{
    {body}
}}");

            var diag = Assert.Single(diags);
            Assert.StartsWith("'Items' is a non-nullable collection, but a struct contract is never constructed on deserialize", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        // only the hierarchy root's callbacks are invoked (TypeSerializer checks IsRootType), so a
        // sub-type's own callback restores nothing at run time and must not silence the warning
        [Theory]
        [InlineData("[ProtoContract(SkipConstructor = true)]", "= new();")]
        [InlineData("[ProtoContract]", "= null!;")]
        public async Task ReportsWhenOnlyASubTypeCallbackRestores(string contract, string initializer)
        {
            var diags = await NullCollectionDiagnosticsAsync($@"
#nullable enable
using ProtoBuf;
using System.Collections.Generic;
[ProtoContract, ProtoInclude(10, typeof(Derived))]
public class Base {{ }}
{contract}
public class Derived : Base {{
    [ProtoMember(1)] public List<int> Items {{ get; set; }} {initializer}
    [ProtoAfterDeserialization] public void After() => Items ??= new();
}}");

            var diag = Assert.Single(diags);
            Assert.Contains("'Items'", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        // a list-like type that opts out of list handling is an ordinary message, so SkipConstructor
        // applies to it like any other contract
        [Fact]
        public async Task ReportsOnListLikeContractThatIgnoresListHandling()
        {
            var diags = await NullCollectionDiagnosticsAsync(@"
#nullable enable
using ProtoBuf;
using System.Collections.Generic;
[ProtoContract(SkipConstructor = true, IgnoreListHandling = true)]
public class Foo : List<int> {
    [ProtoMember(1)] public List<string> Tags { get; } = new();
}");

            var diag = Assert.Single(diags);
            Assert.StartsWith("'Tags' is a non-nullable collection, but SkipConstructor means", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        // a contract base that does not [ProtoInclude] the type leaves it a root of its own, and a
        // root's callbacks run (probed) - PBN0013 is what reports the missing include
        [Fact]
        public async Task DoesNotReportWhenAnUnincludedSubTypeCallbackRestores()
        {
            var diags = await NullCollectionDiagnosticsAsync(@"
#nullable enable
using ProtoBuf;
using System.Collections.Generic;
[ProtoContract]
public class Base { }
[ProtoContract(SkipConstructor = true)]
public class Derived : Base {
    [ProtoMember(1)] public List<int> Items { get; set; } = new();
    [ProtoAfterDeserialization] public void After() => Items ??= new();
}");

            Assert.Empty(diags);
        }

        // ...but overriding a virtual callback the root declares does run, since the root's method
        // is invoked virtually
        [Fact]
        public async Task DoesNotReportWhenASubTypeOverridesTheRootCallback()
        {
            var diags = await NullCollectionDiagnosticsAsync(@"
#nullable enable
using ProtoBuf;
using System.Collections.Generic;
[ProtoContract, ProtoInclude(10, typeof(Derived))]
public class Base {
    [ProtoAfterDeserialization] public virtual void After() { }
}
[ProtoContract(SkipConstructor = true)]
public class Derived : Base {
    [ProtoMember(1)] public List<int> Items { get; set; } = new();
    public override void After() => Items ??= new();
}");

            Assert.Empty(diags);
        }

        [Theory]
        // no initializer at all: CS8618 says so too, but CS8618 is routinely switched off in DTO projects
        [InlineData("[ProtoMember(1)] public List<int> Items { get; set; }")]
        // ...and these are the routine ways of silencing it, none of which assign anything
        [InlineData("[ProtoMember(1)] public List<int> Items { get; set; } = null!;")]
        [InlineData("[ProtoMember(1)] public List<int> Items { get; set; } = default!;")]
        [InlineData("[ProtoMember(1)] public List<int> Items { get; set; } = default(List<int>)!;")]
        [InlineData("[ProtoMember(1)] public string[] Items = null!;")]
        [InlineData("[ProtoMember(1)] public required List<int> Items { get; set; }")]
        // protobuf-net calls the parameterless constructor, not the one that assigns it
        [InlineData(@"[ProtoMember(1)] public List<int> Items { get; set; }
                      private Foo() { }
                      public Foo(List<int> items) { Items = items; }")]
        public async Task ReportsWhenNothingInitializes(string body)
        {
            var diags = await NullCollectionDiagnosticsAsync($@"
#nullable enable
using ProtoBuf;
using System.Collections.Generic;
[ProtoContract]
public class Foo {{
    {body}
}}");

            var diag = Assert.Single(diags);
            Assert.Equal(
                "'Items' is a non-nullable collection, but nothing assigns it when protobuf-net constructs the instance; it is null whenever the payload does not carry it, and an empty collection is normally not written at all. To fix: declare it nullable, initialize it, or mark it [NullWrappedCollection] so that an empty one is written (this changes the wire format).",
                diag.GetMessage(CultureInfo.InvariantCulture));
        }

        // the message suggests only what works: an initializer needs a constructor that runs,
        // null-wrapping needs a member that is written, and with no constructor a callback is left
        [Theory]
        [InlineData("[ProtoContract]", "public List<int> Items { get; set; } = null!;",
            "declare it nullable, or initialize it")]
        [InlineData("[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public List<int> Items { get; set; } = new();",
            "declare it nullable, mark it [NullWrappedCollection] so that an empty one is written (this changes the wire format), or restore it in a deserialization callback")]
        [InlineData("[ProtoContract(SkipConstructor = true)]", "public List<int> Items { get; set; } = new();",
            "declare it nullable, or restore it in a deserialization callback")]
        public async Task SuggestsOnlyFixesThatWork(string contract, string body, string fixes)
        {
            var diags = await NullCollectionDiagnosticsAsync($@"
#nullable enable
using ProtoBuf;
using System.Collections.Generic;
{contract}
public class Foo {{
    {body}
}}");

            var diag = Assert.Single(diags);
            Assert.EndsWith(" To fix: " + fixes + ".", diag.GetMessage(CultureInfo.InvariantCulture));
            Assert.Equal(!contract.Contains("SkipConstructor") ? "true" : "false", diag.Properties["Constructed"]);
            Assert.Equal(body.Contains("ProtoMember") ? "true" : "false", diag.Properties["HasProtoMember"]);
        }

        // an abstract base is constructed as part of the concrete type, which runs its constructor
        // as usual - so the question is only whether anything in it assigns the member
        [Fact]
        public async Task ReportsOnAbstractBaseWhenNothingInitializes()
        {
            var diags = await NullCollectionDiagnosticsAsync(@"
#nullable enable
using ProtoBuf;
using System.Collections.Generic;
[ProtoContract, ProtoInclude(10, typeof(Derived))]
public abstract class Base {
    [ProtoMember(1)] public List<int> Items { get; set; } = null!;
}
[ProtoContract]
public class Derived : Base { }");

            var diag = Assert.Single(diags);
            Assert.Contains("'Items'", diag.GetMessage(CultureInfo.InvariantCulture));
        }

        [Theory]
        // declared nullable: the compiler already makes every reader deal with null
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public List<int>? Items { get; set; }")]
        [InlineData("#nullable enable", "[ProtoContract]", "[ProtoMember(1)] public List<int>? Items { get; set; }")]
        // oblivious: nothing was promised
        [InlineData("", "[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public List<int> Items { get; set; } = new();")]
        [InlineData("#nullable disable", "[ProtoContract]", "[ProtoMember(1)] public List<int> Items { get; set; }")]
        // not a collection, or not one that can be null
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public string Name { get; set; } = \"\";")]
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public byte[] Data { get; set; } = new byte[0];")]
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public System.Collections.Immutable.ImmutableArray<int> Items { get; set; }")]
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", "public static List<int> Items { get; } = new();")]
        // initialized by construction
        [InlineData("#nullable enable", "[ProtoContract]", "[ProtoMember(1)] public List<int> Items { get; set; } = new();")]
        [InlineData("#nullable enable", "[ProtoContract]", "[ProtoMember(1)] public List<int> Items { get; } = new List<int>();")]
        [InlineData("#nullable enable", "[ProtoContract]", "[ProtoMember(1)] public List<int> Items { get; set; } = [];")]
        [InlineData("#nullable enable", "[ProtoContract]", "[ProtoMember(1)] public int[] Items = System.Array.Empty<int>();")]
        [InlineData("#nullable enable", "[ProtoContract]", "[ProtoMember(1)] public List<int> Items { get; set; } public Foo() { Items = new(); }")]
        [InlineData("#nullable enable", "[ProtoContract]", "[ProtoMember(1)] public List<int> Items { get; set; } public Foo() { this.Items = new(); }")]
        [InlineData("#nullable enable", "[ProtoContract]", "[ProtoMember(1)] public List<int> Items { get; set; } public Foo() : this(new List<int>()) { } public Foo(List<int> items) { Items = items; }")]
        // restored by a deserialization callback, which runs on either path
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1)] public List<int> Items { get; set; } = new();
                      [ProtoAfterDeserialization] public void After() => Items ??= new();")]
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1)] public List<int> Items { get; set; } = new();
                      [ProtoBeforeDeserialization] public void Before() { Items = new(); }")]
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1)] public List<int> Items { get; set; } = new();
                      [System.Runtime.Serialization.OnDeserialized] public void After(System.Runtime.Serialization.StreamingContext ctx) => this.Items ??= new();")]
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1)] public List<int> Items { get; set; } = new();
                      [System.Runtime.Serialization.OnDeserializing] public void Before(System.Runtime.Serialization.StreamingContext ctx) => Items = new();")]
        [InlineData("#nullable enable", "[ProtoContract]", @"[ProtoMember(1)] public List<int> Items { get; set; } = null!;
                      [ProtoAfterDeserialization] public void After() => Items ??= new();")]
        // SkipConstructor on an abstract type is inert: the concrete type's own setting decides
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public List<int> Items { get; set; } = new();", "abstract class")]
        // a surrogate is what gets constructed, never this type
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true, Surrogate = typeof(FooSurrogate))]", "[ProtoMember(1)] public List<int> Items { get; set; } = new();")]
        // with no parameterless constructor there is nothing to deserialize into - PBN0015's error
        [InlineData("#nullable enable", "[ProtoContract]", "[ProtoMember(1)] public List<int> Items { get; set; } public Foo(int x) { Items = null!; }")]
        // an abstract type is constructed through whichever constructor the concrete type picks
        [InlineData("#nullable enable", "[ProtoContract]", "[ProtoMember(1)] public List<int> Items { get; set; } protected Foo(List<int> items) { Items = items; }", "abstract class")]
        // a hand-written serializer does its own constructing
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true, Serializer = typeof(FooSerializer))]", "[ProtoMember(1)] public List<int> Items { get; set; } = new();")]
        // null-wrapping writes the wrapper even for an empty collection, which then comes back empty
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1), NullWrappedCollection] public List<int> Items { get; set; } = new();")]
        [InlineData("#nullable enable", "[ProtoContract]", "[ProtoMember(1), NullWrappedCollection] public Dictionary<int, string> Items { get; set; } = null!;")]
        [InlineData("#nullable enable", "[ProtoContract]", "[ProtoMember(1), NullWrappedCollection(AsGroup = true)] public int[] Items = null!;")]
        // an enumerable message is written even when empty, so it is no collection here
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", "[ProtoMember(1)] public PathMessage Path { get; set; } = new();")]
        // a type protobuf-net treats as a collection is activated with its constructor, so
        // SkipConstructor has no effect on it
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", "public List<string> Tags { get; } = new();", "class", " : List<int>")]
        // assignments the compiler itself accepts as initializing: a [MemberNotNull] helper or
        // setter, and a tuple deconstruction
        [InlineData("#nullable enable", "[ProtoContract]", @"[ProtoMember(1)] public List<int> Items { get; set; }
                      public Foo() { Init(); }
                      [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(Items))] private void Init() => Items = new();")]
        [InlineData("#nullable enable", "[ProtoContract]", @"private List<int> _items;
                      [ProtoMember(1)] public List<int> Items { get => _items; [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(_items))] set => _items = value; }
                      public Foo() { Items = new(); }")]
        [InlineData("#nullable enable", "[ProtoContract]", @"[ProtoMember(1)] public List<int> Items { get; set; }
                      [ProtoMember(2)] public List<int> Others { get; set; }
                      public Foo() { (Items, Others) = (new(), new()); }")]
        [InlineData("#nullable enable", "[ProtoContract(SkipConstructor = true)]", @"[ProtoMember(1)] public List<int> Items { get; set; }
                      [ProtoAfterDeserialization] public void After() => Restore();
                      [System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(Items))] private void Restore() => Items ??= new();")]
        public async Task DoesNotReport(string nullable, string contract, string body, string kind = "class", string bases = "")
        {
            var diags = await NullCollectionDiagnosticsAsync($@"
{nullable}
using ProtoBuf;
using System.Collections.Generic;
{contract}
public {kind} Foo{bases} {{
    {body}
}}
[ProtoContract]
public class FooSurrogate {{
    public static implicit operator Foo?(FooSurrogate? value) => null;
    public static implicit operator FooSurrogate?(Foo? value) => null;
}}
public class FooSerializer {{ }}
[ProtoContract(IgnoreListHandling = true)]
public class PathMessage : IEnumerable<int> {{
    public IEnumerator<int> GetEnumerator() => throw null!;
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw null!;
}}");

            Assert.Empty(diags);
        }

        // the motivating record, fixed by null-wrapping: the wrapper is written for an empty array
        [Fact]
        public async Task DoesNotReportNullWrappedPositionalRecord()
        {
            var diags = await NullCollectionDiagnosticsAsync(@"
#nullable enable
using ProtoBuf;
[ProtoContract(SkipConstructor = true)]
public record TestRecord([property: ProtoMember(1), NullWrappedCollection] string[] Array);
" + IsExternalInit);

            Assert.Empty(diags);
        }

        // a positional record without SkipConstructor needs a parameterless constructor, and C# makes
        // that chain to the primary one - which may assign anything, so the check stands down
        [Fact]
        public async Task DoesNotReportPositionalRecordWithChainedConstructor()
        {
            var diags = await NullCollectionDiagnosticsAsync(@"
#nullable enable
using ProtoBuf;
using System.Collections.Generic;
[ProtoContract]
public record Foo([property: ProtoMember(1)] List<int> Items)
{
    public Foo() : this(new List<int>()) { }
}
" + IsExternalInit);

            Assert.Empty(diags);
        }

        // ...and an abstract one is constructed through whichever constructor the derived type picks
        [Fact]
        public async Task DoesNotReportAbstractPositionalRecord()
        {
            var diags = await NullCollectionDiagnosticsAsync(@"
#nullable enable
using ProtoBuf;
using System.Collections.Generic;
[ProtoContract, ProtoInclude(10, typeof(Derived))]
public abstract record Base([property: ProtoMember(1)] List<int> Items);
[ProtoContract]
public record Derived() : Base(new List<int>());
" + IsExternalInit);

            Assert.Empty(diags);
        }

        // a partial type is visited once per declaration, and each member must be reported once
        [Fact]
        public async Task ReportsOncePerMemberAcrossPartialDeclarations()
        {
            var diagnostics = await AnalyzeMultiFileAsync(new List<string>
            {
@"
#nullable enable
using ProtoBuf;
using System.Collections.Generic;
[ProtoContract(SkipConstructor = true)]
public partial class Foo
{
    [ProtoMember(1)] public List<int> First { get; set; } = new();
}",
@"
#nullable enable
using System.Collections.Generic;
public partial class Foo
{
    public List<int> Second { get; set; } = new();
}",
            });

            var diags = diagnostics.Where(x => x.Descriptor == DataContractAnalyzer.NonNullableCollectionLeftNull)
                .Select(x => x.GetMessage(CultureInfo.InvariantCulture)).OrderBy(x => x).ToList();
            Assert.Equal(2, diags.Count);
            Assert.StartsWith("'First'", diags[0]);
            Assert.StartsWith("'Second'", diags[1]);
        }
    }
}
