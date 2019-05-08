using BenchmarkDotNet.Attributes;
using System;
using System.Buffers.Binary;

namespace Benchmark
{
    // Chunk_Slow2 is winner
    public class ParseVarint
    {
        private const int OperationsPerInvoke = 100;
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong Chunk_Fast()
        {
            ulong acc = 0;
            ReadOnlySpan<byte> span = testDatas[ByteCount - 1];
            for(int i = 0; i < OperationsPerInvoke; i++)
            {
                TryParseUInt32VarintFast(span, out var val);
                acc += val;
            }
            return acc;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong Chunk_Auto()
        {
            ulong acc = 0;
            ReadOnlySpan<byte> span = testDatas[ByteCount - 1];
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                TryParseUInt32Varint(span, out var val);
                acc += val;
            }
            return acc;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong Chunk_Slow()
        {
            ulong acc = 0;
            ReadOnlySpan<byte> span = testDatas[ByteCount - 1];
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                TryParseUInt32VarintSlow(span, out var val);
                acc += val;
            }
            return acc;
        }
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong Chunk_Slow2()
        {
            ulong acc = 0;
            ReadOnlySpan<byte> span = testDatas[ByteCount - 1];
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                TryParseUInt32VarintSlow2(span, out var val);
                acc += val;
            }
            return acc;
        }
        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong Chunk_Slow3()
        {
            ulong acc = 0;
            ReadOnlySpan<byte> span = testDatas[ByteCount - 1];
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                acc += TryParseUInt32VarintSlow3(span);
            }
            return acc;
        }

        internal static int TryParseUInt32Varint(ReadOnlySpan<byte> span, out uint value)
        {
            return span.Length >= 5
                ? TryParseUInt32VarintFast(span, out value)
                : TryParseUInt32VarintSlow(span, out value);
        }
        private static int TryParseUInt32VarintFast(ReadOnlySpan<byte> span, out uint value)
        {
            uint u32 = BinaryPrimitives.ReadUInt32LittleEndian(span);
            // we could have flags at 32,24,16,8
            // mask to just those (0x80808080)
            // now >> by 31, 23, 15, 7 and mask accordingly

            uint msbs = (u32 & 0x80808080);
            // keep in mind that we're working **little** endian; first byte is in 0xFF
            // and the fourth byte is in 0xFF000000
            switch (((msbs >> 28) | (msbs >> 21) | (msbs >> 14) | (msbs >> 7)) & 0x0F)
            {
                default:
                    value = 0;
                    return 0;
                case 0:
                case 2:
                case 4:
                case 6:
                case 8:
                case 10:
                case 12:
                case 14:
                    // ***0
                    value = u32 & 0x7F;
                    return 1;
                case 1:
                case 5:
                case 9:
                case 13:
                    // **01
                    value = (u32 & 0x7F) | ((u32 & 0x7F00) >> 1);
                    return 2;
                case 3:
                case 11:
                    // *011
                    value = (u32 & 0x7F) | ((u32 & 0x7F00) >> 1) | ((u32 & 0x7F0000) >> 2);
                    return 3;
                case 7:
                    // 0111
                    value = (u32 & 0x7F) | ((u32 & 0x7F00) >> 1) | ((u32 & 0x7F0000) >> 2) | ((u32 & 0x7F000000) >> 3);
                    return 4;
                case 15:
                    // 1111
                    var final = span[4];
                    if ((final & 0xF0) != 0) ThrowOverflow(null);
                    value = (u32 & 0x7F) | ((u32 & 0x7F00) >> 1) | ((u32 & 0x7F0000) >> 2) | ((u32 & 0x7F000000) >> 3) | (uint)(final << 28);
                    return 5;
            }
        }
        private static void ThrowOverflow(object obj) => throw new OverflowException();

        private static int TryParseUInt32VarintSlow2(ReadOnlySpan<byte> span, out uint value)
        {
            value = span[0];
            return (value & 0x80) == 0 ? 1 : TryParseUInt32VarintSlow2Tail(span, ref value);
        }
        private static int TryParseUInt32VarintSlow2Tail(ReadOnlySpan<byte> span, ref uint value)
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

            ThrowOverflow(null);
            return 0;
        }

        private static uint TryParseUInt32VarintSlow3(ReadOnlySpan<byte> span)
        {
            uint value = span[0];
            return (value & 0x80) == 0 ? value : TryParseUInt32VarintSlow3Tail(span, value);
        }
        private static uint TryParseUInt32VarintSlow3Tail(ReadOnlySpan<byte> span, uint value)
        {
            uint chunk = span[1];
            value = (value & 0x7F) | (chunk & 0x7F) << 7;
            if ((chunk & 0x80) == 0) return value;

            chunk = span[2];
            value |= (chunk & 0x7F) << 14;
            if ((chunk & 0x80) == 0) return value;

            chunk = span[3];
            value |= (chunk & 0x7F) << 21;
            if ((chunk & 0x80) == 0) return value;

            chunk = span[4];
            value |= chunk << 28; // can only use 4 bits from this chunk
            if ((chunk & 0xF0) == 0) return value;

            ThrowOverflow(null);
            return 0;
        }

        private static int TryParseUInt32VarintSlow(ReadOnlySpan<byte> span, out uint value)
        {
            value = span[0];
            if ((value & 0x80) == 0) return 1;
            value &= 0x7F;

            uint chunk = span[1];
            value |= (chunk & 0x7F) << 7;
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

            ThrowOverflow(null);
            return 0;
        }

        [Params(1,2,3,4,5)]
        public int ByteCount { get; set; }

        private readonly static byte[][] testDatas
            = new byte[][]
            {
                new byte[] { 0x04, 0xFF, 0x03, 0x81, 0x12, },
                new byte[] { 0xF0, 0x04, 0xFF, 0x03, 0x81, },
                new byte[] { 0xF0, 0xF0, 0x04, 0xFF, 0x03, },
                new byte[] { 0xF0, 0xF0, 0xF0, 0x04, 0xFF, },
                new byte[] { 0xF0, 0xF0, 0xF0, 0xF0, 0x04, },
            };
    }
}
