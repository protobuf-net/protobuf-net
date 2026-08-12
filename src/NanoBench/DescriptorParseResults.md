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
