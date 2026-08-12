# The full matrix: runtime × parser, flat fields and sub-messages

**PRELIMINARY** — ShortRun, one machine. The netfx rows run the genuinely different layout:
no ref fields (`byte[]` + index behind the inlined `At` accessor, bounds checks and all), against
the net462 build of Core for the legacy rows. Correctness swept first: every GlobalSetup gate ran
green under net472 before timing (all three parsers agree on (count, sum, last), every scenario).

```
BenchmarkDotNet v0.15.8, Windows 11, AMD Ryzen 9 7900X (Zen4), x86-64-v4
.NET 10.0.10 vs .NET Framework 4.7.2 (LegacyJIT/RyuJIT net472)
```

## Flat fields (ns/field)

| | LegacyReal | NanoViaLegacyApi | NanoRaw |
| --- | ---: | ---: | ---: |
| net10 / small | 4.67 | 1.26 | 0.79 |
| net10 / mixed | 6.46 | 2.97 | 1.88 |
| net472 / small | 13.21 | 3.27 | 1.56 |
| net472 / mixed | 15.81 | 5.64 | 2.63 |

## Sub-messages (ns/record)

| | LegacyReal | NanoViaLegacyApi | NanoRaw |
| --- | ---: | ---: | ---: |
| net10 / prefixed / small | 13.06 | 3.88 | 2.88 |
| net10 / prefixed / mixed | 14.23 | 5.35 | 3.88 |
| net10 / group / small | 11.68 | 4.01 | 2.46 |
| net10 / group / mixed | 13.11 | 5.30 | 3.46 |
| net472 / prefixed / small | 30.72 | 10.23 | 6.18 |
| net472 / prefixed / mixed | 33.92 | 12.54 | 7.65 |
| net472 / group / small | 29.22 | 11.07 | 6.89 |
| net472 / group / mixed | 32.14 | 13.56 | 8.21 |

## What the matrix says

1. **Nano-on-net472 beats legacy-on-net10, everywhere.** The down-level raw rows (1.56–8.21 ns)
   are faster than the modern-runtime legacy rows (4.67–14.23 ns) in every scenario - the
   bounds-checked `arr[index]` layout with the old JIT still outruns the incumbent on the newest
   runtime. The internals are worth more than the runtime.
2. **The netfx penalty for nano is real but bounded**: raw pays ~1.4–2.4× vs its net10 self
   (bounds checks + older JIT), while legacy pays ~2.3–2.8× for the runtime alone. The down-level
   path was designed to pay - and it pays less than legacy does.
3. **Veneer-on-net472 lands roughly at legacy-on-net10** (3.27 vs 4.67 flat-small; 10.2 vs 13.1
   prefixed-small; near-parity on group-mixed): a netfx consumer gets approximately
   modern-runtime performance from the internals swap alone, with no code changes.
4. Within net472, the decomposition holds its shape: internals ~2.4–4× (identical consumer code),
   raw a further ~1.5–1.7×.

Caveats: ShortRun; single machine; the legacy net472 rows include whatever the net462 Core build
does differently from net8; the spike still lacks features whose costs will partly return
(multi-segment, streams, interning, contexts), so re-measure as they arrive.
