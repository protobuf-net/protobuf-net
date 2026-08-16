# A `.proto` schema as an AOT model, in one project

**Status: design, with the mechanism AND the plan-building half proved by spike.**
`SchemaSourcedModelProbeTests` establishes the mechanism; `SchemaSourcedModelSpikeTests` plus
`Internal/SchemaPlanBuilder.cs` take a real schema all the way to a model that compiles against
the generator-emitted DTOs. Nothing is wired into the generator yet, and no public API has been
added - the switch below is still a recommendation, not a decision.

## The problem

A `.proto`-first consumer cannot get the AOT generator's performance without **two projects and
two builds**. `ProtoFileGenerator` turns schemas into DTOs; `ProtoModelGenerator` turns
`[ProtoSerializable(typeof(Foo))]` seeds into serializers - and source generators all run against
the same input compilation and never see each other's output, so a `typeof` naming a generated
DTO resolves to an **error symbol**. `PBN3002` recognises `TypeKind.Error` and says so; the
workaround is `src/AotSchemaDtos` plus a separate consumer, which is fine for this repo's own
smoke test and unreasonable to ask of a consumer.

That is a shame, because **nothing in the AOT model actually needs the C# type**. The plan types
in `Internal/Aot` are hand-written equatable values holding strings, bools and enums - they are
*required* to hold no Roslyn reference at all (`ProtoModelPlanShapeTests` enforces it, because a
symbol in a cached model both defeats the incremental cache and pins the compilation alive). So a
plan can be built from anything that knows the same facts, and a `.proto` schema knows all of
them - arguably better, since it is the wire contract in the first place.

## The mechanism, probed rather than assumed

`SchemaSourcedModelProbeTests` establishes all three halves of it against a real generator driver:

| | |
| --- | --- |
| a generator **cannot see** another generator's emitted type | `GetTypeByMetadataName("Probe.Thing")` is null inside the second generator - the obstacle is real, and is asserted so it cannot later be confused with "nobody tried" |
| emitted code **may name** what it cannot see | a second generator emits `new global::Probe.Thing { Id = 42, Name = "probe" }` for a DTO that does not exist when it runs; the final compilation has **zero errors** and both types bind |
| the names are **derivable, not guessable** | the DTO's members really are `Id`/`Name`, and `NameNormalizer.Default.GetName(FieldDescriptorProto)` returns exactly those - the same call the DTO generator makes, on the same descriptor object |

The third row is the one that makes this a design rather than a hope, and it rests on a fact
about the build that is easy to miss: **`protobuf-net.BuildTools` compiles protobuf-net.Reflection's
sources in** (`<Compile Include="../protobuf-net.Reflection/**/*.cs" />`), exactly as it does
Core's. The parser, `FileDescriptorSet`, `NameNormalizer` and `CSharpCodeGenerator` are all
already inside the generator assembly. A schema front-end does not *reimplement* protogen's naming
- it calls it.

So the shape is: both generators consume the same `.proto` additional files, independently; one
emits DTOs, the other emits a model that names them; the compiler joins the two at the end.
Neither needs the other's output.

## The question this turns on: how does a consumer ask for it?

`typeof` is out, since the type does not exist yet. Three candidates were considered.

### A. An MSBuild property

`<ProtoBufAotModel>true</ProtoBufAotModel>`, surfaced as `build_property.ProtoBufAotModel` -
the same route `ProtoBufDisableBuildTools` already uses (`Literals.DisableProperty`).

Cheap, and reaches consumers who never write C# for their DTOs at all. But it is all-or-nothing
per project, has nowhere to put a model *name*, and cannot express "these three schemas in one
model, that one in another".

### B. Per-file metadata on the `AdditionalFiles` item

`<AdditionalFiles Include="shop.proto" ProtoBufAotModel="ShopModel" />`. The plumbing exists -
`ProtoFileGenerator` already reads `ImportPaths` and `IncludeInOutput` this way, through
`Literals.AdditionalFileMetadataPrefix`.

Expressive, but it puts the model's identity in the project file, where nothing else about the
model lives, and where no analyzer, code fix or IDE navigation can see it.

### C. A seed attribute naming the schema — **recommended**

```csharp
[ProtoModel]
[ProtoSchema("shop.proto")]
public partial class ShopModel : TypeModel { }
```

Exactly parallel to the seeding that already exists: `[ProtoSerializable(typeof(Foo))]` says
"seed this model from that type", `[ProtoSchema("shop.proto")]` says "seed this model from that
schema". Reasons to prefer it:

- **The model type must exist in source anyway.** It is a `partial class : TypeModel` the
  consumer writes; that is the natural home for the switch, and it keeps the whole `Model.Instance`
  / migration-analyzer / code-fix story unchanged.
