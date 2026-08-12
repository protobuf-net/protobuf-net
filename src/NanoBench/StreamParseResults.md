# The buffer model, measured: streaming is nearly free

**PRELIMINARY** — ShortRun, one machine. The correctness battery ran first and is the story's
other half: **7,669 split positions** (the descriptor payload as a two-segment sequence split at
every byte offset — every straddle case for every wire construct), streams at chunk sizes
1/7/4096/65536 hinted and unhinted, the all-single-byte-segments sequence, and legacy cross-stack
agreement — all census-gated, **all green on the refill core's first run**. The
"a refill never preserves or carries anything" design left almost nothing to get wrong: every
straddle is byte-wise consumption, so there is no partial-primitive state to mishandle.

The stream is a non-MemoryStream chunk-feeding wrapper BY DESIGN: both stacks special-case
MemoryStream and collapse to their span paths (legacy via reflection into the private buffer), so
a MemoryStream-fed benchmark measures no streaming at all — Marc's like-for-like catch.

```
BenchmarkDotNet v0.15.8, Windows 11, AMD Ryzen 9 7900X (Zen4), x86-64-v4
.NET 10.0.10 vs .NET Framework 4.7.2 (4.8.1 runtime); whole-document parse (7,670 bytes)
```

| | LegacyStream | NanoStream | LegacyMemory | NanoMemory |
| --- | ---: | ---: | ---: | ---: |
| net10 / 4 KB chunks | 23.81 µs | **8.77 µs** | 22.53 µs | 8.55 µs |
| net10 / 64 KB chunks | 25.95 µs | **8.53 µs** | 23.10 µs | 8.44 µs |
| net472 / 4 KB chunks | 39.02 µs | **15.43 µs** | 45.04 µs | 15.10 µs |
| net472 / 64 KB chunks | 38.86 µs | **15.20 µs** | 45.13 µs | 15.36 µs |

## What the table says

1. **The refill machinery costs ~2.5% when active (4 KB chunks) and ~1% at 64 KB** — the
   NanoStream-vs-NanoMemory delta, which is the honest price of streaming under the design.
   The residency-maximizing fill (top up until the buffer is full) keeps the bulk arms and fast
   paths on duty almost all the time; refills are rare and cheap.
2. **Nano streaming beats legacy's stream backend 2.5–2.7×** — and beats legacy reading from
   MEMORY by ~2.6×: the entire single-segment advantage survives the move to streams intact.
3. **A netfx curiosity, recorded**: legacy's memory path (45.0 µs) is SLOWER than its own stream
   path (39.0) on net472 — the span-based reader pays more than the stream reader on the old
   JIT. Nano shows no such inversion.
4. Allocations: the stream rows cost +0.04 KB over memory (the wrapper object; the 16 KB refill
   buffer is pooled). Nano remains the lowest-allocating stack in every row.

## What this brick landed

`GetNextBuffer` (Stream shift-and-top-up; sequence node-walk, TryGetArray-else-lease);
scope-end vs segment-end split in `EndOfScope` with unbounded-root EOF semantics; byte-wise
straddles everywhere (tags, varints, fixed — LE-assembled, endian-free — skip, packed, and
string/bytes scratch assembly with allocation bounded by real bytes on unhinted streams); both
constructors including the MemoryStream unwrap (public half of legacy's parity) and
single-segment sequence collapse; and group skip, retiring the last NotImplementedException in
`SkipTag`. Deferred, by design: `ReaderSnapshot` (the async/resume story) stays a stub until
that work lands.
