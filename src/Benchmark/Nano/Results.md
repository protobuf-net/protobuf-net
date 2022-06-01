# Typical results

"Nano" is the working title of an experimental protobuf-net v4 reader/writer API; the internal details are completely re-imagined and *aggressively* optimized using BenchmarkDotNet. The regular
consumer-facing API should not be impacted, since most users *never touch* the reader/writer API. There are absolutely no guarantees that this API will ever ship, but: it is looking very worthwhile.
It is *incredibly* incomplete and is not fit for any real world use yet! Please don't attempt to use it: it absolutely will not work for you yet.

Key:

- GBP === Google.Protobuf (generated types "as-is")
- PBN === protobuf-net v3 (code-first simple types - typical usage, nothing custom)
- Nano === new experimental protobuf-net v4 API
- NanoNoPool === alternative use-case of Nano, with minimal "pool mode" usage

(the payloads are obviously identical between related tests, and outputs are fully validated for correctness, etc)

If you're wondering what "pool mode" is: this scenario highlit that some scenarios involve a *lot* of leaf-level `bytes` payloads; GBP and PBN currently treat this as a `byte[]` per
usage, which can yield very high allocations; "pool mode" uses pooled buffers for these allocations, and requires that the consumer explicitly tells the API when the memory can be recycled
(using `IDisposable`); if the consumer fails to do this, the buffers will also follow normal GC rules, but: some care is required.

In this scenario "pool mode" reduces allocations to almost zero (very useful if memory/GC is a known bottleneck), but note that the additional buffer management means that in raw CPU throughput,
it may still be preferable to use NanoNoPool; I hope to offer both as options. Both Nano implementations are considerably faster than GBP, with lower allocations. PBN (v3) is ...
almost embarrassing on this table. I guess I've failed to keep up with the state of the art. But maybe not for long! There are also some other ideas rattling around in my head for related
scenarios (in particular to improve the serialization of deep models, which are disproportioately expensive with vanilla protobuf)

---

