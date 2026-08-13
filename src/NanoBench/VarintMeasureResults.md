# Varint length measurement, strategy matrix (docs/nano-writer.md)

net10.0, 2026-08-13. Baseline is the SHIPPED form, `((31 - LeadingZeroCount(v|1)) / 7) + 1`.
Every strategy is verified to AGREE with the shipped one - over the sampled data and every
power-of-two boundary - before any timing is taken. That check paid for itself twice: the first
`MulShift` constants were wrong (`(x*37+37)>>8` instead of `(x*37)>>8`, failing on 99 and 8437),
and a hand-written `Table64` had two 10s where it needed one. Both would simply have looked fast.

## Ratios vs the shipped form (lower is better)

| strategy | prefix | uniform | notes |
| --- | ---: | ---: | --- |
| **`Table`** u32 (u8 blob indexed by lzcnt) | **0.45** | **0.45** | flat across all four distributions |
| **`Table64`** | **0.46** | **0.46** | flat; the u64 answer |
| `MulShift` / `MulShift64` | 0.63 | 0.63/0.64 | the divide removed |
| `Log2Div` | 0.77 | 0.77 | divide retained |
| `Ladder` u32 | 0.74 | 0.57 | distribution-dependent |
| `Ladder64` | 0.74 | 0.67 | |
| `Hybrid64` | 0.67 | 1.07 | the extra branch does not pay |
| `Switch` (over lzcnt) | 0.99 | **1.14** | loses, with huge variance (StdDev 163ns vs ~5ns) |
| `SwitchShift` (over magnitude) | 0.87 | 0.38* | *see the distribution caveat |
| `Loop` (the shipped DOWN-LEVEL arm) | 0.82 | **1.16** | worse than the intrinsic baseline on wide data |

## Conclusions

- **`Table` wins both widths and is distribution-INDEPENDENT** (u32 1,553-1,560 ns across all
  four; u64 likewise). It needs no bet on the data, which is why it beats `Ladder` as a *choice*
  even where `Ladder` is faster on one distribution.
- **The shipped form is the slowest intrinsic option in every distribution** - a 2.2x was sitting
  untested.
- **The divide is NOT free**: `Log2Div` 0.77 vs `MulShift` 0.63, same `Log2`, differing only in
  `/7`. Worth ~18%. (This contradicted a prediction that the JIT's divide-by-constant lowering
  was already optimal; likely the sign-correction on `int` division that the explicit `uint`
  magic skips.)
- **A jump table loses**, matching this repo's own `DispatchResults.md` finding for tag dispatch:
  an indirect branch is beaten by a load, and its variance is 30x everyone else's.
- **Down-level wants `Ladder`.** `Table` needs `lzcnt`, which net472 does not have, so the choice
  there is `Ladder` (0.74/0.57) or the shipped `Loop` (0.82/1.16) - and `Loop` is worse than the
  *intrinsic* baseline on wide data. `Ladder` needs no intrinsic and wins everywhere.

## Caveats, including one mislabelling

- **"uniform" is NOT the adversarial case for a ladder, despite being labelled that way.** A
  uniform random 32-bit value is almost always >= 2^21 (only ~1 in 2048 is below), so every value
  takes the same ladder arm and prediction is near-perfect. That is why `SwitchShift` reads 0.38
  there while reading 0.87 on `prefix`. The genuinely hard case is an even spread ACROSS
  byte-lengths, which `prefix` approximates and which no distribution here targets directly. Read
  the ladder rows with that in mind; `Table`'s flatness is unaffected.
- These are tight loops over arrays. Branch prediction and ILP differ at real call sites
  interleaved with other work, so the RANKING is informative but the magnitudes will not transfer.
  The descriptor serialize benchmark arbitrates anything that lands.
- The realistic hot input is a sub-message LENGTH PREFIX - constant tags are folded to literals by
  the generator - which is what `prefix` models.

## Recommendation

- intrinsic path: `Table` for `MeasureUInt32`, `Table64` for `MeasureUInt64`
- down-level path: `Ladder`
- validate on `DescriptorSerializeBenchmarks` before landing
