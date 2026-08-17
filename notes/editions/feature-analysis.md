# Editions: feature analysis for protobuf-net

*Written 2026-08-17, against upstream `descriptor.proto` from `protocolbuffers/protobuf@main`
(which now includes the in-development Edition 2026) and the protobuf.dev editions docs.
Scope decision: full semantics for editions **2023 and 2024**; 2026 is parse-tolerated only.
Work lands on `main`.*

## The one-paragraph conclusion

Editions is almost entirely a **schema-layer** feature: every editions feature that reaches the
wire maps onto machinery protobuf-net has had for years (`DataFormat.Group`, `IsPacked`,
`IsRequired`, presence tracking), and the features that don't reach the wire (`json_format`,
`utf8_validation`, naming style, symbol visibility) need parser/validation work but **no runtime
library changes**. The expected outcome is that `protobuf-net` (the runtime package) ships
unchanged, and all the work is in `protobuf-net.Reflection` (parser, codegen, schema-writer) plus
the corpus/protoc refresh. The one deliberate divergence to document: `enum_type = CLOSED` will
parse but not be enforced at runtime — protobuf-net removed enum validity checking on purpose and
is not reintroducing it.

## How editions works (mechanics we must build)

An editions file replaces `syntax = "proto3";` with `edition = "2023";` (or `"2024"`). In the
descriptor this becomes `FileDescriptorProto.syntax = "editions"` **plus**
`FileDescriptorProto.edition = EDITION_2023` (enum `Edition`: `EDITION_PROTO2 = 998`,
`EDITION_PROTO3 = 999`, `EDITION_2023 = 1000`, `EDITION_2024 = 1001`, `EDITION_2026 = 1002`, plus
legacy/test placeholders).

All per-item behaviour moves into a `FeatureSet` message, reachable as a `features` field on
*every* `*Options` message (file, message, field, oneof, extension-range, enum, enum-value,
service, method). Spelled `option features.enum_type = CLOSED;` at file/type scope, or
`[features.field_presence = IMPLICIT]` at field scope.

