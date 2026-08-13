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

## Outcome: applied, validated in situ, and REVERTED

The recommendation above was applied and then measured on `DescriptorSerializeBenchmarks`, which
is the arbiter. **The 2.2x does not transfer, and may cost slightly:**

| | before | after | vs the Google gauge (-1.1%/-1.2%) |
| --- | ---: | ---: | --- |
| stream `NanoGenerated` | 14.27 us | 14.38 us | ~+2% worse |
| stream `NanoGeneratedMeasure` | 3.626 us | 3.600 us | flat |
| buffer-writer `NanoGenerated` | 9.81 us | 9.96 us | ~+2% worse |

So it was reverted, per this repo's standing rule that nothing merges on "should be faster".

**Why the micro-benchmark misled, which is the durable lesson here:** the table swaps
register-only arithmetic for a memory load, and it was measured in a tight loop of back-to-back
calls - an access pattern that never occurs. In a real write the measure is interleaved with
other work, where the arithmetic latency was already hidden by instruction-level parallelism and
the extra load is not free. A primitive microbenchmark measures THROUGHPUT of a thing that is
never issued back-to-back; the win it reports is available only in that shape.

Keep the matrix: it is a correct, checked record of the relative cost of these formulations, and
it says useful things (the divide costs 18%; a jump table loses with 30x the variance; the
down-level loop is worse than even the intrinsic baseline on wide data). Just do not assume any
of it converts into end-to-end time.

**The down-level arm was then measured too, and is also null.** net472 descriptor serialize,
paired, ladder vs the shipped shift loop: `NanoGenerated` 25.42 -> 25.74 us and
`GeneratedProtogen` 27.13 -> 27.39 us, against a gauge that moved +1.2% - i.e. flat. The telling
row is `NanoGeneratedMeasure`, which is *nothing but* the arithmetic traversal: even there the
ladder is worth only ~2%. Reverted, for consistency with the intrinsic arm.

**So the line is closed: the varint length primitive is not a meaningful fraction of a write on
EITHER TFM.** Two microbenchmark winners, 2.2x and "beats the loop everywhere", both producing
nothing end to end. The matrix remains a correct record of relative cost; it is simply measuring
something that does not matter at this scale.

**One caveat on "does not matter": it is true of THIS corpus.** Packed repeated writes measure
per ELEMENT, so if packed lands (ladder item 5) the measure becomes far hotter and this should be
re-tested rather than assumed still-null. The matrix is already built, so that re-test is cheap.

**Superseded, for the record:** the previous text below. (Now answered above.)
