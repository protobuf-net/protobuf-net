# Typical results

(deserialize only)

|                        Method |                  Job |              Runtime |      Mean |     Error |    StdDev |    Median |    Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|------------------------------ |--------------------- |--------------------- |----------:|----------:|----------:|----------:|---------:|--------:|--------:|----------:|
|         DeserializeRequestGBP |             .NET 6.0 |             .NET 6.0 | 262.01 us |  4.738 us |  8.172 us | 259.50 us |  91.7969 | 39.0625 |       - | 770,800 B |
|        DeserializeResponseGBP |             .NET 6.0 |             .NET 6.0 | 184.28 us |  1.325 us |  1.473 us | 183.74 us |  61.2793 | 21.7285 |       - | 513,960 B |
|        DeserializeRequestNano |             .NET 6.0 |             .NET 6.0 | 188.31 us |  2.426 us |  2.150 us | 188.06 us |        - |       - |       - |     101 B |
|       DeserializeResponseNano |             .NET 6.0 |             .NET 6.0 | 117.61 us |  2.154 us |  1.910 us | 117.12 us |        - |       - |       - |      64 B |
|  DeserializeRequestNanoNoPool |             .NET 6.0 |             .NET 6.0 | 127.15 us |  2.418 us |  2.970 us | 125.89 us |  60.5469 | 27.3438 |       - | 508,984 B |
| DeserializeResponseNanoNoPool |             .NET 6.0 |             .NET 6.0 |  79.70 us |  1.472 us |  1.446 us |  80.03 us |  30.0293 | 10.0098 |       - | 252,096 B |
|    DeserializeRequestNanoSlab |             .NET 6.0 |             .NET 6.0 | 422.53 us | 28.035 us | 82.661 us | 451.46 us |  33.2031 | 33.2031 | 33.2031 | 401,981 B |
|   DeserializeResponseNanoSlab |             .NET 6.0 |             .NET 6.0 | 228.54 us |  4.481 us |  6.427 us | 229.56 us |  10.0098 | 10.0098 | 10.0098 | 199,455 B |
|         DeserializeRequestGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 521.86 us |  4.468 us |  4.179 us | 520.35 us | 143.5547 | 60.5469 |       - | 905,451 B |
|        DeserializeResponseGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 327.92 us |  2.382 us |  2.228 us | 327.46 us |  81.5430 | 31.7383 |       - | 515,387 B |
|        DeserializeRequestNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 494.70 us |  2.361 us |  1.972 us | 494.78 us |        - |       - |       - |     104 B |
|       DeserializeResponseNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 280.40 us |  1.438 us |  1.345 us | 280.42 us |        - |       - |       - |      64 B |
|  DeserializeRequestNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 185.17 us |  2.985 us |  2.646 us | 184.50 us |  80.8105 | 26.8555 |       - | 510,288 B |
| DeserializeResponseNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 110.48 us |  0.857 us |  0.760 us | 110.55 us |  39.9170 | 13.3057 |       - | 252,706 B |
|    DeserializeRequestNanoSlab | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 392.46 us |  7.146 us |  6.684 us | 392.04 us |  37.1094 | 37.1094 | 37.1094 | 402,441 B |
|   DeserializeResponseNanoSlab | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 225.93 us |  4.402 us |  4.117 us | 226.84 us |  20.5078 | 20.5078 | 20.5078 | 199,634 B |

(everything, earlier build)

