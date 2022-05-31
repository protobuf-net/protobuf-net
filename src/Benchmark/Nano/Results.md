# Typical results

|                        Method |      Mean |    Error |   StdDev |   Gen 0 |   Gen 1 | Allocated |
|------------------------------ |----------:|---------:|---------:|--------:|--------:|----------:|
|             MeasureRequestGBP |  50.91 us | 0.392 us | 0.347 us |       - |       - |         - |
|            MeasureResponseGBP |  26.04 us | 0.386 us | 0.342 us |       - |       - |         - |
|            MeasureRequestNano |  14.14 us | 0.024 us | 0.019 us |       - |       - |         - |
|           MeasureResponseNano |  10.25 us | 0.048 us | 0.043 us |       - |       - |         - |
|      MeasureRequestNanoNoPool |  15.90 us | 0.104 us | 0.097 us |       - |       - |         - |
|     MeasureResponseNanoNoPool |  11.04 us | 0.018 us | 0.015 us |       - |       - |         - |
|                               |           |          |          |         |         |           |
|           SerializeRequestGBP | 143.76 us | 0.807 us | 0.674 us |       - |       - |         - |
|          SerializeResponseGBP |  83.71 us | 0.396 us | 0.331 us |       - |       - |         - |
|          SerializeRequestNano |  95.85 us | 1.465 us | 1.299 us |       - |       - |         - |
|         SerializeResponseNano |  46.27 us | 0.222 us | 0.208 us |       - |       - |         - |
|    SerializeRequestNanoNoPool |  75.08 us | 0.465 us | 0.435 us |       - |       - |         - |
|   SerializeResponseNanoNoPool |  42.85 us | 0.476 us | 0.446 us |       - |       - |         - |
|                               |           |          |          |         |         |           |
|         DeserializeRequestGBP | 259.22 us | 2.323 us | 2.173 us | 91.7969 | 39.0625 | 770,800 B |
|        DeserializeResponseGBP | 185.57 us | 3.009 us | 2.668 us | 61.2793 | 21.7285 | 513,960 B |
|        DeserializeRequestNano | 196.39 us | 0.814 us | 0.680 us |       - |       - |     101 B |
|       DeserializeResponseNano | 120.87 us | 0.640 us | 0.568 us |       - |       - |      64 B |
|  DeserializeRequestNanoNoPool | 129.87 us | 1.542 us | 1.204 us | 69.5801 | 23.1934 | 584,232 B |
| DeserializeResponseNanoNoPool |  84.69 us | 1.661 us | 3.240 us | 39.0625 | 12.9395 | 327,344 B |