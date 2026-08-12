# The north-star composite: descriptor.proto, three stacks, real data

**Full-job run** (default BDN iteration counts, StdDev ≤1% on every row), one machine. Gate
green: the structural census (entity counts, every
string's chars, field-number/path sums, presence-guarded enum sums) agrees across all three
object models. Payload: **7,670 bytes** — descriptor.proto's own descriptor, generated in setup
by protobuf-net.Reflection parsing its embedded descriptor.proto, so it cannot drift.
Census: 1 file, 27 messages, 126 fields, 6 enums, 33 enum values, 5 options objects, 5,475
string chars. (No SourceCodeInfo in this payload, so the packed-int path is present in the
parser but dormant here.)

Each stack parses into **its own natural object model**: legacy = RuntimeTypeModel over
protobuf-net.Reflection's protogen DTOs (the real shipped stack), Google.Protobuf 3.34 = its
generated parser over its generated DTOs (home turf), nano = the raw surface over the
hand-written emitted-shape model in `DescriptorNano.cs`. That is deliberately a product
comparison, not a machinery isolation — the per-primitive isolations live in the sibling
results files.

```
BenchmarkDotNet v0.15.8, Windows 11, AMD Ryzen 9 7900X (Zen4), x86-64-v4
.NET 10.0.10 vs .NET Framework 4.7.2 (4.8.1 runtime)
```

| whole-document parse | Legacy (real stack) | Google.Protobuf | NanoRaw |
| --- | ---: | ---: | ---: |
| net10 | 25.00 µs / 62.3 KB | 12.57 µs / 53.1 KB | **8.32 µs / 51.9 KB** |
| net472 | 44.56 µs / 69.2 KB | 28.10 µs / 71.0 KB | **14.89 µs / 55.8 KB** |

## What the table says

1. **Nano beats Google.Protobuf on its home-turf format: 1.51× (net10), 1.89× (net472)** — and
   legacy by 3.0× on both runtimes. The composite landed where the per-primitive calibration
   predicted (between the framing rows' 3–5× and the string rows' ~1.1–1.3×, weighted by
   descriptor.proto's message-heavy, string-heavy mix).
2. **Lowest allocations of the three** (0.83× legacy on net10), with the caveat that the object
   models differ by design: the nano DTO has no unknown-field bags (legacy's IExtensible) and no
   presence bitfields (Google's hasBits) — nullable scalars carry presence instead.
3. **Nano on net472 (14.7 µs) nearly matches Google on net10 (12.2 µs)** and comfortably beats
   legacy on net10 (22.8 µs): the down-level story keeps holding at document scale.
4. The parse exercises, on real data: strings everywhere, repeated messages as run loops,
   repeated strings, enums/bools/int32 as raw varint reads, genuinely recursive nesting
   (DescriptorProto containing itself, depth checks live), and the options subtree. Dormant in
   this payload: packed ints (`loc=0`), bytes, `UninterpretedOption` — the parser covers them,
   the data does not reach them.

## The review pass (2026-08-12): every change priced

The first human read (Marc) produced five changes, applied and measured in two steps against the
pre-review baseline (8.34 µs net10 / 14.68 µs net472, full-job):

| step | change | net10 | net472 |
| --- | --- | ---: | ---: |
| A | framing pairs (`PushScope(tag)`, length-or-group on every message field), auto-properties for all DTO scalars, real enums | 8.19 µs (short) | 14.71 µs (short) |
| B | +86 wire-type tolerance labels (varint/fixed32/fixed64 on every scalar; 160 → 246 labels) | 8.35 µs (short) | 14.85 µs (short) |
| final | full-job confirmation of the reviewed shape | **8.32 µs** | **14.89 µs** |

Readings:

- **Everything measured free on net10** — the whole review pass nets −0.02 µs against baseline.
  The sparse-switch concern (`FileOptions` reaches field 999, so these are binary-search trees,
  not jump tables) did not materialize: unhit labels stay unpriced even there. net472 shows
  +0.21 µs (+1.4%) for the full pass — real but marginal on the old JIT.
- **Consequence: tolerant is simply the default; no strict-mode knob is built.** The strict-mode
  design (a model attribute, natural wire type only) stays recorded in docs/nano-core.md with no
  trigger. Packed↔unpacked and length↔group pairs are spec, not tolerance, and would survive
  strict mode anyway.
- The one tolerance exception: `double` stays fixed64-only — the fixed32 float promotion needs
  `Int32BitsToSingle`, which netfx lacks.
- Also in the pass: every case label now carries its `// name, field N, wire` comment
  (unconditionally — comments are free in every configuration), and the model rewrite retired
  both of the file's earlier abbreviation caveats.

## Milestone gate (docs/nano-core.md)

Both halves of the gate are now done: the full-job runs above, and the human top-to-bottom read
of `DescriptorNano.cs` — whose feedback also produced the forward-only rule for the reader (see
docs/nano-core.md) and the deferred-construction design note for the inheritance brick.

## The generator closes the loop (post-review)

The nano pass now emits everything `DescriptorNano.cs` hand-writes — sub-messages as framing
pairs over `PushScope` with direct static calls, tag-local run loops, the packed `List<int>`
helper with tolerance siblings, enums, nullable scalars, double/bytes, per-label comments, and
an eligibility FIXPOINT (a skipped target cascades to its referrers, and every skip says why in
the emitted output itself). The proof is structural: BuildTools runs as a real analyzer inside
NanoBench over the attributed DTOs, and the **generator-emitted reader is census-gated against
the hand-written one on the real payload** — which turns `DescriptorNano.cs` from "the
implementation" into what it was written to be: the reviewed specification the generator is held
to at document scale, with the goldens holding it at snippet scale.

Measured (ShortRun): NanoGenerated 8.65 µs vs NanoRaw 8.51 (net10, within error), 15.59 vs
15.30 (net472), allocations byte-identical. The machine writes what the hand wrote, at the same
speed.

## Re-verification on the swapped tree (2026-08-14, net10, full job)

| Method         | Mean      | Ratio | Allocated |
| -------------- | --------: | ----: | --------: |
| LegacyReal     | 17.755 us |  1.00 |  62.27 KB |
| GoogleProtobuf | 12.309 us |  0.69 |  53.14 KB |
| NanoRaw        |  9.266 us |  0.52 |  51.91 KB |
| NanoGenerated  |  9.122 us |  0.51 |  51.91 KB |

Three readings, in order of importance:

1. **The generated readers are at parity with the hand-written ones** (9.12 vs 9.27 us) -
   the goal of the whole generator arc. Both remain ~26% faster than Google.Protobuf.
2. **The "legacy" classic path got ~29% faster by riding the swapped core** (25.0 -> 17.8 us
   against the pre-swap record): the classic emit shapes now run over the raw core's veneers,
   so even un-regenerated consumers benefit from the swap.
3. NanoRaw drifted 8.32 -> 9.27 us against the spike record. Same machine, but a different
   session/thermal state and the raw surface has since grown the forward-only pending slot,
   the IsScopeEnd legacy fallback and scope bookkeeping - per the tiebreaker rule this is a
   measure-before-concluding item, not a finding; if it holds under a controlled A/B it is
   the price of the compatibility seams and worth attributing properly.

Native AOT re-verified the same day: AotSmoke publish green (round-trip PASSED as ILC
native code, the full raw read stack under it), trim/AOT warnings 19 = the recorded
baseline count (membership drift inside the structural residue: DeserializeRootFallback
and KeyValuePairSerializer.CreateDefault appear; both runtime-model fallback paths).
Binary 3.65 MB - a NEW baseline datum (fixture set and ILC version both moved since the
2.7 MB-era records; do not compare across them).
