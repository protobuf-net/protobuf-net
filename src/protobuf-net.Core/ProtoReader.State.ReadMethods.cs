using ProtoBuf.Internal;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    partial class ProtoReader
    {
#if PLAT_AGGRESSIVE_OPTIMIZE
        internal const MethodImplOptions HotPath = MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization;
#else
        internal const MethodImplOptions HotPath = MethodImplOptions.AggressiveInlining;
#endif

        ref partial struct State
        {
            // The rewritten edition: every member here runs over the raw core (see
            // ProtoReader.State.Raw.cs and PORTING.md). The stateful semantics -
            // the WireType=None release choreography, the two SubItemToken encodings, SetTag's
            // end-group spoof - are ported faithfully from the class-backend implementation this
            // file replaces; the composites are the original bodies with the substitution
            // glossary applied.

            // ------------------------------------------------------------ header state

            /// <summary>
            /// Indicates the underlying proto serialization format on the wire.
            /// </summary>
            public readonly WireType WireType
            {
                [MethodImpl(HotPath)]
                get => _wireType;
            }

            /// <summary>
            /// Gets the number of the field being processed.
            /// </summary>
            public readonly int FieldNumber
            {
                [MethodImpl(HotPath)]
                get => _fieldNumber;
            }

            /// <summary>
            /// Gets / sets a flag indicating whether strings should be checked for repetition; if
            /// true, any repeated UTF-8 byte sequence will result in the same String instance, rather
            /// than a second instance of the same string. Disabled by default. Note that this uses
            /// a <i>custom</i> interner - the system-wide string interner is not used.
            /// </summary>
            public bool InternStrings
            {
                readonly get => _internStrings;
                set => _internStrings = value;
            }

            internal readonly TypeModel Model
            {
                [MethodImpl(HotPath)]
                get => _model;
            }

            /// <summary>
            /// Additional information about this deserialization operation.
            /// </summary>
            public ISerializationContext Context
            {
                [MethodImpl(HotPath)]
                get => _contextShim ??= new StateContext(_model, _userState);
            }

            /// <summary>
            /// Returns the position of the current reader (note that this is not necessarily the same as the position
            /// in the underlying stream, if multiple readers are used on the same stream)
            /// </summary>
            [MethodImpl(HotPath)]
            public readonly long GetPosition() => Position;

            // ------------------------------------------------------------ header machinery

            /// <summary>
            /// Reads a field header from the stream, setting the wire-type and retuning the field number. If no
            /// more fields are available, then 0 is returned. This methods respects sub-messages.
            /// </summary>
            [MethodImpl(HotPath)]
            public int ReadFieldHeader()
            {
                // at the end of a group the caller must call EndSubItem to release the reader
                if (_wireType == WireType.EndGroup) return 0;
                var tag = _pendingTag;
                if (tag != 0) _pendingTag = 0;
                else tag = ReadRawTag(); // 0 at the end of a length scope / clean EOF
                if (tag == 0)
                {
                    _wireType = WireType.None;
                    return _fieldNumber = 0;
                }
                return SetTag(tag);
            }

            [MethodImpl(HotPath)]
            private int SetTag(uint tag)
            {
                if ((_fieldNumber = (int)(tag >> 3)) < 1) ThrowInvalidField(_fieldNumber);
                if ((_wireType = (WireType)(tag & 7)) == WireType.EndGroup)
                {
                    if (_depth > 0) return 0; // spoof an end, but note we still set the field-number
                    ThrowUnexpectedEndGroup();
                }
                return _fieldNumber;
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowInvalidField(int fieldNumber)
                => ThrowHelper.ThrowProtoException("Invalid field in source data: " + fieldNumber.ToString());

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ThrowUnexpectedEndGroup()
                => ThrowHelper.ThrowProtoException("Unexpected end-group in source data; this usually means the source data is corrupt");

            /// <summary>
            /// Looks ahead to see whether the next field in the stream is what we expect
            /// (typically; what we've just finished reading - for example ot read successive list items)
            /// </summary>
            [MethodImpl(HotPath)]
            public bool TryReadFieldHeader(int field)
            {
                // forward-only, unconditionally: the reader cannot rewind, so a miss parks the
                // already-decoded tag in the pending slot for the next header read (see
                // notes/nano-core.md, "the reader is forward-only")
                if (_wireType == WireType.EndGroup) return false;
                uint tag = _pendingTag;
                if (tag != 0)
                {
                    if ((int)(tag >> 3) == field && (tag & 7) != 4)
                    {
                        _pendingTag = 0;
                        _fieldNumber = field;
                        _wireType = (WireType)(tag & 7);
                        return true;
                    }
                    return false; // stays pending
                }
                tag = ReadRawTag();
                if (tag != 0 && (int)(tag >> 3) == field && (tag & 7) != 4)
                {
                    _fieldNumber = field;
                    _wireType = (WireType)(tag & 7);
                    return true;
                }
                _pendingTag = tag; // 0 at a scope end = slot stays empty, correctly
                return false;
            }

            /// <summary>
            /// Raw-convention entry to the stateful surface: pushes an already-consumed tag into
            /// the field-number and wire-type slots, so a generated raw read can hand ONE member
            /// to the stateful read machinery - maps, the repeated engines, BCL kinds, anything
            /// the raw pass has no native form for - and return to raw dispatch afterwards. The
            /// tag was matched at a case label, so it is never 0 and never an end-group; no
            /// validation is repeated here.
            /// </summary>
            [MethodImpl(HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void StashTag(uint tag)
            {
                _fieldNumber = (int)(tag >> 3);
                _wireType = (WireType)(tag & 7);
            }

            /// <summary>
            /// Raw-convention tag read that first drains the pending slot: the stateful
            /// repeated/map engines consume field runs through <see cref="TryReadFieldHeader"/>,
            /// which parks the first non-matching header there - so a raw dispatch loop that
            /// mixes stateful members must pick that header up rather than read past it. A pure
            /// raw loop never populates the slot, which is why <see cref="ReadRawTag"/> itself
            /// does not pay for this branch.
            /// </summary>
            [MethodImpl(HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public uint ReadRawTagOrPending()
            {
                var tag = _pendingTag;
                if (tag != 0)
                {
                    _pendingTag = 0;
                    return tag;
                }
                return ReadRawTag();
            }

            /// <summary>
            /// Raw-convention wire-type failure: the dispatch matched a KNOWN field number but no
            /// acceptable wire-type label. The stateful path reports that as a wire-type
            /// exception; silently skipping it as if unknown would be an invalid-data detection
            /// gap.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public void ThrowUnexpectedWireType(uint tag)
            {
                StashTag(tag);
                ThrowWireTypeException();
            }

            /// <summary>
            /// Compares the streams current wire-type to the hinted wire-type, updating the reader if necessary; for example,
            /// a Variant may be updated to SignedVariant. If the hinted wire-type is unrelated then no change is made.
            /// </summary>
            [MethodImpl(HotPath)]
            public void Hint(WireType wireType)
            {
                if (_wireType == wireType) { }  // fine; everything as we expect
                else if (((int)wireType & 7) == (int)_wireType)
                {   // the underling type is a match; we're customising it with an extension
                    _wireType = wireType;
                }
                // note no error here; we're OK about using alternative data
            }

            /// <summary>
            /// Verifies that the stream's current wire-type is as expected, or a specialized sub-type (for example,
            /// SignedVariant) - in which case the current wire-type is updated. Otherwise an exception is thrown.
            /// </summary>
            [MethodImpl(HotPath)]
            public void Assert(WireType wireType)
            {
                var actual = _wireType;
                if (actual == wireType) { }  // fine; everything as we expect
                else if (((int)wireType & 7) == (int)actual)
                {   // the underling type is a match; we're customising it with an extension
                    _wireType = wireType;
                }
                else
                {   // nope; that is *not* what we were expecting!
                    ThrowWireTypeException();
                }
            }

            [MethodImpl(HotPath)]
            internal void CheckFullyConsumed()
            {
                if (!IsFullyConsumed()) ThrowProtoException("Incorrect number of bytes consumed");
            }

            private bool IsFullyConsumed()
            {
                if (_scope == long.MaxValue)
                {
                    // unbounded root: consumed when the source is exhausted
                    return _offset >= _count && !GetNextBuffer();
                }
                if (_scope >= 0) return Position == _scope;
                return false; // inside an unfinished group: never fully consumed
            }

            // ------------------------------------------------------------ varint helpers

            [MethodImpl(HotPath)]
            private uint ReadUInt32Varint(Read32VarintMode mode)
                // Signed mode tolerates the 10-byte negative-int32 form (truncating); Unsigned is
                // strict-5, overflowing beyond 32 bits - exactly the legacy backend split
                => mode == Read32VarintMode.Signed ? ReadRawVarint32() : ReadRawVarint32Strict();

            [MethodImpl(HotPath)]
            private ulong ReadUInt64Varint() => ReadRawVarint64();

            // ------------------------------------------------------------ scalars

            /// <summary>
            /// Reads an unsigned 16-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public ushort ReadUInt16()
            {
                checked { return (ushort)ReadUInt32(); }
            }

            /// <summary>
            /// Reads a signed 16-bit integer from the stream: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            [MethodImpl(HotPath)]
            public short ReadInt16()
            {
                checked { return (short)ReadInt32(); }
            }

            /// <summary>
            /// Reads an unsigned 8-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public byte ReadByte()
            {
                checked { return (byte)ReadUInt32(); }
            }

            /// <summary>
            /// Reads a signed 8-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            [MethodImpl(HotPath)]
            public sbyte ReadSByte()
            {
                checked { return (sbyte)ReadInt32(); }
            }

            /// <summary>
            /// Reads a native integer from the stream; if the value exceeds the native width, an error will occur; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public IntPtr ReadIntPtr() => new(ReadInt64());

            /// <summary>
            /// Reads a native integer from the stream; if the value exceeds the native width, an error will occur; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public UIntPtr ReadUIntPtr() => new(ReadUInt64());

            /// <summary>
            /// Reads an unsigned 32-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            [MethodImpl(HotPath)]
            public uint ReadUInt32()
            {
                switch (_wireType)
                {
                    case WireType.Varint:
                        return ReadUInt32Varint(Read32VarintMode.Signed);
                    case WireType.Fixed32:
                        return ReadRawFixed32();
                    case WireType.Fixed64:
                        ulong val = ReadRawFixed64();
                        checked { return (uint)val; }
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            /// <summary>
            /// Reads a signed 32-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            [MethodImpl(HotPath)]
            public int ReadInt32()
            {
                switch (_wireType)
                {
                    case WireType.Varint:
                        return (int)ReadUInt32Varint(Read32VarintMode.Signed);
                    case WireType.Fixed32:
                        return (int)ReadRawFixed32();
                    case WireType.Fixed64:
                        long l = ReadInt64();
                        checked { return (int)l; }
                    case WireType.SignedVarint:
                        return Zag(ReadUInt32Varint(Read32VarintMode.Signed));
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            /// <summary>
            /// Reads a signed 64-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
            /// </summary>
            [MethodImpl(HotPath)]
            public long ReadInt64()
            {
                switch (_wireType)
                {
                    case WireType.Varint:
                        return (long)ReadUInt64Varint();
                    case WireType.Fixed32:
                        return (int)ReadRawFixed32();
                    case WireType.Fixed64:
                        return (long)ReadRawFixed64();
                    case WireType.SignedVarint:
                        return Zag(ReadUInt64Varint());
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            /// <summary>
            /// Reads an unsigned 64-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public ulong ReadUInt64()
            {
                switch (_wireType)
                {
                    case WireType.Varint: return ReadUInt64Varint();
                    case WireType.Fixed32: return ReadRawFixed32();
                    case WireType.Fixed64: return ReadRawFixed64();
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            /// <summary>
            /// Reads a boolean value from the stream; supported wire-types: Variant, Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public bool ReadBoolean() => ReadUInt32() != 0;

            /// <summary>
            /// Reads a double-precision number from the stream; supported wire-types: Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public double ReadDouble()
            {
                switch (_wireType)
                {
                    case WireType.Fixed32:
                        return ReadSingle();
                    case WireType.Fixed64:
                        return BitConverter.Int64BitsToDouble(ReadInt64());
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            /// <summary>
            /// Reads a single-precision number from the stream; supported wire-types: Fixed32, Fixed64
            /// </summary>
            [MethodImpl(HotPath)]
            public float ReadSingle()
            {
                switch (_wireType)
                {
                    case WireType.Fixed32:
                        {
                            uint bits = ReadRawFixed32();
#if NET7_0_OR_GREATER
                            return BitConverter.Int32BitsToSingle(unchecked((int)bits));
#else
                            return Unsafe.As<uint, float>(ref bits);
#endif
                        }
                    case WireType.Fixed64:
                        {
                            double value = ReadDouble();
                            float f = (float)value;
                            if (float.IsInfinity(f) && !double.IsInfinity(value)) ThrowOverflow();
                            return f;
                        }
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            [MethodImpl(HotPath)]
            private static int Zag(uint ziggedValue)
            {
                const int Int32Msb = 1 << 31;
                int value = (int)ziggedValue;
                return (-(value & 0x01)) ^ ((value >> 1) & ~Int32Msb);
            }

            [MethodImpl(HotPath)]
            private static long Zag(ulong ziggedValue)
            {
                const long Int64Msb = 1L << 63;
                long value = (long)ziggedValue;
                return (-(value & 0x01L)) ^ ((value >> 1) & ~Int64Msb);
            }

            // ------------------------------------------------------------ plausibility

            /// <summary>
            /// The maximum number of bytes that could still be read at this point: the lesser of what
            /// the source can supply and what the enclosing length-based sub-message allows. Negative
            /// if neither is knowable (an unbounded stream, outside of any sub-message).
            /// </summary>
            private readonly long GetMaxRemaining()
            {
                long fromSource = _remaining < 0 ? -1 : (_count - _offset) + _remaining;
                if (_scope < 0 || _scope == long.MaxValue) return fromSource; // group / unbounded
                long fromBlock = _scope - Position;
                return fromSource < 0 ? fromBlock : Math.Min(fromSource, fromBlock);
            }

            /// <summary>
            /// Rejects a length taken from the payload when the source provably cannot satisfy it.
            /// Lengths at or below <see cref="ProtoReader.EagerAllocationLimit"/> are not policed,
            /// and neither is anything on a source whose remaining length is unknowable.
            /// </summary>
            [MethodImpl(HotPath)]
            internal void AssertPlausibleLength(long length)
            {
                if (length > ProtoReader.EagerAllocationLimit) AssertPlausibleLengthSlow(length);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void AssertPlausibleLengthSlow(long length)
            {
                var remaining = GetMaxRemaining();
                if (remaining >= 0 && length > remaining) ThrowImplausibleLength(length, remaining);
            }

            /// <summary>
            /// Indicates whether it is safe to allocate <paramref name="length"/> bytes for data that
            /// hasn't been read yet. Throws if the source is known to be too short; returns
            /// <c>false</c> if the length is large and the source can't confirm it.
            /// </summary>
            [MethodImpl(HotPath)]
            internal bool CanAllocate(long length)
                => length <= ProtoReader.EagerAllocationLimit || CanAllocateSlow(length);

            [MethodImpl(MethodImplOptions.NoInlining)]
            private bool CanAllocateSlow(long length)
            {
                var remaining = GetMaxRemaining();
                if (remaining < 0) return false; // unknowable; the caller needs to chunk it
                if (length > remaining) ThrowImplausibleLength(length, remaining);
                return true;
            }

            private byte[] ReadBytesOversized(int length)
            {
                var pool = ArrayPool<byte>.Shared;
                var buffer = pool.Rent(Math.Min(length, ProtoReader.EagerAllocationLimit));
                try
                {
                    int have = 0;
                    while (have < length)
                    {
                        if (have == buffer.Length)
                        {   // double up, but never past what was claimed
                            var larger = pool.Rent((int)Math.Min((long)buffer.Length * 2, length));
                            Buffer.BlockCopy(buffer, 0, larger, 0, have);
                            pool.Return(buffer);
                            buffer = larger;
                        }
                        var take = Math.Min(Math.Min(buffer.Length, length) - have, ProtoReader.EagerAllocationLimit);
                        ReadRawBytesInto(new Span<byte>(buffer, have, take)); // EOF if short
                        have += take;
                    }
                    return buffer;
                }
                catch
                {
                    pool.Return(buffer);
                    throw;
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal string ReadStringOversized(int bytes)
            {
                var buffer = ReadBytesOversized(bytes);
                try
                {
                    return ProtoReader.UTF8.GetString(buffer, 0, bytes);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            // ------------------------------------------------------------ strings and bytes

            /// <summary>
            /// Reads a string from the stream (using UTF8); supported wire-types: String
            /// </summary>
            [MethodImpl(HotPath)]
#pragma warning disable IDE0060 // map isn't implemented yet, but we definitely want it
            public string ReadString(StringMap map = null)
#pragma warning restore IDE0060
            {
                if (_wireType == WireType.String)
                {
                    var s = ReadRawString(); // handles resident/straddle and growth-bounded scratch
                    if (_internStrings) { s = Intern(s); }
                    return s;
                }
                ThrowWireTypeException();
                return default;
            }

            /// <summary>
            /// Reads a byte-sequence from the stream, appending them to an existing byte-sequence (which can be null); supported wire-types: String
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            public byte[] AppendBytes(byte[] value)
                => AppendBytesImpl(value, DefaultMemoryConverter<byte>.Instance);

            /// <summary>
            /// Reads a byte-sequence from the stream, appending them to an existing byte-sequence; supported wire-types: String
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            public ReadOnlyMemory<byte> AppendBytes(ReadOnlyMemory<byte> value)
                => AppendBytesImpl(value, DefaultMemoryConverter<byte>.Instance);

            /// <summary>
            /// Reads a byte-sequence from the stream, appending them to an existing byte-sequence; supported wire-types: String
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            public Memory<byte> AppendBytes(Memory<byte> value)
                => AppendBytesImpl(value, DefaultMemoryConverter<byte>.Instance);

            /// <summary>
            /// Reads a byte-sequence from the stream, appending them to an existing byte-sequence; supported wire-types: String
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            public ArraySegment<byte> AppendBytes(ArraySegment<byte> value)
                => AppendBytesImpl(value, DefaultMemoryConverter<byte>.Instance);

            /// <summary>
            /// Reads a byte-sequence from the stream, appending them to an existing byte-sequence (which can be null); supported wire-types: String
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            public TStorage AppendBytes<TStorage>(TStorage value, IMemoryConverter<TStorage, byte> converter = null)
                => AppendBytesImpl(value, converter ?? DefaultMemoryConverter<byte>.GetFor<TStorage>(Model));

            /// <summary>
            /// Raw-convention append: reads a length-prefixed byte chunk (the tag already consumed
            /// by the caller, per the raw convention) and concatenates it onto an existing
            /// byte-sequence (which may be null) - the legacy merge semantics for a plain bytes
            /// member, which the generated raw read must reproduce. Unlike <see cref="AppendBytes(byte[])"/>
            /// this does not consult the stateful wire type, so it is callable mid-raw-read.
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public byte[] AppendRawBytes(byte[] value)
                => AppendRawBytesCore(value, DefaultMemoryConverter<byte>.Instance);

            /// <inheritdoc cref="AppendRawBytes(byte[])"/>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public ReadOnlyMemory<byte> AppendRawBytes(ReadOnlyMemory<byte> value)
                => AppendRawBytesCore(value, DefaultMemoryConverter<byte>.Instance);

            /// <inheritdoc cref="AppendRawBytes(byte[])"/>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public Memory<byte> AppendRawBytes(Memory<byte> value)
                => AppendRawBytesCore(value, DefaultMemoryConverter<byte>.Instance);

            /// <inheritdoc cref="AppendRawBytes(byte[])"/>
            [MethodImpl(ProtoReader.HotPath)]
            [System.Diagnostics.CodeAnalysis.Experimental("PBN9002")]
            public ArraySegment<byte> AppendRawBytes(ArraySegment<byte> value)
                => AppendRawBytesCore(value, DefaultMemoryConverter<byte>.Instance);

            private TStorage AppendBytesImpl<TStorage>(TStorage value, IMemoryConverter<TStorage, byte> converter)
            {
                switch (_wireType)
                {
                    case WireType.String:
                        // the length read does not consult _wireType, so releasing first is safe
                        // and lets the raw core carry the whole read
                        _wireType = WireType.None;
                        return AppendRawBytesCore(value, converter);
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            private TStorage AppendRawBytesCore<TStorage>(TStorage value, IMemoryConverter<TStorage, byte> converter)
            {
                int len = (int)ReadUInt32Varint(Read32VarintMode.Signed);
                if (len == 0) return converter.NonNull(value);
                if (len < 0) ThrowInvalidLength(len);

                byte[] oversized = CanAllocate(len) ? null : ReadBytesOversized(len);
                try
                {
#if DEBUG
                    var oldLength = converter.GetLength(value);
#endif
                    var newChunk = converter.Expand(Context, ref value, len);
#if DEBUG
                    if (converter.GetLength(value) != (oldLength + len))
                        ThrowHelper.ThrowInvalidOperationException($"The memory converter ({converter.GetType().NormalizeName()}) got the lengths wrong for the updated value; expected {oldLength + len}, got {converter.GetLength(value)}");
                    if (newChunk.Length != len)
                        ThrowHelper.ThrowInvalidOperationException($"The memory converter ({converter.GetType().NormalizeName()}) got the lengths wrong for the returned chunk; expected {len}, got {newChunk.Length}");
#endif
                    if (oversized is null) ReadRawBytesInto(newChunk.Span);
                    else new ReadOnlySpan<byte>(oversized, 0, len).CopyTo(newChunk.Span);
                }
                finally
                {
                    if (oversized is not null) ArrayPool<byte>.Shared.Return(oversized);
                }

                return value;
            }

            /// <summary>
            /// Tries to read a string-like type directly into a span; if successful, the span
            /// returned indicates the available amount of data; if unsuccessful, an exception
            /// is thrown; this should only be used when there is confidence that the length
            /// is bounded.
            /// </summary>
            [Browsable(false)] // hide; not the intended API now due to span scopes
            public Span<byte> ReadBytes(Span<byte> destination)
            {
                ReadBytes(destination, out var length);
                return destination.Slice(0, length);
            }

            /// <summary>
            /// Tries to read a string-like type directly into a span; if successful, the span
            /// returned indicates the available amount of data; if unsuccessful, an exception
            /// is thrown; this should only be used when there is confidence that the length
            /// is bounded.
            /// </summary>
            public void ReadBytes(Span<byte> destination, out int length)
            {
                switch (_wireType)
                {
                    case WireType.String:
                        length = (int)ReadUInt32Varint(Read32VarintMode.Signed);
                        if (length < 0) ThrowInvalidLength(length);
                        if (length > destination.Length)
                            ThrowHelper.ThrowInvalidOperationException($"Insufficient space in the target span to read a string/bytes value; {destination.Length} vs {length} bytes");
                        _wireType = WireType.None;
                        ReadRawBytesInto(destination.Slice(0, length));
                        break;
                    default:
                        length = 0;
                        ThrowWireTypeException();
                        break;
                }
            }

            // ------------------------------------------------------------ sub-items

            /// <summary>
            /// Begins consuming a nested message in the stream; supported wire-types: StartGroup, String
            /// </summary>
            /// <remarks>The token returned must be help and used when callining EndSubItem</remarks>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public SubItemToken StartSubItem()
            {
                switch (_wireType)
                {
                    case WireType.StartGroup:
                        _wireType = WireType.None; // to prevent glitches from double-calling
                        if (IncrDepthExceeded()) ThrowTooDeep();
                        return new SubItemToken((long)(-_fieldNumber));
                    case WireType.String:
                        long len = (long)ReadUInt64Varint();
                        if (len < 0) ThrowInvalidOperationException();
                        // deliberately *not* vetting len against the source length here: nothing is
                        // allocated from it, and callers depend on corruption being reported by the
                        // existing end-group/sub-message checks instead (see issue 697). The
                        // allocating paths still bound themselves via the block end.
                        long lastEnd = _scope;
                        _scope = Position + len;
                        RecomputeEffectiveEnd();
                        if (IncrDepthExceeded()) ThrowTooDeep();
                        return new SubItemToken(lastEnd);
                    default:
                        ThrowWireTypeException();
                        return default;
                }
            }

            /// <summary>
            /// Makes the end of consuming a nested message in the stream; the stream must be either at the correct EndGroup
            /// marker, or all fields of the sub-message must have been consumed (in either case, this means ReadFieldHeader
            /// should return zero)
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void EndSubItem(SubItemToken token)
            {
                long value64 = token.value64;
                switch (_wireType)
                {
                    case WireType.EndGroup:
                        if (value64 >= 0) ThrowProtoException("A length-based message was terminated via end-group; this indicates data corruption");
                        if (-(int)value64 != _fieldNumber) ThrowProtoException("Wrong group was ended"); // wrong group ended!
                        _wireType = WireType.None; // this releases ReadFieldHeader
                        _depth--;
                        break;
                    default:
                        long position = Position;
                        if (value64 < position) ThrowProtoException($"Sub-message not read entirely; expected {value64}, was {position}");
                        if (_scope != position && _scope != long.MaxValue)
                        {
                            ThrowProtoException($"Sub-message not read correctly (end {_scope} vs {position})");
                        }
                        _scope = value64;
                        RecomputeEffectiveEnd();
                        _depth--;
                        break;
                }
            }

            /// <summary>
            /// Discards the data for the current field.
            /// </summary>
            [MethodImpl(HotPath)]
            public void SkipField()
            {
                switch (_wireType)
                {
                    case WireType.Fixed32:
                        Advance(4);
                        break;
                    case WireType.Fixed64:
                        Advance(8);
                        break;
                    case WireType.String:
                        long len = (long)ReadUInt64Varint();
                        if (len < 0) ThrowInvalidLength(len);
                        Advance(checked((int)len));
                        break;
                    case WireType.Varint:
                    case WireType.SignedVarint:
                        ReadUInt64Varint(); // and drop it
                        break;
                    case WireType.StartGroup:
                        SkipGroupField();
                        break;
                    case WireType.None: // treat as explicit error
                    case WireType.EndGroup: // treat as explicit error
                    default: // treat as implicit error
                        ThrowWireTypeException();
                        break;
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void SkipGroupField()
            {
                int originalFieldNumber = _fieldNumber;
                if (IncrDepthExceeded()) ThrowTooDeep(); // need to satisfy the sanity-checks in ReadFieldHeader
                while (ReadFieldHeader() > 0) { SkipField(); }
                _depth--;
                if (_wireType == WireType.EndGroup && _fieldNumber == originalFieldNumber)
                { // we expect to exit in a similar state to how we entered
                    _wireType = WireType.None;
                    return;
                }
                ThrowWireTypeException();
            }

            internal void SkipAllFields()
            {
                while (ReadFieldHeader() > 0) SkipField();
            }

            // ------------------------------------------------------------ repeated engine

            [MethodImpl(ProtoReader.HotPath)]
            private void PrepareToReadRepeated<T>(ref SerializerFeatures features, SerializerFeatures serializerFeatures, out SerializerFeatures category, out bool packed)
            {
                if (serializerFeatures.IsRepeated()) TypeModel.ThrowNestedListsNotSupported(typeof(T));
                features.InheritFrom(serializerFeatures);
                category = serializerFeatures.GetCategory();
                packed = false;
                if (TypeHelper<T>.CanBePacked && WireType == WireType.String && !features.HasAny(SerializerFeatures.OptionWrappedValue))
                {
                    // the wire type should never by "string" for a type that *can* be
                    // packed, so this *is* packed
                    if (category != SerializerFeatures.CategoryScalar)
                        ThrowInvalidOperationException("Packed data expected a scalar serializer");

                    packed = true;
                }
            }

            [MethodImpl(ProtoReader.HotPath)]
            private void ReadRepeatedCore<TSerializer, TList, T>(ref TList values, SerializerFeatures category, WireType wireType, in TSerializer serializer, SerializerFeatures features)
                where TSerializer : ISerializer<T>
                where TList : ICollection<T>
            {
                int field = FieldNumber;
                bool isWrapped = features.HasAny(SerializerFeatures.OptionWrappedValue);
                var initialValue = features.DefaultFor<T>();
                do
                {
                    T element;
                    if (isWrapped)
                    {
                        element = ReadWrapped<T>(features, initialValue, serializer);
                    }
                    else
                    {
                        switch (category)
                        {
                            case SerializerFeatures.CategoryScalar:
                                Hint(wireType);
                                element = serializer.Read(ref this, initialValue);
                                break;
                            case SerializerFeatures.CategoryMessage:
                            case SerializerFeatures.CategoryMessageWrappedAtRoot:
                                element = ReadMessage<TSerializer, T>(default, initialValue, serializer);
                                break;
                            default:
                                category.ThrowInvalidCategory();
                                element = default;
                                break;
                        }
                    }
                    values.Add(element);
                } while (TryReadFieldHeader(field));
            }

            [MethodImpl(ProtoReader.HotPath)]
            private void ReadPackedScalar<TSerializer, TList, T>(ref TList list, WireType wireType, in TSerializer serializer)
                where TSerializer : ISerializer<T>
                where TList : ICollection<T>
            {
                var bytes = (int)ReadUInt32Varint(Read32VarintMode.Unsigned);
                if (bytes == 0) return;
                if (bytes < 0) ThrowInvalidLength(bytes);
                AssertPlausibleLength(bytes);
                switch (wireType)
                {
                    case WireType.Fixed32:
                        if ((bytes % 4) != 0) ThrowHelper.ThrowInvalidOperationException("packed length should be multiple of 4");
                        var count = bytes / 4;
                        goto ReadFixedQuantity;
                    case WireType.Fixed64:
                        if ((bytes % 8) != 0) ThrowHelper.ThrowInvalidOperationException("packed length should be multiple of 8");
                        count = bytes / 8;
                    ReadFixedQuantity:
                        // boost the List<T> capacity if we can, as long as it is within reason
                        const int MAX_GROW = 8192;
                        if (list is List<T> l) l.Capacity = Math.Max(l.Capacity, l.Count + Math.Min(count, MAX_GROW));

                        for (int i = 0; i < count; i++)
                        {
                            _wireType = wireType;
                            list.Add(serializer.Read(ref this, default));
                        }
                        break;
                    case WireType.Varint:
                    case WireType.SignedVarint:
                        long end = GetPosition() + bytes;
                        do
                        {
                            _wireType = wireType;
                            list.Add(serializer.Read(ref this, default));
                        } while (GetPosition() < end);
                        if (GetPosition() != end) ThrowHelper.ThrowInvalidOperationException("over-read packed data");
                        break;
                    default:
                        ThrowWireTypeException();
                        break;
                }
            }

            internal ReadBuffer<T> FillBuffer<TSerializer, T>(SerializerFeatures features, in TSerializer serializer, T initialValue)
                where TSerializer : ISerializer<T>
            {
                PrepareToReadRepeated<T>(ref features, serializer.Features, out var category, out var packed);
                var buffer = ReadBuffer<T>.Create();
                try
                {
                    var wireType = features.GetWireType();
                    if (packed) ReadPackedScalar<TSerializer, ReadBuffer<T>, T>(ref buffer, wireType, serializer);
                    else ReadRepeatedCore<TSerializer, ReadBuffer<T>, T>(ref buffer, category, wireType, serializer, features);
                    return buffer;
                }
                catch
                {
                    try { buffer.Dispose(); } catch { }
                    throw;
                }
            }

            // ------------------------------------------------------------ objects / aux

            /// <summary>
            /// Reads (merges) a sub-message from the stream, internally calling StartSubItem and EndSubItem, and (in between)
            /// parsing the message in accordance with the model associated with the reader
            /// </summary>
            [MethodImpl(HotPath)]
            internal object ReadObject(object value, Type type) => ReadTypedObject(value, type);

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal object ReadTypedObject(object value, Type type)
            {
                var model = Model;
                if (model is null) ThrowInvalidOperationException("Cannot deserialize sub-objects unless a model is provided");

                if (DynamicStub.TryDeserialize(ObjectScope.WrappedMessage, type, model, ref this, ref value))
                    return value;

                SubItemToken token = StartSubItem();
                if (type is not null && model.TryDeserializeAuxiliaryType(ref this, DataFormat.Default, TypeModel.ListItemTag, type, ref value, true, false, true, false, null, isRoot: false))
                {
                    // handled it the easy way
                }
                else
                {
                    TypeModel.ThrowUnexpectedType(type, Model);
                }
                EndSubItem(token);
                return value;
            }

            internal readonly Type DeserializeType(string typeName)
                => TypeModel.DeserializeType(_model, typeName);

            /// <summary>
            /// Reads a Type from the stream, using the model's DynamicTypeFormatting if appropriate; supported wire-types: String
            /// </summary>
            [MethodImpl(HotPath)]
            public Type ReadType() => TypeModel.DeserializeType(_model, ReadString());

            // ------------------------------------------------------------ extension data

            /// <summary>
            /// Copies the current field into the instance as extension data
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void AppendExtensionData(IExtensible instance)
            {
                if (instance is null) ThrowHelper.ThrowArgumentNullException(nameof(instance));
                // reconstruct the tag from state (the join the raw path never splits), then the
                // raw capture: byte-preserving, allocation-free - replacing the legacy
                // ProtoWriter-based re-encode outright
                AppendExtensionData((uint)((_fieldNumber << 3) | ((int)_wireType & 7)), instance);
                _wireType = WireType.None;
            }

            /// <summary>
            /// Copies the current field into the instance as extension data
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void AppendExtensionData(ITypedExtensible instance, Type type)
            {
                if (instance is null) ThrowHelper.ThrowArgumentNullException(nameof(instance));
                AppendExtensionData((uint)((_fieldNumber << 3) | ((int)_wireType & 7)), instance, type);
                _wireType = WireType.None;
            }

            // ------------------------------------------------------------ message family

            /// <summary>
            /// Reads a sub-item from the input reader
            /// </summary>
            [MethodImpl(HotPath)]
            public T ReadMessage<T>(T value = default)
                => ReadMessage<T>(default, value, null);

            /// <summary>
            /// Reads a sub-item from the input reader
            /// </summary>
            [MethodImpl(ProtoReader.HotPath)]
            public T ReadMessage<T>(SerializerFeatures features, T value = default, ISerializer<T> serializer = null)
                => ReadMessage<ISerializer<T>, T>(features, value, serializer ?? TypeModel.ResolveSerializer<T>(Model));

#pragma warning disable IDE0060 // unused (yet!) features arg
            /// <summary>
            /// Reads a sub-item from the input reader
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            internal T ReadMessage<TSerializer, T>(SerializerFeatures features, T value, in TSerializer serializer)
                where TSerializer : ISerializer<T>
#pragma warning restore IDE0060
            {
                var tok = StartSubItem();
                var result = serializer.Read(ref this, value);
                EndSubItem(tok);
                return result;
            }

            /// <summary>
            /// Reads a value or sub-item from the input reader
            /// </summary>
            [MethodImpl(HotPath)]
            public T ReadAny<T>(T value = default)
                => ReadAny<T>(default, value, null);

            /// <summary>
            /// Reads a value or sub-item from the input reader
            /// </summary>
            [MethodImpl(HotPath)]
            public T ReadAny<T>(SerializerFeatures features, T value = default, ISerializer<T> serializer = null)
            {
                serializer ??= TypeModel.ResolveSerializer<T>(Model);
                var serializerFeatures = serializer.Features;
                features.InheritFrom(serializerFeatures);

                if (features.HasAny(SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedCollection))
                {
                    return ReadWrapped<T>(features, value, serializer);
                }

                switch (serializerFeatures.GetCategory())
                {
                    case SerializerFeatures.CategoryMessage:
                    case SerializerFeatures.CategoryMessageWrappedAtRoot:
                        return ReadMessage<T>(features, value, serializer);
                    case SerializerFeatures.CategoryRepeated:
                        return ((IRepeatedSerializer<T>)serializer).ReadRepeated(ref this, features, value);
                    case SerializerFeatures.CategoryScalar:
                        features.HintIfNeeded(ref this);
                        return serializer.Read(ref this, value);
                    default:
                        features.ThrowInvalidCategory();
                        return default;
                }
            }

            /// <summary>
            /// Read a value or sub-item with an additional level of message wrapping, that can be used to express <c>null</c> values of arbitrary types (as field 1)
            /// </summary>
            public T ReadWrapped<T>(SerializerFeatures features, T value, ISerializer<T> serializer = null)
            {
                serializer ??= TypeModel.ResolveSerializer<T>(Model);
                features.InheritFrom(serializer.Features);

                ProtoWriter.State.AssertWrappedAndGetWireType(ref features, out var fieldPresence);
                var tok = StartSubItem();
                int field;
                while ((field = ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case 1:
                            // read the inner value (note: wrap options alreay removed to avoid recursion)
                            value = ReadAny(features, value, serializer);
                            break;
                        default:
                            SkipField();
                            break;
                    }
                }
                EndSubItem(tok);
                if (!fieldPresence && TypeHelper<T>.CanBeNull && TypeHelper<T>.ValueChecker.IsNull(value))
                {
                    // even if the field isn't found, the fact that we had the wrapper at all means that
                    // we shouldn't return null
                    value = TypeHelper<T>.NonTrivialDefault;
                }

                return value;
            }

            /// <summary>
            /// Gets the serializer associated with a specific type
            /// </summary>
            [MethodImpl(HotPath)]
            public ISerializer<T> GetSerializer<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>() => TypeModel.GetSerializer<T>(Model);

            /// <summary>
            /// Reads a sub-item from the input reader
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public T ReadBaseType<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TBaseType, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(T value = null, ISubTypeSerializer<TBaseType> serializer = null)
                where TBaseType : class
                where T : class, TBaseType
            {
                return (T)(serializer ?? TypeModel.GetSubTypeSerializer<TBaseType>(_model)).ReadSubType(ref this, SubTypeState<TBaseType>.Create<T>(Context, value));
            }

            /// <summary>
            /// Creates a new instance of the supplied type
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public T CreateInstance<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(ISerializer<T> serializer = null)
            {
                var obj = TypeModel.CreateInstance<T>(Context, serializer);
#if FEAT_DYNAMIC_REF
                if (TypeHelper<T>.IsReferenceType) NoteObject(obj);
#endif
                return obj;
            }

            // ------------------------------------------------------------ roots

            /// <summary>
            /// Deserialize an instance of the provided type
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public T DeserializeRoot<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(T value = default, ISerializer<T> serializer = null)
            {
                value = ReadAsRoot<T>(value, serializer ?? TypeModel.GetSerializer<T>(Model));
                CheckFullyConsumed();
                return value;
            }

            internal T ReadAsRoot<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(T value, ISerializer<T> serializer)
            {
                var features = serializer.Features;
                var category = features.GetCategory();

                switch (category)
                {
                    case SerializerFeatures.CategoryMessageWrappedAtRoot:
                        // to preserve legacy behavior of DateTime/TimeSpan etc
                        return ReadFieldOne(ref this, features, value, serializer);
                    case SerializerFeatures.CategoryMessage:
#if FEAT_DYNAMIC_REF
                        if (TypeHelper<T>.IsReferenceType && value is object)
                            SetRootObject(value);
#endif
                        return serializer.Read(ref this, value);
                    case SerializerFeatures.CategoryRepeated:
                    case SerializerFeatures.CategoryScalar:
                        return ReadFieldOne(ref this, features, value, serializer);
                    default:
                        features.ThrowInvalidCategory();
                        return default;
                }

                static T ReadFieldOne(ref State state, SerializerFeatures features, T value, ISerializer<T> serializer)
                {
                    int field;
                    bool found = false;
                    while ((field = state.ReadFieldHeader()) > 0)
                    {
                        if (field == 1)
                        {
                            found = true;
                            value = state.ReadAny<T>(features, value, serializer);
                        }
                        else
                        {
                            state.SkipField();
                        }
                    }
                    if (TypeHelper<T>.IsReferenceType && !found && value is null)
                    {
                        value = state.CreateInstance<T>(serializer);
                    }
                    return value;
                }
            }

            internal object DeserializeRootFallbackWithModel(object value, Type type, TypeModel overrideModel)
            {
                var oldModel = _model;
                try
                {
                    _model = overrideModel;
                    return DeserializeRootFallback(value, type);
                }
                finally
                {
                    _model = oldModel;
                }
            }

            internal object DeserializeRootFallback(object value, Type type)
            {
                bool autoCreate = TypeModel.PrepareDeserialize(value, ref type);
                object obj = Model.DeserializeRootAny(ref this, type, value, autoCreate);
                CheckFullyConsumed();
                return obj;
            }

            internal T DeserializeRootImpl<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>(T value = default)
            {
                var serializer = TypeModel.TryGetSerializer<T>(Model);
                if (serializer is null)
                {
                    return (T)DeserializeRootFallback(value, typeof(T));
                }
                else
                {
                    return DeserializeRoot<T>(value, serializer);
                }
            }

            /// <summary>
            /// Indicates whether the reader still has data remaining in the current sub-item,
            /// additionally setting the wire-type for the next field if there is more data.
            /// </summary>
            public bool HasSubValue(WireType wireType)
            {
                // check for virtual end of stream
                if ((_scope >= 0 && _scope != long.MaxValue && Position >= _scope) || _wireType == WireType.EndGroup)
                {
                    return false;
                }
                _wireType = wireType;
                return true;
            }

            // ------------------------------------------------------------ error helpers

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowWireTypeException()
            {
                var message = $"Invalid wire-type ({_wireType}); this usually means you have over-written a file without truncating or setting the length; see https://stackoverflow.com/q/2152978/23354 (pos={Position}, scope={_scope}, depth={_depth}, field={_fieldNumber}, offset={_offset}, count={_count}, effEnd={_effectiveEnd})";
                ThrowProtoException(message);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowProtoException(string message)
            {
                throw AddErrorData(new ProtoException(message), ref this);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowEoF()
            {
                throw AddErrorData(new EndOfStreamException(), ref this);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowInvalidOperationException(string message = null)
            {
                var ex = string.IsNullOrWhiteSpace(message) ? new InvalidOperationException() : new InvalidOperationException(message);
                throw AddErrorData(ex, ref this);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowInvalidLength(long length) => ThrowInvalidOperationException("Invalid length: " + length.ToString());

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowImplausibleLength(long length, long remaining)
                => ThrowInvalidOperationException($"Invalid length: {length}; the source has at most {remaining} bytes remaining");

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowArgumentException(string message)
            {
                throw AddErrorData(new ArgumentException(message), ref this);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            internal void ThrowOverflow()
            {
                throw AddErrorData(new OverflowException(), ref this);
            }

            /// <summary>
            /// Throws an exception indication that the given value cannot be mapped to an enum.
            /// </summary>
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void ThrowEnumException(Type type, int value)
            {
                string desc = type is null ? "<null>" : type.FullName;
                throw AddErrorData(new ProtoException("No " + desc + " enum is mapped to the wire-value " + value.ToString()), ref this);
            }

            internal static Exception AddErrorData(Exception exception, ref State state)
            {
                if (exception is not null && !exception.Data.Contains("protoSource"))
                {
                    exception.Data.Add("protoSource", string.Format("tag={0}; wire-type={1}; offset={2}; depth={3}",
                        state._fieldNumber, state._wireType, state.GetPosition(), state._depth));
                }
                return exception;
            }
        }

        /// <summary>
        /// The serialization-context shim: what the pooled reader instance used to be for the
        /// Context property - a tiny lazily-allocated holder instead of a pooled machine.
        /// </summary>
        internal sealed class StateContext : ISerializationContext
        {
            private readonly TypeModel _model;
            private readonly object _userState;
            internal StateContext(TypeModel model, object userState)
            {
                _model = model;
                _userState = userState;
            }
            TypeModel ISerializationContext.Model => _model;
            object ISerializationContext.UserState => _userState;
        }
    }
}
