# protobuf-net — notes for agents

Only non-obvious things live here; the code is the reference for everything else.

## Layout and build

- The solution is **`protobuf-net.slnx`**. `protobuf-net.sln.old` is a stale leftover — don't use it.
- CI (`.github/workflows/dotnet.yml`) runs on **windows-latest** and builds/tests **`Build.csproj`**,
  a `Microsoft.Build.Traversal` project. It globs `src\*\*.csproj`, so a new project under `src/`
  is picked up by CI automatically — including `net4x`-only ones, which are fine because CI is Windows.
- **Central package management is on.** Add versions to `src/Directory.Packages.props`; leave
  `Version=` off the `PackageReference` in the csproj.

## protobuf-net.BuildTools compiles in its dependencies

`src/protobuf-net.BuildTools/protobuf-net.BuildTools.csproj` does not *reference*
protobuf-net.Core / protobuf-net.Reflection — it **compiles their sources in**
(`<Compile Include="../protobuf-net.Core/**/*.cs" />`), because package references inside analyzers
are painful.

Consequence worth knowing in tests: `typeof(TypeModel).Assembly` resolves to the **BuildTools**
assembly, not protobuf-net. That is deliberate and is what `MetadataReferenceHelpers` relies on.

## Persist-to-dll is .NET Framework only

`RuntimeTypeModel.Compile(string name, string path)` is inside `#if !PLAT_NO_EMITDLL`, and
`PLAT_NO_EMITDLL` is defined for every TFM **except `net462`** (`src/protobuf-net/protobuf-net.csproj`).
On modern TFMs `CompilerOptions.OutputPath` is `[Obsolete]` and a non-empty path throws
`NotSupportedException`. The save path still uses `AssemblyBuilderAccess.RunAndSave`;
`PersistedAssemblyBuilder` (.NET 9+) is not used.

**The trap:** `src/Examples/CoreFxHacks.cs` defines, under `#if COREFX`, an extension method
`Compile(this RuntimeTypeModel, string x, string y) => model.Compile();` that silently discards the
path. So `src/Examples` calls `model.Compile("X", "X.dll")` and compiles cleanly on `net8.0` while
writing no dll at all. Don't infer from that code that persistence works on modern .NET.

If a new TFM is ever needed, prefer **net10.0** (LTS) over net9.0.

## AOT source generator (work in progress)

`ProtoModelGenerator` (`src/protobuf-net.BuildTools/Generators/ProtoModelGenerator.cs`) is an
`IIncrementalGenerator` building compile-time serializers for code-first contracts. It is
deliberately separate from `ProtoFileGenerator` (the `.proto` → DTO path, still `ISourceGenerator`).

Design constraints that are settled, and should not be quietly relaxed:

- The consumer opts in with `[ProtoModel] partial class MyModel : TypeModel`, seeded by
  `[ProtoSerializable(typeof(Foo))]`; everything reachable from a seed is pulled in automatically.
- The trigger attributes are **generator-owned**, emitted via `RegisterPostInitializationOutput`,
  not declared in protobuf-net.Core.
- The model is **closed over what is visible at compile time**. It never consults the runtime model.
  A contract the generator can't handle gets a diagnostic and is omitted — it must *not* fall back
  to ref-emit, since that would silently defeat AOT. `TypeModel`'s inherited "no serializer for
  type X" throw is the intended backstop.
- Dropping a contract must **cascade to its referrers** (`DropUnsatisfiable`, run to a fixed point).
  A contract whose member type was dropped would emit `ReadMessage<T>(..., this)` for a `T` the
  services type does not implement `ISerializer<T>` for, which does not compile.
- **Nothing in `Internal/Aot/` may hold a Roslyn reference** — no `ISymbol`, `Location`, `SyntaxNode`,
  `Compilation`. Doing so causes two silent failures at once: equality becomes reference-based so the
  cache never hits, *and* the model pins the whole compilation graph alive for as long as the driver
  holds it, which is a serious leak in a long-running IDE session. This has sunk previous attempts at
  this work. Roslyn *value* types (`TextSpan`, `LinePositionSpan`) are fine — they are plain data —
  which is why `PlanLocation` stores those and reconstitutes a `Location` only at report time.
  `ProtoModelPlanShapeTests` enforces this by reflection, scoped to the `Internal/Aot` namespace.
  (Analyzer helpers in `Internal/` proper *do* legitimately hold symbols; analyzers aren't cached
  across compilations. That is why the model types have their own namespace.)
- The model types are hand-written equatable values so the driver can cache them; `ImmutableArray<T>`
  is deliberately *not* used, as its equality is reference-based and would defeat caching silently.
  `ProtoModelGeneratorIncrementalTests` asserts both directions: cached on irrelevant edits, and
  *not* cached on real ones, so the test is known to be able to fail.
