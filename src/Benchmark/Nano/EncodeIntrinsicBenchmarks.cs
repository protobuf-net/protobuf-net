using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
#if NETCOREAPP3_1_OR_GREATER
using System.Runtime.Intrinsics.X86;
#endif

namespace Benchmark.Nano
{
    [SimpleJob(RuntimeMoniker.Net60)]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class EncodeIntrinsicBenchmarks
    {
        private byte[] _source = new byte[64];
        private int _length = 1, _byteOffset;
        private uint _value;
        private Random rand = new Random(12345);
        private void BuildSource()
        {
            _value = 0;
            for (int i = 0; i < _length; i++)
            {
                _value |= ((uint)i) << (7 * i);
            }

            var writer = GetWriter();
            writer.Reset();
            writer.WriteVarintUInt32_Unoptimized(_value);
            var expected = writer.GetHexAndReset();

            writer.WriteVarintUInt32_WriteIntrinsic(_value);
            var actual = writer.GetHexAndReset();
            if (actual != expected) throw new InvalidOperationException($"Encode failure {nameof(writer.WriteVarintUInt32_WriteIntrinsic)}: {expected} vs {actual}");

            writer.WriteVarintUInt32_Switched(_value);
            actual = writer.GetHexAndReset();
            if (actual != expected) throw new InvalidOperationException($"Encode failure {nameof(writer.WriteVarintUInt32_Switched)}: {expected} vs {actual}");

            writer.WriteVarintUInt32_WithZeroHighBits(_value);
            actual = writer.GetHexAndReset();
            if (actual != expected) throw new InvalidOperationException($"Encode failure {nameof(writer.WriteVarintUInt32_WithZeroHighBits)}: {expected} vs {actual}");

            writer.WriteVarintUInt32_WithZeroHighBits2(_value);
            actual = writer.GetHexAndReset();
            if (actual != expected) throw new InvalidOperationException($"Encode failure {nameof(writer.WriteVarintUInt32_WithZeroHighBits2)}: {expected} vs {actual}");

            writer.WriteVarintUInt32_ShiftedMasks(_value);
            actual = writer.GetHexAndReset();
            if (actual != expected) throw new InvalidOperationException($"Encode failure {nameof(writer.WriteVarintUInt32_ShiftedMasks)}: {expected} vs {actual}");

            var actualLen = BenchmarkWriter.Measure_Leq(_value);
            if (actualLen != _length) throw new InvalidOperationException($"Measure failure {nameof(writer.Measure_Leq)}: {_length} vs {actualLen}");

            actualLen = BenchmarkWriter.Measure_BitTest(_value);
            if (actualLen != _length) throw new InvalidOperationException($"Measure failure {nameof(writer.Measure_BitTest)}: {_length} vs {actualLen}");

            actualLen = BenchmarkWriter.Measure_LzcntDiv(_value);
            if (actualLen != _length) throw new InvalidOperationException($"Measure failure {nameof(writer.Measure_LzcntDiv)}: {_length} vs {actualLen}");

            actualLen = BenchmarkWriter.Measure_LzcntMulShift(_value);
            if (actualLen != _length) throw new InvalidOperationException($"Measure failure {nameof(writer.Measure_LzcntMulShift)}: {_length} vs {actualLen}");
        }

        private const int OperationsPerInvoke = 32 * 1024;

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke, Baseline = true)]
        [BenchmarkCategory("Measure32")]
        public void Measure_Leq()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                BenchmarkWriter.Measure_Leq(_value);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Measure32")]
        public void Measure_BitTest()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                BenchmarkWriter.Measure_BitTest(_value);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Measure32")]
        public void Measure_LzcntDiv()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                BenchmarkWriter.Measure_LzcntDiv(_value);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Measure32")]
        public void Measure_LzcntMulShift()
        {
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                BenchmarkWriter.Measure_LzcntMulShift(_value);
            }
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke, Baseline = true)]
        [BenchmarkCategory("Encode32")]
        public int Unoptimized()
        {
            var writer = GetWriter();
            int index = writer.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                writer.Index = index; // reset for multiple reads on same data
                writer.WriteVarintUInt32_Unoptimized(_value);
            }
            return writer.Count;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Encode32")]
        public int Switched()
        {
            var writer = GetWriter();
            int index = writer.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                writer.Index = index; // reset for multiple reads on same data
                writer.WriteVarintUInt32_Switched(_value);
            }
            return writer.Count;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Encode32")]
        public int Intrinsic()
        {
            var writer = GetWriter();
            int index = writer.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                writer.Index = index; // reset for multiple reads on same data
                writer.WriteVarintUInt32_WriteIntrinsic(_value);
            }
            return writer.Count;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Encode32")]
        public int WithZeroHighBits()
        {
            var writer = GetWriter();
            int index = writer.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                writer.Index = index; // reset for multiple reads on same data
                writer.WriteVarintUInt32_WithZeroHighBits(_value);
            }
            return writer.Count;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Encode32")]
        public int WithZeroHighBits2()
        {
            var writer = GetWriter();
            int index = writer.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                writer.Index = index; // reset for multiple reads on same data
                writer.WriteVarintUInt32_WithZeroHighBits2(_value);
            }
            return writer.Count;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        [BenchmarkCategory("Encode32")]
        public int ShiftedMasks()
        {
            var writer = GetWriter();
            int index = writer.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                writer.Index = index; // reset for multiple reads on same data
                writer.WriteVarintUInt32_ShiftedMasks(_value);
            }
            return writer.Count;
        }

        private BenchmarkWriter GetWriter() => new BenchmarkWriter(_source, _byteOffset);

        [Params(1, 2, 3, 4, 5)]
        public int VarintLen
        {
            get => _length;
            set { _length = value; BuildSource(); }
        }

        [Params(0)] //, 3)] // see whether alignment matters (it doesn't)
        public int ByteOffset
        {
            get => _byteOffset;
            set { _byteOffset = value; BuildSource(); }
        }

        private ref struct BenchmarkWriter
        {
#if NETCOREAPP3_1_OR_GREATER
            const MethodImplOptions HotPath = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
#else
            const MethodImplOptions HotPath = MethodImplOptions.AggressiveInlining;
#endif
            
            byte[] _underlying;
            Span<byte> _buffer;
            int _index, _end, _start;

            public int Index
            {
                get => _index;
                set => _index = value;
            }
            public BenchmarkWriter(byte[] target, int offset)
            {
                _end = target.Length;
                _index = _start = offset;
                _buffer = _underlying = target;
            }

            public void Reset()
            {
                Array.Clear(_underlying, 0, _underlying.Length);
                _index = _start;
            }


            /// <summary>
            /// Write an unsigned integer with varint encoding
            /// </summary>
            [MethodImpl(HotPath)]
            public void WriteVarintUInt64(ulong value)
            {
                if ((value >> 32) == 0) WriteVarintUInt32_Unoptimized((uint)value);
                else WriteVarintUInt64Full(value);
            }

            /// <summary>
            /// Write an unsigned integer with varint encoding
            /// </summary>
            [MethodImpl(HotPath)]
            public void WriteVarintUInt32_Unoptimized(uint value)
            {
                if (_index + 5 <= _end)
                {
                    while ((value & ~0x7FU) != 0)
                    {
                        _buffer[_index++] = (byte)((value & 0x7F) | 0x80);
                        value >>= 7;
                    }
                    _buffer[_index++] = (byte)value;
                }
                else
                {
                    WriteVarintUInt32Slow(value);
                }
            }

            /// <summary>
            /// Write an unsigned integer with varint encoding
            /// </summary>
            [MethodImpl(HotPath)]
            public void WriteVarintUInt32_Switched(uint value)
            {
#if NETCOREAPP3_1_OR_GREATER
                if (_index + 5 <= _end)
                {
                    switch (((31 - (uint)BitOperations.LeadingZeroCount(value | 1)) * 37) >> 8)
                    {
                        case 0:
                            _buffer[_index++] = (byte)value;
                            return;
                        case 1:
                            _buffer[_index++] = (byte)(value | 0x80);
                            _buffer[_index++] = (byte)(value >> 7);
                            return;
                        case 2:
                            _buffer[_index++] = (byte)(value | 0x80);
                            _buffer[_index++] = (byte)((value >> 7) | 0x80);
                            _buffer[_index++] = (byte)(value >> 14);
                            return;
                        case 3:
                            _buffer[_index++] = (byte)(value | 0x80);
                            _buffer[_index++] = (byte)((value >> 7) | 0x80);
                            _buffer[_index++] = (byte)((value >> 14) | 0x80);
                            _buffer[_index++] = (byte)(value >> 21);
                            return;
                        default:
                            _buffer[_index++] = (byte)(value | 0x80);
                            _buffer[_index++] = (byte)((value >> 7) | 0x80);
                            _buffer[_index++] = (byte)((value >> 14) | 0x80);
                            _buffer[_index++] = (byte)((value >> 21) | 0x80);
                            _buffer[_index++] = (byte)(value >> 28);
                            return;
                    }
                }
                else
                {
                    WriteVarintUInt32Slow(value);
                }
#else
                WriteVarintUInt32_Unoptimized(value);
#endif
            }

            private void WriteVarintUInt64Full(ulong value)
            {
                if (_index + 5 <= _end)
                {
#if NETCOREAPP3_1_OR_GREATER
                    if (Lzcnt.X64.IsSupported)
                    {
                        var bits = 64 - Lzcnt.X64.LeadingZeroCount(value);
                        const uint HI_BIT = 0b10000000;

                        switch ((bits + 6) / 7)
                        {
                            case 0:
                            case 1:
                                _buffer[_index++] = (byte)value;
                                return;
                            case 2:
                                _buffer[_index++] = (byte)(value | HI_BIT);
                                _buffer[_index++] = (byte)(value >> 7);
                                return;
                            case 3:
                                _buffer[_index++] = (byte)(value | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                                _buffer[_index++] = (byte)(value >> 14);
                                return;
                            case 4:
                                _buffer[_index++] = (byte)(value | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                                _buffer[_index++] = (byte)(value >> 21);
                                return;
                            case 5:
                                _buffer[_index++] = (byte)(value | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                                _buffer[_index++] = (byte)(value >> 28);
                                return;
                            case 6:
                                _buffer[_index++] = (byte)(value | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 28) | HI_BIT);
                                _buffer[_index++] = (byte)(value >> 35);
                                return;
                            case 7:
                                _buffer[_index++] = (byte)(value | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 28) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 35) | HI_BIT);
                                _buffer[_index++] = (byte)(value >> 42);
                                return;
                            case 8:
                                _buffer[_index++] = (byte)(value | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 28) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 35) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 42) | HI_BIT);
                                _buffer[_index++] = (byte)(value >> 49);
                                return;
                            case 9:
                                _buffer[_index++] = (byte)(value | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 28) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 35) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 42) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 49) | HI_BIT);
                                _buffer[_index++] = (byte)(value >> 56);
                                return;
                            default:
                                _buffer[_index++] = (byte)(value | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 7) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 14) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 21) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 28) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 35) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 42) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 49) | HI_BIT);
                                _buffer[_index++] = (byte)((value >> 56) | HI_BIT);
                                _buffer[_index++] = (byte)(value >> 63);
                                return;
                        }
                    }
                    else
