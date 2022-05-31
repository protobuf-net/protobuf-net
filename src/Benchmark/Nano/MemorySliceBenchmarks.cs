using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;

namespace Benchmark.Nano;

[SimpleJob(RuntimeMoniker.Net60), SimpleJob(RuntimeMoniker.Net472)]
public class MemorySliceBenchmarks
{
    public MemorySliceBenchmarks()
    {
        _array = new byte[4096];
        _memory = _array;
        _offset = 85;
    }
    private readonly byte[] _array;
    private int _offset;
    private readonly Memory<byte> _memory;

    private const int OPCOUNT = 1024;

    [Benchmark(OperationsPerInvoke = OPCOUNT)]
    public void NewMemory()
    {
        for (int i = 0; i < OPCOUNT; i++)
        {
            _ = new Memory<byte>(_array, _offset, 42);
        }
    }

    [Benchmark(OperationsPerInvoke = OPCOUNT)]
    public void MemorySlice()
    {
        for (int i = 0; i < OPCOUNT; i++)
        {
            _ = _memory.Slice(_offset, 42);
        }
    }


    [Benchmark(OperationsPerInvoke = OPCOUNT)]
    public void NewArray_Small()
    {
        for (int i = 0; i < OPCOUNT; i++)
        {
            _ = new byte[32];
        }
    }

    [Benchmark(OperationsPerInvoke = OPCOUNT)]
    public void GCArray_Small()
    {
#if NET5_0_OR_GREATER
        for (int i = 0; i < OPCOUNT; i++)
        {
            _ = GC.AllocateUninitializedArray<byte>(32);
        }
#else
        throw new PlatformNotSupportedException();
#endif
    }

    [Benchmark(OperationsPerInvoke = OPCOUNT)]
    public void NewArray_Large()
    {
        for (int i = 0; i < OPCOUNT; i++)
        {
            _ = new byte[4096];
        }
    }

    [Benchmark(OperationsPerInvoke = OPCOUNT)]
    public void GCArray_Large()
    {
#if NET5_0_OR_GREATER
        for (int i = 0; i < OPCOUNT; i++)
        {
            _ = GC.AllocateUninitializedArray<byte>(4096);
        }
#else
        throw new PlatformNotSupportedException();
#endif
    }
}
