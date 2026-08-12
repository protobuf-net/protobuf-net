# Sub-messages, both terminations: repeated "(child: field 1 varint)" with merge semantics

**PRELIMINARY** — ShortRun, one machine. 64K records, deliberate merge (one child instance reused
throughout - a repeated message field merges - so the loop measures parsing, not allocation).
Correctness gate passed: both parsers agree on (count, sum, last) against expected, both encodings.

```
BenchmarkDotNet v0.15.8, Windows 11, AMD Ryzen 9 7900X (Zen4), .NET 10.0.10, x86-64-v4
```

| ns/record | LegacyReal | NanoRaw |
| --- | ---: | ---: |
| prefixed / small | 12.98 | 2.86 (4.5×) |
| prefixed / mixed | 14.20 | 3.80 (3.7×) |
| group / small | 11.52 | 2.46 (4.7×) |
| group / mixed | 13.11 | 3.45 (3.8×) |

(Re-measured after correcting the child reader to the exact emitted shape - value-in/value-out with
`??=` construction and assign-back at the call site, rather than a void mutate-in-place that
drifted from what the generator emits; the correction cost ~0.1 ns/record. The return-vs-`ref`
design question this surfaced is settled in docs/nano-core.md: return is canonical - `ref` binds
only to fields/locals/elements, never properties, and return matches the interface veneer - with a
`ref` overload as a targeted future specialization for structs, accessor fields and in-place
collection merge.)

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
