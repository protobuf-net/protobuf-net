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
