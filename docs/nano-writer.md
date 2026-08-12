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
