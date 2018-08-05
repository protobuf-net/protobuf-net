#if PLAT_SPANS
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoBuf
{
    public partial class ProtoReader
    {
        /// <summary>
        /// Creates a new reader against a multi-segment buffer
        /// </summary>
        /// <param name="source">The source buffer</param>
        /// <param name="model">The model to use for serialization; this can be null, but this will impair the ability to deserialize sub-objects</param>
        /// <param name="context">Additional context about this serialization operation</param>
        public static ProtoReader Create(ReadOnlySequence<byte> source, TypeModel model, SerializationContext context = null)
        {
            var reader = ReadOnlySequenceProtoReader.GetRecycled()
                ?? new ReadOnlySequenceProtoReader();
            reader.Init(source, model, context);
            return reader;
        }

        private sealed class ReadOnlySequenceProtoReader : ProtoReader
        {
            [ThreadStatic]
            private static ReadOnlySequenceProtoReader s_lastReader;
            private ReadOnlySequence<byte>.Enumerator _source;
            private ReadOnlyMemory<byte> _current;
            private int _offsetInCurrent, _remainingInCurrent;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int GetSomeData(bool throwIfEOF = true)
            {
                var data = _remainingInCurrent;
                return data == 0 ? ReadNextBuffer(throwIfEOF) : data;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ReadOnlySpan<byte> Peek(int bytes)
                => _current.Span.Slice(_offsetInCurrent, bytes);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ReadOnlySpan<byte> Peek(out int offset)
            {
                offset = _offsetInCurrent;
                return _current.Span;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ReadOnlySpan<byte> Consume(int bytes)
            {
                Log($"TAKE - {bytes}");
                var span = _current.Span.Slice(_offsetInCurrent, bytes);
                _offsetInCurrent += bytes;
                _remainingInCurrent -= bytes;
                Advance(bytes);
                return span;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ReadOnlySpan<byte> Consume(int bytes, out int offset)
            {
                Log($"TAKE - {bytes}");
                var span = _current.Span;
                offset = _offsetInCurrent;
                _offsetInCurrent += bytes;
                _remainingInCurrent -= bytes;
                Advance(bytes);
                return span;
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Skip(int bytes)
            {
                Log($"SKIP - {bytes}");
                _offsetInCurrent += bytes;
                _remainingInCurrent -= bytes;
                Advance(bytes);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private int ReadNextBuffer(bool throwIfEOF)
            {
                do
                {
                    if (!_source.MoveNext())
                    {
                        if (throwIfEOF) throw EoF(this);
                        return 0;
                    }
                    _current = _source.Current;
                } while (_current.IsEmpty);
                _offsetInCurrent = 0;
                return _remainingInCurrent = _current.Length;
            }

            internal static ReadOnlySequenceProtoReader GetRecycled()
            {
                var tmp = s_lastReader;
                s_lastReader = null;
                return tmp;
            }

            internal void Init(ReadOnlySequence<byte> source, TypeModel model, SerializationContext context)
            {
                base.Init(model, context);
                _source = source.GetEnumerator();
            }

            internal override void Recycle()
            {
                Dispose();
                s_lastReader = this;
            }

            public override void Dispose()
            {
                base.Dispose();
                _source = default;
            }

            private protected override void ImplReadBytes(ArraySegment<byte> target)
                => ImplReadBytes(new Span<byte>(target.Array, target.Offset, target.Count));

            private void ImplReadBytes(Span<byte> target)
            {
                while (!target.IsEmpty)
                {
                    var take = Math.Min(GetSomeData(), target.Length);
                    Consume(take).CopyTo(target);
                    target = target.Slice(take);
                }
            }
            private int ImplPeekBytes(Span<byte> target)
            {
                var currentBuffer = Peek(Math.Min(target.Length, _remainingInCurrent));
                currentBuffer.CopyTo(target);
                int available = currentBuffer.Length;
                target = target.Slice(available);

                var iterCopy = _source;
                while (!target.IsEmpty && iterCopy.MoveNext())
                {
                    var nextBuffer = iterCopy.Current.Span;
                    var take = Math.Min(nextBuffer.Length, target.Length);

                    nextBuffer.Slice(0, take).CopyTo(target);
                    target = target.Slice(take);
                    available += take;
                }
                return available;
            }

            private protected override void ImplSkipBytes(long count, bool preservePreviewField)
            {
                while (count != 0)
                {
                    var take = (int)Math.Min(GetSomeData(), count);
                    Skip(take);
                    count -= take;
                }
                if (!preservePreviewField)
                {   // drop it
                    previewFieldBytes = 0;
                }
            }

            private protected override string ImplReadString(int bytes)
            {
                var available = GetSomeData();
                if (available >= bytes)
                    return ToString(Consume(bytes, out var offset), offset, bytes);

                return ImplReadStringMultiSegment(bytes);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static string ToString(ReadOnlySpan<byte> span, int offset, int bytes)
            {
#if PLAT_SPAN_OVERLOADS
                return UTF8.GetString(span.Slice(offset, bytes));
#else
                unsafe
                {
                    fixed (byte* sPtr = &MemoryMarshal.GetReference(span))
                    {
                        var bPtr = sPtr + offset;
                        int chars = UTF8.GetCharCount(bPtr, bytes);
                        string s = new string('\0', chars);
                        fixed (char* cPtr = s)
                        {
                            UTF8.GetChars(bPtr, bytes, cPtr, chars);
                        }
                        return s;
                    }
                }
#endif
            }

            private string ImplReadStringMultiSegment(int bytes)
            {
                // we should probably do the work with a Decoder,
                // but this works for today
                using (var mem = MemoryPool<byte>.Shared.Rent(bytes))
                {
                    var span = mem.Memory.Span;
                    ImplReadBytes(span);
                    return ToString(span, 0, bytes);
                }
            }

            private protected override uint ImplReadUInt32Fixed()
            {
                if (previewFieldBytes != 0)
                {
                    var val = checked((uint)ConsumePreviewField());
                    Log($"U32F* - {val}");
                    return val;
                }
                else if (GetSomeData() >= 4)
                {
                    var val = BinaryPrimitives.ReadUInt32LittleEndian(Consume(4));
                    Log($"U32F - {val}");
                    return val;
                }
                else
                {
                    Span<byte> span = stackalloc byte[4];
                    ImplReadBytes(span);
                    var val = BinaryPrimitives.ReadUInt32LittleEndian(span);
                    Log($"U32FS - {val}");
                    return val;
                }
            }

            private protected override ulong ImplReadUInt64Fixed()
            {
                if (previewFieldBytes != 0)
                {
                    var val = ConsumePreviewField();
                    Log($"U64F* - {val}");
                    return val;
                }
                else if (GetSomeData() >= 8)
                {
                    var val = BinaryPrimitives.ReadUInt64LittleEndian(Consume(8));
                    Log($"U64F - {val}");
                    return val;
                }
                else
                {
                    Span<byte> span = stackalloc byte[8];
                    ImplReadBytes(span);
                    var val = BinaryPrimitives.ReadUInt64LittleEndian(span);
                    Log($"U64F! - {val}");
                    return val;
                }
            }

            private protected override int TryReadUInt32VarintWithoutMoving(Read32VarintMode mode, out uint value)
            {
                var read = previewFieldBytes;
                if (read != 0)
                {
                    value = checked((uint)PeekPreviewField());
                    Log($"T32* - {read}:{value}");
                    return read;
                }
                
                if (GetSomeData(false) >= 10)
                {
                    var span = Peek(out var offset);
                    read = TryParseUInt32Varint(mode == Read32VarintMode.Signed, span, offset, out value);
                    Log($"T32 - {read}:{value}");
                    if (read != 0 && mode == Read32VarintMode.FieldHeader) ReadPreviewField(value, span, offset + read);
                    return read;
                }
                return TryReadUInt32VarintWithoutMovingSlow(mode, out value);
            }
            int previewFieldBytes;
            ulong previewField;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ulong PeekPreviewField()
            {
                Debug.Assert(previewFieldBytes != 0);
                Log($"<PPF - {previewFieldBytes}:{previewField}");
                return previewField;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private ulong ConsumePreviewField()
            {
                Debug.Assert(previewFieldBytes != 0);
                Skip(previewFieldBytes);
                Log($"<CPF - {previewFieldBytes}:{previewField}");
                previewFieldBytes = 0;                
                return previewField;
            }

            private void ReadPreviewField(uint fieldHeader, ReadOnlySpan<byte> span, int offset)
            {
                switch ((WireType)(fieldHeader & 7))
                {
                    case WireType.Fixed32:
                        previewField = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(offset));
                        previewFieldBytes = 4;
                        Log($">RPF F32: {previewFieldBytes}: {previewField}");
                        break;
                    case WireType.Fixed64:
                        previewField = BinaryPrimitives.ReadUInt64LittleEndian(span.Slice(offset));
                        previewFieldBytes = 8;
                        Log($">RPF F64: {previewFieldBytes}: {previewField}");
                        break;
                    case WireType.String:
                    case WireType.Variant:
                        previewFieldBytes = TryParseUInt64Varint(span, offset, out previewField);
                        Log($">RPF V64: {previewFieldBytes}: {previewField}");
                        break;
                    default:
                        previewFieldBytes = 0;
                        Log(">RPF: nil");
                        break;
                }
            }
            [Conditional("VERBOSE")]
            void Log(string message)
            {
#if VERBOSE
                Console.WriteLine("[" + LongPosition.ToString() + "] " + message);
#endif
            }
            [MethodImpl(MethodImplOptions.NoInlining)]
            private int TryReadUInt32VarintWithoutMovingSlow(Read32VarintMode mode, out uint value)
            {
                Span<byte> span = stackalloc byte[20];
                var available = ImplPeekBytes(span);
                if (available != 20) span = span.Slice(0, available);
                var read = TryParseUInt32Varint(mode == Read32VarintMode.Signed, span, 0, out value);
                Log($"T32! - {read}:{value}");
                if (read != 0 && mode == Read32VarintMode.FieldHeader) ReadPreviewField(value, span, read);
                return read;
            }
            private protected override int TryReadUInt64VarintWithoutMoving(out ulong value)
            {
                var read = previewFieldBytes;
                if (read != 0)
                {
                    value = checked((uint)PeekPreviewField());
                    Log($"T64* - {read}:{value}");
                    return read;
                }

                if (GetSomeData(false) >= 10)
                {
                    read = TryParseUInt64Varint(Peek(out var offset), offset, out value);
                    Log($"T64 - {read}:{value}");
                    return read;
                }

                Span<byte> span = stackalloc byte[10];
                read = ImplPeekBytes(span);
                if (read != 10) span = span.Slice(0, read);
                read = TryParseUInt64Varint(span, 0, out value);
                Log($"T64! - {read}:{value}");
                return read;
            }
            private int TryParseUInt32Varint(bool trimNegative, ReadOnlySpan<byte> span, int offset, out uint value)
            {
                var available = span.Length - offset;
                if (available == 0)
                {
                    value = 0;
                    return 0;
                }
                value = span[offset++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;
                if (available == 1) throw EoF(this);

                uint chunk = span[offset++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;
                if (available == 2) throw EoF(this);

                chunk = span[offset++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;
                if (available == 3) throw EoF(this);

                chunk = span[offset++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;
                if (available == 4) throw EoF(this);

                chunk = span[offset];
                value |= chunk << 28; // can only use 4 bits from this chunk
                if ((chunk & 0xF0) == 0) return 5;

                if (trimNegative // allow for -ve values
                    && (chunk & 0xF0) == 0xF0
                    && available >= 10
                        && span[++offset] == 0xFF
                        && span[++offset] == 0xFF
                        && span[++offset] == 0xFF
                        && span[++offset] == 0xFF
                        && span[++offset] == 0x01)
                {
                    return 10;
                }
                throw AddErrorData(new OverflowException(), this);
            }
            private int TryParseUInt64Varint(ReadOnlySpan<byte> span, int offset, out ulong value)
            {
                var available = span.Length - offset;
                if (available == 0)
                {
                    value = 0;
                    return 0;
                }
                value = span[offset++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;
                if (available == 1) throw EoF(this);

                ulong chunk = span[offset++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;
                if (available == 2) throw EoF(this);

                chunk = span[offset++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;
                if (available == 3) throw EoF(this);

                chunk = span[offset++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;
                if (available == 4) throw EoF(this);

                chunk = span[offset++];
                value |= (chunk & 0x7F) << 28;
                if ((chunk & 0x80) == 0) return 5;
                if (available == 5) throw EoF(this);

                chunk = span[offset++];
                value |= (chunk & 0x7F) << 35;
                if ((chunk & 0x80) == 0) return 6;
                if (available == 6) throw EoF(this);

                chunk = span[offset++];
                value |= (chunk & 0x7F) << 42;
                if ((chunk & 0x80) == 0) return 7;
                if (available == 7) throw EoF(this);

                chunk = span[offset++];
                value |= (chunk & 0x7F) << 49;
                if ((chunk & 0x80) == 0) return 8;
                if (available == 8) throw EoF(this);

                chunk = span[offset++];
                value |= (chunk & 0x7F) << 56;
                if ((chunk & 0x80) == 0) return 9;
                if (available == 9) throw EoF(this);

                chunk = span[offset];
                value |= chunk << 63; // can only use 1 bit from this chunk

                if ((chunk & ~(ulong)0x01) != 0) throw AddErrorData(new OverflowException(), this);
                return 10;
            }

            private protected override bool IsFullyConsumed => GetSomeData(false) == 0;
        }
    }
}
#endif