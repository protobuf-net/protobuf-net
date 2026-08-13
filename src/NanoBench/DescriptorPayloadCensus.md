# Descriptor payload census (docs/nano-writer.md)

**A one-off, and the answer to "how are the weights biased".** Every byte of a realistic
protobuf payload classified by role, and every varint tallied by encoded width - so that
"is this primitive worth optimising" can be answered by counting rather than by guessing.

Payload: the `FileDescriptorSet` for `google/protobuf/descriptor.proto`, the same shape the
serialize benchmark uses (7,698 bytes here against the benchmark's 7,670 - the difference is
source path strings, not structure).

Reproduce:

```
dotnet run --project src/protogen -f net8.0 -- -o./descset.bin     src/protobuf-net.Reflection/google/protobuf/descriptor.proto
python src/NanoBench/census.py descset.bin
```

The walker is schema-free, so a length-delimited payload that is both printable ASCII **and**
parses as a message is ambiguous; it is read as a string, which is right for a descriptor set.
The count of such cases is reported at the bottom, and the classification is asserted to account
for every byte exactly.

---

# Payload byte census (7698 bytes)

| role | bytes | share |
| --- | ---: | ---: |
| string/bytes payload | 5503 | 71.5% |
| tag | 1062 | 13.8% |
| length prefix | 628 | 8.2% |
| varint value | 505 | 6.6% |
| **total** | **7698** | 100% |

## Varints, by encoded width

| role | width | count | bytes |
| --- | ---: | ---: | ---: |
| length prefix | 1 | 588 | 588 |
| length prefix | 2 | 20 | 40 |
| tag | 1 | 1056 | 1056 |
| tag | 2 | 3 | 6 |
| varint value | 1 | 424 | 424 |
| varint value | 2 | 18 | 36 |
| varint value | 5 | 9 | 45 |

## Length prefixes (608 of them)

| magnitude | count | share |
| --- | ---: | ---: |
| 1..127 | 588 | 96.7% |
| 128..16383 | 20 | 3.3% |

## Tags written: 1059

| tag width | count | share |
| --- | ---: | ---: |
| 1 byte (fields 1-15) | 1056 | 99.7% |
| 2 bytes (fields 16-2047) | 3 | 0.3% |

## Varint VALUES that are 0 or 1 (the bool ceiling)

| value | count | share of all varint values |
| --- | ---: | ---: |
| 0 | 3 | 0.7% |
| 1 | 122 | 27.1% |

(ambiguous length-delimited payloads - printable AND parseable as a message, read as strings: 2)


---

## What this says

- **99.7% of tags written are one byte.** Three of 1,059 are two-byte. The generated descriptor
  model has *27 two-byte call sites* - but they are overwhelmingly `uninterpreted_option`
  (field 999), which is absent from real data. Static call-site population and dynamic op
  population are not the same distribution, and it is the second one that pays.
- **The one-byte tag arm has been a single store since cut 9**, so the pre-encoded ladder for
  the wider tags moves 3 ops out of roughly 2,700. That is a *ceiling* argument for its flat
  measurement, not a noise argument - which is a much better thing to have.
- **Every length prefix is one or two bytes**, 96.7% of them one. So the length-prefix varint
  write is also almost always a single store, and the varint measure strategies raced in
  `VarintMeasureResults.md` are being asked a question whose answer is nearly always "1".
- **Bools are an upper bound of 122 fields** (varint values equal to 1; a false bool is not
  written at all, so every bool on the wire is a 1). Folding tag+bool into one store therefore
  removes at most 122 room-checks-and-stores from ~2,700 ops.
- **71.5% of the payload is string/bytes payload**, i.e. `UTF8.GetBytes` - a memcpy-class
  operation over 5.5 KB. Against that, the entire tag budget is 1,062 bytes and the entire
  varint-value budget is 505.

**So the standing conclusion in `docs/nano-writer.md` now has a measurement behind it rather
than an inference**: per-op cost in the varint/tag primitives is not where this workload's time
goes, because those ops are already at their floor - one store - for 99.7% of tags and 96.7% of
length prefixes, and the mass is in string encoding and structure traversal.

The caveat recorded there still stands and is now sharper: **packed repeated writes measure and
write per ELEMENT**, so a packed-heavy payload would have a completely different census - far
more varint values, far fewer tags and length prefixes. This is one workload; a payload built
around extensions (high field numbers) or packed columns would re-open the question. Re-run the
census on it rather than assuming either way.
