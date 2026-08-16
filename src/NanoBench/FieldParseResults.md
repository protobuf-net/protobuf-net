# First end-to-end three-way: repeated "field 1, varint" parse

**PRELIMINARY** — ShortRun, one machine, and a deliberately minimal payload (one varint field,
last-wins, 64K records; non-repeating values to blunt predictor memorization). The correctness
gate in GlobalSetup passed: all three parsers agree on (count, sum, last) against expected.

```
BenchmarkDotNet v0.15.8, Windows 11, AMD Ryzen 9 7900X (Zen4), .NET 10.0.10, x86-64-v4
```

| ns/field | LegacyReal | NanoViaLegacyApi | NanoRaw |
| --- | ---: | ---: | ---: |
| small (1-byte values) | 4.65 | 1.21 (3.9×) | 0.80 (5.8×) |
| mixed (1–5 byte values) | 6.43 | 3.11 (2.1×) | 1.88 (3.4×) |

The decomposition is the point, and it is exactly the landing-strategy design:

- **(a) → (b): the internals, isolated.** Same consumer code, same stateful API
  (`ReadFieldHeader`/`ReadInt32`), different reader underneath — worth 2–4× by itself. This is the
  like-for-like row: what swapping `State`'s internals buys every existing consumer with no code
  change.
- **(b) → (c): the raw surface, isolated.** Constant-tag switch + encoding-in-the-name reads over
  the same internals — a further ~1.5×. This is what the generator's nano pass buys on top.

Honesty notes before quoting these anywhere:

- the legacy reader is paying for generality the nano spike has not built yet (multi-segment
  sources, streams, groups, interning, contexts, solid-state) — some of that cost will return as
  those features arrive, which is why the veneer row must be re-measured as the spike grows;
- single-field flat payloads flatter everyone; strings, sub-messages (scope push/pop) and skips
  are the next rows;
- ShortRun; full job before any number leaves this file.
