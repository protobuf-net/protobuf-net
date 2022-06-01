using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ProtoBuf.Nano;
using ProtoBuf.Nano.Internal;
using System;

namespace Benchmark.Nano;

[SimpleJob(RuntimeMoniker.Net60), SimpleJob(RuntimeMoniker.Net472)]
[MemoryDiagnoser]
public class ArrayAllocBenchmarks
{
    private const int OperationsPerInvoke = 1000;

    [Params(8, 32, 256)]
    public int Length { get; set; }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void SimpleSlabRent()
    {
        for (int i = 0; i < 1000; i++)
        {
            _ = SimpleSlabAllocator<byte>.Rent(Length);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void RefCountedSlabRent()
    {
        for (int i = 0; i < 1000; i++)
        {
            RefCountedMemory.Release(RefCountedSlabAllocator<byte>.Rent(Length));
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void NewArray()
    {
        for (int i = 0; i < 1000; i++)
        {
            _ = new byte[Length];
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void GCUninitializedArray()
    {
        for (int i = 0; i < 1000; i++)
        {
#if NET5_0_OR_GREATER
            _ = GC.AllocateUninitializedArray<byte>(Length);
#else
            throw new PlatformNotSupportedException();
#endif
        }
    }
}
