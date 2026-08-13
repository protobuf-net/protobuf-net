# Descriptor serialize composite (docs/nano-writer.md)

## net472 leg (2026-08-12, cut 5 in place)

The down-level forms - plain foreach (no CollectionsMarshal.AsSpan), loop-counted varint
measures (no lzcnt) - with every setup gate passing:

| row | mean | vs LegacyReal |
| --- | ---: | ---: |
| LegacyReal | 36.40 us | baseline |
| GeneratedProtogen | 26.69 us | -27% |
| NanoGenerated | 24.58 us | **-32%, and 13% ahead of Google** |
| NanoGeneratedMeasure | 7.94 us | - |
| GoogleProtobuf | 28.26 us | -22% |


## The buffer-writer backend, measured at last (net10.0, 2026-08-13)

`DescriptorSerializeBufferWriterBenchmarks`: the same five-way composite against
`IBufferWriter<byte>` instead of a `MemoryStream`. Everything above this line is the STREAM
backend; nothing had ever measured the other one, which is the backend the buffer core is
actually about. Means are not comparable across the two classes - only ratios within each.

| row | mean | vs LegacyReal | (same row, stream) |
| --- | ---: | ---: | ---: |
| LegacyReal | 40.37 us | baseline | ~20 us |
| GeneratedProtogen | 19.87 us | **-51%** | ~15 us |
| NanoGenerated | 12.44 us | **-69%** | ~13.5 us |
| GoogleProtobuf | 12.73 us | -68% | ~13.2 us |

Two readings, both of which change how the arc should be judged:

- **The legacy engine is TWICE as slow against a buffer-writer as against a stream** (40.4 vs
  ~20 us) - and it is the same contracts and the same objects. The stream writer back-fills a
  length prefix into its own byte[]; the buffer-writer cannot reach back into a chunk it may
  already have handed over, so every prefix costs a full null-writer traversal of the subtree.
  That is precisely the cost measure-first exists to delete.
- **So the generated model's lead is 3.2x here against 1.5x on the stream**, and it passes
  Google.Protobuf (12.44 vs 12.73) on the destination modern code actually uses. The headline
  numbers recorded above understate the writer arc, because they were all taken on the backend
  that already had a workaround.

**Allocation is the open question this leg exposes**: 22392 B per serialize on both generated
rows, and identical on the stream backend - so it is not the writer. Google allocates zero
here. The size and its backend-independence both point at the `??=` lengthCache
(`Dictionary<object, long>`, one entry per sub-message, hundreds of them on this tree). Worth
a cut of its own; the cache won the race on time and was never priced on bytes.

## Buffer-core step 1: the deferred position (net10.0, 2026-08-13, cut 8)

Measured PAIRED - `git stash`, run, pop, run - because the recorded tables above turned out
not to be comparable across days (see below).

| row | before | after | delta |
| --- | ---: | ---: | ---: |
| LegacyReal | 19.85 us | 20.87 us | +5.1% |
| GeneratedProtogen | 15.42 us | 15.14 us | **-1.8%** |
| NanoGenerated | 13.77 us | 13.54 us | **-1.7%** |
| NanoGeneratedMeasure | 3.65 us | 3.66 us | +0.3% |
| GoogleProtobuf | 13.19 us | 13.27 us | +0.6% |

A small consistent gain on the two generated rows, which is what removing one add-and-store
per op should buy while every op still routes through the virtual `Impl*`. LegacyReal's +5.1%
is inside its own between-run spread (it has read 19.85, 20.15, 20.64, 20.87, 20.97 for
unchanged code across this arc); Google is the untouched drift gauge at +0.6%.

**Between-DAY drift dwarfs between-run drift, and nearly caused a wrong conclusion.** The
"before" row here is byte-identical to the cut-5 code that measured 12.64 us on 2026-08-12,
and reads 13.77 us today - the machine is ~9% slower. Compared cold against the table below,
this cut looks like a 7% regression; measured paired, it is a 1.7% gain. Re-measure the
baseline in the same session; do not diff against a recorded table from another day.

## The memoization race: lengthCache wins (net10.0, 2026-08-12, cut 5)

Same rig, same machine, `??=` lengthCache in place (per-writer Dictionary keyed by
reference identity, populated post-order by Measure_, consumed at write sites). Baseline
drift between runs: legacy +1.6%, Google +1.8% - normalize before reading deltas.

| row | recompute (cuts 1-4) | lengthCache (cut 5) | normalized delta |
| --- | ---: | ---: | ---: |
| LegacyReal | 20.64 us | 20.97 us | (drift) |
| GeneratedProtogen | 13.56 us | 13.93 us | ~wash |
| NanoGenerated | 14.48 us | **12.64 us** | **-14%** |
| NanoGeneratedMeasure | 2.23 us | 3.82 us | cold measure now includes cache population |
| GoogleProtobuf | 13.08 us | 13.32 us | (drift) |

Verdict per the tiebreaker rule: **keep the cache**. It wins outright on the plain-DTO
row - which puts the generated model AHEAD of Google.Protobuf on its home turf (12.64 vs
13.32 us) - is a wash on the extensible protogen row, regresses nothing, and is the only
entrant that also answers shared references (measured once) and the future mixed-contract
counting mode. The measure row's rise is the cost moving, not growing: the write rows pay
exactly one cold population plus all-hit lookups, where recompute paid a full re-walk per
nesting level.


The self-describing descriptor payload (7,670 bytes), serialized five ways into one reused
MemoryStream. Setup gates: byte identity with the legacy payload for BOTH Reflection-DTO
rows *and* (observed, reported rather than required) the bench-DTO row; census equality
through a legacy reparse for the Google row; and `Measure_` returning exactly the byte
count the write then produces.

## net10.0, 2026-08-12, writer cuts 1-4 (surface-first veneers, recompute-always measure)

| row | mean | vs LegacyReal | notes |
| --- | ---: | ---: | --- |
| LegacyReal | 20.64 us | baseline | RuntimeTypeModel over protobuf-net.Reflection's DTOs |
| GeneratedProtogen | 13.56 us | **-34%** | the SAME object graph through the shipped generated model - the pure engine-swap row |
| NanoGenerated | 14.48 us | -30% | generated model over the bench DTOs (nullable-guard shapes) |
| NanoGeneratedMeasure | 2.23 us | - | `Measure_` alone, one full-tree traversal |
| GoogleProtobuf | 13.08 us | -37% | home turf (generated parser/writer, its own DTOs) |

Readings, for the next cuts:

- **The engine swap alone is worth 34%** before the fast buffer core exists: every raw op
  still routes through the virtual `Impl*` backends, and every sub-message length is
  recomputed. Both planned cuts attack the remaining ~13.5 us directly.
- **Measure is ~2.2 us for one traversal, but recompute-always pays it repeatedly**: the
  write re-measures each subtree per nesting level, so the aggregate measure share of the
  write rows is a multiple of 2.2 us on this tree. That bounds what the `??=` lengthCache
  can recover - worth several microseconds here, i.e. potentially ~20% of the write row -
  and gives the race its stakes.
- Google.Protobuf is only ~4% ahead of the shipped generated model already; the read arc
  ended ~26% ahead of Google on parse, which is the shape to aim for once the buffer core
  and memoization land.
