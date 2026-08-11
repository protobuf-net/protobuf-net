# Findings from the AOT generator work

Things turned up while building `ProtoModelGenerator` that are **about protobuf-net itself**, not
about the generator. Kept here so they can become issues rather than being lost in commit messages.

Each was found by deriving the generator's expected output from ref-emit (`src/AotRefGen`) or by the
native-AOT smoke test (`src/AotSmoke`) — i.e. by comparison, not by reading the code and guessing.

## Handover: what to validate on Windows

Everything below was developed on Linux. These are the checks that **could not be run here**, in the
order I would run them, with what "good" looks like. Nothing here is expected to fail — but each is a
place where "it works" is currently my inference rather than an observation.

**1. CI on the branch, first.** It has been red twice today and both times it was my mistake, so treat
a green tick as the precondition for the rest rather than a formality. The most recent changes touch
`Extensible` and `MetaType`, which the **net472** leg exercises through different facades than net8.0.

**2. `dotnet run --project src/AotRefGen`** — the one piece of owed work.

It writes `*.reference.cs` beside every fixture, so:

- **two new files should appear**: `Keywords.reference.cs` and `DynamicCategory.reference.cs`. Both
  fixtures were added here and neither involves anything ref-emit refuses, so an *empty* or absent
  result for either is a finding, not a formality — see the table in `AGENTS.md` for which fixtures
  legitimately have none.
- **`git diff` over the others is itself the check.** It regenerates all of them, so any change to an
  existing reference means ref-emit's behaviour moved under something done here. Read it before
  committing; that diff is the whole reason these files are tracked.

**3. `dotnet pack src/protobuf-net/protobuf-net.csproj -c Release`** — a full-TFM pack, which cannot
run on Linux because `net462` will not build here. I verified the packaging with
`-p:TargetFrameworks=net8.0` only. Confirm the nupkg contains:

```
analyzers/dotnet/cs/protobuf-net.BuildTools.dll
build/protobuf-net.props
```

The props **must** be named after the package or it is not auto-imported, and the auto-import is what
makes `ProtoBufDisableBuildTools` work. Worth actually consuming the package once from a scratch
project rather than reading the zip, which is how it was checked here.

**4. `dotnet publish src/AotSmoke/AotSmoke.csproj -c Release -r win-x64`** (`vswhere.exe` on `PATH`).
Expect the smoke test to pass and **19 warnings**, the same count as linux-x64. The *binary size* will
differ and is not comparable across RIDs; compare against a win-x64 baseline if you want a number.

**5. Optionally, `-p:ReportAnalyzer=true` on a large build.** The analyzer's build cost was measured
here as *below the noise floor* (see next-steps item 3) — but by wall clock, which bounds it rather
than isolating it. That switch produced no output in this SDK; if it works on Windows it would turn a
bound into a number, and the IDE's analyzer performance view is another route.

**6. The full suite, including the net472 legs** — `dotnet test Build.csproj`. Three behaviour changes
here are worth a specific eye, because each changes what existing code does:

- the model's constructor is now non-public, so `new MyModel()`, `Activator.CreateInstance(type)` and
  DI construction all break. Every in-repo consumer was migrated; a consumer outside is not;
- `[ProtoPartialMember(OverwriteList = true)]` is now honoured, so such a member *replaces* rather
  than appends. Anyone who set it and never noticed it doing nothing gets a behaviour change;
- `Extensible.AppendValue`/`GetValue` keep `TValue` rather than boxing. The net472 leg is the one I
  could not exercise.

If the intermittent `Issue1232` failures reappear (item 5), note the conditions — they have not been
reproduced in a dozen attempts since, and a sighting with context would be worth more than the
sighting itself.

## Open

