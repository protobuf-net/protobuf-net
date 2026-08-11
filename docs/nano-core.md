# Nano-core: rewriting the reader/writer internals

The goal, stated bluntly: the existing `ProtoReader.State` / `ProtoWriter.State` internals are slow —
2–5× behind both Google.Protobuf and a 2022 prototype on every axis that was measured — and they do
not use the ref struct shape for what it is good at. This is a **systematic rewrite of the core**,
not a parallel library: "nano" is the new *implementation* of `ProtoReader.State` and
`ProtoWriter.State`, behind the same surface, so every consumer and every generated serializer
inherits it without change.

## Where the prior art lives

The `v4` branch (still on the remote, last touched 2023-02) contains the prototype, under the
working name "Nano". The parts that matter, all at `origin/v4`:

| path | what |
| --- | --- |
| `src/protobuf-net.Nano/Nano/PrepReader.cs`, `PrepWriter.cs` | the working POC reader/writer (the `Reader.cs`/`Writer.cs` beside them are aspirational stubs) |
| `src/protobuf-net.Nano/Nano/INanoSerializer.cs` | the three-method contract: `Read(ref reader) → T`, `Measure(in T) → long`, `Write(in T, ref writer)` |
| `src/protobuf-net.Nano/Nano/Internal/` | the memory strategies: `RefCountedMemory`, `RefCountedSlabAllocator`, `SimpleSlabAllocator` |
| `src/Benchmark/Nano/Results.md` | the headline numbers (below) |
| `src/Benchmark/Nano/DecodeResults.md`, `EncodeResults.md` | intrinsic-level varint studies with kept tables |
| `src/Benchmark/Nano/HandWritten*.cs` | the direct-code serializer shape, in three memory-strategy variants |
| `src/NanoTestRig/` | client/server harness proving it over real gRPC |

Headline numbers from `Results.md` (net6.0, outputs validated; GBP = Google.Protobuf, PBN = v3):
serialize ~3–4× faster than PBN and ~1.8–2× faster than GBP; deserialize ~2–3× faster than PBN;
**measure ~10–12× faster than either**; deserialize in pool mode allocates **101 B** where GBP
allocates 770 KB. PBN v3 loses to GBP on every row.

## The design principles to carry over

Each of these is visible in the v4 code, and most are the same discipline as SE.Redis's
`RespReader`/`RespWriter`:

1. **Hot state is registers.** The reader is a span/array plus `_index`/`_end` ints. Every hot
   method is bounds-check-plus-arithmetic; slow halves split into `*Slow` methods; throws in
   `NoInlining` helpers.
2. **Measure is pure arithmetic** (that is the 10×) and write then targets a single pre-sized
   region — no capacity checks in the hot path, one flush at the end.
3. **Direct simple code for the common case.** Serializers switch on raw uint tags as compile-time
   constants — `case (2 << 3) | (int)WireType.String:` — with plain `is { Length: > 0 }` write
   guards. There is a `TryReadTag(expected)` fast path for fields arriving in order.
4. **Repeated fields via function pointers**, not interface dispatch, consuming a *run* of same-tag
   fields in one call (`UnsafeAppendLengthPrefixed(list, &Item.Merge, tag)`).
5. **Varint intrinsics are measured, not assumed.** Decode via `tzcnt`-based branchless handling
   (~2× at length ≥4, slightly worse at 1–2 — the `PreferShort` hedge exists for small-value
   streams); measure via `lzcnt` is effectively free (0.23 ns); encode via zero-high-bits /
   shifted-masks ~1.5–2× at length ≥2. The tables are in the two results files; re-measure on
   current runtimes before locking choices.
6. **Memory strategy is a spectrum, not a decision**: plain allocation (best CPU), pooled leaf
   `bytes` with explicit `Dispose` (near-zero alloc), slab allocation between them. v4's conclusion
   was to offer both ends.
7. Supporting details worth keeping: `GC.AllocateUninitializedArray`, `CollectionsMarshal.AsSpan`
   over lists, `Unsafe.As` reinterprets, and the `USE_SPAN_BUFFER` A/B (span-field vs array-field
   buffer representation — benchmark both; v4 kept both compiled).

## Micro-benchmarks: "functionally correct" is step 0

Expect a *lot* of silly micro-benchmarks for minutiae, and treat that as the method, not overhead.
The differential suite gets a change through the door; the benchmark table is the actual review.
Every hot-path method — field headers, integer reads/writes, varint/zigzag, strings, length
prefixes — gets squeezed individually, because this is exactly where v4 found its wins and its
traps:

- **winners flip with the input distribution.** The tzcnt varint decode is ~2× at length ≥4 and a
  *regression* at length 1–2 (see `DecodeResults.md`) — which is why the `PreferShort` hedge exists,
  and why every varint benchmark is parameterised by byte-length *and* buffer offset. A single
  "varint benchmark" number is a lie; the table is the result.
