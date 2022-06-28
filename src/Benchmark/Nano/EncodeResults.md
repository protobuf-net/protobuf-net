|                Method | VarintLen | ByteOffset |      Mean |     Error |    StdDev | Ratio | RatioSD |
|---------------------- |---------- |----------- |----------:|----------:|----------:|------:|--------:|
|           Measure_Leq |         1 |          0 | 0.2374 ns | 0.0045 ns | 0.0052 ns |  1.00 |    0.00 |
|       Measure_BitTest |         1 |          0 | 0.2293 ns | 0.0012 ns | 0.0010 ns |  0.96 |    0.02 |
|      Measure_LzcntDiv |         1 |          0 | 0.2313 ns | 0.0024 ns | 0.0020 ns |  0.97 |    0.02 |
| Measure_LzcntMulShift |         1 |          0 | 0.2306 ns | 0.0014 ns | 0.0011 ns |  0.97 |    0.02 |
|                       |           |            |           |           |           |       |         |
|           Unoptimized |         1 |          0 | 1.3855 ns | 0.0081 ns | 0.0068 ns |  1.00 |    0.00 |
|              Switched |         1 |          0 | 1.6689 ns | 0.0208 ns | 0.0173 ns |  1.20 |    0.01 |
|             Intrinsic |         1 |          0 | 1.6163 ns | 0.0265 ns | 0.0248 ns |  1.17 |    0.02 |
|      WithZeroHighBits |         1 |          0 | 1.5338 ns | 0.0151 ns | 0.0141 ns |  1.11 |    0.01 |
|     WithZeroHighBits2 |         1 |          0 | 1.2694 ns | 0.0189 ns | 0.0168 ns |  0.92 |    0.01 |
|          ShiftedMasks |         1 |          0 | 1.3645 ns | 0.0266 ns | 0.0273 ns |  0.99 |    0.02 |
|                       |           |            |           |           |           |       |         |
|           Measure_Leq |         2 |          0 | 0.2313 ns | 0.0017 ns | 0.0015 ns |  1.00 |    0.00 |
|       Measure_BitTest |         2 |          0 | 0.2315 ns | 0.0028 ns | 0.0025 ns |  1.00 |    0.01 |
|      Measure_LzcntDiv |         2 |          0 | 0.2339 ns | 0.0036 ns | 0.0032 ns |  1.01 |    0.02 |
| Measure_LzcntMulShift |         2 |          0 | 0.2313 ns | 0.0011 ns | 0.0009 ns |  1.00 |    0.01 |
|                       |           |            |           |           |           |       |         |
|           Unoptimized |         2 |          0 | 2.5930 ns | 0.0430 ns | 0.0381 ns |  1.00 |    0.00 |
|              Switched |         2 |          0 | 2.6756 ns | 0.0424 ns | 0.0396 ns |  1.03 |    0.03 |
|             Intrinsic |         2 |          0 | 1.8818 ns | 0.0197 ns | 0.0184 ns |  0.73 |    0.01 |
|      WithZeroHighBits |         2 |          0 | 1.8518 ns | 0.0119 ns | 0.0111 ns |  0.71 |    0.01 |
|     WithZeroHighBits2 |         2 |          0 | 2.0974 ns | 0.0235 ns | 0.0208 ns |  0.81 |    0.01 |
|          ShiftedMasks |         2 |          0 | 1.8551 ns | 0.0079 ns | 0.0066 ns |  0.71 |    0.01 |
|                       |           |            |           |           |           |       |         |
|           Measure_Leq |         3 |          0 | 0.2325 ns | 0.0028 ns | 0.0025 ns |  1.00 |    0.00 |
|       Measure_BitTest |         3 |          0 | 0.2322 ns | 0.0022 ns | 0.0020 ns |  1.00 |    0.01 |
|      Measure_LzcntDiv |         3 |          0 | 0.2337 ns | 0.0038 ns | 0.0033 ns |  1.01 |    0.02 |
| Measure_LzcntMulShift |         3 |          0 | 0.2313 ns | 0.0026 ns | 0.0023 ns |  0.99 |    0.02 |
|                       |           |            |           |           |           |       |         |
|           Unoptimized |         3 |          0 | 3.4403 ns | 0.0143 ns | 0.0111 ns |  1.00 |    0.00 |
|              Switched |         3 |          0 | 3.6650 ns | 0.0473 ns | 0.0395 ns |  1.07 |    0.01 |
|             Intrinsic |         3 |          0 | 1.8919 ns | 0.0292 ns | 0.0273 ns |  0.55 |    0.01 |
|      WithZeroHighBits |         3 |          0 | 1.8388 ns | 0.0294 ns | 0.0230 ns |  0.53 |    0.01 |
|     WithZeroHighBits2 |         3 |          0 | 2.0985 ns | 0.0288 ns | 0.0255 ns |  0.61 |    0.01 |
|          ShiftedMasks |         3 |          0 | 1.8700 ns | 0.0201 ns | 0.0167 ns |  0.54 |    0.01 |
|                       |           |            |           |           |           |       |         |
|           Measure_Leq |         4 |          0 | 0.2301 ns | 0.0013 ns | 0.0010 ns |  1.00 |    0.00 |
|       Measure_BitTest |         4 |          0 | 0.2305 ns | 0.0015 ns | 0.0012 ns |  1.00 |    0.01 |
|      Measure_LzcntDiv |         4 |          0 | 0.2343 ns | 0.0023 ns | 0.0021 ns |  1.02 |    0.01 |
| Measure_LzcntMulShift |         4 |          0 | 0.2323 ns | 0.0026 ns | 0.0025 ns |  1.01 |    0.01 |
|                       |           |            |           |           |           |       |         |
|           Unoptimized |         4 |          0 | 4.8901 ns | 0.0630 ns | 0.0590 ns |  1.00 |    0.00 |
|              Switched |         4 |          0 | 4.8135 ns | 0.0928 ns | 0.1270 ns |  0.98 |    0.03 |
|             Intrinsic |         4 |          0 | 1.6614 ns | 0.0270 ns | 0.0300 ns |  0.34 |    0.01 |
|      WithZeroHighBits |         4 |          0 | 1.9071 ns | 0.0380 ns | 0.0337 ns |  0.39 |    0.01 |
|     WithZeroHighBits2 |         4 |          0 | 2.1037 ns | 0.0395 ns | 0.0369 ns |  0.43 |    0.01 |
|          ShiftedMasks |         4 |          0 | 1.8767 ns | 0.0270 ns | 0.0226 ns |  0.38 |    0.01 |
|                       |           |            |           |           |           |       |         |
|           Measure_Leq |         5 |          0 | 0.2365 ns | 0.0040 ns | 0.0033 ns |  1.00 |    0.00 |
|       Measure_BitTest |         5 |          0 | 0.2354 ns | 0.0047 ns | 0.0075 ns |  0.98 |    0.02 |
|      Measure_LzcntDiv |         5 |          0 | 0.2390 ns | 0.0025 ns | 0.0023 ns |  1.01 |    0.02 |
| Measure_LzcntMulShift |         5 |          0 | 0.2348 ns | 0.0045 ns | 0.0060 ns |  0.98 |    0.03 |
|                       |           |            |           |           |           |       |         |
|           Unoptimized |         5 |          0 | 5.1533 ns | 0.0353 ns | 0.0276 ns |  1.00 |    0.00 |
|              Switched |         5 |          0 | 5.1139 ns | 0.0345 ns | 0.0306 ns |  0.99 |    0.01 |
|             Intrinsic |         5 |          0 | 1.8781 ns | 0.0167 ns | 0.0130 ns |  0.36 |    0.00 |
|      WithZeroHighBits |         5 |          0 | 2.1024 ns | 0.0415 ns | 0.0444 ns |  0.41 |    0.01 |
|     WithZeroHighBits2 |         5 |          0 | 2.1795 ns | 0.0409 ns | 0.0738 ns |  0.42 |    0.02 |
|          ShiftedMasks |         5 |          0 | 2.0985 ns | 0.0394 ns | 0.0422 ns |  0.41 |    0.01 |
