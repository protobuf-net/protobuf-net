# protobuf-net — notes for agents

Only non-obvious things live here; the code is the reference for everything else.

## Usage policy

This project does not exclude LLM etc tool usage under human guidance. All responsibility for
code-quality rests with the human submitter/reviewer; "slop" will be culled without mercy.

## Where the notes live — and which branch they are on

**`docs/` is the published site** (<https://docs.protobuf-net.dev/>, via `docs/CNAME`), so it holds
consumer documentation only. Working notes — design, findings, corpus snapshots — live in
**`notes/`**, which is not published. The test for which is which is `docs/index.md`: everything in
`docs/` is linked from it. If you add a working note to `docs/`, you have published it.

The notes are deliberately versioned **with the code**, not in one central place: a note that says
"X is refused" only stays true if changing the refusal also changes the note in the same commit,
and a note on a branch correctly describes *that branch*. The cost is that you have to know where
to look while a stack is in flight, which is what this section is for.

**A branch IS in flight as of 2026-09-11** — `nrt-core`, carrying gap B51 **stage 4**: NRT on
`protobuf-net.Core`. It is pushed, green and **unfinished**; `notes/gaps.md` B51's "Stage 4" section
is the working document. Its predecessor `nrt-reflection` merged as PR #1332. The "current on" column
below is therefore restored, per the rule that follows; drop it again when the branch merges and `v4`
is once more the only answer. Two wrong-branch claims were shipped last time it was left off while a
stack was in flight, one of them in this very table.

`v4` last took `main` on **2026-09-11** (the 3.4.21 line). `main` is the released line and is no
longer ahead of this one; a fix that has to ship before v4 does goes there first and is merged here.

`notes/readme.md` states the `docs/` vs `notes/` split and why it matters; read it before adding a
file to either. The short form is that `docs/` is the published site, so "it is obviously internal"
buys nothing — the test is which directory it is in.

**This file is loaded into every session's context, so its size is a standing cost** and it had
grown to ~2,500 lines. On 2026-08-25 the per-feature reference moved out to
`notes/aot/generator-reference.md`, verbatim, halving it. The dividing line to hold when adding
here: **what a session needs before it knows what it is doing** — conventions, traps, the gate
battery, who owns which diagnostic id — as against what it can look up once it does. A new fact
about how one shape is emitted belongs in the reference; a new way to get something silently wrong
belongs here.

| document | covers | current on |
| --- | --- | --- |
| `AGENTS.md` (this file) | conventions, traps, gate battery | `v4` |
| **`notes/gaps.md`** | **every known gap with its DECISION — start here for "what is missing?"** | `nrt-core` |
| **`notes/aot/generator-reference.md`** | **what the generator emits for each shape, and why — the per-feature reference, carved out of this file on 2026-08-25** | `v4` |
| `notes/nano-core.md` | the reader arc: design and the cuts | `v4` |
| `notes/nano-writer.md` | the writer arc, **plus an index of everything parked or owed** | `v4` |
| `notes/packed-writes.md` | the packed matrix, **and the raw packed surface that came out of it** | `v4` |
| `notes/aot-schema-model.md` | `[ProtoSchema]`: design and open items | `v4` |
| `notes/aot/findings.md` | numbered findings from the AOT generator work, and the ranked next-steps list | `nrt-core` |
| `notes/aot/coverage.md`, `notes/aot/differential.md` | the two corpus sweeps' last snapshots (tool output — regenerated, not maintained) | `v4` |
| `notes/aot/grpc.md` | the gRPC proxy generator (from `main`) | `v4` |
| `notes/editions/feature-analysis.md` | the editions arc (from `main`) | `v4` |
| `docs/aot.md` | the consumer-facing AOT guide, incl. the throughput table | `v4` |
| `tools/` | repo scripts that are not part of a build — currently `annotate-public-api.py`, which rewrites `PublicAPI.*.txt` baselines from the analyzer's own `RS0036` output (gap B51) | `v4` |

Two rules that keep this honest, both learned the hard way here:

- **`notes/gaps.md` is the entry point for "what is missing"**, and the parked/owed index in
  `notes/nano-writer.md` for "what is deferred and why" — not a commit log. Anything
  deferred goes there with its reason, because a commit message is not a backlog and does not
  survive a squash. **Starting cold, read `notes/aot/findings.md`'s Handover section first**: it
  carries the branch, the gate battery with the numbers it last reported, the live items in priority
  order, and — deliberately — what recently CLOSED, so a fresh session does not go hunting for work
  in a finished entry;
- **a change big enough to appear in a handover is big enough to have its own commit.** A gate once
  landed as an unmentioned passenger in an unrelated commit, and the handover — assembled from
  commit messages — recorded it as never having landed. That cost a later session most of a
  sitting; see "Three corrections to this handover" in `notes/nano-writer.md`.

Refresh the table above when a branch is cut or merged.

## Layout and build

- The solution is **`protobuf-net.slnx`**. `protobuf-net.sln.old` is a stale leftover — don't use it.
- CI (`.github/workflows/dotnet.yml`) runs on **windows-latest** and builds/tests **`Build.csproj`**,
  a `Microsoft.Build.Traversal` project. It globs `src\*\*.csproj`, so a new project under `src/`
  is picked up by CI automatically — including `net4x`-only ones, which are fine because CI is Windows.
- **`docs/` is published; `notes/` is not.** `docs/` is the Jekyll source for
  <https://docs.protobuf-net.dev>, with `jekyll-sitemap` on and no `exclude:` in `_config.yml` — so a
  file put there is built into the site and handed to search engines, however internal it reads.
  Working notes, handovers and generated snapshots go in **`notes/`**, grouped per topic —
  `notes/aot/` and `notes/editions/`. The four AOT files were published for a while before anyone
  noticed.
- **Central package management is on.** Add versions to `src/Directory.Packages.props`; leave
  `Version=` off the `PackageReference` in the csproj.

## protobuf-net.BuildTools compiles in its dependencies

`src/protobuf-net.BuildTools/protobuf-net.BuildTools.csproj` does not *reference*
protobuf-net.Core / protobuf-net.Reflection — it **compiles their sources in**
(`<Compile Include="../protobuf-net.Core/**/*.cs" />`), because package references inside analyzers
are painful.

Consequence worth knowing in tests: `typeof(TypeModel).Assembly` resolves to the **BuildTools**
assembly, not protobuf-net. That is deliberate and is what `MetadataReferenceHelpers` relies on.

## Nullable reference types: four ways to get this wrong (gap B51)

The rollout is partway through — `protobuf-net.ServiceModel` and `protobuf-net.Reflection` are
NRT-enabled, `protobuf-net.Core` and `protobuf-net` are not, and **protogen emits annotations** into
its C# output. Sequencing and history are in `notes/gaps.md` B51; these four are the traps.

**1. A polyfilled nullability attribute MUST live in the assembly that uses it.** net462 and
netstandard2.0 have no `NotNullWhen`; nothing below net5.0 has `MemberNotNullWhen`. Putting the
polyfill in one assembly and sharing it by `[InternalsVisibleTo]` compiles, tests green, and **fails
at runtime**: protobuf-net.Reflection has no net8.0 target, so a net8.0 app loads its
*netstandard2.0* build against Core's *net8.0* build — where the polyfill is compiled out — and the
first thing to reflect over the annotated members throws `TypeLoadException`. It surfaces inside
`RuntimeTypeModel.FindOrAddAuto`, i.e. on a consumer's first serialize. The copy that exists lives in
`protobuf-net.Reflection/Internal/NullableAttributes.cs`; when Core needs its own, it needs a
*separate* copy, and then one of the two must be `Compile Remove`d from **both**
`protobuf-net.BuildTools` and `protobuf-net.BuildTools.Legacy`, which compile both projects' sources
in and would otherwise see `CS0101`.

**2. `string.IsNullOrEmpty` does not narrow here, and is worse than that.** These TFMs' reference
assemblies are *oblivious*, so the BCL methods declare nothing — and **null-testing an oblivious
value moves it to maybe-null**, so the BCL call manufactures warnings rather than merely failing to
remove them. Use `ProtoBuf.Reflection.Internal.StringNullability`'s `IsNullOrEmpty()` /
`IsNullOrWhiteSpace()` extensions, which carry `[NotNullWhen(false)]`. Reaching for `!` instead is
against standing policy: every `!` is a reading cost, so convince the compiler.

**3. The two analyzer assemblies need a `CS8632` `NoWarn` until stage 4.** They compile Core's and
Reflection's sources in while their own compilations have NRT off, which is `CS8632` once per `?`.
Both carry the `NoWarn` with the reason; enabling NRT on Core is what removes them.

**4. The generated-code half is C# only.** VB has no nullable reference types, so `VBCodeGenerator`
emits nothing for this (its own C# source is annotated, which is a different thing). What the C#
generator emits per member shape — and why a `ShouldSerializeX()` sometimes carries
`[MemberNotNullWhen]`, sometimes `[AllowNull]`, and sometimes neither — is in `notes/gaps.md` B51.
Note the compiler does **not** verify `[MemberNotNullWhen]` on the shape we emit (an expression body
returning a computed bool), so the claim has to be true by construction; nothing will catch it.

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

## Don't improve the legacy library or ref-emit "while you're in there"

`RepeatedSerializer`, the other `protobuf-net.Core` serializers, and the ref-emit path are
**deliberately left alone**, and a change there needs a reason beyond "it was easy from here".
This is not conservatism about old code; the legacy path has three jobs that a well-meaning
optimisation quietly destroys:

- **it is the CONTROL.** `[ProtoModel(ClassicEmit = true)]` exists so a generated model can be
  compared against the old engine. If the old engine absorbs the new work, that comparison stops
  measuring anything;
- **it is the FALLBACK.** Every shape the generator refuses lands here, so it needs to stay boring
  and trustworthy rather than clever and new;
- **it is the BASELINE for perf claims.** A speedup measured against a baseline that already
  contains the speedup is not a speedup.

**This has already happened once, at full scale.** The packed work put ~130 lines of fast paths
into `RepeatedSerializer` — block copies, a bool blit, direct varint arms, a vectorised measure —
and because both models route through that one method, **every "classic vs raw" number in
`notes/packed-writes.md` was comparing classic against classic**. It looked for weeks like the raw
writer simply had no advantage on packed data. It was reverted on 2026-08-15 (`a73d6fc0`) and the
work rebuilt where it belongs, as raw writer state APIs the generator calls directly.

**The diagnostic worth memorising: if a benchmark shows classic tracking raw within ~1%, suspect
they are the same code before you conclude the optimisation did nothing.** A control that shares
the code under test is not a control. Confirm it the cheap way — emit the generator output
(`-p:EmitCompilerGeneratedFiles=true`) and count `Measure_`/`RawWrite_` methods; a model with zero
of them is not exercising the raw writer at all, whatever its name says. Note this also means
`ClassicVsRawTests` can pass while comparing a thing to itself.

**When a revert IS the answer, revert to the right point, not to `main`.** That same file had also
been touched by the writer arc for unrelated reasons (a typo fix with an `[Obsolete]` forwarder,
and an `AdvanceAndReset` → `ResetWireType` change), so `git checkout main -- <file>` would have
silently taken those too. Find the last commit before the work being backed out and restore to
that.

**None of this is "never touch it".** Revisiting the library and ref-emit with what the raw arc
learned is a genuine future item — but as its own piece of work, measured on its own terms, and
deliberately *after* the new path is established enough to be the reference. Doing it
opportunistically, one fast path at a time, is what removes the ability to tell whether any of it
helped.

## The writer's measure-first path: three invariants that are easy to break

The generated writer measures a contract arithmetically (`Measure_`) and then writes it
(`RawWrite_`) with an exact length prefix, instead of writing it twice. Design and history are in
`notes/nano-writer.md`; these three are the ones that bite silently.

**1. Not every contract is measurable, and losing it cascades.** `RawMeasurableShape` plus
`RawMemberMeasureBlocked` decide, and an ineligible member removes its *whole contract* from the
measurable set — computed **to a fixed point**, so the exclusion spreads to everything that
references it. One awkward member can therefore drop a large subtree onto the classic
write-to-count path. When something is unexpectedly slow, check whether it is still measurable
before looking anywhere else.

**2. A `[ProtoBeforeSerialization]` callback fires in BOTH passes, and that is load-bearing.**
It used to disqualify the contract outright — `Measure_` had no `ISerializationContext` and so could
not fire callbacks at all — and this file recorded that refusal as necessary. It was not: firing
only in `RawWrite_` would indeed let the object change between measuring and writing, but firing in
*both* is correct, and is what the classic buffer-writer backend has always done. `Measure_` now
takes a context and fires before-serialize *and* after-serialize around the arithmetic, exactly as
`RawWrite_` does around the bytes, so both passes observe the same object. See `notes/gaps.md` B42.

Two parts of that are easy to break:

- **the context the measure pass hands out is not `state.Context`.** It is a wrapper for which
  `ProtoWriter.IsMeasuring` answers `true` (`RawLengthBuffer.AsMeasuring`, recognised through
  `Internal.IMeasuringPassContext`). Firing twice while answering `false` both times would double a
  consumer's side-effects with nothing to notice it by — worse than the old refusal. A model with no
  serialize callback anywhere never emits the wrap.
- **`ISerializationContext` is an accepted callback signature, and had to be**: it is the only
  flavour carrying the context *object* rather than a copy of its data, so it is the only one that
  can ask. A callback taking nothing or a `StreamingContext` still works and simply cannot tell.

**How often a callback fires is per-BACKEND on the CLASSIC path**, which is easy to miss and is
pinned by `CallbackMeasurePassTests`:

| route | `BeforeSerialization` fires | `IsMeasuring` |
| --- | ---: | --- |
| plain `Serialize` to a **stream**, nested contract | **once** | `false` |
| to an **`IBufferWriter`**, nested contract | **twice** | `true`, then `false` |
| explicit `Measure` + `Serialize` | **twice** | `true`, then `false` |

The stream writer *reserves, writes and back-fills* the length — shuffling bytes when the varint
width changes — so it crawls once. The buffer-writer path computes the length first, writes the
prefix, writes for real, and then **validates** (`Length mismatch; calculated 'x', actual 'y'`), so
it crawls twice. So a consumer's callback side-effect already behaves differently depending on
which output they serialize to, with nobody having asked to measure.

**A measure-first GENERATED contract is twice on EVERY backend** — that is the whole of the
alignment Marc asked for on 2026-08-14, and it is **done** (B17, closed 2026-08-25). The generated
`Write` measures and then writes whatever the destination is, so it has no per-backend split; the
table above is a statement about the *classic* engine and not about the model. Pinned by
`MeasurableContractTests.AGeneratedContractMeasuresToAStreamToo`, which gets the identical
`bs*;as;bs;as;` from a `MemoryStream` that the `ArrayBufferWriter` case gives.

**The classic asymmetry is deliberately left in place**, not outstanding: it predates this arc, and
a consumer moving to the generator gets the consistent behaviour either way.

Where doubling happens it is *required*, not incidental: both passes must observe the same object,
or the measured length will not match the bytes written — which is exactly what that validation
catches. `ProtoWriter.IsMeasuring(context)` is how a callback tells the passes apart.

**It is not as simple as "twice for anything nested", though**, because a **group** carries no
length prefix — so nothing measures it and its callback fires once, *unless something above it
needs a length*, at which point the parent's measure walks through it anyway. The count follows the
nearest **length-prefixed ancestor**, i.e. the path from the root, not the member's own framing:

| shape | stream | buffer-writer |
| --- | --- | --- |
| group at the root | `[false]` | `[false]` |
| same group under a length-prefixed parent | `[false]` | `[true, false]` |

**The invariant to hold is "AT MOST TWICE"** (Marc), for a node not duplicated in the tree. Every
length-prefixed ancestor needs a length for everything beneath it, so a naive measure-by-writing
would re-walk the innermost node once per ancestor — 2^depth crawls rather than 2. **Verified at
depth 3: two calls, not eight** (`AtMostTwiceHoweverDeepTheNesting`).

**Two different mechanisms hold that invariant, and conflating them has already misled once** — this
paragraph used to explain both as "memoising by reference", which was only ever true of the first:

- the **classic** path measures by writing, and `NetObjectCache._knownLengths` memoises by reference
  identity. That really is what stops the exponential blowup, and it is what
  `AtMostTwiceHoweverDeepTheNesting` exercises — note the test builds a `RuntimeTypeModel`, so it
  says nothing at all about the raw path;
- the **generated raw** path measures *arithmetically*, and its recursion visits each node once by
  construction, so nothing needs memoising for the invariant to hold. What it needs is **transport**:
  carrying n measured lengths from the measure pass to the write pass. Since 2026-08-21 that is
  `RawLengthBuffer` — an append-only `long[]` consumed in visit order, no hashing at all. It was a
  `Dictionary<object, long>` and that cost around half of a length-prefixed serialize; see
  `notes/gaps.md` B38, which also records the three ways a positional scheme breaks that the design
  did not predict.

**The rule that falls out, and is worth stating because it is the whole correctness argument: a slot
is reserved exactly where the write calls `Next()`, one for one, in the same order.** So the slot
belongs to the *call site*, not to the contract being measured — a member that is measured but
whose write does not read a length (a group, or anything handed to the classic engine) must reserve
nothing, and a sub-tree the raw write will not walk is measured with **`RawLengthBuffer.Discard`**,
a shared instance whose `Reserve`/`Set` are no-ops. Widening either eligibility predicate without
the other now shifts every subsequent length rather than merely wasting a cache entry.

That was a literal `null` until 2026-08-22, and the phrase "suppresses reservation all the way down"
was **not true of it**: the callee's own `Reserve()` sites are unconditional, so the first nested
length-prefixed member below such a sub-tree dereferenced it. Reachable from the moment
nullable-struct message members became measurable, and never fixtured until `DepthBoundary`. A
no-op instance suppresses it for real, and without a null test at every call site.

**A SHARED (aliased) object in one graph is owed no memoisation — POLICY** (Marc, 2026-08-21).
Where a design is faster for trees and slower for graphs that reference the same instance several
times *in parallel* (not recursively), take it. The reasoning is not that the case is rare, though it
is: it is that **anyone duplicating the same non-trivial object repeatedly has already declined to
care about efficiency.** protobuf has no reference sharing on the wire, so every occurrence is
written out in full whatever we do — someone who cared would send a cross-reference token instead.
Optimising the *measure* pass for a graph whose *write* is already paying k× is optimising the wrong
half.

So the order of preference is: (a) live without memoisation between re-uses; and only if (a) is
measured to hurt, (b) an opt-in AOT-only flag turning it back on. Do not reach for (b) speculatively
— when this was measured for `notes/gaps.md` B38, (a) was **faster even on the aliased graph**
(1.37×–1.49×), because re-measuring a small shared subtree costs less than the hashing that avoiding
it buys. The flag would have protected a case that does not exist.

**Decided (Marc, 2026-08-14): twice becomes the consistent normal for both backends**, rather than
the stream being the odd one out — stated as "**once per pass over this node, at most twice**",
since the number of passes is a property of the path. That is also where measure-first leads
anyway, since it *is* measure-then-write. **Delivered for the generated path and closed there**
(B17); the classic stream path keeps its single crawl, which is a statement about that engine rather
than an outstanding item.

**3. The write recursion is depth-guarded on its own, and used not to be.** It was once safe *by
construction* — every write was preceded by a measure, and the measure carried the budget. A
**grouped** sub-message has no length prefix, so nothing measures it; making groups write raw
removed that guard, and a cycle through grouped members recursed until the process died. `RawWrite_`
now threads a remaining depth budget, seeded at the boundary from `state.RawDepthBudget` and never
touching `writer.Depth` — the "raw API does not maintain all the members" convention. Every
`RawWrite_` checks it, deliberately: the alternative is a predicate over which contracts are
reachable through a group, and that predicate's failure mode is an uncatchable stack overflow.
`GroupCycleTests` drives exactly that graph.

**The two caps have to ADD across a hand-back, and did not** (gap B15, fixed 2026-08-22). Where a
raw body falls *back* to the stateful engine, the engine counts from `writer.Depth`, last set at the
outer boundary — so a deep raw chain that then went stateful under-counted. `state.SyncRawDepth`
sets it on entry and restores on exit, emitted **only** into a `RawWrite_` whose body actually
contains a hand-back, so a fully-raw body pays nothing.

Note this is only observable on a **fully unmeasured** chain — every nesting site a group — because
a *measure* recursion crosses a stateful boundary without re-seeding and is therefore already
additive. `RawDepthBoundaryTests` ladders through grouped members for exactly that reason, and was
rewritten twice because the first two versions passed with the fix removed.

Note **`MaxDepth` (512) only bounds recursion if frames are small.** A large contract used to emit a
local per member — measured at 1004 in a 400-member `Measure_` — and at roughly 8 KB a frame the
stack is exhausted around 128 levels, long before the depth guard fires.

**So the write and measure bodies now share ONE local per distinct member TYPE** (`TempPool`, gap
B16): 400 scalar members over 5 types went 400 → **5** locals, and the 1004 above → 310. Three
things about that are easy to break:

- **the compiler will not do this for you.** Twenty same-typed locals in twenty *sibling* `{ }`
  scopes still compile to twenty IL slots, in Release as in Debug — Roslyn's slot allocator does not
  reuse by scope. Only emitting genuinely fewer declarations helps, which is why this lives in the
  generator. (Release *does* halve the corpus's local count against Debug, which makes "Roslyn
  already folds" look plausible; that is single-use values staying on the evaluation stack.)
- **a new per-member local family must be pooled too, or the body goes back to being linear in
  members.** `slot` and the map measure's `mapEntry` are folded on the same argument as the
  long-standing `sub`: reserve, call *another* method, consume — never two live at once. Note
  `mapEntry` is not spelled `entry` because **a shared `entry` already exists** in these bodies for
  a `slots.Mark()`, and aliasing two unrelated locals of different types is the failure mode here.
- **`AppendFoldingLengthTemp` appends the BODY as well as declaring it**, so anything that must
  precede the body is emitted *before* that call. Getting it backwards put declarations after their
  uses and broke 52 goldens — caught only because the goldens compile.

The measured instrument is `MethodBody.LocalVariables.Count` on a deliberately wide contract, not a
benchmark: the argument is frame size, which is arithmetic. `notes/gaps.md` B16 has the numbers, and
what is left — the `RawRead_` body, and the `foreach` variables, which C# requires one of per loop
and so cannot be shared at all.

**4. A measured length that disagrees with the body is caught in DEBUG, and only in DEBUG.** Every
length-prefixed raw write is followed by `DebugAssertPosition`, comparing `state.Position64` against
the prefix that was written; the pair of `[Conditional("DEBUG")]` helpers is emitted onto the
services type. This is the raw path's answer to the classic buffer-writer's *"Length mismatch;
calculated 'x', actual 'y'"* validation, and it reaches the consumer's own contracts, which the
differential corpus never can.

Three things about it are load-bearing:

- **`Debug.Fail` terminates the process on .NET Core.** It is an uncatchable `FailFast`, not a
  logged warning — so drift cannot be ignored, and equally a **false positive would kill every
  consumer's Debug build**. Widening what the assert covers demands the same breadth of proof the
  original did (the whole fixture set under `-c Debug`, plus `AotSmoke -c Debug`).
- **`Position64` is safe to lean on here despite the raw path not maintaining most writer state**,
  because it is *derived* — committed bytes plus the backend's uncommitted buffer offset, which is
  exactly what a raw write advances. Contrast `writer.Depth`, which the raw path genuinely does not
  maintain.
- **`ref`, not `out`**: `out` on a conditional member is **CS0685**, precisely because the call may
  vanish and leave the target unassigned. The capture local costs a Release consumer nothing —
  measured at 0 IL locals and 2 bytes of IL once both calls are removed.

### Repeated members on the raw path: four predicates, and one trap that has bitten three times

The generator decides per member whether a repeated, BCL or null-wrapped member takes the raw path.
The decision is spread over seven small predicates in `ProtoModelGenerator.Emit.cs`, and they are easy
to widen one at a time without noticing the others:

| predicate | decides |
| --- | --- |
| `RawRepeatedWritable` | an **unpacked** repeated member: collection shape and element kind |
| `RawPackedWritable` | a **packed** one — a different engine, not a variation, since packing needs the payload measured before the prefix goes down |
| `RepeatedSpan` | how the collection becomes a span: `T[]` directly, `List<T>` via `CollectionsMarshal` (probed), `ImmutableArray<T>` via `AsSpan()` (probed separately — it rides on a *package*, not the framework) |
| `BclMeasurable` | whether a compatibility-level BCL member has arithmetic sizing (currently: default format, `DateTime`/`TimeSpan` below level 240, `Guid`/`decimal` below 300) |
| `WrappedValueMeasurable` | a **lone** `[NullWrappedValue]`, whose inner field is *omitted* when trivial |
| `WrappedRepeatedMeasurable` | a wrapped **collection**, in either scope, whose element wrapper *always* carries its inner field — the opposite rule, hence a second predicate rather than a widening of the first |
| `RawMapNestedValueMeasurable` | a **nested** map value (`Dictionary<K, List<V>>`), which is a different shape again — see below |

**A nested map value repeats field 2; it does not fill it.** `KeyValuePairSerializer` hands the
collection to `WriteAny`, which sees `CategoryRepeated` and calls `WriteRepeated`, so the entry
carries one field-2 occurrence *per element* and no length of its own for the value. The measure is
therefore a loop, not a term — `EmitMapSide` grew an arm for it rather than `MapSideBody` growing a
case.

**A packable element is a different shape, not a harder one — and getting that wrong cost a round
trip.** `WriteRepeated` packs on `CanBePacked && !IsPackedDisabled && (count == 0 || count > 1)`, so
the encoding depends on the *count*: probed, `{1:[]}` writes `12-00`, `{1:[9]}` the **unpacked**
`10-09`, and `{1:[9,10]}` the packed `12-02-09-0A`. A single element is never packed.

The first pass excluded all of that, reasoning that the count and the `SkipZeroLengthPackedArrays`
**model option** are "not knowable at generation time". **That confused a compile-time constant with
a value the generated code can read.** `Measure_` is run-time code: the count is a `foreach` away,
and the option is `context.Model.Options` — `TypeModel.Options` and `TypeModelOptions` are both
public. So it is three arms in one pass, and `EmitMapNestedPackedMeasure` emits them. Worth
remembering as a *pattern*: "the generator cannot know this" is only an argument about the emitted
constant, never about what the emitted code may compute.

Every packable kind was probed inside a map value rather than assumed, and they all pack — the
integrals, `bool`, `char`, the floats, **enums** and **arrays** — which also settles that
`EnumSerializer` is an `IMeasuringSerializer`, as the packed branch requires.

Two things stay out, each for a stated reason rather than caution: a **nullable** element packs too,
but what the writer does with a null inside a packed run is untested, so its measure would be
guesswork; and a **value-type collection** (`ImmutableArray<T>`) cannot be null-guarded the way the
reference ones are — `default` throws on `foreach` while the writer treats it as empty and still
emits the two-byte header. A nested *map* value is out for a third reason: it is the outer shape
again one level down.

The **raw read** pass is a separate axis and is unchanged: `map with repeated value` is still a
legacy-mode reason there, alongside the map shapes that were already legacy-mode. Measure and write
eligibility are independent of it, exactly as for a repeated BCL member.

**Emitting a long `or` pattern is emitting a deep stack — and the axis that matters is not the one
it looks like.** Roslyn parses `a or b or c` into a left-nested tree and binds it by *recursing*
once per `or`, so `IsKnownField`'s `(tag >> 3) is 1 or 2 or ... or 1000` — which a protogen stress
schema in the corpus really does produce — binds a thousand frames deep. `AotDifferential` died on
it with a bare **`Stack overflow.`** and no diagnostic at all.

Three things were measured rather than assumed, and each changed the answer:

- **It is not a term count.** An in-process `Compilation.Emit` — what `AotDifferential`,
  `AotCoverage` and every IDE-hosted analyzer use, on ordinary thread-pool stacks — takes 1000 terms
  and dies at 5000, while **`csc.exe` swallows 30000**, binding on dedicated large-stack threads. So
  the same source compiles under `dotnet build` and crashes in our own harness, and there is no
  threshold worth switching on.
- **The chain was pre-existing.** The baseline compiled it; this branch's extra code merely tipped
  the stack. Bisecting to the commit that "broke" it would have pointed at the wrong change.
- **It never cost anything per call.** Benchmarked at 20M calls, the `or` chain, a range pattern and
  a switch are indistinguishable (~3ns dense, ~5ns sparse, delegate-bound), because Roslyn's
  decision DAG already lowers a dense constant set to a jump table and a sparse one to a binary
  search. Only the compiler was paying.

So `EmitKnownFieldTest` emits a **switch statement**, whose sections are a flat list and therefore
bind in a loop: good to 60000 labels in the very host that died at 5000 terms, and free at run time.
Contiguous runs collapse to a relational label (`case >= 1 and <= 1000:`) purely to keep the emitted
source small — a `.proto` message numbers its fields from 1, so the thousand-field case is one label.

At **two terms or fewer** the one-line `is` form is kept, which is most contracts (measured over the
fixtures: 109 one-liners against 29 switches). That is not the threshold rejected above: two terms is
two binder frames, a constant, where the objection was to picking a number *near* a limit that
cannot be located.

Anything else generated per-member into a *single expression* has the same latent shape; a flat list
of statements or labels does not.

`MapNested.input.cs` carries `RawNested` (only the measurable nested shapes, so the contract is
measure-first at all — one blocked member takes the whole contract out) and `RawHolder`, which
**exists so the measure is reached**: at root `RawWrite_` writes straight out and never measures, so
a wrong measure would not show. Both arms were proven able to fail by perturbing the emitted
arithmetic and watching the conformance suite go red, not by observing it pass.

**Widening any of them without a matching measure arm makes the generator THROW**, and the symptom
does not name the cause: every model in the compilation loses its generated `Instance`, producing a
pile of unrelated-looking `CS0117`s (and, in `AotDifferential`, a segfault). The real message is in
a `CS8785` generator warning — *"unmeasurable member slipped eligibility: …"*. That self-check is
load-bearing; without it the first symptom would be wrong bytes.

**The measure emitter reaches scalar kinds from THREE branches** — the nullable path, the tuple
path, and the main switch — and each calls `RawScalarMeasure(...)!`. For a kind that helper has
nothing for, the `!` turns null into an empty string and emits `len += 1 + ;`, which compiles
nowhere. Adding a kind means visiting all three — **four** since the lone null-wrapped arm, which
has its own call and needed a `MeasureRawString` special case because `String` is not in
`RawScalarMeasure` at all. This has bitten **four times**: `DateOnly`
(main switch), the level-200 BCL pair (nullable), `Guid`/`decimal` (tuple — surfaced by an
*unrelated* fixture, `Diagnostics/TupleLevels`), and `string` (the lone null-wrapped arm, where the
corpus segfaulted rather than the goldens catching it). The goldens caught three of the four; review
caught none.

**`NeedsNullGuard` exists because a value-type collection must NOT get one.** `ImmutableArray<T>`
declares lifted equality over `ImmutableArray<T>?`, so `tmp != null` compiles for it and evaluates
to **false** for a `default` instance — silently skipping the member. Harmless unpacked (empty
writes nothing) and a wire divergence packed (it drops the zero-length header). `AsSpan()` on a
default yields an empty span on every targeted runtime, and protobuf-net treats default as empty,
so the guard is not merely unnecessary there but wrong.

`BclHelpers.Measure*` is the sizing surface these rely on. Each measure lives **beside its writer**
(in `PrimaryTypeProvider.*.cs`), not next to the public entry point, because the two must agree
field-for-field and adjacency is the only cheap way to keep that true; `BclHelpers` forwards. They
take no `ISerializationContext` — a generated `Measure_` has none. `BclMeasureTests` proves them
against **bytes protobuf-net actually wrote**, never against restated arithmetic.

## AOT source generator (work in progress)

> **Picking this up on a new machine?** `notes/aot/findings.md` opens with a **Handover** section
> recording the Windows-only validations and their results — all run and green as of 2026-08-11,
> including the full-TFM `pack` (note the `dotnet pack`/NU5026 wrinkle recorded there), the win-x64
> native publish (19 warnings, matching linux-x64), and the net472 test legs.


`ProtoModelGenerator` (`src/protobuf-net.BuildTools/Generators/ProtoModelGenerator.cs`) is an
`IIncrementalGenerator` building compile-time serializers for code-first contracts. It is
deliberately separate from `ProtoFileGenerator` (the `.proto` → DTO path, still `ISourceGenerator`).

Design constraints that are settled, and should not be quietly relaxed:

- The consumer opts in with `[ProtoModel] partial class MyModel : TypeModel`, seeded by
  `[ProtoSerializable(typeof(Foo))]`; everything reachable from a seed is pulled in automatically.
- The trigger attributes (`[ProtoModel]`, `[ProtoSerializable]`, `[ProtoSurrogate]`) are **real API
  in protobuf-net.Core**, marked `[Experimental("PBN9001")]`. They were generator-owned — emitted via
  `RegisterPostInitializationOutput`, one `internal` copy per consuming assembly — while the shape was
  still moving; what forced the move is that `[ProtoSurrogate]` must cross assembly boundaries so a
  library can ship surrogates to its consumers, and a shared type needs a shared home. The generator
  still matches them by **full name** rather than by symbol — the unit-test harnesses declare their
  own stubs (avoiding the `[Experimental]` gate), and the reflection-loaded generator in
  `AotDifferential` can never share symbol identity with the corpus — so keep it that way.
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

  **An auto-tuple may be reached at only one compatibility level.** It is keyed in the model by type
  alone, but its *encoding* follows the level of the member that reaches it — so the same tuple at
  two levels is not expressible with one serializer, and protobuf-net refuses the whole model
  (`RuntimeTypeModel.FindWithAmbientCompatibility`: *"must use a single compatibility level"*). One
  serializer per type is our constraint too, so the tuple is dropped and its referrers cascade. Note
  a contract can be perfectly valid *alone* and still fall to this, because the conflict belongs to
  the model: `CompatibilityLevelAmbientAutoTupleTests+Level300` is fine by itself and fails only
  because a sibling contract reaches the same tuple at Level200.

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
- Every dropped contract must **say why**: `PBN3001` unsupported member, `PBN3002` unsupported
  declaration, `PBN3003` unsupported protobuf-net option, `PBN3004` dropped by cascade. All are
  **warnings**, not errors — an incomplete model still builds, and the runtime "no serializer" throw
  is the backstop; erroring would make the generator unusable while coverage is partial. Anyone
  wanting strictness can escalate via `WarningsAsErrors`.
- **C# 12 is a hard floor.** Below it the generator reports `PBN3000` and emits nothing, rather than
  emitting code that won't compile. Do not add down-level fallbacks: supporting multiple language
  versions multiplies every emitted construct for no benefit to anyone doing AOT. (netstandard2.0
  and net4x default to C# 7.3, so those consumers must set `<LangVersion>` — accepted deliberately.)

### One switch to decline all of it

`<ProtoBufDisableBuildTools>true</ProtoBufDisableBuildTools>` (surfaced as
`build_property.ProtoBufDisableBuildTools`, see `Literals.DisableProperty`) turns off every analyzer
and generator here. It exists so that shipping the tooling *by default* is cheap to decline, which is
the thing that makes shipping-by-default arguable at all.

**Check it first, before any symbol work** — `Utils.BuildToolsDisabled()` is the shared helper, and
every entry point calls it as its opening line: both analyzers, `ProtoFileGenerator.Execute`, and
`ProtoModelGenerator`'s two source outputs. The promise is that being installed-but-unwanted costs one
dictionary lookup, so anything added here has to keep it.

Note `ProtoModelGenerator`'s parse was already near-free when unwanted, since
`ForAttributeWithMetadataName` only fires for a type carrying `[ProtoModel]`; gating it is about
honouring the switch completely rather than about cost.

### The migration analyzer

`AotMigrationAnalyzer` (`PBN3010`/`PBN3011`) exists because turning the generator on **moves no
existing code onto it**: `Serializer.Serialize(...)` and friends go through `RuntimeTypeModel.Default`,
which reflects. Worse, those call sites keep working under JIT, so the failure lands at publish time.

Three decisions worth keeping:

- **It is silent without a `[ProtoModel]` in the compilation.** The runtime model is a perfectly good
  way to use protobuf-net and this has nothing to say to those users; a analyzer that nags everyone
  would be turned off by everyone.
- **The announcements are reported from a *symbol* action, and that is load-bearing.** A diagnostic
  reported from `RegisterCompilationEndAction` is "non-local", and Roslyn will not offer a code fix
  for one however good its location is — which defeats the point, since `PBN3012`'s value is the
  lightbulb that writes the model. They are anchored on the ordinal-first `[ProtoContract]` in the
  compilation (deterministic, so the squiggle does not wander) and fire exactly once because the
  action tests for that one symbol. The first cut used `Location.None` and an end action, and both
  had to go.
- **Two diagnostics, split on whether the contract type is knowable.** `PBN3010` is the generic case,
  where the fix is mechanical (name the model). `PBN3011` is the `object`/`Type` API, where nothing —
  analyzer or generator — can tell what will be serialized. That one is deliberately *reported rather
  than passed over*: a call site nobody can resolve statically is exactly the kind that fails only
  once ILC has trimmed.
- **A call on any other `TypeModel` is left alone**, including `RuntimeTypeModel.Create()`. That is
  the shape we are asking people to write, and flagging it would be worse than saying nothing.

`[ProtoModel]` is ordinary Core API now, so the analyzer sees it like any referenced type. (While the
attributes were generator-owned this needed post-init sources to be visible to analyzers, which was
verified in a real build; that dependency is gone with the move.) The unit tests still stub
`ProtoModelAttribute`, `Serializer` and `RuntimeTypeModel`: the analyzer matches on full names, the
stub dodges the `[Experimental]` gate, and the test harness references Core (through BuildTools),
which has `TypeModel` but not the other two.

`UseAotModelCodeFixProvider` fixes `PBN3010` by swapping the receiver. It offers anything of the
model's type already in scope (field, property, local, parameter), and then the generated
**`Model.Instance`** — which is why that accessor exists: a codebase part-way through migrating has
no model in scope *anywhere*, so without it the fixer would be useless in exactly the situation it
is for. The alternative, writing `new MyModel()` per call site, would be doing harm tidily: a
`TypeModel` is a cache meant to be built once and reused.

`Instance` is emitted onto every model **except** where the consumer already declares a member of
that name (CS0102 in their build) or the model has no accessible parameterless constructor. Both are
their code, so the answer is to emit nothing rather than to complain.

Alongside it, where the consumer declared **no constructor at all**, the generator emits a non-public
parameterless one — `private` if their model is sealed, `protected` otherwise, since `private` would
stop anything deriving. That removes the *implicit public* constructor, so `new MyModel()` no longer
compiles and `Instance` is the obvious route.

**This has teeth, and the blast radius is the point rather than a surprise:** it also breaks
`Activator.CreateInstance(type)` and any DI container asked to construct the model. Our own harnesses
were built on exactly that and had to move to `nonPublic: true` — `AotConformanceTests` in three
places and `AotDifferential` in one — which is a fair preview of what a consumer hits. Declaring any
constructor opts out completely, which is the escape hatch to point people at.

**The consumers of a `[ProtoModel]` are scattered, and a directory-scoped grep will miss one** — this
broke CI on `AotNodaTimeSmoke`, which was not in the list of projects I checked. The reliable sweep is
`grep -rn "ProtoModel\]" src --include=*.cs -l`, which finds every real consumer:
`AotSmoke`, `DownLevelSmoke`, `AotNodaTimeSmoke`, `AotColdStart`, `AotConformanceTests`,
`AotDifferential` (reflectively) and `AotRefGen` (which links the fixtures but never constructs a
model, so it is unaffected).

The shared-instance form arrives fully qualified, because the analyzer cannot know what is in scope
at the call site; `Simplifier.Annotation` is what reduces it to `MyModel.Instance` on application, and
leaves `global::` only where it is genuinely needed. That annotation lives in Workspaces — available
here, see below.

`PBN3011` is not fixable and never will be; the whole difficulty there is that nobody can tell what
it serializes.

**`PBN3014` is a third case with a different trigger**, added for gap B49: a `.proto` extension
accessor (`obj.GetFooExt()`) whose value is a **message or an enum**, called with no model. Those
resolve through `TypeModel.DefaultModel` — a `NullModel` until something touches
`RuntimeTypeModel.Default`, which a generated-model app never does — and throw *"no serializer could
be resolved"*. Four things about it are worth knowing here:

- it matches by **shape, not name**: protogen lets the accessor class be renamed and the method names
  come from the field names, so the signal is *a static method on a static class that takes a
  `TypeModel` or has a same-named sibling that does* — i.e. the overload pair protogen emits **is**
  the recognition key. Regenerating with a current protogen is therefore part of the fix;
- **only message and enum values**, because a scalar/string/bytes/repeated-scalar extension genuinely
  works with no model (the typed path finds an inbuilt serializer and consults none). That narrowness
  is what makes it defensible as an error;
- **warning by default, error under `Utils.AsksForAot()`** — the failure is not AOT-specific, but a
  project that asked for AOT has no working configuration at all;
- the generated file **would report on its own body** without a carve-out: protogen's legacy overload
  forwards with a literal `null`, which is exactly the reported shape. Suppressed narrowly — a
  same-named sibling call inside the same static class.

`PassModelToExtensionCodeFixProvider` fixes it, and **appends** an argument rather than replacing the
trailing one. That distinction is the whole risk: a setter's last argument is the *value*, so
`obj.SetFooExt(null)` is an ordinary call and overwriting it would silently discard it. The model
parameter is located by **symbol**, and only an argument genuinely bound to it is replaced.

**A note recorded because I got it wrong first:** there is no packaging obstacle to a fixer here.
BuildTools *already* references `Microsoft.CodeAnalysis.CSharp.Workspaces` (`Pack="false"
PrivateAssets="all"`) and already ships three fixers in the same assembly. The compiler ships only
`Microsoft.CodeAnalysis`, `.CSharp` and `.VisualBasic` — no Workspaces — but that does not matter:
csc discovers analyzers from *metadata* and never instantiates a `CodeFixProvider`, so the reference
is inert at compile time. Verified by building an analyzer and a fixer in one assembly against csc:
the analyzer ran and there was no `CS8032`/`CS8034`. Do not pack the Workspaces dll; the IDE supplies
it.

**Every diagnostic id in this assembly, by owner — check this before claiming a block is free:**

| block | owner |
| --- | --- |
| `PBN0001`–`PBN0027` | `DataContractAnalyzer` |
| `PBN1000+` | `ProtoFileGenerator`'s schema errors |
| **`PBN2001`–`PBN2010`** | **`ServiceContractAnalyzer`** (the gRPC analyzers, since #735) |
| `PBN3000`–`PBN3004` | `ProtoModelGenerator` — the language floor and the four drop reasons |
| `PBN3010`–`PBN3014` | `AotMigrationAnalyzer` |
| `PBN4000`–`PBN4014`, `PBN4018` | `GrpcProxyGenerator` — the language floor, the drop reasons, and the AOT escalation |
| **`PBN4015`–`PBN4017`** | **`GrpcMigrationAnalyzer`** — a *different owner inside the same block* |

**Note the `PBN40xx` block has two owners**, and the numbering is interleaved rather than split at a
boundary: `PBN4018` belongs to the generator even though `PBN4015`–`PBN4017` sit below it on the
analyzer. That is not an accident to tidy — the ids were assigned in the order the features landed, and
renumbering a shipped id is worse than an untidy table. It does mean "the 4000 block is the generator's"
is *false*, which is the assumption the story below is about.

**The AOT block was `PBN2000+` until 2026-08-16 and collided with the gRPC analyzers on five ids**
(`PBN2001`–`PBN2004`, `PBN2010`), which shipped that way in 3.3. They are one assembly, so a
severity or suppression is by id and cannot tell the two apart: `dotnet_diagnostic.PBN2002.severity
= none`, to quiet an AOT drop, also silenced *"The data parameter of a gRPC method must be…"* — an
**error** downgraded to nothing. The whole AOT block moved to `PBN3000+` rather than only the five,
keeping the last three digits, so `PBN2001`→`PBN3001` and the mapping needs no table.

**The cause was this very paragraph**, which used to read "AOT generator diagnostics use their own
`PBN2000+` block", enumerate `DataContractAnalyzer` and `ProtoFileGenerator`, and never mention
`ServiceContractAnalyzer` at all — the file had zero occurrences of that name. A list of *some* of
the owners reads exactly like a list of all of them, which is why the table above is exhaustive and
names the assembly rather than the feature.

New IDs **must** be added to `AnalyzerReleases.Unshipped.md`: release tracking is enforced as of
2026-08-18 — `Microsoft.CodeAnalysis.Analyzers` is referenced and `RS2000`/`RS2001`/`RS2002` are
escalated to errors, so an unlisted id fails the build. Proven by deleting an entry and watching
`error RS2000` appear, rather than by observing a green build.

**...but "listed" is only half of it: release tracking discovers ids from `DiagnosticDescriptor`
DECLARATIONS**, not from any analyzer's `SupportedDiagnostics`. That is why the *generator*
diagnostics pass despite having no analyzer behind them — `PBN3020`–`PBN3023` and `PBN4000+` are
declared as descriptor fields. An id reported through the id-and-strings
`Diagnostic.Create(string id, string category, ...)` overload is invisible to it, and the symptom is
`RS2002` saying the id "is not a supported diagnostic for any analyzer" — which points at analyzers
and is therefore misleading, since the fix is to declare a descriptor. `PBN1900` was the last one
reported that way and was converted on the `main` merge; declare a descriptor for anything new.

That kills the *register* half of this problem but not the *ownership* half, which is why the table
above stays: the release file maps id → category/severity/title and never says which type declares
one. The RS10xx analyzer-authoring rules that arrive with the same package are `NoWarn`ed with a
reason in the csproj — declined rather than unexamined.

Historically the table was documentation rather than a build gate, and it *had* drifted: it listed only the AOT
half, while `ServiceContractAnalyzer`'s ten shipped ids were recorded nowhere at all — which is the
other half of how this happened.

Separately, `PBN9001` is not an analyzer diagnostic at all: it is the `[Experimental]` id on
`ProtoModelAttribute`/`ProtoSurrogateAttribute`, so it is an **error** by default and a consumer
opting into the generator must suppress it. Anything that compiles a model programmatically has to
suppress it too — see `src/AotCoverage`.

**`PBN9002` is the same thing for the raw reader/writer surface, and carrying it is POLICY**
(Marc, 2026-08-19): *every new Raw API gets `[System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]`*,
without exception, for as long as that surface may still reshape. It is not decoration — it is the
one thing standing between "we may still change this" and a consumer depending on it.

This is easy to forget because **it costs nothing internally and so nothing fails when you omit it**:
`src/Directory.Build.props` carries a repo-wide `NoWarn` including `PBN9002`, deliberately, since the
repo consumes its own raw surface throughout. So an unmarked raw member builds, tests, and ships
looking exactly like a marked one — the only difference appears in a *consumer's* build, which no
gate here exercises. The packed write surface (sixteen `WriteRawPacked*`/`MeasureRawPacked*` members)
shipped unmarked for precisely that reason and was corrected on 2026-08-19.

The tracking file records it as a prefix — `[PBN9002]ProtoBuf.ProtoWriter.State.WriteRawVarint32(uint
value) -> void` — so a missing attribute is *visible* there if anyone reads it, which is the cheapest
place to notice.

Note the shipped analyzer still compiles against the low Roslyn baseline (4.3.1), which predates
`LanguageVersion.CSharp12` — hence the numeric constant in `ProtoModelGenerator`. `BuildToolsUnitTests`
carries a `VersionOverride` to 4.8.0 purely so its in-memory compilations can parse C# 12.

**`protobuf-net.BuildTools.Legacy` compiles a hand-picked subset of these sources, and the two
inclusion styles differ** — analyzers are listed **by name**, code fixes by **glob**. So a new
analyzer is invisible to Legacy while a new *fixer* is pulled in automatically, and a fixer that
references an analyzer Legacy does not have is a build break in a project you were not thinking
about. That is exactly how `UseAotModelCodeFixProvider` broke CI; it is now explicitly `Compile
Remove`d, which is the right answer anyway — Legacy serves very old SDKs, which never get
`ProtoModelGenerator`, so no `[ProtoModel]` can exist and the whole AOT migration story is inert
there. Build `protobuf-net.BuildTools.Legacy` after touching `CodeFixes/`.

**Don't rev the central Roslyn version speculatively.** Only bump it if we genuinely need a modern
Roslyn API we cannot work around — e.g. detecting a language feature we actually use. The old
baseline is what lets `protobuf-net.BuildTools.Legacy` serve very old SDKs; those users are not doing
AOT by definition, so none of the AOT generator's requirements apply to them.

### Everything the generator does, feature by feature → `notes/aot/generator-reference.md`

The per-feature reference used to live here and made this file ~2,500 lines, all of it loaded into
every session whether or not it was needed. It moved out on 2026-08-25, **verbatim**. Go there for:

| | |
| --- | --- |
| **collections** | the provider walk, why it cannot be a lookup table, packing, element wire types |
| **maps** | the factory shapes, `[ProtoMap]` per-side formats, `OptionFailOnDuplicateKey`, enum sides |
| **inheritance** | `[ProtoInclude]`, interfaces as roots, out-of-band `[ProtoSubType]` |
| **surrogates** | `[ProtoContract(Surrogate=)]`, hand-written serializers, `[ProtoSurrogate]`, NodaTime |
| **compatibility level** | the 200/240/300 table for the four BCL types, and `[ProtoDataFormat]` |
| **members** | fields, getter-only, `init`/non-public setters, `ShouldSerialize`/`Specified`, `ImplicitFields` |
| **types** | generics, enums as contracts, `nint`/`DateOnly`/`TimeOnly`, `System.Uri`, parseable types |
| **options** | `[ProtoPartialMember]`/`[ProtoPartialIgnore]`, `IsGroup`/`IgnoreUnknownSubTypes`/`UseProtoMembersOnly`, schema-only options |
| **other shapes** | extensible contracts, null-wrapping, serialization callbacks, identifiers and `extern alias` |

**Read it before touching the emitter for any of those**, because most of what is in it is a fact
probed against ref-emit rather than a design choice, and several are the opposite of the obvious
guess. What stayed here is what a session needs *before* it knows which of them it is in.

### Telling our gaps from protobuf-net's

Several refusals are **matches** rather than shortfalls: protobuf-net throws for them too, so there is
no behaviour to reproduce and nothing outstanding. These were established by probing
`RuntimeTypeModel`, and their diagnostics quote what it says so they stop reading as our backlog:

| shape | what protobuf-net does |
| --- | --- |
| no parameterless constructor, no `SkipConstructor` | throws *"No parameterless constructor found"* |
| a member type that is not a contract | throws *"No serializer defined for type"* |
| lone `[NullWrappedValue]` on a non-scalar / non-nullable, or with `[DefaultValue]` / `IsRequired` / `IsPacked` / a `DataFormat` | throws |
| `[NullWrappedCollection]` on a non-collection | throws *"can only be used with collection types"* |
| `[ProtoInclude(tag, "TypeName")]` | resolves at runtime; throws *"Unable to resolve sub-type"* even for a live type |
| a member or sub-type on a `[ProtoReserved]` number or name | throws *"Field 31 is reserved and cannot be used for…"* |
| a **value-type** `[ProtoInclude]` sub-type | throws *"Unexpected sub-type"* |
| a **multi-dimensional array** (`int[,]`) | throws *"Repeated data of type System.Int32[,] is not supported"* |
| a **jagged or nested** collection (`string[][]`, `List<int>[]`, `List<List<int>>`) | throws *"Nested or jagged lists, arrays and maps are not supported"* |
| one type named by **two** `[ProtoInclude]` hierarchies | throws *"can only participate in one inheritance hierarchy"* |
| `DataFormat.Group` on a collection of **scalars** | throws *"Operation is not valid due to the current state of the object"* |
| an **auto-tuple** reached at two compatibility levels | throws *"must use a single compatibility level"* |

That list has grown a good deal, and the direction is worth noting: every addition *lowers* the
sweep's "% emitted" while raising correctness, because a contract protobuf-net will not build a
serializer for was never usefully emitted. The two numbers measure different things.

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

**See `notes/gaps.md`** - every gap, with the decision taken against it. As of 2026-08-14 there
are exactly two the generator refuses that protobuf-net itself would handle, both decided
*keep omitting*: a collection as a map key, and a hand-written serializer as a map key or value.

Everything else it refuses is a **match** with protobuf-net rather than a shortfall - see
"Telling our gaps from protobuf-net's" above, which stays here because it is about how to READ
a refusal, not about what is outstanding.

**The ranked candidate list lives in the "Next steps" section of `notes/aot/findings.md`**, with the
reasoning for the ordering. Keep it there rather than scattering next-step opinions through this
file, as had started to happen.

### Golden-file tests

`src/BuildToolsUnitTests/Aot/` pairs each `Data/*.input.cs` with the exact code it generates
(`*.output.cs`) and the diagnostics it reports (`*.txt`).

**The tests rewrite those goldens in the source tree on every run**, then assert. So:

- a new fixture fails on its first run (nothing to compare against) — re-run, then review `git diff`;
- a behaviour change shows up as a diff to read, not an assertion to appease;
- don't hand-edit a golden to make a test pass — fix the generator and re-run.

**They only write back in DEBUG.** `WriteBack` locates the source tree from `[CallerFilePath]`, and
a Release build stamps deterministic paths (`/_1/...`) that do not exist on disk, so it silently
returns. Run the golden tests in **Debug** to regenerate; a Release run just fails twice and writes
nothing, which looks exactly like a generator producing unstable output.

**The goldens COMPILE their output** (`Assert.Equal(0, result.ErrorCount)`), not merely diff it —
which is the only reason a change that emits a local in one code path but declares it in another is
caught at all. Worth remembering before "simplifying" that assertion away.

**A `.txt` golden pins diagnostic LINE NUMBERS**, so editing a fixture's header comment moves them
and fails the test for a reason that has nothing to do with the generator.

`Data/*.cs` files are excluded from compilation via `<Compile Remove="Aot/Data/**/*.*.cs" />` and
copied to the output directory instead.

### Reading what the generator actually emitted

`-p:EmitCompilerGeneratedFiles=true`, and then look under
`obj/<config>/<tfm>/generated/protobuf-net.BuildTools/...`. Two traps, both hit on 2026-08-16:

- **without that flag the generated files are never written to disk at all**, so an empty (or
  absent) `generated` folder is indistinguishable from a generator that emitted nothing. A grep
  returning zero is not evidence until you have checked the files exist;
- **the folder survives a branch switch**, so a stale file from another branch reads as current.
  It is also incremental: if nothing recompiled, nothing is rewritten. `--no-incremental` and a
  timestamp check are the cheap guards.

Fixture conventions:

- **One namespace per fixture** (`namespace AotFixtures.Simple;`). Every fixture is linked into a
  single assembly by both `AotRefGen` and `AotConformanceTests`, so unqualified names would collide.
- `<Name>.input.cs` declares model type `<Name>Model`, and may declare `<Name>Samples.Values`
  (a `public static object[]`) supplying the values the differential tests exercise.
- A sibling `<Name>.langver` file pins the parse language version for that fixture — used to prove
  the `PBN3000` floor fires.
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

Note the project needs `<LangVersion>12.0</LangVersion>` (net4x defaults to 7.3, below the `PBN3000`
floor) and its own `IsExternalInit` polyfill, since net472 cannot even *compile* `init` without one.

An in-memory test was tried first and abandoned: the test process is net8.0, so its reference set
always supplies `UnsafeAccessorAttribute`, and a reference set thin enough to exclude it is too thin
to compile the input (CS0518). Being a real down-level project is the only honest version of this.

### Native AOT smoke test

`src/AotSmoke` is a `PublishAot` console app that round-trips through a generated model and returns
a non-zero exit code on mismatch. It is the only thing here that proves the actual goal; everything
else runs on a JIT runtime where ref-emit still exists.

**Whatever it does not cover is not "fine", it is unmeasured** — and that distinction has already
cost twice. Maps had *no* native coverage at all until recently, which silently made every
`MapSerializer` annotation look harmless; adding three map members moved the warning count 20 → 29
on the spot. Before concluding that some area is clean, check there is a member here that reaches
it. Adding one moves the baseline, so re-measure both sides when you do.

Two members earn their place for reasons that are not obvious from reading them:

- the **hand-written serializers** (`Barcode`/`Gauge`/`Batch`) are the only members that reach
  `SerializerCache.Get<TProvider, T>()`, and so the only ones exercising
  `Activator.CreateInstance(typeof(TProvider), nonPublic: true)` — the last genuinely reflective step
  on the generated path, held open by `DynamicAccess.Serializer` alone. That annotation has gone
  missing once already, and the symptom was a `MissingMethodException` on first serialize that
  nothing but a native publish could catch. All three categories are present because the category
  changes the *framing*, and the payload is worth checking by eye: field 30 is a bare varint and
  field 31 a bare fixed32, where a wrongly-assumed message category would have written a length
  prefix over them.
- `Dictionary<int, List<int>>` and `List<Status>` are the two shapes that pass **no** serializer and
  resolve one from the model through `ISerializerProxy`, i.e. the arm `ResolveSerializer` gates.
- the **`.proto`-generated DTO tree** (`FileDescriptorSet`, from `src/AotSchemaDtos`) is the only
  member here that is not hand-written, and it reaches shapes the rest do not: getter-only
  collections restored through backing-field accessors, `ShouldSerialize*` on nearly every member,
  `IExtensible` throughout, and a type that nests inside itself. Everything else in the repo tested
  that path on a **JIT** runtime only.

  **It has to be a separate project**, and that is a constraint worth knowing before trying to
  simplify it: source generators all run against the same input compilation and never see each
  other's output, so a `[ProtoModel]` in the same project as the `.proto` gets an *error symbol* for
  the seed — one with a name but no attributes, which used to be reported as "not marked
  `[ProtoContract]`". `PBN3002` now recognises `TypeKind.Error` and names the fix.

**Packed columns** were added 2026-08-15 (`Readings`/`Offsets`/`Flags`/`Levels`) and are the only
members reaching the raw packed surface — the SIMD blit, `Vector<T>` under ILC, and the
`MemoryMarshal.Cast` enum pun, none of which any other member touches. 40 elements each so the
vector path engages with a ragged tail; a short column would exercise only the scalar fallback.
Adding them moved the warning count by **zero** (19 → 19), which is the opposite of what the map
members did (20 → 29) and is explained by the surface being pure span work with no metadata demand.

The feature sweep is **complete** as of this branch: every generator feature that was listed as
natively unexercised now has a member here — the immutable *reference* families, both callback
families, `Specified`/`ShouldSerialize`, both `ImplicitFields` modes, `DateOnly`/`TimeOnly`/`nint`,
a parseable type and `[DefaultValue]`. Three of those need their sample chosen so the check can
actually fail, which is easy to lose in an edit:

- `Retries` is `[DefaultValue(5)]` **set to 0**, so it is written and must come back as 0 rather than
  as the initialiser's 5. Set to anything else, the guard's operand is untested;
- `Conditional.Explicit` is **zero with `ExplicitSpecified = true`**, which is the only combination
  that distinguishes a `Specified` guard from the trivial-value one it replaces;
- `Sorted`'s members are declared `Zebra, Apple, Mango` and asserted as `a/m/z`, since implicit
  numbering sorts by name rather than by declaration order.

`AllowParseableTypes = true` on the model is required by the `IPAddress` member and applies to the
whole model — harmless here only because nothing else in it has a `static Parse(string)`. A type that
grew one would silently move from message to string.

Note `Debug.Assert` in the generated services constructor — the one checking a stated `IsScalar`
against the serializer's real `Features` — is `[Conditional("DEBUG")]` against the *consumer's*
compilation, so a Release publish never runs it. `dotnet run --project src/AotSmoke -c Debug` does,
and is worth doing after touching anything in that area.

```
dotnet publish src/AotSmoke/AotSmoke.csproj -c Release -r win-x64
```

`vswhere.exe` must be on `PATH` (`%ProgramFiles(x86)%\Microsoft Visual Studio\Installer`) or ILC's
link step fails with a mangled command line — the error names `link.exe`, which is misleading.

`-r linux-x64` also works, with the platform toolchain and no `vswhere`. The **warning count is the
same on both** (21 at the time of writing), so either RID is a valid measuring stick for it; the
**byte sizes are not comparable across RIDs**, so measure a delta against a baseline taken on the
same machine rather than against a figure recorded here.

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

**The same mistake was hiding one interface family over, and cost 808 KB.** `IProtoInput<TInput>`,
`IProtoOutput<TOutput>` and `IMeasuredProtoOutput<TOutput>` carried `DynamicAccess.ContractType` on
the **transport** type — `Stream`, `byte[]`, `ReadOnlySequence<byte>`, `IBufferWriter<byte>` — of
which `TypeModel` implements nine instantiations. Nothing reflects over a stream; the contract is
`T` on `Deserialize<T>`, and that one was never annotated. Because the mask includes nested types,
ILC kept **1738 framework members** reflectable (`Task` 520, `Array` 160, `Enum` 99, `Stream` 72, …)
and *no* protobuf-net members. Removing the three annotations: **34 → 20** warnings and
**3.52 MB → 2.73 MB**, i.e. **22.4%** of the native binary. Item 4 of `notes/aot/findings.md` has the
measurement and the tracing recipe.

So the standing rule is: **before annotating a type parameter, ask what would reflect over it.** A
transport, a buffer, a destination — nothing does. `TInput` survived the `ISerializer<T>` cleanup
purely because the name reads like a contract type.

**The rule cuts both ways, and the other direction is a live bug rather than dead weight.**
`TCollection` on the repeated/map serializers had *never* been annotated, yet
`TypeModel.ActivatorCreate<TCollection>()` constructs the collection when a member arrives null —
so ILC trimmed the constructors and every `HashSet<T>`/`Queue<T>`/`ConcurrentQueue<T>` member threw
`MissingMethodException` on **deserialize** (item 4b). The answer to "what reflects over it" was
"`Activator` does", and the fix is `DynamicAccess.Activated` —
`PublicParameterlessConstructor | NonPublicConstructors`, and deliberately nothing else, since the
collection is constructed but never inspected. Applied to `ActivatorCreate<T>`, the concrete
serializers that call it, and the public factories that name them.

Note `List<T>` worked throughout, purely because application code elsewhere calls `new List<T>()` —
so the long-standing `List<T>` coverage in `AotSmoke` was evidence about nothing.

`Requires*` attributes were tried on the dynamic helpers and **reverted** — they do not remove
warnings, they relocate them to callers, and the callers here are
`ProtoReader.State.DeserializeRootImpl<T>`, `TypeModel.CreateInstance<T>` and
`TypeHelper<T>..cctor()` — i.e. the *generic paths generated models use*. The reflective fallbacks
are entangled inside the AOT-safe paths rather than sitting behind a boundary, so `Requires*` has
nowhere clean to terminate. Fixing that properly means **restructuring** (the generic path must not
reference the fallback at all), not annotating.

**...but a feature switch does terminate cleanly, which `Requires*` could not**, and that is now
`TypeModel.ResolveSerializer<T>`. Gating a fallback on `RuntimeFeature.IsDynamicCodeSupported` lets
ILC substitute a constant and eliminate the arm *before* trim analysis, so the demand disappears
rather than moving to the caller. A generic utility ending `serializer ??= GetSerializer<T>(model)`
must otherwise declare `DynamicAccess.ContractType` on its own `T`, and every instantiation inherits
that — including generated models, which pass a serializer and never reach the fallback.

Applied in two passes, measured separately: `RepeatedSerializer<TCollection, TItem>` took **49 → 45**
(`IL2091` 11 → 7), and the writer/reader cluster took **45 → 34** (`IL2091` 7 → 2). Every annotation
named below is gone as a result.

**The cluster is one unit and cannot be done piecemeal.** `WriteAny`, `WriteWrapped`, `WriteMessage`,
`WriteGroup`, `WriteWrappedItem`, `ReadMessage`, `ReadAny`, `ReadWrapped` and `ReadRepeatedCore` all
hand `T` to each other, so half a treatment relocates instead of removing — which is precisely what
the `RepeatedSerializer` pass did, surfacing two fresh warnings at `RepeatedSerializer.Write`. What
makes it safe is that the *callers* already supply the serializer, so the annotation was only ever
serving a fallback they never take. `MapSerializer<TCollection, TKey, TValue>` and the two
`External*Serializer` bases followed, on the same reasoning.

Deliberately excluded: the root entry points (`DeserializeRoot`/`SerializeRoot`/`ReadAsRoot`/
`WriteAsRoot`) and the two `GetSerializer<T>()` accessors — an explicit resolution request is not a
fallback, so the annotation is honest there.

**`MapSerializer` was excluded once on the grounds that it "produces no warning today", and that
reasoning was wrong** — `AotSmoke` simply had no map member, so the whole family was *unmeasured*
rather than harmless. Adding three map members moved the baseline 20 → 29 and `IL2091` 2 → 11, all
of it previously invisible. "No warning" is only evidence if the code is on the measured path.

Note the two arms are the *same resolution*, deliberately: a generated model resolves a repeated enum
through it — we pass no serializer and rely on `ISerializerProxy<TEnum>` — so an arm that threw would
break exactly the shapes `AotSmoke` covers.

**Only a native publish exercises the AOT arm at all**; a JIT run takes the other one, so the
differential suite says nothing about this. That is why `AotSmoke` carries `List<Status>`,
`List<Customer>`, and the three map members — the repeated enum and the repeated *map value* being
the sharp cases, since resolution has to find the proxy through the model with nothing passed in.

**The element type is not a contract type either**, and this was the last big one. The public
repeated and map factories annotated their *element* — `T`, `TKey`, `TValue` — with
`DynamicAccess.ContractType`, so every collection or map member demanded a fully-reflectable element.
Nothing inside those serializers reflects over it: the element's serializer is passed in by the
caller or resolved through the `IsDynamicCodeSupported`-gated arm, which ILC substitutes away. The
*class-level* annotations were removed when that gate went in; the **public factories that name them
were missed**, and those are exactly what generated code calls. Removing them: **39 → 25 warnings and
−385 KB (−8.9%)**, with nothing relocated.

`TCollection` keeps its `DynamicAccess.Activated` on the same methods, and that is the rule working
rather than an exception to it — `ActivatorCreate<T>()` genuinely constructs the collection. Two
parameters of one method, opposite answers, because the question is always "what would reflect over
*this* one".

**The enum connection is worth knowing, because it disguises the cause.** Reflection returns
*inherited statics*, so `typeof(SomeEnum).GetMethods()` includes `Enum.GetValues` and `Enum.Parse` —
both `RequiresDynamicCode`. So any `PublicMethods` demand that lands on an enum type parameter emits
`IL3050` naming `Enum.GetValues`, which reads as an enum-serialization problem and is really an
annotation-scope problem. It was the element annotation on `List<Status>`, not the enum member.

**`ThrowUnexpectedSubtype<T>` was the last one, and it was the widest**, because the generated code
emits that call for *every* non-sealed reference-type contract — so its `ContractType` demand applied
to the whole model. Nothing in it inspects `T`: `IsSubType<T>` is `typeof(T) != value.GetType()`, and
the pair then goes to the `Type`-based overload, whose parameters carry no demand. Removing it:
**25 → 19 warnings and a further −277 KB (−7.0%)**.

That is also where the `Enum.GetValues` reports came from, and the route is worth following once
because no part of it is guessable: `ContractType` includes `PublicNestedTypes`; a `.proto` DTO nests
its enums *inside* the message (`FieldDescriptorProto.Label`, `FieldOptions.CType`, …); so the demand
kept those enums reflectable, and an enum's reflectable members include the statics it inherits from
`System.Enum`, which are `RequiresDynamicCode`. Four contracts, six warnings, one annotation.

**Do not try to fix an `Enum.GetValues` report by switching to `Enum.GetValues<T>()`.** The advice in
that warning's text is boilerplate from the attribute and is aimed at callers; there is no call here
at all — protobuf-net contains no `Enum.GetValues` call in any shipped assembly. The lever is always
the annotation that demanded the metadata.

**On this branch the count is 6** on win-x64, measured: 3 `IL2067` and the spent `IL2091` trio —
`CreateInstance` ×2 (whose fallback is genuinely live) and `SubTypeState<T>.Cast` (which would need
the annotation on every consumer). It was 5 before the `Dictionary<int, List<Customer>>` member went
in for #1337, which added one `IL2067`; B48 in `notes/aot/findings.md` is where the 23 → 5 came from
and why it paused there.

The paragraphs above are **main's** history and stop at *its* number — 20, having been 19 before that
same member and 21 before the `.proto` DTO tree. Do not read them as a v4 baseline: none of these
figures are comparable with each other, since the count tracks both fixtures and the annotation work
on the branch.

**Watch bytes as well as warnings — they do not move together.** Two changes of identical shape:
removing the transport annotation was −14 warnings and **−808 KB**; removing the `MapSerializer`
family's was −8 warnings and **exactly zero bytes**, because a map's `TKey`/`TValue` are contract
types the generated model's `GetSerializer<T>` override annotates regardless. A sweep of every
remaining `[DynamicallyAccessedMembers]` in Core found one further redundancy —
`SerializerCache<TProvider, T>`'s `T`, which nothing in that class reflects over — and removing it
was measured as **byte-for-byte identical**, so it was left in place. `TProvider` there is the
load-bearing one.

**Split them by source location, not by id.** A warning with a `file:line` is a reflective call in
our source; one attributed to a bare type name with no location is a member kept *reflectable* by a
`DynamicallyAccessedMembers` demand and never called — which is a metadata-size problem wearing a
warning's clothes, and is where the 808 KB above was found. There are currently none of the latter.

**Measure with a publish rather than reasoning about them** — clear `obj`/`bin` first, since the
publish is incremental and a second run reports nothing at all, and re-measure the *baseline*
whenever a fixture changes, since the count tracks fixtures. **Watch the binary size too**, not just
the count: the two do not move together, and the largest win so far was invisible in the count.
`notes/aot/findings.md` A2 no longer quotes a floor — every estimate so far was beaten by the next
measurement.

When a warning needs attributing, get the graph rather than guessing —
`/p:IlcGenerateDgmlFile=true`, then walk *incoming* edges from the offending member and read each
edge's `Reason` attribute, which names the responsible generic parameter. Two confident hypotheses
were wrong before that was tried; item 4 records both.

### Build-time gRPC proxies (`GrpcProxyGenerator`)

**`notes/aot/grpc.md` is the reference. It opens with a Handover section, followed by a "Plan forward"
that is written for a cold start** - the ordered queue, what is declined and why, where to be
suspicious, and the standing verification recipe. Read both before touching
`Generators/GrpcProxyGenerator.*` or `Internal/Grpc/`. The short version:

- it is a *second* generator in BuildTools, triggered by `[ProtoGrpc]` on a consumer-declared
  `partial class X : ClientFactory`, seeded by `[ProtoService(typeof(IContract), typeof(Impl))]`, and
  linked to a `[ProtoModel]` by `Model = typeof(...)`;
- **the link to the serializer model is the whole point.** Generated proxies with marshallers left on
  `BinderConfiguration.Default` are AOT-safe code carrying reflectively-built bytes — the build
  succeeds and ILC is where it fails. `MarshallerCache.CreateMarshaller<T>` gates on
  `CanSerialize(typeof(T))`, which reaches `DynamicStub` → `MakeGenericType` and returns **false**
  under AOT. The generator pre-registers marshallers via the public
  `BinderConfiguration.SetMarshaller<T>` to sidestep it; that is load-bearing, not an optimisation;
- the *generated code* needs no protobuf-net.Grpc changes — `ClientFactory`'s two members are already
  abstract, and `IServiceMethodProvider<T>` is registered via `TryAddEnumerable` so a generated
  provider is added alongside. `[ProtoGrpc]`/`[ProtoService]` are real API in **protobuf-net.Grpc
  1.3.0+** (they were generator-owned post-init while the shape moved, as `[ProtoModel]` was), and are
  matched by **full name** — keep it that way, the tests stub them;
- `Internal/Grpc/` is subject to the same no-Roslyn-references rule as `Internal/Aot/`, and is
  explicitly `Compile Remove`d from `protobuf-net.BuildTools.Legacy` because `Internal/**` is a glob
  there — the same trap `UseAotModelCodeFixProvider` hit;
- `src/AotGrpcSmoke` is the only thing that proves the goal (real client, real server, real socket,
  real packages, native publish). `Grpc/Data/_ContractSurface.cs` is a *snapshot* for the goldens and
  can drift — the smoke project is what catches it, and already has once;
- `src/AotGrpcMetadataDiff` is the endpoint-metadata oracle: it reconstructs each endpoint's metadata
  from symbols, **compiles and runs** it, and compares the objects against the real
  `ServiceBinder.GetMetadata`. It gates CI, and it exists because a dropped `[Authorize]` is a more
  permissive endpoint with no error anywhere. Note it reaches BuildTools through an **`extern alias`**
  — it references the real protobuf-net.Grpc, so the usual Core-ambiguity applies.

Diagnostics are `PBN40xx` — `PBN3xxx` is the AOT serializer generator's since #1283, and `PBN2xxx` is
`ServiceContractAnalyzer`'s. **The block has two owners**: `PBN4000`–`PBN4014` and `PBN4018` are the
generator's, `PBN4015`–`PBN4017` are `GrpcMigrationAnalyzer`'s. See the id table earlier in this file,
which is the only exhaustive one. `AnalyzerReleases.Unshipped.md` is the register of what is taken;
check it before adding an id, and add the id to it.

Beyond the generator, three pieces are worth knowing about before touching this area:

- **`GrpcMigrationAnalyzer`** is the gRPC counterpart of `AotMigrationAnalyzer`, and its trigger is
  *consumer-side usage* rather than the presence of service contracts — shipping `[Service]` interfaces
  in a shared package is the recommended layout and needs no `[ProtoGrpc]`, so triggering on
  declarations would nag hardest at the project laid out correctly. `Utils.AsksForAot` is shared with
  the serializer analyzer rather than re-listing the four MSBuild properties.
- **`UseGeneratedClientFactoryCodeFixProvider`** fixes `PBN4016` by inserting the factory argument, and
  is `Compile Remove`d from Legacy — `CodeFixes/**` is a glob there while analyzers are listed by name,
  the same asymmetry that made `UseAotModelCodeFixProvider` a build break.
- **the interceptor half** (`GrpcProxyGenerator.ParseIntercept.cs` / `.EmitIntercept.cs`,
  `Internal/Grpc/InterceptorSupport.cs`, `InterceptableLocations.cs`) rewrites plain
  `CreateGrpcService<T>` calls. Two constraints there are easy to break: the location payload is
  obtained by **reflecting** into the host's `GetInterceptableLocation` (Roslyn 4.11+) so the shipped
  baseline can stay at 4.3.1 — `BuildToolsUnitTests` overrides Roslyn to 4.11 *only* so this is
  testable in-process; and nothing may be emitted unless the consumer enabled the namespace, because
  `CS9137` is an **error**. `notes/aot/grpc.md` records the encoding, which was reverse-engineered and
  proven by hand, as the fallback if that reflection ever stops working.

### Coverage sweep

`src/AotCoverage` runs the generator over every `[ProtoContract]` in the already-built
`protobuf-net.Test`, `Examples` and `protobuf-net.Reflection.Test` assemblies and tallies what it
could and could not handle, grouped by reason. It exists so that "what should the generator support
next" is answered by counting real contracts rather than by guessing; `notes/aot/coverage.md` is the
last snapshot. Build those three projects first — it seeds from **metadata**, not source.

Two artefacts to know about: it can only seed types a `typeof(...)` in another assembly can name, so
non-public and open-generic contracts are reported as "not seedable" rather than analysed; and
because it flattens every dll beside the targets into one reference set, `CS0433` in its output means
two scanned assemblies declare the same type name, not a generator fault. That one is now reported
under its own "harness artefact" heading rather than counted as a generator bug, so the "does the
emitted code compile" line means what it says.

It writes to stdout and the snapshot carries a hand-added header line, so regenerating it is
`{ header; dotnet run --project src/AotCoverage; } > notes/aot/coverage.md`, not a plain redirect.

### Differential sweep (the corpus, on bytes)

`src/AotDifferential` is the coverage sweep's other half. `AotCoverage` proves the generated code
**compiles**; this one *runs* it, comparing bytes against `RuntimeTypeModel` for a populated instance
of every contract. That is the property that actually matters — every serious bug this generator has
had (the `DataFormat` cast that mis-mapped, the BCL element wire types, `OverwriteList` on a bytes
member) compiled perfectly and wrote the wrong bytes. `notes/aot/differential.md` is the last snapshot.

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
- **Assembly-level `[ProtoSurrogate]` declarations are replayed onto the reference model.** The
  generator gathers those from referenced assemblies — that is how `protobuf-net.NodaTime` makes
  `Instant` serializable — and a `RuntimeTypeModel.Create()` knows nothing about them, so without
  the replay the reference throws where we correctly emit a surrogate, which reads as a generator
  fault and is the opposite. The declaring assembly is usually not loaded (a package shipping
  surrogates for types it does not own is exactly that shape), so the neighbours are loaded eagerly
  first. Matched by full name — the generator is loaded reflectively here, so the Roslyn-side
  attribute symbol can never be identity-equal to anything this harness holds.
- **Values are deterministic and every scalar differs from the last.** Two members holding the same
  value serialize identically under either numbering, so a swapped field number would be invisible.
- **Half the corpus is `.proto`-generated** (`Schemas.cs`): the schema tree under
  `protobuf-net.Reflection.Test/Schemas` is parsed, run through `CSharpCodeGenerator` and compiled
  in-process into a `SchemaCorpus` assembly, which is then seeded exactly like the other three
  targets. It is generated at run time rather than checked in, so it cannot drift from the codegen
  that produced it; `PBN_NO_SCHEMAS=1` skips it, since it is the slowest part of the run (~25s total).

  This is not "more corpus", it is a **different distribution**, and that is the point: people do not
  write `public int @case`, and machine-generated contracts do. It found a bug that broke the
  consumer's *build* on its first run — item 14 of `notes/aot/findings.md`.

  Two things about it are load-bearing. The DTO assembly must be compiled against **the same
  reference set the corpus loads**, or every contract gets a second, incompatible
  `ProtoContractAttribute`; that is why it is built inside `Corpus.Build` rather than by the caller.
  And collisions with the hand-written corpus are resolved by **dropping the schema copy** — the
  hand-written one is already being compared, so the duplicate adds nothing, and the type still
  resolves for schemas that reference it because the declaring assembly is in the reference set.
  Note the collision probe must ask **each assembly** rather than the compilation:
  `Compilation.GetTypeByMetadataName` returns null for an *ambiguous* name, and these names are
  ambiguous by definition, so the compilation-level lookup silently misses exactly what it is for.

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
`AotRefGen`; see the "Retracted" section of `notes/aot/findings.md`. Regenerate before concluding
anything from a member that is missing, and commit the regenerated file in the same commit as the
fixture change so the two cannot drift.

**That is a gate now, not merely advice** — `ReferenceProvenanceTests`, which arrived from `main`.
`AotRefGen` stamps each `*.reference.cs` with the fixture it came from and that fixture's sha256
(`src/AotRefGen/ReferenceProvenance.cs`), and the test fails on a missing or stale stamp. It earned
itself on the `main` merge by catching `DynamicCategory.reference.cs` describing five members where
its input declares six — #1275 had added one and the reference was never re-run. Nothing else could
have found it: the goldens compare us against *ourselves*, and no test consumes `.reference.cs`.

Note **the failure is normal for a contributor and is not carelessness** — `AotRefGen` is net472 and
Windows-only, so anyone else literally cannot regenerate. Expect to run it yourself when merging
fixture changes from elsewhere.

Two operational traps, both hit while clearing exactly that:

- **it takes no arguments**, so `dotnet run --project src/AotRefGen --nologo` passes `--nologo`
  through and it fails with `fixture directory not found: ...\--nologo`;
- **the test reads the reference from the test project's OUTPUT directory**, since `Data/**` is
  copied rather than compiled. So regenerating and then running with `--no-build` re-checks the
  *stale* copy and fails; rebuild first. Same shape as the stale-generator trap in `notes/gaps.md`
  B27.

- Fixture convention: `<Name>.input.cs` declares model type `<Name>Model`, giving `<Name>.reference.cs`.
- Contract types in fixtures must be `public` — full ref-emit compilation only reaches public members.
- the trigger attributes come from protobuf-net.Core like any other consumer (`TriggerAttributes.cs`,
  which duplicated them while they were generator-owned, is gone).
- **It is net472, so it does not run on Linux at all.** Anything added from there has no
  `.reference.cs` until someone runs it on Windows.

**Every fixture without a `.reference.cs` now says why, in its own header**, because an absence and a
genuinely-empty ref-emit output look identical and that has already caused one wrong conclusion.
None of the absences is work — each is a shape ref-emit cannot produce output for:

| fixture | why |
| --- | --- |
| `NonPublicSetter`, `NonPublicCtor`, `InheritAccessor`, `ImplicitPrivate` | ref-emit's *compiled* path refuses the shape outright ("Non-public member cannot be used with full dll compilation"), so there is no output to compare |
| `TrivialGetter` | no reference behaviour exists — we are strictly more capable there |
| `DateOnly` | `<Compile Remove>`d from `AotRefGen`, which is net472 and has no `DateOnly` |

Two artefacts of decompilation are cosmetic, not semantic: `Features` appears as a uniquely-named
method plus an ILSpy `.override` note (it's really an explicit-interface property), and
"Error decoding local variables" reflects ref-emit's empty locals signature.
