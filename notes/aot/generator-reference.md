# The AOT generator, feature by feature

**This is the reference for "what does the generator emit for shape X, and why that shape".** It was
carved out of `AGENTS.md` on 2026-08-25 because that file is loaded into *every* session's context
and had grown to ~2,500 lines, most of it this material. `AGENTS.md` keeps what a session needs
before it knows what it is doing — conventions, traps, the gate battery, the diagnostic-id owner
table — and points here for the rest.

**The content below is verbatim**, moved rather than rewritten, precisely so that nothing was quietly
lost or "improved" in transit.

Two things to know about how to read it:

- **almost every claim here was PROBED against ref-emit**, not reasoned about, and where a section
  says so it is worth believing over your own expectation — several of these facts are the opposite
  of the obvious guess (callbacks fire per *root* while members are written per *layer*; a repeated
  enum *is* packed; `[DefaultValue("green")]` on an enum parses by name);
- **a refusal here is usually a MATCH**, not a shortfall. `AGENTS.md`'s "Telling our gaps from
  protobuf-net's" is the section that explains how to read one, and it deliberately stayed there.

For what is genuinely missing, `notes/gaps.md` remains the entry point.

---

### Collections

Arrays, `List<T>`, the collection interfaces, sets/queues/stacks, and the immutable and concurrent
families, of scalars, messages and enums.

Which `RepeatedSerializer` factory serves which collection is **not a lookup table** and cannot be
one: `ResolveRepeated` is a port of `RepeatedSerializers.TryGetRepeatedProvider`, walking the
base-type chain and then the interfaces against a priority-ordered provider table. Three parts of
that algorithm are load-bearing, and a table keyed on the declared type gets all three wrong:

- **Order is priority**, lowest wins. The immutable family is registered *ahead* of the mutable
  lookalikes precisely so it wins on types implementing both.
- Most entries are **exact-only**: they apply to the member's own type but not to anything deriving
  from or implementing it. This is why `SortedSet<T>` gets `CreateEnumerable`, not `CreateSet` —
  the `ISet<T>` registration is exact-only, so it does not apply through an interface — and why
  `class MySet : HashSet<int>` also gets `CreateEnumerable`, while `class MyQueue : Queue<int>`
  keeps `CreateQueue` (the `Queue<T>` registration is not exact-only).
- Two matches at the **same** priority resolving differently — `IEnumerable<int>` *and*
  `IEnumerable<string>` on one type — is treated as **no match at all**, leaving an ordinary
  message. `Derived.input.cs` pins all four cases against ref-emit.

`List<T>` alone gets `CreateList<T>()`; anything derived from it needs `CreateList<TRoot, T>()`.
More generally the factories come in two shapes: `Create{X}<TCollection, TElement>()` needs the
member's declared type, while `Create{X}<TElement>()` has it fixed by the factory (arrays,
`List<T>`, the immutable family). `ImmutableArray<T>` is a **struct**, so neither side null-tests
it. Read uses the same merge shape as sub-messages (existing collection passed in, result assigned
back only when non-null). Facts confirmed against ref-emit rather than assumed:

- **Packing is a compile-time decision, and `IsPacked` IS honoured** — this previously said the
  argument "is not supported yet, so we always emit the disabled form", which was **stale**. The
  features constant carries `OptionPackedDisabled` by default and both ref-emit and the generator
  *omit* it for `[ProtoMember(IsPacked = true)]`; `ListOptions.input.cs` pins five such members
  against `ListOptions.reference.cs`.

  Two things worth knowing before treating packing as a correctness matter. **Whether to pack is
  the writer's free choice** — protobuf requires a *reader* to accept both forms, so declining to
  pack is never a wire bug. And **protobuf-net packs only when it can cheaply size the elements**:
  `RepeatedSerializer.Write` takes the packed branch only when the element serializer is an
  `IMeasuringSerializer<T>`. **A repeated enum IS packed** — this file claimed the opposite for a
  long time, on the grounds that `EnumSerializer<TEnum>` is not an `IMeasuringSerializer`. That is
  true only of the *public abstract* base: the concrete `EnumSerializer<TEnum, TRaw>` implements
  `IMeasuringSerializer<TEnum>`, and the branch tests the instance. `PackedBlockCopyTests`
  `.PackedEnumsAreActuallyPacked` pins the bytes. `notes/gaps.md` B1 retracted this on 2026-08-15
  and this file did not catch up until 2026-08-22, by which point the stale claim had been repeated
  in a fresh piece of work — so treat a "never" here as worth re-checking against B1.
- The features wire type is the **element's**, not the member's.
- A message element passes `this` as the sub-serializer; a scalar element passes nothing.
- **A repeated enum needs a serializer proxy.** Unlike an inline scalar, `RepeatedSerializer`
  resolves an `ISerializer<TEnum>` *from the model* — ref-emit emits `values, this as
  ISerializer<TEnum>` — so the services type implements `ISerializerProxy<TEnum>` (and `<TEnum?>`)
  returning `EnumSerializer.CreateXxx<TEnum>()`, per `EmitEnumProxies`. Without it the failure is a
  runtime "no serializer for type", not a build error. **A map with an enum key or value works the
  same way**, and needed exactly what that sentence predicted: the proxy scan pointed at the map plan,
  with the enum's name carried on `ProtoMapPlan` because `KeyKind`/`ValueKind` hold the *underlying*
  scalar. The serializer argument stays absent on that side — passing nothing lands on
  `serializer ??= TypeModel.GetSerializer<T>(Model)`, which finds the proxy. It was blocking 16
  contracts in the corpus, so it was worth considerably more than it looked.
- protobuf-net **rejects null elements** inside a collection (`ThrowNullRepeatedContents`), so
  fixtures must not contain them.
- **A list-like `[ProtoContract]` is refused.** The same resolution decides whether a *contract* is
  a collection; if it is, protobuf-net serializes it as one and ignores its members entirely.
  Emitting a message there would silently disagree on the wire, so the contract is dropped (and
  anything referencing it cascades). `[ProtoContract(IgnoreListHandling = true)]` is the documented
  opt-out and makes it an ordinary message — that is exactly what the runtime honours, in
  `RuntimeTypeModel.TryGetRepeatedProvider`. There is no "has a public `Add`" or "has a `GetEnumerator`" heuristic in the **repeated-resolution** path - that is all `TryGetRepeatedProvider`. `ResolveUniqueEnumerableT` is the old heuristic and is `[Obsolete]`, but it is **not** unused (this file previously said it was): `TypeModel.CanSerialize` and the auxiliary-type flow both still call it, which is why it appears in the native-AOT warning list. It does not affect how a *member* resolves, which is all the generator cares about.
- **Maps are repeated too**, resolving to a `MapSerializer` — see below.
- `Span<T>`, `Memory<T>`, `ArraySegment<T>` and friends resolve to a serializer that *throws* at
  runtime; refused up front. `byte[]`, `Memory<byte>`, `ReadOnlyMemory<byte>` and
  `ArraySegment<byte>` are "bytes", not collections — and that test has to run **before** the
  auto-tuple test, not after: `ArraySegment<byte>` satisfies the tuple predicate exactly, with a
  `(T[], int, int)` constructor and matching read-only `Array`/`Offset`/`Count`, so it was going out
  as a three-member message. Three of the four are **structs**, so neither side null-tests them;
  for `Memory<byte>` that is not a nicety, since `!= null` does not compile against it.
- `IProducerConsumerCollection<T>` resolves to a provider, but **reading** one needs a concrete type
  to construct, so ref-emit throws on deserialize. There is nothing to compare against, so it has no
  fixture.
- `IReadOnlySet<T>` maps to `CreateReadOnlySet`, which only exists in the net6.0+ build of the
  library; the generator checks the symbol is present before emitting a call to it, and falls back
  to the 3.x spelling `CreateReadOnySet` (sic - now an `[Obsolete]` forwarder, kept because
  previously-generated code binds to it by name) when only an older Core is referenced.

### Maps

Dictionaries resolve through the same provider walk and land on a `MapSerializer` factory, with the
same two factory shapes and the same merge shape on read. `SortedDictionary<K,V>` is not in the
table at all — it matches through `IDictionary<K,V>`, so it gets `CreateDictionary<TRoot, K, V>()`.

The map's own features are `WireTypeString | OptionPackedDisabled`; `IsPacked` and `OverwriteList`
compose exactly as for a repeated member. Three things are specific to maps, all confirmed against
ref-emit:

- The key and value wire types are passed **as separate arguments**, after the collection.
- `OptionFailOnDuplicateKey` is added when the shape is **not a valid protobuf map**
  (`RepeatedSerializerStub.IsValidProtobufMap`): the key must be an integral, string or enum type —
  `bool`, `char` and the floating-point types are *not* in that list — and the value must not itself
  be repeated. It changes reading from `SetValues` (overwrite) to `AddRange`, which **throws** on a
  repeated key. That is why `MapKey.input.cs`'s samples use disjoint keys per field: the differential
  suite manufactures repeated fields by concatenating payloads.
- A message key or value is passed `this`, **positionally**: `, this` for a key alone, `, null, this`
  for a value alone, `, this, this` for both.

The member's own `DataFormat` selects only the root wire type, so `Group` is the one value that
changes anything there and `FixedSize`/`ZigZag` are silently ignored. The **per-key and per-value**
formats come from `[ProtoMap]`, and land on the two wire-type arguments rather than on the features:

- the format only bites where there is something to select. A **`string` key ignores `FixedSize`**
  entirely, and `Group` is meaningful only on a message value.
- the `FixedSize` **width comes from the element type**, exactly as for a scalar member —
  `Dictionary<int, int>` gives `Fixed32`, `Dictionary<long, long>` gives `Fixed64`.
- **`DisableMap = true` lands on `OptionFailOnDuplicateKey`**, the same flag an invalid map shape
  already gets, since reading switches from `SetValues` to `AddRange`.
- `KeyFormat`/`ValueFormat` are read **only when `DisableMap` is not set** — `MetaType` takes the
  `else` branch — so the two do not compose.
