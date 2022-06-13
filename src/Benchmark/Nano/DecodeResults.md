|                 Method | VarintLen | ByteOffset |      Mean |     Error |    StdDev |    Median | Ratio | RatioSD |
|----------------------- |---------- |----------- |----------:|----------:|----------:|----------:|------:|--------:|
|            Unoptimized |         1 |          0 | 0.9315 ns | 0.0127 ns | 0.0106 ns | 0.9350 ns |  1.00 |    0.00 |
|              UnsafeAdd |         1 |          0 | 1.1526 ns | 0.0112 ns | 0.0105 ns | 1.1555 ns |  1.24 |    0.01 |
|             Intrinsics |         1 |          0 | 1.6272 ns | 0.0111 ns | 0.0092 ns | 1.6292 ns |  1.75 |    0.02 |
|  IntrinsicsPreferShort |         1 |          0 | 1.1543 ns | 0.0128 ns | 0.0113 ns | 1.1528 ns |  1.24 |    0.02 |
| IntrinsicsPreferShort2 |         1 |          0 | 0.9495 ns | 0.0184 ns | 0.0189 ns | 0.9423 ns |  1.02 |    0.02 |
| IntrinsicsPreferShort3 |         1 |          0 | 1.6477 ns | 0.0203 ns | 0.0180 ns | 1.6500 ns |  1.77 |    0.03 |
|                        |           |            |           |           |           |           |       |         |
|            Unoptimized |         2 |          0 | 1.6722 ns | 0.0318 ns | 0.0327 ns | 1.6730 ns |  1.00 |    0.00 |
|              UnsafeAdd |         2 |          0 | 1.6303 ns | 0.0127 ns | 0.0112 ns | 1.6286 ns |  0.98 |    0.02 |
|             Intrinsics |         2 |          0 | 1.6582 ns | 0.0294 ns | 0.0275 ns | 1.6475 ns |  0.99 |    0.01 |
|  IntrinsicsPreferShort |         2 |          0 | 1.4150 ns | 0.0236 ns | 0.0221 ns | 1.4198 ns |  0.85 |    0.02 |
| IntrinsicsPreferShort2 |         2 |          0 | 1.6513 ns | 0.0327 ns | 0.0390 ns | 1.6366 ns |  0.99 |    0.03 |
| IntrinsicsPreferShort3 |         2 |          0 | 1.6336 ns | 0.0115 ns | 0.0107 ns | 1.6303 ns |  0.98 |    0.02 |
|                        |           |            |           |           |           |           |       |         |
|            Unoptimized |         3 |          0 | 2.2582 ns | 0.0435 ns | 0.0610 ns | 2.2389 ns |  1.00 |    0.00 |
|              UnsafeAdd |         3 |          0 | 2.3005 ns | 0.0343 ns | 0.0321 ns | 2.3088 ns |  1.01 |    0.03 |
|             Intrinsics |         3 |          0 | 1.6436 ns | 0.0248 ns | 0.0220 ns | 1.6362 ns |  0.72 |    0.02 |
|  IntrinsicsPreferShort |         3 |          0 | 2.7886 ns | 0.0278 ns | 0.0260 ns | 2.7801 ns |  1.23 |    0.04 |
| IntrinsicsPreferShort2 |         3 |          0 | 2.8906 ns | 0.0346 ns | 0.0307 ns | 2.8976 ns |  1.27 |    0.04 |
| IntrinsicsPreferShort3 |         3 |          0 | 1.6730 ns | 0.0323 ns | 0.0409 ns | 1.6594 ns |  0.74 |    0.02 |
|                        |           |            |           |           |           |           |       |         |
|            Unoptimized |         4 |          0 | 2.7883 ns | 0.0148 ns | 0.0124 ns | 2.7918 ns |  1.00 |    0.00 |
|              UnsafeAdd |         4 |          0 | 2.9560 ns | 0.0342 ns | 0.0319 ns | 2.9401 ns |  1.06 |    0.01 |
|             Intrinsics |         4 |          0 | 1.6288 ns | 0.0084 ns | 0.0075 ns | 1.6272 ns |  0.58 |    0.00 |
|  IntrinsicsPreferShort |         4 |          0 | 2.8880 ns | 0.0573 ns | 0.0637 ns | 2.8725 ns |  1.04 |    0.03 |
| IntrinsicsPreferShort2 |         4 |          0 | 2.9575 ns | 0.0591 ns | 0.0552 ns | 2.9505 ns |  1.06 |    0.02 |
| IntrinsicsPreferShort3 |         4 |          0 | 1.6672 ns | 0.0325 ns | 0.0411 ns | 1.6494 ns |  0.60 |    0.02 |
|                        |           |            |           |           |           |           |       |         |
|            Unoptimized |         5 |          0 | 3.4792 ns | 0.0157 ns | 0.0147 ns | 3.4834 ns |  1.00 |    0.00 |
|              UnsafeAdd |         5 |          0 | 3.4869 ns | 0.0300 ns | 0.0281 ns | 3.4849 ns |  1.00 |    0.01 |
|             Intrinsics |         5 |          0 | 1.6249 ns | 0.0063 ns | 0.0056 ns | 1.6236 ns |  0.47 |    0.00 |
|  IntrinsicsPreferShort |         5 |          0 | 2.7964 ns | 0.0409 ns | 0.0383 ns | 2.7829 ns |  0.80 |    0.01 |
| IntrinsicsPreferShort2 |         5 |          0 | 2.8774 ns | 0.0170 ns | 0.0133 ns | 2.8787 ns |  0.83 |    0.00 |
| IntrinsicsPreferShort3 |         5 |          0 | 3.0180 ns | 0.0251 ns | 0.0235 ns | 3.0259 ns |  0.87 |    0.01 |
|                        |           |            |           |           |           |           |       |         |
|            Unoptimized |         6 |          0 | 4.1627 ns | 0.0332 ns | 0.0277 ns | 4.1574 ns |  1.00 |    0.00 |
|              UnsafeAdd |         6 |          0 | 4.2029 ns | 0.0802 ns | 0.0985 ns | 4.1960 ns |  1.01 |    0.03 |
|             Intrinsics |         6 |          0 | 1.6762 ns | 0.0327 ns | 0.0322 ns | 1.6719 ns |  0.40 |    0.01 |
|  IntrinsicsPreferShort |         6 |          0 | 2.7819 ns | 0.0400 ns | 0.0355 ns | 2.7776 ns |  0.67 |    0.01 |
| IntrinsicsPreferShort2 |         6 |          0 | 2.8681 ns | 0.0187 ns | 0.0166 ns | 2.8676 ns |  0.69 |    0.01 |
| IntrinsicsPreferShort3 |         6 |          0 | 3.1852 ns | 0.0554 ns | 0.0462 ns | 3.1840 ns |  0.77 |    0.01 |
|                        |           |            |           |           |           |           |       |         |
|            Unoptimized |         7 |          0 | 4.9214 ns | 0.0880 ns | 0.0823 ns | 4.9555 ns |  1.00 |    0.00 |
|              UnsafeAdd |         7 |          0 | 4.5341 ns | 0.0879 ns | 0.0863 ns | 4.5263 ns |  0.92 |    0.02 |
|             Intrinsics |         7 |          0 | 1.6387 ns | 0.0105 ns | 0.0093 ns | 1.6407 ns |  0.33 |    0.01 |
|  IntrinsicsPreferShort |         7 |          0 | 2.8215 ns | 0.0147 ns | 0.0130 ns | 2.8222 ns |  0.57 |    0.01 |
| IntrinsicsPreferShort2 |         7 |          0 | 2.8975 ns | 0.0261 ns | 0.0244 ns | 2.8908 ns |  0.59 |    0.01 |
| IntrinsicsPreferShort3 |         7 |          0 | 3.1334 ns | 0.0306 ns | 0.0271 ns | 3.1247 ns |  0.64 |    0.01 |
|                        |           |            |           |           |           |           |       |         |
|            Unoptimized |         8 |          0 | 5.7113 ns | 0.1053 ns | 0.1978 ns | 5.6296 ns |  1.00 |    0.00 |
|              UnsafeAdd |         8 |          0 | 5.2129 ns | 0.0990 ns | 0.0926 ns | 5.2132 ns |  0.89 |    0.04 |
|             Intrinsics |         8 |          0 | 1.6357 ns | 0.0125 ns | 0.0110 ns | 1.6313 ns |  0.28 |    0.01 |
|  IntrinsicsPreferShort |         8 |          0 | 2.8141 ns | 0.0292 ns | 0.0259 ns | 2.8072 ns |  0.48 |    0.02 |
| IntrinsicsPreferShort2 |         8 |          0 | 2.9319 ns | 0.0330 ns | 0.0293 ns | 2.9309 ns |  0.50 |    0.02 |
| IntrinsicsPreferShort3 |         8 |          0 | 3.2284 ns | 0.0487 ns | 0.0455 ns | 3.2131 ns |  0.55 |    0.03 |
|                        |           |            |           |           |           |           |       |         |
|            Unoptimized |         9 |          0 | 6.3097 ns | 0.1021 ns | 0.0955 ns | 6.2750 ns |  1.00 |    0.00 |
|              UnsafeAdd |         9 |          0 | 5.6407 ns | 0.0426 ns | 0.0332 ns | 5.6419 ns |  0.89 |    0.01 |
|             Intrinsics |         9 |          0 | 2.0868 ns | 0.0292 ns | 0.0273 ns | 2.0854 ns |  0.33 |    0.01 |
|  IntrinsicsPreferShort |         9 |          0 | 2.8012 ns | 0.0490 ns | 0.0459 ns | 2.7943 ns |  0.44 |    0.01 |
| IntrinsicsPreferShort2 |         9 |          0 | 2.9056 ns | 0.0556 ns | 0.0742 ns | 2.8827 ns |  0.47 |    0.01 |
| IntrinsicsPreferShort3 |         9 |          0 | 3.1286 ns | 0.0336 ns | 0.0314 ns | 3.1302 ns |  0.50 |    0.01 |
|                        |           |            |           |           |           |           |       |         |
|            Unoptimized |        10 |          0 | 6.9705 ns | 0.0664 ns | 0.0554 ns | 6.9636 ns |  1.00 |    0.00 |
|              UnsafeAdd |        10 |          0 | 6.3892 ns | 0.0653 ns | 0.0510 ns | 6.4013 ns |  0.92 |    0.01 |
|             Intrinsics |        10 |          0 | 2.7644 ns | 0.0317 ns | 0.0296 ns | 2.7518 ns |  0.40 |    0.00 |
|  IntrinsicsPreferShort |        10 |          0 | 2.8162 ns | 0.0475 ns | 0.0444 ns | 2.8110 ns |  0.40 |    0.01 |
| IntrinsicsPreferShort2 |        10 |          0 | 2.9190 ns | 0.0512 ns | 0.0454 ns | 2.9084 ns |  0.42 |    0.01 |
| IntrinsicsPreferShort3 |        10 |          0 | 3.1537 ns | 0.0158 ns | 0.0148 ns | 3.1517 ns |  0.45 |    0.00 |