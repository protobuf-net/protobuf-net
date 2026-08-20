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

## Delimited encoding

`message_encoding = DELIMITED` is the one editions feature that is not a new capability for protobuf-net at
all: it is the framing protobuf-net has read and written since 2008, as `DataFormat.Group`. What editions
adds is the ability to *say so in a schema* - proto2 could only express it as an inline `group`, and proto3
removed the syntax entirely.

It is also usually the faster way to write. That, the recipes for both code-first and schema-first, and the
measurements are on their own page: **[delimited encoding (groups)](https://docs.protobuf-net.dev/delimited)**.

## See also

- [Delimited encoding (groups)](https://docs.protobuf-net.dev/delimited) - the `DELIMITED` feature in depth
- [Editions overview](https://protobuf.dev/editions/overview/) and
  [feature settings](https://protobuf.dev/editions/features/) on protobuf.dev
- [Edition 2023 language specification](https://protobuf.dev/reference/protobuf/edition-2023-spec/)
- [Generating code from `.proto` files](https://docs.protobuf-net.dev/contract_first)
- [Schema analysis tools](https://docs.protobuf-net.dev/schemas)