- ...and they are then **applied only if the shape is a valid protobuf map**: `MetaType` assigns
  `MapKeyFormat`/`MapValueFormat` inside `if (mapEnabled && IsValidProtobufMap(…))`, so a
  `Dictionary<DateTime, DateTime>` discards both formats however they were spelled and falls back to
  the level-200 form. Note the ordering — validity is decided *using* the declared key format, and
  only then are the formats kept or dropped.
- `[ProtoMap]` on a non-dictionary member is refused: protobuf-net reads it only for a member that
  resolved as repeated, so anywhere else it is silently inert.

**Convert the format with `GetDataFormat`, never a cast.** `DataFormat` and `ProtoDataFormat` do not
share ordinals — `DataFormat.FixedSize` is 3, which is `ProtoDataFormat.Group` — so a cast compiles,
silently mis-maps, and produces a map that disagrees with ref-emit on the wire. This was caught by
diffing against `MapFormat.reference.cs`, where `ZigZag` (ordinal 1 in both) worked and everything
else did not, which is exactly the shape of bug a partial test would miss.

An **enum** on either side **works**, through the same `ISerializerProxy` route a repeated enum
takes — this file claimed it was "refused" until 2026-08-22, contradicting its own account of that
support in "Collections" above. `MapMeasure.input.cs` carries an enum key and an enum value, and
both round-trip against ref-emit. What *was* missing until then is the arithmetic measure, and note
the rule it needed, because it is the opposite of the scalar one: **an enum map side is written even
when zero** (`{1:None}` is `0A-04-08-01-10-00`), since `EnumSerializer` supplies no `IValueChecker`
and the "non-null is non-trivial" default applies.

A **repeated value is supported**, and is the one place nesting is legal at all:
`TestIfNestedNotSupported` exempts maps, so `Dictionary<int, List<int>>` works where
`List<List<int>>` throws. Ref-emit passes `this as ISerializer<List<int>>` — it resolves one *from
the model* — so we emit `ISerializerProxy<List<int>>` returning the same
`RepeatedSerializer.CreateList<int>()` and pass nothing, exactly as for a repeated enum. The value
wire type is the **element's** (`WireTypeVarint` for `List<int>`), and such a shape is never a valid
protobuf map, so it also picks up `OptionFailOnDuplicateKey`.

**The element may be a message, and `ProtoMapPlan`'s `ValueKind` then describes *that element* while
`ValueTypeName` describes the collection.** The two are a matched pair everywhere else, so reading
them as one is the trap here, and it cost #1337 twice over: the emitter paired them and passed
`this` — an `ISerializer<Leaf>` where an `ISerializer<List<Leaf>>` is wanted, i.e. `CS1503` in the
consumer's build — and `DropUnsatisfiable` paired them, looked for a contract named `List<Leaf>`,
found none, and removed **every** contract with such a member before the emit bug could show. Both
now key off `ValueSerializerFactory`, which is set exactly when the value resolves its serializer
from the model, and `ValueElementTypeName` carries the element's name so the cascade still has a
real pair to test. `RawMapMeasurable` had found the same trap from the other side and guards with
the same marker, so the two are consistent.

Note ref-emit writes `this as ISerializer<List<Leaf>>` here, which is **null at run time** — the
services type implements `ISerializer<KeyValuePair<int, Leaf>>` — so both paths land on
`serializer ??= GetSerializer<T>(Model)` and find the proxy. That is why passing nothing agrees on
the wire, and it is also why the **fully compiled** ref-emit path *fails* on a message element:
resolution falls back to a model with no entry, and it throws *"No serializer for type
`List<Tuple<double,String>>`"*. A **scalar** element survives that route (a `List<int>` serializer
needs no model entry), which is why `Issue54` passes and `Issue1337` pins the throw. So we match the
reflection path and exceed the compiled one, exactly as for a nested map value.

A nested **key** is still refused. That one is a limit of the plan rather than of protobuf-net's
reflection path — but note it *does* match the compiled path, which is the more interesting half:

**A compiled model throws on a map whose key or value is a collection — unless that collection's
serializer can be built with no model entry at all.** `Compile(name, path)` succeeds and emits the
member, then the first use throws *"No serializer for type `Dictionary<string,String>` is available
for model X"* — the emitted code passes `this as ISerializer<Dictionary<string,string>>` and the
services type implements `ISerializer<KeyValuePair<string,string>>`, so the cast is null and
resolution falls back to a model with no entry. The escape is a collection of **scalars**, whose
serializer resolution needs nothing from the model: `Dictionary<float, List<int>>` survives the
compiled path (`Examples/Issues/Issue54`) while `Dictionary<double, List<Tuple<double,string>>>`
throws (`Issue1337`, which pins it). The reflection path handles all of them. So our repeated and nested map **values**
match reflection and *exceed* the compiled path, and our refused nested **key** matches the compiled
path and falls short of reflection. Item 9 in `notes/aot/findings.md`.

That distinction is only visible if you **run** the compiled model. `AotRefGen` compiles and
decompiles but never executes, so `*.reference.cs` shows the member emitted and says nothing about
whether it works — which is how this was first mis-recorded as protobuf-net dropping the member
silently, and then mis-corrected as protobuf-net handling it. Emitted is not working.

Collection options are pure features composition, and compose orthogonally:
`IsPacked = true` *omits* `OptionPackedDisabled`; `OverwriteList = true` *adds*
`OptionClearCollection`.

On a **non-collection** member they are accepted and ignored, which is what protobuf-net does:
`ComposeListFeatures` is only reached from the repeated and map paths, so neither option has anywhere
to land. **The exception is `OverwriteList` on a "bytes" member** — `byte[]`, `Memory<byte>`,
`ReadOnlyMemory<byte>`, `ArraySegment<byte>` — which is a scalar here but still reaches
`BlobSerializer`'s `overwriteList`: it selects `AppendBytes(default)` over `AppendBytes(existing)`,
i.e. **replace rather than append**, and does not read the current value at all (`RequiresOldValue`
is false with it set). `IsPacked` on a bytes member is ignored like any other scalar. The `default()`
is spelled out because the four `AppendBytes` overloads make a bare `default` ambiguous.

`DataFormat` and `IsRequired` change the emitted *shape*, not just the features:

- **`DataFormat`** selects the wire type. On a scalar that means `WriteFieldHeader(n, WireType.X)` +
  the wire-type-aware `WriteInt32`/`WriteInt64` — **not** the `WriteInt32Varint` shortcut, which
  writes its own varint header and therefore only applies at the default format. `FixedSize` picks
  `Fixed32`/`Fixed64` from the *member's* width. On a repeated member it is only a features swap.
  On a **BCL type** it shifts the field header, and not uniformly — see `BclWireType`, which is a
  probed table rather than a rule: `decimal` ignores the format entirely, `Guid` honours `Group` but
  ignores `FixedSize` below level 300, and `DateTime`/`TimeSpan` honour both. `ZigZag` is refused,
  since it throws while ref-emit builds the model (for `decimal` it is merely ignored, so refusing it
  there is a small deliberate over-reach).

  Note the `BclHelpers.WriteXxx` methods are **wire-type aware**: under a `Fixed64` header
  `WriteDateTime` emits the 8-byte fixed form rather than a message, so `FixedSize` on a
  `DateTime`/`TimeSpan` is a compact encoding and not, as it first looks, a message body mislabelled
  as `Fixed64`. The payload is ordinary, valid protobuf — `09-80-80-85-75-3A-0F-38-00` is a tag plus
  exactly eight bytes.
- **`ZigZag` reads need `state.Hint(WireType.SignedVarint)` before the read**; no other format does.
- **`Group`** differs on the **write only** (`WriteGroup` for `WriteMessage`) for a *scalar*
  sub-message member; its read is an ordinary `ReadMessage`. On a **collection** it is not write-only
  at all: it lands in the element features as `WireTypeStartGroup`, so the element carries group
  markers in both directions. On a **map** the member's `Group` moves the *map's own* features to
  `WireTypeStartGroup` (the group frames each key/value entry), while `[ProtoMap(ValueFormat = Group)]`
  leaves the map length-prefixed and groups only the value. A collection of **scalars** cannot be
  grouped at all — there is no sub-message for the markers to frame, and protobuf-net throws while
  building the model on both paths, with the unhelpful *"Operation is not valid due to the current
  state of the object"*.
- **`TwosComplement` is byte-identical to `Default`** for every type we handle, so it maps onto it.
- **`IsRequired`** drops the write guard so the member is always written. It is only observable for
  value-type scalars — reference types were already unguarded on write — and it does **not** affect
  the read: a required string still keeps its `if (x != null)` on the way back in. The emitter omits
  the test entirely rather than emitting `if (true)`.

`WellKnown` is meaningful only on the compatibility-level BCL types, where it promotes a level-200
member to 240. Anywhere else it has nothing to promote and ref-emit simply ignores it, so we do too.

Note `ListSet` and `RepeatedAsList` are **protogen schema-codegen** options that shape generated DTOs
from `.proto`; they are not `[ProtoMember]` options and have nothing to do with this generator.

### Identifiers: emitting a name is not the same as knowing it

`ISymbol.Name` is the **metadata** name, so a member the consumer declared as `@case` comes back as
`case` and has to be re-escaped before it can appear in generated C# — `Escape` does this, keyed on
`SyntaxFacts.GetKeywordKind`, so *contextual* keywords (`value`, `record`, …) are correctly left
alone since they are already legal identifiers.

The distinction to keep is between the two audiences for a name. Emitted **syntax** takes the escaped
form; anything matched against **metadata** takes the raw one — notably the
`[UnsafeAccessor(Name = "set_case")]` argument, and `Sanitise`, which is building an identifier out
of a type name and replaces every non-alphanumeric anyway.

Type names need no help *with escaping*: Roslyn's `FullyQualifiedFormat` already handles it, which is
why a proto package called `namespace` renders as `global::@namespace.Foo` without our doing anything.

They do need help with **`extern alias`**, which is the one case `global::` cannot express. Two
referenced assemblies may declare the same full type name; C# tells them apart only by an alias, set
as `<Aliases>` metadata on the reference in the *consumer's* project, and `global::` then means "the
un-aliased one". So `Qualified` (`ProtoModelGenerator.Aliases.cs`) replaces `global::` with the alias
for any type whose declaring assembly carries one, per **constituent** type rather than on the leading
prefix — `List<Thing>` renders as `global::…List<global::Ns.Thing>` and it is the inner one that may
need it.

