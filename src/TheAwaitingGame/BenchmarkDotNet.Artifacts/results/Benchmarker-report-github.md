``` ini

BenchmarkDotNet=v0.10.3.0, OS=Microsoft Windows 10.0.15063
Processor=Intel(R) Core(TM) i7-7700HQ CPU 2.80GHz, ProcessorCount=8
Frequency=2742190 Hz, Resolution=364.6720 ns, Timer=TSC
dotnet cli version=1.0.2
  [Host]     : .NET Core 4.6.25009.03, 64bit RyuJIT
  DefaultJob : .NET Core 4.6.25009.03, 64bit RyuJIT


```
 |         Method |      Mean |    StdDev |
 |--------------- |---------- |---------- |
 |           Sync | 3.0318 ms | 0.0373 ms |
 |      TaskAsync | 4.5325 ms | 0.0726 ms |
 | ValueTaskAsync | 6.4374 ms | 0.0426 ms |
