|                 Method | VarintLen | ByteOffset |      Mean |     Error |    StdDev | Ratio | RatioSD |
|----------------------- |---------- |----------- |----------:|----------:|----------:|------:|--------:|
|            Unoptimized |         1 |          0 | 0.9701 ns | 0.0192 ns | 0.0262 ns |  1.00 |    0.00 |
|              UnsafeAdd |         1 |          0 | 1.1643 ns | 0.0216 ns | 0.0202 ns |  1.21 |    0.04 |
|             Intrinsics |         1 |          0 | 1.6273 ns | 0.0102 ns | 0.0079 ns |  1.70 |    0.04 |
|  IntrinsicsPreferShort |         1 |          0 | 1.1577 ns | 0.0162 ns | 0.0152 ns |  1.20 |    0.04 |
| IntrinsicsPreferShort2 |         1 |          0 | 0.9414 ns | 0.0145 ns | 0.0136 ns |  0.97 |    0.03 |
| IntrinsicsPreferShort3 |         1 |          0 | 1.6435 ns | 0.0259 ns | 0.0242 ns |  1.70 |    0.06 |
| IntrinsicsPreferShort4 |         1 |          0 | 0.9355 ns | 0.0145 ns | 0.0121 ns |  0.97 |    0.02 |
|                        |           |            |           |           |           |       |         |
|            Unoptimized |         2 |          0 | 1.6377 ns | 0.0272 ns | 0.0213 ns |  1.00 |    0.00 |
|              UnsafeAdd |         2 |          0 | 1.6382 ns | 0.0244 ns | 0.0228 ns |  1.00 |    0.02 |
|             Intrinsics |         2 |          0 | 1.6477 ns | 0.0192 ns | 0.0180 ns |  1.01 |    0.01 |
|  IntrinsicsPreferShort |         2 |          0 | 1.4166 ns | 0.0201 ns | 0.0188 ns |  0.87 |    0.01 |
| IntrinsicsPreferShort2 |         2 |          0 | 1.6407 ns | 0.0298 ns | 0.0264 ns |  1.00 |    0.02 |
| IntrinsicsPreferShort3 |         2 |          0 | 1.6299 ns | 0.0106 ns | 0.0094 ns |  1.00 |    0.01 |
| IntrinsicsPreferShort4 |         2 |          0 | 1.4119 ns | 0.0251 ns | 0.0299 ns |  0.87 |    0.02 |
|                        |           |            |           |           |           |       |         |
|            Unoptimized |         3 |          0 | 2.2128 ns | 0.0433 ns | 0.0383 ns |  1.00 |    0.00 |
|              UnsafeAdd |         3 |          0 | 2.3240 ns | 0.0318 ns | 0.0298 ns |  1.05 |    0.02 |
|             Intrinsics |         3 |          0 | 1.6360 ns | 0.0132 ns | 0.0117 ns |  0.74 |    0.01 |
|  IntrinsicsPreferShort |         3 |          0 | 2.7948 ns | 0.0316 ns | 0.0296 ns |  1.26 |    0.03 |
| IntrinsicsPreferShort2 |         3 |          0 | 2.8523 ns | 0.0242 ns | 0.0214 ns |  1.29 |    0.03 |
| IntrinsicsPreferShort3 |         3 |          0 | 1.6701 ns | 0.0245 ns | 0.0262 ns |  0.76 |    0.02 |
| IntrinsicsPreferShort4 |         3 |          0 | 2.7362 ns | 0.0236 ns | 0.0221 ns |  1.24 |    0.02 |
|                        |           |            |           |           |           |       |         |
|            Unoptimized |         4 |          0 | 2.8612 ns | 0.0357 ns | 0.0334 ns |  1.00 |    0.00 |
|              UnsafeAdd |         4 |          0 | 2.9482 ns | 0.0283 ns | 0.0251 ns |  1.03 |    0.01 |
|             Intrinsics |         4 |          0 | 1.6418 ns | 0.0221 ns | 0.0206 ns |  0.57 |    0.01 |
|  IntrinsicsPreferShort |         4 |          0 | 2.7791 ns | 0.0297 ns | 0.0263 ns |  0.97 |    0.02 |
| IntrinsicsPreferShort2 |         4 |          0 | 2.8938 ns | 0.0453 ns | 0.0402 ns |  1.01 |    0.02 |
| IntrinsicsPreferShort3 |         4 |          0 | 1.6421 ns | 0.0204 ns | 0.0191 ns |  0.57 |    0.01 |
| IntrinsicsPreferShort4 |         4 |          0 | 2.6646 ns | 0.0122 ns | 0.0108 ns |  0.93 |    0.01 |
|                        |           |            |           |           |           |       |         |
|            Unoptimized |         5 |          0 | 3.5094 ns | 0.0414 ns | 0.0387 ns |  1.00 |    0.00 |
|              UnsafeAdd |         5 |          0 | 3.5221 ns | 0.0282 ns | 0.0264 ns |  1.00 |    0.01 |
|             Intrinsics |         5 |          0 | 1.6325 ns | 0.0106 ns | 0.0094 ns |  0.46 |    0.01 |
|  IntrinsicsPreferShort |         5 |          0 | 2.8206 ns | 0.0253 ns | 0.0224 ns |  0.80 |    0.01 |
| IntrinsicsPreferShort2 |         5 |          0 | 2.8456 ns | 0.0213 ns | 0.0178 ns |  0.81 |    0.01 |
| IntrinsicsPreferShort3 |         5 |          0 | 3.0514 ns | 0.0215 ns | 0.0191 ns |  0.87 |    0.01 |
| IntrinsicsPreferShort4 |         5 |          0 | 2.6700 ns | 0.0382 ns | 0.0357 ns |  0.76 |    0.02 |
|                        |           |            |           |           |           |       |         |
|            Unoptimized |         6 |          0 | 4.2874 ns | 0.0856 ns | 0.1113 ns |  1.00 |    0.00 |
|              UnsafeAdd |         6 |          0 | 4.1358 ns | 0.0575 ns | 0.0538 ns |  0.97 |    0.03 |
|             Intrinsics |         6 |          0 | 1.6394 ns | 0.0159 ns | 0.0141 ns |  0.38 |    0.01 |
|  IntrinsicsPreferShort |         6 |          0 | 2.7627 ns | 0.0248 ns | 0.0232 ns |  0.65 |    0.02 |
| IntrinsicsPreferShort2 |         6 |          0 | 2.8750 ns | 0.0254 ns | 0.0212 ns |  0.67 |    0.02 |
| IntrinsicsPreferShort3 |         6 |          0 | 3.1435 ns | 0.0124 ns | 0.0110 ns |  0.74 |    0.02 |
| IntrinsicsPreferShort4 |         6 |          0 | 2.6936 ns | 0.0429 ns | 0.0381 ns |  0.63 |    0.02 |
|                        |           |            |           |           |           |       |         |
|            Unoptimized |         7 |          0 | 4.8105 ns | 0.0481 ns | 0.0426 ns |  1.00 |    0.00 |
|              UnsafeAdd |         7 |          0 | 4.4880 ns | 0.0692 ns | 0.0614 ns |  0.93 |    0.01 |
|             Intrinsics |         7 |          0 | 1.6293 ns | 0.0151 ns | 0.0134 ns |  0.34 |    0.00 |
|  IntrinsicsPreferShort |         7 |          0 | 2.7554 ns | 0.0097 ns | 0.0091 ns |  0.57 |    0.01 |
| IntrinsicsPreferShort2 |         7 |          0 | 2.8310 ns | 0.0156 ns | 0.0130 ns |  0.59 |    0.01 |
| IntrinsicsPreferShort3 |         7 |          0 | 3.1439 ns | 0.0174 ns | 0.0163 ns |  0.65 |    0.01 |
| IntrinsicsPreferShort4 |         7 |          0 | 2.6583 ns | 0.0209 ns | 0.0196 ns |  0.55 |    0.00 |
|                        |           |            |           |           |           |       |         |
|            Unoptimized |         8 |          0 | 5.5345 ns | 0.0395 ns | 0.0330 ns |  1.00 |    0.00 |
|              UnsafeAdd |         8 |          0 | 5.2156 ns | 0.1018 ns | 0.0795 ns |  0.94 |    0.01 |
|             Intrinsics |         8 |          0 | 1.6254 ns | 0.0093 ns | 0.0077 ns |  0.29 |    0.00 |
|  IntrinsicsPreferShort |         8 |          0 | 2.7634 ns | 0.0194 ns | 0.0172 ns |  0.50 |    0.00 |
| IntrinsicsPreferShort2 |         8 |          0 | 2.8524 ns | 0.0536 ns | 0.0475 ns |  0.51 |    0.01 |
| IntrinsicsPreferShort3 |         8 |          0 | 3.0930 ns | 0.0184 ns | 0.0154 ns |  0.56 |    0.01 |
| IntrinsicsPreferShort4 |         8 |          0 | 2.6736 ns | 0.0259 ns | 0.0230 ns |  0.48 |    0.00 |
|                        |           |            |           |           |           |       |         |
|            Unoptimized |         9 |          0 | 6.1974 ns | 0.0261 ns | 0.0218 ns |  1.00 |    0.00 |
|              UnsafeAdd |         9 |          0 | 5.6896 ns | 0.0802 ns | 0.0670 ns |  0.92 |    0.01 |
|             Intrinsics |         9 |          0 | 2.0850 ns | 0.0414 ns | 0.0407 ns |  0.34 |    0.01 |
|  IntrinsicsPreferShort |         9 |          0 | 2.7523 ns | 0.0161 ns | 0.0150 ns |  0.44 |    0.00 |
| IntrinsicsPreferShort2 |         9 |          0 | 2.8590 ns | 0.0563 ns | 0.0553 ns |  0.46 |    0.01 |
| IntrinsicsPreferShort3 |         9 |          0 | 3.2325 ns | 0.0219 ns | 0.0171 ns |  0.52 |    0.00 |
| IntrinsicsPreferShort4 |         9 |          0 | 2.6912 ns | 0.0287 ns | 0.0255 ns |  0.43 |    0.00 |
|                        |           |            |           |           |           |       |         |
|            Unoptimized |        10 |          0 | 7.0200 ns | 0.0905 ns | 0.0755 ns |  1.00 |    0.00 |
|              UnsafeAdd |        10 |          0 | 6.4425 ns | 0.1027 ns | 0.0960 ns |  0.92 |    0.02 |
|             Intrinsics |        10 |          0 | 2.7786 ns | 0.0191 ns | 0.0159 ns |  0.40 |    0.00 |
|  IntrinsicsPreferShort |        10 |          0 | 2.8116 ns | 0.0352 ns | 0.0312 ns |  0.40 |    0.01 |
| IntrinsicsPreferShort2 |        10 |          0 | 2.8928 ns | 0.0248 ns | 0.0219 ns |  0.41 |    0.01 |
| IntrinsicsPreferShort3 |        10 |          0 | 3.1092 ns | 0.0217 ns | 0.0193 ns |  0.44 |    0.01 |
| IntrinsicsPreferShort4 |        10 |          0 | 2.7226 ns | 0.0375 ns | 0.0332 ns |  0.39 |    0.01 |