The division of labour is the part worth remembering, and every clause was probed (`ExternAliasTests`):

- a generator **can** emit `extern alias X;` in its own file, and **can** discover which references
  carry aliases (`compilation.GetMetadataReference(assembly).Properties.Aliases`);
- it **cannot create** one — that is the consumer's project file, and nothing else can set it;
- an **unused** `extern alias` is legal, while one naming a reference that does not carry it is
  CS0430 — which is why the emitter declares the compilation's whole set unconditionally and never
  works out which it needs;
- aliasing **one** of a colliding pair is enough, since `global::` then names the other.

When neither is aliased the type cannot be named by *any* C# syntax, so the contract is refused with
`PBN3002` naming both assemblies and the `<Aliases>` fix. That is a limitation of C#, not of the
generator — and refusing beats the alternative, which is CS0433 in a file the consumer never wrote.

**The corpus cannot catch any of this**, which is why it needs its own test: `AotDifferential`
resolves ambiguity by dropping an assembly wholesale, so the generator is never run against an
aliased reference there. Note the realistic shape is an ambiguous **member** type, not a seed — a
consumer cannot seed a type they cannot name, so it arrives via a contract in a third assembly where
the member type was unambiguous.

This is one of the few places where the hand-written corpus was structurally blind — nobody writes
`public int @case` — and the `.proto` half found it immediately.

### Fields

Fields are members exactly as properties are — ref-emit emits the identical guards and read shapes,
so the whole change was in parsing. Auto-property backing fields are `IsImplicitlyDeclared` and so
skipped (the property itself covers them). Two states a property cannot be in are refused:
`readonly` (the same problem `init` has — no assignment after construction) and `const`. `static`
and non-public are refused as for properties, though note ref-emit reaches both by reflection.

### Inheritance (`[ProtoInclude]`)

A hierarchy is a second emit shape, and every type in one implements **both** `ISerializer<T>` and
`ISubTypeSerializer<T>`. All the traffic goes through the **root's** `ISubTypeSerializer`, which
walks down the chain writing each layer's own members and nesting the next inside a sub-type marker,
so `ISerializer<T>` collapses to a pair of one-line delegations — for the root as much as the leaves,
with a cast back on read for anything below it. A layer only ever sees **its own declared members**;
inherited ones belong to the layer that declares them.

- `WriteSubType` dispatches on the runtime type: `if (TypeModel.IsSubType(value))` then an `is` chain
  over the direct `[ProtoInclude]` types, `else ThrowUnexpectedSubtype`. A leaf has no chain and
  falls back to the plain unconditional throw.
- `ReadSubType` hoists `value.Value` **per case** — reading it is what constructs the instance — and
  each sub-type field is `value.ReadSubType<TDerived>(ref state, this)`.
- **`[ProtoInclude]` takes a `DataFormat`**, and `Group` is the only value that reaches the wire -
  a sub-type is a sub-message, so `FixedSize`/`ZigZag` have nothing to select and are ignored. It
  affects the **write only**: `WriteSubType(int, …)` hard-codes `WireType.String`, so the grouped
  form writes `WriteFieldHeader(n, StartGroup)` itself and then calls the overload that takes no
  field number. The read is identical either way, since the framing comes off the header.
- **`sealed` omits `ThrowUnexpectedSubtype` entirely**, in a hierarchy or out of one. This was a
  pre-existing divergence: we emitted it everywhere a struct or tuple did not apply. It is benign
  (the call cannot throw for a sealed type) but it is not what ref-emit produces.
- **Abstract is allowed only as a root** — with no sub-types there would be nothing to construct.
  An abstract root also needs no public parameterless constructor, since nothing ever calls `new` on
  it; `SubTypeState<T>` constructs the layer the payload actually names.
- **Inheritance without the `[ProtoInclude]` link is refused.** protobuf-net treats such a derived
  type as an independent contract that *silently ignores every inherited member*; refusing is the
  safer half of that surprise. `[ProtoInclude(tag, "name")]` is refused too — it resolves the type at
  runtime.
- A hierarchy is **all-or-nothing** in the cascade: one dropped member anywhere takes the whole
  hierarchy, since the root dispatches to each sub-type by name and every type routes back to the root.
  **It is all-or-nothing in the MEASURABLE set for the same reason**, and that is a separate fixed
  point that had to be taught the rule (gap B41): every layer's `Measure_` forwards to the root's
  `MeasureSub_`, and each marker arm calls its sub-type's, so one unmeasurable layer leaves calls to
  bodies that were never emitted. The symptom is a pile of `CS0103` in the **consumer's** build
  rather than a diagnostic here.

**A hierarchy takes the measure-first raw path, since 2026-08-22 (gap B41).** It was excluded, and
the exclusion was never principled — a sub-type marker *is* a nested sub-message, which was already
measured for a member. Two statics per layer, mirroring the two interface methods:

- **`MeasureSub_`** mirrors `WriteSubType` statement for statement, and **`RawWriteSub_`** mirrors
  `MeasureSub_`. Marker order is load-bearing where one sub-type derives from another (`Puppy : Dog`),
  and the three cannot drift because all walk `contract.SubTypes` in its own order.
- **`Measure_` for any layer forwards to the ROOT's `MeasureSub_`**, because `ISerializer<T>.Write`
  routes to the root's `WriteSubType` whatever `T` is — so a member declared as the base measures the
  whole chain. For the same reason a hierarchy target has no `RawWrite_`: `RawWriteEntry` names the
  root's `RawWriteSub_`, keeping the measure and the write both root-based.
- **Only the ROOT may use `Leave`.** The boundary recorded by `IMeasuringSerializer<T>` describes a
  root-based run; a derived layer's own `WriteSubType` is reachable only from the stateful engine and
  its run would be a different, shorter one, so it always measures afresh.
- **A marker-only layer asks `IsSubType` before measuring at all.** A root instance that is not
  sub-typed writes no marker, so the prologue would walk for a length nobody reads; the measure takes
  the same branch on the same object, so the two stay paired. Left unconditional this cost **18% at
  depth 1** — enough to put the generated model behind the classic engine on that shape.
- **Only the ROOT's callbacks fire, in BOTH directions** - probed against ref-emit, and the obvious
  guess is wrong: *members* work per layer, callbacks do not. Serialize fires in `WriteSubType`,
  `RawWriteSub_` and `MeasureSub_` (the measure one is not optional - both passes must observe the
  same object). Deserialize goes through `SubTypeState<T>.OnBeforeDeserialize`, which runs at
  *materialisation* rather than at a fixed point in the field loop, because the hook must see the
  instance already constructed as the sub-type; that is the same API `TypeSerializer` uses. See
  `notes/gaps.md` B46.

The prize was the **cascade** as much as the hierarchy itself: +158 contracts measurable in the
corpus (2499 → 2657), and 2.99× on a depth-16 length-prefixed hierarchy, with the per-layer curve
now *decreasing* rather than superlinear.

#### Interfaces are inheritance roots

An interface contract is **exactly** an inheritance root and needs no new emit shape — the same
`ISubTypeSerializer`, `is` chain and `ReadSubType` a class root gets. Probed rather than assumed:

| shape | ref-emit | us |
| --- | --- | --- |
| `[ProtoContract]` + `[ProtoInclude]`, unary member | works | emitted |
| same, as `List<IAnimal>` | works | emitted |
| same, serialized directly as root | works | emitted |
| an interface deriving another, both contracts | works | emitted |
| a **closed generic** interface (`IBox<int>`) | works | emitted |
| an interface as a map **value** | works | emitted |
| an interface as a map **key** | works | emitted |
| `[ProtoContract]`, **no** `[ProtoInclude]` | **throws** `Unexpected sub-type` on write | refused |
| interface with no attributes, as a member | **throws** `No serializer defined for type` | refused |
| a **value-type** sub-type | **throws** `Unexpected sub-type` | refused |
| one type named by **two** hierarchies | **throws** while building | refused |

Every row was probed against *both* ref-emit paths, and the two paths agree on all of them. The four
refusals *match* ref-emit rather than fall short of it. An interface root is implicitly abstract,
which "abstract is allowed only as a root" already covers; the changes were teaching `DerivesFrom`
and `GetLinkedBase` that **implementing** counts as deriving.

Two of those refusals are newer and worth the detail:

- **A value-type sub-type does not merely misbehave, it does not compile.** Every hierarchy API is
  constrained to reference types — `ISubTypeSerializer<T>`, `WriteSubType`, `ReadSubType`,
  `SubTypeState<T>` — so emitting one produced seven `CS0452`s in the consumer's build, which is the
  worst failure mode available. It is only reachable through an interface, since a struct cannot
  derive from a class.
- **A type may be named by only one hierarchy.** Each works in isolation — the wire form follows the
  *member's* declared type, so the same instance goes out under tag 10 as an `IFirst` and tag 20 as
  an `ISecond` — but protobuf-net refuses the pair once both are in one model, and the generator's
  model is always one model. Note the two paths refuse it differently, which is why the diagnostic
  quotes the compiled one: `Compile` says *"can only participate in one inheritance hierarchy"*,
  while the reflection path gets further and then fails with *"the type cannot be changed once a
  serializer has been generated"*. This is why `GetLinkedBases` returns a list — the count is the
  check, and `GetLinkedBase` is just its first element.

**The trap, and the reason `PBN0023` exists:** the interface layer writes its *own* declared members
in addition to the implementation's, so a property declared on both goes on the wire **twice**. That
is consistent — the interface property and the implementing property genuinely are different members
— but it is not what anyone writing that contract intends, and it is why the analyzer says
"supported but not recommended". `notes/aot/findings.md` has the decoded bytes.

