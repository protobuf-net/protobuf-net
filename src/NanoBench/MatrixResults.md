# The full matrix: runtime × parser, flat fields and sub-messages

**PRELIMINARY** — ShortRun, one machine. The netfx rows run the genuinely different layout:
no ref fields (`byte[]` + index behind the inlined `At` accessor, bounds checks and all), against
the net462 build of Core for the legacy rows. Correctness swept first: every GlobalSetup gate ran
green under net472 before timing (all three parsers agree on (count, sum, last), every scenario).

**Fairness note, and a claim tested**: the raw rows carry the same wire-type tolerance the legacy
API has — an int field's switch has case labels for varint, fixed32 AND fixed64, dispatching to
the correctly-named raw read (mirroring the emitted shape, which the NanoPass golden pins). The
"jump table absorbs the extra labels" claim from docs/nano-core.md was then measured: **true on
net10** (identical within noise), **a small real tax on net472** (~5–13% on the tightest loops —
the old JIT's switch lowering is less clever). All numbers below are the tolerant form.

```
BenchmarkDotNet v0.15.8, Windows 11, AMD Ryzen 9 7900X (Zen4), x86-64-v4
.NET 10.0.10 vs .NET Framework 4.7.2
```

## Flat fields (ns/field)

| | LegacyReal | NanoViaLegacyApi | NanoRaw |
| --- | ---: | ---: | ---: |
| net10 / small | 4.67 | 1.26 | 0.79 |
| net10 / mixed | 6.46 | 2.97 | 1.87 |
| net472 / small | 13.21 | 3.27 | 1.73 |
| net472 / mixed | 15.81 | 5.64 | 2.96 |

## Sub-messages (ns/record)

| | LegacyReal | NanoViaLegacyApi | NanoRaw |
| --- | ---: | ---: | ---: |
| net10 / prefixed / small | 13.06 | 3.88 | 2.90 |
| net10 / prefixed / mixed | 14.23 | 5.35 | 3.82 |
| net10 / group / small | 11.68 | 4.01 | 2.53 |
| net10 / group / mixed | 13.11 | 5.30 | 3.61 |
| net472 / prefixed / small | 30.72 | 10.23 | 6.57 |
| net472 / prefixed / mixed | 33.92 | 12.54 | 7.97 |
| net472 / group / small | 29.22 | 11.07 | 6.92 |
| net472 / group / mixed | 32.14 | 13.56 | 8.28 |

(Legacy and veneer rows are from the pre-tolerance run - their code did not change; the raw rows
are the tolerant re-run.)

## What the matrix says

1. **Nano-on-net472 beats legacy-on-net10, everywhere** — even carrying the tolerant switch. The
   down-level raw rows (1.73–8.28 ns) are faster than the modern-runtime legacy rows
   (4.67–14.23 ns) in every scenario. The internals are worth more than the runtime.
2. **The netfx penalty for nano is real but bounded**: raw pays ~1.6–2.7× vs its net10 self
   (bounds checks + older JIT + the switch tax), while legacy pays ~2.3–2.8× for the runtime
   alone. The down-level path was designed to pay - and it still pays less than legacy does.
3. **Veneer-on-net472 lands roughly at legacy-on-net10** (3.27 vs 4.67 flat-small; 10.2 vs 13.1
   prefixed-small): a netfx consumer gets approximately modern-runtime performance from the
   internals swap alone, with no code changes.
4. Within each runtime the decomposition holds its shape: internals ~2.4–4× under identical
   consumer code, the raw surface a further ~1.4–1.9× on top.

Caveats: ShortRun; single machine; the legacy net472 rows include whatever the net462 Core build
does differently from net8; the spike still lacks features whose costs will partly return
(multi-segment, streams, interning, contexts), so re-measure as they arrive.
