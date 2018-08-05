#if PLAT_SPANS
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.Buffers.Binary;
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
            private ReadOnlySpan<byte> Consume(int bytes)
            {
                var span = _current.Span.Slice(_offsetInCurrent, bytes);
                _offsetInCurrent += bytes;
                _remainingInCurrent -= bytes;
                LongPosition += bytes;
                return span;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void Skip(int bytes)
            {
                _offsetInCurrent += bytes;
                _remainingInCurrent -= bytes;
                LongPosition += bytes;
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

            internal override void ImplSkipBytes(long count)
            {
                while (count != 0)
                {
                    var take = (int)Math.Min(GetSomeData(), count);
                    Skip(take);
                    count -= take;
                }
            }

            private protected override string ImplReadString(int bytes)
            {
                var available = GetSomeData();
                if (available >= bytes) return ToString(Consume(bytes));
                return ImplReadStringMultiSegment(bytes);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static string ToString(ReadOnlySpan<byte> span)
            {
#if PLAT_SPAN_OVERLOADS
                return UTF8.GetString(span);
#else
                unsafe
                {
                    fixed (byte* bPtr = &MemoryMarshal.GetReference(span))
                    {
                        int chars = UTF8.GetCharCount(bPtr, span.Length);
                        string s = new string('\0', chars);
                        fixed (char* cPtr = s)
                        {
                            UTF8.GetChars(bPtr, span.Length, cPtr, chars);
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
                    var span = mem.Memory.Span.Slice(0, bytes);
                    ImplReadBytes(span);
                    return ToString(span);
                }
            }

            private protected override uint ImplReadUInt32Fixed()
            {
                if (GetSomeData() >= 4) return BinaryPrimitives.ReadUInt32LittleEndian(Consume(4));

                Span<byte> span = stackalloc byte[4];
                ImplReadBytes(span);
                return BinaryPrimitives.ReadUInt32LittleEndian(span);
            }

            private protected override ulong ImplReadUInt64Fixed()
            {
                if (GetSomeData() >= 8) return BinaryPrimitives.ReadUInt64LittleEndian(Consume(8));

                Span<byte> span = stackalloc byte[8];
                ImplReadBytes(span);
                return BinaryPrimitives.ReadUInt64LittleEndian(span);
            }

            internal override int TryReadUInt32VarintWithoutMoving(bool trimNegative, out uint value)
            {
                if (GetSomeData(false) >= 10) return TryReadUInt32VarintWithoutMoving(trimNegative, Peek(10), out value);

                Span<byte> span = stackalloc byte[10];
                var read = ImplPeekBytes(span);
                return TryReadUInt32VarintWithoutMoving(trimNegative, span.Slice(0, read), out value);
            }
            private protected override int TryReadUInt64VarintWithoutMoving(out ulong value)
            {
                if (GetSomeData(false) >= 10) return TryReadUInt64VarintWithoutMoving(Peek(10), out value);

                Span<byte> span = stackalloc byte[10];
                var read = ImplPeekBytes(span);
                return TryReadUInt64VarintWithoutMoving(span.Slice(0, read), out value);
            }
            private int TryReadUInt32VarintWithoutMoving(bool trimNegative, ReadOnlySpan<byte> span, out uint value)
            {
                var available = span.Length;
                if (available == 0)
                {
                    value = 0;
                    return 0;
                }
                int readPos = 0;
                value = span[readPos++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;
                if (available == 1) throw EoF(this);

                uint chunk = span[readPos++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;
                if (available == 2) throw EoF(this);

                chunk = span[readPos++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;
                if (available == 3) throw EoF(this);

                chunk = span[readPos++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;
                if (available == 4) throw EoF(this);

                chunk = span[readPos];
                value |= chunk << 28; // can only use 4 bits from this chunk
                if ((chunk & 0xF0) == 0) return 5;

                if (trimNegative // allow for -ve values
                    && (chunk & 0xF0) == 0xF0
                    && available >= 10
                        && span[++readPos] == 0xFF
                        && span[++readPos] == 0xFF
                        && span[++readPos] == 0xFF
                        && span[++readPos] == 0xFF
                        && span[++readPos] == 0x01)
                {
                    return 10;
                }
                throw AddErrorData(new OverflowException(), this);
            }
            private int TryReadUInt64VarintWithoutMoving(ReadOnlySpan<byte> span, out ulong value)
            {
                var available = span.Length;
                if (available == 0)
                {
                    value = 0;
                    return 0;
                }
                int readPos = 0;
                value = span[readPos++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;
                if (available == 1) throw EoF(this);

                ulong chunk = span[readPos++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;
                if (available == 2) throw EoF(this);

                chunk = span[readPos++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;
                if (available == 3) throw EoF(this);

                chunk = span[readPos++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;
                if (available == 4) throw EoF(this);

                chunk = span[readPos++];
                value |= (chunk & 0x7F) << 28;
                if ((chunk & 0x80) == 0) return 5;
                if (available == 5) throw EoF(this);

                chunk = span[readPos++];
                value |= (chunk & 0x7F) << 35;
                if ((chunk & 0x80) == 0) return 6;
                if (available == 6) throw EoF(this);

                chunk = span[readPos++];
                value |= (chunk & 0x7F) << 42;
                if ((chunk & 0x80) == 0) return 7;
                if (available == 7) throw EoF(this);

                chunk = span[readPos++];
                value |= (chunk & 0x7F) << 49;
                if ((chunk & 0x80) == 0) return 8;
                if (available == 8) throw EoF(this);

                chunk = span[readPos++];
                value |= (chunk & 0x7F) << 56;
                if ((chunk & 0x80) == 0) return 9;
                if (available == 9) throw EoF(this);

                chunk = span[readPos];
                value |= chunk << 63; // can only use 1 bit from this chunk

                if ((chunk & ~(ulong)0x01) != 0) throw AddErrorData(new OverflowException(), this);
                return 10;
            }

            private protected override bool IsFullyConsumed => GetSomeData(false) == 0;
        }
    }
}
#endif