This also turned up a bug in the *shipped* analyzer: `PBN0012` ("declared as an include, but is not
a direct sub-type") compared `BaseType` only, so it reported a build **error** for every interface
hierarchy — a pattern that works perfectly well at runtime. Same class of bug as `PBN0015` on
surrogated types.

Two library-level things this exposed, both invisible on the JIT differential path:

- `TypeHelper<T>.ValueChecker` reached `StructValueChecker<TStruct>` through `MakeGenericType`, so
  ILC never generated it and the first serialize of any **struct contract member** threw *"missing
  native code or metadata"*. For a non-nullable value type both answers are constants, so
  `NonNullValueChecker<T>` — deliberately unconstrained, so `TypeHelper<T>` can name it — is used
  instead. The reflective path remains only for `Nullable<TStruct>`.
- `TypeModel.GetSubTypeSerializer<T>` and `SubTypeState<T>.ReadSubType<TSubType>` needed
  `DynamicAccess.ContractType`; annotating both terminates at the generated call sites, which pass a
  concrete type. The one left is `SubTypeState<T>.Cast`'s `Merge`, which would need the annotation on
  the **class**'s `T` — i.e. on every consumer, including the generated path. Left alone deliberately.

**Merging incompatible sibling sub-types overflows the stack** — `SubTypeState.Cast` → `Merge` →
`Model.Serialize<object>` → … — and that reproduces with `RuntimeTypeModel` alone, with no generated
code involved. It is only reachable from a payload carrying the same field twice with two different
sub-type markers (`Dog` then `Cat`); same-branch merges, in either direction, are fine.
`Inherit.input.cs`'s `Holder` samples stay on one branch because of it, since the differential suite
manufactures repeated fields by concatenating every sample of a type.

#### Out-of-band sub-types: `[ProtoSubType]`

`[ProtoInclude]` lives on the base, so it cannot express a hierarchy whose base has never heard of the
sub-type — a base in a package that does not reference yours, or a sub-type like `Tagged<Order>` that
no base library could have named. `AddSubType` is the runtime answer; `[ProtoSubType(typeof(Base),
typeof(Sub), fieldNumber)]` is the compile-time one (protobuf-net#1308). It is **generator-only**: the
runtime model does not honour it, exactly as it does not honour `[ProtoSurrogate]`.

- **the merge happens in `TryGetSubTypes`, and that is the whole trick.** It is the single choke point
  `GetLinkedBases` (and so `GetHierarchyRoot`) runs through, so nothing downstream can tell an
  out-of-band link from a `[ProtoInclude]` — the emit needed **no** change at all.
- **sites are `[ProtoSurrogate]`'s plus module**: referenced assemblies (and their modules), this
  assembly, its module, then the model. Scanning assembly/module attributes is bounded; scanning every
  type in every reference is not, which is the same reason recorded for surrogates.
- **they accumulate rather than override** — the one place the surrogate precedent misleads. Two
  packages each adding a sub-type to one base give a hierarchy with both. An *exact* restatement of a
  declaration (same base, sub, number and framing) is absorbed rather than reported, since a consumer
  repeating a reference's declaration is a fair thing to write.
- **no seeding is needed**, and the design note in `notes/aot/findings.md` was wrong about this before
  it was built: merging at the choke point makes the hierarchy self-seeding from *either* end —
  reaching the base enqueues the sub-types as `reachable`, and seeding a sub-type finds the base
  through `GetLinkedBases`. `ProtoSubTypeReferenceTests` pins both directions.
- **framing is a `bool`, not a `DataFormat`.** A sub-type is always a sub-message, so length-prefixed
  versus delimited is the only choice there is; the other `DataFormat` values would silently mean
  nothing. "Left to protobuf-net" is told from "explicitly length-prefixed" by **constructor arity**
  (`(base, sub, field)` vs `(base, sub, field, group)`), which is why it is an overload rather than an
  optional argument. Nothing consumes that distinction yet.
- **it is deliberately not generic.** `[ProtoInclude<A, B>(100)]` was the shape proposed in the ticket
  and `where TSub : class, TSuper` would have done `PBN0012`'s job for free — but on .NET Framework,
  `GetCustomAttributes` on *any* member carrying a generic attribute throws `NotSupportedException:
  Generic types are not valid`, and `AttributeMap.Create` is `GetCustomAttributes`, on both types and
  assemblies. An attribute with no runtime meaning must not be able to break a net4x consumer's
  runtime model. Probed with a net462 console app; `CustomAttributeData` is fine, net8.0 is fine.
- **validation is `MetaType.AddSubType`'s**, in its order, and each refusal *matches* protobuf-net
  rather than falling short: field number outside 1–536870911, a sealed/struct/unbound base
  ("Sub-types can only be added to non-sealed classes"), and a sub-type that does not derive from the
  base ("is not a valid sub-type of") — which also catches a type declared as a sub-type of itself,
  since `DerivesFrom` starts at `BaseType`.
- **duplicates are checked across both surfaces at once**, in `ParseContract` after the `DerivesFrom`
  filtering — which matters, because a generic base's includes are shared by every closed construction
  and only one applies to each. This is new for `[ProtoInclude]` too: two sub-types at one field
  number, or one sub-type named twice, previously reached the emitter and produced a duplicate switch
  label. `PBN0003`/`PBN0011` are *errors*, so no compiling source tree contains one — hence the corpus
  did not move.
- **a bad declaration is recorded against the base and reported only if that base is reached.** A
  declaration lives on an assembly, so reporting eagerly would warn about a library's mistake in every
  model that references it. It reports `PBN3002` and drops the base, which cascades to the sub-types —
  a half-linked hierarchy is worse than none, since an unlinked sub-type emits standalone and silently
  disagrees on the wire.
- **`PBN0013` had to be taught about it.** "No include is declared for X" fired on every sub-type
  linked this way, nagging people to write the attribute they cannot write. A `[ProtoSubType]` at
  **any of the three sites** now satisfies it, which is what made `DataContractAnalyzer` grow a
  `RegisterCompilationStartAction`: the declarations are a property of the compilation, so they are
  gathered once (assembly, module, and every type the compilation declares, which is what reaches a
  model class) and shared by the syntax-node action. Built **lazily** — only PBN0013 asks, so most
  compilations never walk anything. Note the file's old comment claiming `AnalysisContext` has no
  compilation-start hook was simply wrong; `AotMigrationAnalyzer` had been using one all along.
- **the sides are matched on their `OriginalDefinition`s**, because a declaration names a *closed*
  construction (`Tagged<int>`) while `PBN0013` is reported against the *open* declaration it came
  from (`Tagged<T>`) — a plain symbol comparison never matches, which is the same trap `PBN0012` has
  a test for. Nothing is lost: the open declaration is the only place there is to report.
- **`PBN0013` carries a help link now** (`docs/rules/PBN0013.md`, following DapperAOT's per-rule
  layout) because the message cannot hold the whole story. The part that needs the room: registering
  at another layer — `AddSubType` on a `RuntimeTypeModel` — is invisible to any analyzer, so the
  warning is *expected* there and can be ignored. New rule pages go in that folder and are linked by
  `helpLinkUri`, not by prose in the message.
- **the replay is in all three harnesses** — `AotRefGen`, `AotConformanceTests` and `AotDifferential` —
  through the public `MetaType.AddSubType(fieldNumber, type, dataFormat)`, beside the surrogate replay.
  Without it the reference model has never heard of the linkage and serializes the sub-type as a
  standalone contract, which reads as a generator fault and is the opposite.
- fixture layout follows the assembly-attribute rule already recorded for surrogates: the Data/
  fixture declares on the **model** (a declaration in Data/ at assembly level would apply to every
  fixture, and `AotRefGen` would then register those types in every model and grow every
  `.reference.cs`), `Diagnostics/AssemblySubType` covers the assembly and module spellings in
  isolation, and `ProtoSubTypeReferenceTests` covers the genuinely cross-assembly hand-off.

### Compatibility level and the BCL types

`DateTime`, `TimeSpan`, `Guid` and `decimal` are the **only** things the compatibility level touches,
so the two features are one piece of work. All four are length-prefixed —
`WriteFieldHeader(n, WireType.String)` — and go through `BclHelpers`; the level picks the method:

| | 200 | 240 | 300 |
| --- | --- | --- | --- |
| `DateTime` | `DateTime` | `Timestamp` | `Timestamp` |
| `TimeSpan` | `TimeSpan` | `Duration` | `Duration` |
| `Guid` | `Guid` | `Guid` | `GuidString`, or `GuidBytes` with `DataFormat.FixedSize` |
| `decimal` | `Decimal` | `Decimal` | `DecimalString` |

Resolution is a port of `TypeCompatibilityHelper`: **member attribute → type attribute (inherited
from base types) → module → assembly → 200**, then `ValueMember.GetEffectiveCompatibilityLevel`,
where at or below 200 `DataFormat.WellKnown` promotes to 240 and above 200 it means nothing.

Facts taken from ref-emit rather than assumed:

- **`DateTime` is written unconditionally.** The other three are guarded against `TimeSpan.Zero`,
  `Guid.Empty` and `0m` — zero is a legitimate date, so there is no trivial value to skip. A
  *nullable* one is guarded by `HasValue` like any other nullable, with no inner value test.
- `DataFormat.FixedSize` on a `Guid` **below** level 300 is simply ignored, not an error.
- `DataFormat.WellKnown` on a `Guid` or `decimal` is a no-op, since 240 equals 200 for those two.
- `[DefaultValue]` on any of the four is refused: there is no ref-emit shape to copy.

`[module: CompatibilityLevel(...)]` is fixtured under `Data/Diagnostics/` **deliberately**: a module
attribute applies to the whole assembly, and `AotRefGen`/`AotConformanceTests` link every fixture
into one, so placing it beside them would silently re-level all of them. The golden tests compile
each input in isolation, which is exactly what is needed.

The level-200 `Guid` path used to be recorded here as costing four AOT warnings the other forms did
not. It never did: those warnings were kept-reflectable members of `System.Enum`, and ILC merely
attributed them to `WriteGuid`/`ReadGuid` as one of several retained paths. They are gone, along
with the rest of that group — see item 4 of `notes/aot/findings.md`, and treat per-feature warning
attributions with suspicion generally.

`[ProtoDataFormat(type, format)]` rides the same machinery as the level: it resolves **type → module
→ assembly**, exactly like `CompatibilityLevel`, and an explicit member format always wins over the
default. `Default` is the zero sentinel, so "explicit `Default`" cannot be distinguished from
"unstated" — **at member scope**: a member cannot state `DataFormat.Default` to opt itself back out
of a default declared above it, because that is indistinguishable from the member saying nothing at
all. **At type scope this is not true**: a type carrying its own `[ProtoDataFormat(typeof(X),
DataFormat.Default)]` genuinely does shadow an assembly- or module-level default for `X` on its own
members, because type beats module/assembly in the walk regardless of which value the type declared —
first match wins, even when the matched value is `Default`. It keys on the
**Nullable-unwrapped scalar or element type** — a `Guid?` member picks up a `Guid` default exactly as
a bare `Guid` does — and deliberately never reaches **maps** or **null-wrapped** members, both of
which have their own per-side format story already. Both the runtime (`TypeDataFormatHelper` plus the
`MetaType.ApplyDefaultBehaviour` hook) and the generator (`GetDataFormatDefault`, alongside
`GetCompatibilityLevel`) honour it, so the differential suite covers it with no replay needed. Note
the fixture-assembly trap applies to it exactly as to `[module: CompatibilityLevel]`: an assembly- or
module-scoped declaration would re-level every fixture linked into the same compilation, so an
assembly/module-scoped golden fixture belongs under `Data/Diagnostics/` for the same reason —
`AotRefGen`/`AotConformanceTests` link every fixture into one.

The ambient default is applied **exactly as if the member had stated it explicitly** — including
triggering whatever refusal that combination would produce. Maps and null-wrapped members are exempt
because the injection never reaches them at all (above), but everything else is not: an assembly
declaring `[assembly: ProtoDataFormat(typeof(int), DataFormat.Group)]` makes every bare `List<int>`
member in that assembly newly throw while building the model, exactly as `DataFormat.Group` on an
explicitly-stated scalar collection member already does. That is not a parity bug between the runtime
and the generator — both agree on the refusal — it is simply a consequence of the default being
applied before the usual per-member checks run, worth knowing before reaching for an assembly-wide
default.

**On v4 it also costs measure-first, and that is the expensive half.** `RawMemberMeasureBlocked`
blocks a member on *any* non-default `DataFormat` bar two carve-outs (a packed column, which vets
its own format, and `Group` on a unary message), and `BclMeasurable` gates on the default format
outright — so an ambient default reaches members that were measurable and makes them not, and one
blocked member removes its **whole contract** from the measurable set, to a fixed point through
every referrer. `[assembly: ProtoDataFormat(typeof(int), DataFormat.ZigZag)]` is therefore enough to
take essentially a whole model off the raw write path, silently and with no diagnostic. The
`FormatDefault` golden is the canary: it emits `RawRead_` and **zero** `Measure_`/`RawWrite_`
methods, which is exactly the "count the methods" diagnostic recorded under "Don't improve the
legacy library". This is a gap in the measure arms, not in the feature — see `notes/gaps.md` B26,
and note the formats this attribute makes common are the *easiest* arms to add, being constant-width
(a level-300 `FixedSize` Guid is 16 bytes; `FixedSize` `DateTime`/`TimeSpan` is 8).

### Extensible contracts

An extensible contract keeps the fields it does not recognise: the read's `default:` case becomes
`state.AppendExtensionData(...)` instead of `state.SkipField()`, and the write appends the stored
bytes after every declared member. That is the whole change — the serializer only ever copies raw
bytes, so it needs no reflection.

Which overload is used is **not** simply "whichever interface is implemented". Ref-emit's rule
(`TypeSerializer.UseTypedExtensible`) is `ITypedExtensible && (in a hierarchy || IExtensible is not
also implemented)`. Since `Extensible` supplies both interfaces, that means:

| declares | standalone | in a hierarchy |
| --- | --- | --- |
| `IExtensible` | untyped | **refused** |
| `ITypedExtensible` | typed | typed |
| both (i.e. `Extensible`) | untyped | typed |

The typed overload passes `typeof(<this layer>)` — not the root — so each layer of a hierarchy keys
its own bag and the same field number can appear at several levels without colliding. In a sub-type
read the `default:` case uses `value.Value` rather than the per-case local, since the instance has to
exist before anything can be stored on it.

Two combinations ref-emit rejects while *building* the model, so there is nothing to reproduce and we
refuse them up front: extensible **structs**, and `IExtensible` without `ITypedExtensible` on a type
with inheritance.

Deriving from `ProtoBuf.Extensible` is exempted from the "derives from a type that does not declare
`[ProtoInclude]` for it" refusal — it is the documented way to get the interfaces and declares no
serializable members of its own.

**`Extensible.AppendValue` does not work under AOT**, and fails silently. It serializes through
`TrySerializeAuxiliaryType` with a null type — i.e. the reflective path — and the return value is
discarded, so the value is simply never stored. It is fine in the JIT fixtures, which is where
`Extensible.input.cs` uses it to manufacture an unknown field; `AotSmoke` instead produces one by
serializing a wider contract (`NoteV2`) and reading it back as the narrower one, which keeps the test
on the generated path.

### Setters that C# cannot call: `init`-only and non-public

Both route through `[UnsafeAccessor]`, which is why they share `ProtoMemberPlan.UsesAccessor`.

IL has neither restriction — `init` is a modreq the C# compiler enforces, and IL does not care about
accessibility — so ref-emit's *runtime* path simply calls the setter. `[UnsafeAccessor]` is the exact
equivalent for generated code, and unlike reflection it is resolved at publish time, so it stays
AOT-safe; `AotSmoke` carries one of each specifically to prove ILC resolves them.

It is **net8.0 and up**, so the generator probes for `UnsafeAccessorAttribute` and refuses below
that. The accessors are emitted onto the services type as `private static extern` methods named after
the sanitised contract type plus the member; a struct target takes `ref`.

Note `AotRefGen` is net472 and so predates `IsExternalInit`; `src/AotRefGen/Polyfills.cs` declares
it, since it is a pure compile-time marker.

### Enums as contracts

`[ProtoContract]`'s own `AttributeUsage` allows **class, struct, enum and interface**, and an enum
seeded by `[ProtoSerializable]` is a model root in its own right. It needs no new emit shape: ref-emit
puts `ISerializerProxy<TEnum>` and `ISerializerProxy<TEnum?>` on the services type and **no
`ISerializer<TEnum>` body at all**, because `EnumSerializer` *is* the serializer — the same proxies a
repeated or null-wrapped enum member already requires, with the same
`EnumSerializer.Create{Underlying}<TEnum>()` body.

So a seeded enum joins the proxy set rather than becoming a contract plan; `ProtoEnumPlan` carries
just the type name and the underlying scalar kind. Reached as a *member* an enum was always an inline
scalar, and still is — the two paths coexist, which the fixture pins.

### Serialization callbacks

Both families — protobuf-net's `[ProtoBeforeSerialization]`/`[ProtoAfterSerialization]`/
`[ProtoBeforeDeserialization]`/`[ProtoAfterDeserialization]` and `System.Runtime.Serialization`'s
`[OnSerializing]`/`[OnSerialized]`/`[OnDeserializing]`/`[OnDeserialized]` — map onto the same four
points and are honoured identically by `MetaType`. They differ only in that the
`System.Runtime.Serialization` spelling takes a `StreamingContext`, supplied as
`SerializationContext.AsStreamingContext(state.Context)`.

Placement is ref-emit's: the "before" hook fires **after construction but before the field loop**,
and the "after" hook after it — so a deserialization callback sees a fully populated instance.

We accept a narrower set of signatures than `MetaType` does: public, non-static, `void`, taking
nothing, a `StreamingContext`, or an `ISerializationContext`. `MetaType` reaches non-public ones by
reflection and tolerates more shapes (`SerializationContext`, `System.Type`); anything outside our
subset is refused rather than mis-called.

**The three are not interchangeable, and the third earns its place.** Only `ISerializationContext`
carries the context *object* rather than a copy of its data, so it is the only one a callback can
hand to `ProtoWriter.IsMeasuring` — which is how it tells a measure-first contract's two
before-serialization passes apart. A callback taking nothing or a `StreamingContext` still fires in
both and simply cannot tell which it is in.

### `ImplicitFields`

Members are inferred by convention instead of by attribute. `AllPublic` (**= 1**) takes any public
member — a property counts when its *getter* is public, whatever the setter is; `AllFields` (**= 2**)
takes any field. Note that numbering: the constants read in the opposite order to their values, and
getting them backwards silently swaps the two modes.

Tags come from sorting the whole set, so they cannot be worked out member-by-member: candidates sort
by `(pinnedTag, ordinal name)` and the unpinned ones are numbered from `ImplicitFirstTag`. Confirmed
against ref-emit rather than inferred:

- ordering is by **name, not declaration order** — `Zebra, Apple, Mango` numbers as `Apple`=1,
  `Mango`=2, `Zebra`=3;
- a member with an explicit `[ProtoMember]` keeps its pinned tag and does **not** consume a
  sequential number, nor is that number avoided — `5` pinned alongside `1, 2` is normal;
- the **type-level** attributes count too, and have to be applied where the numbering is worked out
  rather than only in the read/write loop: `[ProtoPartialIgnore]` removes a name from the candidate
  set, and `[ProtoPartialMember]` pins one exactly as `[ProtoMember]` does. Excluding a name only
  from the loop leaves it *consuming a tag*, which shifts every unpinned member after it — a
  one-member mistake that corrupts the whole message;
- implicit mode narrows the attribute family to ProtoBuf only, so `[DataMember]`/`[XmlElement]`
  orders stop applying.

**The trap: `AllFields` takes auto-property backing fields.** `Ignored { get; set; }` is serialized
as `<Ignored>k__BackingField`, and because `<` precedes letters in ordinal order it sorts *first* and
takes tag 1 — shifting every real field. This was found by the differential suite disagreeing over
`Dictionary`-free two-field contract, and only makes sense once you see the backing field in
`GetFields()`. It also means the member name reaches `AccessorName`, so that sanitises the **member**
name as well as the type's.

A non-public field needs `[UnsafeAccessor]` for **both directions** — unlike a property reached by
its backing field, it cannot be read directly either, hence `ProtoMemberPlan.AccessorReads`. That
also widened explicit `[ProtoMember]` on a private field, which used to be refused: same three-way
split as a non-public setter, so `ImplicitPrivate.input.cs` has no `.reference.cs`.

### Surrogates

`[ProtoContract(Surrogate = typeof(X))]` moves the wire shape onto another type. The emitted
serializer for the underlying type **is the surrogate's body**, with a conversion at each end — and
nothing changes for a *member* whose type is surrogated, which stays an ordinary sub-message. The
surrogate is a contract in its own right and gets its own serializer alongside.

So the plan for a surrogated contract carries the **surrogate's** members, and the surrogate is also
what decides construction, `IsSealed` and `ThrowUnexpectedSubtype` — the underlying type is never
constructed, which is exactly what lets an *immutable* type be surrogated. The parse deliberately
defers the parameterless-constructor check until after the surrogate is known, for that reason.

We emit an explicit cast in both directions rather than ref-emit's implicit conversion, so that an
`explicit operator` works as well as an `implicit` one. `Compilation.ClassifyConversion` decides
whether the pairing is legal — which conveniently rules out protobuf-net's third option, a
`[ProtoConverter]`-attributed *method*, that no cast can express. A surrogate on a type with
inheritance, or a surrogate that is a collection, are both refused: protobuf-net throws for those.

#### Hand-written serializers

`[ProtoContract(Serializer = typeof(X))]` means the contract has a hand-written serializer, so we
emit **no body at all**: the services type implements `ISerializerProxy<T>` handing that serializer
out, and members of that type pass `SerializerCache.Get<X, T>()` rather than `this`.

**The serializer's *category* changes the emitted shape, and cannot be looked up.** A hand-written
serializer declares `CategoryScalar` or `CategoryMessage` in its `Features`, and a scalar one means
the member is not a sub-message at all: it is framed by the serializer's own wire type. Assuming
"message" writes a length prefix over a bare varint, which throws at runtime — or, for a scalar
serializer whose wire type happens to be `String`, disagrees silently. `Features` is a *property*,
which ref-emit obtains by instantiating the serializer and a generator cannot. So, in order:

1. **`[ProtoContract(Serializer = …, IsScalar = true)]`** — an attribute *argument*, so it survives
   into metadata. The only route that works for a serializer in a compiled reference.
2. **the `Features` declaration**, when the serializer is in this compilation: it is nearly always an
   expression body over constants (`CategoryScalar | WireTypeVarint`), which Roslyn folds.
3. otherwise the framing is **deferred to run time**: the member emits
   `state.WriteAny<T>(n, value, serializer)` and `state.ReadAny<T>(default, value, serializer)`, both
   of which switch on the serializer's real `Features` — `WriteMessage`/`ReadMessage` for a message,
   `serializer.Write`/`serializer.Read` for a scalar. That is the identical decision, made a little
   later, so there is nothing to refuse.

   This is worth remembering as a *pattern*, not just a fix: where the library already has a public
   API that makes a decision at run time, a compile-time generator does not have to make it at all.
   The write shape was already `WriteAny` for the known-scalar case; only the read needed a new form.
   Verified byte-identical to ref-emit for **both** categories by the corpus differential, which is
   the only reason to believe it — `DynamicCategory.input.cs` pins both in a fixture, using a
   `Features` expression Roslyn cannot fold so that the undetermined path is genuinely taken.

Where both routes answer and disagree, that is reported too — a stale annotation would otherwise
change the framing on the wire silently.

The scalar write is `state.WriteAny<T>(n, value, serializer)`, **not** a hand-written field header:
`WriteAny` takes the features off the serializer and frames accordingly, and it is public, whereas
the `GetWireType` extension that would let us write the header ourselves lives on an `internal`
class. Byte-identical to ref-emit's `WriteFieldHeader` + `Write`, which the differential confirms.
The read is `serializer.Read(ref state, value)` with no framing either way.

Note what step 3 costs, since it is a real trade rather than a free win: an external serializer
reached only through metadata is now refused even when it *is* a message, which the corpus sweep
feels acutely — everything there is metadata, so it lost ~6 contracts that had been emitting
correctly. They were correct by luck rather than by check, and the fix is one attribute; but a
consumer whose serializer lives in their own source is unaffected, which is the common case.

The category is then **asserted at runtime**, since a stated `IsScalar` is unverifiable while
generating by definition — it may name a serializer in another assembly. The services type's
constructor carries a `Debug.Assert` per external serializer, comparing its real `Features` against
what was generated for.

Two details worth keeping. It is in the **constructor**, not the proxy: members call
`SerializerCache.Get<X, T>()` directly, so the proxy is not on that path at all and an assert there
never runs — the first attempt at this passed its own test by never executing. And `Debug.Assert` is
`[Conditional("DEBUG")]` resolved against the **consumer's** compilation, so a release build pays
nothing but an empty constructor. Proven to fire by stating the wrong category with the source route
suppressed, not merely by observing it pass.

A scalar serializer as a **collection element or map value** is refused: the unary shape is derived
from ref-emit, that one is not.

There is a wrinkle: protobuf-net's own well-known types name the **internal** `PrimaryTypeProvider`,
which a consumer's generated code cannot reference. Those are inbuilt types that
`TypeModel.GetSerializer<T>` resolves without a model, so an *inaccessible* serializer is treated as
"inbuilt": the member passes `null` (which is what resolution does anyway) and the type is not pulled
into the model at all.

`[ProtoSerializer]` externalizes `Serializer = …` exactly as `[ProtoSurrogate]` externalizes
`Surrogate = …`: a declaration on an assembly or the model, for a type you cannot put the attribute
on directly, with the same three-scope gathering (referenced assemblies, then this assembly, then
the model, most specific wins), the same full-name matching, and the same closed-beats-open
precedence — a declaration naming a closed type wins over one naming the open generic definition it
would otherwise map through, and a type's own `[ProtoContract(Serializer = …)]` beats a declaration
from its assembly but not one from the model. All three harnesses (`AotRefGen`, the corpus
differential, and `AotConformanceTests`) replay a declaration the same way — through
`MetaType.SerializerType` — rather than teaching each harness a second mechanism, and the category
assert's message now names both routes (the contract's own `Serializer =` and a `[ProtoSerializer]`
declaration) so a mismatch says which one supplied the serializer.

**A `Nullable<TStruct>` member whose serializer is scalar- or undetermined-category used not to
compile at all**, and now works. The two switches disagreed about precedence: the *write* tested
`IsNullable && Kind == Message` **before** the scalar arm and routed to `WriteMessage` — a length
prefix over a bare scalar — while the *read* tested the scalar arm **first** and emitted
`ISerializer<T>.Read(ref state, T?)`. Probed rather than reasoned: the generated code failed with
`CS1503: cannot convert from 'Gauge?' to 'Gauge'`, so the wrong bytes were unreachable — the
consumer's build broke first. Both sides now unwrap with `GetValueOrDefault()` and the write uses
`WriteAny`, which takes the framing off the serializer. `AotSmoke`'s `Tally<string>? Bonus` covers
it under ILC.

Worth keeping as a shape, not just a fix: **presence and framing are decided by different switches
in the two directions**, so a member kind that is both nullable *and* specially framed has to be
handled in both, and getting only one produces a build break rather than a wire bug — which is the
good failure, and the reason this sat undiscovered.

#### `[ProtoSurrogate]` on the model

You cannot put an attribute on `System.Uri`, so the contract-level form cannot reach a type you do
not own — which is most of the coverage sweep's member-type tail. `[ProtoSurrogate(typeof(Uri),
typeof(UriSurrogate))]` on the **model** is the compile-time equivalent of
`RuntimeTypeModel.SetSurrogate`, and like `[ProtoModel]` itself it is real `[Experimental]` API in
protobuf-net.Core — it is the attribute that forced that move, since it has to cross assembly
boundaries (see the design constraints at the top).

A surrogated type needs no contract attribute at all — the declaration stands in for it, which is
why it is resolved *before* the "is this even a contract" checks and threaded down into
`GetMessageKind`, so a `Uri`-typed member resolves as a message.

Conversion is a cast in each direction by default. `Converter` + `ToSurrogate` + `ToType` name
static methods instead, which is how a type with no usable operators is hooked up — protobuf-net's
own `AddNodaTime` passes exactly such method pairs to `SetSurrogate`. The named methods are checked
for existence, accessibility and signature when the declaration is read.

It can also be declared on an **assembly**, which is what lets a package ship surrogates for the
types it supports. Declarations are gathered least-to-most specific — referenced assemblies, then
this assembly, then the model — so the more specific wins, and a consumer can always override a
library's choice. Note this scans *assembly* attributes only: that is cheap and bounded, where
scanning every type in every reference would not be, which is why the pairing lives on the assembly
rather than on the surrogate type.

**The three-assembly hand-off works**, which is the case that matters for NodaTime: the types live in
one package, the protobuf-net helper that knows how to serialize them is a second, and the consumer
is a third that references the helper and says nothing about surrogates. `ProtoSurrogateReferenceTests`
pins it, and cannot be a golden fixture because it needs genuinely separate compilations.

Historical note: this hand-off is *why* the trigger attributes are Core API now. While they were
generator-owned, the helper's `ProtoSurrogateAttribute` was a **different type** from the consumer's
(each assembly compiled its own `internal` copy), and the hand-off only worked because the comparison
was by **full name** rather than by symbol. The attribute is one shared Core type today, but the
full-name matching deliberately remains — the test harnesses and the reflection-loaded generator in
`AotDifferential` still cannot rely on symbol identity — so still don't "tidy" it into a symbol
equality check.

`ModelSurrogate.input.cs` covers both forms, and **is** differentially covered: `AotRefGen` replays
the declarations onto the reference model through `RuntimeTypeModel.SetSurrogate` — the cast form via
the public `MetaType.SetSurrogate(Type)`, the named-method form via the generic overload taking
conversion delegates. (This file previously said it was a golden-only first cut; that stopped being
true at `83a1b6f9`.)

`System.DateTimeOffset` is the fixture's third case and the one worth knowing about, because it looks
inbuilt and is not: protobuf-net has **no** serializer for it, which is why `Examples/Issues/Issue222.cs`
registers a surrogate by hand. A bare `DateTimeOffset` member is therefore a refusal that *matches*
ref-emit, not a gap — and a `[ProtoSurrogate]` on the model is the whole fix.

**NodaTime works end to end** — `src/AotNodaTimeSmoke` is a consumer that references only
`protobuf-net.NodaTime`, declares no surrogates of its own, and round-trips `Instant` and `Duration`.

Getting there needed a **second surrogate emit shape**. The usual one inlines the surrogate's
members, but `WellKnownTypes.Duration`/`Timestamp` carry their own hand-written serializer and so
have no members to inline. When a surrogate has a serializer, the body converts and then
**delegates** to it. For the well-known types that serializer is the *internal* `PrimaryTypeProvider`,
which generated code cannot name — the public `TypeModel.GetInbuiltSerializer<T>(default, default)`
is how it is obtained instead. (Both arguments are spelled out: the defaulted overload is ambiguous
with the explicit one from a call site supplying neither.)

Such a surrogate is also **not** a contract in the model — it is delegated to, not emitted — so it is
neither enqueued nor a reason to drop anything by cascade.

### Null-wrapping

`docs/nullwrappers.md` is the reference, and unusually complete — but the *shapes* were still taken
from ref-emit. There are two distinct mechanisms:

- **On a collection or map it is pure features composition.** A collection element gets
  `OptionWrappedValue | OptionWrappedValueFieldPresence` (plus `OptionWrappedValueGroup` for
  `AsGroup`); the field-presence flag is what separates a null element from a zero one. On a map the
  two flags **split**: `OptionWrappedValueFieldPresence` rides on the *map*, `OptionWrappedValue` on
  the *value features*. `[NullWrappedCollection]` adds `OptionWrappedCollection` (+`…Group`) and
  composes with the above, since they apply at different scopes — **a map wraps exactly as a
  collection does**, in both scopes.
- **A lone value uses a different API**: `state.WriteAny(n, features, value)` and
  `state.ReadAny(features, value)`. Note the read passes **no wire type** — it comes from the field
  header — while the write states it. The write is *unguarded*: `WriteAny` handles a null itself.

protobuf-net enforces the rules by **throwing** rather than ignoring the attribute, deliberately, so
that widening them later cannot silently change behaviour. Which shapes throw was probed rather than
read off the docs: a **message** and a **compatibility-level BCL type** are both "not scalar" for
this purpose, and a non-nullable value is refused. `[DefaultValue]` cannot combine with it.

Ref-emit passes `this as ISerializer<TEnum?>` for a wrapped enum; **we pass nothing**. Our services
type implements `ISerializerProxy<TEnum?>` rather than `ISerializer<TEnum?>`, and C# rejects that
cast on a sealed type where IL merely yields null — both end up at
`serializer ??= TypeModel.GetSerializer<T>(Model)`, which resolves the proxy through the model.

**The line is drawn by scope, not by type.** A *lone* `[NullWrappedValue]` is valid only on a nullable
scalar; in a **collection**, `docs/nullwrappers.md` says "any scalar or message type will be accepted
(but not nested collections)", and probing confirms it — message, nullable enum, nullable BCL, string
and map-value elements all work. So the **six** lone refusals — non-scalar, non-nullable, and
combined with `[DefaultValue]`, `IsRequired`, `IsPacked` or a non-default `DataFormat` — *match*
protobuf-net, which throws while building the model, and are worded to say so rather than "not
supported yet"; every collection form is supported. They are one contiguous run of guards in
`ValueMember`, so if that run grows, this list should grow with it.

A **nullable element without the attribute is an ordinary element** — `List<int?>` emits plain
features and only faults at runtime if a null actually turns up. That holds for every element kind,
including enums and BCL types.

This is also where a **pre-existing wire bug** surfaced: element and map-side wire types were derived
from a switch that defaulted to `WireTypeVarint`, but the compatibility-level BCL types are
**length-prefixed** — so `List<DateTime>`, `List<decimal>` and `Dictionary<int, Guid>` all disagreed
with ref-emit, wrapped or not. `KindWireType` is now the single source for "the wire type a kind
carries when no `DataFormat` is selecting one", and note it cannot simply ask "is this a BCL kind":
`DateOnly`/`TimeOnly` use `BclHelpers` under a *varint* header and go the other way.

### `ShouldSerialize` / `Specified`

The `{Name}Specified` property and `ShouldSerialize{Name}()` method conventions, inherited from
`System.ComponentModel` / `XmlSerializer` and matched **by name** — no attribute inspection would
find them, which is why they used to drop the whole contract.

- The condition **replaces** the trivial-value guard rather than adding to it, and wraps the whole
  write. So a member with `Specified = true` writes an explicit zero, which is the entire point.
- `{Name}Specified` is also **assigned on read**, and that assignment sits *outside* any null test
  the read itself carries — a null string still sets `NamedSpecified = true`.
- `ShouldSerialize{Name}()` affects the **write only**.
- When a member has both, **`Specified` wins**; probed against ref-emit, not assumed.

`Specified` must be a public get/set `bool`, since we assign it; `ShouldSerialize` must be a public
parameterless `bool` method.

### Getter-only members

A property with no setter is **assigned through its backing field** whenever that field can be named
exactly — see "Reaching a member C# will not let us assign" below. Only when it cannot is the read
discarded: the read still runs exactly as it would otherwise, and for a collection, map or
sub-message that is the whole mechanism, since the instance the property already holds is passed in
and mutated. For a scalar the value really is read and thrown away.

Two consequences for the emitter: the discarded read is a bare statement, so the enum and `char`
casts have to go (a cast expression is not a valid C# statement — hence `ScalarRead(discard: true)`),
and a nullable scalar drops the wrapper too (ref-emit emits a pointless `new int?(…);`).

That includes a **struct or nullable sub-message**: the read runs into a copy and is discarded, so
the member writes but never comes back. Pointless, but it is what ref-emit emits, and refusing would
cost the whole contract.

### Constructors C# will not let us call

A **non-public parameterless constructor** goes through `[UnsafeAccessor(UnsafeAccessorKind.Constructor)]`,
which puts it in the same family as the setters below and produces the same three-way split, probed
rather than assumed:

| | persisted dll | `RuntimeTypeModel` | generated |
|---|---|---|---|
| non-public parameterless ctor | throws *"Non-public member cannot be used with full dll compilation"* | reflection | accessor |
| no parameterless ctor at all | throws *"No parameterless constructor found"* | **also throws** | refused |

So the first row matches the runtime model, and the second is a refusal that matches *both* ref-emit
paths — those contracts do not work in protobuf-net at all, which is exactly what the shipped
analyzer's `PBN0015` (an **error**) already tells you. Worth knowing before trying to "fix" them:
every one of the sweep's remaining 12 is that shape, not a non-public constructor, so the corpus
number does not move.

`[ProtoContract(SkipConstructor = true)]` remains the documented way out, and is unaffected.

### Reaching a member C# will not let us assign

Three shapes need help: `init`-only setters, non-public setters, and no setter at all. All three go
through `[UnsafeAccessor]` (net8.0+), which unlike reflection is resolved at publish time and so
stays AOT-safe. **The field is preferred over the setter** wherever we can name it exactly — it is
the only way to reach a getter-only member, and for the other two it is simply less machinery.

The field is taken from one of two places, and never guessed:

- an **auto-property**, where Roslyn hands us the backing field outright (an `IsImplicitlyDeclared`
  field whose `AssociatedSymbol` is the property). No inference, so no chance of naming the wrong
  field; the name renders as `<Foo>k__BackingField`;
- a **trivial getter** — `Foo => _foo;`, `get => _foo;`, or `get { return _foo; }` — read off the
  syntax and matched to a field of the same type on the same type.

Anything less than trivial (`Doubled => _value * 2;`) falls back: the property accessor if there is
a setter to call, otherwise read-and-discard. A guessed field name would silently write to the
wrong place, which is far worse than not writing at all.

A getter-only auto-property's backing field is `initonly`; `UnsafeAccessorKind.Field` hands back a
plain `ref` regardless and writing through it is fine — proven under native AOT (`AotSmoke` covers
both an `initonly` backing field and an explicitly `readonly` one), not just on JIT.

**This is where we diverge from ref-emit, and the two paths diverge from each other:**

| | persisted dll | `RuntimeTypeModel` | generated |
|---|---|---|---|
| non-public setter | throws | reflection | field |
| getter-only auto-property | discards | backing field | field |
| getter-only trivial getter | throws | throws | **field** |

So for auto-properties we are *matching* the runtime model — which the generator previously failed
to do, discarding values ref-emit restores. For a trivial getter we are strictly more capable than
either: `PropertyDecorator.SanityCheck` throws ("cannot apply changes to property") because
reflection has no setter to call and no way to know which field the getter reads. We know, because
we can see the source.

That last row is why `TrivialGetter.input.cs` has **no samples and no `.reference.cs`** and is on
`DifferentialTests.NotDifferentiable`: there is no reference behaviour to differ from. It is covered
by `TrivialGetterTests` instead, which round-trips directly and pins the ref-emit throw.

Getter-only members are also invisible to the differential suite *in principle*, and the reason is
worth remembering before adding a fixture for one: a sample can only ever hold the value its
constructor gave it, so "discard the incoming value" and "store it" agree on every sample that can
be built. It only shows up against a payload that disagrees with the constructor, which is what
`GetterOnlyMemberRoundTrips` builds by hand.

Consequently `NonPublicSetter.input.cs` has **no `*.reference.cs`**: ref-emit declines to compile it,
and `AotRefGen` now skips a model it cannot emit rather than failing the whole run. The differential
suite still covers the fixture, because `RuntimeTypeModel` *does* handle these — which is exactly the
comparison that matters for a divergence from the *compiled* path.

### Generics

The distinction is **open versus closed**, not generic versus not. A closed construction is an
ordinary contract — Roslyn hands us its members already substituted, so `Wrapper<int>` and
`Wrapper<string>` are simply two contracts that happen to share a definition, each with its own
`ISerializer<>`. They arrive as member types far more often than as seeds, which is why refusing
them cost 24 contracts in the sweep.

An **open** one is refused, and cannot be otherwise: the services type is a single non-generic class,
so there is nowhere to put the type parameter. The test recurses (`Wrapper<List<T>>` is open too) and
walks `ContainingType`, since `Outer<T>.Inner` carries the parameter on the enclosing type.

`typeof(Foo<>)` needs its own check: it yields an **unbound** symbol whose `TypeArguments` are *not*
type parameters, so `ContainsTypeParameter` alone returns false and the contract falls through to be
refused for an unrelated-sounding reason — it was reported as "there is no public parameterless
constructor" before `IsUnboundGenericType` was added alongside it.

Nothing else needed changing: the emitted identifiers already sanitise non-alphanumerics (so
`Wrapper<int>` and `Wrapper<string>` get distinct accessor names), and `AotSmoke` covers both a
reference and a value instantiation, the latter being the one ILC must generate concrete code for.

### `nint`, `DateOnly` and `TimeOnly`

Four types with dedicated built-in serializers in `ValueMember.TryGetCoreSerializer`'s switch, which
the generator simply did not emit. Facts taken from ref-emit:

- `nint`/`nuint` are ordinary varints. Ref-emit asks `GetIntWireType` for width **64 regardless of
  the platform**, so `FixedSize` is `Fixed64` on both and the wire form does not vary by
  architecture — which is the only reason these are safe to support at all.
- `DateOnly`/`TimeOnly` go through `BclHelpers` like the four compatibility-level types, but under a
  **varint** header rather than a length prefix, and the compatibility level does not reach them.
- **Both are written unconditionally**, like `DateTime` and for the same reason: zero is a
  legitimate date, so there is no trivial value to skip. This was caught by the differential suite,
  which is what a guess would have got wrong — `TimeSpan`, by contrast, *is* guarded against
  `TimeSpan.Zero`.

`BclHelpers.ReadDateOnly` lives inside `#if NET6_0_OR_GREATER`, so the generator probes for the
**method**, not the language type — a consumer below net6.0 has `DateOnly` but nothing to call. Two
consequences worth knowing before touching `DateOnly.input.cs`:

