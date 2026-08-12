# Sub-messages, both terminations: repeated "(child: field 1 varint)" with merge semantics

**PRELIMINARY** — ShortRun, one machine. 64K records, deliberate merge (one child instance reused
throughout - a repeated message field merges - so the loop measures parsing, not allocation).
Correctness gate passed: both parsers agree on (count, sum, last) against expected, both encodings.

```
BenchmarkDotNet v0.15.8, Windows 11, AMD Ryzen 9 7900X (Zen4), .NET 10.0.10, x86-64-v4
```

| ns/record | LegacyReal | NanoRaw |
| --- | ---: | ---: |
| prefixed / small | 12.74 | 2.71 (4.7×) |
| prefixed / mixed | 14.35 | 3.68 (3.9×) |
| group / small | 11.50 | 2.36 (4.9×) |
| group / mixed | 13.14 | 3.34 (3.9×) |

## What this proved beyond the numbers

- **the scope design works end-to-end**: `PushLimit` (clamped int compare in `ReadRawTag`),
  `PushGroup` (sign-overlapped sentinel), `PopScope` (one long in a caller local), and
  `IsScopeEnd` in the switch default - matched fields never touch any of it. Both encodings parse
  byte-identically to the real legacy reader;
- **the gap widened vs flat fields** (2–4× there, 4–5× here): legacy's per-record
  `StartSubItem`/`EndSubItem` token machinery is where its framing cost lives, and nano's
  replacement is two longs through locals;
- **group vs prefixed is nearly free on the nano side** (2.36 vs 2.71 small) - the sentinel
  design's promise that group framing costs only a default-case compare, kept.

## Integration note for the swap plan

The veneer row is absent, deliberately: legacy consumer code frames sub-messages via
`StartSubItem`/`EndSubItem` returning `SubItemToken`, whose constructor is **internal to Core** -
the spike cannot mint one. When nano lands inside Core this evaporates (the veneer maps
mechanically: String-wire → read length + `PushLimit`; StartGroup → `PushGroup`; `EndSubItem` →
`PopScope`, with the token being exactly the sign-discriminated long `ReadScope` already is). Until
then, veneer rows exist only for APIs the spike can express.

## Next rows

- strings (`ReadRawString` is still a stub) and bytes; skips over mixed unknown fields;
- nesting depth (child-in-child, mixed encodings);
- the full-job run before any number leaves this file.
