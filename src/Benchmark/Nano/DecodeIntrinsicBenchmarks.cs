using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
#if NETCOREAPP3_1_OR_GREATER
using System.Runtime.Intrinsics.X86;
#endif

namespace Benchmark.Nano
{
    [SimpleJob(RuntimeMoniker.Net60)]
    public class DecodeIntrinsicBenchmarks
    {
        private byte[] _source = new byte[64];
        private int _length = 1, _byteOffset;
        private Random rand = new Random(12345);
        private void BuildSource()
        {
            rand.NextBytes(_source);
            var span = new Span<byte>(_source, _byteOffset, _length);
            ulong expectedValue = 0;
            for (int i = 0; i < _length; i++)
            {
                span[i] = (byte)((i + 1) | 0x80);
                expectedValue |= ((ulong)(i + 1)) << (7 * i);
            }
            span[_length - 1] &= 0x7F; // remove the high bit from the last one

            ulong actualValue;
            var expectedIndex = _byteOffset + _length;

            var reader = GetReader();
            if ((actualValue = reader.Unoptimized()) != expectedValue) throw new InvalidOperationException($"{nameof(BenchmarkReader.Unoptimized)} value: {expectedValue} vs {actualValue}");
            if (reader.Index != expectedIndex) throw new InvalidOperationException($"{nameof(BenchmarkReader.Unoptimized)} index: {expectedIndex} vs {reader.Index}");

            reader = GetReader();
            if ((actualValue = reader.UnsafeAdd()) != expectedValue) throw new InvalidOperationException($"{nameof(BenchmarkReader.UnsafeAdd)} value: {expectedValue} vs {actualValue}");
            if (reader.Index != expectedIndex) throw new InvalidOperationException($"{nameof(BenchmarkReader.UnsafeAdd)} index: {expectedIndex} vs {reader.Index}");

            reader = GetReader();
            if ((actualValue = reader.Intrinsics()) != expectedValue) throw new InvalidOperationException($"{nameof(BenchmarkReader.Intrinsics)} value: {expectedValue} vs {actualValue}");
            if (reader.Index != expectedIndex) throw new InvalidOperationException($"{nameof(BenchmarkReader.Intrinsics)} index: {expectedIndex} vs {reader.Index}");

            reader = GetReader();
            if ((actualValue = reader.IntrinsicsSwitched()) != expectedValue) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsSwitched)} value: {expectedValue} vs {actualValue}");
            if (reader.Index != expectedIndex) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsSwitched)} index: {expectedIndex} vs {reader.Index}");

            reader = GetReader();
            if ((actualValue = reader.IntrinsicsPreferShort()) != expectedValue) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsPreferShort)} value: {expectedValue} vs {actualValue}");
            if (reader.Index != expectedIndex) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsPreferShort)} index: {expectedIndex} vs {reader.Index}");

            reader = GetReader();
            if ((actualValue = reader.IntrinsicsPreferShort2()) != expectedValue) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsPreferShort2)} value: {expectedValue} vs {actualValue}");
            if (reader.Index != expectedIndex) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsPreferShort2)} index: {expectedIndex} vs {reader.Index}");

            reader = GetReader();
            if ((actualValue = reader.IntrinsicsPreferShort3()) != expectedValue) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsPreferShort3)} value: {expectedValue} vs {actualValue}");
            if (reader.Index != expectedIndex) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsPreferShort3)} index: {expectedIndex} vs {reader.Index}");

            reader = GetReader();
            if ((actualValue = reader.IntrinsicsPreferShort4()) != expectedValue) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsPreferShort4)} value: {expectedValue} vs {actualValue}");
            if (reader.Index != expectedIndex) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsPreferShort4)} index: {expectedIndex} vs {reader.Index}");

            reader = GetReader();
            if ((actualValue = reader.IntrinsicsTzcntDiv()) != expectedValue) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsTzcntDiv)} value: {expectedValue} vs {actualValue}");
            if (reader.Index != expectedIndex) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsTzcntDiv)} index: {expectedIndex} vs {reader.Index}");

            reader = GetReader();
            if ((actualValue = reader.IntrinsicsTzcntDiv2()) != expectedValue) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsTzcntDiv2)} value: {expectedValue} vs {actualValue}");
            if (reader.Index != expectedIndex) throw new InvalidOperationException($"{nameof(BenchmarkReader.IntrinsicsTzcntDiv2)} index: {expectedIndex} vs {reader.Index}");
        }

        [Params(1, 2, 3, 4, 5, 6, 7, 8, 9, 10)]
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
        private const int OperationsPerInvoke = 32 * 1024;

        private BenchmarkReader GetReader() => new BenchmarkReader(_source, _byteOffset);

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke, Baseline = true)]
        public ulong Unoptimized()
        {
            var reader = GetReader();
            ulong final = 0;
            int index = reader.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                reader.Index = index; // reset for multiple reads on same data
                final = reader.Unoptimized();
            }
            return final;
        }

        //[Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong UnsafeAdd()
        {
            var reader = GetReader();
            ulong final = 0;
            int index = reader.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                reader.Index = index; // reset for multiple reads on same data
                final = reader.UnsafeAdd();
            }
            return final;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong IntrinsicsTzcntDiv()
        {
            var reader = GetReader();
            ulong final = 0;
            int index = reader.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                reader.Index = index; // reset for multiple reads on same data
                final = reader.IntrinsicsTzcntDiv();
            }
            return final;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong IntrinsicsTzcntDiv2()
        {
            var reader = GetReader();
            ulong final = 0;
            int index = reader.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                reader.Index = index; // reset for multiple reads on same data
                final = reader.IntrinsicsTzcntDiv2();
            }
            return final;
        }

        //[Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong Intrinsics()
        {
            var reader = GetReader();
            ulong final = 0;
            int index = reader.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                reader.Index = index; // reset for multiple reads on same data
                final = reader.Intrinsics();
            }
            return final;
        }

        //[Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong IntrinsicsSwitched()
        {
            var reader = GetReader();
            ulong final = 0;
            int index = reader.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                reader.Index = index; // reset for multiple reads on same data
                final = reader.IntrinsicsSwitched();
            }
            return final;
        }

        //[Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong IntrinsicsPreferShort()
        {
            var reader = GetReader();
            ulong final = 0;
            int index = reader.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                reader.Index = index; // reset for multiple reads on same data
                final = reader.IntrinsicsPreferShort();
            }
            return final;
        }

        [Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong IntrinsicsPreferShort2()
        {
            var reader = GetReader();
            ulong final = 0;
            int index = reader.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                reader.Index = index; // reset for multiple reads on same data
                final = reader.IntrinsicsPreferShort2();
            }
            return final;
        }

        //[Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong IntrinsicsPreferShort3()
        {
            var reader = GetReader();
            ulong final = 0;
            int index = reader.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                reader.Index = index; // reset for multiple reads on same data
                final = reader.IntrinsicsPreferShort3();
            }
            return final;
        }

        //[Benchmark(OperationsPerInvoke = OperationsPerInvoke)]
        public ulong IntrinsicsPreferShort4()
        {
            var reader = GetReader();
            ulong final = 0;
            int index = reader.Index;
            for (int i = 0; i < OperationsPerInvoke; i++)
            {
                reader.Index = index; // reset for multiple reads on same data
                final = reader.IntrinsicsPreferShort4();
            }
            return final;
        }

        ref struct BenchmarkReader
        {
            public int Index
            {
                get => _index;
                set => _index = value;
            }
            public BenchmarkReader(byte[] buffer, int index)
            {
                _buffer = buffer;
                _index = index;
                _end = buffer.Length;
            }
            private int _index, _end;
            private ReadOnlySpan<byte> _buffer;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong Unoptimized()
            {
                if (_index + 10 <= _end)
                {
                    var buffer = _buffer;
                    ulong result = buffer[_index++];
                    if (result < 128)
                    {
                        return result;
                    }
                    result &= 0x7f;
                    int shift = 7;
                    do
                    {
                        byte b = buffer[_index++];
                        result |= (ulong)(b & 0x7F) << shift;
                        if (b < 0x80)
                        {
                            return result;
                        }
                        shift += 7;
                    }
                    while (shift < 64);

                    ThrowMalformed();
                }
                return ReadVarintUInt64Slow();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong UnsafeAdd()
            {
                if (_index + 10 <= _end)
                {
                    var buffer = _buffer;
                    ref var ptr = ref Unsafe.AsRef(in buffer[0]);
                    ulong result = Unsafe.Add(ref ptr, _index++);
                    if (result < 128)
                    {
                        return result;
                    }
                    result &= 0x7f;
                    int shift = 7;
                    do
                    {
                        byte b = Unsafe.Add(ref ptr, _index++);
                        result |= (ulong)(b & 0x7F) << shift;
                        if (b < 0x80)
                        {
                            return result;
                        }
                        shift += 7;
                    }
                    while (shift < 64);

                    ThrowMalformed();
                }
                return ReadVarintUInt64Slow();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong Intrinsics()
            {
#if NETCOREAPP3_1_OR_GREATER
                if (Bmi2.X64.IsSupported && Bmi1.IsSupported)
                {
                    if (_index + 10 <= _end)
                    {
                        // read the value from the first 8 octets
                        var value = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(in _buffer[_index]));
                        if (!BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);

                        // use PEXT to get all the high bits as a contiguous chunk
                        var hiBits = Bmi2.X64.ParallelBitExtract(
                            value, 0x8080808080808080); // MSB in each octet
                        // and use PEXT to retain all the data bits
                        value = Bmi2.X64.ParallelBitExtract(value, 0x7f7f7f7f7f7f7f7f);

                        // now use TZCNT on the complement to find all the continuations
                        var continuations = Bmi1.TrailingZeroCount(~(uint)hiBits);

                        // if all MSBs are set, we still need to check the last 2 octets
                        _index += (int)continuations + 1;
                        if (continuations != 8)
                        {
                            // if we have zero continuations, we want to read 1 byte
                            var mask = ~((~0x7FUL) << (7 * (int)continuations));
                            return value & mask;
                        }
                        else
                        {
                            return WithFirst56(value);
                        }
                    }
                    else
                    {
                        return ReadVarintUInt64Slow();
                    }
                }
                else
#endif
                {
                    return Unoptimized();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong IntrinsicsPreferShort()
            {
#if NETCOREAPP3_1_OR_GREATER
                if (Bmi2.X64.IsSupported && Bmi1.IsSupported)
                {
                    if (_index + 10 <= _end)
                    {
                        // optimize for small values - one/two bytes
                        var val16 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AsRef(in _buffer[_index++]));
                        if (!BitConverter.IsLittleEndian) val16 = BinaryPrimitives.ReverseEndianness(val16);
                        switch (val16 & 0x8080)
                        {
                            case 0x0000: // no continuation
                            case 0x8000: // (with/without next MSB set)
                                return (ulong)(val16 & 0x7F);
                            case 0x0080: // 1 continuation, two bytes
                                _index++;
                                return (ulong)(((val16 & 0x7F00) >> 1) | (val16 & 0x7F));
                        }
                        // read the remaining 8 octets (varint is max 10)
                        var value = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(in _buffer[++_index]));
                        if (!BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);

                        // use PEXT to get all the high bits as a contiguous chunk
                        var hiBits = Bmi2.X64.ParallelBitExtract(
                            value, 0x8080808080808080); // MSB in each octet
                        // and use PEXT to retain all the data bits
                        value = Bmi2.X64.ParallelBitExtract(value, 0x7f7f7f7f7f7f7f7f) << 14 | (((uint)((val16 & 0x7F00) >> 1)) | (uint)(val16 & 0x7F));

                        // now use TZCNT on the complement to find all the continuations
                        var continuations = Bmi1.TrailingZeroCount(~(uint)hiBits);
                        if (continuations == 8) ThrowMalformed();
                        _index += (int)continuations + 1;
                        // if we have zero continuations, we want to read 1 byte
                        var mask = ~((~0x7FUL) << (7 * (int)(continuations + 2)));
                        return value & mask;
                    }
                    else
                    {
                        return ReadVarintUInt64Slow();
                    }
                }
                else
#endif
                {
                    return Unoptimized();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong IntrinsicsPreferShort2()
            {
#if NETCOREAPP3_1_OR_GREATER
                if (Bmi2.IsSupported && Bmi2.X64.IsSupported && Bmi1.IsSupported)
                {
                    if (_index + 10 <= _end)
                    {
                        var b0 = _buffer[_index++];
                        if ((b0 & 0x80) == 0) return b0;

                        var b1 = _buffer[_index++];
                        if ((b1 & 0x80) == 0) return ((uint)(b1 & 0x7f) << 7) | (uint)(b0 & 0x7f);

                        // read the remaining 8 octets (varint is max 10)
                        var value = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(in _buffer[_index]));
                        if (!BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);

                        // use PEXT to get all the high bits as a contiguous chunk
                        var hiBits = Bmi2.X64.ParallelBitExtract(
                            value, 0x8080808080808080); // MSB in each octet
                        // and use PEXT to retain all the data bits
                        value = (Bmi2.X64.ParallelBitExtract(value, 0x7f7f7f7f7f7f7f7f) << 14) | ((uint)(b1 & 0x7f) << 7) | (uint)(b0 & 0x7f);

                        // now use TZCNT on the complement to find all the continuations
                        var continuations = Bmi1.TrailingZeroCount(~(uint)hiBits);
                        if (continuations == 8) ThrowMalformed();
                        _index += (int)continuations + 1;
                        // if we have zero continuations, we want to read 1 byte
                        var mask = ~((~0x7FUL) << (7 * (int)(continuations + 2)));
                        return value & mask;
                    }
                    else
                    {
                        return ReadVarintUInt64Slow();
                    }
                }
                else
#endif
                {
                    return Unoptimized();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong IntrinsicsPreferShort4()
            {
#if NETCOREAPP3_1_OR_GREATER
                if (Bmi2.X64.IsSupported && Bmi1.IsSupported)
                {
                    if (_index + 10 <= _end)
                    {
                        var val16 = Unsafe.ReadUnaligned<ushort>(ref Unsafe.AsRef(in _buffer[_index++]));
                        if (!BitConverter.IsLittleEndian) val16 = BinaryPrimitives.ReverseEndianness(val16);

                        if ((val16 & 0x80) == 0) return val16 & 0xffU;

                        if ((val16 & 0x8000) == 0)
                        {
                            _index++;
                            return ((val16 & 0x7f00U) >> 1) | (val16 & 0x7fU);
                        }

                        // read the remaining 8 octets (varint is max 10)
                        var value = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(in _buffer[++_index]));
                        if (!BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);

                        // use PEXT to get all the high bits as a contiguous chunk
                        var hiBits = Bmi2.X64.ParallelBitExtract(
                            value, 0x8080808080808080); // MSB in each octet
                        // and use PEXT to retain all the data bits
                        value = (Bmi2.X64.ParallelBitExtract(value, 0x7f7f7f7f7f7f7f7f) << 14) | ((val16 & 0x7f00U) >> 1) | (val16 & 0x7fU);

                        // now use TZCNT on the complement to find all the continuations
                        var continuations = Bmi1.TrailingZeroCount(~(uint)hiBits);
                        if (continuations == 8) ThrowMalformed();
                        _index += (int)continuations + 1;
                        // if we have zero continuations, we want to read 1 byte
                        var mask = ~((~0x7FUL) << (7 * (int)(continuations + 2)));
                        return value & mask;
                    }
                    else
                    {
                        return ReadVarintUInt64Slow();
                    }
                }
                else
#endif
                {
                    return Unoptimized();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong IntrinsicsTzcntDiv()
            {
#if NETCOREAPP3_1_OR_GREATER
                if (Bmi2.X64.IsSupported && Bmi1.IsSupported)
                {
                    if (_index + 10 <= _end)
                    {
                        var lo28 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AsRef(in _buffer[_index]));
                        if (!BitConverter.IsLittleEndian) lo28 = BinaryPrimitives.ReverseEndianness(lo28);
                        // we'll pick out all the MSBs, invert and take the tzcnt; if this gives 0-7,
                        // then the first inverted MSB was 1, meaning: the original MSB was zero;
                        // likewise, if this gives 32 then the final inverted MSB was zero, meaning: the
                        // original MSB was one
                        var continuations = Bmi1.TrailingZeroCount((~lo28) & 0x80808080U) / 8;
                        _index += (int)continuations + 1;
                        switch (continuations)
                        {   
                            case 0: return lo28 & 0x7fU;
                            case 1: return Bmi2.ParallelBitExtract(lo28, 0x7f7fU);
                            case 2: return Bmi2.ParallelBitExtract(lo28, 0x7f7f7fU);
                            case 3: return Bmi2.ParallelBitExtract(lo28, 0x7f7f7f7fU);
                        }

                        lo28 = Bmi2.ParallelBitExtract(lo28, 0x7f7f7f7f);
                        // we want another 6 octets; we'll read 8 (because that's easy), overlapping with
                        // the data we already read, so we can avoid needing to allow 12 bytes for this path
                        var hi36 = (BitConverter.IsLittleEndian ? Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(in _buffer[_index - 3]))
                            : BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(in _buffer[_index - 3])))
                            ) >> 16; // drop the 2 octets we already inspected

                        continuations = (uint)Bmi1.X64.TrailingZeroCount((~hi36) & 0x808080808080U) / 8;
                        _index += (int)continuations;
                        return (continuations switch
                        {
                            0 => hi36 & 0x7fU,
                            1 => Bmi2.X64.ParallelBitExtract(hi36, 0x7f7fU),
                            2 => Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f7fU),
                            3 => Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f7f7fU),
                            4 => Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f7f7f7fU),
                            5 => Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f7f7f7f7fU),
                            _ => ThrowMalformedWithResult(),
                        } << 28) | lo28;
                    }
                    else
                    {
                        return ReadVarintUInt64Slow();
                    }
                }
                else
