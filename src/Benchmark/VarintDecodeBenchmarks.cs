#if INTRINSICS
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System;
using System.Runtime.CompilerServices;

namespace Benchmark
{
    [CoreJob]
    public class VarintDecodeBenchmarks
    {
        byte[] _payload = new byte[16 * 1024];
        const int VARINTS_IN_PAYLOAD = 3366;
        [GlobalSetup]
        public void Setup()
        {

            var encoder = new VarintEncodeBenchmarks();
            Span<byte> span = _payload;

            var rand = new Random(12345);

            int offset = 0, count = 0;
            int remaining = span.Length;
            while (remaining >= 5)
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

                readBytes = ParseVarintUInt32_Loop(span, offset, out readValue);
                Verify(nameof(ParseVarintUInt32_Loop));

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
            for(int i = 0; i < VARINTS_IN_PAYLOAD; i++)
            {
                int bytes = ParseVarintUInt32_Baseline(span, offset, out _);
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

        // basline implementation from code snapshot
        internal static int ParseVarintUInt32_Baseline(ReadOnlySpan<byte> span, int offset, out uint value)
        {
            value = span[offset];
            return (value & 0x80) == 0 ? 1 : ParseVarintUInt32Tail(span.Slice(offset), ref value);
        }
        private static int ParseVarintUInt32Tail(ReadOnlySpan<byte> span, ref uint value)
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowOverflow()
        {
            throw new OverflowException();
        }
    }
}
#endif