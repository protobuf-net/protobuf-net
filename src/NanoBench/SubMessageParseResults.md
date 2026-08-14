# Sub-messages, both terminations: repeated "(child: field 1 varint)" with merge semantics

**PRELIMINARY** — ShortRun, one machine. 64K records, deliberate merge (one child instance reused
throughout - a repeated message field merges - so the loop measures parsing, not allocation).
Correctness gate passed: both parsers agree on (count, sum, last) against expected, both encodings.

```
BenchmarkDotNet v0.15.8, Windows 11, AMD Ryzen 9 7900X (Zen4), .NET 10.0.10, x86-64-v4
```

| ns/record | LegacyReal | NanoViaLegacyApi | NanoRaw |
| --- | ---: | ---: | ---: |
| prefixed / small | 12.82 | 3.82 (3.4×) | 2.90 (4.4×) |
| prefixed / mixed | 14.13 | 5.27 (2.7×) | 3.78 (3.7×) |
| group / small | 11.61 | 3.70 (3.1×) | 2.45 (4.7×) |
| group / mixed | 13.00 | 5.30 (2.5×) | 3.43 (3.8×) |

The veneer row's consumer code is character-for-character identical to LegacyReal's - including
`StartSubItem`/`EndSubItem` - so (a) → (b) is the purest internals-isolation this design allows:
2.5–3.4× from the swap alone, with the raw surface adding a further ~1.3–1.5× on top.

(Re-measured after correcting the child reader to the exact emitted shape - value-in/value-out with
`??=` construction and assign-back at the call site, rather than a void mutate-in-place that
drifted from what the generator emits; the correction cost ~0.1 ns/record. The return-vs-`ref`
design question this surfaced is settled in notes/nano-core.md: return is canonical - `ref` binds
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

The veneer row initially sat out - `SubItemToken`'s constructor is internal to Core - and was
unblocked by an **IVT grant** (`InternalsVisibleTo("NanoState")` in Core's AssemblyInfo), the
interim bridge until nano moves into Core. The discipline recorded with the grant: IVT serves the
*veneers* reaching legacy internals, never the new raw surface, or the eventual move stops being
mechanical. The satisfying confirmation en route: `SubItemToken` is *literally* the same
sign-discriminated long that `ReadScope` is (its own ToString reads "Group -value64" for
negatives), so the veneer is a cast in each direction - String-wire → `PushLengthPrefix`,
StartGroup → `PushGroup`, `EndSubItem` → `PopScope`.

## Next rows

- strings (`ReadRawString` is still a stub) and bytes; skips over mixed unknown fields;
- nesting depth (child-in-child, mixed encodings);
- the full-job run before any number leaves this file.