#endif
                {
                    return Unoptimized();
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong IntrinsicsTzcntDiv2()
            {
#if NETCOREAPP3_1_OR_GREATER
                if (Bmi2.X64.IsSupported && Bmi1.IsSupported)
                {
                    if (_index + 10 <= _end)
                    {
                        var lo28 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AsRef(in _buffer[_index++]));
                        if (!BitConverter.IsLittleEndian) lo28 = BinaryPrimitives.ReverseEndianness(lo28);
                        // we'll pick out all the MSBs, invert and take the tzcnt; if this gives 0-7,
                        // then the first inverted MSB was 1, meaning: the original MSB was zero;
                        // likewise, if this gives 32 then the final inverted MSB was zero, meaning: the
                        // original MSB was one
                        var continuations = Bmi1.TrailingZeroCount((~lo28) & 0x80808080U) / 8;
                        //_index += (int)continuations + 1;
                        switch (continuations)
                        {
                            case 0: return lo28 & 0x7fU;
                            case 1:
                                _index++;
                                return Bmi2.ParallelBitExtract(lo28, 0x7f7fU);
                            case 2:
                                _index += 2;
                                return Bmi2.ParallelBitExtract(lo28, 0x7f7f7fU);
                            case 3:
                                _index += 3;
                                return Bmi2.ParallelBitExtract(lo28, 0x7f7f7f7fU);
                        }

                        // we want another 6 octets; we'll read 8 (because that's easy), overlapping with
                        // the data we already read, so we can avoid needing to allow 12 bytes for this path
                        var hi36 = (BitConverter.IsLittleEndian ? Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(in _buffer[_index + 1]))
                            : BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(in _buffer[_index + 1])))
                            ) >> 16; // drop the 2 octets we already inspected

                        continuations = (uint)Bmi1.X64.TrailingZeroCount((~hi36) & 0x808080808080U) / 8;
                        _index += (int)(continuations + 4);
                        return (continuations switch
                        {
                            0 => hi36 & 0x7fU,
                            1 => Bmi2.X64.ParallelBitExtract(hi36, 0x7f7fU),
                            2 => Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f7fU),
                            3 => Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f7f7fU),
                            4 => Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f7f7f7fU),
                            5 => Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f7f7f7f7fU),
                            _ => ThrowMalformedWithResult(),
                        } << 28) | Bmi2.ParallelBitExtract(lo28, 0x7f7f7f7f);
                    }
                    else
                    {
                        return ReadVarintUInt64Slow();
                    }
                }
                else
