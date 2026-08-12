# Strings: 64K × "field 1: string", replace semantics, memory diagnosed

**PRELIMINARY** — ShortRun, one machine. Gates green (all three parsers agree on
(count, total chars, last string), all four scenarios, both runtimes).

```
BenchmarkDotNet v0.15.8, Windows 11, AMD Ryzen 9 7900X (Zen4), x86-64-v4
.NET 10.0.10 vs .NET Framework 4.7.2
```

| ns/record | LegacyReal | NanoViaLegacyApi | NanoRaw | alloc/op |
| --- | ---: | ---: | ---: | ---: |
| net10 / short / ascii | 20.2 | 16.3 | 15.7 | 40 B (all) |
| net10 / short / unicode | 35.1 | 32.1 | 30.9 | 40 B |
| net10 / long / ascii | 43.4 | 37.2 | 37.3 | 225 B |
| net10 / long / unicode | 243.9 | 228.6 | 234.1 | 225 B |
| net472 / short / ascii | 41.6 | 26.8 | 25.8 | 43 B |
| net472 / short / unicode | 74.3 | 60.5 | 59.2 | 43 B |
| net472 / long / ascii | 78.3 | 63.1 | 62.1 | 230 B |
| net472 / long / unicode | 488.1 | 467.4 | 466.6 | 230 B |

## What the table says

1. **UTF-8 materialization is shared physics.** All three parsers call the same decoder and
   allocate the same string - the diagnoser confirms nobody allocates anything else - so the rows
   converge as strings lengthen: nano's edge is the *framing* fraction, ~1.3× at short-ascii,
   ~1.05× at long-unicode. Legacy's string path was already allocation-clean; Amdahl caps this row.
2. **The netfx story holds** at the framing-dominated end (1.6× at short-ascii) and converges the
   same way at the decode-dominated end.
3. **Veneer ≈ raw for strings** - one header per record, shared string read; expected.
4. Implementation notes: the plausible-length guard lives in ReadRawString (the single-segment
   form is the strong version of legacy's EagerAllocationLimit - a hostile prefix cannot drive
   allocation); zero length returns the empty-string singleton; the netfx path uses
   GetString(byte[], index, count), which IS the down-level layout - no marshalling detour.

## Calibration for the descriptor.proto milestone

The composite number will land between the framing rows (3–5×) and these string rows
(1.05–1.3×), weighted by the payload's field mix - which is why the real-data benchmark is the
north star rather than extrapolation. Future levers if strings ever need more: interning (veneer
parity exists as InternStrings), pooled/slab strings (the v4 sketch, parked), and
ReadOnlyMemory-shaped access that skips materialization entirely - all out of scope until the
milestone review says otherwise.
