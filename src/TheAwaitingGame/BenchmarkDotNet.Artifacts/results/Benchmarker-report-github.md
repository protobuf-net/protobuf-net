``` ini

BenchmarkDotNet=v0.10.3.111-nightly, OS=Windows 10.0.15063
Processor=Intel(R) Core(TM) i7-7700HQ CPU 2.80GHz, ProcessorCount=8
Frequency=2742188 Hz, Resolution=364.6723 ns, Timer=TSC
dotnet cli version=1.0.2
  [Host]     : .NET Core 4.6.25009.03, 64bit RyuJIT
  Job-RZXVEI : .NET Core 4.6.25009.03, 64bit RyuJIT

Force=False  

```
 |                         Method |       Mean |    StdDev |     Op/s |  Gen 0 |  Gen 1 | Allocated |
 |------------------------------- |-----------:|----------:|---------:|-------:|-------:|----------:|
 |                           Sync | 20.2238 us | 0.0964 us | 49446.57 |      - |      - |      0 kB |
 |                      TaskAsync | 30.6326 us | 0.3065 us |    32645 | 7.7250 |      - |  24.25 kB |
 |               TaskCheckedAsync | 30.0058 us | 0.1833 us | 33326.85 | 7.7250 |      - |  24.25 kB |
 |                 ValueTaskAsync | 35.1996 us | 0.3619 us | 28409.39 |      - |      - |      0 kB |
 |          ValueTaskWrappedAsync | 33.2181 us | 0.2537 us | 30104.09 | 1.2667 | 0.0083 |   3.99 kB |
 | ValueTaskDecimalReferenceAsync | 30.1789 us | 0.6205 us | 33135.76 | 3.0917 |      - |    9.7 kB |
 |          ValueTaskCheckedAsync | 32.5184 us | 0.2960 us | 30751.79 |      - |      - |      0 kB |
 |               HandCrankedAsync | 25.8619 us | 0.1911 us | 38666.89 |      - |      - |      0 kB |
 |           AssertCompletedAsync | 27.9178 us | 0.1402 us | 35819.44 |      - |      - |      0 kB |
 |                TaskDoubleAsync | 26.0973 us | 0.2359 us |  38318.2 | 6.9500 |      - |  21.83 kB |
 |           ValueTaskDoubleAsync | 28.7127 us | 0.2823 us | 34827.83 |      - |      - |      0 kB |