#endif
                    {
                        while ((value & ~0x7FUL) != 0)
                        {
                            _buffer[_index++] = (byte)((value & 0x7F) | 0x80);
                            value >>= 7;
                        }
                        _buffer[_index++] = (byte)value;
                    }
                }
                else
                {
                    WriteVarintUInt64Slow(value);
                }
            }
            [MethodImpl(MethodImplOptions.NoInlining)]
            private void WriteVarintUInt64Slow(ulong value) => throw new NotImplementedException();

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void WriteVarintUInt32Slow(ulong value) => throw new NotImplementedException();

            public string GetHexAndReset()
            {
                var hex = BitConverter.ToString(_underlying, _start, Count);
                Reset();
                return hex;
            }

            public int Count => _index - _start;

            [MethodImpl(HotPath)]
            public void WriteVarintUInt32_WriteIntrinsic(uint value)
            {
#if NETCOREAPP3_1_OR_GREATER
                if (Lzcnt.IsSupported && Bmi2.IsSupported && BitConverter.IsLittleEndian)
                {
                    if (_index + 5 <= _end)
                    {
                        var octets = (38 - Lzcnt.LeadingZeroCount(value)) / 7;
                        switch (octets)
                        {
                            case 0:
                            case 1:
                                _buffer[_index++] = (byte)value;
                                return;
                            case 2:
                                Unsafe.WriteUnaligned(ref _buffer[_index], Bmi2.ParallelBitDeposit(value, 0x7f7fU) | 0x80U);
                                break;
                            case 3:
                                Unsafe.WriteUnaligned(ref _buffer[_index], Bmi2.ParallelBitDeposit(value, 0x7f7f7fU) | 0x8080U);
                                break;
                            case 4:
                                Unsafe.WriteUnaligned(ref _buffer[_index], Bmi2.ParallelBitDeposit(value, 0x7f7f7f7fU) | 0x808080U);
                                break;
                            default:
                                Unsafe.WriteUnaligned(ref _buffer[_index], Bmi2.ParallelBitDeposit(value, 0x7f7f7f7fU) | 0x80808080U);
                                _buffer[_index + 4] = (byte)(value >> 28);
                                break;
                        }
                        _index += (int)octets;
                    }
                    else
                    {
                        WriteVarintUInt32Slow(value);
                    }
                }
                else
#endif
                {
                    WriteVarintUInt32_Unoptimized(value);
                }
            }

            [MethodImpl(HotPath)]
            public void WriteVarintUInt32_ShiftedMasks(uint value)
            {
#if NETCOREAPP3_1_OR_GREATER
                if (Lzcnt.IsSupported && Bmi2.IsSupported && BitConverter.IsLittleEndian)
                {
                    if (_index + 5 <= _end)
                    {
                        if ((value >> 7) == 0) // optimize for single octet
                        {
                            _buffer[_index++] = (byte)value;
                            return;
                        }
                        var octets = (38 - Lzcnt.LeadingZeroCount(value)) / 7;
                        if (octets < 5)
                        {
                            var shift = (4 - (int)octets) << 3;
                            Unsafe.WriteUnaligned(ref _buffer[_index], Bmi2.ParallelBitDeposit(value,
                                0x7f7f7f7fU >> shift // PDEP mask
                                ) | 0x808080U >> shift); // continuation mask
                            _index += (int)octets;
                        }
                        else
                        {
                            // 5 is the maximum we can write for a 32-bit value
                            Unsafe.WriteUnaligned(ref _buffer[_index], Bmi2.ParallelBitDeposit(value, 0x7f7f7f7fU) | 0x80808080U);
                            _buffer[_index + 4] = (byte)(value >> 28);
                            _index += 5;
                        }
                    }
                    else
                    {
                        WriteVarintUInt32Slow(value);
                    }
                }
                else
#endif
                {
                    WriteVarintUInt32_Unoptimized(value);
                }
            }

            [MethodImpl(HotPath)]
            public void WriteVarintUInt32_WithZeroHighBits2(uint value)
            {
#if NETCOREAPP3_1_OR_GREATER
                if (Lzcnt.IsSupported && Bmi2.IsSupported && BitConverter.IsLittleEndian)
                {
                    if (_index + 5 <= _end)
                    {
                        if ((value >> 7) == 0) // optimize for single octet
                        {
                            _buffer[_index++] = (byte)value;
                            return;
                        }
                        var octets = (38 - Lzcnt.LeadingZeroCount(value)) / 7;
                        if (octets < 5)
                        {
                            Unsafe.WriteUnaligned(ref _buffer[_index], Bmi2.ParallelBitDeposit(value,
                                Bmi2.ZeroHighBits(0x7f7f7f7fU, octets << 3) // PDEP mask
                                ) | Bmi2.ZeroHighBits(0x808080U, (octets - 1) << 3)); // continuation mask
                            _index += (int)octets;
                        }
                        else
                        {
                            // 5 is the maximum we can write for a 32-bit value
                            Unsafe.WriteUnaligned(ref _buffer[_index], Bmi2.ParallelBitDeposit(value, 0x7f7f7f7fU) | 0x80808080U);
                            _buffer[_index + 4] = (byte)(value >> 28);
                            _index += 5;
                        }
                    }
                    else
                    {
                        WriteVarintUInt32Slow(value);
                    }
                }
                else
#endif
                {
                    WriteVarintUInt32_Unoptimized(value);
                }
            }

            [MethodImpl(HotPath)]
            public void WriteVarintUInt32_WithZeroHighBits(uint value)
            {
#if NETCOREAPP3_1_OR_GREATER
                if (Lzcnt.IsSupported && Bmi2.IsSupported && BitConverter.IsLittleEndian)
                {
                    if (_index + 5 <= _end)
                    {
                        var octets = (38 - Lzcnt.LeadingZeroCount(value)) / 7;
                        switch (octets)
                        {
                            case 0:
                            case 1:
                                _buffer[_index++] = (byte)value;
                                return;
                            case 2:
                            case 3:
                            case 4:
                                Unsafe.WriteUnaligned(ref _buffer[_index], Bmi2.ParallelBitDeposit(value,
                                    Bmi2.ZeroHighBits(0x7f7f7f7fU, octets << 3) // PDEP mask
                                    ) | Bmi2.ZeroHighBits(0x808080U, (octets - 1) << 3)); // continuation mask
                                _index += (int)octets;
                                return;
                            default:
                                Unsafe.WriteUnaligned(ref _buffer[_index], Bmi2.ParallelBitDeposit(value, 0x7f7f7f7fU) | 0x80808080U);
                                _buffer[_index + 4] = (byte)(value >> 28);
                                _index += 5;
                                return;
                        }
                    }
                    else
                    {
                        WriteVarintUInt32Slow(value);
                    }
                }
                else
#endif
                {
                    WriteVarintUInt32_Unoptimized(value);
                }
            }

            [MethodImpl(HotPath)]
            public static uint Measure_Leq(uint value)
            {
                if (value <= 0x7F) return 1;
                if (value <= 0x3FFF) return 2;
                if (value <= 0x1FFFFF) return 3;
                if (value <= 0xFFFFFFF) return 4;
                return 5;
            }

            [MethodImpl(HotPath)]
            public static uint Measure_BitTest(uint value)
            {
                if ((value & (~0U << 7)) == 0) return 1;
                if ((value & (~0U << 14)) == 0) return 2;
                if ((value & (~0U << 21)) == 0) return 3;
                if ((value & (~0U << 28)) == 0) return 4;
                return 5;
            }

            [MethodImpl(HotPath)]
            public static uint Measure_LzcntDiv(uint value)
            {
#if NETCOREAPP3_1_OR_GREATER
                return (38 - (uint)BitOperations.LeadingZeroCount(value | 1)) / 7;
#else
                if ((value & (~0U << 7)) == 0) return 1;
                if ((value & (~0U << 14)) == 0) return 2;
                if ((value & (~0U << 21)) == 0) return 3;
                if ((value & (~0U << 28)) == 0) return 4;
                return 5;
#endif
            }

            [MethodImpl(HotPath)]
            public static uint Measure_LzcntMulShift(uint value)
            {
#if NETCOREAPP3_1_OR_GREATER
                // the | 1 ensures we treat zero as one byte; the use
                // of BitOperations rather than specific CPU instructions
                // is for CPU portability; the (x * 37) >> 8 is evil
                // hackness that works (faster) as / 7, for x in [0,85]
                return ((38 - (uint)BitOperations.LeadingZeroCount(value | 1)) * 37) >> 8;
#else
                if ((value & (~0U << 7)) == 0) return 1;
                if ((value & (~0U << 14)) == 0) return 2;
                if ((value & (~0U << 21)) == 0) return 3;
                if ((value & (~0U << 28)) == 0) return 4;
                return 5;
#endif
            }


        }
    }
}