|                        Method |                  Job |              Runtime |       Mean |      Error |     StdDev |    Gen 0 |   Gen 1 | Allocated |
|------------------------------ |--------------------- |--------------------- |-----------:|-----------:|-----------:|---------:|--------:|----------:|
|             MeasureRequestGBP |             .NET 6.0 |             .NET 6.0 |  50.895 us |  0.6979 us |  0.6528 us |        - |       - |         - |
|            MeasureRequestNano |             .NET 6.0 |             .NET 6.0 |   9.319 us |  0.1258 us |  0.1177 us |        - |       - |         - |
|      MeasureRequestNanoNoPool |             .NET 6.0 |             .NET 6.0 |  10.926 us |  0.1254 us |  0.1047 us |        - |       - |         - |
|             MeasureRequestPBN |             .NET 6.0 |             .NET 6.0 | 111.809 us |  2.2272 us |  3.0486 us |        - |       - |         - |
|                               |                      |                      |            |            |            |          |         |           |
|            MeasureResponseGBP |             .NET 6.0 |             .NET 6.0 |  24.509 us |  0.2023 us |  0.1689 us |        - |       - |         - |
|           MeasureResponseNano |             .NET 6.0 |             .NET 6.0 |   7.480 us |  0.1450 us |  0.1834 us |        - |       - |         - |
|     MeasureResponseNanoNoPool |             .NET 6.0 |             .NET 6.0 |   7.793 us |  0.0645 us |  0.0572 us |        - |       - |         - |
|            MeasureResponsePBN |             .NET 6.0 |             .NET 6.0 |  96.487 us |  1.1576 us |  0.9038 us |        - |       - |         - |
|                               |                      |                      |            |            |            |          |         |           |
|           SerializeRequestGBP |             .NET 6.0 |             .NET 6.0 | 151.183 us |  2.9636 us |  5.7099 us |        - |       - |         - |
|          SerializeRequestNano |             .NET 6.0 |             .NET 6.0 |  84.329 us |  1.6760 us |  2.2941 us |        - |       - |         - |
|    SerializeRequestNanoNoPool |             .NET 6.0 |             .NET 6.0 |  63.205 us |  0.3531 us |  0.3130 us |        - |       - |         - |
|           SerializeRequestPBN |             .NET 6.0 |             .NET 6.0 | 257.734 us |  1.9678 us |  1.8407 us |        - |       - |       1 B |
|                               |                      |                      |            |            |            |          |         |           |
|          SerializeResponseGBP |             .NET 6.0 |             .NET 6.0 |  86.499 us |  0.4763 us |  0.4222 us |        - |       - |         - |
|         SerializeResponseNano |             .NET 6.0 |             .NET 6.0 |  39.455 us |  0.7836 us |  0.9328 us |        - |       - |         - |
|   SerializeResponseNanoNoPool |             .NET 6.0 |             .NET 6.0 |  37.678 us |  0.2242 us |  0.1872 us |        - |       - |         - |
|          SerializeResponsePBN |             .NET 6.0 |             .NET 6.0 | 205.672 us |  0.9280 us |  0.9114 us |        - |       - |         - |
|                               |                      |                      |            |            |            |          |         |           |
|         DeserializeRequestGBP |             .NET 6.0 |             .NET 6.0 | 315.088 us |  4.1709 us |  3.9015 us |  91.7969 | 39.0625 | 770,801 B |
|        DeserializeRequestNano |             .NET 6.0 |             .NET 6.0 | 176.539 us |  3.0904 us |  3.7953 us |        - |       - |     101 B |
|  DeserializeRequestNanoNoPool |             .NET 6.0 |             .NET 6.0 | 127.660 us |  0.9104 us |  0.8071 us |  60.5469 | 27.3438 | 508,984 B |
|         DeserializeRequestPBN |             .NET 6.0 |             .NET 6.0 | 349.117 us |  6.9347 us | 11.1982 us |  61.5234 | 24.4141 | 517,017 B |
|                               |                      |                      |            |            |            |          |         |           |
|        DeserializeResponseGBP |             .NET 6.0 |             .NET 6.0 | 198.088 us |  3.7659 us |  3.1447 us |  61.2793 | 21.7285 | 513,960 B |
|       DeserializeResponseNano |             .NET 6.0 |             .NET 6.0 | 109.753 us |  1.0583 us |  0.8837 us |        - |       - |      64 B |
| DeserializeResponseNanoNoPool |             .NET 6.0 |             .NET 6.0 |  78.541 us |  1.1933 us |  1.0578 us |  30.0293 | 10.0098 | 252,096 B |
|        DeserializeResponsePBN |             .NET 6.0 |             .NET 6.0 | 226.393 us |  4.4686 us |  8.9243 us |  31.0059 | 10.2539 | 260,128 B |
|                               |                      |                      |            |            |            |          |         |           |
|             MeasureRequestGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  48.221 us |  0.5436 us |  0.4244 us |        - |       - |         - |
|            MeasureRequestNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 |   8.309 us |  0.1626 us |  0.1521 us |        - |       - |         - |
|      MeasureRequestNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  20.093 us |  0.3977 us |  0.9058 us |        - |       - |         - |
|             MeasureRequestPBN | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 175.283 us |  1.9264 us |  1.8019 us |        - |       - |         - |
|                               |                      |                      |            |            |            |          |         |           |
|            MeasureResponseGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  23.243 us |  0.2664 us |  0.2362 us |        - |       - |         - |
|           MeasureResponseNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 |   7.390 us |  0.0981 us |  0.0819 us |        - |       - |         - |
|     MeasureResponseNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  12.980 us |  0.0975 us |  0.0814 us |        - |       - |         - |
|            MeasureResponsePBN | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 157.080 us |  1.9521 us |  1.6301 us |        - |       - |         - |
|                               |                      |                      |            |            |            |          |         |           |
|           SerializeRequestGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 318.822 us |  3.4894 us |  3.0932 us |        - |       - |         - |
|          SerializeRequestNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 229.121 us |  3.1362 us |  2.6189 us |        - |       - |         - |
|    SerializeRequestNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 255.074 us |  3.6162 us |  3.2057 us |        - |       - |         - |
|           SerializeRequestPBN | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 537.404 us |  3.5983 us |  2.8093 us |        - |       - |         - |
|                               |                      |                      |            |            |            |          |         |           |
|          SerializeResponseGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 219.558 us |  2.8499 us |  2.6658 us |        - |       - |         - |
|         SerializeResponseNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 138.503 us |  2.6998 us |  2.7725 us |        - |       - |         - |
|   SerializeResponseNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 157.571 us |  2.7458 us |  2.4341 us |        - |       - |         - |
|          SerializeResponsePBN | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 446.688 us |  5.6884 us |  5.0427 us |        - |       - |         - |
|                               |                      |                      |            |            |            |          |         |           |
|         DeserializeRequestGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 547.854 us | 10.7472 us | 12.7938 us | 143.5547 | 60.5469 | 905,448 B |
|        DeserializeRequestNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 444.395 us |  6.1638 us |  5.4641 us |        - |       - |     104 B |
|  DeserializeRequestNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 180.206 us |  3.1968 us |  6.0044 us |  80.8105 | 26.8555 | 510,289 B |
|         DeserializeRequestPBN | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 742.822 us |  4.3232 us |  4.0439 us |  89.8438 | 44.9219 | 574,371 B |
|                               |                      |                      |            |            |            |          |         |           |
|        DeserializeResponseGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 343.011 us |  6.8412 us | 10.4472 us |  81.5430 | 31.7383 | 515,387 B |
|       DeserializeResponseNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 270.225 us |  5.3343 us |  6.7462 us |        - |       - |      64 B |
| DeserializeResponseNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  98.141 us |  1.4152 us |  1.3238 us |  39.9170 | 13.3057 | 252,706 B |
|        DeserializeResponsePBN | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 560.650 us |  8.2128 us |  7.6822 us |  48.8281 | 16.6016 | 316,788 B |
