# Typical results

|                        Method |                  Job |              Runtime |       Mean |      Error |     StdDev |     Median |    Gen 0 |   Gen 1 |   Gen 2 | Allocated |
|------------------------------ |--------------------- |--------------------- |-----------:|-----------:|-----------:|-----------:|---------:|--------:|--------:|----------:|
|             MeasureRequestGBP |             .NET 6.0 |             .NET 6.0 |  51.946 us |  0.3024 us |  0.2681 us |  51.947 us |        - |       - |       - |         - |
|            MeasureRequestNano |             .NET 6.0 |             .NET 6.0 |   9.251 us |  0.0387 us |  0.0343 us |   9.233 us |        - |       - |       - |         - |
|      MeasureRequestNanoNoPool |             .NET 6.0 |             .NET 6.0 |  10.749 us |  0.0704 us |  0.0624 us |  10.736 us |        - |       - |       - |         - |
|        MeasureRequestNanoSlab |             .NET 6.0 |             .NET 6.0 |   9.303 us |  0.0241 us |  0.0188 us |   9.300 us |        - |       - |       - |         - |
|                               |                      |                      |            |            |            |            |          |         |         |           |
|            MeasureResponseGBP |             .NET 6.0 |             .NET 6.0 |  24.009 us |  0.4217 us |  0.3945 us |  23.970 us |        - |       - |       - |         - |
|           MeasureResponseNano |             .NET 6.0 |             .NET 6.0 |   7.186 us |  0.0501 us |  0.0444 us |   7.167 us |        - |       - |       - |         - |
|     MeasureResponseNanoNoPool |             .NET 6.0 |             .NET 6.0 |   7.481 us |  0.0210 us |  0.0186 us |   7.479 us |        - |       - |       - |         - |
|       MeasureResponseNanoSlab |             .NET 6.0 |             .NET 6.0 |   7.180 us |  0.0470 us |  0.0417 us |   7.176 us |        - |       - |       - |         - |
|                               |                      |                      |            |            |            |            |          |         |         |           |
|           SerializeRequestGBP |             .NET 6.0 |             .NET 6.0 | 145.794 us |  2.3325 us |  1.9477 us | 144.850 us |        - |       - |       - |         - |
|          SerializeRequestNano |             .NET 6.0 |             .NET 6.0 |  81.631 us |  1.5517 us |  1.5240 us |  81.096 us |        - |       - |       - |         - |
|    SerializeRequestNanoNoPool |             .NET 6.0 |             .NET 6.0 |  63.954 us |  0.6078 us |  0.5075 us |  64.025 us |        - |       - |       - |         - |
|      SerializeRequestNanoSlab |             .NET 6.0 |             .NET 6.0 |  71.465 us |  0.7119 us |  0.6311 us |  71.471 us |        - |       - |       - |         - |
|                               |                      |                      |            |            |            |            |          |         |         |           |
|          SerializeResponseGBP |             .NET 6.0 |             .NET 6.0 |  86.608 us |  1.7013 us |  1.7471 us |  85.594 us |        - |       - |       - |         - |
|         SerializeResponseNano |             .NET 6.0 |             .NET 6.0 |  39.379 us |  0.5536 us |  0.5178 us |  39.149 us |        - |       - |       - |         - |
|   SerializeResponseNanoNoPool |             .NET 6.0 |             .NET 6.0 |  36.341 us |  0.1409 us |  0.1100 us |  36.312 us |        - |       - |       - |         - |
|     SerializeResponseNanoSlab |             .NET 6.0 |             .NET 6.0 |  37.266 us |  0.6589 us |  0.7324 us |  37.271 us |        - |       - |       - |         - |
|                               |                      |                      |            |            |            |            |          |         |         |           |
|         DeserializeRequestGBP |             .NET 6.0 |             .NET 6.0 | 282.495 us |  5.5114 us |  6.3469 us | 282.009 us |  91.7969 | 39.0625 |       - | 770,800 B |
|        DeserializeRequestNano |             .NET 6.0 |             .NET 6.0 | 171.949 us |  1.8431 us |  1.7241 us | 171.186 us |        - |       - |       - |     101 B |
|  DeserializeRequestNanoNoPool |             .NET 6.0 |             .NET 6.0 | 133.144 us |  2.5078 us |  2.3458 us | 133.289 us |  60.6689 | 27.4658 |       - | 508,984 B |
|    DeserializeRequestNanoSlab |             .NET 6.0 |             .NET 6.0 | 324.004 us | 27.3029 us | 80.5032 us | 359.708 us |  28.3203 | 28.3203 | 28.3203 | 271,019 B |
|                               |                      |                      |            |            |            |            |          |         |         |           |
|        DeserializeResponseGBP |             .NET 6.0 |             .NET 6.0 | 206.401 us |  4.0346 us |  4.6463 us | 205.933 us |  61.2793 | 21.7285 |       - | 513,960 B |
|       DeserializeResponseNano |             .NET 6.0 |             .NET 6.0 | 109.305 us |  1.6402 us |  1.5342 us | 108.968 us |        - |       - |       - |      64 B |
| DeserializeResponseNanoNoPool |             .NET 6.0 |             .NET 6.0 |  82.017 us |  1.1615 us |  1.0296 us |  81.728 us |  30.0293 | 10.0098 |       - | 252,096 B |
|   DeserializeResponseNanoSlab |             .NET 6.0 |             .NET 6.0 | 126.729 us |  2.7272 us |  7.9985 us | 126.547 us |  15.0146 | 15.0146 | 15.0146 | 112,176 B |
|                               |                      |                      |            |            |            |            |          |         |         |           |
|             MeasureRequestGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  47.214 us |  0.3166 us |  0.2644 us |  47.126 us |        - |       - |       - |         - |
|            MeasureRequestNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 |   8.768 us |  0.0527 us |  0.0467 us |   8.768 us |        - |       - |       - |         - |
|      MeasureRequestNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  17.391 us |  0.3229 us |  0.3021 us |  17.254 us |        - |       - |       - |         - |
|        MeasureRequestNanoSlab | .NET Framework 4.7.2 | .NET Framework 4.7.2 |   8.767 us |  0.0291 us |  0.0258 us |   8.763 us |        - |       - |       - |         - |
|                               |                      |                      |            |            |            |            |          |         |         |           |
|            MeasureResponseGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  26.249 us |  0.1197 us |  0.1062 us |  26.253 us |        - |       - |       - |         - |
|           MeasureResponseNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 |   6.365 us |  0.0454 us |  0.0402 us |   6.377 us |        - |       - |       - |         - |
|     MeasureResponseNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  13.715 us |  0.0827 us |  0.0691 us |  13.698 us |        - |       - |       - |         - |
|       MeasureResponseNanoSlab | .NET Framework 4.7.2 | .NET Framework 4.7.2 |   7.183 us |  0.0543 us |  0.0482 us |   7.172 us |        - |       - |       - |         - |
|                               |                      |                      |            |            |            |            |          |         |         |           |
|           SerializeRequestGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 320.283 us |  1.8741 us |  1.5649 us | 320.173 us |        - |       - |       - |         - |
|          SerializeRequestNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 224.098 us |  1.5311 us |  1.4322 us | 223.544 us |        - |       - |       - |         - |
|    SerializeRequestNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 268.024 us |  1.0433 us |  0.9759 us | 267.954 us |        - |       - |       - |         - |
|      SerializeRequestNanoSlab | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 231.627 us |  0.4887 us |  0.4081 us | 231.367 us |        - |       - |       - |         - |
|                               |                      |                      |            |            |            |            |          |         |         |           |
|          SerializeResponseGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 216.657 us |  1.7237 us |  1.6124 us | 215.985 us |        - |       - |       - |         - |
|         SerializeResponseNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 135.464 us |  0.5065 us |  0.4738 us | 135.488 us |        - |       - |       - |         - |
|   SerializeResponseNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 157.948 us |  1.3205 us |  1.1706 us | 158.161 us |        - |       - |       - |         - |
|     SerializeResponseNanoSlab | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 134.287 us |  0.5314 us |  0.4711 us | 134.243 us |        - |       - |       - |         - |
|                               |                      |                      |            |            |            |            |          |         |         |           |
|         DeserializeRequestGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 552.766 us |  5.2001 us |  4.8642 us | 551.546 us | 143.5547 | 60.5469 |       - | 905,451 B |
|        DeserializeRequestNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 440.356 us |  3.6924 us |  3.4539 us | 439.782 us |        - |       - |       - |     104 B |
|  DeserializeRequestNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 169.978 us |  2.9460 us |  3.0253 us | 169.062 us |  80.8105 | 26.8555 |       - | 510,288 B |
|    DeserializeRequestNanoSlab | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 278.708 us |  2.2225 us |  2.0789 us | 278.545 us |  30.2734 | 30.2734 | 30.2734 | 271,259 B |
|                               |                      |                      |            |            |            |            |          |         |         |           |
|        DeserializeResponseGBP | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 333.082 us |  1.8117 us |  1.6947 us | 332.819 us |  81.5430 | 31.7383 |       - | 515,387 B |
|       DeserializeResponseNano | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 256.672 us |  4.8297 us |  4.7434 us | 255.551 us |        - |       - |       - |      64 B |
| DeserializeResponseNanoNoPool | .NET Framework 4.7.2 | .NET Framework 4.7.2 |  94.747 us |  1.7730 us |  2.3669 us |  93.424 us |  39.9170 | 13.3057 |       - | 252,706 B |
|   DeserializeResponseNanoSlab | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 168.535 us |  1.1699 us |  1.0943 us | 168.307 us |  13.4277 | 13.4277 | 13.4277 | 112,207 B |
