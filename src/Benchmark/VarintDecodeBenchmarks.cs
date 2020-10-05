#if INTRINSICS
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Benchmark
{
    [SimpleJob(RuntimeMoniker.Net472), SimpleJob(RuntimeMoniker.NetCoreApp31), SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class VarintDecodeBenchmarks
    {
        readonly byte[] _payload = new byte[16 * 1024];
        const int VARINTS_IN_PAYLOAD = 3364;
        [GlobalSetup]
        public void Setup()
        {

            var encoder = new VarintEncodeBenchmarks();
            Span<byte> span = _payload;

            var rand = new Random(12345);

            int offset = 0, count = 0;
            int remaining = span.Length;
            while (remaining >= 16)
            {
                uint val = (uint)rand.Next();
                int bytes = encoder.WriteVarint32_Loop_PreNext(val, span, offset);

                int readBytes = default;
                uint readValue = default;
                void Verify(string name)
                {
                    if (readBytes != bytes || readValue != val)
                    {
                        var hex = BitConverter.ToString(_payload, offset, bytes);
                        throw new InvalidOperationException($"[{name}, {count}:{offset}] expected {val}/{bytes}; got {readValue}/{readBytes}; {hex}");
                    }
                }

                // verify decoders
                readBytes = ParseVarintUInt32_Baseline(span, offset, out readValue);
                Verify(nameof(ParseVarintUInt32_Baseline));

                readBytes = ParseVarintUInt32_Baseline_Ptr(span, offset, out readValue);
                Verify(nameof(ParseVarintUInt32_Baseline_Ptr));

                readBytes = ParseVarintUInt32_Loop(span, offset, out readValue);
                Verify(nameof(ParseVarintUInt32_Loop));

                readBytes = ParseVarintUInt32_MoveMask(span, offset, out readValue);
                Verify(nameof(ParseVarintUInt32_MoveMask));

                readBytes = ParseVarintUInt32_MoveMask_Ptr(span, offset, out readValue);
                Verify(nameof(ParseVarintUInt32_MoveMask_Ptr));

                // progress
                offset += bytes;
                remaining -= bytes;
                count++;
            }
            if (count != VARINTS_IN_PAYLOAD)
                throw new InvalidOperationException($"Expected {VARINTS_IN_PAYLOAD}, got {count}");
        }

        [Benchmark(OperationsPerInvoke = VARINTS_IN_PAYLOAD, Baseline = true)]
        public void Baseline()
        {
            ReadOnlySpan<byte> span = _payload;
            int offset = 0;
            for (int i = 0; i < VARINTS_IN_PAYLOAD; i++)
            {
                int bytes = ParseVarintUInt32_Baseline(span, offset, out _);
                offset += bytes;
            }
        }

        [Benchmark(OperationsPerInvoke = VARINTS_IN_PAYLOAD)]
        public void Baseline_Ptr()
        {
            ReadOnlySpan<byte> span = _payload;
            int offset = 0;
            for (int i = 0; i < VARINTS_IN_PAYLOAD; i++)
            {
                int bytes = ParseVarintUInt32_Baseline_Ptr(span, offset, out _);
                offset += bytes;
            }
        }


        [Benchmark(OperationsPerInvoke = VARINTS_IN_PAYLOAD)]
        public void Loop()
        {
            ReadOnlySpan<byte> span = _payload;
            int offset = 0;
            for (int i = 0; i < VARINTS_IN_PAYLOAD; i++)
            {
                int bytes = ParseVarintUInt32_Loop(span, offset, out _);
                offset += bytes;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static int ParseVarintUInt32_Loop(ReadOnlySpan<byte> span, int offset, out uint value)
        {
            uint b, result = 0;
            int shift = 0, origOffset = offset;
            while (((b = span[offset++]) & 0x80) != 0)
            {
                result |= ((b & 0x7F) << shift);
                shift += 7;
            }
            value = result | ((b & 0x7F) << shift);
            return offset - origOffset;
        }

        [Benchmark(OperationsPerInvoke = VARINTS_IN_PAYLOAD)]
        public void MoveMask()
        {
            ReadOnlySpan<byte> span = _payload;
            int offset = 0;
            for (int i = 0; i < VARINTS_IN_PAYLOAD; i++)
            {
                int bytes = ParseVarintUInt32_MoveMask(span, offset, out _);
                offset += bytes;
            }
        }

        [Benchmark(OperationsPerInvoke = VARINTS_IN_PAYLOAD)]
        public void MoveMask_Ptr()
        {
            ReadOnlySpan<byte> span = _payload;
            int offset = 0;
            for (int i = 0; i < VARINTS_IN_PAYLOAD; i++)
            {
                int bytes = ParseVarintUInt32_MoveMask_Ptr(span, offset, out _);
                offset += bytes;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static int ParseVarintUInt32_MoveMask(ReadOnlySpan<byte> span, int offset, out uint value)
        {
            value = span[offset];
            return (value & 0x80) == 0 ? 1 : ParseVarintUInt32_MoveMask_Impl(span, offset, out value);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        static int ParseVarintUInt32_MoveMask_Impl(ReadOnlySpan<byte> span, int offset, out uint value)
        {
            var stopbits = ~(uint)Sse2.MoveMask(MemoryMarshal.AsRef<Vector128<byte>>(span.Slice(offset)));
            switch (BitOperations.TrailingZeroCount(stopbits))
            {
                case 1:
                    value = (uint)(
                        (span[offset++] & 0x7F) |
                        (span[offset] << 7)
                        );
                    return 2;
                case 2:
                    value = (uint)(
                        (span[offset++] & 0x7F) |
                        ((span[offset++] & 0x7F) << 7) |
                        (span[offset] << 14)
                        );
                    return 3;
                case 3:
                    value = (uint)(
                        (span[offset++] & 0x7F) |
                        ((span[offset++] & 0x7F) << 7) |
                        ((span[offset++] & 0x7F) << 14) |
                        (span[offset] << 21)
                        );
                    return 4;
                case 4:
                    value = (uint)(
                        (span[offset++] & 0x7F) |
                        ((span[offset++] & 0x7F) << 7) |
                        ((span[offset++] & 0x7F) << 14) |
                        ((span[offset++] & 0x7F) << 21) |
                        (span[offset] << 28)
                        );
                    return 5;
                default:
                    ThrowOverflow();
                    value = default;
                    return -1;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static int ParseVarintUInt32_MoveMask_Ptr(ReadOnlySpan<byte> span, int offset, out uint value)
        {
            value = span[offset];
            return (value & 0x80) == 0 ? 1 : ParseVarintUInt32_MoveMask_Ptr_Impl(span, offset, out value);
            
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        static unsafe int ParseVarintUInt32_MoveMask_Ptr_Impl(ReadOnlySpan<byte> span, int offset, out uint value)
        {
            fixed (byte* bPtr = span)
            {
                var ptr = bPtr + offset;

                var stopbits = ~(uint)Sse2.MoveMask(*(Vector128<byte>*)ptr);
                switch (BitOperations.TrailingZeroCount(stopbits))
                {
                    case 1:
                        value = (uint)(
                            (*ptr++ & 0x7F) |
                            (*ptr << 7)
                            );
                        return 2;
                    case 2:
                        value = (uint)(
                            (*ptr++ & 0x7F) |
                            ((*ptr++ & 0x7F) << 7) |
                            (*ptr << 14)
                            );
                        return 3;
                    case 3:
                        value = (uint)(
                            (*ptr++ & 0x7F) |
                            ((*ptr++ & 0x7F) << 7) |
                            ((*ptr++ & 0x7F) << 14) |
                            (*ptr << 21)
                            );
                        return 4;
                    case 4:
                        value = (uint)(
                            (*ptr++ & 0x7F) |
                            ((*ptr++ & 0x7F) << 7) |
                            ((*ptr++ & 0x7F) << 14) |
                            ((*ptr++ & 0x7F) << 21) |
                            (*ptr << 28)
                            );
                        return 5;
                    default:
                        ThrowOverflow();
                        value = default;
                        return -1;
                }
            }
        }


        // basline implementation from code snapshot
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static int ParseVarintUInt32_Baseline(ReadOnlySpan<byte> span, int offset, out uint value)
        {
            value = span[offset];
            return (value & 0x80) == 0 ? 1 : ParseVarintUInt32_Baseline_Impl(span.Slice(offset), ref value);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        static int ParseVarintUInt32_Baseline_Impl(ReadOnlySpan<byte> span, ref uint value)
        {
            uint chunk = span[1];
            value = (value & 0x7F) | (chunk & 0x7F) << 7;
            if ((chunk & 0x80) == 0) return 2;

            chunk = span[2];
            value |= (chunk & 0x7F) << 14;
            if ((chunk & 0x80) == 0) return 3;

            chunk = span[3];
            value |= (chunk & 0x7F) << 21;
            if ((chunk & 0x80) == 0) return 4;

            chunk = span[4];
            value |= chunk << 28; // can only use 4 bits from this chunk
            if ((chunk & 0xF0) == 0) return 5;

            ThrowOverflow();
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        internal static unsafe int ParseVarintUInt32_Baseline_Ptr(ReadOnlySpan<byte> span, int offset, out uint value)
        {
            fixed (byte* ptr = &span[offset])
            {
                value = *ptr;
                return (value & 0x80) == 0 ? 1 : Impl(ptr, ref value);

                static int Impl(byte* span, ref uint value)
                {
                    uint chunk = span[1];
                    value = (value & 0x7F) | (chunk & 0x7F) << 7;
                    if ((chunk & 0x80) == 0) return 2;

                    chunk = span[2];
                    value |= (chunk & 0x7F) << 14;
                    if ((chunk & 0x80) == 0) return 3;

                    chunk = span[3];
                    value |= (chunk & 0x7F) << 21;
                    if ((chunk & 0x80) == 0) return 4;

                    chunk = span[4];
                    value |= chunk << 28; // can only use 4 bits from this chunk
                    if ((chunk & 0xF0) == 0) return 5;

                    ThrowOverflow();
                    return 0;
                }
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowOverflow()
        {
            throw new OverflowException();
        }
    }
}
#endif