- Diagnostics are projected through a **separate** `Select` from the plan, because they carry
  locations (which shift whenever anything above them moves) and the plan does not — so the emit
  step stays cached across edits that only move code around.
- **Not everything that changes the wire format is a `ProtoBuf` attribute.** `MetaType.ApplyDefaultBehaviour`
  also honours `System.Runtime.Serialization` (`[DataContract]`/`[DataMember]`, and the
  `[OnDeserialized]` callback family — which live on *methods*), `System.Xml.Serialization`,
  `[NonSerialized]`, and `[DefaultValue]` (which changes the write guard from `!= 0` to `!= default`).
  Worse, the `{Name}Specified` / `ShouldSerialize{Name}()` conventions are matched **by name**, so no
  attribute inspection finds them at all. `IsSignificantAttribute` and `GetConditionalPattern` exist
  to bail on all of these; anything added to `MetaType`'s list must be added there too, or the
  generator will silently emit wrong bytes.
- **Auto-tuples** are a second emit shape, not a variation on the first. The read declares a local
  per constructor parameter (seeded from the incoming value so merge still works), reads into those,
  and calls the constructor at the *end*; the write emits every scalar **unconditionally** — with
  construction-time assignment there is no way to tell "absent" from "default" — and skips
  `ThrowUnexpectedSubtype`. Field numbers are 1..n in constructor-parameter order.

  Detection mirrors `MetaType.ResolveTupleConstructor` and must not drift from it. It engages **only
  when the type carries no contract attribute family at all** (`MetaType.GetContractFamily`) — a
  `[ProtoContract]` on an immutable type *defeats* detection and makes ref-emit produce a serializer
  that finds no members and throws `ThrowCannotCreateInstance`; we drop such types instead, which is
  deliberately better than matching. The rule is "no *public* setter", not "immutable": non-public
  and `init`-only setters are both tolerated, and any type with **"Tuple" in its name** is exempt
  from the read-only demand entirely — which is the only reason `ValueTuple`'s public mutable fields
  qualify. Members include fields as well as properties, the constructor is matched by parameter
  name (case-insensitive) with exact type equality, and **exactly one** constructor may map.
  Closed constructed generics are supported, since `KeyValuePair<K,V>` is the common case.

  `ValueTuple` needs two Roslyn-specific allowances, both found the hard way: its `Item1`/`Item2`
  fields are reported as **`IsImplicitlyDeclared`**, so a filter for that (intended to skip
  auto-property backing fields — which the public-accessibility test already excludes) leaves it with
  no members at all; and its name renders as `(int, string)`, so it must be built with a **tuple
  literal** — `new (int, string)(...)` is not legal C#. Note `TupleUnderlyingType` is *null* for
  these symbols, so it is no help in normalising them.

  Tuple-typed **members** are supported as sub-messages even though they carry no contract
  attribute: `GetMemberShape` falls through to `IsTupleCandidate`, and the closure handles the rest.

  **Element names are accepted from consumers but erased in our output** (`EraseTupleNames`, applied
  recursively and also through enclosing generics such as `KeyValuePair<int, (int A, int B)>`).
  A consumer writing `public (int Id, string Name) Pair { get; set; }` works — be gracious in what we
  accept; erasure governs only what *we* key on and emit. It is free, because the conversion between
  tuple types differing only in names is an *identity* conversion, so `value.Named` passes to
  `WriteMessage<(int, string)>(...)` with no cast.

  Erasure is not cosmetic, and all three reasons were confirmed by probing the symbols rather than
  assumed:
  - **Detection**: a *named* tuple reports four public fields (`Item1, Id, Item2, Name`), which fails
    the constructor-arity match; the erased form reports the two we want.
  - **De-duplication**: `SymbolEqualityComparer.Default` returns **false** between the two spellings,
    and `ToDisplayString` includes the names — so the same shape named two ways would emit
    `ISerializer<(int, string)>` *twice* and fail to compile.
  - **Alignment**: ref-emit works in metadata where names do not exist, so it collapses them for
    free; erasing keeps our serializer set identical to its.
- **Value types are first-class contracts.** A struct needs no construction or null test on read, and
  no `ThrowUnexpectedSubtype` on write (that is constrained to reference types). A struct-typed
  *member* is never null, so neither side tests for it — and unlike a reference-type message,
  `Nullable<TStruct>` **is** expressible and uses `HasValue`/`GetValueOrDefault`.