|                        Method |                  Job |              Runtime |       Mean |     Error |    StdDev |    Gen 0 |   Gen 1 | Allocated |
|------------------------------ |--------------------- |--------------------- |-----------:|----------:|----------:|---------:|--------:|----------:|
|             MeasureRequestGBP |             .NET 6.0 |             .NET 6.0 |  50.860 us | 0.3675 us | 0.3438 us |        - |       - |         - |
|            MeasureResponseGBP |             .NET 6.0 |             .NET 6.0 |  24.241 us | 0.4807 us | 0.4497 us |        - |       - |         - |
|            MeasureRequestNano |             .NET 6.0 |             .NET 6.0 |   9.125 us | 0.0334 us | 0.0312 us |        - |       - |         - |
|           MeasureResponseNano |             .NET 6.0 |             .NET 6.0 |   7.006 us | 0.0387 us | 0.0362 us |        - |       - |         - |
|      MeasureRequestNanoNoPool |             .NET 6.0 |             .NET 6.0 |  10.409 us | 0.0871 us | 0.0815 us |        - |       - |         - |
|     MeasureResponseNanoNoPool |             .NET 6.0 |             .NET 6.0 |   7.312 us | 0.0552 us | 0.0517 us |        - |       - |         - |
|             MeasureRequestGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  47.276 us | 0.9193 us | 0.8600 us |        - |       - |         - |
|            MeasureResponseGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  23.517 us | 0.4234 us | 0.3961 us |        - |       - |         - |
|            MeasureRequestNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 |   7.939 us | 0.0427 us | 0.0378 us |        - |       - |         - |
|           MeasureResponseNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 |   6.383 us | 0.0480 us | 0.0426 us |        - |       - |         - |
|      MeasureRequestNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  17.289 us | 0.3338 us | 0.3122 us |        - |       - |         - |
|     MeasureResponseNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  12.882 us | 0.2225 us | 0.2082 us |        - |       - |         - |
|                               |                      |                      |            |           |           |          |         |           |
|           SerializeRequestGBP |             .NET 6.0 |             .NET 6.0 | 146.540 us | 1.4781 us | 1.3826 us |        - |       - |         - |
|          SerializeResponseGBP |             .NET 6.0 |             .NET 6.0 |  85.702 us | 1.4229 us | 1.2614 us |        - |       - |         - |
|          SerializeRequestNano |             .NET 6.0 |             .NET 6.0 |  81.349 us | 1.6188 us | 1.9271 us |        - |       - |         - |
|         SerializeResponseNano |             .NET 6.0 |             .NET 6.0 |  38.839 us | 0.2251 us | 0.2106 us |        - |       - |         - |
|    SerializeRequestNanoNoPool |             .NET 6.0 |             .NET 6.0 |  63.770 us | 0.7047 us | 0.6247 us |        - |       - |         - |
|   SerializeResponseNanoNoPool |             .NET 6.0 |             .NET 6.0 |  36.376 us | 0.7032 us | 0.7816 us |        - |       - |         - |
|           SerializeRequestGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 314.577 us | 1.7378 us | 1.5406 us |        - |       - |         - |
|          SerializeResponseGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 212.582 us | 1.9417 us | 1.8163 us |        - |       - |         - |
|          SerializeRequestNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 220.461 us | 1.0803 us | 1.0105 us |        - |       - |         - |
|         SerializeResponseNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 135.061 us | 0.6462 us | 0.6044 us |        - |       - |         - |
|    SerializeRequestNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 247.045 us | 1.3053 us | 1.2210 us |        - |       - |         - |
|   SerializeResponseNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 149.158 us | 0.7334 us | 0.6501 us |        - |       - |         - |
|                               |                      |                      |            |           |           |          |         |           |
|         DeserializeRequestGBP |             .NET 6.0 |             .NET 6.0 | 258.004 us | 2.8702 us | 2.6848 us |  91.7969 | 39.0625 | 770,800 B |
|        DeserializeResponseGBP |             .NET 6.0 |             .NET 6.0 | 185.871 us | 2.0612 us | 1.9280 us |  61.2793 | 21.7285 | 513,960 B |
|        DeserializeRequestNano |             .NET 6.0 |             .NET 6.0 | 196.716 us | 3.2016 us | 2.9947 us |        - |       - |     101 B |
|       DeserializeResponseNano |             .NET 6.0 |             .NET 6.0 | 122.160 us | 1.6066 us | 1.4242 us |        - |       - |      64 B |
|  DeserializeRequestNanoNoPool |             .NET 6.0 |             .NET 6.0 | 128.820 us | 1.8065 us | 1.6014 us |  60.7910 | 26.8555 | 508,984 B |
| DeserializeResponseNanoNoPool |             .NET 6.0 |             .NET 6.0 |  78.814 us | 0.7522 us | 0.6668 us |  30.0293 | 10.0098 | 252,096 B |
|         DeserializeRequestGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 527.291 us | 1.6554 us | 1.4675 us | 143.5547 | 60.5469 | 905,448 B |
|        DeserializeResponseGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 311.179 us | 1.5358 us | 1.3614 us |  81.5430 | 31.7383 | 515,387 B |
|        DeserializeRequestNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 525.308 us | 4.3354 us | 3.8432 us |        - |       - |     104 B |
|       DeserializeResponseNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 286.937 us | 1.6254 us | 1.4408 us |        - |       - |      64 B |
|  DeserializeRequestNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 174.761 us | 1.5208 us | 1.3481 us |  80.8105 | 26.8555 | 510,287 B |
| DeserializeResponseNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 101.514 us | 0.9897 us | 0.8265 us |  39.9170 | 13.3057 | 252,706 B |