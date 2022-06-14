|              Method | VarintLen | ByteOffset |      Mean |     Error |    StdDev | Ratio |
|-------------------- |---------- |----------- |----------:|----------:|----------:|------:|
| Measure_Unoptimized |         1 |          0 | 0.2248 ns | 0.0026 ns | 0.0023 ns |  1.00 |
|       Measure_Lzcnt |         1 |          0 | 0.2227 ns | 0.0014 ns | 0.0013 ns |  0.99 |
|    Measure_LzcntMax |         1 |          0 | 0.2259 ns | 0.0025 ns | 0.0022 ns |  1.00 |
|                     |           |            |           |           |           |       |
|         Unoptimized |         1 |          0 | 1.4904 ns | 0.0071 ns | 0.0063 ns |  1.00 |
|           Intrinsic |         1 |          0 | 1.5047 ns | 0.0139 ns | 0.0116 ns |  1.01 |
|    WithZeroHighBits |         1 |          0 | 1.3977 ns | 0.0085 ns | 0.0067 ns |  0.94 |
|   WithZeroHighBits2 |         1 |          0 | 1.2017 ns | 0.0049 ns | 0.0041 ns |  0.81 |
|        ShiftedMasks |         1 |          0 | 1.2353 ns | 0.0085 ns | 0.0075 ns |  0.83 |
|                     |           |            |           |           |           |       |
| Measure_Unoptimized |         2 |          0 | 0.2227 ns | 0.0017 ns | 0.0015 ns |  1.00 |
|       Measure_Lzcnt |         2 |          0 | 0.2232 ns | 0.0017 ns | 0.0016 ns |  1.00 |
|    Measure_LzcntMax |         2 |          0 | 0.2255 ns | 0.0018 ns | 0.0016 ns |  1.01 |
|                     |           |            |           |           |           |       |
|         Unoptimized |         2 |          0 | 2.5854 ns | 0.0193 ns | 0.0151 ns |  1.00 |
|           Intrinsic |         2 |          0 | 1.5811 ns | 0.0072 ns | 0.0064 ns |  0.61 |
|    WithZeroHighBits |         2 |          0 | 1.8138 ns | 0.0147 ns | 0.0123 ns |  0.70 |
|   WithZeroHighBits2 |         2 |          0 | 2.0295 ns | 0.0260 ns | 0.0217 ns |  0.78 |
|        ShiftedMasks |         2 |          0 | 1.8171 ns | 0.0159 ns | 0.0149 ns |  0.70 |
|                     |           |            |           |           |           |       |
| Measure_Unoptimized |         3 |          0 | 0.2235 ns | 0.0013 ns | 0.0011 ns |  1.00 |
|       Measure_Lzcnt |         3 |          0 | 0.2242 ns | 0.0012 ns | 0.0011 ns |  1.00 |
|    Measure_LzcntMax |         3 |          0 | 0.2243 ns | 0.0016 ns | 0.0014 ns |  1.00 |
|                     |           |            |           |           |           |       |
|         Unoptimized |         3 |          0 | 3.6751 ns | 0.0158 ns | 0.0140 ns |  1.00 |
|           Intrinsic |         3 |          0 | 1.5832 ns | 0.0089 ns | 0.0079 ns |  0.43 |
|    WithZeroHighBits |         3 |          0 | 1.8112 ns | 0.0124 ns | 0.0110 ns |  0.49 |
|   WithZeroHighBits2 |         3 |          0 | 2.0221 ns | 0.0153 ns | 0.0136 ns |  0.55 |
|        ShiftedMasks |         3 |          0 | 1.8508 ns | 0.0310 ns | 0.0275 ns |  0.50 |
|                     |           |            |           |           |           |       |
| Measure_Unoptimized |         4 |          0 | 0.2221 ns | 0.0015 ns | 0.0012 ns |  1.00 |
|       Measure_Lzcnt |         4 |          0 | 0.2229 ns | 0.0014 ns | 0.0013 ns |  1.00 |
|    Measure_LzcntMax |         4 |          0 | 0.2233 ns | 0.0011 ns | 0.0010 ns |  1.00 |
|                     |           |            |           |           |           |       |
|         Unoptimized |         4 |          0 | 4.7138 ns | 0.0633 ns | 0.0592 ns |  1.00 |
|           Intrinsic |         4 |          0 | 1.5768 ns | 0.0073 ns | 0.0061 ns |  0.33 |
|    WithZeroHighBits |         4 |          0 | 1.8118 ns | 0.0247 ns | 0.0206 ns |  0.38 |
|   WithZeroHighBits2 |         4 |          0 | 2.0260 ns | 0.0238 ns | 0.0211 ns |  0.43 |
|        ShiftedMasks |         4 |          0 | 1.7961 ns | 0.0120 ns | 0.0112 ns |  0.38 |
|                     |           |            |           |           |           |       |
| Measure_Unoptimized |         5 |          0 | 0.2225 ns | 0.0014 ns | 0.0013 ns |  1.00 |
|       Measure_Lzcnt |         5 |          0 | 0.2217 ns | 0.0011 ns | 0.0009 ns |  1.00 |
|    Measure_LzcntMax |         5 |          0 | 0.2226 ns | 0.0013 ns | 0.0011 ns |  1.00 |
|                     |           |            |           |           |           |       |
|         Unoptimized |         5 |          0 | 5.0034 ns | 0.0248 ns | 0.0207 ns |  1.00 |
|           Intrinsic |         5 |          0 | 1.7994 ns | 0.0114 ns | 0.0095 ns |  0.36 |
|    WithZeroHighBits |         5 |          0 | 2.0165 ns | 0.0107 ns | 0.0095 ns |  0.40 |
|   WithZeroHighBits2 |         5 |          0 | 2.0159 ns | 0.0100 ns | 0.0078 ns |  0.40 |
|        ShiftedMasks |         5 |          0 | 2.0702 ns | 0.0411 ns | 0.0652 ns |  0.41 |