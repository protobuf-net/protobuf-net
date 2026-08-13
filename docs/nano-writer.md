# The writer arc: measure-first, hoisted from the 2023 prototype

The read arc is released (4.0-alpha); this is the writer's planning doc, sibling to
`docs/nano-core.md`. Step zero, per Marc: hoist the writer shape from the old prototype
branch — now `origin/v4-prototype-2023` (formerly named `v4`, renamed when v4 became the
release feature branch) — "paying attention to the *measure*, which is the biggest single
change in direction".

## The prototype shape (all paths at `origin/v4-prototype-2023`)

- `src/protobuf-net.Nano/Nano/PrepWriter.cs` — the writer: a `ref struct` over one buffer
  region (`_buffer`/`_index`/`_end`, `IBufferWriter<byte>`-backed, 300KB region up front in
  the POC), with `USE_SPAN_BUFFER` as a kept A/B. Write primitives are unrolled by measured
  length (`lzcnt` bit-count selects the byte count, then straight-line stores — the
  EncodeResults.md tables), with a capacity test per call that the presized-region design
  makes always-true on the hot path.
- `Measure` statics are PURE ARITHMETIC: `MeasureVarint32/64` via `lzcnt` (or the shift
  ladder down-level), `MeasureWithLengthPrefix(bytes) => MeasureVarint64(bytes) + bytes`,
  string measure via `UTF8.GetByteCount`. No state, no buffer — this is the 10-12× result
  in the v4 tables, and why serialize was 3-4×: write targets a single pre-sized region
  with no growth checks and one flush.
- `src/protobuf-net.Nano/Nano/INanoSerializer.cs` — the three-method contract:
  `T Read(ref reader)`, `long Measure(in T)`, `void Write(in T, ref writer)`.
- `src/Benchmark/Nano/HandWrittenNoPool.cs` (+Pool/Slab variants) — the generated-code
  shape to emit.

## The composition rule — the direction change, verbatim from the prototype

**Write re-calls Measure for every nested length prefix.** The hand-written shape is:

```csharp
// root: measure once, size the region, write with no checks
var len = checked((int)Measure(value));
ctx.SetPayloadLength(len);
var writer = new PrepWriter(ctx.GetBufferWriter());
WriteSingle(value, ref writer);

// nested, inside WriteSingle: the prefix is the child's Measure, recomputed
writer.WriteTag(...);
writer.WriteVarint(ForwardPerItemRequest.Measure(item));
ForwardPerItemRequest.WriteSingle(in item, ref writer);
```

No size tree, no memoization, no patch-back, no buffering of sub-messages. Sizes are
derived on demand by pure functions — at the root for the region size, and again per
sub-message prefix during the write. Everything protobuf-net's current writer does to
DISCOVER lengths while writing (buffer-and-patch machinery) simply does not exist here.

**The recorded hazard**: recompute-per-prefix makes deep nesting quadratic-ish — a parent's
Measure recurses the whole subtree, then the write measures each subtree again for its
prefix, at every level. The prototype accepted this (typical messages are shallow and
Measure is nearly free); the real implementation should treat recompute-vs-memoize as
tiebreaker-rule territory — benchmark on a deep tree (the descriptor set is conveniently
deep) before choosing, and note protobuf-net has prior art for memoization if needed
(`IMeasuredProtoOutput` / measured-write states in the CURRENT surface — the concept
already exists at the API level; the prototype shows it need not exist per-message).

## Mapping onto today's architecture (the read arc's playbook, direction-flipped)

1. **Same-surface swap**: `ProtoWriter.State` keeps its API; the internals become the raw
   write core (the `ProtoWriter.State.Raw.cs` mirror), with the legacy veneers over it —
   exactly the reader swap. The v4-era `generate-shape.ps1` inventory said 72 methods + 9
   properties on the writer surface.
2. **Generator**: `Measure_X`/`RawWrite_X` private statics per contract, mirroring
   `RawRead_X`; `ISerializer<T>.Write` proxies. Per-member LEGACY-MODE fallback transfers
   directly — and more naturally than on reads, since classic write bodies are already
   per-member statements, not a dispatch loop: a hard member stays a stateful
   `WriteMap`/`WriteAny` call between raw-written siblings (the StashTag equivalent is
   trivial: the stateful write entry points take the field number as an argument already).
3. **What the write side does NOT need** (why it is simpler, Marc's observation in
   nano-core.md): no wire-type tolerance, no alternative-format decode variants, no
   forward-only pending slot, no run speculation, no IsKnownField — we control the output;
   one canonical form per member, chosen once.
4. **What is genuinely new**: the measure pass (above); the output buffer strategy
   (IBufferWriter single-region vs stream flush boundaries — the read arc's refill problem,
   mirrored); and packed-length precomputation (a packed repeated's prefix is a Measure
   over elements — pure arithmetic again, and the fixed-size cases are multiplications).
5. **Gates already exist**: the differential corpus compares BYTES both directions — the
   write side is held to byte-identity with `RuntimeTypeModel` by the same 3,021 contracts,
   the conformance suite's write comparisons, and the merge test's re-serialization. The
   goldens/AotRefGen loop covers emission review. Nothing new to build before starting.
6. **`ClassicEmit` gates both directions by prior agreement** (nano-core.md): the raw write
   emission keys off the same plan flag; and the raw write surface joins `PBN9002`.

## Step ladder (mirroring the read arc's cuts)

1. Raw write primitives on `ProtoWriter.State` (varint unrolled-by-measure, fixed, string,
   tag) + the measure statics — micro-benchmarked against the EncodeResults.md tables
   (2022 numbers are hypotheses; re-measure on net10).
