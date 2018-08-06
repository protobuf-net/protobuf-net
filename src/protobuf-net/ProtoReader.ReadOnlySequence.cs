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
                offset = _offsetInCurrent;
                _offsetInCurrent += bytes;
                _remainingInCurrent -= bytes;
                Advance(bytes);
                return _current.Span;
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
                        if (throwIfEOF) ThrowEoF(this);
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

            private protected override void ImplReadBytes(ref State state, ArraySegment<byte> target)
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

            private protected override void ImplSkipBytes(ref State state, long count, bool preservePreviewField)
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

            private protected override string ImplReadString(ref State state, int bytes)
            {
                var available = GetSomeData();
                if (available >= bytes)
                    return ToString(Consume(bytes, out var offset), offset, bytes);

                return ImplReadStringMultiSegment(bytes);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static string ToString(ReadOnlySpan<byte> span, int offset, int bytes)
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

            private protected override uint ImplReadUInt32Fixed(ref State state)
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
                    return ViaStackAlloc();
                }

                uint ViaStackAlloc()
                {
                    Span<byte> span = stackalloc byte[4];
                    ImplReadBytes(span);
                    var val = BinaryPrimitives.ReadUInt32LittleEndian(span);
                    Log($"U32FS - {val}");
                    return val;
                }
            }

            private protected override ulong ImplReadUInt64Fixed(ref State state)
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
                    return ViaStackAlloc();
                }

                ulong ViaStackAlloc()
                {
                    Span<byte> span = stackalloc byte[8];
                    ImplReadBytes(span);
                    var val = BinaryPrimitives.ReadUInt64LittleEndian(span);
                    Log($"U64F! - {val}");
                    return val;
                }
            }

            private protected override int ImplTryReadUInt32VarintWithoutMoving(ref State state, Read32VarintMode mode, out uint value)
            {
                if (previewFieldBytes != 0)
                {
                    var read = previewFieldBytes;
                    value = checked((uint)PeekPreviewField());
                    Log($"T32* - {read}:{value}");
                    return read;
                }

                if (GetSomeData(false) >= 10)
                {
                    var span = Peek(out var offset);
                    var read = TryParseUInt32Varint(this, offset, mode == Read32VarintMode.Signed, out value, span);
                    Log($"T32 - {read}:{value}");
                    if (read != 0 && mode == Read32VarintMode.FieldHeader) ReadPreviewField(value, span, offset + read);
                    return read;
                }
                else
                {
                    return ViaStackAlloc(mode, out value);
                }

                int ViaStackAlloc(Read32VarintMode m, out uint val)
                {
                    Span<byte> span = stackalloc byte[20];
                    var available = ImplPeekBytes(span);
                    if (available != 20) span = span.Slice(0, available);
                    var read = TryParseUInt32Varint(this, 0, m == Read32VarintMode.Signed, out val, span);
                    Log($"T32! - {read}:{val}");
                    if (read != 0 && m == Read32VarintMode.FieldHeader) ReadPreviewField(val, span, read);
                    return read;
                }
            }
            private int previewFieldBytes;
            private ulong previewField;

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
                        previewFieldBytes = TryParseUInt64Varint(this, offset, out previewField, span);
                        Log($">RPF V64: {previewFieldBytes}: {previewField}");
                        break;
                    default:
                        previewFieldBytes = 0;
                        Log(">RPF: nil");
                        break;
                }
            }
            [Conditional("VERBOSE")]
            private void Log(string message)
            {
#if VERBOSE
                Console.WriteLine("[" + LongPosition.ToString() + "] " + message);
#else
                Debug.WriteLine("[" + LongPosition.ToString() + "] " + message);
#endif
            }

            private protected override int ImplTryReadUInt64VarintWithoutMoving(ref State state, out ulong value)
            {
                if (previewFieldBytes != 0)
                {
                    var read = previewFieldBytes;
                    value = checked((uint)PeekPreviewField());
                    Log($"T64* - {read}:{value}");
                    return read;
                }

                if (GetSomeData(false) >= 10)
                {
                    var span = Peek(out var offset);
                    var read = TryParseUInt64Varint(this, offset, out value, span);
                    Log($"T64 - {read}:{value}");
                    return read;
                }
                else
                {
                    return ViaStackAlloc(out value);
                }

                int ViaStackAlloc(out ulong val)
                {
                    Span<byte> span = stackalloc byte[10];
                    var read = ImplPeekBytes(span);
                    if (read != 10) span = span.Slice(0, read);
                    read = TryParseUInt64Varint(this, 0, out val, span);
                    Log($"T64! - {read}:{val}");
                    return read;
                }
            }

            internal static int TryParseUInt32Varint(ProtoReader @this, int offset, bool trimNegative, out uint value, ReadOnlySpan<byte> span)
            {
                if ((uint)offset >= (uint)span.Length)
                {
                    value = 0;
                    return 0;
                }

                value = span[offset++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                uint chunk = span[offset++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= chunk << 28; // can only use 4 bits from this chunk
                if ((chunk & 0xF0) == 0) return 5;

                if (trimNegative // allow for -ve values
                    && (chunk & 0xF0) == 0xF0
                    && offset + 4 < (uint)span.Length
                        && span[offset] == 0xFF
                        && span[offset + 1] == 0xFF
                        && span[offset + 2] == 0xFF
                        && span[offset + 3] == 0xFF
                        && span[offset + 4] == 0x01)
                {
                    return 10;
                }

                ThrowOverflow(@this);
                return 0;
            }
            internal static int TryParseUInt64Varint(ProtoReader @this, int offset, out ulong value, ReadOnlySpan<byte> span)
            {
                if ((uint)offset >= (uint)span.Length)
                {
                    value = 0;
                    return 0;
                }
                value = span[offset++];
                if ((value & 0x80) == 0) return 1;
                value &= 0x7F;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                ulong chunk = span[offset++];
                value |= (chunk & 0x7F) << 7;
                if ((chunk & 0x80) == 0) return 2;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 14;
                if ((chunk & 0x80) == 0) return 3;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 21;
                if ((chunk & 0x80) == 0) return 4;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 28;
                if ((chunk & 0x80) == 0) return 5;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 35;
                if ((chunk & 0x80) == 0) return 6;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 42;
                if ((chunk & 0x80) == 0) return 7;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 49;
                if ((chunk & 0x80) == 0) return 8;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset++];
                value |= (chunk & 0x7F) << 56;
                if ((chunk & 0x80) == 0) return 9;

                if ((uint)offset >= (uint)span.Length) ThrowEoF(@this);
                chunk = span[offset];
                value |= chunk << 63; // can only use 1 bit from this chunk

                if ((chunk & ~(ulong)0x01) != 0) ThrowOverflow(@this);
                return 10;
            }

            private protected override bool IsFullyConsumed(ref State state) => GetSomeData(false) == 0;
        }
    }
}
#endif