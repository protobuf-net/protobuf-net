# Editions

For most of protobuf's life a schema had to pick a dialect: `syntax = "proto2";` or `syntax = "proto3";`.
Each dialect bundled a fixed set of decisions - whether fields track presence, whether `repeated` scalars are
packed, whether enums are open or closed - and the only way to change one of those decisions was to change
dialect, which changes all of the others too.

[Editions](https://protobuf.dev/editions/overview/) replaces that with a version and a set of dials. A file
says:

``` proto
edition = "2023";
```

and each of the behaviours that used to be implied by the dialect becomes an individually settable
[*feature*](https://protobuf.dev/editions/features/), at file, message, enum, oneof or field scope, inherited
lexically from the enclosing scope:

``` proto
edition = "2023";

message Order {
  int32 id = 1;
  Address home = 2 [features.message_encoding = DELIMITED];
  repeated int32 codes = 3 [features.repeated_field_encoding = EXPANDED];
}
```

Editions 2023 and 2024 are both generally available upstream (2024 shipped in protobuf v32).

## What protobuf-net supports

Editions is almost entirely a *schema-layer* feature, so nearly all of the work is in the schema tools -
[`protobuf-net.Reflection`](https://www.nuget.org/packages/protobuf-net.Reflection) and the things built on
it: the [`protogen` tool](https://www.nuget.org/packages/protobuf-net.Protogen),
[build-time generation](https://docs.protobuf-net.dev/contract_first), and
[protobuf-net.dev](https://protobuf-net.dev/).

- **Parsing**: editions 2023 and 2024 files parse, with full feature resolution (per-edition defaults,
  lexical inheritance, feature target and support-window checks). Behaviour is pinned against `protoc` 35.1:
  the same schemas produce the same descriptors, and the schemas `protoc` rejects are rejected here too.
- **Code generation**: C# and VB generation consults the *resolved* features at every decision point -
  presence, packedness, delimited encoding, legacy-required, closed-enum defaults.
- **Schema writing**: a code-first model can be written out as an editions file, which is where this gets
  interesting - see below.
- Edition 2026 is parse-tolerated only; it is not GA upstream yet.

The runtime library needed no new wire behaviour for any of this. Every editions feature that reaches the
wire maps onto something protobuf-net has had for years:

| feature | protobuf-net |
| --- | --- |
| `field_presence` (`EXPLICIT`, `IMPLICIT`, `LEGACY_REQUIRED`) | presence tracking; `LEGACY_REQUIRED` is `IsRequired` |
| `message_encoding` (`LENGTH_PREFIXED`, `DELIMITED`) | `DataFormat.Group` |
| `repeated_field_encoding` (`PACKED`, `EXPANDED`) | `IsPacked` |
| `enum_type` (`OPEN`, `CLOSED`) | parsed and round-tripped; **not enforced at runtime** - protobuf-net enums are open, as they are in Google's own C# runtime |
| `utf8_validation`, `json_format` | parsed; no runtime behaviour (protobuf-net does not offer a UTF-8 verify mode, and does not implement canonical JSON) |
| `enforce_naming_style`, `default_symbol_visibility` (2024) | enforced by the parser; source-retention only, so they never reach a descriptor |

## Delimited encoding, eighteen years early

The one that deserves its own section is `message_encoding = DELIMITED`.

A sub-message can be framed on the wire in two ways. The usual one is **length-prefixed**: wire type 2, a tag,
a varint length, then that many bytes. The other is **delimited**: wire type 3 to open, the body, then wire
type 4 to close - a start tag and an end tag, with no length anywhere. Both are core protobuf; the second is
what proto2 called a `group`.

protobuf-net has written and read this framing since **2008**: group reading landed on 23 July 2008, six
days after the project's first commit; group writing on 31 July; and it acquired its current spelling,
`DataFormat.Group`, on 13 August 2008. What it has *never* had is a way to say so in a `.proto` file:

- **proto2** had `group` syntax, but the group's body is declared inline and the field name is derived from
  the group name. That cannot express what protobuf-net does - an independently named member whose type is a
  separately declared (and possibly shared) message.
- **proto3** removed groups from the language entirely, while leaving the wire types perfectly valid.

Editions is the first dialect in which the intent has an exact spelling. The message is declared normally,
and the *field* says how it is framed:

``` proto
edition = "2023";

message Order {
  Address home = 3 [features.message_encoding = DELIMITED];
}
```

Both directions work.

**Schema first**: generating from the schema above gives you the attribute protobuf-net has always
understood -

``` c#
[global::ProtoBuf.ProtoMember(3, Name = @"home", DataFormat = global::ProtoBuf.DataFormat.Group)]
public Address Home { get; set; }
```

**Code first**: a contract that uses `DataFormat.Group` -

``` c#
[ProtoContract]
public class Order
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2, IsRequired = true)] public string Reference { get; set; }
    [ProtoMember(3, DataFormat = DataFormat.Group)] public Address Home { get; set; }
}
```

can now be described exactly:

``` c#
var schema = model.GetSchema(typeof(Order), ProtoSyntax.Edition2023);
```

``` proto
edition = "2023";
package ProtoBuf.Schemas;

message Address {
   string Line1 = 1;
}
message Order {
   int32 Id = 1;
   string Reference = 2 [features.field_presence = LEGACY_REQUIRED];
   Address Home = 3 [features.message_encoding = DELIMITED];
}
```

Ask the same model for proto2 or proto3 and it still writes `group Address Home = 3;`, but with a warning
comment above it: in proto3 that is not valid syntax at all, and in proto2 it is shorthand for a declaration
proto2 cannot actually make. Editions is the first dialect where the emitted schema needs no apology.

The bytes are not protobuf-net's own dialect of anything: for the same shape and data, protobuf-net's
`DataFormat.Group` output is byte-for-byte identical to what `protoc`'s C# generator plus `Google.Protobuf`
emit for `features.message_encoding = DELIMITED`, and likewise for the length-prefixed framing.

## Why you might want it

A length prefix has to be written *before* the body it describes, and its value is only known *after* the
body has been written. Every implementation resolves that somehow, and none of the options are free:

- measure the body first, then write it - a second pass over the object graph; or
- write the body into a buffer, then emit the length and copy the buffer out; or
- reserve space for the prefix, write the body, then go back and fill it in - and shuffle the body along if
  the length turned out to need more room than was reserved.

protobuf-net's stream writer takes the third route: one byte is reserved for the prefix, optimistically, and
while a sub-message is open the writer holds a *flush lock* - nothing can go through to the underlying stream,
because the reserved prefix and everything after it may still have to move. Nest sub-messages and the locks
nest with them, so the whole nested payload stays resident until the outermost one closes; and every
sub-message whose length needs more than the one byte reserved memmoves its entire body along to make room.

Delimited framing has none of that. Write the start tag, write the body, write the end tag; the writer never
looks back and never has to hold anything. That property is old enough to be regression-tested: the repo has
a test that builds a hundred thousand nested objects, serializes them with `DataFormat.Group`, and asserts
that the writer buffered nothing at all along the way.

### Numbers

`src/Benchmark/DelimitedEncodingBenchmarks.cs` in this repo measures both framings, serializing and
deserializing, in protobuf-net and in Google.Protobuf, over two shapes: **deep** (a chain of `Size` nested
messages) and **wide** (one message with `Size` children). Both libraries write the same bytes for a given
framing, so the four columns really are the same work done four ways. Run it with:

``` txt
dotnet run -c Release -f net8.0 --project src/Benchmark -- --filter *DelimitedEncoding*
```

Numbers below are from an AMD Ryzen 9 7900X on .NET 8, BenchmarkDotNet 0.15.8, in nanoseconds; they are
here to show the *shape* of the effect, and are worth re-measuring on your own data rather than trusting to
three significant figures.

**Serialize**

| shape, size | protobuf-net, prefixed | protobuf-net, delimited | Google, prefixed | Google, delimited |
| --- | ---: | ---: | ---: | ---: |
| deep, 8 | 421 | 420 | 169 | 128 |
| deep, 64 | 4,157 | 3,864 | 6,489 | 603 |
| deep, 512 | 100,621 | **85,799** | 869,482 | **4,777** |
| wide, 8 | 390 | 382 | 151 | 149 |
| wide, 64 | 2,311 | 2,402 | 732 | 658 |
| wide, 512 | 17,818 | 17,358 | 5,437 | 4,827 |

**Deserialize**

| shape, size | protobuf-net, prefixed | protobuf-net, delimited | Google, prefixed | Google, delimited |
| --- | ---: | ---: | ---: | ---: |
| deep, 8 | 424 | 408 | 281 | 221 |
| deep, 64 | 2,973 | 2,913 | 1,624 | 1,166 |
| deep, 512 | 26,906 | 22,679 | 13,228 | 9,141 |
| wide, 8 | 485 | 456 | 306 | 265 |
| wide, 64 | 2,089 | 1,996 | 1,660 | 1,252 |
| wide, 512 | 14,541 | 13,952 | 12,375 | 8,786 |

Reading those honestly:

- **Depth is the axis that matters, breadth is not.** For protobuf-net, delimited framing is worth about 15%
  of both serialize and deserialize at depth 512, a few percent at depth 64, and nothing at all when the
  graph is shallow or merely wide - one wide case is even marginally slower. Lots of *small* sub-messages
  cost little to length-prefix, because their lengths fit in the one byte reserved and nothing has to move.
- **The same axis is far more dramatic in Google.Protobuf**, for a different reason. Its generated
  `WriteTo` calls `CalculateSize()` on each sub-message before writing it, and that call walks the whole
  subtree - so writing a chain of *n* nested messages does *O(n²)* measuring work. At depth 512
  that is 869 µs length-prefixed against 4.8 µs delimited: the same data, the same library, **180× apart**,
  purely because the delimited path never needs to know a length. protobuf-net's back-fill approach is
  linear instead, which is why its length-prefixed number at that depth is 8.6× faster than Google's.
- **Google.Protobuf is faster than protobuf-net in most of the other cells**, and that is worth saying
  plainly: this page is about a framing choice, not a scoreboard.

Allocations are in the benchmark output rather than the tables above, because the two libraries are not
directly comparable there: protobuf-net's serialize path allocates nothing at all, while Google's API builds
a fresh `CodedOutputStream` / `CodedInputStream` - and its buffer - per call, which the numbers include.

### Size

Delimited framing is never larger here, and at depth it is smaller. A length-prefixed field costs
`tag + varint(length)`; a delimited one costs `start tag + end tag`. Those are the same size while the body
stays under 128 bytes, and the delimited form wins as soon as the length needs a second varint byte - which
in a deep graph is true of every outer level:

| shape, size | length-prefixed | delimited |
| --- | ---: | ---: |
| deep, 8 | 30 | 30 |
| deep, 64 | 285 | 254 |
| deep, 512 | 2,917 | 2,431 |
| wide, 8 | 34 | 34 |
| wide, 64 | 258 | 258 |
| wide, 512 | 2,435 | 2,435 |

## When not to use it

- **Skipping is cheaper with a length prefix.** A reader that wants to ignore a field can jump over a
  length-prefixed one; a delimited one has to be walked to find its matching end tag. If your readers skip a
  lot of what they receive, the prefix earns its keep.
- **Changing the framing changes the bytes.** It is a wire-breaking change, like changing a field number:
  both ends have to agree.
- **Not every consumer is willing.** The wire types are core protobuf and every complete implementation reads
  them, but a consumer generating from a proto3 schema has no way to *declare* the field, so they would be
  stuck hand-writing it. Editions is what fixes that - which is rather the point of this page.

## See also

- [Editions overview](https://protobuf.dev/editions/overview/) and
  [feature settings](https://protobuf.dev/editions/features/) on protobuf.dev
- [Edition 2023 language specification](https://protobuf.dev/reference/protobuf/edition-2023-spec/)
- [Generating code from `.proto` files](https://docs.protobuf-net.dev/contract_first)
- [Schema analysis tools](https://docs.protobuf-net.dev/schemas)