2. Buffer model: presized-region fast path + the flush boundary (the writer's refill).
3. Generator write pass v1 on the descriptor feature set, differential-gated; NanoBench
   gains the serialize/measure legs (Google.Protobuf and legacy on home turf, as the read
   battery did).
4. Widen per the census machinery (it already tallies write-relevant member shapes);
   legacy-mode arms carry the rest from day one.

## Design refinements (Marc, 2026-08-14)

- **Constant tags cost nothing in either pass.** The generator knows every tag at compile
  time: Measure_ statics fold tag lengths into literal constants (no MeasureVarint32 call
  for tags), and the write side emits pre-encoded constant bytes - a single store for
  fields <= 15 (the dominant case; the write mirror of the read's range-trick), unrolled
  stores beyond. Consequence: the repeated-measure cost concentrates in STRING sizing
  (GetByteCount is the one measure that touches data) and sub-message recursion.
- **The deep-tree memoization race, three entrants, tiebreaker-ruled on the descriptor set:**
  1. recompute-always (prototype default; zero context, wins at typical depth);
  2. keyed memo on the writer (Marc's length-lookup sketch, morally v3's
     Measure/MeasureState machinery - inspect that in step 1): pooled
     Dictionary<object, long> by reference identity, post-order populated; costs a hash
     per node and needs an identity story for struct contracts;
  3. ordered length QUEUE: the root measure appends each sub-message length to a pooled
     buffer in traversal order, the write pass consumes via cursor - no keys, O(1),
     struct-friendly, valid because measure and write are emitted from the same plan and
     traverse identically BY CONSTRUCTION. Suggested default; the keyed memo is the
     fallback if any shape breaks traversal identity. All schemes share the existing
     assumption that the value (including ShouldSerialize answers) does not mutate
     mid-serialization.
- **Buffer storage reworks like the read side**: per-TFM accessor (ref byte root + offset
  on net7+, arr[index] down-level behind one inlined accessor), presized-region fast path,
  flush boundary as the refill problem mirrored.

## The `??=` formulation wins the design (Marc, 2026-08-14)

Sub-object lengths become `+ (lengthCache[obj] ??= Measure(obj))` plus constant tag+prefix
sizes. This subsumes the ordered queue and flips the recommendation, for two reasons:

1. **Order-independence.** The queue required measure and write to traverse identically,
   which breaks down for contracts mixing native and LEGACY-MODE members (legacy writes go
   through the stateful machinery, not the generated traversal). A keyed cache does not
   care; and shared references measure ONCE - a win no other entrant has.
2. **It composes with the mixed-contract measure answer**: a native parent needs its
   legacy-mode child's size, and the child's only size story is the classic
   discover-and-patch write. The unification is a COUNTING MODE on the raw writer (stores
   are no-ops, position advances), so any classic write body doubles as its own measure -
   morally v3's Measure/MeasureState design (the "length lookup used in some contexts"),
   reborn on the raw core. One population rule: native members via pure-arithmetic
   Measure_ statics, legacy-mode members via their classic body against the counting
   writer, both landing in lengthCache[obj].

Caveats, recorded: struct sub-messages have no identity (recompute; cheap leaves); the
cache is a pooled dictionary with reference-equality (RuntimeHelpers.GetHashCode, dodging
user overrides) on the writer, cleared per root; and it still RACES recompute-always on
the descriptor set per the tiebreaker rule - a hash per node is not free at shallow depth -
but enters as the favorite, being the only entrant that also answers mixed contracts.

## Aggressive-optimization checklist for the repeated-write cut (Marc, 2026-08-14)

- **CollectionsMarshal.AsSpan(list)** for enumerating `List<T>` on write (net8+,
  library-owned #if): per-element raw ops over a span beat the enumerator and the indexer
  both, for packed and unpacked runs alike.
- **Packed fixed-size elements are one block**: measure = `count * width` (pure
  arithmetic), write = prefix + MemoryMarshal.Cast copy (little-endian fast path with the
  folded BitConverter.IsLittleEndian guard) - the exact mirror of the read side's
  SetCount/Cast bulk arm.
- **Packed varint elements**: measure via a span walk summing MeasureVarint32 (lzcnt per
  element); the length prefix is then exact, and the write is a second span walk of
  unrolled varint stores. Same two-pass shape the read side's terminator-scan used, in
  reverse.
- Arrays: the span IS the array; strings-in-collections: GetByteCount per element measures,
  the write reuses nothing (no double GetByteCount - the lengthCache question applies to
  string elements too, worth including in the memoization race).

## The presized buffer core: the plan (2026-08-13, agreed shape with Marc)

Two levels. Level 1 is the op-level mirror of the read arc's step 2; level 2 is the
presized lease ("measure; if reasonable, lease the whole payload; if unreasonable, cap
and chunk" - Marc's formulation, and correct). Level 2 is a small policy layered on
level 1, so level 1 goes first.

### What exists today (audited, not assumed)

- **Writer State ALREADY holds the current chunk**: `_span`/`_memory`,
  `OffsetInCurrent`, `RemainingInCurrent`, and the `LocalWrite*` family. Both real
  backends serve `Impl*` by ensuring room (`GetBuffer`) and then writing locally. So
  "hoist a span into State" is DONE and shared; what is wrong is the per-op protocol
  around it.
- **Per op today**: one virtual `Impl*` call + room check + local write + a writer-object
  `AdvanceAndReset(n)` (`_position64 += n; WireType = None`). That is double bookkeeping -
  the span offset AND the writer position advance for every op - plus a virtual hop. The
  cut-1 raw veneers ride this protocol deliberately (surface-first).
- **Position has one source, `_position64`, maintained per-op** - and every reader is
  internal (`state.GetPosition()`, the dispose incomplete-state check, the
  length-mismatch checks, the measure machinery's `ResetWriteState`/`SetWriteState`
  snapshot). There is NO public writer position anywhere in Core or protobuf-net, so the
  invariant is ours to change without an [Obsolete] dance; if the audit turns up a
  compat member after all, the fallback is Marc's treatment - mark it approximate and
  point at the accurate accessor.

### The design

1. **One position invariant, changed globally per backend** (not two regimes with commit
   barriers - that is a divergence-bug factory). For the span-backed backends
   (buffer-writer, stream): `_position64` counts COMMITTED bytes only, advanced at chunk
   commit (`ConsiderWritten`/flush), never per-op. True position is DERIVED:
   `_position64 + state.OffsetInCurrent`, surfaced as `writer.GetPosition64(in state)`
   (Marc's shape) / `state.Position64`. The Null writer has no span (`OffsetInCurrent`
   is always 0) and keeps its per-op advance - the SAME formula answers correctly for
   it, so the accessor is uniform and branchless.
2. **Raw ops go span-direct**: when `state.IsActive` with room, a raw op is a direct
   local write - no virtual call, and NO writer-object touch at all:
   - `WireType`: raw ops stop writing it entirely. It was `None` on entry to a raw body
     (every stateful op resets after itself) and stays `None` because nothing touches it;
     legacy-mode statements inside a raw body do their own handshake and end at `None`.
     Cut 1 only reset it per-op because the veneers shared `AdvanceAndReset`.
   - `_needFlush`: set once at lease time, not per tag.
   - slow path (no room / no span, i.e. the Null writer or an exhausted chunk): the
     current virtual veneer body, out-of-line.
3. **The flush-to-writer surface, enumerated** - what a legacy/stateful statement inside
   a raw body needs to be correct, under the new invariant:
   - the DATA needs nothing: the legacy `Impl*` path writes through the SAME state-local
     span, so there is no buffer hand-off at all;
   - the POSITION needs nothing: derivation makes it always-accurate on both sides;
   - `WireType`/`fieldNumber`/`packedFieldNumber`: owned by the stateful ops themselves,
     untouched by raw ops - nothing to reconcile;
   - `_depth`: writer-side only, raw sub-message writes never touch it (the measure walk
     guards the recursion instead) - nothing to reconcile.
   So under invariant (1) there are NO per-statement commit points; the residual audit
   is the sites that mutate or read `_position64` directly:
   `Advance`/`AdvanceAndReset` callers per backend (each either moves to chunk-commit or
   is Null-writer-only), `ResetWriteState`/`SetWriteState` (the measure snapshot zeroes
   position for a null-run: fresh State, offset 0 - verify, this is the sharpest edge),
   `ImplCopyRawFromStream` and `State.ReadFrom` (stream copies: confirm they route via
   the span or commit explicitly), `ConsiderWritten`, `TryFlush`, the dispose check, and
   the two throw-message readers.
4. **Presized lease (level 2)**: at lease time, size the demand from what measure-first
   already knows. A measurable root primes it - the generator emits a capacity hint at
   the top of the write (total = root `Measure_`, already needed for nothing extra since
   the lengthCache holds the subtree lengths) - `clamp(total, backendDefault, cap)`.
   `IBufferWriter.GetSpan(sizeHint)` MUST honour the hint, which is why the cap exists:
   an unreasonable payload caps and chunks through level 1's boundary. Stream backend:
   one pooled `byte[]` of the clamped size instead of the default block. **Cap policy,
   decided (Marc, 2026-08-13): `max(TypeModel.BufferSize, backend preference)`** - i.e.
   the model's configured buffer size (default `BufferPool.BUFFER_LENGTH`) or what the
   backend would hand out anyway (`GetSpan(0).Length` for a buffer-writer), whichever is
   larger - so existing configuration keeps meaning something and a generous backend is
   not artificially truncated.
5. **API surface**: level 1 needs only the position accessor (`[PBN9002]`
   `state.Position64` / `GetPosition64(in state)`); internal readers move to it. Level 2
   adds one capacity-hint veneer for the generator. No public behaviour changes; no
   obsoletions required on current evidence.

### Step ladder (gates per step, as always)

1. ~~**Invariant flip alone**~~ - **landed, see cut 8 below.**
2. ~~**Span-direct raw ops**~~ - **landed, see cut 9 below.**
3. ~~**Presized lease + cap policy**~~ - **built, measured, and PARKED; see below.** It is
   neutral in both directions on every destination measured, and the only route that knows
   the total up front is the one path where that knowledge is already paid for many times
   over.
4. Re-validate net472 and native (clean publish, warning baseline 19), re-record both
   benchmark legs.

Fixture debt to pay alongside: a mixed contract that interleaves raw and legacy-mode
member WRITES around a chunk boundary (the write-side mirror of the read's
split-at-every-offset sweeps) - `StreamParseBenchmarks.ChunkedStream` has the recipe,
and a tiny-block `IBufferWriter` harness would force the boundary through every member
shape. **Half paid by cut 8**: `WriterChunkBoundaryTests` is that harness, sweeping
`TypeModel.BufferSize` against an exactly-as-asked `IBufferWriter`. What it does NOT cover
is the *generated* path - it drives the classic engine only - so the raw-and-legacy
interleave through a chunk boundary is still owed, and wants a buffer-writer leg in
`AotConformanceTests` rather than a fixture here.

## The presized lease: built, measured, parked (2026-08-13)

Step 3 of the buffer-core ladder, implemented as designed - `SetCapacityHint` feeding a
`ClampedLease()` between `MinimumBufferSize` and a 64KiB cap (chosen to keep every presized
lease under the LOH threshold and on an `ArrayPool` bucket), an `OnCapacityHint` hook so the
stream backend sizes its own buffer once, and `MeasureState` supplying the total for free.
Correct, green on the whole battery - and **reverted**, because paired measurement says it is
worth nothing. Recorded here rather than in the git history alone, so nobody builds it twice.

| net10.0, stream leg, paired | without step 3 | with step 3 |
| --- | ---: | ---: |
| NanoGenerated | 15.76 us | 15.58 us |
| NanoGeneratedMeasured (the only row where the hint fires) | 24.72 us | 24.42 us |

Both differences are inside the noise. The mechanism costs nothing and buys nothing.

**Why, and it is worth understanding rather than just recording:** the lease size was never the
bottleneck. A 7,670-byte payload at the default 1KiB block takes about eight `GetBuffer` calls,
each a flush plus a `GetMemory` - a few hundred nanoseconds against a 15us write. And on any
realistic destination (`ArrayBufferWriter`, a pipe, a pooled writer) the chunk handed back is
already far larger than the hint, so the boundary was rare before presizing and rare after. The
same fact retro-explains cut 9's size: the span-direct fast arm was *already* being taken
almost every time, so there was little boundary cost left for a bigger lease to remove.

### Asking the root for its length costs more than it looks

The first attempt primed the hint in `SerializeRoot`, by asking a measurable root for its
length. `Issue1232` failed immediately - it pins the exact `Measure` and `Write` call counts,
and `writeCallCount` went 1 -> 3. The test is right and the design was wrong:
`IMeasuringSerializer.Measure` is **user-visible API**. Consumers implement it, consumers count
it, and an implementation is entitled to measure **by writing** - which is precisely what that
fixture's does. `OptionTrySkipWritingWhenMeasuring` does not distinguish "cheap arithmetic"
from "expensive traversal", so there is no predicate that makes this safe in general.

That leaves a generator-emitted hint as the only honest route to the common path - the
generator does know its `Measure_` is arithmetic. Given the table above, that is not worth
building until something demonstrates the lease size matters at all.

### A "supports write-free measure" feature bit: considered, not built (Marc, 2026-08-13)

The natural fix for the trap above - `OptionTrySkipWritingWhenMeasuring` says "you may ask",
not "asking is cheap and free of side-effects" - is a second flag saying the latter. Bits are
available (`SerializerFeatures` uses up to `1 << 16`, with `1 << 30` reserved). It is not
built, and the reasons are worth keeping because two of them are non-obvious.

- **Its motivating use case evaporated.** It would have made root capacity-priming safe; the
  presized lease that priming served is measured as neutral. A flag that unlocks a feature
  worth nothing is worth nothing.
- **Its one surviving use is the `>= 0` question**, and there it is genuinely elegant: the
  objection to `>= 0` was the audit surface (it reverses the meaning of an answer shipped API
  already assigns, across three codegen paths plus hand-written implementations). An opt-in
  flag dissolves that entirely - existing implementations keep "non-positive: traverse", and
  only an opted-in serializer gets "zero means genuinely empty". Purely additive. But the
  payoff is the double-measure of an *empty* contract, which essentially never happens.
- **For the GENERATOR, a features bit is the wrong shape**, and this is the part to remember.
  `Features` is a property, obtained by instantiating the serializer - which is exactly why
  `IsScalar` needed its three-way resolution (attribute argument, then folding the `Features`
  expression when the serializer is in this compilation, then deferring to run time). A
  write-free claim would need the same, **minus the third arm**: you cannot defer "is it safe
  to ask", because asking is the thing with the side effect. So it would have to be an
  attribute *argument*, with a feature bit as the assertable runtime counterpart - not a
  feature bit alone.
- **The failure mode is worse than IsScalar's.** Every `Measure` call today goes to a null
  writer, so an implementation that lies about being cheap is merely slow. A flag licensing a
  speculative ask against the LIVE writer turns that into a corrupted stream. Anything built
  here wants the `Debug.Assert`-in-the-constructor treatment the generator already gives
  `IsScalar`.

Revisit if either changes: something demonstrates that lease sizing matters after all (a
destination that allocates per lease, rather than the generous ones measured here), or the
empty-contract double-measure turns up in a real profile.

### The lead this did turn up: the measured path copies its length cache

`NanoGeneratedMeasured` allocates **44,784 B against NanoGenerated's 22,392 B - exactly twice**,
on both backends. `NetObjectCache.InitializeFrom` copies `_rawLengths` pair by pair into the
target's dictionary rather than handing the instance over, so a tree with hundreds of
sub-messages builds the whole cache twice and pays hundreds of hash inserts to do it. Against
`NanoGenerated`'s 15.6us plus the 3.6us measure, roughly 5us of the measured path's 24.4us is
unaccounted for, and this is the obvious candidate.

A **swap** rather than a copy is O(1) and allocation-free, and keeps single ownership (each
cache still holds exactly one dictionary, so disposal semantics do not change) - unlike sharing
the instance, which would alias two writers whose lifetimes only happen to nest today.
**Done, and it pays:**

| `NanoGeneratedMeasured`, net10.0, paired | copy | swap |
| --- | ---: | ---: |
| mean | 25.29 us | **22.44 us** (-11.2%) |
| allocated | 44,784 B | **22,552 B** (-50%) |

Exactly the duplicate cache, gone - the measured path now allocates what the direct path does.
Nothing else moves. The one behaviour worth stating: serializing the same `MeasureState` twice
still works, because the source is left holding the (empty) dictionaries it was handed, so a
second pass simply finds nothing cached and re-derives - which is what an unmeasured write does
anyway. These are pure caches keyed by object identity, and a length is a length whoever
computed it.

Note both fields had to lose `readonly` (it was conditional on `#if NET`), and the exchange
uses plain temporaries rather than tuple deconstruction, since net462 has no `ValueTuple`.

## The lease hint is a HINT (Marc, 2026-08-13) - and this was already broken

`IBufferWriter<T>.GetMemory`/`GetSpan` document their argument as "at least this much, or
throw", but that is a contract we neither control nor can verify. A simplistic destination can
hand back a fixed small block however much is asked for, and in the limit one byte at a time.
**Optimise for a friendly lease; survive an unfriendly one.**

Probed rather than reasoned about, with a destination that ignores the hint entirely:

| the destination grants | before | why |
| --- | --- | --- |
| >= 10 bytes | **worked already** | a chunk at least as wide as the widest single op is usable; the room checks just re-lease more often |
| < 10 bytes | **`IndexOutOfRangeException`** | `GetBuffer` called `state.Init(buffer)` without ever looking at what came back, and `LocalWriteVarint64` then wrote past it |

So "large but not large enough" needed nothing, and only the pathological case was broken -
a useful narrowing, because it means the fix does not touch the normal path at all.

**The fix is Marc's: stop using their memory.** When a chunk comes back narrower than
`UsableLease` (16), the writer leases its own pooled region and hands the bytes over at flush
via `BuffersExtensions.Write`, which loops `GetSpan`/`Advance` internally and copes with any
size the destination offers - so the fragmentation becomes its problem, which is where it
belongs. The choice **latches**: a destination that gave an unusable chunk once will do it
again, and re-probing would burn a `GetMemory` call per chunk to learn nothing. The latch is
cleared in `Cleanup`, since the writer is pooled and a friendly destination must not inherit
the penalty.

### Call `BuffersExtensions.Write` by type name, never as an extension method

This is not style, and the reason is a live bug in a popular library.
`CommunityToolkit.HighPerformance` declares an *identically shaped*
`Write<T>(this IBufferWriter<T>, ReadOnlySpan<T>)` whose body is the naive
`GetSpan(len)` / `CopyTo` / `Advance(len)` - i.e. it assumes the hint is honoured. It is meant
to be a polyfill, gated `#if !NETSTANDARD2_1_OR_GREATER`, but that symbol is defined **only for
netstandard2.1+ TFMs**, not for `net8.0` (which gets `NET`, `NET8_0_OR_GREATER`, `NETCOREAPP*`),
so the polyfill ships on every target *except* the one it was excluded from. Verified with a
two-TFM probe project, not from memory.

Worse than dead weight: with both namespaces imported on net8.0 the call is **not ambiguous** -
it compiles, the Toolkit's version wins silently, and against a one-byte-granting destination it
throws where the BCL's would have looped and succeeded. (The BCL implementation cannot throw
there, so whatever threw was not it.)

Hence the static, type-qualified call in `TryFlush`. It pins the multi-segment implementation
and makes the other one unbindable.

## The minimum lease: `TypeModel.MinimumBufferSize` = 128 (Marc, 2026-08-13)

Two floors in one number, and it is worth keeping both reasons because they justify very
different values:

- **correctness** wants 10: the buffer-writer checks for room once per op and then writes up
  to a 10-byte varint, so a narrower lease overruns it. An `IBufferWriter` promises only the
  hint, and a strict one gives exactly that (which is how the overrun above was found);
- **policy** wants much more: every lease is a `GetMemory`/`Advance` pair, which on a real
  pipe may rent or allocate, so a 16-byte chunk is pathological however correct.

**128, decided by Marc**, and enforced in the `BufferSize` **setter** rather than in the
backend - the property already normalises non-positive values to the default, so this is the
established pattern, it is *observable* (you can read back what you actually got), and step
3's cap policy inherits it instead of needing its own floor. The backend keeps a clamp as
belt-and-braces for the model-less path.

**"Do we fight our own measure?"** (Marc's check, and the right one to make): no. The floor
bounds the *lease*, never the message - a 2-byte message still measures 2, writes 2 and
advances 2; the extra leased space is simply not used, and a real buffer-writer satisfies a
128-byte hint out of a block it already holds. The formulation step 3 should implement is
therefore `clamp(measured, MinimumBufferSize, cap)`: the measure decides, with a floor below
and the decided cap above.

Consequence for the fixtures: sweeping `BufferSize` below 128 no longer varies anything, so
both chunk-boundary sweeps now start there. That costs nothing where it matters - a bigger
lease exercises the FAST arm more, and that is the arm nothing else in the battery reaches;
the slow arm is covered everywhere already, since the stream backend has no span and takes
it always.

### Zero-length measures: we already skip, and it is safe for a non-obvious reason

Also Marc's question. The classic buffer-writer engine skips the body entirely when the
calculated length is zero - four sites, `if (calculatedLength != 0) // don't bother
serializing if nothing there` - and that predates this arc. The generated raw path does not
skip; it calls `RawWrite_` unconditionally, which writes nothing. Both are correct.

The worry worth having is that the **calculated-vs-actual length check lives inside that
`if`**, so a measure that wrongly reported zero would skip the body *and* the check that
would have caught it - the one measure bug the runtime net cannot see. It cannot happen, and
the reason is a convention that was chosen for something else entirely:
`ProtoWriter.Measure` accepts an `IMeasuringSerializer` answer only when it is **`> 0`**. A
zero from arithmetic is *rejected* and re-measured by traversal. So the same rule that means
"non-positive: measure by traversal" also guarantees a skipped body was measured **by
writing it**, which is exact by construction.

So no extra check is wanted, and one would cost the common path for a case that cannot arise.

There is a small *cost* hiding in it rather than a risk, recorded and deliberately NOT fixed:
a genuinely empty measurable contract measures twice - arithmetic says 0, `> 0` rejects it,
and a full null-writer traversal confirms 0. The obvious tightening is `>= 0` (Marc's
instinct, and mine), with the generated implementation's existing **-1** spill - already there
for the int-overflow case - carrying "cannot answer". It is declined, and the reason is the
size of the audit rather than the size of the change:

- `>= 0` reverses the meaning of an answer that **shipped API** already assigns, so every
  existing `IMeasuringSerializer` implementation returning 0 to mean "don't know" would
  silently start writing nothing. Consumers can and do hand-write these;
- and the producers are not one place. Measure-first has to be right in **all three codegen
  paths** - runtime-no-emit, runtime ref-emit, and the AOT generator - and only the third
  emits `IMeasuringSerializer` today, so the other two would have to be got right at the
  moment they start, forever, for a case that essentially never occurs.

A one-token change with a three-generator-plus-ecosystem audit behind it, buying nothing on
any real payload. The rule this is an instance of: **a convention is cheap to choose and
expensive to reverse once anything outside the repo can implement it.**

## Cut 9 landed: span-direct raw ops, buffer-core step 2 (2026-08-13)

Every raw write op now has two arms: where the backend holds a leased chunk with room, the
store goes **straight into State's span** - no virtual `Impl*` hop, and no writer-object touch
of any kind; otherwise control falls out-of-line to the previous veneer body. Cut 8 is what
made the second half possible: with position derived from the offset the store already
maintains, there is nothing left on the writer object for a raw op to update.

Three things had to go with it, each a writer-object store that would have defeated the point:

- **the wire-type reset, entirely.** It is `None` on entry to any serializer body (every
  framing path resets before handing over) and every stateful op resets after itself, so a raw
  body starts at `None` and stays there. Cut 1 reset per-op only because the veneers shared
  `AdvanceAndReset`. The raw/legacy interleave within one body is unaffected - that was always
  what the invariant was for;
- **`_needFlush`, moved to LEASE time.** A chunk being out *is* the condition it records, and
  `GetBuffer` is the one place a chunk is taken. The slow tag arm still sets it, for backends
  that never lease;
- the position advance, already gone in cut 8.

`WriteRawTag` additionally gets a **single-byte arm** for fields 1-15. The argument is a
compile-time constant at every generated call site, so `tag < 0x80` folds away and the
dominant case becomes one store - the write mirror of the read side's range trick.

**The stream and Null backends have no span at all, so they always take the slow arm.** That is
correct but has a consequence worth stating plainly: the benchmark headline rows write to a
`MemoryStream`, so **this cut is invisible there**. It had to be measured on the buffer-writer
leg added just before it.

Paired, buffer-writer backend:

| row | before | after | delta |
| --- | ---: | ---: | ---: |
| LegacyReal | 42.30 us | 41.03 us | -3.0% |
| GeneratedProtogen | 20.37 us | 18.76 us | **-7.9%** |
| NanoGenerated | 12.41 us | 11.32 us | **-8.8%** |
| GoogleProtobuf (the drift gauge) | 12.47 us | 12.58 us | +0.9% |

Normalised against the gauge that is ~-9.6%, and the generated model is now **10% ahead of
Google.Protobuf** on this destination (11.32 vs 12.58).

### The coverage hole this exposed, which matters more than the 9%

The fast arm is only reachable on a backend that has a span - i.e. the buffer-writer - and
**nothing in the battery drove that backend.** Every gate serializes to a stream or a byte[].
Probed rather than assumed, by deliberately corrupting each arm in turn:

| probe | ChunkBoundaryTests | DifferentialTests | AotDifferential (3023 contracts) |
| --- | --- | --- | --- |
| corrupt the **slow** string arm | 89 fail | 90 fail | (caught) |
| corrupt the **fast** tag arm | **132 fail** | **652 pass** | **3023 match, exit 0** |

So the corpus differential - the CI gate, and the sharpest measurement in this arc - would
have shipped a completely broken span-direct write path without a murmur. The slow arm looked
well covered only because the stream backend has no span and therefore takes it always.

`AotConformanceTests/ChunkBoundaryTests` closes it: every case in the generated corpus is
re-serialized through an exactly-as-asked `IBufferWriter` at twelve lease sizes, so the chunk
boundary walks through every emitted op in turn and the fast/slow transition happens mid-message.
Note the lease sizes start at **16**: the backend clamps its demand to `MinimumLease`, so
smaller values would silently all be the same test.

The general lesson, which is the same one AGENTS.md records for native AOT: **a path that no
gate drives is not "fine", it is unmeasured** - and "the differential is at 100%" means only
that the destinations it uses are at 100%.

## Cut 8 landed: the deferred position, buffer-core step 1 (2026-08-13)

`_position64` now counts **committed** bytes only; the true position is DERIVED as committed
plus whatever the backend still holds uncommitted, through one
`private protected virtual long GetUncommitted(in State)`:

| backend | uncommitted | commits at |
| --- | --- | --- |
| buffer-writer | `state.OffsetInCurrent` | `TryFlush` (`ConsiderWritten` then `Advance`) |
| stream | `ioIndex` | flush, and the two write-straight-to-`dest` arms |
| null | always 0 | its own `Impl*` stores, which ARE the measurement |

So `AdvanceAndReset(count)` loses its count and becomes `ResetWireType()`: the ~40 shared write
sites stop maintaining a position alongside the buffer offset they were already advancing,
which is the second half of the double bookkeeping step 2 needs gone before a raw op can be a
span-direct store touching the writer object not at all. Surfaced as `[PBN9002]
state.Position64` / `writer.GetPosition(in state)`, and every internal reader now goes through
it - `state.GetPosition()` is a one-line forwarder, so there is a single source.

Two audit results worth keeping, both the opposite of what the plan expected:

- **`ResetWriteState`/`SetWriteState`, called out above as the sharpest edge, is safe by
  construction.** All five call sites are `Measure*` statics taking a `NullProtoWriter`, which
  has no pending buffer - so zeroing the committed count *is* zeroing the position. Noted at
  the method, along with what a buffered backend would have to do differently, since the
  next person will ask the same question.
- **The stream backend is not span-backed at all.** The plan said "the span-backed backends
  (buffer-writer, stream)"; in fact `StreamProtoWriter` never calls `State.Init` and keeps its
  own `ioBuffer`/`ioIndex`, so `State`'s span belongs to the buffer-writer alone. The same
  invariant still fits it exactly - `ioIndex` is its uncommitted count - which is why the
  accessor is one virtual and not a special case.

**The Null writer's `Impl*` stores now advance for themselves.** They used to be empty (or a
bare `MeasureUInt32`) with the caller advancing; with the caller out of that business they own
it. The two sites that consumed a preamble length as a *return value* - `AdvanceSubMessage`
and `ImplEndLengthPrefixedSubItem` - would otherwise double-count, so their `Impl`-calling arms
now contribute zero and only the arms that write nothing contribute a width.

New fixture, `WriterChunkBoundaryTests`, paying part of the debt below: an `IBufferWriter` that
hands out **exactly** what was asked for and never a byte more, swept across `TypeModel.BufferSize`,
so a commit lands inside every member shape rather than wherever a generous pool put it. It
pins bytes against the stream backend, the reported root length, the measured-write path (which
runs both position regimes in one operation), and `Position64` read mid-write with bytes still
uncommitted. `BufferWriterTests.ManualWriter_*` already refereed the same invariant for free -
it asserts position after every op on all three backends, and would have failed flat if the
derivation were wrong.

**It found a pre-existing bug, and the sweep floor records it rather than hiding it:** a
`BufferSize` below 10 overruns the lease, because the buffer-writer's room checks
(`if (RemainingInCurrent < 10) GetBuffer`) assume a lease at least as wide as the widest
primitive written without re-checking, while `GetBuffer` asks for exactly `BufferSize`. With a
strict `IBufferWriter` that is an `IndexOutOfRangeException` on a public config knob. Confirmed
pre-existing by re-running the fixture against the writer as it stood before this cut - the
same 18 cases fail identically - so it is not this cut's. **Fixed in the following commits**
(see "The minimum lease" below). It went unnoticed because a real `IBufferWriter` hands out
far more than the hint - but the hint is all the interface promises. The stream backend has
no equivalent problem: `DemandSpace` resizes `ioBuffer` on demand rather than living within
a lease.

**Benchmark: measured back-to-back on one machine, which turned out to matter more than
expected.** net10.0, descriptor serialize:

| row | before | after | delta |
| --- | ---: | ---: | ---: |
| LegacyReal | 19.85 us | 20.87 us | +5.1% (inside its own between-run spread) |
| GeneratedProtogen | 15.42 us | 15.14 us | **-1.8%** |
| NanoGenerated | 13.77 us | 13.54 us | **-1.7%** |
| NanoGeneratedMeasure | 3.65 us | 3.66 us | +0.3% |
| GoogleProtobuf (the drift gauge) | 13.19 us | 13.27 us | +0.6% |

A small consistent gain, which is what this step should produce: one add-and-store leaves the
hot path, but every op still routes through the virtual `Impl*`, which dominates. Step 2 is
where the win is.

**And a caution worth more than the numbers: between-DAY drift dwarfs between-run drift.** This
run's *baseline* - byte-identical code to cut 5 - reads 13.77 us where cut 5 recorded 12.64 us,
so the machine is ~9% slower today than on 2026-08-12. Read cold against the recorded table,
this cut looks like a 7% regression; measured paired, it is a 1.7% gain. The 1.6-1.8% figure
recorded for cut 5's race was two runs minutes apart and does NOT bound this. **Never compare
against a table from another day** - re-measure the baseline in the same session, and prefer
`git stash` + two runs to any amount of arithmetic.

## Cut 7 landed: the measure state, made right (2026-08-13)

Two Marc questions, two fixes, one cut:

- **"Does the state traverse, so we capture everything the first time?"** It didn't - the
  RawLengths cache was homed on the writer, but the codebase's established home for
  cross-writer measurement state is **NetObjectCache**: the buffer-writer's null-writer
  sidecar shares the parent's instance by construction ("share the *same* known objects
  key"), and MeasureState's Serialize hands the measuring writer's cache to the writing
  writer via netCache.InitializeFrom. RawLengths moved there, so both traversals now come
  free: a length measured during the classic engine's prefix measure serves the real
  write all the way down, and the global Measure(value)->Serialize(output) flow writes
  entirely from cache hits. Clearing rides the same lifecycle as _knownLengths - the
  staleness guarantees are now identical to what has shipped for years, replacing the
  parallel Init/Cleanup clears.
- **"Should the reply be 64-bit?"** The interface reply stays int: the engine's contract
  is "non-positive -> measure by traversal" and the traversal is long-capable end to end,
  so the generated implementation spills a body wider than int.MaxValue to it
  (`len <= int.MaxValue ? (int)len : -1`) - correct at every size with zero new surface.
  But the question exposed that OUR arithmetic was int, which a colossal single body
  (many large byte[] members) would overflow SILENTLY where classic is correct: Measure_
  now accumulates long, count-folds multiply in 64-bit, prefix sites use
  WriteRawVarint64/MeasureRawVarint64 (byte-identical for small values), and RawLengths
  is Dictionary<object, long>, matching _knownLengths - all changed in place, being
  [PBN9002]-unshipped. **A 64-bit interface member is parked, with the mechanism
  recorded**: if ever wanted, a `Measure64` DIM under `#if` (absent on net462/ns2.0,
  defaulted-to-Measure where DIM exists) - Core already ships per-TFM interface members
  (CreateReadOnySet is net6+), so the shape has precedent - but the int+spill design
  makes it unnecessary today.

## Cut 6 landed: IMeasuringSerializer, the classic engine's measure hook (2026-08-13)

Marc's spot: `IMeasuringSerializer<T>` + `OptionTrySkipWritingWhenMeasuring` is SHIPPED
API whose single consumer is `ProtoWriter.Measure<T>` - the hook the classic buffer-writer
engine already calls for every length prefix (its WriteMessage has been measure-first all
along; it just measured by null-writer traversal). Measurable contracts now declare the
interface, carry the flag in Features, and answer with Measure_ - recovering the depth
budget and the ??= cache from the ISerializationContext via `TryMeasureRaw` (a non-writer
context answers -1, "measure by writing", exactly as before). InheritFrom copies only
masked category/wire-type bits, so the flag cannot leak into member features.

This is the mixed-contract bridge from the other direction: the arithmetic now reaches
every STATEFUL write of a measurable contract - measurable members inside unmeasurable
parents, elements of non-native collections (immutable families, sets, queues), and map
entries with message values - without any of those shapes being native. Free correctness
gate: WriteWithLengthPrefix throws on calculated-vs-actual mismatch, so the differential's
100% doubles as proof the arithmetic agrees with the engine everywhere it is now consulted.
The interface's OTHER consumer is the packed repeated write, which will matter when packed
lands. The counting-mode idea (legacy bodies against the Null writer) remains for the
members INSIDE an unmeasurable contract; this cut covers such a contract's measurable
children.

## Native validation (2026-08-13, cuts 1-9 + the buffer core so far)

A clean `dotnet publish src/AotSmoke -r win-x64` (obj/bin removed first) with the deferred
position, the span-direct raw ops and the minimum lease all in place: **19 IL warnings,
exactly the recorded baseline**, and the native executable PASSES - the same 559 bytes,
round-tripped and verified. Deep surgery on the shared writer internals, and no native
regression at all.

(Trap hit and worth repeating since it is already in AGENTS.md: running `publish` twice in
one command to get a warning breakdown yields nothing the second time - the publish is
incremental and reports no warnings on a no-op run. Take the count from the first run, or
clear `obj`/`bin` between them.)

## Native validation (2026-08-12, cuts 1-5)

A clean `dotnet publish src/AotSmoke -r win-x64` (obj/bin removed first - a second run
reports nothing) with the whole writer arc in place: **19 IL warnings, exactly the
recorded baseline**, and the native executable PASSES - 559 bytes serialized through the
generated measure-first writer including the descriptor-set member, round-tripped and
verified. The writer arc adds no native regression. (The vswhere-on-PATH trap struck
again, exactly as AGENTS.md describes: the link step fails naming link.exe.)

## Where this stands / what's next (current as of 2026-08-13, cuts 1-7 pushed)

**Handover note: this section plus "The presized buffer core: the plan" above is the
entry point for a fresh session.** Cuts 1-9 are pushed to `raw-writer` and green on every
gate; the serialize numbers are in `src/NanoBench/DescriptorSerializeResults.md`, which now
carries BOTH backends - and the buffer-writer one is the interesting half (the generated
model is ~10% ahead of Google.Protobuf there, against a legacy baseline that is twice as
slow as its own stream figure). The remaining ladder, in priority order:

1. **The stream backend is not span-backed**, so cut 9's fast arm never fires there - and that
   backend is what the *headline* benchmark rows use. Moving `ioBuffer`/`ioIndex` onto
   `State`'s span would let both backends share one fast arm; cut 9 was worth ~9% on the
   backend that has a span, and the stream backend got none of it. **This is now the top of
   the ladder**, having overtaken the presized lease on evidence (below). Real surgery - the
   length back-fill in `ImplEndLengthPrefixedSubItem` is the delicate part.
2. **The lengthCache allocates ~22 KB per serialize** on the descriptor tree (identical on
   both backends, so it is not the writer; Google allocates zero, and its whole payload is
   7.6 KB). It won its race on time and was never priced on bytes. The measure hand-off's
   duplicate of it is gone; the remaining copy is the cache itself. Its own cut.
3. ~~**The presized buffer core**~~ - **steps 1 and 2 landed as cuts 8 and 9; step 3 was built,
   measured and parked** (see its own section). What remains of that ladder is step 4:
   re-validating net472 and native, which has been done at each milestone since.
4. Counting mode for mixed contracts (legacy-mode members measured via the classic body
   against the Null writer, landing in the same lengthCache - which now lives on
   NetObjectCache, shared with the sidecar and the MeasureState hand-off, so the landing
   spot is already right).
5. Packed repeated writes (IsPacked support arrived on the read side; the write needs the
   zero-length-header model option and per-element measure - the MemoryMarshal block
   trick for fixed widths is recorded in the checklist above; `IMeasuringSerializer` is
   already implemented by measurable contracts, which is what the packed engine keys on).
6. Maps measure-first (entry = one KV sub-message; both sides already have measure forms
   for the native kinds).

### Working practices this arc runs on (learned the hard way; do not relearn)

- **The gate battery, per cut**: goldens x2 (first run rewrites, second asserts; review
  `git diff`), AotConformanceTests, AotDifferential (see trap below), SchemaTests both
  TFMs, protobuf-net.Test + Examples both TFMs, AotSmoke Release build (trim analysis) +
  DownLevelSmoke run. Native publish + warning count (baseline 19, vswhere on PATH per
  AGENTS.md) at milestones. Commit + push per green cut.
- **The differential staleness trap, which has struck three times**: the differential
  scans the *built* Debug binaries of protobuf-net.Test, Examples and
  protobuf-net.Reflection.Test - rebuild all three after ANY Core or BuildTools change,
  or you are comparing against a stale engine. Two sub-forms: (a) the rawWriter/rawReader
  probes gate on member EXISTENCE, so every new emitted-call target must be added as a
  probe sentinel (the "newest member" rule, recorded in Parse.cs) or a newer BuildTools
  against an older Core emits calls that do not compile; (b) a member *signature* change
  passes the name probe and still fails to compile - only the rebuild fixes that one.
  And check the differential's EXIT CODE explicitly; in a `;`-chained script a later
  command masks it.
- **The one-build-behind hand-nudge**: BuildTools compiles in protobuf-net.Reflection's
  committed `Generated/` file, so an emission-shape change that alters generated
  *signatures* breaks the BuildTools build until the committed file is hand-patched to
  the new shape; the next Reflection build then regenerates it properly. Documented in
  the Reflection csproj; it has been needed twice in this arc (the static
  ThrowNullRepeatedContents, the long Measure_).
- **Raw surface changes are cheap right now**: everything `[PBN9002]` is unshipped, so
  shapes change in place (as int->long did) rather than accreting overloads. That window
  closes at release.
- Measure/write agreement is refereed twice over: the differential's cross-deserialization,
  and the classic engine's own calculated-vs-actual length throw (which cut 6 put on
  every stateful path that consults the generated arithmetic).

## Cut 5 landed: the ??= lengthCache, and it wins the race (2026-08-12)

The design as sketched: a per-writer `Dictionary<object, int>` keyed by REFERENCE identity
(RuntimeHelpers.GetHashCode via a custom comparer - the BCL's is net5+ only), surfaced as
`state.RawLengths`, cleared in the writer's Init AND Cleanup (a stale entry is a corrupt
stream, not an error). Measure_ gained the cache as a third parameter, populating it
post-order for every reference-typed sub-message it walks; write sites then hit it -
usually populated by an enclosing measure - with the miss arm serving root writes. Struct
contracts have no identity and bypass it.

Race result (DescriptorSerializeResults.md): -14% on the plain-DTO row - which puts the
generated model AHEAD of Google.Protobuf on home turf (12.64 vs 13.32 us) - a wash on the
extensible protogen row, no regressions. Kept, per the tiebreaker rule; it is also the
only entrant that answers shared references and the future mixed-contract counting mode.
Remaining lever: the presized buffer core (every raw op is still a virtual Impl* call).

## First serialize numbers (2026-08-12, cuts 1-4 in place)

`src/NanoBench/DescriptorSerializeResults.md`: on the self-describing descriptor payload,
the shipped generated model serializes the SAME object graph 34% faster than the legacy
engine (13.56 us vs 20.64 us), within ~4% of Google.Protobuf on its home turf - with the
surface-first veneers (every raw op still a virtual Impl* call), no presized buffer core,
and recompute-always measure. One full-tree Measure_ traversal is 2.23 us; recompute pays
it once per nesting level during the write, which bounds the lengthCache's recoverable
share at several microseconds on this tree (~20% of the write row) and gives the
memoization race its stakes. The setup gates double as correctness evidence: the generated
protogen row is byte-identical to legacy, the bench-DTO row happens to be byte-identical
too, and Measure_ agrees with the written length exactly.

## Cut 4 landed: extensible contracts measure (2026-08-12)

The extension blob carries its own field headers, so its size is its LENGTH -
`MeasureRawExtensionData` (untyped and typed-bag overloads) reads it off the extension
object's query stream (`Length - Position`; must be seekable, which every buffer-backed
extension is - a custom forward-only IExtension throws with ClassicEmit named as the
escape hatch). That one veneer flipped the entire descriptor tree measurable: 29 Measure_
statics in the protogen model where cut 3 produced zero, with SchemaTests (556 x both
TFMs, real schemas, real extension bags) as the parity gate. Note the recompute cost now
includes double BeginQuery/EndQuery per extensible node (measure + write) - one more
entrant for the memoization race, which the descriptor corpus can now actually exercise.

## Cut 3 landed: measure-first sub-messages, recompute-always (2026-08-12)

Generated `Measure_X(value, depth)` statics size a contract by pure arithmetic - constant
tag lengths folded to literals, `MeasureRawVarint32/64` for scalars, `MeasureRawString`
(prefix + GetByteCount) for strings - and a native message member becomes exact-prefix +
direct call: `WriteRawTag; WriteRawVarint32((uint)Measure_T(tmp, state.RawDepthBudget));
RawWrite_T(ref state, tmp)`. The interface Write proxies to the RawWrite_ static exactly
as Read proxies to RawRead_. Repeated message elements take the same shape per element.

Decisions worth keeping:
- **Eligibility is a fixed point, unlike the read side**: there is no legacy-mode arm
  inside arithmetic, so one unmeasurable member takes the contract's Measure_ out, and a
  dropped target drops its referrers' measures (their raw scalar statements stay). Blocked
  at shape level: surrogates, hierarchies, extensibles, group-framed contracts, and
  before-serialize callbacks (measure runs before the pipeline would fire one, so a
  mutating callback would falsify the prefix). Blocked at member level: maps, wrapping,
  non-default formats, BCL kinds, parseable (ToString would also run twice), bytes-struct
  storage, inbuilt/hand-written serializer targets, nullable-struct messages (parked, not
  hard).
- **Only the measure recursion carries the depth budget** (`RawDepthBudget`, honouring
  TypeModel.MaxDepth): the write recursion that follows traverses the graph the measure
  just proved finite, so a cycle throws in measure before a byte is written.
- **Recompute-always, deliberately**: a parent's Measure_ walks the child, then the write
  re-measures it for the prefix - O(depth^2) on deep trees, the hazard the `??=`
  lengthCache exists to fix. Correctness shipped first; the memoization race benchmarks
  against this baseline.
- **The descriptor tree gets ZERO measure-first benefit today**: every descriptor DTO is
  IExtensible, so the whole tree is shape-blocked. Since that is both the benchmark corpus
  and protogen's hot path, extensible measure (the extension blob has a knowable length -
  it needs a public veneer, not new arithmetic) is the highest-value next increment, ahead
  of the memoization race which would otherwise measure a corpus the feature cannot touch.
