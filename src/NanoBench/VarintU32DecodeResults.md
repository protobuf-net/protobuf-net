# u32 varint decode: strategy comparison

**PRELIMINARY** — ShortRun job (3 warmup, 3 iterations), one machine, and see the trust caveat on
the mixed row below. Numbers are ns/op over 1,024-value streams, serial offset chain (parsing is
serial). Re-measure before believing anything to a fraction of a nanosecond.

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9032)
AMD Ryzen 9 7900X (Zen4), .NET 10.0.10, X64 RyuJIT x86-64-v4
```

Length 1–5 = uniform streams of that encoded length; 0 = shuffled mix of 1–5 (equal weights).

| ns/op | mixed | len 1 | len 2 | len 3 | len 4 | len 5 |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| ByteLoop (baseline) | 1.09 | 0.36 | 0.72 | 1.09 | 1.44 | 1.82 |
| **ByteUnrolled** | **0.85** | **0.36** | 0.63 | **0.86** | **1.13** | **1.24** |
| EarlyExit1Then4 | 2.24 | 0.38 | 2.75 | 2.74 | 2.75 | 2.74 |
| EarlyExit2Then8 | 1.82 | 0.36 | **0.57** | 2.67 | 2.69 | 2.69 |
| Load4Then1 | 2.22 | 2.64 | 2.65 | 2.64 | 2.63 | 1.13 |
| Load8TzcntSwar | 2.28 | 2.11 | 2.12 | 2.12 | 2.13 | 2.29 |
| Load8TzcntPext | 1.93 | 1.93 | 1.91 | 1.92 | 1.92 | 1.93 |
| Load8Switch | 2.06 | 2.00 | 1.87 | 1.93 | 1.99 | 2.14 |

## What the table says

1. **The dumb unrolled if-chain wins nearly everything.** On uniform streams the Zen4 branch
   predictor makes `ByteUnrolled` close to free: 0.36 ns at length 1, and still fastest at length 5
   (1.24 ns) where it takes four predicted-taken branches. Every clever load-based strategy loses to
   it on every uniform row.
2. **Branchless buys the worst case, not the average.** `Load8TzcntPext` is a flat ~1.93 ns
   *everywhere* — completely distribution-immune, and the only strategy whose mixed number equals
   its uniform number. That flatness is the product; it just isn't cheap enough to beat predicted
   branches on this data.
3. **⚠ Do not trust the mixed row yet.** The mixed stream is a fixed 1,024-value shuffle, replayed
   identically every invocation — and a Zen4 TAGE predictor can *learn* a repeating pattern of that
   period. `ByteUnrolled` at 0.85 ns on mixed — faster than its own uniform length-3 — is the
   smoking gun: on genuinely unpredictable data the branchy strategies should degrade toward
   mispredict-dominated costs, and here they did not. The mixed row needs streams long enough (or
   regenerated per iteration) to defeat pattern memorization before any branchy-vs-branchless
   conclusion is drawn. This is exactly the "distribution is a benchmark axis" trap from
   docs/nano-core.md, one level deeper: *periodicity* is an axis too.
4. `EarlyExit2Then8` is the best hybrid where it exits early (0.57 ns at length 2 — the only
   strategy to beat `ByteUnrolled` on any row) and pays ~2.7 ns beyond its exits. If real tag/length
   distributions are as 1–2-byte-heavy as expected, a measured-on-real-corpus version of this hedge
   is the contender.
5. `Load4Then1` at length 5 (1.13 ns) is the predictability story again in disguise: on a uniform
   length-5 stream its `msbs != 0` branch is never taken, so it runs a straight, predicted,
   tzcnt-free path. A cute artifact, not a strategy.
6. `Pext` vs `Swar` vs `Switch` are within ~0.4 ns of each other; pext is the flattest but carries
   the Zen1/Zen2 microcode hazard (catastrophic there; must stay guarded).

## Next steps for this table

- defeat pattern memorization on the mixed row (bigger streams / per-iteration data), then re-run;
- add a **realistic distribution** row: tag/length frequencies sampled from an actual corpus (the
  differential suite can dump one) rather than uniform-over-lengths;
- offset/alignment axis; the tolerant 6–10-byte value spill; arm64;
- call-shape (ref-struct field vs local, crossing an inlining boundary) only as a **tiebreaker** —
  it matters iff the corrected mixed row leaves finalists within noise of each other;
- full (non-Short) job before any number is quoted outside this file.