- **It costs nothing when absent.** The generator's trigger is already
  `ForAttributeWithMetadataName` on `[ProtoModel]`, which fires only for a type carrying it -
  and `AdditionalTextsProvider` is a separate, cached input.
- **A string here is not the `[ProtoInclude(tag, "TypeName")]` smell**, and the distinction is
  worth stating because that one is *refused* on principle. That resolves a **type** at
  **runtime**, by name, which AOT cannot do. This resolves a **file** at **compile time**,
  against the additional-files list, and a miss is a diagnostic pointing at the attribute.
- **It composes.** A model can carry `[ProtoSchema]` and `[ProtoSerializable(typeof(...))]` and
  `[ProtoSurrogate]` together - schema-derived contracts and hand-written ones in one model,
  which is what a real service usually has.
- **Per-model granularity**, which neither A nor B gives.

C supports A as a shorthand: with the property set and no `[ProtoSchema]` anywhere, seed every
schema in the project into the single model that carries `[ProtoModel]`. That covers the
"I just want it on" case without giving up the expressive form.

#### Resolving the path (Marc, 2026-08-14)

The string has to behave like a path, not like a name: **`foo/bar/blap.proto` and `x/blap.proto`
are different files and must stay distinguishable, while `/` and `\` are the same separator and
must not be.** `SchemaFileMatcher` implements exactly that, and `SchemaFileMatcherTests` pins it:

- **separators are interchangeable**, including mixed within one string, and a leading `./` is
  ignored;
- **matching is on whole path SEGMENTS from the right**, never on raw substrings - so
  `bar/blap.proto` matches `…/foo/bar/blap.proto`, and `ar/blap.proto` matches nothing. This is
  the difference between a resolver and an `EndsWith`, and it is what keeps sibling directories
  apart;
- **a bare leaf is allowed while it is unambiguous** - the common case is one `shop.proto` and
  writing the directory would be noise. Where two additional files share a leaf, it is an
  **error naming both**, never a silent pick;
- **an exact whole-path match wins outright**, so a consumer who writes the full path is never
  told it is ambiguous with a deeper file it happens to be a suffix of;
- **case is folded**, since a consumer on Windows will write whichever casing they remember.

Note the resolution is against the compilation's additional files, which is also what makes the
failure diagnosable: a miss can list what *is* available, which a `typeof` of a not-yet-generated
type never could.

## What the plan builder needs, and where each fact comes from

The schema supplies the wire facts outright, and they are the ones that matter: field number,
type, `repeated`/`map`, `optional`/`required`, packedness, enums, nested messages, oneofs,
defaults, groups, extensions.

What it does **not** supply is the *emitted C# shape*, and that is the real work - because the
plan describes the C# the serializer will talk to:

| plan fact | source |
| --- | --- |
| `TypeName`, member names | `NameNormalizer` - the same instance/settings the DTO generator used |
| `ProtoExtensibleKind` | protogen emits `IExtensible` on messages; a codegen convention, not a schema fact |
| `Specified` / `ShouldSerialize` conditionals | protogen's syntax-dependent choice for `optional` |
| getter-only collections, backing fields | protogen emits `List<T>` with a getter only - so the plan needs the accessor route it already has for that shape |
| construction | protogen always emits a public parameterless constructor |
| `IsSealed`, `IsValueType`, tuples, surrogates | not reachable from a schema at all - and not needed, since protogen never emits them |

Note the direction of that list: the schema path is **narrower and more predictable** than the
symbol path, because we control both ends. There is no `[UnsafeAccessor]` guesswork, no
auto-tuple detection, no `extern alias`. The risk is not complexity, it is **drift** - if the DTO
codegen changes a name or a conditional and the plan builder does not, the consumer gets a build
break in code neither of them wrote.

## Where the projection may live — settled (Marc, 2026-08-14)

The obvious spelling is `descriptorSet.ToAotCodegenModel()`, and it is **not available**. The
projection has to be a static (or extension) local to the generator project, which is what
`SchemaPlanBuilder` is. Four reasons, all checked rather than assumed:

- **`FileDescriptorSet` is shipped public API** of protobuf-net.Reflection
  (`PublicAPI.Shipped.txt`), so anything hung off it is a compatibility surface forever;
- **`Descriptor.cs` is auto-generated** from `descriptor.proto` and says so in its header - direct
  changes are overwritten, so it would have to be a partial anyway;
- **BuildTools compiles Reflection's sources in**, so a method added there exists in *both*
  assemblies - including the shipped one, where it means nothing;
- and decisively: **the plan types are `internal` to BuildTools**. An instance method on the
  shared model could not name `ProtoModelPlan` as its return type without making the whole plan
  surface public, in a shipped library, for the benefit of one caller.

So the direction of the dependency is fixed: the generator knows about the descriptor model, and
the descriptor model knows nothing about the generator.

## The one design decision that matters: don't predict, share

Two ways to keep the two halves in step:

1. **Shared calls** - the plan builder calls `NameNormalizer` and mirrors protogen's conventions
   at each decision point. Simple, and what the probe demonstrates; but every convention is a
   separate opportunity to drift, and nothing forces them to move together.
2. **A shared intermediate model** - the schema is projected once into a codegen model that
   records the C# actually being emitted (names, nullability, conditionals), and *both* the DTO
   writer and the plan builder consume it. Drift becomes impossible rather than merely tested for.

(2) is the better shape, and it has history here: commit `c6c2a42a` (Sep 2022) built exactly that
- `ProtoBuf.CodeGen`'s `CodeGenSet.Parse(FileDescriptorSet, CodeGenContext)` walking into
`CodeGenFile`/`CodeGenMessage`/`CodeGenField`/`CodeGenEnum`, with golden tests serialising the
model to JSON. It never merged and is not in the tree today, but it is the right ancestor to read
first. This is the `ToAotCodegenModel()` idea, and the name says the whole thing: the codegen
model already knows what it is about to emit, so let it hand that to the AOT plan builder.

Starting with (1) and moving to (2) is defensible - the probe shows (1) works - but the gate below
has to exist either way, and (2) is what makes the gate mostly redundant.

## The gate

The comparison that matters is the one the rest of this arc uses: **bytes against
`RuntimeTypeModel`**. For each schema, compile the generated DTOs plus the generated model, and
check that serializing a populated instance agrees with the runtime model over the same DTOs, in
both directions.

Most of that harness exists. `src/AotDifferential`'s `Schemas.cs` already parses the schema tree
under `protobuf-net.Reflection.Test/Schemas`, runs it through `CSharpCodeGenerator` and compiles
the DTOs in-process into a `SchemaCorpus` assembly - which is how the corpus differential got its
machine-generated half. The new leg adds the generated *model* to that compilation and compares.
Note what that half already found when it was first turned on: a member named after a C# keyword
that broke the consumer's build (item 14 of `docs/aot-findings.md`). Machine-generated contracts
are a different distribution, and this path is entirely machine-generated.

A second, cheaper gate: the emitted model must *compile* against the emitted DTOs, which the
probe test's shape already demonstrates and which a golden fixture would pin per schema.

## Locations, and why they are not a problem

`PlanLocation` deliberately stores Roslyn *value* types (`TextSpan`, `LinePositionSpan`) and
reconstitutes a `Location` only at report time - so a schema-sourced plan can carry a location
into the `.proto` file rather than into C#. `ProtoFileGenerator` already does exactly this for
parse errors: it turns the parser's `LineNumber`/`ColumnNumber` into a `LinePositionSpan` and
calls `Location.Create(error.File, default, span)`. The same route serves plan diagnostics.

And the deeper point stands: **most such diagnostics should not arise**. The generator drops a
contract it cannot handle - but on this path we also *emit* the contract, so a shape the plan
cannot serialize is a shape protogen should not have emitted. Where the two genuinely disagree,
pointing at the `.proto` line is the most useful thing that could happen.

## What the spike established (2026-08-13)

`SchemaPlanBuilder` + `SchemaSourcedModelSpikeTests` run the whole loop for a representative
proto3 schema - scalars of every width, `bytes`, an enum member, and a nested message reference:

1. `ProtoFileGenerator` emits the DTOs, exactly as a consumer's build would;
2. the same schema is parsed again and projected into `ProtoContractPlan`s;
3. `ProtoModelGenerator.Emit` turns them into a model;
4. DTOs + model + the consumer's `partial class : TypeModel` compile together, **with zero
   errors**, and the model really does carry `ISerializer<global::Spike.Thing>` and
   `ISerializer<global::Spike.Inner>`.

So the direction works, and the remaining work is breadth rather than risk.

**The conventions the plan must match are now concrete rather than hypothetical.** Reading
protogen's actual output for that schema, each of these is a decision the plan builder has to
mirror, and each is marked `CONVENTION` in the source.

**To be unambiguous, since an earlier draft of this section read otherwise: none of these is a
scenario that fails today.** The existing two-project route handles all of them - the corpus
differential's machine-generated half covers 63 map-using schemas and every schema's proto3
strings, at 100% byte match. What follows is a list of ways the NEW front-end could be written
wrongly, i.e. the work, not a defect report.

- the package becomes the namespace via `NameNormalizer` (`spike` -> `Spike`);
- **every** message is emitted `: IExtensible` with a private `__pbn__extensionData` field, so
  the plan is `ProtoExtensibleKind.Untyped` throughout - not `None`;
- a proto3 **string** member is emitted with `[DefaultValue("")]` *and* an `= ""` initialiser,
  which moves the write guard from `!= null` to `!= ""` - so an empty string is **not written**,
  which is what proto3 requires. The hazard is one-directional and specific: if the front-end
  omits the declared default from the plan, the generated serializer writes two bytes where
  ref-emit over the very same DTO writes none. The DTO is unaffected either way, so it compiles
  cleanly and disagrees only on the wire. This is the sharpest item here for that reason, and the
  spike sets `defaultLiteral` for exactly this;
- `repeated` scalars become `T[]` with `IsPacked = true`, while `repeated` messages/strings
  become a **getter-only** `List<T>` with an initialiser - two different shapes from one schema
  construct, and the getter-only one needs the plan's existing accessor route;
- a `map<k,v>` is not a distinct feature at descriptor level: it compiles to a **synthetic nested
  entry message** (`options.map_entry = true`) plus a `repeated` field of it. Two consequences,
  and neither is a limitation of anything shipped - they are instructions for building the
  front-end:
  1. **map support is gated on nested-type support**, since the entry message is a nested type -
     so the two cannot be sequenced independently, which the scope test pins;
  2. when nested types do land, the front-end must **skip** the entry messages rather than walk
     them. protogen emits a `Dictionary<K,V>` property and **no C# type** for the entry, so a
     front-end that treated it as an ordinary nested message would emit a contract for a type
     that does not exist. That one *is* caught by the compile gate.

**The drift gate is real, and was verified able to fail** by perturbing the namespace convention
in the builder: the spike test goes red with `CS0234`/`CS0246` rather than passing quietly. But
note its limit, which is exactly the string-default case above - **a compile gate catches wrong
NAMES and cannot catch wrong GUARDS.** That is the argument for putting the byte differential in
early (step 3 below) rather than treating it as the finishing touch.

## What works, and what does not

**The gap list has moved to `notes/gaps.md`** (section C), which is the single place every
outstanding item across all three arcs is tracked. Everything under "works" there is verified
ON BYTES against `RuntimeTypeModel` in both directions, not merely "it compiles".

What stays in this file is the design and the findings - why the mechanism works, what probing
it turned up, and which conventions the front-end has to match. Those do not belong in a list
of what is outstanding, and they are what this document is for.

### Pointed at `descriptor.proto`, and what it found (2026-08-14)

`descriptor.proto` is the best test available: the largest real schema in the tree, and the only
one whose symbol-derived model is **checked in** — so the two routes can be diffed directly.
`SchemaSourcedDescriptorProbeTests` builds the plan from the embedded schema and dumps the
emitted source (opt-in via `PBN_SCHEMA_DUMP`) for comparison against
`src/protobuf-net.Reflection/Generated/…CustomProtogenSerializer.ProtoModel.g.cs`.

**It built all 27 contracts with no refusal** — nested types, nested enums, maps, repeated,
`oneof`-free, the lot — which is a much stronger result than the hand-written conformance schema
gives. Comparing the emitted `WriteRawTag` sequence per contract:

| | |
| --- | --- |
| first run | **17 of 21** shared contracts identical |
| after the ordering fix | **20 of 21** identical |
| remaining difference | one field, and it is not ours |

**The bug it found: member ORDER.** protobuf-net writes members in field-number order; a schema
may declare them in any order at all, and the front-end was emitting in declaration order. That
is a straight byte disagreement for any schema written that way — `DescriptorProto` declares
field 6 before field 3, `FieldDescriptorProto` declares 3 before 2. The hand-written conformance
schema had missed it completely by declaring everything ascending, which is exactly the blind
spot a hand-written fixture has. Fixed by sorting, and now pinned by a deliberately shuffled
message in `conformance.proto`.

**The remaining difference is a finding about the repo, not the generator.** `FieldOptions` field
15 (`unverified_lazy`) is present in the embedded `descriptor.proto` and absent from the
checked-in `Descriptor.cs` — the generated DTOs have **drifted behind their own schema**, because
protogen has not been re-run since that field was added upstream. Worth knowing on its own, and
worth noting as an argument for this whole direction: a single-pass model generated from the
schema cannot drift from it.

### The gap that is not a feature

**The corpus.** `conformance.proto` is hand-written and deliberately small. The
`protobuf-net.Reflection.Test/Schemas` tree — the one `AotDifferential` already compiles
in-process, 63 of whose schemas use maps — has **never been run through the schema-sourced
path**. Pointing it there is the single highest-value remaining item: it is a different
distribution (machine-generated, wide, full of shapes nobody writes by hand), and the last time
that corpus was turned on against a new path it found a consumer-build-breaking bug on the first
run (item 14 of `docs/aot-findings.md`).

Doing that *before* items 1-9 would also answer "which gaps actually occur", rather than working
through the list in the order I happened to think of it.

## Suggested order

**Moved to `notes/gaps.md`**, section C, where the items are ranked alongside everything else
outstanding. Keeping an order here as well would be a second copy of the same decisions.