- `ThrowNullRepeatedContents` became STATIC (unshipped API, changed in place) so Measure_
  bodies - pure arithmetic, no state - can raise the identical null-element failure at the
  identical point: the measure runs first, so it owns that throw now.

## Cut 2 landed: unpacked repeated runs (2026-08-12)

The unpacked default needs NO measure infrastructure - per-element tag+value, nothing for
empty, so it shipped ahead of the measure cut. `RawRepeatedWritable` gates it (unlike the
scalar raw writes, which are unconditionally safe, this replaces the ENGINE): exact
`List<T>`/`T[]` only (a derived-declared list's foreach could bind to a hiding
GetEnumerator), unpacked, unwrapped, default format, scalar/enum/string elements.
`CollectionsMarshal.AsSpan` is probed per compilation (net5+; a `ListAsSpan` plan flag,
not a TFM guess) - the golden harness shows the span form, the net462/netstandard2.0
consumers the plain foreach. Null elements throw through a new
`State.ThrowNullRepeatedContents<T>` veneer, same exception and message as the stateful
engine. Still classic, deliberately: packed (framing + the zero-length-header model
option, wants measure), message elements (want lengthCache), bytes/BCL/Uri/parseable
elements, maps (every entry is a length-prefixed sub-message - the measure cut's first
customer).
