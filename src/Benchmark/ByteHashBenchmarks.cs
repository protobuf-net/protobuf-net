using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
#if INTRINSICS
using System.Runtime.Intrinsics;
#endif

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.Net472), SimpleJob(RuntimeMoniker.Net70), MemoryDiagnoser]
    public class ByteHashBenchmarks
    {
        const int MAXLENGTH = 12451;
        private readonly byte[] _bytes = new byte[MAXLENGTH];
        private ReadOnlyMemory<byte> _payload;

        public ByteHashBenchmarks() => new Random(1231).NextBytes(_bytes);

        [Params(0, 7, 8, 127, 128, 1024, MAXLENGTH)]
        public int Length { get; set; }

        [GlobalSetup]
        public void Setup() => _payload = new ReadOnlyMemory<byte>(_bytes, 0, Length);

        [Benchmark(Baseline = true)]
        public void Basic() => GetHashCodeBasic(_payload);

        [Benchmark]
        public void U8() => GetHashCodeU8(_payload);

        [Benchmark]
        public void NumVec() => GetHashCodeNumVec(_payload);

        [Benchmark]
        public void IntrVec()
        {
#if INTRINSICS
            GetHashCodeIntrVec(_payload);
#else
            GetHashCodeNumVec(_payload);
#endif
        }

        private static int GetHashCodeBasic(ReadOnlyMemory<byte> obj)
        {
            var b8 = obj.Span;
            var hash = b8.Length;
            if (hash == 0) return 0;

            foreach (var value in b8)
            {
                hash = 37 * hash + value;
            }
            return hash;
        }

        private static int GetHashCodeU8(ReadOnlyMemory<byte> obj)
        {
            var b8 = obj.Span;
            var hash = b8.Length;
            if (hash == 0) return 0;

            if (b8.Length >= sizeof(ulong))
            {
                var b64 = MemoryMarshal.Cast<byte, ulong>(b8);
                foreach (var value in b64)
                {
                    hash = 37 * hash + value.GetHashCode();
                }
                b8 = b8.Slice(b64.Length * sizeof(ulong));
            }
            // mop up anything at the end
            foreach (var value in b8)
            {
                hash = 37 * hash + value;
            }
            return hash;
        }

        private static int GetHashCodeNumVec(ReadOnlyMemory<byte> obj)
        {
            var b8 = obj.Span;
            var hash = b8.Length;
            if (hash == 0) return 0;

            if (Vector.IsHardwareAccelerated && b8.Length >= (Vector<int>.Count * sizeof(int)))
            {
                var b32Vec = MemoryMarshal.Cast<byte, Vector<int>>(b8);
                var rollup = Vector<int>.Zero;
                foreach (ref readonly var value in b32Vec)
                {
                    rollup = Vector.Add(Vector.Multiply(37, rollup), value);
                }
#if NET6_0_OR_GREATER
                hash = 37 * hash + Vector.Sum(rollup);
#else
                hash = 37 * hash + Vector.Dot(rollup, Vector<int>.One);
#endif
                b8 = b8.Slice(b32Vec.Length * Vector<int>.Count * sizeof(int));
            }
            if (b8.Length >= sizeof(ulong))
            {
                var b64 = MemoryMarshal.Cast<byte, ulong>(b8);
                foreach (var value in b64)
                {
                    hash = 37 * hash + value.GetHashCode();
                }
                b8 = b8.Slice(b64.Length * sizeof(ulong));
            }
            // mop up anything at the end
            foreach (var value in b8)
            {
                hash = 37 * hash + value;
            }
            return hash;
        }

#if INTRINSICS
        private static int GetHashCodeIntrVec(ReadOnlyMemory<byte> obj)
        {
            var b8 = obj.Span;
            var hash = b8.Length;
            if (hash == 0) return 0;

            const int Vector256Bytes = 256 / sizeof(byte);
            if (Vector256.IsHardwareAccelerated && b8.Length >= Vector256Bytes)
            {
                var b32Vec = MemoryMarshal.Cast<byte, Vector256<int>>(b8);
                var rollup = Vector256<int>.Zero;
                foreach (ref readonly var value in b32Vec)
                {
                    rollup = 37 * rollup + value;
                }
                hash = 37 * Vector256.Sum(rollup);
                b8 = b8.Slice(b32Vec.Length * Vector256Bytes);
            }
            if (b8.Length >= sizeof(ulong))
            {
                var b64 = MemoryMarshal.Cast<byte, ulong>(b8);
                foreach (var value in b64)
                {
                    hash = 37 * hash + value.GetHashCode();
                }
                b8 = b8.Slice(b64.Length * sizeof(ulong));
            }
            // mop up anything at the end
            foreach (var value in b8)
            {
                hash = 37 * hash + value;
            }
            return hash;
        }
#endif
    }
}
