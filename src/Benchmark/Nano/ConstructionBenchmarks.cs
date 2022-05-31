using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Benchmark.Nano;

[SimpleJob(RuntimeMoniker.Net60), SimpleJob(RuntimeMoniker.Net472)]
public class ConstructionBenchmarks
{
    private const int OperationsPerInvoke = 1000;

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void CopyCtor()
    {
        Context ctx = default;
        A value = default;
        for(int i = 0; i < 1000; i++)
        {
            value = new A(in value, ref ctx);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void Mutable()
    {
        Context ctx = default;
        B value = default;
        for (int i = 0; i < 1000; i++)
        {
            B.Read(ref value, ref ctx);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void InnerBuilder()
    {
        Context ctx = default;
        C value = default;
        for (int i = 0; i < 1000; i++)
        {
            C.Read(ref value, ref ctx);
        }
    }

    [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
    public void EvilOverwrite()
    {
        Context ctx = default;
        C value = default;
        for (int i = 0; i < 1000; i++)
        {
            C.Read(ref value, ref ctx);
        }
    }
}



public readonly struct A
{
    readonly Guid a, b, c, d, e, f;
    readonly int x;
    public A(in A src, ref Context ctx)
    {
        this = src;
        x = ctx.Read();
    }
}

public struct B
{
    Guid a, b, c, d, e, f;
    int x;
    public static void Read(ref B value, ref Context ctx)
    {
        value.x = ctx.Read();
    }
}

public readonly struct C
{
    readonly Guid a, b, c, d, e, f;
    readonly int x;

    private struct Mutable
    {
        public Guid a, b, c, d;
        public int x;
    }

    public static void Read(ref C value, ref Context ctx)
    {
        ref var mutable = ref Unsafe.As<C, Mutable>(ref value);
        mutable.x = ctx.Read();
    }
}

public readonly struct D
{
    readonly Guid a, b, c, d, e, f;
    readonly int x;

    public static void Read(ref D value, ref Context ctx)
    {
        Unsafe.AsRef(in value.x) = ctx.Read();
    }
}



public ref struct Context
{
    public int Read() => 42;

}

