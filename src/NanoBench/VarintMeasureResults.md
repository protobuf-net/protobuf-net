# Varint length measurement, strategy matrix (docs/nano-writer.md)

net10.0, 2026-08-13. Baseline is the SHIPPED form, `((31 - LeadingZeroCount(v|1)) / 7) + 1`.
All 40 agreement checks passed first - every strategy is verified against the shipped one over
the sampled data *and* every power-of-two boundary before any timing is taken. That check earned
its place immediately: the first `MulShift` magic constants were wrong (`(x*37+37)>>8` rather
than `(x*37)>>8`) and failed on ordinary values like 99 and 8437. A wrong strategy is cheap and
would simply have looked fast.

## 32-bit (ratio vs shipped; lower is better)

| strategy | small | prefix | uniform | wide |
| --- | ---: | ---: | ---: | ---: |
| `Log2Div` `(Log2(v\|1)/7)+1` | 0.77 | 0.77 | 0.77 | 0.77 |
| `MulShift` `((Log2(v\|1)*37)>>8)+1` | 0.63 | 0.63 | 0.63 | 0.63 |
| **`Table`** (u8 blob indexed by lzcnt) | **0.45** | **0.45** | **0.45** | **0.45** |
| `Ladder` (comparison chain) | **0.29** | 0.74 | 0.57 | 0.56 |
| `Loop` (the shipped DOWN-LEVEL form) | 0.45 | 0.83 | **1.16** | **1.17** |

## 64-bit

| strategy | small | prefix | uniform | wide |
| --- | ---: | ---: | ---: | ---: |
| `Ladder64` | 0.29 | 0.74 | 0.67 | 0.66 |
| `Hybrid64` (one small test, then the shipped form) | 0.29 | 0.67 | 1.07 | 1.07 |
| `Loop64` | 0.45 | 0.83 | 1.16 | 1.19 |

## Readings

- **The shipped form is the slowest intrinsic option in every distribution.** Nothing here was
  ever raced against an alternative, and a 2.2x is sitting on the table.
- **`Table` wins u32 outright, and is distribution-INDEPENDENT** (1,553-1,560 ns across all
  four), so it needs no assumption about the data. `Ladder` is faster still on small-only input
  (0.29) but loses on everything else - it is a bet on the distribution, and `Table` is not.
- **The down-level arm should be `Ladder`, unconditionally.** It needs no intrinsic and beats
  `Loop` in every distribution, and today's loop is *worse than the intrinsic baseline* on wide
  data (1.16-1.17). This is the arm nobody looks at, and it is a shipped configuration.
- `Hybrid64`'s extra branch does not pay outside small-only data.

## Gaps and caveats

- **No `Table64` was tested** - given `Table` dominates u32, it is very likely the u64 answer
  too, and `Ladder64` may not be the real winner. Do this before choosing.
- These are tight loops over arrays. Branch prediction and ILP behave differently at real call
  sites, interleaved with other work, so the RANKING is informative but the magnitudes will not
  transfer intact. The descriptor serialize benchmark is the arbiter for anything that lands.
- The realistic hot input is a sub-message LENGTH PREFIX (constant tags are folded by the
  generator), which is what the `prefix` distribution models.
