```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26300.9032)
AMD Ryzen 9 7900X 4.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]               : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  .NET 10.0            : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  .NET Framework 4.7.2 : .NET Framework 4.8.1 (4.8.9337.0), X64 RyuJIT VectorSize=256

IterationCount=3  LaunchCount=1  WarmupCount=3  

```
| Method           | Job                  | Runtime              | RunLength | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------- |--------------------- |--------------------- |---------- |---------:|---------:|---------:|------:|--------:|-------:|-------:|----------:|------------:|
| **LegacyReal**       | **.NET 10.0**            | **.NET 10.0**            | **1**         | **28.87 ns** | **5.593 ns** | **0.307 ns** |  **1.00** |    **0.01** | **0.0019** | **0.0012** |      **32 B** |        **1.00** |
| NanoViaLegacyApi | .NET 10.0            | .NET 10.0            | 1         | 17.04 ns | 1.663 ns | 0.091 ns |  0.59 |    0.01 | 0.0019 | 0.0012 |      32 B |        1.00 |
| NanoRaw          | .NET 10.0            | .NET 10.0            | 1         | 15.13 ns | 0.975 ns | 0.053 ns |  0.52 |    0.01 | 0.0019 | 0.0012 |      32 B |        1.00 |
| LegacyReal       | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 1         | 59.16 ns | 1.665 ns | 0.091 ns |  2.05 |    0.02 | 0.0054 | 0.0026 |      34 B |        1.06 |
| NanoViaLegacyApi | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 1         | 30.74 ns | 1.690 ns | 0.093 ns |  1.06 |    0.01 | 0.0054 | 0.0026 |      34 B |        1.06 |
| NanoRaw          | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 1         | 27.03 ns | 0.975 ns | 0.053 ns |  0.94 |    0.01 | 0.0054 | 0.0026 |      34 B |        1.06 |
|                  |                      |                      |           |          |          |          |       |         |        |        |           |             |
| **LegacyReal**       | **.NET 10.0**            | **.NET 10.0**            | **8**         | **24.88 ns** | **6.415 ns** | **0.352 ns** |  **1.00** |    **0.02** | **0.0019** | **0.0012** |      **32 B** |        **1.00** |
| NanoViaLegacyApi | .NET 10.0            | .NET 10.0            | 8         | 15.91 ns | 1.828 ns | 0.100 ns |  0.64 |    0.01 | 0.0019 | 0.0012 |      32 B |        1.00 |
| NanoRaw          | .NET 10.0            | .NET 10.0            | 8         | 14.70 ns | 1.355 ns | 0.074 ns |  0.59 |    0.01 | 0.0019 | 0.0012 |      32 B |        1.00 |
| LegacyReal       | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 8         | 50.12 ns | 9.172 ns | 0.503 ns |  2.01 |    0.03 | 0.0054 | 0.0026 |      34 B |        1.06 |
| NanoViaLegacyApi | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 8         | 29.52 ns | 5.989 ns | 0.328 ns |  1.19 |    0.02 | 0.0054 | 0.0026 |      34 B |        1.06 |
| NanoRaw          | .NET Framework 4.7.2 | .NET Framework 4.7.2 | 8         | 26.41 ns | 2.633 ns | 0.144 ns |  1.06 |    0.01 | 0.0054 | 0.0026 |      34 B |        1.06 |
