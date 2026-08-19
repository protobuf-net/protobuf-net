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
| `message_encoding` (`LENGTH_PREFIXED`, `DELIMITED`) | `DataFormat.Group` (v4: also spelled `DataFormat.Delimited`) |
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

**Code first**: a contract that uses `DataFormat.Group` (v4 adds `DataFormat.Delimited` as a synonym for the
same value, matching the editions vocabulary; `Group` is not deprecated, and remains what `protogen` emits) -

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

protobuf-net picks between them by target, and neither choice is free:

- Writing to a **`Stream`**, it takes the third route. One byte is reserved for the prefix, optimistically,
  and while a sub-message is open the writer holds a *flush lock* - nothing can go through to the underlying
  stream, because the reserved prefix and everything after it may still have to move. Nest sub-messages and
  the locks nest with them, so the whole nested payload stays resident until the outermost one closes; and
  every sub-message whose length needs more than the byte reserved memmoves its entire body along to make
  room.
- Writing to an **`IBufferWriter<byte>`**, it cannot do that: the buffer belongs to the caller, so the writer
  must be strictly forwards-only and nothing may be revised after the fact. It takes the first route instead
  - a measuring pass over the sub-message into a null writer, then the prefix, then the body. The buffer
  writer refuses the back-fill path outright: *"You must use the WriteMessage API with this writer type"*.

Delimited framing needs neither: a start tag, the body, an end tag - no lock, no back-fill, no measuring
pass, whichever the target. The writer never looks back and never has to hold anything, which is old enough
to be regression-tested: the repo has a test that builds a hundred thousand nested objects, serializes them
with `DataFormat.Group`, and asserts that the writer buffered nothing at all along the way.

### Numbers

`src/Benchmark/DelimitedEncodingBenchmarks.cs` in this repo measures both framings, in protobuf-net and in
Google.Protobuf, over two shapes - **deep** (a chain of `Size` nested messages) and **wide** (one message
with `Size` children) - serializing to both write targets, and deserializing. Both libraries write the same
bytes for a given framing (the benchmark asserts it before measuring), so every column below is the same
work done a different way. The same file runs on the 3.3 line and on v4, which is what makes those columns
comparable. Run it with:

``` txt
dotnet run -c Release -f net8.0 --project src/Benchmark -- --filter *DelimitedEncoding*
```

Numbers below are from an AMD Ryzen 9 7900X on .NET 8, BenchmarkDotNet 0.15.8, in nanoseconds; they are
here to show the *shape* of the effect, and are worth re-measuring on your own data rather than trusting to
three significant figures. The v4 columns are from a development branch and will move; Google.Protobuf is
3.34.1 and is unaffected by which protobuf-net branch it is measured beside.

**Serialize to a `Stream`** - the back-fill route

| shape, size | 3.3 prefixed | 3.3 delimited | v4 prefixed | v4 delimited | Google prefixed | Google delimited |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| deep, 8 | 284 | 264 | 246 | **123** | 172 | 128 |
| deep, 64 | 3,168 | 2,325 | 1,653 | **363** | 6,453 | 613 |
| deep, 512 | 90,798 | 77,941 | 13,877 | **2,967** | 765,402 | 4,710 |
| wide, 8 | 353 | 356 | 240 | **139** | 159 | 151 |
| wide, 64 | 2,177 | 2,111 | 1,219 | **367** | 730 | 655 |
| wide, 512 | 16,848 | 16,430 | 9,131 | **2,522** | 5,424 | 4,863 |

**Serialize to an `IBufferWriter<byte>`** - the measure-first route

| shape, size | 3.3 prefixed | 3.3 delimited | v4 prefixed | v4 delimited | Google prefixed | Google delimited |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| deep, 8 | 638 | 277 | 226 | **107** | 108 | 64 |
| deep, 64 | 6,897 | 2,740 | 1,636 | **330** | 6,776 | 466 |
| deep, 512 | 120,501 | 81,601 | 14,044 | **2,340** | 787,929 | 4,424 |
| wide, 8 | 648 | 337 | 213 | **107** | 93 | 81 |
| wide, 64 | 4,308 | 2,182 | 1,172 | **345** | 636 | 554 |
| wide, 512 | 33,130 | 16,925 | 9,050 | **2,611** | 5,153 | 4,581 |