#endif
                {
                    return Unoptimized();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong IntrinsicsPreferShort3()
            {
#if NETCOREAPP3_1_OR_GREATER
                if (Bmi2.IsSupported && Bmi2.X64.IsSupported && Bmi1.IsSupported)
                {
                    if (_index + 10 <= _end)
                    {
                        var lo28 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AsRef(in _buffer[_index]));
                        if (!BitConverter.IsLittleEndian) lo28 = BinaryPrimitives.ReverseEndianness(lo28);

                        var continuations = Bmi1.TrailingZeroCount(~Bmi2.ParallelBitExtract(
                            lo28, 0x80808080)); // MSB in each octet
                        _index += (int)continuations + 1;
                        switch (continuations)
                        {
                            case 0: return lo28 & 0x7f;
                            case 1: return Bmi2.ParallelBitExtract(lo28, 0x7f7f);
                            case 2: return Bmi2.ParallelBitExtract(lo28, 0x7f7f7f);
                            case 3: return Bmi2.ParallelBitExtract(lo28, 0x7f7f7f7f);
                        }
                        lo28 = Bmi2.ParallelBitExtract(lo28, 0x7f7f7f7f);
                        // we want another 6 octets; we'll read 8 (because that's easy), overlapping with
                        // the data we already read, so we can avoid needing to allow 12 bytes for this path
                        var hi36 = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(in _buffer[_index - 3]));
                        if (!BitConverter.IsLittleEndian) hi36 = BinaryPrimitives.ReverseEndianness(hi36);

                        continuations = Bmi1.TrailingZeroCount(~(uint)Bmi2.X64.ParallelBitExtract(
                            hi36, 0x8080808080800000)); // MSB in each octet (ignoring the low two=overlap)
                        _index += (int)continuations;
                        switch (continuations)
                        {
                            case 0: return ((hi36 & 0x7f0000) << 12) | lo28;
                            case 1: return (Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f0000) << 28) | lo28;
                            case 2: return (Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f7f0000) << 28) | lo28;
                            case 3: return (Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f7f7f0000) << 28) | lo28;
                            case 4: return (Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f7f7f7f0000) << 28) | lo28;
                            case 5: return (Bmi2.X64.ParallelBitExtract(hi36, 0x7f7f7f7f7f7f0000) << 28) | lo28;
                        }
                        ThrowMalformed();
                        return 0; // not reached
                    }
                    else
                    {
                        return ReadVarintUInt64Slow();
                    }
                }
                else