- the golden tests compile against the **netstandard2.0** BuildTools assembly, so the fixture's
  golden is deliberately a *drop*; the differential suite (net8.0) is where it is really exercised;
- `AotRefGen` is net472, where `DateOnly` does not exist at all, so that one fixture is explicitly
  `<Compile Remove>`d from it and has no `.reference.cs`.

### `System.Uri`

**`Uri` is an inbuilt scalar, not a surrogate case** — worth stating plainly, because it looks like
the canonical "type you don't own and must surrogate" and is not. `ProtoTypeCode.Uri` resolves to
`StringSerializer` wrapped in a `UriDecorator`, and `SetSurrogate` on it *throws*: "Data of this type
has inbuilt behaviour, and cannot be added to a model in this way".

On the wire it is a plain string. Two details from ref-emit rather than inference:

- the write's **null test is explicit**, unlike a plain string where `WriteString(int, string)` skips
  nulls itself — by then `OriginalString` would already have thrown;
- the read treats an **empty string as null** (`text.Length != 0 ? new Uri(…) : null`), and the kind
  is `UriKind.RelativeOrAbsolute`, so relative URIs round-trip.

As an element or a map value it is simply a string-typed scalar; the repeated and map serializers
handle it with no proxy.

### Parseable types

A type with a `ToString()` and a `static T Parse(string)` can go on the wire as a string
(`ParseableSerializer`). This is **opt-in on both sides**: `RuntimeTypeModel.AllowParseableTypes` is
off by default, so `[ProtoModel(AllowParseableTypes = true)]` is the compile-time mirror of it.
Emitting it unconditionally would disagree with the runtime model's *default* behaviour, which is
worse than not supporting it.