- **Field numbers have three sources**, in the precedence `MetaType.ApplyDefaultBehaviour` uses:
  `[ProtoMember]`, then `[DataMember(Order)]`, then `[XmlElement(Order)]`/`[XmlArray(Order)]`.
  `[DataContract]` and `[XmlType]` are contract markers in their own right, and the two families can
  be mixed on one type — `[ProtoMember]` wins for its own member while `[DataMember]` supplies the
  rest. `DataMemberOffset` applies **only** to the `[DataMember]` orders, never to the Xml ones. An
  order below 1 means "not declared" (`DataMember.Order` defaults to -1). `[XmlIgnore]` and
  `[NonSerialized]` exclude a member rather than dropping the contract.
- `[ProtoContract(SkipConstructor = true)]` constructs via
  `BclHelpers.GetUninitializedObject(typeof(T))` and additionally implements `IFactory<T>`. Its
  effect is only visible in non-serialized members, which is why the differential tests compare
  simply-typed properties as well as bytes.
- An **enum** is its underlying scalar plus a cast in each direction, compared against
  `default(TEnum)`. `[Flags]` makes no difference to the wire form, and `[ProtoEnum]` only renames
  for schema purposes (`ProtoEnumAttribute.Value` is `[Obsolete(..., error: true)]`), so neither
  needs guarding against. Older protobuf-net supported enum value-aliasing and validity-checking
  (auto-disabled by `[Flags]`, which is why that looks worth testing); that was simplified away in
  line with .proto, and is now *unreachable* rather than merely deprecated — `EnumPassthru`'s setter
  throws on both `ProtoContractAttribute` and `MetaType`. Don't reintroduce support for it. **char** is a `ushort` varint, needing an explicit cast on read. Note the
  CLR permits char-backed enums even though C# cannot declare one (CS1008) — those are refused,
  since the shape cannot be tested from C#.
- Write guards, all three proven against ref-emit rather than assumed: a plain scalar is written when
  `!= <type default>`; a `[DefaultValue]` scalar when `!= <declared>`; a `Nullable<T>` when
  `HasValue` — **presence, not value**, so a nullable zero *is* written where a plain zero is not.
  The two compose by nesting (`if (HasValue) { if (v != declared) ... }`), and `[DefaultValue(null)]`
  means "no declared default". `[DefaultValue]` is write-only — the reader never applies it, so a
  declared default without a matching initialiser is lossy across a round-trip (protobuf-net
  behaviour generally; `PBN0020`/`PBN0021` exist to nag about it).
- **A string `[DefaultValue]` is *parsed*, not converted** — `ValueMember.ParseDefaultValue` branches
  on the member's type before any `Convert.ChangeType`, so `Convert` is the wrong model for it:
  an **enum** goes through `Enum.Parse(type, s, ignoreCase: true)`, i.e. by member *name* and
  case-insensitively (`[DefaultValue("green")]` on a `Shade` means `Shade.Green`); a **char** takes
  `s[0]` and throws unless the string is exactly one character. Both are resolved at compile time —
  the enum by looking the field up on the symbol — since `Convert.ToUInt16("green")` throws and the
  member was previously dropped. `nint`/`nuint` need a cast rather than a suffix, having no literal
  form of their own.
- Every dropped contract must **say why**: `PBN2001` unsupported member, `PBN2002` unsupported
  declaration, `PBN2003` unsupported protobuf-net option, `PBN2004` dropped by cascade. All are
  **warnings**, not errors — an incomplete model still builds, and the runtime "no serializer" throw
  is the backstop; erroring would make the generator unusable while coverage is partial. Anyone
  wanting strictness can escalate via `WarningsAsErrors`.