#endif
                {
                    return Unoptimized();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong IntrinsicsSwitched()
            {
#if NETCOREAPP3_1_OR_GREATER
                if (Bmi2.X64.IsSupported && Bmi1.IsSupported)
                {
                    if (_index + 10 <= _end)
                    {
                        // read the value from the first 8 octets
                        var value = Unsafe.ReadUnaligned<ulong>(ref Unsafe.AsRef(in _buffer[_index]));
                        if (!BitConverter.IsLittleEndian) value = BinaryPrimitives.ReverseEndianness(value);

                        // use PEXT to get all the high bits as a contiguous chunk
                        var hiBits = Bmi2.X64.ParallelBitExtract(
                            value, 0x8080808080808080); // MSB in each octet

                        // now use TZCNT on the complement to find all the continuations
                        var continuations = Bmi1.TrailingZeroCount(~(uint)hiBits);

                        _index += (int)(continuations + 1);
                        return continuations switch
                        {
                            0 => value & 0x7F,
                            // more fun with PEXT
                            1 => Bmi2.X64.ParallelBitExtract(value, 0x7F7F),
                            2 => Bmi2.X64.ParallelBitExtract(value, 0x7F7F7F),
                            3 => Bmi2.X64.ParallelBitExtract(value, 0x7F7F7F7F),
                            4 => Bmi2.X64.ParallelBitExtract(value, 0x7F7F7F7F7F),
                            5 => Bmi2.X64.ParallelBitExtract(value, 0x7F7F7F7F7F7F),
                            6 => Bmi2.X64.ParallelBitExtract(value, 0x7F7F7F7F7F7F7F),
                            7 => Bmi2.X64.ParallelBitExtract(value, 0x7F7F7F7F7F7F7F7F),
                            // fallback
                            _ => WithFirst56(Bmi2.X64.ParallelBitExtract(value, 0x7F7F7F7F7F7F7F7F)),
                        };
                    }
                    else
                    {
                        return ReadVarintUInt64Slow();
                    }
                }
                else
#endif
                {
                    return Unoptimized();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ulong WithFirst56(ulong value)
            {
                ulong b = _buffer[_index - 1]; // already pre-incremented by simplified caller
                value |= (b & 0x7f) << 56;
                if ((b & 0x80) != 0)
                {
                    b = _buffer[_index++];
                    value |= (b & 0x7f) << 63;
                    if ((b & 0x80) != 0) ThrowMalformed();
                }
                return value;
            }


            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowMalformed() => throw new NotImplementedException();
            [MethodImpl(MethodImplOptions.NoInlining)]
            static ulong ThrowMalformedWithResult()
            {
                ThrowMalformed();
                return 0;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private ulong ReadVarintUInt64Slow() => throw new NotImplementedException();
        }

    }
}