The predicate is a port of `ParseableSerializer.TryCreate`, and every clause is load-bearing:
`Parse` and **not** `TryParse`; declared on the type itself, so an inherited one does not count;
exactly one `string` in, the type out. A **value type** additionally needs its own `ToString()`
override — one inheriting `object.ToString()` would round-trip its type name, which is the case the
library guards against.

**Placement is the part that is easy to get wrong.** In `ValueMember.TryGetCoreSerializer` the
parseable test sits *after* the built-in scalar switch but *before* contracts — so a `[ProtoContract]`
type that happens to carry a `Parse(string)` is serialized as a **string, not a message**, and a type
with a built-in serializer keeps it even though it would also qualify (`DateOnly` and `nint` both
would). Putting the check at the end — the tidier-looking option, and what this first did — silently
disagrees: it made a parseable contract a message, and an auto-tuple out of a parseable class.

Both harnesses have to mirror the model's options or they are not comparing against ref-emit at all:
`AotRefGen` reads `AllowParseableTypes` off the `[ProtoModel]` attribute, and `DifferentialTests`
does the same in `CreateReference`. Without that the reference is the *unparsed* shape and every
parseable fixture looks like a generator bug.

Note the coverage sweep does **not** enable this, deliberately — a real consumer has to opt in, so
counting these as emittable would overstate what works out of the box.

