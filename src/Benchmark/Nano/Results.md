# Typical results

(deserialize only)

|                        Method |                  Job |              Runtime |      Mean |     Error |    StdDev |    Median |    Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|------------------------------ |--------------------- |--------------------- |----------:|----------:|----------:|----------:|---------:|--------:|--------:|----------:|
|         DeserializeRequestGBP |             .NET 6.0 |             .NET 6.0 | 275.46 us |  5.263 us | 10.869 us | 275.43 us |  91.7969 | 39.0625 |       - | 770,800 B |
|        DeserializeRequestNano |             .NET 6.0 |             .NET 6.0 | 173.84 us |  2.553 us |  2.388 us | 173.81 us |        - |       - |       - |     101 B |
|  DeserializeRequestNanoNoPool |             .NET 6.0 |             .NET 6.0 | 122.70 us |  1.226 us |  1.024 us | 122.26 us |  60.6689 | 27.4658 |       - | 508,984 B |
|    DeserializeRequestNanoSlab |             .NET 6.0 |             .NET 6.0 | 406.75 us | 29.150 us | 85.950 us | 445.55 us |  36.6211 | 36.6211 | 36.6211 | 402,005 B |
|         DeserializeRequestGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 529.83 us |  5.707 us |  5.338 us | 528.02 us | 143.5547 | 60.5469 |       - | 905,451 B |
|        DeserializeRequestNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 427.54 us |  2.430 us |  2.154 us | 427.52 us |        - |       - |       - |     104 B |
|  DeserializeRequestNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 165.13 us |  1.954 us |  1.632 us | 165.87 us |  80.8105 | 26.8555 |       - | 510,289 B |
|    DeserializeRequestNanoSlab | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 373.16 us |  7.418 us | 14.469 us | 371.98 us |  37.5977 | 37.5977 | 37.5977 | 402,184 B |
|                               |                      |                      |           |           |           |           |          |         |         |           |
|        DeserializeResponseGBP |             .NET 6.0 |             .NET 6.0 | 195.93 us |  3.818 us |  3.750 us | 195.74 us |  61.2793 | 21.7285 |       - | 513,960 B |
|       DeserializeResponseNano |             .NET 6.0 |             .NET 6.0 | 112.71 us |  1.179 us |  1.045 us | 112.25 us |        - |       - |       - |      64 B |
| DeserializeResponseNanoNoPool |             .NET 6.0 |             .NET 6.0 |  78.00 us |  1.328 us |  1.109 us |  78.09 us |  30.0293 | 10.0098 |       - | 252,096 B |
|   DeserializeResponseNanoSlab |             .NET 6.0 |             .NET 6.0 | 231.52 us |  4.475 us |  6.418 us | 231.40 us |  10.7422 | 10.7422 | 10.7422 | 199,469 B |
|        DeserializeResponseGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 320.39 us |  3.307 us |  3.094 us | 320.92 us |  81.5430 | 31.7383 |       - | 515,387 B |
|       DeserializeResponseNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 253.57 us |  1.856 us |  1.736 us | 254.05 us |        - |       - |       - |      64 B |
| DeserializeResponseNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  96.08 us |  0.752 us |  0.587 us |  96.02 us |  39.9170 | 13.3057 |       - | 252,706 B |
|   DeserializeResponseNanoSlab | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 220.88 us |  4.174 us |  6.621 us | 220.01 us |  15.1367 | 15.1367 | 15.1367 | 199,594 B |

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