**Deserialize**

| shape, size | 3.3 prefixed | 3.3 delimited | v4 prefixed | v4 delimited | Google prefixed | Google delimited |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| deep, 8 | 283 | 268 | 97 | 83 | 279 | 223 |
| deep, 64 | 1,784 | 1,754 | 727 | 597 | 1,615 | 1,137 |
| deep, 512 | 14,842 | 14,968 | 5,901 | 6,594 | 13,120 | 9,189 |
| wide, 8 | 439 | 408 | 134 | 119 | 310 | 263 |
| wide, 64 | 2,016 | 1,921 | 689 | 601 | 1,652 | 1,241 |
| wide, 512 | 14,635 | 13,557 | 5,142 | 4,517 | 12,459 | 8,829 |

Reading those honestly:

- **On v4, delimited is the faster way to write, everywhere.** Every serialize cell, on both targets, by
  2-6×: at depth 512 that is 13,877 ns against 2,967 ns to a stream, and 14,044 against 2,340 to a buffer
  writer. There is no shape in this benchmark where length-prefixing wins on the write side.
- **On 3.3 the answer depends on the target.** Writing to an `IBufferWriter<byte>`, where a prefix costs a
  measuring pass over each sub-message, delimited wins every cell by 1.5-2.5×. Writing to a `Stream`, where
  the prefix is back-filled instead, the win narrows to depth - 14% at depth 512, 27% at depth 64, and a wash
  when the graph is shallow or merely wide, because a small sub-message's length fits the byte reserved and
  nothing has to move.
- **Google.Protobuf's length-prefixed writer goes quadratic in depth.** Its generated `WriteTo` calls
  `CalculateSize()` on each sub-message before writing it, and that call walks the whole subtree, so writing
  a chain of *n* nested messages does *O(n²)* measuring work. At depth 512 that is 765 µs length-prefixed
  against 4.7 µs delimited: the same data, the same library, **160× apart**, purely because the delimited
  path never needs to know a length. protobuf-net keeps a length cache and stays out of that.
- **Deserialization barely cares**, and is the one place delimited can lose: the reader has to walk to the
  end tag where a length would have let it skip. Mostly it is a few percent ahead; at depth 512 on v4 it is
  12% behind.
- This is a framing comparison rather than a scoreboard, but for reference: v4 is ahead of Google.Protobuf
  in every deserialize cell and every delimited-write cell, while Google still leads the *length-prefixed*
  writes of wide graphs.

The numbers are from the compile-time model (`[ProtoModel]`); on 3.3 the reflection-based `RuntimeTypeModel`
path is 10-35% slower on serialize and about 1.8× slower on deserialize, with the same shape to the framing
comparison.

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
- **Changing the framing changes the bytes** - though what that costs you depends entirely on who reads
  them. protobuf-net itself is relaxed: its reader dispatches on the wire type actually present, not on how
  the member is declared, so a protobuf-net consumer deserializes either framing either way round. Other
  implementations are generally not - `protoc`-generated parsers match on the whole tag, field number *and*
  wire type together, so a field that arrives in the framing they were not generated for falls through to
  their unknown-field set: it reads back as unset rather than as an error. Treat the switch as a wire break
  as soon as anything other than protobuf-net is reading, and as a rolling upgrade you can take at leisure
  when it is protobuf-net at both ends.
- **Not every consumer is willing.** The wire types are core protobuf and every complete implementation reads
  them, but a consumer generating from a proto3 schema has no way to *declare* the field, so they would be
  stuck hand-writing it. Editions is what fixes that - which is rather the point of this page.

## See also

- [Editions overview](https://protobuf.dev/editions/overview/) and
  [feature settings](https://protobuf.dev/editions/features/) on protobuf.dev
- [Edition 2023 language specification](https://protobuf.dev/reference/protobuf/edition-2023-spec/)
- [Generating code from `.proto` files](https://docs.protobuf-net.dev/contract_first)
- [Schema analysis tools](https://docs.protobuf-net.dev/schemas)
