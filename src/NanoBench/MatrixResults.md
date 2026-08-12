# The full matrix: runtime × parser, flat fields and sub-messages

**PRELIMINARY** — ShortRun, one machine. The netfx rows run the genuinely different layout:
no ref fields (`byte[]` + index behind the inlined `At` accessor, bounds checks and all), against
the net462 build of Core for the legacy rows. Correctness swept first: every GlobalSetup gate ran
green under net472 before timing.

Two fairness passes are baked into these numbers, each measured on its own:

- **wire-type tolerance** (int fields carry varint/fixed32/fixed64 case labels, mirroring the
  emitted shape): free on net10, ~5–13% on net472's tightest loops (old JIT switch lowering);
- **safeguard parity** (field-0 rejection folded into the tag range check; max depth 512;
  exact-consumption on length-scope pop; truncated-group detection; reference-tracking recursion
  detection deliberately waived - the depth cap is the fair trade): noise-level on flat rows (the
  range check is one compare, same as the bare MSB test), **+10–28% on net10 sub-message rows**
  and +5–12% on netfx - the per-record push/pop carries the depth counter. An int-compare
  consumption check reclaimed nothing measurable; the cost is the counter and branch structure.

That sub-message cost is precisely the case for the **elision lever** (docs/nano-core.md): the
benchmark's Child is provably non-recursive, so a generator with model cycle analysis would emit
unchecked pushes here and reclaim all of it, retaining checks only for genuinely self-repeating
trees (the skip path and open-world paths always check).

```
BenchmarkDotNet v0.15.8, Windows 11, AMD Ryzen 9 7900X (Zen4), x86-64-v4
.NET 10.0.10 vs .NET Framework 4.7.2
```

## Flat fields (ns/field)

| | LegacyReal | NanoViaLegacyApi | NanoRaw |
| --- | ---: | ---: | ---: |
| net10 / small | 4.67 | 1.29 | 0.87 |
| net10 / mixed | 6.46 | 3.28 | 1.93 |
| net472 / small | 13.21 | 3.27 | 1.73 |
| net472 / mixed | 15.81 | 5.59 | 2.64 |

## Sub-messages (ns/record)

| | LegacyReal | NanoViaLegacyApi | NanoRaw |
| --- | ---: | ---: | ---: |
| net10 / prefixed / small | 13.06 | 4.66 | 3.78 |
| net10 / prefixed / mixed | 14.23 | 6.47 | 4.85 |
| net10 / group / small | 11.68 | 3.99 | 2.96 |
| net10 / group / mixed | 13.11 | 5.81 | 4.03 |
| net472 / prefixed / small | 30.72 | 11.49 | 7.35 |
| net472 / prefixed / mixed | 33.92 | 13.95 | 8.71 |
| net472 / group / small | 29.22 | 11.87 | 7.30 |
| net472 / group / mixed | 32.14 | 14.22 | 8.73 |

(Legacy rows from the first matrix run - that code never changed; nano rows are the fully-guarded,
wire-tolerant re-run.)

## What the matrix says

1. **Nano-on-net472 beats legacy-on-net10, everywhere** — carrying the tolerant switch AND full
   safeguard parity. The down-level raw rows (1.73–8.73 ns) are faster than the modern-runtime
   legacy rows (4.67–14.23 ns) in every scenario. The internals are worth more than the runtime.
2. **Veneer-on-net472 lands at or better than legacy-on-net10** (3.27 vs 4.67 flat-small; 11.49
   vs 13.06 prefixed-small): a netfx consumer gets modern-runtime-class performance from the
   internals swap alone, with no code changes.
3. Within each runtime the decomposition holds: internals ~2–4× under identical consumer code,
   the raw surface a further ~1.2–1.7× on top.
4. Safeguards are the honest tax on dive-heavy paths — and the elision lever exists to refund
   them wherever the model provably cannot recurse.

Caveats: ShortRun (observed cross-run variance up to ~11% on individual netfx cells - treat all
single-digit-percent deltas with suspicion); single machine; the spike still lacks features whose
costs will partly return (multi-segment, streams, interning, contexts), so re-measure as they
arrive; full-job runs before any number leaves this file.