A UTF-8 fast path for these (`IUtf8SpanFormattable`) is parked in `notes/aot/findings.md` under
"Future ideas" — it is blocked on protobuf-net having no UTF-8 `WriteString` equivalent, and the
read half of the interface pair is implemented by far fewer types than the write half.

### Schema-only options

Several protobuf-net options exist purely to shape the generated `.proto` and never reach the wire.
Refusing a contract over one of those loses a serializer for no reason, and the coverage sweep says
they are common, so each is **accepted and ignored**: `[ProtoContract(Name = …, Origin = …)]` and
`[ProtoMember(Name = …)]`.

**`[ProtoReserved]` was on that list and does not belong there.** It is not merely schema decoration:
protobuf-net *enforces* it while building the model and throws *"Field 31 is reserved and cannot be
used for data member 'B'"*. Ignoring it means emitting a contract that protobuf-net rejects outright,
which the corpus differential caught on six `Issue633` contracts. Still true of the other two, which
really are schema-only. `[ProtoIgnore]` excludes the member, like
`[XmlIgnore]` and `[NonSerialized]`.

There is also **no "it has no members" refusal**: an empty message is entirely legal protobuf, and
`.proto`-generated DTOs are full of them — it was the single largest cause of dropped contracts in
the sweep. It emits a bare skip loop with no `switch`, matching ref-emit.