Several entries below are resolved and kept for the reasoning rather than the status, so here is the
index. **Still outstanding, and candidates to raise against protobuf-net:** 3 (one trim
warning that needs the annotation on every consumer; see Future Ideas A2 for the measured
breakdown), 5 (intermittent test failures, not reproduced since), 10 (assorted API surprises), 11 (a
compiled model throws on a collection-typed map key/value). **Resolved in place:** 1 (the sibling
sub-type stack overflow, now a catchable exception), 2 (`AppendValue`, which now *works* under AOT
rather than merely failing loudly), 7 (`OverwriteList` on a partial
member, a one-token bug in `MetaType`), 4 (a misplaced annotation on the transport type parameter — 808 KB
of the native binary, the largest single win here), 6 and 9 (analyzer false positives, fixed here), 8 (was the
harness — and was masking a real bug), 12 (`CategoryScalar` serializers, now supported), 13 (the
differential's status, currently clean).


### 1. Merging incompatible sibling sub-types overflows the stack

**Severity: high** — it is an unrecoverable process kill, reachable from untrusted input.

A payload carrying the same field twice with two *different* sub-type markers (a `Dog` then a `Cat`
under a common `[ProtoInclude]` base) sends `SubTypeState<T>.Cast` into `Merge`, which calls
`Model.Serialize<object>` / `Deserialize<T>` and recurses without bound. `StackOverflowException`
cannot be caught, so the process dies.

Reproduces with `RuntimeTypeModel` alone — no generated code involved:

```
Dog then Dog:   ok -> Dog
Dog then Puppy: ok -> Puppy
Puppy then Dog: ok -> Puppy
Dog then Cat:   *** exit -1073741571 (0xC00000FD, stack overflow)
```

Only incompatible siblings do it; same-branch merges are fine in either direction. Anyone
deserializing untrusted protobuf into a type with `[ProtoInclude]` is exposed. `Inherit.input.cs`
keeps its `Holder` samples on one branch because of this.

### 2. `Extensible.AppendValue` under AOT — **fixed twice, and the second time properly**

**First pass:** the result of `TrySerializeAuxiliaryType` was discarded and `commit = true` set
regardless, so a write that never happened was committed as success — silent data loss on an API whose
whole purpose is round-trip fidelity, and the *normal* outcome under native AOT, since that path
resolves the serializer reflectively. Checked now, so a failure throws.

**Second pass — it now works, rather than merely failing loudly.** The type was never actually
unknown: every public entry point is generic (`AppendValue<TValue>`, `GetValue<TValue>`,
`TryGetValue<TValue>`), and `TValue` was being *thrown away* by boxing to `object` and asking
`TrySerializeAuxiliaryType(type: null)` to work it out again by reflection. Keeping `TValue` all the
way down — `ResolveSerializer<TValue>` plus `WriteAny`/`ReadAny` — resolves through the model, which
is exactly what a generated model answers.

Both halves had to move: an API you can append to but cannot read back from would be worse than one
that refuses, so the read path is typed too. Restricted to `DataFormat.Default`, since any other
format selects a wire type that the serializer's own features would otherwise supply; those keep the
reflective path, and now report rather than losing the value. The genuinely untyped legacy overload
(`AppendValue(model, instance, tag, format, object)`) likewise.

`AotSmoke` asserts the **round-trip**, unconditionally, and that strictness is what caught the first
attempt: the typed call had been added inside the *non-generic* overload, so `TValue` was `object`,
`ResolveSerializer<object>` failed, and it fell through to the reflective path — invisible under JIT,
where the fallback works. The earlier "either it works or it throws" assertion would have passed.

The other thing the suite caught: going straight to the extension object skipped the argument
validation the untyped overload performs, so `Examples.Extensibility`'s invalid-tag tests got an empty
result where they expected `ArgumentOutOfRangeException`. Repeated in the typed path.

### 3. `SubTypeState<T>.Cast` → `Merge` keeps one trim warning alive

`IL2091`: `Merge` calls `TypeModel.Deserialize<T>`, which wants `DynamicAccess.ContractType` on `T`.
Fixing it means annotating the **class**'s `T`, which pushes the requirement onto every consumer of
`SubTypeState<T>` — including the generated path, which never reflects. Left alone deliberately;
it needs the same restructuring as the other reflective fallbacks (see AGENTS.md).

### 4. `IProtoInput<TInput>`/`IProtoOutput<TOutput>` annotated the **transport** — 808 KB

**Severity: high for anyone publishing native AOT** — it was 22% of the binary. Fixed here.

```csharp
public interface IProtoInput<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TInput>
public interface IProtoOutput<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TOutput>
public interface IMeasuredProtoOutput<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TOutput>
```

`TInput`/`TOutput` is the **transport** — `Stream`, `byte[]`, `ArraySegment<byte>`,
`ReadOnlyMemory<byte>`, `ReadOnlySequence<byte>`, `IBufferWriter<byte>` — and `TypeModel` implements
nine instantiations of them (`TypeModel.InputOutput.cs`). Nothing reflects over a stream. The
contract type is `T` on `Deserialize<T>`/`Serialize<T>`, and *that* one was never annotated: the
attribute was simply on the wrong parameter, on a name that reads like a contract type and is not
one.

`DynamicAccess.ContractType` demands constructors, methods, fields, properties **and nested types**,
so ILC had to keep all of them, on every transport type, transitively. Measured from the ILC
dependency graph: **1738 reflectable framework members** — 1411 methods, 262 fields, 65 nested types
— and **not one protobuf-net member among them**. `Stream`'s async internals drag in `Task` and its
whole promise family, which is why `Task` alone accounts for 269:

| | members kept |
| --- | ---: |
| `System.Threading.Tasks.Task` (+ namespace) | 520 |
| `System.Array` | 160 |
| `System.Enum` | 99 |
| `System.IO.Stream` | 72 |
| `System.Buffers.ReadOnlySequence<T>` | 50 |
| `ArraySegment<T>`, `Queue<T>`, `ManualResetEventSlim`, … | the rest |

Removing the three annotations: `AotSmoke` **34 → 20** warnings and **3,685,888 → 2,858,496 bytes**,
i.e. **808 KB / 22.4%** of the native binary. Round-trip unchanged, corpus differential unchanged.

The 14 warnings this had been producing were `Enum.GetValues(Type)` (×12) and four
`Array.CreateInstance` overloads — *public statics of `System.Enum` and `System.Array`*, kept
**reflectable** rather than called, which is why they carried no source location and why they moved
whenever an unrelated annotation was removed. Three of the four `Array.CreateInstance` overloads
protobuf-net never calls at all.

**This is the same bug as the `ISerializer<T>` one** in "Fixed on this branch" — an annotation on a
type parameter that describes *how a serializer is obtained* rather than *what is reflected over* —
and it survived that cleanup precisely because `TInput` does not look like a contract type
parameter. Worth a standing rule: before annotating a type parameter, ask what would reflect over
it. For a transport, nothing does.

Two earlier explanations of these warnings were **wrong**, and are recorded because the second one
looked convincing:

- *"They arrive through a dependency reached by a static constructor."* No cctor is involved. The
  origin was a single graph root, `Dataflow analysis for type definition ProtoBuf.Meta.TypeModel`,
  and a `.cctor` origin would have printed as `TypeModel..cctor()`.
- *"It is `TypeModel`'s `Type`-based public API"* — the ~28 `[DAM(ContractType)] Type type`
  parameters on `Deserialize`/`SerializeWithLengthPrefix`/`GetSchema`/etc. Plausible, and false:
  stripping all 28 moved the total 34 → 33 and left `IL3050` at 21 and the dataflow node at exactly
  1738 edges. Worth knowing before anyone re-proposes routing that API through a generated switch to
  fix this — it would not have.

**How to trace this** rather than guess, which is what finally settled it:

```
dotnet publish src/AotSmoke/AotSmoke.csproj -c Release -r win-x64 /p:IlcGenerateDgmlFile=true
```

gives `obj/…/native/AotSmoke.scan.dgml.xml`. Find the node for the offending member
(`<Node Id="…" Label="Reflectable method: …Enum.GetValues(Type)"`), walk *incoming* `<Link>` edges to
the root, and read the edge's **`Reason`** attribute — it names the generic parameter responsible.
Here every one of the 1738 said `Reason="TInput"`, which is the whole answer in one string.

### 4b. Collection members throw under native AOT — the constructor is trimmed

**Severity: high for native AOT** — any `HashSet<T>`, `Queue<T>`, `Stack<T>`, `SortedSet<T>` or
concurrent-collection member failed to *deserialize*. Found by widening `AotSmoke`; fixed here.

```
ProtoException: No parameterless constructor found for HashSet<string>
 ---> MissingMethodException: No parameterless constructor defined
   at TypeModel.ActivatorCreate[T]()
   at RepeatedSerializer`2.ReadRepeated(...)
```

When a collection member arrives null, the repeated serializers construct one through
`TypeModel.ActivatorCreate<TCollection>()`, i.e. `Activator.CreateInstance(type, nonPublic: true)`.
Generated code never names those constructors — it calls `RepeatedSerializer.CreateSet<HashSet<string>, string>()`
and the construction happens inside the library — so ILC trimmed them. Serialization was fine;
the first *deserialize* into a null member threw.

`TCollection` had never carried an annotation, on any of the factories or the concrete serializers,
so this predates the trim work on this branch. It is the `IL2087` on `TypeModel.ActivatorCreate<T>`,
which had been catalogued in A2 as "a correct warning about code that genuinely reflects" — correct,
and it was describing a live bug rather than an accepted cost. **A warning classified as
"expected" is still a warning about something.**

The fix is an annotation, which is the opposite direction from the rest of this work and is right
here for the reason the rest was wrong: something genuinely *does* reflect over `TCollection`. It is
deliberately the narrow `DynamicAccess.Activated`
(`PublicParameterlessConstructor | NonPublicConstructors`) rather than `ContractType` — the
collection is constructed, never inspected — applied to `TypeModel.ActivatorCreate<T>`, the concrete
serializers that call it (`Set`/`Queue`/`Stack`/`List`/`Enumerable`/`Dictionary`/`ProducerConsumer`
and the three concurrent ones) and the public factories that name them.

Two things worth keeping:

- **`List<T>` worked by luck.** Its constructor survives because application code elsewhere calls
  `new List<T>()`; nothing in protobuf-net kept it. So the pre-existing `List<T>` coverage in
  `AotSmoke` proved nothing about this whole class of member.
- **The annotation is self-locating once it flows.** Fixing `HashSet` surfaced a *new* `IL2091`
  naming `ProducerConsumerSerializer.Initialize` exactly, which was the next failure
  (`ConcurrentQueue<int>`) before it happened. Two rounds, no guessing.

Also relevant to plain `PublishTrimmed` without AOT, where the same constructors can be trimmed.

### 5. `Issue1232` fails **intermittently** — and every sharper claim about it has been wrong

Four `StreamSerializer_RootStream` cases, `trySkipWritingWhenMeasuring: True` in every one, failing
with `InvalidOperationException: Invalid length; expected 988, actual: 1029` (size 1023) / `1033`
(size 1025).

**Read the history of this entry before theorising about it again**, because it has now been wrong
twice in two different directions:

- it once said `NonRootStream`, which is the wrong test — they are `RootStream`;
- it then said "pre-existing, so CI must be red", and CI is **green**: the job log for the last run
  on `main` (`85e8e39f`, run `30191810999`) has nine "Test Run Successful", zero failures, and these
  very cases listed as `Passed` on both TFMs;
- it then said "**Linux-only**", on the strength of two consecutive failing runs here against a
  green Windows CI. That does not survive either. The same four cases now pass on `main` on the same
  Linux machine — 6 warm runs, 2 cold runs with `obj`/`bin` deleted, and a full-project run — and
  pass on the `codegen` branch too.

So the honest status is **intermittent**, observed twice and not reproduced since, which puts it
beside the other intermittent already recorded here (the net472 `Examples` failure). Two runs is not
a platform.

The one datum worth keeping is the number: the *expected* length is **988 for both input sizes**. It
does not vary with the input, where the test's own `Measure` is plain arithmetic over `value.Length`
and would give 1030 for size 1025. So the measured length is not derived from the value being
written — which is consistent with leaked or cached state, and state that is sometimes stale is
exactly the shape of thing that fails intermittently. `MeasureState`'s length cache is the obvious
place to look; recent commits on `main` touch it.

Not diagnosed further: no AOT content, and nothing on this branch reaches `Measure`.

### 6. `PBN0015` is a false positive on a surrogated type — and it is an **error**

**Fixed on this branch**, but worth calling out separately because it affects the *shipped analyzer*,
not the AOT work.

`DataContractAnalyzer.ConstructorMissing` fires when a `[ProtoContract]` has constructors but no
parameterless one, and `SkipConstructor` is not set. It did not consider `Surrogate`. But with a
surrogate protobuf-net constructs the **surrogate** and converts — the type itself never needs a
constructor, and surrogating an immutable type is precisely the canonical use.

Severity is `DiagnosticSeverity.Error`, so this **fails the build** for anyone doing that. Found by
adding an immutable surrogated type to a fixture and watching `AotConformanceTests` refuse to
compile. The fix is a new `DataContractContextFlags.HasSurrogate`, set from the attribute and
checked alongside `SkipConstructor`.

### 7. `[ProtoPartialMember(OverwriteList = ...)]` is silently ignored — **fixed**

**Severity: low** — one option on one attribute, but silent, and it changes read-merge behaviour.

`MetaType.NormalizeProtoMember`'s partial-member branch reads every option off `ppma` (the
`[ProtoPartialMember]` being examined) *except* two, which it reads off `attrib`:

```csharp
GetFieldBoolean(ref isPacked, ppma, "IsPacked");
GetFieldBoolean(ref overwriteList, attrib, "OverwriteList");   // <-- attrib, not ppma
GetDataFormat(ref dataFormat, ppma, "DataFormat");
GetFieldBoolean(ref asReferenceHasValue, attrib, "AsReferenceHasValue", false);   // <-- and here
```

`attrib` is the member's own `[ProtoMember]`, and that branch only runs when the member has **no**
`[ProtoMember]` that pinned a tag — so `attrib` is null every time it is read there and the option is
discarded. `IsPacked` and `DataFormat` on the same attribute work fine, which is what makes it read
as a copy-paste slip rather than a decision.

Consequences are confined to reading: `OverwriteList` selects replace-over-append for a collection or
a `byte[]`, so a caller who set it still gets append. The generator refuses the option there rather
than honouring it, since honouring it would disagree with ref-emit.

### 8. A `[ProtoMember]` private field is lost when the contract is read from *metadata*

**Resolved: a harness bug, and it was masking a real generator bug.** Kept in full because the
shape of the mistake is worth remembering, and because the resolution changed the sweep's numbers.

`MetadataImportOptions` defaults to **`Public`**, so private members of a *metadata reference* are
never imported: `GetMembers()` does not return them and nothing reports their absence. Both sweeps
built their compilations with the default, so every contract whose members are non-public emitted a
serializer with no members and still counted as "emitted". Both now set
`MetadataImportOptions.All`.

Two consequences, and the second is why this was worth chasing rather than caveating:

- **The corpus numbers were overstated.** Dropped went 87 → 93 once the members were visible, mostly
  as `member '…' is not public` — a legitimate refusal that had simply never been reached. Worse
  numbers, truer ones.
- **It was hiding a generator bug that does not compile.** With private members visible,
  `Examples.Issues.Issue295.Asset` produced `CS1503`: `ReadSubType` hoists the instance into a
  per-case local, because reading `value.Value` is what constructs it — but `Assign` hard-coded the
  literal `value` for the `[UnsafeAccessor]` target, which *there* is the `SubTypeState<T>` wrapper
  rather than the contract. Any non-public member inside a hierarchy broke the consumer's build.
  Nothing covered that combination; `InheritAccessor.input.cs` does now.

The original write-up follows, since the reasoning that led here is the useful part.

---

**Severity: high** — silent data loss, and the generated code compiles.

`Examples.ProtoWithFields` (a `[ProtoContract]` whose only members are two `[ProtoMember]`-attributed
**private fields**, each wrapped by a public property) produces a serializer whose `Write` body is
*empty*:

```csharp
void ISerializer<Examples.ProtoWithFields>.Write(ref ProtoWriter.State state, Examples.ProtoWithFields value)
{
    TypeModel.ThrowUnexpectedSubtype(value);
}
```

`RuntimeTypeModel` writes `08-01-12-02-73-31` for the same instance. Nothing is reported — the
members are *skipped*, not refused, which means they fall out at `if (fieldNumber is null) continue;`
rather than at any of the accessibility checks below it.

**It does not reproduce from source.** A fixture with byte-identical declarations emits both members
correctly, so this is specific to reading the contract from a *metadata* reference, which is how
`AotCoverage` and `AotDifferential` drive the generator and is **not** how a real consumer does. That
makes it plausibly a harness artefact rather than a generator bug — but it is not yet established
which, and the failure mode (an empty serializer, no diagnostic) is bad enough either way that it
should not be assumed benign.

*(It was the harness — `MetadataImportOptions`. `PBN_MEMBERS=<type>` on `src/AotDifferential`, added
to settle it, prints the members and attributes the generator actually sees for a contract; the
answer was two properties and no fields at all.)*

Reproduce with `PBN_DUMP='ISerializer<global::Examples.ProtoWithFields>.Write' dotnet run --project
src/AotDifferential`, which prints the generated source around any matching line.

### 9. `PBN0003` is a false positive on a generic hierarchy root — and it is an **error**

**Fixed.** The include numbering space is now split per closed construction, so two includes may
share a tag when they belong to different constructions, while a member-versus-include clash and two
includes on the *same* construction still report. Three tests pin those directions; note the includes
are walked before the members, so the two spaces cannot be maintained by snapshotting one into the
other — the first attempt did exactly that and broke the member case.

The fourth of this family, after `PBN0015` (surrogates) and `PBN0012` (interfaces, then generics).

A generic base declares its `[ProtoInclude]` list once, and that list is shared by every closed
construction — but each construction only ever matches the includes that actually derive from it. So

```csharp
[ProtoContract, ProtoInclude(1, typeof(ShipHolder)), ProtoInclude(1, typeof(CrateHolder))]
public class Holder<T> : Node { }
public class ShipHolder : Holder<Ship> { }
public class CrateHolder : Holder<Crate> { }
```

is unambiguous — `Holder<Ship>` sees exactly one sub-type at tag 1 — and ref-emit serializes it.
`PBN0003` counts tags across the whole list and reports a build **error**. `Examples/Issues/SO9408133.cs`
is the shape from a real report, so this is not contrived.

`GenericHierarchy.input.cs` no longer needs its `#pragma`.

### 10. Assorted API surprises

Not bugs exactly, but each cost time and each is a trap for callers:

- **A `[ProtoContract]` that resolves as a collection is serialized as one, and its own members are
  ignored entirely** — silently. `IgnoreListHandling = true` is the opt-out. Nothing warns.
- **A derived `[ProtoContract]` whose base does not `[ProtoInclude]` it is an independent contract
  that silently ignores every inherited member.** Also nothing warns.
- **An interface root writes its own declared members *in addition to* the implementation's**, so a
  property declared on both goes on the wire twice. `[ProtoContract] interface IAnimal` declaring
  `[ProtoMember(1)] Name`, with `Dog` implementing it and declaring `[ProtoMember(1)] Name` itself,
  serializes `Name` once inside the `Dog` sub-type layer and again in the `IAnimal` layer:
  `0A-0E · 52-07(0A-03 "rex" · 10-03) · 0A-03 "rex"`. Consistent with "a layer only sees its own
  declared members" — the interface property and the implementing property genuinely *are* different
  members — but it is not what anyone writing that contract intends, and nothing warns.
- `IProducerConsumerCollection<T>` resolves to a provider that can be written but **not read** —
  deserialize throws, because there is no concrete type to construct.
- `RepeatedSerializer.CreateReadOnySet` is missing an "l" — public API, so presumably stuck.

### 11. A compiled model throws on a map whose key or value is a collection

**Severity: medium** — the model compiles clean and fails on first use, so it is a deployment-time
failure rather than a build-time one. Confirmed on both halves of `Compile(name, path)`.

| shape | `RuntimeTypeModel` | persisted dll |
| --- | --- | --- |
| `Dictionary<string, Dictionary<string, string>>` | ok, 13 bytes | **compiles, then throws on use** |
| `Dictionary<string, List<int>>` | ok, 9 bytes | **compiles, then throws on use** |
| `Dictionary<List<int>, List<string>>` | ok, 12 bytes | **compiles, then throws on use** |

```
InvalidOperationException: No serializer for type
  System.Collections.Generic.Dictionary`2[System.String,System.String] is available for model X
```

The compiled serializer emits the member and passes `this as ISerializer<Dictionary<string,string>>`
for the value serializer. The generated services type does not implement that interface — it
implements `ISerializer<KeyValuePair<string, string>>` — so the cast yields null, resolution falls
back to the model, and the model has no entry for it.

**This entry supersedes two earlier wrong ones, and the way both went wrong is the point:**

- `0ef7af2c` recorded it as *silent data loss* — "the compiled model emits no code at all for it".
  That was read off a `MapNested.reference.cs` that had never been regenerated after the member was
  added to the fixture, so the absence was the harness, not ref-emit.
- The correction to that then claimed the persisted path **handles** the shape, on the strength of
  regenerating the reference and seeing field 3 appear in the read and the write. Also wrong, and
  wrong in a more instructive way: `AotRefGen` only *compiles and decompiles*, it never runs the
  model. Emitted code is not working code, and here the difference is exactly the bug.

So the rule that "a reference is only evidence if it was generated from the input beside it" is
necessary but not sufficient. `*.reference.cs` answers *what ref-emit emits*; it cannot answer
whether the result runs. For anything where the two could differ, run it.

For the generator this means: our support for a repeated or nested map **value** matches the
reflection path and exceeds the compiled one, and our refusal of a nested map **key** matches the
compiled path while falling short of the reflection one.

### 12. A `CategoryScalar` hand-written serializer is emitted as a sub-message

**Fixed.** `ProtoContractAttribute.IsScalar` was added as the metadata-readable escape hatch, the
`Features` declaration is folded from source where it is available, and the contract is refused with
advice where neither settles it. See "Hand-written serializers" in AGENTS.md; the original analysis
follows because the *reason* it could not simply be looked up is the durable part.

**Severity: high** — wrong framing on the wire, and the two known cases *throw* rather than
silently disagreeing, which is the only reason they were noticed.

`[ProtoContract(Serializer = typeof(X))]` says a type has a hand-written serializer. We assume every
such contract is a **message** and frame a member of it with `WriteMessage`/`ReadMessage`. But the
serializer declares its own category, and it may be a *scalar*:

```csharp
[ProtoContract(Serializer = typeof(Serializer))]
public readonly struct CustomType
{
    public class Serializer : ISerializer<CustomType>
    {
        public SerializerFeatures Features
            => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;
        ...
    }
}
```

Ref-emit frames that as the serializer asks — taken from a derived reference, not guessed:

```csharp
// scalar category                          // message category, for contrast
state.WriteFieldHeader(2, WireType.Variant);
SerializerCache.Get<S, T>().Write(ref state, v);   state.WriteMessage(1, CategoryRepeated, v, SerializerCache.Get<S2, T2>());
v = SerializerCache.Get<S, T>().Read(ref state, v); v = state.ReadMessage(CategoryRepeated, v, SerializerCache.Get<S2, T2>());
```

Two corpus contracts hit it — `ProtoBuf.Test.Issues.Issue598+Item` and
`ProtoBuf.Issues.Issue1083+WithWrapping` — and both throw *"Invalid wire-type"* on serialize, so the
damage is loud rather than silent. A scalar serializer whose wire type happened to be `String` would
disagree quietly instead.

**Why it is not fixed here.** The category lives in the serializer's `Features` property, which
ref-emit obtains by *instantiating* the serializer at model-build time. A source generator cannot.
The options are a design decision rather than a bug fix:

- **Read it from source.** When the serializer is in the same compilation its `Features` body is
  usually a constant expression (`CategoryScalar | WireTypeVarint`), and `GetConstantValue` folds it.
  That covers both corpus cases and any consumer writing their own — but not a serializer arriving
  through a metadata reference, which would still have to be refused.
- **Refuse whenever the category cannot be established.** Safe and honest, but it withdraws support
  from the message case that works today (`ExternalSerializer.input.cs`) whenever the serializer is
  not in source.

Either way the current behaviour — assume message, emit, and let it throw at runtime — is the one
option that should not survive.

### 13. The corpus differential's remaining disagreements

**This entry exists because `docs/aot-differential.md` is generated and overwritten on every run.**
It is a snapshot, not a backlog. Read the snapshot for current numbers.

**There are none.** Every bucket is either zero or a category where comparison is impossible by
definition:

| outcome | count | |
| --- | ---: | --- |
| bytes match ref-emit | 2988 | 1283 of them hand-written, the rest `.proto`-generated |
| bytes differ | 0 | CI fails if this regresses |
| one model threw | 0 | was 19 at the start of the audit |
| both threw | 1 | a match — protobuf-net refuses it too |
| no instance could be built | 0 | |
| no instance *can* exist | 5 | abstract contracts with no `[ProtoInclude]`; nothing to compare, ever |
| the reference model refused | 0 | |

That last distinction is deliberate. "Could not build an instance" is a `Filler` limitation and so
genuinely unmeasured; "no instance can exist" is an abstract contract with no declared sub-types,
where no value of the type can be constructed by anyone. Lumping them together overstated what was
untested, so they are counted apart.

The cost of getting here: dropped went 86 → 117 of 1392 over the audit. Every addition is a contract
protobuf-net will not build a serializer for, so emitting one was never useful — but it does mean the
sweep's "% emitted" fell while the generator became strictly more correct. The two numbers measure
different things and should not be read together.

`PBN_DUMP=<substring>` prints the generated source around a match; `PBN_MEMBERS=<type>` prints what
the generator sees for a contract. Between them they settle "ours or the harness's" fastest.
`PBN_NO_SCHEMAS=1` leaves the `.proto` half out, which is the slowest part of the run.

### 14. A member named after a C# keyword emitted code that does not compile — **fixed**

The first thing the `.proto` half of the corpus found, and the worst *kind* of bug this generator can
have: not wrong bytes, but a **consumer build that does not compile at all**.

`ISymbol.Name` is the metadata name, so a property the consumer declared as `@case` comes back as
`case`, and the emitter wrote `value.case = state.ReadInt32();`. The fix is to re-escape reserved
keywords at the point of emission (`Escape`, using `SyntaxFacts.GetKeywordKind` so contextual
keywords such as `value` are correctly left alone). Three sites needed it: the member access itself,
and the two auto-tuple constructor-argument reads.

Note where it must *not* be applied: the `[UnsafeAccessor(Name = "set_case")]` argument is matched
against metadata, so it takes the unescaped name. Escaping is a property of emitting C# *syntax*,
not of naming the member.

Why it survived this long is the interesting half. Nobody writing a contract by hand types
`public int @case`, so no fixture had one and no hand-written corpus contract did either. It arrives
through `.proto` DTOs, where a schema field may be named after any C# keyword and protogen escapes it
for them — `google/cloud/language/v1`'s `PartOfSpeech.case` is the field that found it.
`Keywords.input.cs` now pins it, including the negative case (`value`, contextual, must not be
escaped) and the auto-tuple path.

### 15. A `.proto`-generated DTO tree works natively — and costs 18 warnings that are not calls

`src/AotSchemaDtos` compiles `descriptor.proto` to DTOs; `AotSmoke` references it and round-trips a
populated `FileDescriptorSet`. It **passes under native AOT**, which is the headline: schema-generated
contracts were previously only known to match ref-emit on a JIT runtime.

Two things came out of it.

**Generators cannot see each other's output**, so the DTOs need their own assembly. All source
generators run against the same input compilation, so a `[ProtoModel]` seeded with a type that
`ProtoFileGenerator` produces from a `.proto` in the *same* project finds nothing. Worse, the seed
arrives as an **error symbol with a name but no attributes**, so the old diagnostic said "it is not
marked `[ProtoContract]`" about a type whose generated source says exactly that. `PBN2002` now
recognises `TypeKind.Error` and says what is really wrong, naming the project-layout fix. Probed by
building it that way, not assumed.

**The warning count went 21 → 39, all `IL3050`, and none of them is a call.** The graph
(`/p:IlcGenerateDgmlFile=true`) shows `System.Enum.GetValues(Type)` and friends as *Reflectable
method* nodes reached from "Dataflow analysis for `…ISerializer<Order>.Read` — **reason: `T`**". That
is the same category as the 808 KB transport finding: members kept **reflectable** by a
`DynamicallyAccessedMembers` demand and never invoked, i.e. a metadata-size problem wearing a
warning's clothes. It is consistent with the round-trip passing.

The mechanism is worth stating because it is not obvious: reflection returns **inherited statics**, so
`typeof(SomeEnum).GetMethods()` includes `Enum.GetValues`, `Enum.Parse` and the rest — and those carry
`RequiresDynamicCode`. So *any* `PublicMethods` demand that lands on an enum type parameter keeps
them, and reports one `IL3050` per site. descriptor.proto did not introduce this; it has many
contracts, and the count scales with contracts.

Not yet done: identifying **which** annotation the demand comes from, and whether it can be narrowed
(the generated `GetSerializer<T>` override restates `DynamicAccess.ContractType`, which includes
`PublicMethods`, and is the first thing to check). Worth a size measurement before and after, since
the count and the bytes have never moved together.

## Next steps

The candidate list as it stands, roughly in the order worth taking them. Recorded so the ordering
survives, not as a commitment.

1. ~~**Widen `AotSmoke`'s coverage, systematically.**~~ **Done** — the list of natively-unexercised
   features is now empty, and the last round found nothing.

   Covered: repeated enum and repeated message; the three map shapes; the **hand-written
   serializers** in all three categories; the **collection families** (array of scalar and of
   message, `HashSet`, `Queue`, `SortedSet`, `ImmutableArray`, `ConcurrentQueue`, and the immutable
   *reference* families `ImmutableList`/`ImmutableDictionary`); **`DataFormat`** variation
   (`ZigZag`, `FixedSize`, `Group` on both a lone message and a collection); **`SkipConstructor`**;
   both **callback** families; **`Specified`/`ShouldSerialize`**; both **`ImplicitFields`** modes;
   **`DateOnly`/`TimeOnly`/`nint`**; a **parseable** type; and **`[DefaultValue]`**.

   **The middle round found item 4b — a real bug, and the second-largest thing on this branch.**
   Worth noting what it says about the exercise: the hand-written serializers, chosen first precisely
   because their reflective step looked most dangerous, passed immediately; the collections, added as
   routine breadth, threw on the first run. Predicting where the bug is has a poor record here.

   The final round — the seven features above from the callbacks down — passed first time, natively
   and on a Debug JIT run, with the **warning count unchanged at 21** and no contract dropped. That
   is a weaker result than the round before it, and it is the expected one: those seven are shapes
   the *generator* emits directly (a method call, a comparison against a constant, an
   `[UnsafeAccessor]`), where the middle round reached construction *inside the library*, which is
   where the reflective steps are. If a further round is ever wanted, that is the axis to pick on —
   not feature breadth, which is now spent.

   The binary grew 3,727,688 → 4,032,072 bytes (linux-x64) across both rounds, which is the cost of
   the new generic instantiations rather than a regression; the immutable pair alone was 225 KB of it.
2. ~~**Turning the generator on does not make existing code use it.**~~ **Done** — `PBN2010`/`PBN2011`
   flag the call sites, `UseAotModelCodeFixProvider` rewrites the fixable ones onto `Model.Instance`,
   and `PBN2012`/`PBN2013` announce the feature to a project that has contracts and no model, with
   `AddProtoModelCodeFixProvider` offering to write the stub. The reasoning behind the severity split
   is under "Future ideas".
3. ~~**Ship `protobuf-net.BuildTools` by default.**~~ **Done** — packed into `protobuf-net` as
   `analyzers/dotnet/cs` plus `build/protobuf-net.props`. The severity audit that gated it is done
   (three false positives found and fixed), and `ProtoBufDisableBuildTools` makes declining it one
   property.

   **The build-time cost is measured, and it is below the noise floor.** `Examples` is the largest
   project here — 34k lines, and the corpus sweep counts ~1400 contracts across the three test
   projects — so it is a fair worst case. Clean builds (`--no-incremental`), median of three:

   | | |
   | --- | ---: |
   | no analyzer at all | 4652 ms |
   | analyzer on (the shipped default) | 4475 ms |
   | analyzer on, `ProtoBufDisableBuildTools=true` | 4605 ms |

   All within 4%, and the *analyzer-on* configuration came out fastest — which is noise, not a
   speed-up, and that is the point: the cost does not rise above run-to-run variance on the biggest
   thing available to measure.

   Two things that measurement does and does not say. It **does** include `AotMigrationAnalyzer`'s
   full symbol walk, which runs precisely in the shipped-by-default case (contracts present, no
   `[ProtoModel]`) and was the part most likely to be expensive. It does **not** isolate analyzer time
   from MSBuild overhead — it bounds it rather than measuring it. `-p:ReportAnalyzer=true` is the tool
   that would isolate it, and produced no output in this SDK invocation; worth another go on Windows,
   where the IDE's analyzer performance view is also available.

   Note the generator itself is not exercised by this: those projects have no `[ProtoModel]`, so
   `ForAttributeWithMetadataName` never fires. That is the right thing to have measured, since a
   consumer who has not opted in is exactly who pays by default.
4. ~~**An "announce" diagnostic for discoverability.**~~ **Done** — `PBN2012` (warning, for a project
   that has asked for AOT) and `PBN2013` (info, on cold-start grounds, for everyone else).
5. ~~**The sibling sub-type stack overflow.**~~ **Fixed** — it was a four-line guard once the
   ping-pong was understood; see item 1.
6. ~~**Establish whether CI is actually red on `main`.**~~ **Done: it is green**, and the corpus
   differential therefore sits next to a clean baseline, which is what that gate needed. The
   `Issue1232` failures turn out to be **intermittent** rather than a standing failure, so the "it
   presumably exits non-zero" that motivated this item was wrong twice over — see item 5, which
   records both wrong answers, because the pattern of getting this one wrong is itself the warning.

   Worth keeping the method rather than the answer: the job log is readable long after the run with
   `gh api repos/protobuf-net/protobuf-net/actions/jobs/<jobId>/logs`, which is how "green" was
   turned into "these specific cases passed". `gh run view --log` returns nothing for a run this old;
   the API route still works.
7. ~~**Widen the corpus differential to the `.proto`-generated DTO path.**~~ **Done**, and it paid
   for itself on the first run: **1283 → 2988** contracts compared, still 100% matching, and one
   real generator bug that broke the *consumer's build* — see item 14.

   The lesson generalises past this one bug. Every contract in the corpus until now was written by a
   person, and people do not write `public int @case`, do not name a type `SearchRequest` in a
   namespace someone else uses, and do not produce forty-deep nested message trees. Machine-generated
   contracts are a different distribution, not merely more of the same one, and only one of the two
   was being measured.

   Still unmeasured on that path, in case it is wanted later: schemas with `message_set_wire_format`
   (protogen emits `#error` for it), and the ~10 schemas whose generated C# does not compile — a
   couple of which look like **protogen** bugs rather than corpus artefacts, `stringEscaping.proto`
   emitting `Unexpected character '\'` being the clearest. Those are protobuf-net.Reflection's to
   answer for, not the AOT generator's, but nobody had run the whole tree through a compiler before.
8. **Direct emit** — see A2. The remaining 18 warnings need the reflective paths not to exist on the
   AOT route at all. Note the warning count is now a *poor* motivation for it: those 18 are correct
   warnings about code that does reflect. The real arguments are one less layer of indirection on the
   generated path, and possibly size — get a size estimate first.
9. **The remaining generator gaps** — a hand-written serializer as a map key or value, and a
   collection as a map key. Both refused with a diagnostic naming the reason; both deliberately
   deferred, each bounded and each with a known reference behaviour: a
   nested map key, a `CategoryScalar` hand-written serializer as a collection element or map value,
   and an external serializer whose category cannot be established.

One loose end, unresolved rather than closed: an intermittent single failure in `Examples` on
**net472 only**, seen twice in full-traversal runs and never reproducible standalone. Never
captured by name, but everything points at `PEVerify.AssertValid` — it shells out to `PEVerify.exe`
with a **20-second timeout** (`src/Examples/PEVerify.cs`), it is inside `#if !COREFX` so it is net472
only, and a subprocess timeout is exactly what contention in a full run would trip. Tied to
`Compile(name, path)`, so no AOT path can reach it.

## Future ideas

### ~~An "announce" diagnostic~~ — built, as `PBN2012`/`PBN2013`

Kept for the reasoning, since the severity split is the whole design and it is not the one I first
proposed.

The generator is invisible: nothing tells a consumer it exists. The obvious answer — an info-level
"did you know" — is also the one people learn to switch off, taking the other diagnostics with it.
What makes this defensible is that **there are two different statements to make, and only one of them
is an offer**:

- **`PBN2012`, a warning.** The project has contracts, asks for AOT or trimming (`PublishAot`,
  `PublishTrimmed`, `IsAotCompatible`, `IsTrimmable`, read through `CompilerVisibleProperty` in
  `protobuf-net.BuildTools.props`), and declares no `[ProtoModel]`. That is not an advertisement, it
  is a **defect report**: they have asked for AOT and their serializers are going to be built by
  reflection. Info would be under-calling it; an error is arguable, and a consumer who wants that can
  escalate. The default stays a warning so that switching `PublishAot` on does not break a build on
  the spot.
- **`PBN2013`, info.** Everyone else. And the argument here is *not* AOT, which is what makes it
  worth making at all: the runtime model inspects metadata and emits IL on **first use** of each
  contract, and that cold-start cost is real enough to time CI out. So there is something in it for a
  consumer who will never publish native, and the offer is honest.

Both are reported **once per compilation** at `Location.None` — this is a property of the project, not
of any one line, and a squiggle on an arbitrarily-chosen contract would be worse than none. Neither
fires once a `[ProtoModel]` exists. `dotnet_diagnostic.PBN2013.severity = none` dismisses the quiet
one permanently, in the standard way.

Verified in a real build rather than only in tests, because the property plumbing is the part that
could silently do nothing: a project with `PublishAot=true`, a contract and no model reports
`PBN2012` naming `PublishAot`; with it false, nothing appears in normal build output.

Still open: pairing `PBN2012` with a fix that writes the `[ProtoModel]` stub. That is what would turn
it from a notification into an action, and it is the obvious next piece.

### Cold start, measured: 51 ms → 17 ms → 0.4 ms

`PBN2013` tells a non-AOT consumer that compile-time serializers help cold start. That claim was
being made on reasoning alone, so `src/AotColdStart` measures it — a three-horse race over the
`descriptor.proto` contract closure, median of 30 **process launches**:

| | wall | in-process |
| --- | ---: | ---: |
| baseline, no serialization | 15.9 ms | 0.001 ms |
| **A** vanilla, runtime model | 67.5 ms | **50.6 ms** |
| **B** generated model, same build | 32.2 ms | **16.9 ms** |
| **C** generated model, native AOT | 3.5 ms | **0.43 ms** |
| native baseline | 3.0 ms | — |

Both arms emit **byte-identical payloads** (12,648 bytes), which is the check that they are doing
equivalent work rather than one of them cheating.

Read net of the baseline: first serialize costs **51.6 ms** vanilla, **16.3 ms** generated on the same
runtime, **0.5 ms** native. So the claim holds — **~3× on an ordinary JIT build, ~100× native** — and
B is the number that matters for `PBN2013`, since that consumer is not publishing native at all.

Why B is not near-zero: it still pays **JIT for the generated serializer code**. What it no longer
pays is metadata inspection and ref-emit. C removes the remaining JIT as well, which is why it is
another 39× below B.

**The methodology is the hard part, and is the reason for the shape of the harness.** What is being
measured happens exactly once per process, so an in-process loop would measure iterations 2..N — all
warm — and report the opposite of the answer. The program therefore does *one* serialize and exits,
and `run-coldstart.sh` supplies the repetition by launching it 30 times. Two clocks, because they
answer different questions: the in-process one starts at the top of `Main` and isolates the
serialization work, while the wall clock includes host startup and is what a user feels. They diverge
most for the native build, so quoting either alone would flatter one arm.

One internal check worth noting, since these are the sort of numbers that turn out to be artefacts:
the two clocks agree independently. For the native arm the wall-clock delta over its own baseline is
3.5 − 3.0 = **0.5 ms**, and the in-process figure is **0.43 ms** — measured by different means, and
consistent. The same holds for A (67.5 − 15.9 = 51.6 against 50.6).

Caveats worth stating before anyone quotes these: one machine, one payload, Linux; the **ratios** are
the transferable part, not the milliseconds.

### ...and it scales with contract count, measured

The claim that this scales with the number of *distinct contracts first used* was inference from the
mechanism. `AotColdStart`'s `scale25`/`scale100`/`scale400` modes serialize three **disjoint** sets of
synthetic contracts, so the only thing varying is the type count:

| contracts | runtime model |
| ---: | ---: |
| 25 | 44.3 ms |
| 100 | 58.0 ms |
| 400 | 129.1 ms |

Roughly linear at **~0.2 ms per contract** above a fixed floor. So a 400-contract model spends about
an eighth of a second before it serializes its first byte, which is the shape of the "timeout
inspecting metadata" failures that prompted the question.

**The honest half: a generated model's advantage narrows as the model grows — on a JIT runtime.** The
same 400 contracts through the generated model take **72.3 ms**, a 1.8× improvement rather than the
3× seen at descriptor size, because the generated *code* has to be JIT-compiled too and there is now
a great deal of it. Published native, where nothing is JIT-compiled, the same case is **0.9 ms**.

| 400 contracts | |
| --- | ---: |
| runtime model | 129.1 ms |
| generated, JIT | 72.3 ms |
| generated, native AOT | **0.9 ms** |

That is worth knowing before quoting a single ratio at people: on an ordinary build the win is real
but shrinks with model size, and it is native AOT that makes it a different order of magnitude.

### A. A UTF-8 fast path for string-shaped members (`IUtf8SpanFormattable`)

Every string-shaped member goes through a `string`: parseable types do `ToString()` on the way out
and `Parse(string)` on the way back, and `WriteString` takes a `string`. For types implementing
`IUtf8SpanFormattable` the write could format straight into the output buffer, skipping the
intermediate `string` allocation entirely.

**This needs a library-side addition first.** `ProtoWriter.State` has no `WriteString`-equivalent
taking UTF-8 bytes, so there is nothing for the generator to call. That is the blocking item, not
the generator work — which is easy, because a source generator can test the interface at *compile*
time and pick per-type, where ref-emit would need a runtime check.

The two halves are **not symmetric**, which is the thing to know before designing around this.
Probed against the actual ref assemblies rather than recalled:

| | `IUtf8SpanFormattable` | `IUtf8SpanParsable<TSelf>` |
| --- | :-: | :-: |
| `IPAddress`, `Version`, `BigInteger`, `Guid`, `decimal`, `int`, `Half`, `Int128`, `UInt128` | yes | yes |
| `DateOnly`, `TimeOnly`, `TimeSpan`, `DateTime`, `DateTimeOffset` | yes | **no** |
| `IPEndPoint`, `EntityTagHeaderValue` | no | no |

- Both interfaces exist from **net8.0**, so the generator would probe for them exactly as it does for
  `UnsafeAccessorAttribute` and `CreateReadOnySet`.
- Implementations **move between versions**: `IPAddress` implements only the formattable half on
  net8.0 and both by net10. So this must be decided from the compilation's own reference set, never
  from a hard-coded list.
- The read side is the weak half — the date/time family formats to UTF-8 but does not parse from it —
  so a design gated on "implements both" would cover almost nothing. Format-via-UTF-8,
  parse-via-`string` is the realistic shape.
- **The utf8 pair is not a superset of parseable**, so it cannot be used as the gate on its own:
  `Complex` and `Rune` implement both interfaces but have no `static Parse(string)`, and accepting
  them would *widen* the set of types protobuf-net serializes — a wire-compat change, not an
  optimisation.

Worth noting the win is allocation, not correctness: the wire bytes are identical either way, so this
is measurable rather than observable, and the differential suite would pass unchanged.

### A2. The native-AOT warnings: what is fixable and what is not

Measured from `AotSmoke` (`dotnet publish -c Release -r win-x64`, after clearing `obj`/`bin` — the
publish is incremental and a second run reports nothing). **The count tracks the fixtures**, so the
baseline has to be re-measured alongside any change to them: 36, then 47, 49 with the repeated
members, and 29 once maps were added. It is now **21**.

| count | id | what it is | | |
| ---: | --- | --- | --- | --- |
| 5 | IL3050 | `MakeGenericType` ×4, `MakeArrayType` ×1 | reflective | |
| 5 | IL2067 | `type` argument not annotated for `Activator.CreateInstance` | reflective | |
| 3 | IL2070 | `GetInterfaces`/`GetConstructor` on an unannotated `Type` | reflective | |
| 3 | IL2087 / IL2057 / IL2055 | one each, same paths | reflective | |
| 3 | IL2091 | a generic path handing `T` to a reflective entry point | **all intractable** | was 11 |
| 2 | IL3050 | `Enum.GetValues(Type)`, in *generated* code | not ours | was 18 |

**The useful split is by source location, not by id.** A warning carrying a `file:line` is a
reflective call in protobuf-net's own source; one attributed to a bare type name with no location is
a member kept *reflectable* by a `DynamicallyAccessedMembers` demand and never called at all. There
were 14 of the latter, all of them one misplaced annotation — see item 4, which is where the 808 KB
went. There are now none.

So: 18 of the remaining 21 are the runtime model, `DynamicStub` and the auxiliary/list paths
correctly declaring that they reflect, and 3 are the `IL2091` below.

**Warnings and binary size do not move together, and both need watching.** Two changes of the same
*shape* — remove a `DynamicallyAccessedMembers` from a serializer type parameter — landed completely
differently:

| | warnings | bytes |
| --- | ---: | ---: |
| the transport annotation (item 4) | −14 | **−827,392** |
| the `MapSerializer` family | **−8** | 0 |

The transport was the only thing keeping `Stream`/`Task`/`Array`/`Enum` alive; a map's `TKey`/`TValue`
are contract types that the generated model's own `GetSerializer<T>` override annotates anyway, so
removing the demand silences the complaint and frees nothing. Neither number is the real one — report
both.

**Not ours: the 2 remaining `Enum.GetValues(Type)`.** protobuf-net does not call it anywhere —
confirmed by grepping the whole tree, where the only hit is `AotDifferential`'s own `Filler`. Both
land on *generated code*, through an enum member resolving its serializer via the model rather than
inline, and both are kept reflectable rather than called.

This group was 18 and is the one that moves with the fixtures, so the baseline must be re-measured
whenever those change. **Two successive explanations of it were wrong** before the ILC dependency
graph settled it; item 4 records both, the tracing recipe, and the 808 KB that turned out to be
sitting underneath. The one durable lesson: this class of warning counts *attributions of kept
metadata*, not dynamic calls, so it responds to annotations rather than to code — which cuts both
ways, since a falling count can mean either a real fix or merely a pruned path.

**Genuinely reflective: the 9 remaining IL3050, the IL2067/IL2070/IL2057/IL2055.** These are the
runtime model's dynamic paths — `DynamicStub.SlowGet`, `TypeModel.CreateListInstance`,
`TypeHelper.ResolveUniqueEnumerableT`, `TypeModel.DeserializeType`. They are correct warnings about
code that genuinely does reflect.

**The tractable group was the 11 `IL2091`, and the fix was not an annotation.** Nine of them were a
generic *library* path passing its `T` to a reflective entry point, always the same shape:

```csharp
serializer ??= TypeModel.GetSerializer<TItem>(state.Model);   // RepeatedSerializer, twice
serializer ??= TypeModel.GetSerializer<T>(Model);             // WriteAny, ReadAny, and friends
```

The `??=` fallback is **dead code for a generated model**, which always passes a serializer — but ILC
cannot prove that, so `TItem` must carry `DynamicAccess.ContractType`, and every instantiation of
`RepeatedSerializer<,>`, `ListSerializer<,>`, `WriteWrappedItem<T>` and `ReadRepeatedCore<,,>`
inherits the complaint.

Removing the annotation does **not** work, and this was tried rather than assumed: dropping
`[DynamicallyAccessedMembers]` from `RepeatedSerializer<TCollection, TItem>`'s `TItem` — the same
annotation already removed from `ISerializer<T>` — took the count 47 → 45, but *created* two new
warnings at `WriteRepeated`/`ReadRepeated` where none had been, because that is where the fallback
lives. It relocates rather than removes, exactly as the earlier `Requires*` attempt did, and it gives
up a real trimming guarantee for the runtime path in exchange. Reverted.

**A feature switch does remove them, and this is now done.** Gating the fallback on
`RuntimeFeature.IsDynamicCodeSupported` — which ILC substitutes with a constant `false`, eliminating
the arm *before* trim analysis — and then dropping the annotation took **49 → 45** for
`RepeatedSerializer<TCollection, TItem>` alone (`IL2091` 11 → 7), and **45 → 34** for the
writer/reader cluster that followed (`IL2091` 7 → 2). This suppresses rather than relocates, which
is what distinguishes it from the earlier `Requires*` attempt. It lives in
`TypeModel.ResolveSerializer<T>`, so each site is a one-line change:

```csharp
serializer ??= TypeModel.ResolveSerializer<TItem>(state.Model);
```

Both arms are the *same resolution* — inbuilt, then `model.GetSerializerCore<T>`. They differ only
in the annotation, which is deliberate on two counts:

- **The AOT arm must not throw.** That middle term is how a *generated* model resolves a repeated
  enum: we pass no serializer and rely on `ISerializerProxy<TEnum>`. An arm that threw would break
  exactly the shapes `AotSmoke` covers.
- **The suppression on that arm closes no hole that was open.** The only override on the route that
  reflects over `T` is `RuntimeTypeModel`'s, which cannot build a serializer without dynamic code at
  all; and wherever both arms *are* live — ordinary trimming, where the switch is not substituted —
  the annotated arm preserves `T` anyway.

**Only a native publish exercises the AOT arm.** A JIT run — every test in the repo, including the
differential suite — takes the other one, so nothing but `AotSmoke` can catch a mistake here. It
gained `List<Status>` and `List<Customer>` for that reason: they are the only members that reach
`RepeatedSerializer`'s fallback, and the enum is the sharp case since resolution must find the proxy
through the model with nothing passed in. (Adding them also moved the count, +2 `IL3050` in the
`Enum.GetValues` block — hence 49 rather than 47 as the like-for-like baseline. Re-measure the
baseline whenever a fixture changes.)

**The writer/reader cluster had to be done in one go.** `WriteAny`, `WriteWrapped`, `WriteMessage`
(both the `State` overloads and the `ProtoWriter` virtual), `WriteGroup`, `WriteWrappedItem`,
`ReadMessage`, `ReadAny`, `ReadWrapped` and `ReadRepeatedCore` all hand `T` to each other, so
treating half of them relocates rather than removes — which is exactly what the `RepeatedSerializer`
step did, surfacing two at `RepeatedSerializer.Write`. Their annotations are gone, and the `??=` in
each now goes through `ResolveSerializer`, along with the measuring writers (`NullWriter`,
`BufferWriter`) that mirror them.

The point that makes this safe is that the *callers* of these already supply the serializer —
`ReadRepeatedCore` and `RepeatedSerializer.Write` pass theirs in — so the annotation was never
serving them; it was serving a fallback they do not take. It is a broad public-signature change, but
only in the relaxing direction, so no caller breaks.

Deliberately **not** included: `DeserializeRoot`/`SerializeRoot`/`ReadAsRoot`/`WriteAsRoot` and the
two `GetSerializer<T>()` accessors on the reader/writer state. The roots are entry points rather than
generic plumbing, and the accessors are an *explicit* resolution request, not a fallback — the
annotation is honest on both. `MapSerializer`'s two `??=` are likewise untouched: they produce no
warning today, and the rule here is to change what the measurement rewards.

**The 2 remaining `IL2091` are both intractable, for different reasons:**

| site | reaching | why not |
| --- | --- | --- |
| `TypeHelper<T>.Factory` | `CreateInstance<T>` | `ActivatorCreate<T>` genuinely reflects, and a generated contract only implements `IFactory<T>` under `SkipConstructor` — so the fallback is live, not dead |
| `SubTypeState<T>.Cast` → `Merge` | `Deserialize<T>` | item 3; needs the annotation on the *class*'s `T`, i.e. on every consumer including the generated path |

**The alternative route is to remove the fallback rather than gate it**, and it still stands for
anything further:

- **split the library methods** into a "serializer supplied" overload with no fallback and no
  annotation, and a "serializer resolved" one that keeps both. Generated code calls the first;
  the runtime model calls the second, and the warnings stay where the reflection actually is.
- **emit more direct code**, so a simple sub-message, collection or map does not route through the
  generic utility layer at all — removing the instantiations rather than gating them, and cutting a
  layer of indirection generated code does not need anyway.

Neither is worth doing for warning-count now. At **20** the `IL2091` group is spent, the misplaced
annotation is gone, and 18 of what remains is the runtime model, `DynamicStub` and the auxiliary/list
paths correctly declaring that they reflect. Getting under that means those paths not existing on the
AOT route at all — the "emit more direct code" idea — rather than any annotation change.

Successive floor estimates here have all been guesses that the next measurement beat: "around 25",
then "about 20" (reached immediately, by an unrelated fix). So: **no floor is quoted**. The pattern
worth carrying instead is that every real win so far came from finding an annotation that described
the wrong thing, and none came from reasoning about the count. Direct emit remains interesting for
its own sake — one less layer of indirection on the generated path — rather than for this.

### B. The coverage sweep undercounts generics

`src/AotCoverage` reports "not seedable, generic: 19" — open generic definitions it cannot name with
a `typeof(...)` in the generated seed list. That was accurate when the generator refused all
generics, but closed constructions are supported now, so those 19 are unmeasured rather than
unsupported.

The tool could substitute a plausible argument (`int`, or the first type satisfying the constraints)
and seed the construction, giving a truer denominator. Care needed: constraint satisfaction is not
guaranteed, and a construction the sweep invents is not evidence that a *consumer* has one.

### C. `System.Type` members are deliberately not supported

Seven contracts in the sweep have a `System.Type` member, which ref-emit serializes with
`SystemTypeSerializer`. Refused on purpose rather than not yet done: it round-trips assembly-qualified
names through `Type.GetType`, which is exactly the reflection AOT cannot do, so emitting it would
produce a serializer that compiles and then fails at runtime. The honest options are a diagnostic
(what happens today) or a surrogate the consumer supplies.

The diagnostic now **says so**, and so does the one for a parseable type, so the sweep's
"unsupported type" row splits three ways rather than reading as one undifferentiated backlog:
7 `System.Type`, 4 parseable-but-not-opted-in, 13 genuinely unclassified.

Same reasoning applies to `System.IO.Stream` (1), which is not serializable in any case.

### D. Generated accessor names could collide in principle

`AccessorName` builds an identifier by replacing every non-alphanumeric character in the contract's
full name with `_`. Distinct types can therefore sanitise to the same identifier — `Ns.A<B>` and
`Ns.A_B_` both give `Ns_A_B_`. It needs two such types in one model, both needing `[UnsafeAccessor]`,
so it has not been hit; a uniqueness pass (suffix on clash) would close it cheaply.

Worth doing before generics see real use, since closed constructions make contrived-looking names
much more likely.

### E. Nested and generic *model* types are refused

`ProtoModelGenerator.Parse` bails on `model.ContainingType is not null || model.IsGenericType`, so
`[ProtoModel]` must be on a top-level non-generic class. Nested would be straightforward — the emit
needs to reopen the enclosing types as `partial`. A generic model is a different question and
probably not worth it: the services type is what makes the closed-world guarantee work.

## Fixed on this branch

- **`PBN0012` reported a build error for a *generic* hierarchy root** — the same descriptor as the
  interface bug below, and found the same way. The attribute lives on the open `IFoo<T>` while the
  sub-type implements the closed `IFoo<int>`, so `SymbolEqualityComparer` finds no link and the
  check reports an **error** for a shape ref-emit resolves on both its paths. It now compares
  `OriginalDefinition` as well, with tests for a generic interface root, a generic base class, and a
  genuinely unrelated generic — so the relaxation is not blanket.

- **`PBN0012` reported a build error for every interface hierarchy.** "The type '{0}' is declared as
  an include, but is not a direct sub-type" compared `include.Type.BaseType` against the contract, so
  an interface root — where the link is *implementing*, not deriving — could never satisfy it. The
  severity is `Error`, so `[ProtoContract] interface IAnimal` with `[ProtoInclude(10, typeof(Dog))]`
  failed the build despite round-tripping correctly at runtime. Same class of bug as the `PBN0015`
  false positive on surrogated types, and found the same way: by adding the pattern to a fixture and
  watching the build refuse it. The check now also accepts an implemented interface, and there is a
  test for each direction, so a genuinely unrelated include still errors.

- **`SerializerCache.Get<TProvider, T>` had no trim annotations**, while the
  `SerializerCache<TProvider>` it forwards to needs `DynamicAccess.Serializer` to preserve the
  constructor `Activator.CreateInstance` uses. ILC trimmed it and the first serialize threw
  `MissingMethodException`. This affected **any** hand-written `TypeModel` under AOT, not just
  generated ones.
- **`TypeHelper<T>.ValueChecker` reached `StructValueChecker<TStruct>` through `MakeGenericType`**,
  so ILC never generated it: the first serialize of any **struct contract member** threw *"missing
  native code or metadata"*, and later the same for any **nullable enum or struct** reached through
  the null-wrapping paths. The reflective lookup is now gone entirely, replaced by two unconstrained
  checkers `TypeHelper<T>` can name statically — `NonNullValueChecker<T>` (a plain value type is
  always present) and `NullableValueChecker<T>` (both answers are `HasValue`; the `is null` test
  looks like it boxes, but box-of-nullable followed by a null comparison is a JIT peephole).
  Anything reaching those branches is a value type, because `IValueChecker<in T>` is contravariant
  and so a reference type is already taken by `ReferenceValueChecker`.
- **`[DynamicallyAccessedMembers]` was declared on `ISerializer<T>` and its siblings**, so every
  consumer paid the reflection-based model's cost — including generated models, which never reflect.
  `PrimaryTypeProvider` implements `ISerializer<Type>`, and `System.Type` is saturated with
  `RequiresDynamicCode` members, so that one instantiation produced ~180 warnings. Moving the
  annotations to the reflection entry points took `AotSmoke` from **200 warnings to 33**.
- `TypeModel.GetSubTypeSerializer<T>` and `SubTypeState<T>.ReadSubType<TSubType>` were missing
  `DynamicAccess.ContractType`; both now terminate at the generated call sites, which pass concrete
  types.