- **the v4 convention is the right one**: per-concern benchmark files
  (`DecodeIntrinsicBenchmarks`, `EncodeIntrinsicBenchmarks`, `StringMaterialization`,
  `MemorySliceBenchmarks`, `ArrayAllocBenchmarks`, `ConstructionBenchmarks`) with the results
  **committed as markdown tables beside them**, so review sees numbers, not claims — the same
  derive-don't-guess rule the AOT work ran on, applied to nanoseconds.
- **2022 numbers are hypotheses, not facts.** Everything in the v4 tables predates net8/net10
  codegen changes and newer intrinsics; re-measure before locking any variant in, and note the
  hardware in the committed table (deltas on one machine, never absolute figures across machines —
  the same rule the AOT binary-size work used).

The micro-benchmarks resurrect under `src/Benchmark` (the project already exists and carried the
v4 Nano suites). The working rule for the hot path: **no change without its table** — a hot-path
PR that says "should be faster" without a before/after is not reviewable.

## What v4 never built — and the rewrite cannot dodge

The POC only ever handles a **single contiguous buffer**: `ReadStringSlow`, `ReadRawByteSlow` and
the multi-segment story are `NotImplementedException`, and `PrepWriter` grabs one 300 KB region and
hopes. The real `State` supports streams and `ReadOnlySequence<byte>`; the rewrite has to decide the
buffer model *first* (contiguous fast path with an explicit refill boundary is the obvious shape —
the same trick the current reader plays, but with the fast path actually fast).

Also unbuilt: slab strings (`ReadOnlyMemory<char>`) were sketched only, and the whole POC predates
`SearchValues`/net8 intrinsics — some of the 2022 measurements deserve re-running.

## What "same surface" means, and the escape valve

The shape being preserved is captured in `src/NanoState/` — `ReaderState.cs` and `WriterState.cs`
are **generated from the real types** (`generate-shape.ps1`, re-runnable) as throwing stubs: 82
methods + 8 properties on the reader, 72 + 9 on the writer, with the two members whose signatures
name Core-internal types kept as comments. That is the to-do list; implement by moving members out
of the generated files into hand-written partials, so what remains generated is what remains to do.

Not everything on that surface deserves a fast path. The 42-niche-scenarios machinery — extension
data, groups-as-framing, dynamic types, `IExtensible`, reference-tracking remnants — can sit on a
deliberately-boring implementation *behind the same members*, provided the boundary is explicit and
the hot scalar/message/repeated paths never pay for it. The AOT generator is what makes this viable
now in a way it was not in 2022: the generated serializers are the "direct simple code" consumer,
and the generator knows *at compile time* which contracts stay on the boring path.

**The existing surface is the floor, not the ceiling — the new surface is the point.** It is
absolutely expected and planned that the key optimisations arrive as *new* members, with the
existing API reimplemented over them for compatibility. The archetype is the field header:

- today, `ReadFieldHeader()` decodes the tag varint into a field *number* (returned) and a
  `WireType` (written to state) — shift, mask, two results, and every consumer that wants to
  dispatch reassembles what the wire had already joined;
- the new primitive returns **the raw units**: `ReadTag() → uint`, the tag varint as-is. Generated
  code switches on compile-time constants — `case (2 << 3) | (int)WireType.String:` — one read, no
  decomposition, no state writes, and the switch is a jump table over exactly what came off the
  wire. `TryReadTag(expectedTag)` is its companion for the fields-in-order fast path, and the old
  `ReadFieldHeader()`/`WireType` pair becomes a shift-and-mask veneer over it.

The same pattern recurs across the surface: raw-run append for repeated fields, measure primitives
that are pure statics, wire-type-carrying write methods that skip the features indirection. New
members live in hand-written `*.Nano.cs` partials beside the generated shape files, so the split
between "compatibility floor" and "new surface" stays visible in the file layout.

## Step plan

1. **(done)** Shape clone in `src/NanoState/` — the surface, as compiling stubs.
2. **Buffer model decision** — the one question v4 dodged. Contiguous fast path + refill boundary;
   sequence/stream input feeds the refill. Write it down before writing code.
3. **Scalar hot paths** — varint read/write/measure with the intrinsic variants from the v4 tables,
   re-measured on net8/net10; fixed32/64; string materialization (see `StringMaterialization.cs`).
4. **The new-surface API set** — the raw-tag loop (`ReadTag`/`TryReadTag`), same-tag run appends,
   static measure primitives; the generator emits against these, and each existing member that they
   subsume becomes a veneer. This list is additive API, so it also lands in `PublicAPI.Unshipped`
   when it reaches Core — the API tracking makes the new surface reviewable as such.
5. **The niche fence** — enumerate which `State` members are hot-path and which sit on the boring
   implementation; this list is the real design review.
6. **Swap-in** — the new implementation becomes the internals of the real `State` types. The
   differential suite (`src/AotDifferential`) is the correctness gate: byte-for-byte agreement over
   ~3,000 contracts, both directions. Resurrect the Nano benchmarks as the performance gate.

Rules of the road, inherited from the AOT work: derive rather than guess (the shape files are
generated; the perf tables are measured), and nothing merges on "should be faster" — the
differential decides correctness, and correctness is only the entry ticket: BenchmarkDotNet tables,
committed beside the benchmarks, decide the rest.
