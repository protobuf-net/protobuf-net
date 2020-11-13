#if INTRINSICS
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using System;
using System.Numerics;

namespace Benchmark
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [SimpleJob(RuntimeMoniker.Net472), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class VarintEncodeBenchmarks
    {
        const int LOOP_SIZE = 2048;
        readonly uint[] _values32 = new uint[LOOP_SIZE];
        readonly ulong[] _values64 = new ulong[LOOP_SIZE];

        [GlobalSetup]
        public void Setup()
        {
            var rand = new Random(12345);
            Span<byte> spanLzcnt = stackalloc byte[16];
            Span<byte> spanLoop = stackalloc byte[16];
            for (int i = 0; i < LOOP_SIZE; i++)
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
                    throw new InvalidOperationException($"32-len: {_values32[i]}, expected {loop}, got {lzcnt}");

                lzcnt = ComputeVarintLength64_Lzcnt(_values64[i]);
                loop = ComputeVarintLength64_Loop(_values64[i]);
                if (lzcnt != loop)
                    throw new InvalidOperationException($"64-len: {_values64[i]}, expected {loop}, got {lzcnt}");

                lzcnt = WriteVarint32_Lzcnt(_values32[i], spanLzcnt, 4);
                loop = WriteVarint32_Loop(_values32[i], spanLoop, 4);
                if (lzcnt != loop)
                    throw new InvalidOperationException($"32-write: {_values32[i]}, expected {loop}, got {lzcnt}");
                if (!spanLzcnt.Slice(4, lzcnt).SequenceEqual(spanLoop.Slice(4, loop)))
                    throw new InvalidOperationException($"32-write: spans are different");

                loop = WriteVarint32_Loop_Ptr(_values32[i], spanLoop, 4);
                if (lzcnt != loop)
                    throw new InvalidOperationException($"32-write: {_values32[i]}, expected {lzcnt}, got {loop}");
                if (!spanLzcnt.Slice(4, lzcnt).SequenceEqual(spanLoop.Slice(4, loop)))
                    throw new InvalidOperationException($"32-write (ptr): spans are different");

                loop = WriteVarint32_Loop_Ptr_PreNext(_values32[i], spanLoop, 4);
                if (lzcnt != loop)
                    throw new InvalidOperationException($"32-write: {_values32[i]}, expected {lzcnt}, got {loop}");
                if (!spanLzcnt.Slice(4, lzcnt).SequenceEqual(spanLoop.Slice(4, loop)))
                    throw new InvalidOperationException($"32-write (ptr/pre): spans are different");

                loop = WriteVarint32_Loop_PreNext(_values32[i], spanLoop, 4);
                if (lzcnt != loop)
                    throw new InvalidOperationException($"32-write: {_values32[i]}, expected {lzcnt}, got {loop}");
                if (!spanLzcnt.Slice(4, lzcnt).SequenceEqual(spanLoop.Slice(4, loop)))
                    throw new InvalidOperationException($"32-write (pre): spans are different");

                lzcnt = WriteVarint64_Lzcnt(_values64[i], spanLzcnt, 4);
                loop = WriteVarint64_Loop(_values64[i], spanLoop, 4);
                if (lzcnt != loop)
                    throw new InvalidOperationException($"64-write: {_values64[i]}, expected {loop}, got {lzcnt}");
                if (!spanLzcnt.Slice(4, lzcnt).SequenceEqual(spanLoop.Slice(4, loop)))
                    throw new InvalidOperationException($"64-write: spans are different");
            }
        }

        [BenchmarkCategory("Length")]
        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void ComputeVarintLength32_Loop()
        {
            var arr = _values32;
            for (int i = 0; i < arr.Length; i++)
                ComputeVarintLength32_Loop(arr[i]);
        }

        [BenchmarkCategory("Length")]
        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void ComputeVarintLength64_Loop()
        {
            var arr = _values64;
            for (int i = 0; i < arr.Length; i++)
                ComputeVarintLength64_Loop(arr[i]);
        }

        [BenchmarkCategory("Length")]
        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void ComputeVarintLength32_Lzcnt()
        {
            var arr = _values32;
            for (int i = 0; i < arr.Length; i++)
                ComputeVarintLength32_Lzcnt(arr[i]);
        }

        [BenchmarkCategory("Length")]
        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void ComputeVarintLength64_Lzcnt()
        {
            var arr = _values64;
            for (int i = 0; i < arr.Length; i++)
                ComputeVarintLength64_Lzcnt(arr[i]);
        }

        [BenchmarkCategory("Encode")]
        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void Encode32_Loop()
        {
            var arr = _values32;
            Span<byte> span = stackalloc byte[16];
            for (int i = 0; i < arr.Length; i++)
                WriteVarint32_Loop(arr[i], span, 4);
        }

        [BenchmarkCategory("Encode")]
        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void Encode32_Loop_PreNext()
        {
            var arr = _values32;
            Span<byte> span = stackalloc byte[16];
            for (int i = 0; i < arr.Length; i++)
                WriteVarint32_Loop_PreNext(arr[i], span, 4);
        }

        [BenchmarkCategory("Encode")]
        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void Encode32_Loop_Ptr()
        {
            var arr = _values32;
            Span<byte> span = stackalloc byte[16];
            for (int i = 0; i < arr.Length; i++)
                WriteVarint32_Loop_Ptr(arr[i], span, 4);
        }

        [BenchmarkCategory("Encode")]
        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void Encode32_Loop_Ptr_PreNext()
        {
            var arr = _values32;
            Span<byte> span = stackalloc byte[16];
            for (int i = 0; i < arr.Length; i++)
                WriteVarint32_Loop_Ptr_PreNext(arr[i], span, 4);
        }

        [BenchmarkCategory("Encode")]
        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void Encode32_Lzcnt()
        {
            var arr = _values32;
            Span<byte> span = stackalloc byte[16];
            for (int i = 0; i < arr.Length; i++)
                WriteVarint32_Lzcnt(arr[i], span, 4);
        }

        [BenchmarkCategory("Encode")]
        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void Encode32_Lzcnt_Ptr()
        {
            var arr = _values32;
            Span<byte> span = stackalloc byte[16];
            for (int i = 0; i < arr.Length; i++)
                WriteVarint32_Lzcnt_Ptr(arr[i], span, 4);
        }

        [BenchmarkCategory("Encode")]
        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void Encode64_Loop()
        {
            var arr = _values64;
            Span<byte> span = stackalloc byte[16];
            for (int i = 0; i < arr.Length; i++)
                WriteVarint64_Loop(arr[i], span, 4);
        }

        [BenchmarkCategory("Encode")]
        [Benchmark(OperationsPerInvoke = LOOP_SIZE)]
        public void Encode64_Lzcnt()
        {
            var arr = _values64;
            Span<byte> span = stackalloc byte[16];
            for (int i = 0; i < arr.Length; i++)
                WriteVarint64_Lzcnt(arr[i], span, 4);
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

        internal int WriteVarint64_Loop(ulong value, Span<byte> span, int index)
        {
            int count = 0;
            do
            {
                span[index++] = (byte)((value & 0x7F) | 0x80);
                count++;
            } while ((value >>= 7) != 0);
            span[index - 1] &= 0x7F;
            return count;
        }

        internal int WriteVarint32_Loop(uint value, Span<byte> span, int index)
        {
            int origIndex = index;
            do
            {
                span[index++] = (byte)((value & 0x7F) | 0x80);
            } while ((value >>= 7) != 0);
            span[index - 1] &= 0x7F;
            return index - origIndex;
        }

        internal int WriteVarint32_Loop_PreNext(uint value, Span<byte> span, int index)
        {
            int origIndex = index;
            uint next;
            while ((next = value >> 7) != 0)
            {
                span[index++] = (byte)((value & 0x7F) | 0x80);
                value = next;
            }
            span[index] = (byte)value;
            return 1 + index - origIndex;
        }

        internal unsafe int WriteVarint32_Loop_Ptr(uint value, Span<byte> span, int index)
        {
            fixed (byte* spanPtr = span)
            {
                byte* origPtr = spanPtr + index, ptr = origPtr;
                do
                {
                    *ptr++ = (byte)((value & 0x7F) | 0x80);
                } while ((value >>= 7) != 0);
                *(ptr - 1) &= 0x7F;
                return (int)(ptr - origPtr);
            }
        }

        internal unsafe int WriteVarint32_Loop_Ptr_PreNext(uint value, Span<byte> span, int index)
        {
            fixed (byte* spanPtr = span)
            {
                byte* origPtr = spanPtr + index, ptr = origPtr;
                uint next;
                while ((next = value >> 7) != 0)
                {
                    *ptr++ = (byte)((value & 0x7F) | 0x80);
                    value = next;
                }
                *ptr = (byte)value;
                return 1 + (int)(ptr - origPtr);
            }
        }

        internal int WriteVarint32_Lzcnt(uint value, Span<byte> span, int index)
        {
            if ((value & ~(uint)0x7F) == 0)
            {
                span[index] = (byte)value;
                return 1;
            }
            switch ((31 - BitOperations.LeadingZeroCount(value | 1)) / 7)
            {
                case 1:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 7);
                    return 2;
                case 2:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 7) & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 14);
                    return 3;
                case 3:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 7) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 14) & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 21);
                    return 4;
                default:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 7) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 14) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 21) & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 28);
                    return 5;
            }
        }

        internal unsafe int WriteVarint32_Lzcnt_Ptr(uint value, Span<byte> span, int index)
        {
            fixed (byte* spanPtr = span)
            {
                byte* ptr = spanPtr + index;
                switch ((31 - BitOperations.LeadingZeroCount(value | 1)) / 7)
                {
                    case 0:
                        *ptr = (byte)value;
                        return 1;
                    case 1:
                        *ptr++ = (byte)((value & 0x7F) | 0x80);
                        *ptr = (byte)(value >> 7);
                        return 2;
                    case 2:
                        *ptr++ = (byte)((value & 0x7F) | 0x80);
                        *ptr++ = (byte)(((value >> 7) & 0x7F) | 0x80);
                        *ptr = (byte)(value >> 14);
                        return 3;
                    case 3:
                        *ptr++ = (byte)((value & 0x7F) | 0x80);
                        *ptr++ = (byte)(((value >> 7) & 0x7F) | 0x80);
                        *ptr++ = (byte)(((value >> 14) & 0x7F) | 0x80);
                        *ptr = (byte)(value >> 21);
                        return 4;
                    default:
                        *ptr++ = (byte)((value & 0x7F) | 0x80);
                        *ptr++ = (byte)(((value >> 7) & 0x7F) | 0x80);
                        *ptr++ = (byte)(((value >> 14) & 0x7F) | 0x80);
                        *ptr++ = (byte)(((value >> 21) & 0x7F) | 0x80);
                        *ptr = (byte)(value >> 28);
                        return 5;
                }
            }
        }

        internal int WriteVarint64_Lzcnt(ulong value, Span<byte> span, int index)
        {
            switch ((63 - BitOperations.LeadingZeroCount(value | 1)) / 7)
            {
                case 0:
                    span[index] = (byte)value;
                    return 1;
                case 1:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 7);
                    return 2;
                case 2:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 7) & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 14);
                    return 3;
                case 3:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 7) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 14) & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 21);
                    return 4;
                case 4:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 7) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 14) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 21) & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 28);
                    return 5;
                case 5:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 7) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 14) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 21) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 28) & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 35);
                    return 6;
                case 6:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 7) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 14) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 21) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 28) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 35) & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 42);
                    return 7;
                case 7:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 7) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 14) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 21) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 28) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 35) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 42) & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 49);
                    return 8;
                case 8:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 7) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 14) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 21) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 28) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 35) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 42) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 49) & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 56);
                    return 9;
                default:
                    span[index++] = (byte)((value & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 7) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 14) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 21) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 28) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 35) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 42) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 49) & 0x7F) | 0x80);
                    span[index++] = (byte)(((value >> 56) & 0x7F) | 0x80);
                    span[index] = (byte)(value >> 63);
                    return 10;
            }
        }
    }
}
#endif