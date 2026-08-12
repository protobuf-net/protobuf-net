# Repeated fields: runs vs interleaved, the no-Try design measured

**PRELIMINARY** — ShortRun, one machine. Gates green (all three parsers agree on
(idSum, stringCount, charSum, lastString, childCount, childSum), both shapes, both runtimes).

Workload: 32K strings (0–16 ascii chars) + 32K two-byte children per parse, as runs of
`RunLength` elements separated by a varint field; per-element normalization (65,536 ops/invoke).
Lists are pre-sized and Cleared per parse, so allocation is the per-element materialization only.

```
BenchmarkDotNet v0.15.8, Windows 11, AMD Ryzen 9 7900X (Zen4), x86-64-v4
.NET 10.0.10 vs .NET Framework 4.7.2 (4.8.1 runtime)
```

| ns/element | LegacyReal | NanoViaLegacyApi | NanoRaw | alloc/element |
| --- | ---: | ---: | ---: | ---: |
| net10 / interleaved (RunLength=1) | 28.9 | 17.0 (0.59) | 15.1 (0.52) | 32 B (all) |
| net10 / runs of 8 | 24.9 | 15.9 (0.64) | 14.7 (0.59) | 32 B |
| net472 / interleaved | 59.2 | 30.7 | 27.0 | 34 B |
| net472 / runs of 8 | 50.1 | 29.5 | 26.4 | 34 B |

## What the table says

1. **Run-shape insensitivity is the design working.** Interleaved→runs saves legacy
   4.0 ns/element (net10) — its `TryReadFieldHeader` miss is a peek that re-decodes at the next
   header read, and interleaved data misses on every element — while nano raw saves 0.4 ns: the
   tag-local loop condition (`while ((tag = ReadRawTag()) == CONST)` + `continue` to dispatch)
   costs nothing on a miss, because the missed tag is already decoded and flows straight to the
   switch. There is deliberately no `TryReadRawTag`; see docs/nano-core.md, "Run consumption
   needs no API at all".
2. **The relative win is *largest* on hostile (interleaved) data** — 1.9× (net10) / 2.2× (net472)
   — precisely where speculation-based designs degrade. Real payloads sit between the two shapes.
3. **Cross-runtime headline holds**: nano-on-net472 (27.0) beats legacy-on-net10 (28.9) on
   interleaved data, and roughly ties (26.4 vs 24.9) on run-shaped.
4. **Allocations identical** across all three parsers (the strings + Child instances; net472's
   +2 B is measurement rounding). The veneer row runs consumer code *identical* to LegacyReal,
   including the do-while on the new `TryReadFieldHeader` veneer (save-offset/read/restore —
   the miss re-decode is a veneer-only cost, matching legacy's peek-without-moving).

## Milestone relevance

descriptor.proto is repeated-message-shaped almost everywhere (`file`, `message_type`, `field`,
`nested_type`, …), typically run-shaped on the wire. This brick plus strings covers the bulk of
its field mix; enums and bytes are the remaining small pieces before the composite benchmark.