### `[ProtoPartialMember]` and `[ProtoPartialIgnore]`

Both apply a member-level decision **by name, from the type** — which is the point: the member may
live in a generated half of a `partial class` that you cannot decorate. `[ProtoPartialMember(tag,
"Name")]` is `[ProtoMember]`, `[ProtoPartialIgnore("Name")]` is `[ProtoIgnore]`.

Precedence, taken from `Partial.reference.cs`:

- `[ProtoPartialIgnore]` wins over **everything**, including a `[ProtoMember]` the member declares
  itself — `ApplyDefaultBehaviour` tests it before any family or attribute inspection.
- `[ProtoPartialMember]` slots between `[ProtoMember]` and `[DataMember(Order)]`:
  `NormalizeProtoMember` only reaches the partial list when the member did not pin a tag itself, and
  it runs inside the ProtoBuf-family block, so it beats the `[DataMember]`/`[XmlElement]` orders.
- Two declarations naming the same member: the **first** to pin a tag wins (`break` on match).
- `IsRequired`, `IsPacked`, `DataFormat` and `Name` carry over exactly as on `[ProtoMember]`.

**`OverwriteList` used to be refused here, and no longer is.** `MetaType`'s partial branch read it
from `attrib` — the member's *own* `[ProtoMember]`, necessarily null whenever that branch runs —
rather than from `ppma`, so protobuf-net silently ignored it, and accepting it would have made our
reads merge differently from ref-emit's. That was a one-token bug in `MetaType`, fixed on this
branch; both paths honour it now, and `Partial.input.cs` carries a member exercising it.

Worth keeping as a pattern rather than as a fact about this option: **a generator refusal justified
by "the runtime ignores it" is worth re-reading as a possible bug report about the runtime.** This
one sat recorded as settled behaviour for months.

Note the shipped analyzer makes the two *contradictory* shapes build **errors** — `PBN0008` for a
member described by both a `[ProtoMember]` and a `[ProtoPartialMember]`, `PBN0010` for one both
described and `[ProtoPartialIgnore]`d. That is defensible (unlike `PBN0012` on interfaces, it flags a
genuine mistake), so `Partial.input.cs` suppresses them with `#pragma` rather than the analyzer being
changed — pinning a precedence rule requires a contradiction to resolve, so there is no version of
that test the analyzer would allow.

### Three more `[ProtoContract]` options

Each turned out to reuse machinery that was already here, which is why they went in together:

- **`IsGroup`** puts `WireTypeStartGroup` in place of `WireTypeString` in the **contract's own**
  features (`MetaType.GetFeatures`). Note the scope: it is the contract's features, not the member's
  — a member picks its wire type through `DataFormat`, and the two do not interact. It does **not**
  suppress `ThrowUnexpectedSubtype`.
- **`IgnoreUnknownSubTypes`** reaches `TypeSerializer.Init` as `assertKnownType: false`, and that
  flag guards exactly one thing: the `ThrowUnexpectedSubtype` call. So it is the same omission
  `sealed` already gets, asked for explicitly. It applies **per type**, not down a hierarchy — a
  derived contract without the option keeps its own throw — and in a hierarchy it removes the `else`
  arm of the sub-type `is` chain while leaving the chain itself.
- **`UseProtoMembersOnly`** narrows the attribute family to ProtoBuf, so `[DataMember]` and
  `[XmlElement]` orders stop supplying field numbers. `GetContractFamily` returns
  `AttributeFamily.ProtoBuf` outright for it, without inspecting the rest — the identical narrowing
  `ImplicitFields` performs, so it is the same one-line branch.

`ContractOptions.input.cs` pins all three, and carries a `BothFamilies` contract *without* the option
as the contrast — otherwise "only field 3 survives" is indistinguishable from the `[DataMember]`
orders never having been read in the first place.
