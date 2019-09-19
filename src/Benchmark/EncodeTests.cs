#if INTRINSICS
using BenchmarkDotNet.Attributes;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    public class EncodeTests
    {
        const int LOOP_SIZE = 2048;
        uint[] _values32 = new uint[LOOP_SIZE];
        ulong[] _values64 = new ulong[LOOP_SIZE];

        [GlobalSetup]
        public void Setup()
        {
            var rand = new Random(12345);
            for(int i = 0; i < LOOP_SIZE; i++)
            {
                if (i < (LOOP_SIZE / 4))
                {   // 1 byte
                    _values32[i] = (uint)rand.Next(0xFF);
                    _values64[i] = (ulong)rand.Next(0xFF);
                }
                else if (i < (LOOP_SIZE / 2))
                {   // 2 bytes
                    _values32[i] = (uint)rand.Next(0xFFFF);
                    _values64[i] = (ulong)rand.Next(0xFFFF);
                }
                else if (i < ((3 * LOOP_SIZE) / 4))
                {   // non-negative full-width
                    _values32[i] = (uint)rand.Next();
                    _values64[i] = (ulong)(((long)rand.Next()) << 32
                        | (long)rand.Next(int.MinValue, int.MaxValue));
                }
                else
                {
                    // possibly-negative full-width
                    _values32[i] = (uint)rand.Next(int.MinValue, int.MaxValue);
                    _values64[i] = (ulong)(((long)rand.Next(int.MinValue, int.MaxValue)) << 32
                        | (long)rand.Next(int.MinValue, int.MaxValue));
                }

                int lzcnt = ComputeVarintLength32_Lzcnt(_values32[i]);
                int loop = ComputeVarintLength32_Loop(_values32[i]);
                if (lzcnt != loop)
                    throw new InvalidOperationException($"32: {_values32[i]}, expected {loop}, got {lzcnt}");

                lzcnt = ComputeVarintLength64_Lzcnt(_values64[i]);
                loop = ComputeVarintLength64_Loop(_values64[i]);
                if (lzcnt != loop)
                    throw new InvalidOperationException($"64: {_values64[i]}, expected {loop}, got {lzcnt}");
            }
        }

        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void ComputeVarintLength32_Loop()
        {
            var arr = _values32;
            for (int i = 0; i < arr.Length; i++)
                ComputeVarintLength32_Loop(arr[i]);
        }

        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void ComputeVarintLength64_Loop()
        {
            var arr = _values64;
            for (int i = 0; i < arr.Length; i++)
                ComputeVarintLength64_Loop(arr[i]);
        }

        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void ComputeVarintLength32_Lzcnt()
        {
            var arr = _values32;
            for (int i = 0; i < arr.Length; i++)
                ComputeVarintLength32_Loop(arr[i]);
        }

        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void ComputeVarintLength64_Lzcnt()
        {
            var arr = _values64;
            for (int i = 0; i < arr.Length; i++)
                ComputeVarintLength64_Loop(arr[i]);
        }

        // deliberately not static/aggressive-inline; in real impl, this
        // is an override on a virtual method
        int ComputeVarintLength32_Lzcnt(uint value)
            => ((31 - BitOperations.LeadingZeroCount(value | 1)) / 7) + 1;

        int ComputeVarintLength32_Loop(uint value)
        {
            int count = 1;
            while ((value >>= 7) != 0)
            {
                count++;
            }
            return count;
        }

        int ComputeVarintLength64_Lzcnt(ulong value)
            => ((63 - BitOperations.LeadingZeroCount(value | 1)) / 7) + 1;

        int ComputeVarintLength64_Loop(ulong value)
        {
            int count = 1;
            while ((value >>= 7) != 0)
            {
                count++;
            }
            return count;
        }
    }
}
#endif