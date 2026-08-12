# The north-star composite: descriptor.proto, three stacks, real data

**PRELIMINARY** — ShortRun, one machine. Gate green: the structural census (entity counts, every
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
| net10 | 26.85 µs / 62.3 KB | 12.32 µs / 53.1 KB | **8.09 µs / 51.9 KB** |
| net472 | 48.13 µs / 69.2 KB | 28.11 µs / 71.0 KB | **14.60 µs / 55.8 KB** |

## What the table says

1. **Nano beats Google.Protobuf on its home-turf format: 1.52× (net10), 1.93× (net472)** — and
   legacy by 3.3× on both runtimes. The composite landed almost exactly where the per-primitive
   calibration predicted (between the framing rows' 3–5× and the string rows' ~1.1–1.3×,
   weighted by descriptor.proto's message-heavy, string-heavy mix).
2. **Lowest allocations of the three** (0.83× legacy on net10), with the caveat that the object
   models differ by design: the nano DTO has no unknown-field bags (legacy's IExtensible) and no
   presence bitfields (Google's hasBits) — nullable scalars carry presence instead.
3. **Nano on net472 (14.6 µs) nearly matches Google on net10 (12.3 µs)** and comfortably beats
   legacy on net10 (26.9 µs): the down-level story keeps holding at document scale.
4. The parse exercises, on real data: strings everywhere, repeated messages as run loops,
   repeated strings, enums/bools/int32 as raw varint reads, genuinely recursive nesting
   (DescriptorProto containing itself, depth checks live), and the options subtree. Dormant in
   this payload: packed ints (`loc=0`), bytes, `UninterpretedOption` — the parser covers them,
   the data does not reach them.

## Milestone gate (docs/nano-core.md)

Remaining before the milestone closes: a full-job (non-ShortRun) run of this benchmark, and the
human top-to-bottom read of `DescriptorNano.cs` — which was written to be read: it is the
emitted-shape reference for the generator's nano pass at document scale.