- **C# 12 is a hard floor.** Below it the generator reports `PBN2000` and emits nothing, rather than
  emitting code that won't compile. Do not add down-level fallbacks: supporting multiple language
  versions multiplies every emitted construct for no benefit to anyone doing AOT. (netstandard2.0
  and net4x default to C# 7.3, so those consumers must set `<LangVersion>` — accepted deliberately.)

AOT generator diagnostics use their own **`PBN2000+`** block: `PBN0001`–`PBN0023` belong to
`DataContractAnalyzer` and `PBN1000+` to `ProtoFileGenerator`'s schema errors. New IDs should be
added to `AnalyzerReleases.Unshipped.md` — note that release tracking is not actually *enforced*
here (the `Microsoft.CodeAnalysis.Analyzers` RS2000 rules are not active), so the table is
documentation rather than a build gate, and it *had* drifted; it is current as of this branch, which
means nothing but review will keep it that way.

Separately, `PBN9001` is not an analyzer diagnostic at all: it is the `[Experimental]` id on
`ProtoModelAttribute`/`ProtoSurrogateAttribute`, so it is an **error** by default and a consumer
opting into the generator must suppress it. Anything that compiles a model programmatically has to
suppress it too — see `src/AotCoverage`.

Note the shipped analyzer still compiles against the low Roslyn baseline (4.3.1), which predates
`LanguageVersion.CSharp12` — hence the numeric constant in `ProtoModelGenerator`. `BuildToolsUnitTests`
carries a `VersionOverride` to 4.8.0 purely so its in-memory compilations can parse C# 12.

**Don't rev the central Roslyn version speculatively.** Only bump it if we genuinely need a modern
Roslyn API we cannot work around — e.g. detecting a language feature we actually use. The old
baseline is what lets `protobuf-net.BuildTools.Legacy` serve very old SDKs; those users are not doing
AOT by definition, so none of the AOT generator's requirements apply to them.

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

- **Packing is a compile-time decision.** The features constant carries `OptionPackedDisabled`, and
  ref-emit simply *omits* it for `[ProtoMember(IsPacked = true)]`. Unpacked is the default; that
  named argument is not supported yet, so we always emit the disabled form.
- The features wire type is the **element's**, not the member's.
- A message element passes `this` as the sub-serializer; a scalar element passes nothing.
- **A repeated enum needs a serializer proxy.** Unlike an inline scalar, `RepeatedSerializer`
  resolves an `ISerializer<TEnum>` *from the model* — ref-emit emits `values, this as
  ISerializer<TEnum>` — so the services type implements `ISerializerProxy<TEnum>` (and `<TEnum?>`)
  returning `EnumSerializer.CreateXxx<TEnum>()`, per `EmitEnumProxies`. Without it the failure is a
  runtime "no serializer for type", not a build error. **A map with an enum key or value is still
  refused**, though it needs nothing more than pointing the same proxy scan at the map plan.
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
- `IReadOnlySet<T>` maps to `CreateReadOnySet` (sic), which only exists in the net6.0+ build of the
  library; the generator checks the symbol is present before emitting a call to it.

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
else did not, which is exactly the shape of bug a partial test would miss. An **enum** on either side is refused for the same reason a repeated enum is: the
serializer is resolved from the model.

A **repeated value is supported**, and is the one place nesting is legal at all:
`TestIfNestedNotSupported` exempts maps, so `Dictionary<int, List<int>>` works where
`List<List<int>>` throws. Ref-emit passes `this as ISerializer<List<int>>` — it resolves one *from
the model* — so we emit `ISerializerProxy<List<int>>` returning the same
`RepeatedSerializer.CreateList<int>()` and pass nothing, exactly as for a repeated enum. The value
wire type is the **element's** (`WireTypeVarint` for `List<int>`), and such a shape is never a valid
protobuf map, so it also picks up `OptionFailOnDuplicateKey`.

A nested **key** is still refused. That one is a limit of the plan rather than of protobuf-net's
reflection path — but note it *does* match the compiled path, which is the more interesting half:

**A compiled model throws on any map whose key or value is a collection.** `Compile(name, path)`
succeeds and emits the member, then the first use throws *"No serializer for type
`Dictionary<string,String>` is available for model X"* — the emitted code passes
`this as ISerializer<Dictionary<string,string>>` and the services type implements
`ISerializer<KeyValuePair<string,string>>`, so the cast is null and resolution falls back to a model
with no entry. The reflection path handles all three shapes. So our repeated and nested map **values**
match reflection and *exceed* the compiled path, and our refused nested **key** matches the compiled
path and falls short of reflection. Item 9 in `docs/aot-findings.md`.

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
"supported but not recommended". `docs/aot-findings.md` has the decoded bytes.

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

Note the level-200 `Guid` path costs four AOT warnings that the other forms do not — see
`docs/aot-findings.md`.

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
either nothing or a `StreamingContext`. `MetaType` reaches non-public ones by reflection and tolerates
more shapes; anything outside our subset is refused rather than mis-called.

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

### Telling our gaps from protobuf-net's

Several refusals are **matches** rather than shortfalls: protobuf-net throws for them too, so there is
no behaviour to reproduce and nothing outstanding. These were established by probing
`RuntimeTypeModel`, and their diagnostics quote what it says so they stop reading as our backlog:

| shape | what protobuf-net does |
| --- | --- |
| no parameterless constructor, no `SkipConstructor` | throws *"No parameterless constructor found"* |
| a member type that is not a contract | throws *"No serializer defined for type"* |
| lone `[NullWrappedValue]` on a non-scalar / non-nullable / with `[DefaultValue]` | throws |
| `[NullWrappedCollection]` on a non-collection | throws *"can only be used with collection types"* |
| `[ProtoInclude(tag, "TypeName")]` | resolves at runtime; throws *"Unable to resolve sub-type"* even for a live type |

Several of those refusals now **name the route** in the diagnostic itself, because "has unsupported
type X" reads as our backlog even where the fix is one attribute away. Every branch is determined
rather than guessed, and the ones that had to be *excluded* were as instructive as the ones added:

- a **parseable** type — by re-asking `GetMemberShape` with `AllowParseableTypes` on;
- **`System.Type`** — by name; ref-emit does serialize it, through `Type.GetType`, which AOT cannot;
- **`DateOnly`/`TimeOnly`** — a recognised type whose `BclHelpers` methods are inside
  `#if NET6_0_OR_GREATER`, so the refusal is about the *reference*, not the type. Saying
  "protobuf-net has no serializer for it" here would be false;
- **no contract family at all** — a match, and much the largest group: protobuf-net throws
  *"No serializer defined for type"* for it too, on both ref-emit paths. Interfaces and delegates
  land here. For a **collection** the question moves one level down to the element, which is how
  `List<ISomething>` gets an answer;
- **nothing for a map**, deliberately: its key and value are separate so there is no one element to
  name, and an enum on either side is a gap of ours, not something protobuf-net refuses. An enum is
  likewise excluded from the "not a contract" test — it needs no attribute and is a scalar by
  another route.

`System.Net.IPAddress` and `System.DateTimeOffset` are the two worth knowing, since both look like
gaps in the sweep's member-type tail and neither is: `IPAddress` is parseable and works under
`[ProtoModel(AllowParseableTypes = true)]` (`Parseable.input.cs` covers it against ref-emit), and
`DateTimeOffset` has **no** protobuf-net serializer at all, so `[ProtoSurrogate]` is the fix for
ref-emit as much as for us (`ModelSurrogate.input.cs`).

**A member type carrying `[DataContract]` or `[XmlType]` is a contract**, exactly as when seeded.
`GetMessageKind` used to recognise only `[ProtoContract]`, so the very same type was emittable as a
seed and "unsupported" one level down — `Examples/NWind`'s `List<OrderCompat>` is the shape that
turned it up. It now asks `HasContractFamily`, matching `MetaType.GetContractFamily`.

**Audit before building.** Three separate features turned out to be already-working or
already-refused when checked against the runtime model rather than assumed from the sweep table —
`System.Uri` (inbuilt), null-wrapped collection elements (supported), enums as contracts (supported).
A category sitting in the drop table is evidence of a *diagnostic*, not of missing capability.

### Not yet supported

**This list is currently empty**, which is a statement about the sweep rather than about protobuf-net:
every remaining refusal either matches ref-emit or is a deliberate AOT decision (`System.Type`), and
each says which in its diagnostic. The two entries that used to be here both went the same way, and
the way is worth remembering:

- **null-wrapping** was real work, and is now the "Null-wrapping" section above;
- **interfaces as members** was *already done* — the bullet outlived the work. Probing found the
  bare-interface cases were refusals that match ref-emit, and the genuinely-untested shapes (a
  derived interface, a closed generic interface, an interface on either side of a map) already
  emitted correctly; they just had no fixture. What the probe *did* turn up was a value-type sub-type
  emitting code that would not compile — a bug, not a gap.

So the honest next step is not "pick the next bullet" but "widen the corpus": `docs/aot-coverage.md`
is the measurement, and the one genuine gap it still shows is a nested map **key** (1 contract).

### Golden-file tests

`src/BuildToolsUnitTests/Aot/` pairs each `Data/*.input.cs` with the exact code it generates
(`*.output.cs`) and the diagnostics it reports (`*.txt`).

**The tests rewrite those goldens in the source tree on every run**, then assert. So:

- a new fixture fails on its first run (nothing to compare against) — re-run, then review `git diff`;
- a behaviour change shows up as a diff to read, not an assertion to appease;
- don't hand-edit a golden to make a test pass — fix the generator and re-run.

`Data/*.cs` files are excluded from compilation via `<Compile Remove="Aot/Data/**/*.*.cs" />` and
copied to the output directory instead.

Fixture conventions:

- **One namespace per fixture** (`namespace AotFixtures.Simple;`). Every fixture is linked into a
  single assembly by both `AotRefGen` and `AotConformanceTests`, so unqualified names would collide.
- `<Name>.input.cs` declares model type `<Name>Model`, and may declare `<Name>Samples.Values`
  (a `public static object[]`) supplying the values the differential tests exercise.
- A sibling `<Name>.langver` file pins the parse language version for that fixture — used to prove
  the `PBN2000` floor fires.
- **A fixture member with `[DefaultValue(x)]` must also be initialised to `x`.** `[DefaultValue]`
  affects writing only, so without the initialiser an empty payload deserializes to the CLR default
  and the round-trip assertion fails — correctly. This has caught out two fixtures so far; it is a
  fixture-authoring rule, not a generator limitation.
- `Data/Diagnostics/**` holds fixtures that exist to produce diagnostics rather than working code.
  The golden tests glob recursively; `AotRefGen` and `AotConformanceTests` deliberately glob only
  `Data/*.input.cs`, so these are excluded from both.

### Differential tests

`src/AotConformanceTests` references the generator as an **analyzer** (`OutputItemType="Analyzer"`)
and links `Data/*.input.cs`, so the generator runs for real during that project's build. It compiles
only `*.input.cs` — never `*.output.cs`, which is a test artefact, and compiling it would mean
testing a stale snapshot instead of the current generator.

Each sample is checked four ways: bytes from the generated model vs `RuntimeTypeModel` must match,
and each model must read what the other wrote. The cross-deserialization is the point — a serializer
that consistently writes the wrong field number round-trips against itself perfectly. Equivalence is
asserted by re-serializing with the reference model rather than via a hand-written deep comparer.

`RepeatedFieldOccurrencesMergeIdentically` covers what round-tripping structurally cannot: merge
behaviour on **repeated occurrences of the same field**. Serialization never emits a duplicated
field, so `AppendBytes` (which *concatenates* byte arrays) and `ReadMessage`'s merge-into-existing
are otherwise untested. It concatenates every sample's payload — itself a valid protobuf message —
to manufacture the duplicates.

### Down-level consumers

`src/DownLevelSmoke` is a **net472** consumer, and exists because `[UnsafeAccessor]` is net8.0+ while
a great deal of real code is not. It is the other end of `AotSmoke`: same generator, no accessors
available, and the property being pinned is *path of least surprise* —

- the shapes that need an accessor (non-public constructor, `init`, non-public setter) are **dropped
  as warnings**, each naming the shape *and* that net8.0 would fix it. Three warnings, zero errors;
- everything else in the model still emits, compiles and round-trips. A down-level consumer gets a
  **smaller model, not a broken build**;
- a dropped contract then throws `InvalidOperationException` on use — `TypeModel`'s "no serializer"
  backstop — which the smoke test asserts, so the failure is loud rather than silent.

Note the project needs `<LangVersion>12.0</LangVersion>` (net4x defaults to 7.3, below the `PBN2000`
floor) and its own `IsExternalInit` polyfill, since net472 cannot even *compile* `init` without one.

An in-memory test was tried first and abandoned: the test process is net8.0, so its reference set
always supplies `UnsafeAccessorAttribute`, and a reference set thin enough to exclude it is too thin
to compile the input (CS0518). Being a real down-level project is the only honest version of this.

### Native AOT smoke test

`src/AotSmoke` is a `PublishAot` console app that round-trips through a generated model and returns
a non-zero exit code on mismatch. It is the only thing here that proves the actual goal; everything
else runs on a JIT runtime where ref-emit still exists.

```
dotnet publish src/AotSmoke/AotSmoke.csproj -c Release -r win-x64
```

`vswhere.exe` must be on `PATH` (`%ProgramFiles(x86)%\Microsoft Visual Studio\Installer`) or ILC's
link step fails with a mangled command line — the error names `link.exe`, which is misleading.

Because `PublishAot` enables trim/AOT analysis at **build** time too, an ordinary `dotnet build` of
this project catches annotation regressions without paying for a native publish. That is how IL2095
was found. Two things it has already caught, both invisible on JIT:

- The generated `GetSerializer<T>` override must restate the base's
  `[DynamicallyAccessedMembers(DynamicAccess.ContractType)]` exactly, or IL2095 fires. `DynamicAccess`
  is internal to protobuf-net, so the emitter spells the flags out — keep them in step with
  `protobuf-net.Core/Internal/DynamicallyAccessedMembersAttribute.cs`, and note the attribute only
  exists on net5+, so the generator probes for it rather than assuming.
- `SerializerCache.Get<TProvider, T>` had **no** annotations while the `SerializerCache<TProvider>`
  it forwards to needs `DynamicAccess.Serializer` to preserve the constructor used by
  `Activator.CreateInstance`. The chain broke at that public boundary, ILC trimmed the constructor,
  and the first serialize threw `MissingMethodException` at runtime. This affected *any* hand-written
  `TypeModel` under AOT, not just generated ones.

### Trim/AOT annotations: which axis they belong on

`[DynamicallyAccessedMembers]` says *"someone will reflect over this T"*. That is a property of **how
a serializer is obtained** (the reflection-based `RuntimeTypeModel` builds one by inspecting the
contract), **not of what a serializer is**. It was originally declared on `ISerializer<T>` and its
siblings, so every consumer paid the runtime model's cost — including generated models, which never
reflect at all. `PrimaryTypeProvider` implements `ISerializer<Type>`, and since `System.Type` is
saturated with `RequiresDynamicCode` members, that single instantiation produced ~180 warnings.

Removing it from the serializer interfaces took the `AotSmoke` publish from **200 warnings to 33**.
Do not reintroduce it there; annotate the reflection entry points instead.

`Requires*` attributes were tried on the dynamic helpers and **reverted** — they do not remove
warnings, they relocate them to callers, and the callers here are
`ProtoReader.State.DeserializeRootImpl<T>`, `TypeModel.CreateInstance<T>` and
`TypeHelper<T>..cctor()` — i.e. the *generic paths generated models use*. The reflective fallbacks
are entangled inside the AOT-safe paths rather than sitting behind a boundary, so `Requires*` has
nowhere clean to terminate. Fixing that properly means **restructuring** (the generic path must not
reference the fallback at all), not annotating.

The remaining 33 are genuinely dynamic: `MakeGenericType`/`Type.GetType`/`Array.CreateInstance` in
the runtime-model and collection paths. Measure with a publish rather than reasoning about them.

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

There is a wrinkle: protobuf-net's own well-known types name the **internal** `PrimaryTypeProvider`,
which a consumer's generated code cannot reference. Those are inbuilt types that
`TypeModel.GetSerializer<T>` resolves without a model, so an *inaccessible* serializer is treated as
"inbuilt": the member passes `null` (which is what resolution does anyway) and the type is not pulled
into the model at all.

#### `[ProtoSurrogate]` on the model

You cannot put an attribute on `System.Uri`, so the contract-level form cannot reach a type you do
not own — which is most of the coverage sweep's member-type tail. `[ProtoSurrogate(typeof(Uri),
typeof(UriSurrogate))]` on the **model** is the compile-time equivalent of
`RuntimeTypeModel.SetSurrogate`, and it is a generator-owned trigger attribute like `[ProtoModel]`
itself, emitted from `RegisterPostInitializationOutput`.

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

The subtle part is that the helper's `ProtoSurrogateAttribute` is a **different type** from the
consumer's — both are generator-owned and `internal`, so each assembly compiles its own copy. What
makes the hand-off work is matching by **full name** rather than by symbol, plus the fact that
`IAssemblySymbol.GetAttributes()` surfaces internal assembly attributes from referenced metadata.
Keep that in mind before "tidying" the comparison into a symbol equality check. Moving the attribute
into protobuf-net.Core would make it one shared type and remove the subtlety, at the cost of the
deliberate rule that trigger attributes are generator-owned.

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
and map-value elements all work. So the three lone refusals (non-scalar, non-nullable,
with `[DefaultValue]`) *match* protobuf-net, which throws while building the model, and are worded to
say so rather than "not supported yet"; every collection form is supported.

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

A UTF-8 fast path for these (`IUtf8SpanFormattable`) is parked in `docs/aot-findings.md` under
"Future ideas" — it is blocked on protobuf-net having no UTF-8 `WriteString` equivalent, and the
read half of the interface pair is implemented by far fewer types than the write half.

### Schema-only options

Several protobuf-net options exist purely to shape the generated `.proto` and never reach the wire.
Refusing a contract over one of those loses a serializer for no reason, and the coverage sweep says
they are common, so each is **accepted and ignored**: `[ProtoContract(Name = …, Origin = …)]`,
`[ProtoReserved]`, and `[ProtoMember(Name = …)]`. `[ProtoIgnore]` excludes the member, like
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

**`OverwriteList` is refused here on purpose.** `MetaType`'s partial branch reads it from `attrib` —
the member's *own* `[ProtoMember]`, which is necessarily null whenever that branch runs — rather than
from `ppma`, so protobuf-net silently ignores it. Accepting it would make our reads merge differently
from ref-emit's; refusing at least says so. Recorded in `docs/aot-findings.md`.

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

### Coverage sweep

`src/AotCoverage` runs the generator over every `[ProtoContract]` in the already-built
`protobuf-net.Test`, `Examples` and `protobuf-net.Reflection.Test` assemblies and tallies what it
could and could not handle, grouped by reason. It exists so that "what should the generator support
next" is answered by counting real contracts rather than by guessing; `docs/aot-coverage.md` is the
last snapshot. Build those three projects first — it seeds from **metadata**, not source.

Two artefacts to know about: it can only seed types a `typeof(...)` in another assembly can name, so
non-public and open-generic contracts are reported as "not seedable" rather than analysed; and
because it flattens every dll beside the targets into one reference set, `CS0433` in its output means
two scanned assemblies declare the same type name, not a generator fault. That one is now reported
under its own "harness artefact" heading rather than counted as a generator bug, so the "does the
emitted code compile" line means what it says.

It writes to stdout and the snapshot carries a hand-added header line, so regenerating it is
`{ header; dotnet run --project src/AotCoverage; } > docs/aot-coverage.md`, not a plain redirect.

### Differential sweep (the corpus, on bytes)

`src/AotDifferential` is the coverage sweep's other half. `AotCoverage` proves the generated code
**compiles**; this one *runs* it, comparing bytes against `RuntimeTypeModel` for a populated instance
of every contract. That is the property that actually matters — every serious bug this generator has
had (the `DataFormat` cast that mis-mapped, the BCL element wire types, `OverwriteList` on a bytes
member) compiled perfectly and wrote the wrong bytes. `docs/aot-differential.md` is the last snapshot.

Three things about it are load-bearing:

- **The generator is loaded reflectively, not referenced.** BuildTools compiles in protobuf-net.Core's
  sources, so referencing its output alongside protobuf-net would make every type in Core ambiguous.
  The project reference is `ReferenceOutputAssembly="false"` (to force the build order) and the dll is
  `Assembly.LoadFrom`ed at runtime, talked to only through Roslyn's interfaces — so the two copies of
  Core never meet.
- **One reference model holds the whole corpus**, not a fresh one per contract. The generated model is
  a closed world over everything at once; a reference that has heard only of the type under test is a
  *differently configured* model, not ref-emit. An implementation whose hierarchy root is an interface
  is a standalone contract until the root is also present — which showed up as 11 phantom mismatches
  before it was fixed. Everything must be added **before** anything is serialized, since protobuf-net
  refuses to change a model once a serializer has been generated from it.
- **Values are deterministic and every scalar differs from the last.** Two members holding the same
  value serialize identically under either numbering, so a swapped field number would be invisible.

The `Filler` builds instances by reflection, and what it *cannot* build is reported rather than
hidden: `Span<byte>`-shaped members can't be boxed at all, and a few types have no construction route.
Coverage is the honest denominator — "of the N actually compared".

**It gates CI.** The run exits non-zero when any contract disagrees on the wire, and
`.github/workflows/dotnet.yml` runs it after the traversal build (which already produces everything
it scans, so there is no extra build cost). Only *mismatches* fail: the other buckets - contracts the
`Filler` cannot instantiate, and deliberately-invalid fixtures one model refuses - are known and
non-zero, and gating on them would bake today's numbers in as correct rather than as under review.
The gate was verified to be able to fail, by shifting an emitted field number and watching it catch
116 contracts, not merely by observing it pass.

`CS0433` is handled rather than tolerated here, since an ambiguous type name breaks the whole compile
rather than one contract: the clashing assembly that is *not* a scanned target is dropped and the
build retried, with the pair read out of the diagnostic rather than hard-coded.

### Reference output from ref-emit

`src/AotRefGen` (net472, hence the section above) exists so the generator's expected output is
*derived* rather than guessed. It links `BuildToolsUnitTests/Aot/Data/*.input.cs` directly, runs
them through `RuntimeTypeModel`, persists the compiled model, decompiles it with
`ICSharpCode.Decompiler`, and writes `*.reference.cs` beside the fixture. Run it after adding or
changing a fixture; the tests don't consume its output, it's a reviewing aid.

`*.reference.cs` is **intentionally tracked in git** (not generated-and-ignored), so that changes in
ref-emit behaviour show up in review.

**A reference is only evidence if it was generated from the input beside it**, and the dangerous
direction is an *absence*: a file that was never re-run and a ref-emit that genuinely emitted nothing
look exactly alike. That has already produced one wrong conclusion — a recorded "the persisted path
silently drops a map-of-map member" bug that turned out to be a fixture edited without re-running
`AotRefGen`; see the "Retracted" section of `docs/aot-findings.md`. Regenerate before concluding
anything from a member that is missing, and commit the regenerated file in the same commit as the
fixture change so the two cannot drift.

- Fixture convention: `<Name>.input.cs` declares model type `<Name>Model`, giving `<Name>.reference.cs`.
- Contract types in fixtures must be `public` — full ref-emit compilation only reaches public members.
- `src/AotRefGen/TriggerAttributes.cs` duplicates the generator's post-init attributes so the shared
  fixtures compile; keep the two in step.

Two artefacts of decompilation are cosmetic, not semantic: `Features` appears as a uniquely-named
method plus an ILSpy `.override` note (it's really an explicit-interface property), and
"Error decoding local variables" reflects ref-emit's empty locals signature.