**Resolution is lexical inheritance** (the protobuf.dev implementation guide's algorithm):

1. Look up the per-edition defaults (`FeatureSetDefaults`): binary-search for the highest entry
   ≤ the file's edition; merge `overridable_features` into `fixed_features`.
2. Merge the file's explicit `features`.
3. Recurse: each child inherits its parent's resolved set and merges its own explicit `features`.

Each feature declares (via `FieldOptions`) its legal `targets`, its `edition_defaults` (a
piecewise-constant timeline: "EXPLICIT from LEGACY, IMPLICIT from PROTO3, EXPLICIT from 2023"),
its `feature_support` window (`edition_introduced` / `edition_deprecated` / `edition_removed`),
and its `retention` — `RETENTION_SOURCE` features (naming style, visibility) are stripped from
runtime descriptors; `RETENTION_RUNTIME` ones are kept.

**Legacy unification**: the implementation guide recommends treating proto2/proto3 as
"legacy editions" (`EDITION_PROTO2`/`EDITION_PROTO3`) with an inference pass —
`LABEL_REQUIRED` ⇒ `LEGACY_REQUIRED`, `TYPE_GROUP` ⇒ `DELIMITED`, `[packed=true]` ⇒ `PACKED`,
proto3 `[packed=false]` ⇒ `EXPANDED`. That is attractive for us: the parser and codegen currently
branch on the raw `ctx.Syntax` string in ~20 places; unifying onto resolved features + helper
predicates (`HasPresence`, `IsPacked`, `IsDelimited`, `IsRequired`, `IsClosed`) removes the
three-way branching instead of adding a third arm to every site.

**Custom features**: `FeatureSet` has an extension range (1000–9994) with declared slots for
`.pb.cpp`, `.pb.java`, `.pb.go`, `.pb.python` — and **`.pb.csharp` at 1004**, which is Google's
(for their C# runtime): `c_sharp_features.proto` ships in the protoc include set declaring
`CSharpFeatures` as an *empty* placeholder message. Note this numbering space is separate from the
old `*Options` extension registry (`docs/options.md`), where protobuf-net holds **1037** — no
interaction either way. We must *parse and round-trip* third-party feature extensions (they arrive
through the ordinary custom-option machinery); protobuf-net's own features would need a FeatureSet
slot allocated upstream — deliberately deferred until there is a concrete need.

## Descriptor representation: the facts that make this cheap

From the implementation guide, deliberately kept for downstream migration ease:

- A field with `features.message_encoding = DELIMITED` is **still recorded as `TYPE_GROUP`** in
  `FieldDescriptorProto`.
- A field with `features.field_presence = LEGACY_REQUIRED` **still gets `LABEL_REQUIRED`**.

Our codegen already maps `TYPE_GROUP` → `DataFormat.Group` (`CSharpCodeGenerator.cs:960`) and
`LABEL_REQUIRED` → `IsRequired = true` (`CSharpCodeGenerator.cs:498`). So for the two features
with real wire impact, **the descriptor→codegen half already works**; the work is in the parser
producing protoc-identical descriptors (and in flipping the remaining syntax-string checks onto
resolved features).

## Cross-language features

Defaults below read *proto2 / proto3 / 2023 / 2024*. "Classification" answers Marc's question:
brand-new capability, or a re-skin of an existing protobuf-net concept.

| feature | values | defaults (p2/p3/2023/2024) | wire impact | protobuf-net mapping | classification |
| --- | --- | --- | --- | --- | --- |
| `field_presence` | EXPLICIT, IMPLICIT, LEGACY_REQUIRED | EXPLICIT / IMPLICIT / EXPLICIT / EXPLICIT | write-guard semantics (presence vs default-suppression) | existing: presence tracking (`TrackFieldPresence`, currently keyed on syntax + `proto3_optional`); LEGACY_REQUIRED ⇒ existing `IsRequired` via `LABEL_REQUIRED` | **existing, default flip** — editions defaults to proto2-style explicit presence; proto3's implicit becomes the opt-in |
| `message_encoding` | LENGTH_PREFIXED, DELIMITED | LP / LP / LP / LP | **yes** — start/end group tags vs length prefix | `DataFormat.Group`, supported for ~two decades; descriptor still says `TYPE_GROUP`, codegen path already emits it | **exact 1:1 to existing** — the headline match |
| `repeated_field_encoding` | PACKED, EXPANDED | EXPANDED / PACKED / PACKED / PACKED | **yes** — packed encoding | `IsPacked` (currently `IsPackedField(syntax)` + `[packed]` option) | **existing, default flip** vs proto2 |
| `enum_type` | OPEN, CLOSED | CLOSED / OPEN / OPEN / OPEN | no (affects *reader* handling of unknown values) | protobuf-net removed enum validity checking with v3; Marc is open to re-adding **known-value checking** (never value *mapping*, which is not coming back). The protobuf-defined semantic is **read-side**: a closed enum treats an unrecognised value as an unknown field (retained via unknown-field/extension data where possible, field left default) — *not* a throw. Write-side checking is optional strictness no other runtime effectively has (their type systems prevent it); C# enums being raw ints makes it possible here. | **candidate runtime feature** — read-side retention for conformance; write-side strictness a separate opt-in decision |
| `utf8_validation` | VERIFY, NONE | NONE / VERIFY / VERIFY / VERIFY | no (validation, not encoding) | none: protobuf-net encodes via .NET UTF-8 without a verify mode | **parse-only; documented gap** — a runtime VERIFY option would be a new library feature; not planned this pass |
| `json_format` | ALLOW, LEGACY_BEST_EFFORT | LBE / ALLOW / ALLOW / ALLOW | no | protobuf-net.Reflection does no canonical-JSON; ALLOW implies protoc-side JSON-name conflict checks we may mirror in validation | **parse-only** |
| `enforce_naming_style` (2024, `RETENTION_SOURCE`) | STYLE_LEGACY, STYLE2024, (STYLE2026) | legacy / legacy / legacy / **STYLE2024** | no | **new parser validation**: style-guide naming enforced as errors (lower_snake fields, PascalCase messages, etc.) to match protoc behaviour | **brand new (parser-only)** |
| `default_symbol_visibility` (2024, `RETENTION_SOURCE`) | EXPORT_ALL, EXPORT_TOP_LEVEL, LOCAL_ALL, STRICT | all / all / all / **EXPORT_TOP_LEVEL** | no | **new parser feature**: `export`/`local` modifiers, `visibility` fields on `DescriptorProto`(11)/`EnumDescriptorProto`(6), import-resolution enforcement (a `local` symbol can't be referenced cross-file) | **brand new (parser-only)** |
| `enforce_proto_limits` (2026) | LEGACY_NO_EXPLICIT_LIMITS, PROTO_LIMITS2026 | legacy everywhere until 2026 | no | out of chosen scope | **parse-tolerate only** |

Language-scoped features (`(pb.cpp).string_type`, `(pb.java).nest_in_file_class`,
`(pb.go).api_level`, …) affect other generators' output only; for us they are opaque extension
data to parse and round-trip. If protobuf-net ever wants generator knobs as features (the
`protogen.proto` custom options are the obvious candidates), `.pb.csharp`/a protobuf-net slot is
the mechanism — future work, noted and parked.

## Syntax-level changes (parser work)

**Edition 2023** (vs proto2/proto3):

- `edition = "2023";` replaces the `syntax` line.
- Field labels: only `repeated` remains. `required` and `optional` are **prohibited** (explicit
  presence is the default; `LEGACY_REQUIRED` is spelled as a feature).
- `group` syntax removed entirely (delimited via feature; the *message* is declared normally).
- `reserved` takes bare identifiers (`reserved foo, bar;`), not strings.
- `[default = …]` remains (proto2-style), legal only where presence is explicit (verify the exact
  rule against protoc when building the parser: expected to be an error under IMPLICIT presence).
- `[packed = …]` field option prohibited — feature instead.
- `extend` is fully supported (editions is closer to proto2 than proto3 here).
- Features options at every scope per each feature's declared `targets` (protoc errors on a
  feature applied off-target — we should too).

**Edition 2024** additions/removals:

- `import option "x.proto";` — imports a file *only* for its custom options; recorded in the new
  `FileDescriptorProto.option_dependency` (field 15), excluded from `dependency`.
- `import weak` removed.
- `export message Foo` / `local message Bar` visibility modifiers (also on enums).
- `ctype` and `java_multiple_files` options removed (features replace them) — only matters to us
  as "accept in ≤2023, reject in 2024" validation.
- Naming style and symbol visibility defaults flip on (table above).

## Work map (where each piece lands)

| area | work |
| --- | --- |
| **Descriptor DTOs** (`Descriptor.cs` + `google/protobuf/descriptor.proto`) | `Edition` enum; `FileDescriptorProto.edition`(14) + `option_dependency`(15); `FeatureSet` + `FeatureSetDefaults`; `features` field on all nine `*Options`; `FieldOptions.targets`(19)/`edition_defaults`(20)/`feature_support`(22); `SymbolVisibility` + `visibility` fields; assorted upstream drift since 3.21.x. Bootstrapping: hand-extend first, regenerate via our own codegen once round-trip works. |
| **Parser** (`Parsers.cs`, `Token.cs`) | `edition` statement; label prohibitions; reserved-identifier form; features option parsing (it's mostly ordinary option syntax onto known messages); feature resolution engine + per-edition defaults; target/support-window validation; 2024 import/visibility/naming-style; migrate `ctx.Syntax` checks onto resolved features via legacy-inference (proto2/proto3 as legacy editions). |
| **Codegen** (`CSharpCodeGenerator.cs`, `VBCodeGenerator.cs`, `CodeGenerator.cs`) | consume helper predicates instead of syntax: `TrackFieldPresence` (`CodeGenerator.cs:394-398`), `IsPackedField(syntax)` (`:494`), proto2 enum-default branch (`CSharpCodeGenerator.cs:428`). `TYPE_GROUP`/`LABEL_REQUIRED` paths already correct. |
| **Schema-writer** (stretch) | emit `edition = …`; write features as diffs against resolved defaults; delimited ⇒ feature not group syntax. |
| **Corpus / protoc** | **done: bundled protoc bumped 3.21.12 → 35.1** (edition 2024 needs ≥ v32). The churn was three protoc-behaviour quirks, all fixed in `Parsers.cs` to match modern protoc: (1) options extension fields now sorted by field number at *every* depth, root included; (2) packable repeated option values written packed even when unary (enum case, e.g. `google.api.field_behavior`); (3) large round integer float/double defaults rendered in exponent form (`2e+08`, not `200000000`). Still to do: refresh `google/*` corpus protos — blocked on parser capability (latest descriptor.proto does not parse yet, see #1211); add editions schemas incl. upstream `edition_unittest.proto`. |
| **Runtime library** | **no changes expected.** |

## Tracking issues

- [#1231](https://github.com/protobuf-net/protobuf-net/issues/1231) — editions support (the umbrella ask; this work).
- [#1211](https://github.com/protobuf-net/protobuf-net/issues/1211) — bundled descriptor.proto badly outdated; parser fails on the modern one ("annotations on
  extensions" = the `declaration`/`verification` machinery on extension ranges). Resolved by the
  descriptor-DTO + parser steps; also confirms the `google/*` corpus refresh must come *after*
  parser capability, not with the protoc bump.
- [#594](https://github.com/protobuf-net/protobuf-net/issues/594) — `reserved` inside enums rejected by the parser; same code the editions reserved-identifier
  change touches, so fold the fix into the parser step.

## To verify empirically (against the bumped protoc, before relying on them)

- `[default = …]` legality rules under each presence mode, and interaction with oneof.
- Whether editions relaxes proto3's "first enum value must be zero" for closed enums (and what
  protoc actually enforces for open ones).
- Reserved-*string* form in an editions file: warning or error.
- The exact descriptor bytes protoc emits for a delimited field (`TYPE_GROUP` + type name — any
  constraint linking field name to message name, which classic groups had).
- Which naming-style violations are errors vs warnings under STYLE2024.
- How `option_dependency` interacts with `public_dependency` indexing.

## Sources

- [descriptor.proto @ protobuf main](https://github.com/protocolbuffers/protobuf/blob/main/src/google/protobuf/descriptor.proto) (retrieved 2026-08-17; the `FeatureSet`/`edition_defaults` data above is read from source, not docs)
- [Editions overview](https://protobuf.dev/editions/overview/) · [Feature settings](https://protobuf.dev/editions/features/) · [Implementation guide](https://protobuf.dev/editions/implementation/) · [Edition 2023 spec](https://protobuf.dev/reference/protobuf/edition-2023-spec/)
- Edition 2024 GA'd in [protobuf v32](https://github.com/protocolbuffers/protobuf/releases); Edition 2026 [announced 2026-07-13](https://protobuf.dev/news/)
