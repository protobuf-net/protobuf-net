# Descriptor serialize composite (notes/nano-writer.md)

## Where the write path stands (net10.0, 2026-08-13, cut 10 + the tag ladder)

Both destinations, one sitting, `--job short`. The payload is the 7,670-byte self-describing
descriptor set.

| engine | stream | buffer-writer | vs Google (stream) | alloc (stream) |
| --- | ---: | ---: | ---: | ---: |
| **vanilla protobuf-net** (`RuntimeTypeModel`) | 20.026 us | 40.335 us | +52% | 0 B |
| **AOT protobuf-net**, shipped protogen model | 11.590 us | 17.831 us | -12% | 0 B |
| **AOT protobuf-net**, bench DTOs | **10.408 us** | **9.986 us** | **-21%** | 0 B |
| AOT, explicit `IMeasuredProtoOutput` | 15.313 us | 15.074 us | +16% | 1 B |
| **Google.Protobuf** (home turf) | 13.213 us | 12.824 us | - | 4232 B |
| *UTF-8 floor* (see below) | *2.666 us* | *2.666 us* | *-80%* | *0 B* |

As throughput: **737 MB/s** for the AOT model against **580 MB/s** for Google.Protobuf and
383 MB/s for the runtime model, on the stream.

Readings:

- **The two backends have converged**, which is what cut 10 was for: the AOT model is 10.408 us
  on the stream and 9.986 on the buffer-writer, where before that cut the same rows read 14.521
  and ~9.9. The stream was the outlier and no longer is.
- **The runtime model goes the other way, and dramatically**: 20.0 us on the stream against
  **40.3** on the buffer-writer. That is not a writer defect - it is measure-first pricing. A
  buffer-writer cannot back-fill, so a serializer with no arithmetic measure has to be priced by
  null-writer traversal, which the corpus put at ~2.3x. It is exactly why the stream backend's
  measure-first gate is conditional on `IMeasuringSerializer`.
- **AOT beats Google.Protobuf on both destinations** by ~21%, and allocates nothing where Google
  allocates 4,232 B per serialize on the stream. Against vanilla protobuf-net the engine swap is
  **-48% on the stream and -75% on the buffer-writer**.
- **The explicit measure-first API is the slow row**, 15.3/15.1 against 10.4/10.0 for a plain
  `Serialize`. Asking the root for its length up front costs more than it looks - already
  recorded in `notes/nano-writer.md` - and this is a standing item rather than a surprise.

### "We're probably just measuring UTF8 at this point" - no, a quarter of it

`Utf8Floor` is a real row now, not an estimate: every string in the graph (393 of them, 5,475
bytes, **71.4% of the payload**) put through `GetByteCount` *and* `GetBytes` - both halves,
since a measure-first serializer pays both - and nothing else. No tags, no lengths, no
traversal, no destination.

**2.666 us**, i.e. **25.6% of the AOT write row** and 20% of Google's. So the encoder is a
quarter of the job and the other three quarters are still traversal, guards, framing and
destination. Worth knowing in both directions: it is not the whole story, and it *is* a hard
floor that no amount of writer work goes below - the remaining headroom above Google is roughly
7.7 us of non-UTF8 work against their 10.5.


## net10.0, 2026-08-13, THE STREAM BACKEND GOES SPAN-BACKED (the buffer core, stream half)

Paired, same machine, same sitting, `--job short`; the "before" leg is the commit immediately
prior (the museum bridge, which is inert), the "after" leg is measured twice to price the noise.

| row | before | after | after (2nd run) | delta | vs the gauge |
| --- | ---: | ---: | ---: | ---: | ---: |
| LegacyReal | 20.776 us | 20.318 us | - | -2.2% | +3.7% |
| GeneratedProtogen | 14.821 us | 12.590 us | 11.857 us | -15.1% | -9.9% |
| **NanoGenerated** | **14.521 us** | **10.286 us** | 10.163 us | **-29.2%** | **-25.0%** |
| NanoGeneratedMeasured | 20.911 us | 15.537 us | 15.326 us | -25.7% | -21.3% |
| NanoGeneratedMeasure | 3.673 us | 3.635 us | 3.785 us | -1.0% | (writer-free row) |
| GoogleProtobuf (drift gauge) | 13.873 us | 13.084 us | 13.154 us | -5.7% | - |

**The generated model goes from 4.7% BEHIND Google.Protobuf to 21.4% AHEAD, on the stream** -
14.521 vs 13.873 before, 10.286 vs 13.084 after. These are the headline rows: everything in the
serialize battery writes to a `MemoryStream`, which is precisely why cut 9's span-direct arm was
invisible here until now.

Why it is so much larger than cut 9's ~9% on the buffer-writer: that cut removed the virtual
`Impl*` hop from a backend that already had a span. This one gives the stream backend a span at
all, so every raw op moves from `DemandSpace` (two field loads off the writer, then a write
through `ioBuffer[ioIndex]`) to a register compare against `RemainingInCurrent` and a
span-direct store. The runtime-model row (`LegacyReal`) barely moves, as expected - it takes the
stateful path throughout and gains only the cheaper room check.

**The gauge moved -5.7% between the two legs**, which is more drift than any single row's noise,
so the normalised column is the one to read; the two "after" runs agree to within 1.2%, and the
writer-free measure row to within 4%.

### Coverage, checked rather than assumed

The lesson from cut 9 was that a path no gate drives is unmeasured, not fine - the corpus
differential passed with the span-direct arm completely broken, because every gate wrote to a
stream and the stream backend had no span. That is now the opposite way round, and it was
probed rather than believed: corrupting the fast tag arm (`LocalWriteByte((byte)(tag ^ 1))`)
takes **AotDifferential from 3029/3029 (100%) to 1800/3029 (59%), exit 1**, where before this
change the identical corruption was recorded as *"3023 match, exit 0"*. So the arm is genuinely
reached, and the green run means something.

Note `protobuf-net.Test` still passes with that corruption in place: the raw tag path is
generated-model only, so the runtime-model suite never reaches it. AotDifferential is the gate
for this area.


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


## The measured path, priced (net10.0, 2026-08-13)

`NanoGeneratedMeasured` uses `IMeasuredProtoOutput` - measure once, then write - which is the
only route that knows the payload size before writing, and so the only one where a presized
lease can fire. Added to price exactly that.

| row | stream | buffer-writer |
| --- | ---: | ---: |
| NanoGenerated | 15.58 us / 22,392 B | 11.38 us / 22,392 B |
| NanoGeneratedMeasured | 24.42 us / 44,784 B | 18.64 us / 44,784 B |

**The measured path costs ~57-64% more and allocates exactly twice**, on both backends. The
extra measure explains only part of it (the arithmetic measure alone is 3.6 us); the doubled
allocation points at `NetObjectCache.InitializeFrom`, which COPIES the raw length cache into
the target writer rather than handing it over.

Consequence for the buffer core: the presized lease was built and then parked, because it is
neutral in both directions (paired: 15.76 -> 15.58 unmeasured, 24.72 -> 24.42 where it fires)
and the only path that could feed it is dominated by costs the lease cannot touch. See
notes/nano-writer.md.

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
