# Packed scalars: the strategy race, adjudicated

**PRELIMINARY** — ShortRun, one machine. Gates green (all four strategies agree on
(count, sum, last) across both encodings, both run sizes, both runtimes). Workload: 64K int32
elements as "field 1: packed run of RunSize"; lists pre-sized and Cleared per parse, so these
deltas are per-element machinery with growth-realloc off the table — the FLOOR of the bulk arms'
advantage (a cold list adds growth costs that SetCount avoids entirely).

```
BenchmarkDotNet v0.15.8, Windows 11, AMD Ryzen 9 7900X (Zen4), x86-64-v4
.NET 10.0.10 vs .NET Framework 4.7.2 (4.8.1 runtime)
```

| ns/element | LegacyReal | NanoAddLoop | NanoHelper | NanoEnsureCapacity |
| --- | ---: | ---: | ---: | ---: |
| net10 / fixed32 / runs of 4 | 7.81 | 1.93 | **1.15** | 2.05 |
| net10 / fixed32 / runs of 64 | 2.95 | 1.43 | **0.45** | 1.48 |
| net10 / varint / runs of 4 | 9.00 | 1.85 | 1.85 | 1.90 |
| net10 / varint / runs of 64 | 3.73 | 1.35 | **1.18** | 1.29 |
| net472 / fixed32 / runs of 4 | 32.24 | 4.82 | **4.47** | 4.62 |
| net472 / fixed32 / runs of 64 | 12.45 | 3.91 | 3.87 | 3.87 |
| net472 / varint / runs of 4 | 32.13 | 4.03 | **3.68** | 4.05 |
| net472 / varint / runs of 64 | 13.35 | 3.15 | 3.11 | 3.16 |

Legacy allocates on every row (8 B/element at short runs — the FillBuffer pooling machinery);
every nano strategy is zero-allocation.

## Verdicts

1. **The fixed32 block copy is the showcase: 0.45 ns/element** — SetCount + `MemoryMarshal.Cast`
   CopyTo is 3.2× the Add loop and 6.5× legacy at runs of 64, and still wins at runs of four.
   The count is exact by construction (len/4), no second pass needed.
2. **The varint terminator pre-scan survives, narrowly**: dead heat at runs of 4 (the second
   pass costs exactly what it saves), ~12% over the Add loop at runs of 64. It never loses, so
   it stays — but the honest summary is that the cute trick earns pennies while the dumb block
   copy earns the headline. The SIMD upgrade (movemask/popcount over the scan) remains available
   if packed varint ever matters more.
3. **EnsureCapacity is dominated** — never better than the scan, barely better than Add — so no
   net6-specific middle arm is warranted; the library forks net8-bulk vs down-level-Add only.
4. **Scope elision in the helpers is visible on netfx short runs** (4.47 vs 4.82): a packed run
   is not nesting, so the helpers bound by length directly — no scope push/pop, no depth count.
5. **Down-level, removing machinery is the whole win**: all three nano strategies converge at
   ~3.1–4.8 ns/element against legacy's 12.5–32.2 (3.5–8×). Legacy's short-run cost is the
   per-field amortization of the pooled-buffer read path — flexibility priced per element.

## Constraints carried forward

- **Residency**: the bulk arms peek without consuming, which is legal only over bytes already in
  the current segment — nothing replays across a refill. Under GetNextBuffer, the existing
  plausible-length check becomes the fast/slow switch: resident (the overwhelmingly common
  case — a Memory payload is resident by construction) takes the bulk arm; a straddling run
  takes the per-element forward loop. See the residency note in ReaderState.Nano.cs.
- The platform fork lives in the LIBRARY as `#if` (all scenarios in one reviewed place, goldens
  TFM-independent); the emitted code is a one-line helper call either way. Harness wrinkle
  recorded in-source: BDN discovers benchmarks on the host TFM, so a `[Benchmark]` method may
  not be `#if`-gated — only its body may fork.
