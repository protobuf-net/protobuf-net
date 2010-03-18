
using System;

using System.IO;
using System.Text;
using ProtoBuf.Meta;
#if MF
using EndOfStreamException = System.ApplicationException;
using OverflowException = System.ApplicationException;
#endif

namespace ProtoBuf
{
    /// <summary>
    /// A stateful reader, used to read a protobuf stream. Typical usage would be (sequentially) to call
    /// ReadFieldHeader and (after matching the field) an appropriate Read* method.
    /// </summary>
    public sealed class ProtoReader : IDisposable
    {
        Stream source;
        byte[] ioBuffer;
        TypeModel model;

        private int fieldNumber;
        WireType wireType = WireType.None;
        public int FieldNumber { get { return fieldNumber; } }
        
        internal ProtoReader(Stream source, TypeModel model) :
            this(source, model, -1)
        {}
        private int dataRemaining;
        private readonly bool isFixedLength;
        internal ProtoReader(Stream source, TypeModel model, int length)
        {
            if (source == null) throw new ArgumentNullException("dest");
            if (!source.CanRead) throw new ArgumentException("Cannot read from stream", "dest");
            this.source = source;
            this.ioBuffer = BufferPool.GetBuffer();
            this.model = model;
            isFixedLength = length > 0;
            dataRemaining = isFixedLength ? length : 0;
        }
        /// <summary>
        /// Releases resources used by the reader, but importantly <b>does not</b> Dispose the 
        /// underlying stream; in many typical use-cases the stream is used for different
        /// processes, so it is assumed that the consumer will Dispose their stream separately.
        /// </summary>
        public void Dispose()
        {
            // importantly, this does **not** own the stream, and does not dispose it
            source = null;
            model = null;
            BufferPool.ReleaseBufferToPool(ref ioBuffer);
        }
        private int TryReadUInt32VariantWithoutMoving(bool trimNegative, out uint value)
        {
            if(available < 10) Ensure(10, false);
            if (available == 0)
            {
                value = 0;
                return 0;
            }
            int readPos = ioIndex;
            value = ioBuffer[readPos++];
            if ((value & 0x80) == 0) return 1;
            value &= 0x7F;
            if (available == 1) throw new EndOfStreamException();

            uint chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 7;
            if ((chunk & 0x80) == 0) return 2;
            if (available == 2) throw new EndOfStreamException();

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 14;
            if ((chunk & 0x80) == 0) return 3;
            if (available == 3) throw new EndOfStreamException();

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 21;
            if ((chunk & 0x80) == 0) return 4;
            if (available == 4) throw new EndOfStreamException();

            chunk = ioBuffer[readPos];
            value |= chunk << 28; // can only use 4 bits from this chunk
            if ((chunk & 0xF0) == 0) return 5;

            if (trimNegative // allow for -ve values
                && (chunk & 0xF0) == 0xF0
                && available >= 10
                    && ioBuffer[++readPos] == 0xFF
                    && ioBuffer[++readPos] == 0xFF
                    && ioBuffer[++readPos] == 0xFF
                    && ioBuffer[++readPos] == 0xFF
                    && ioBuffer[++readPos] == 0x01)
            {
                return 10;
            }
            throw new OverflowException();
        }
        private uint ReadUInt32Variant(bool trimNegative)
        {
            uint value;
            int read = TryReadUInt32VariantWithoutMoving(trimNegative, out value);
            if (read > 0)
            {
                ioIndex += read;
                available -= read;
                position += read;
                return value;
            }
            throw new EndOfStreamException();
        }
        private bool TryReadUInt32Variant(out uint value)
        {
            int read = TryReadUInt32VariantWithoutMoving(false, out value);
            if (read > 0)
            {
                ioIndex += read;
                available -= read;
                position += read;
                return true;
            }
            return false;
        }
        /// <summary>
        /// Reads an unsigned 32-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        public uint ReadUInt32()
        {
            switch (wireType)
            {
                case WireType.Variant:
                    return ReadUInt32Variant(false);
                case WireType.Fixed32:
                    if (available < 4) Ensure(4, true);
                    position += 4;
                    available -= 4;
                    return ((uint)ioBuffer[ioIndex++])
                        | (((uint)ioBuffer[ioIndex++]) << 8)
                        | (((uint)ioBuffer[ioIndex++]) << 16)
                        | (((uint)ioBuffer[ioIndex++]) << 24);
                case WireType.Fixed64:
                    ulong val = ReadUInt64();
                    checked { return (uint)val; }
                default:
                    throw BorkedIt();
            }
        }
        int ioIndex, position, available; // maxPosition
        /// <summary>
        /// Returns the position of the current reader (note that this is not necessarily the same as the position
        /// in the underlying stream, if multiple readers are used on the same stream)
        /// </summary>
        public int Position { get { return position; } }
        internal void Ensure(int count, bool strict)
        {
            Helpers.DebugAssert(available <= count, "Asking for data without checking first");
            Helpers.DebugAssert(count <= ioBuffer.Length, "Asking for too much data");

            if (ioIndex + count >= ioBuffer.Length)
            {
                // need to shift the buffer data to the left to make space
                Helpers.BlockCopy(ioBuffer, ioIndex, ioBuffer, 0, available);
                ioIndex = 0;
            }
            count -= available;
            int writePos = ioIndex + available, bytesRead;
            int canRead = ioBuffer.Length - writePos;
            if (isFixedLength)
            {   // throttle it if needed
                if (dataRemaining < canRead) canRead = dataRemaining;
            }
            while (count > 0 && (bytesRead = source.Read(ioBuffer, writePos, canRead)) > 0)
            {
                available += bytesRead;
                count -= bytesRead;
                canRead -= bytesRead;
                writePos += bytesRead;
                if(isFixedLength) { dataRemaining -= bytesRead;}
            }
            if (strict && count > 0)
            {
                throw new EndOfStreamException();
            }

        }
        /// <summary>
        /// Reads a signed 16-bit integer from the stream: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public short ReadInt16()
        {
            checked { return (short)ReadInt32(); }
        }
        /// <summary>
        /// Reads an unsigned 16-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        public ushort ReadUInt16()
        {
            checked { return (ushort)ReadUInt32(); }
        }
        /// <summary>
        /// Reads a signed 32-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public int ReadInt32()
        {
            switch (wireType)
            {
                case WireType.Variant:
                    return (int)ReadUInt32Variant(true);
                case WireType.Fixed32:
                    if(available < 4) Ensure(4, true);
                    position += 4;
                    available -= 4;
                    return ((int)ioBuffer[ioIndex++])
                        | (((int)ioBuffer[ioIndex++]) << 8)
                        | (((int)ioBuffer[ioIndex++]) << 16)
                        | (((int)ioBuffer[ioIndex++]) << 24);
                case WireType.Fixed64:
                    long l = ReadInt64();
                    checked { return (int)l; }
                case WireType.SignedVariant:
                    return Zag(ReadUInt32Variant(true));
                default:
                    throw BorkedIt();
            }
        }
        private const long Int64Msb = ((long)1) << 63;
        private const int Int32Msb = ((int)1) << 31;
        private static int Zag(uint ziggedValue)
        {
            int value = (int)ziggedValue;
            return (-(value & 0x01)) ^ ((value >> 1) & ~ProtoReader.Int32Msb);
        }

        private static long Zag(ulong ziggedValue)
        {
            long value = (long)ziggedValue;
            return (-(value & 0x01L)) ^ ((value >> 1) & ~ProtoReader.Int64Msb);
        }
        /// <summary>
        /// Reads a signed 64-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64, SignedVariant
        /// </summary>
        public long ReadInt64()
        {
            switch (wireType)
            {
                case WireType.Variant:
                    return (long)ReadUInt64Variant();
                case WireType.Fixed32:
                    return ReadInt32();
                case WireType.Fixed64:
                    if (available < 8) Ensure(8, true);
                    position += 8;
                    available -= 8;

                    return ((long)ioBuffer[ioIndex++])
                        | (((long)ioBuffer[ioIndex++]) << 8)
                        | (((long)ioBuffer[ioIndex++]) << 16)
                        | (((long)ioBuffer[ioIndex++]) << 24)
                        | (((long)ioBuffer[ioIndex++]) << 32)
                        | (((long)ioBuffer[ioIndex++]) << 40)
                        | (((long)ioBuffer[ioIndex++]) << 48)
                        | (((long)ioBuffer[ioIndex++]) << 56);

                case WireType.SignedVariant:
                    return Zag(ReadUInt64Variant());
                default:
                    throw BorkedIt();
            }
        }

        private int TryReadUInt64VariantWithoutMoving(out ulong value)
        {
            if (available < 10) Ensure(10, false);
            if (available == 0)
            {
                value = 0;
                return 0;
            }
            int readPos = ioIndex;
            value = ioBuffer[readPos++];
            if ((value & 0x80) == 0) return 1;
            value &= 0x7F;
            if (available == 1) throw new EndOfStreamException();

            ulong chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 7;
            if ((chunk & 0x80) == 0) return 2;
            if (available == 2) throw new EndOfStreamException();

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 14;
            if ((chunk & 0x80) == 0) return 3;
            if (available == 3) throw new EndOfStreamException();

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 21;
            if ((chunk & 0x80) == 0) return 4;
            if (available == 4) throw new EndOfStreamException();

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 28;
            if ((chunk & 0x80) == 0) return 5;
            if (available == 5) throw new EndOfStreamException();

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 35;
            if ((chunk & 0x80) == 0) return 6;
            if (available == 6) throw new EndOfStreamException();

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 42;
            if ((chunk & 0x80) == 0) return 7;
            if (available == 7) throw new EndOfStreamException();


            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 49;
            if ((chunk & 0x80) == 0) return 8;
            if (available == 8) throw new EndOfStreamException();

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 56;
            if ((chunk & 0x80) == 0) return 9;
            if (available == 9) throw new EndOfStreamException();

            chunk = ioBuffer[readPos];
            value |= chunk << 63; // can only use 1 bit from this chunk

            if ((chunk & ~(ulong)0x01) != 0) throw new OverflowException();
            return 10;            
        }
        
        private ulong ReadUInt64Variant()
        {
            ulong value;
            int read = TryReadUInt64VariantWithoutMoving(out value);
            if (read > 0)
            {
                ioIndex += read;
                available -= read;
                position += read;
                return value;
            }
            throw new EndOfStreamException();
        }

        static readonly UTF8Encoding encoding = new UTF8Encoding();
        /// <summary>
        /// Reads a string from the stream (using UTF8); supported wire-types: String
        /// </summary>
        public string ReadString()
        {
            if (wireType == WireType.String)
            {
                int bytes = (int)ReadUInt32Variant(false);
                if (bytes == 0) return "";
                if (available < bytes) Ensure(bytes, true);
#if MF
                byte[] tmp;
                if(ioIndex == 0 && bytes == ioBuffer.Length) {
                    // unlikely, but...
                    tmp = ioBuffer;
                } else {
                    tmp = new byte[bytes];
                    Helpers.BlockCopy(ioBuffer, ioIndex, tmp, 0, bytes);
                }
                string s = new string(encoding.GetChars(tmp));
#else
                string s = encoding.GetString(ioBuffer, ioIndex, bytes);
#endif
                available -= bytes;
                position += bytes;
                ioIndex += bytes;
                return s;
            }
            throw BorkedIt();
        }
        private Exception BorkedIt()
        {
            throw new ProtoException();
        }
        /// <summary>
        /// Reads a double-precision number from the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        public unsafe double ReadDouble()
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                    return ReadSingle();
                case WireType.Fixed64:
                    long value = ReadInt64();
                    return *(double*)&value;
                default:
                    throw BorkedIt();
            }
        }

        /// <summary>
        /// Reads (merges) a sub-message from the stream, internally calling StartSubItem and EndSubItem, and (in between)
        /// parsing the message in accordance with the model associated with the reader
        /// </summary>
        public static object ReadObject(object value, int key, ProtoReader reader)
        {
            if (reader.model == null)
            {
                throw new InvalidOperationException("Cannot deserialize sub-objects unless a model is provided");
            }
            SubItemToken token = ProtoReader.StartSubItem(reader);
            value = reader.model.Deserialize(key, value, reader);
            ProtoReader.EndSubItem(token, reader);
            return value;
        }

        /// <summary>
        /// Makes the end of consuming a nested message in the stream; the stream must be either at the correct EndGroup
        /// marker, or all fields of the sub-message must have been consumed (in either case, this means ReadFieldHeader
        /// should return zero)
        /// </summary>
        public static void EndSubItem(SubItemToken token, ProtoReader reader)
        {
            int value = token.value;
            switch (reader.wireType)
            {
                case WireType.EndGroup:
                    if (value >= 0) throw new ArgumentException("token");
                    if (-value != reader.fieldNumber) throw reader.BorkedIt(); // wrong group ended!
                    reader.wireType = WireType.None; // this releases ReadFieldHeader
                    break;
                //case WireType.None: // TODO reinstate once reads reset the wire-type
                default:
                    if (value < reader.position) throw new ArgumentException("token");
                    if (reader.blockEnd != reader.position && reader.blockEnd != int.MaxValue) throw reader.BorkedIt();
                    reader.blockEnd = value;
                    break;
                /*default:
                    reader.BorkedIt(); // throws
                    break;*/
            }
        }

        /// <summary>
        /// Begins consuming a nested message in the stream; supported wire-types: StartGroup, String
        /// </summary>
        /// <remarks>The token returned must be help and used when callining EndSubItem</remarks>
        public static SubItemToken StartSubItem(ProtoReader reader)
        {
            switch (reader.wireType)
            {
                case WireType.StartGroup:
                    reader.wireType = WireType.None; // to prevent glitches from double-calling
                    return new SubItemToken(-reader.fieldNumber);
                case WireType.String:
                    int len = (int)reader.ReadUInt32Variant(false);
                    if (len < 0) throw new InvalidOperationException();
                    int lastEnd = reader.blockEnd;
                    reader.blockEnd = reader.position + len;
                    return new SubItemToken(lastEnd);
                default:
                    reader.BorkedIt(); // throws
                    return new SubItemToken(0);
            }
        }

        int blockEnd = int.MaxValue;
        /// <summary>
        /// Reads a field header from the stream, setting the wire-type and retuning the field number. If no
        /// more fields are available, then 0 is returned. This methods respects sub-messages.
        /// </summary>
        public int ReadFieldHeader()
        {
            // at the end of a group the caller must call EndSubItem to release the
            // reader (which moves the status to Error, since ReadFieldHeader must
            // then be called)
            if (blockEnd <= position || wireType == WireType.EndGroup) { return 0; } 
            uint tag;
            if (TryReadUInt32Variant(out tag))
            {
                wireType = (WireType)(tag & 7);
                fieldNumber = (int)(tag >> 3);
            } else {
                wireType = WireType.None;
                fieldNumber = 0;
            }
            // watch for end-of-group
            return wireType == WireType.EndGroup ? 0 : fieldNumber; 
        }
        /// <summary>
        /// Looks ahead to see whether the next field in the stream is what we expect
        /// (typically; what we've just finished reading - for example ot read successive list items)
        /// </summary>
        public bool TryReadFieldHeader(int field)
        {
            // check for virtual end of stream
            if (blockEnd <= position || wireType == WireType.EndGroup) { return false; }
            uint tag;
            int read = TryReadUInt32VariantWithoutMoving(false, out tag);
            WireType tmpWireType; // need to catch this to exclude (early) any "end group" tokens
            if (read > 0 && ((int)tag >> 3) == field
                && (tmpWireType = (WireType)(tag & 7)) != WireType.EndGroup)
            {
                wireType = tmpWireType;
                fieldNumber = field;                
                position += read;
                ioIndex += read;
                available -= read;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Compares the streams current wire-type to the hinted wire-type, updating the reader if necessary; for example,
        /// a Variant may be updated to SignedVariant. If the hinted wire-type is unrelated then no change is made.
        /// </summary>
        public void Hint(WireType wireType)
        {
            if (this.wireType == wireType) { }  // fine; everything as we expect
            else if (((int)wireType & 7) == (int)this.wireType)
            {   // the underling type is a match; we're customising it with an extension
                this.wireType = wireType;
            }
            // note no error here; we're OK about using alternative data
        }

        /// <summary>
        /// Verifies that the stream's current wire-type is as expected, or a specialized sub-type (for example,
        /// SignedVariant) - in which case the current wire-type is updated. Otherwise an exception is thrown.
        /// </summary>
        public void Assert(WireType wireType)
        {
            if (this.wireType == wireType) {}  // fine; everything as we expect
            else if (((int)wireType & 7) == (int)this.wireType)
            {   // the underling type is a match; we're customising it with an extension
                this.wireType = wireType;
            }
            else
            {   // nope; that is *not* what we were expecting!
                throw BorkedIt();
            }
        }
        
        /// <summary>
        /// Discards the data for the current field.
        /// </summary>
        public void SkipField()
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                    Ensure(4, true);
                    available -= 4;
                    ioIndex += 4;
                    position += 4;
                    return;
                case WireType.Fixed64:
                    Ensure(8, true);
                    available -= 8;
                    ioIndex += 8;
                    position += 8;
                    return;
                case WireType.String:
                    int len = (int)ReadUInt32Variant(false);
                    if (len <= available)
                    { // just jump it!
                        available -= len;
                        ioIndex += len;
                        position += len;
                        return;
                    }
                    // everything remaining in the buffer is garbage
                    position += len; // assumes success, but if it fails we're screwed anyway
                    len -= available; // discount anything we've got to-hand
                    ioIndex = available = 0; // note that we have no data in the buffer
                    if (isFixedLength)
                    {
                        if (len > dataRemaining) throw new EndOfStreamException();
                        // else assume we're going to be OK
                        dataRemaining -= len;
                    }
                    ProtoReader.Seek(source, len, ioBuffer);
                    return;
                case WireType.Variant:
                case WireType.SignedVariant:
                    ReadUInt64Variant(); // and drop it
                    return;
                case WireType.StartGroup:
                    int originalFieldNumber = this.fieldNumber;
                    while(ReadFieldHeader() > 0) { SkipField(); }
                    if (wireType == WireType.EndGroup && fieldNumber == originalFieldNumber)
                    { // we expect to exit in a similar state to how we entered
                        return;
                    }
                    throw BorkedIt();
                case WireType.None: // treat as explicit errorr
                case WireType.EndGroup: // treat as explicit error
                default: // treat as implicit error
                    throw BorkedIt();
            }
        }

        /// <summary>
        /// Reads an unsigned 64-bit integer from the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        public ulong ReadUInt64()
        {
            switch (wireType)
            {
                case WireType.Variant:
                    return ReadUInt64Variant();
                case WireType.Fixed32:
                    return ReadUInt32();
                case WireType.Fixed64:
                    if (available < 8) Ensure(8, true);
                    position += 8;
                    available -= 8;

                    return ((ulong)ioBuffer[ioIndex++])
                        | (((ulong)ioBuffer[ioIndex++]) << 8)
                        | (((ulong)ioBuffer[ioIndex++]) << 16)
                        | (((ulong)ioBuffer[ioIndex++]) << 24)
                        | (((ulong)ioBuffer[ioIndex++]) << 32)
                        | (((ulong)ioBuffer[ioIndex++]) << 40)
                        | (((ulong)ioBuffer[ioIndex++]) << 48)
                        | (((ulong)ioBuffer[ioIndex++]) << 56);
                default:
                    throw BorkedIt();
            }
        }
        /// <summary>
        /// Reads a single-precision number from the stream; supported wire-types: Fixed32, Fixed64
        /// </summary>
        public unsafe float ReadSingle()
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                    {
                        int value = ReadInt32();
                        return *(float*)&value;
                    }
                case WireType.Fixed64:
                    {
                        double value = ReadDouble();
                        float f = (float)value;
                        if (Helpers.IsInfinity(f)
                            && !Helpers.IsInfinity(value))
                        {
                            throw new OverflowException();
                        }
                        return f;
                    }
                default:
                    throw BorkedIt();
            }
        }

        /// <summary>
        /// Reads a boolean value from the stream; supported wire-types: Variant, Fixed32, Fixed64
        /// </summary>
        /// <returns></returns>
        public bool ReadBoolean()
        {
            switch (ReadUInt32())
            {
                case 0: return false;
                case 1: return true;
                default: throw BorkedIt();
            }
        }

        /// <summary>
        /// Reads a byte-sequence from the stream, appending them to an existing byte-sequence (which can be null); supported wire-types: String
        /// </summary>
        public static byte[] AppendBytes(byte[] value, ProtoReader reader)
        {
            switch (reader.wireType)
            {
                case WireType.String:
                    int len = (int)reader.ReadUInt32Variant(false);
                    reader.wireType = WireType.None;
                    if (len == 0) return value;
                    int offset;
                    if(value == null || value.Length == 0) {
                        offset = 0;
                        value = new byte[len];
                    } else {
                        offset = value.Length;
                        byte[] tmp = new byte[value.Length + len];
                        Helpers.BlockCopy(value, 0, tmp, 0, value.Length);
                        value = tmp;
                    }
                    // value is now sized with the final length, and (if necessary)
                    // contains the old data up to "offset"
                    reader.position += len; // assume success
                    while (len > reader.available)
                    {
                        if (reader.available > 0)
                        {
                            // copy what we *do* have
                            Helpers.BlockCopy(reader.ioBuffer, reader.ioIndex, value, offset, reader.available);
                            len -= reader.available;
                            offset += reader.available;
                            reader.ioIndex = reader.available = 0; // we've drained the buffer
                        }
                        //  now refill the buffer (without overflowing it)
                        int count = len > reader.ioBuffer.Length ? reader.ioBuffer.Length : len;
                        if (count > 0) reader.Ensure(count, true);
                    }
                    // at this point, we know that len <= available
                    if (len > 0)
                    {   // still need data, but we have enough buffered
                        Helpers.BlockCopy(reader.ioBuffer, reader.ioIndex, value, offset, len);
                        reader.ioIndex += len;
                        reader.available -= len;
                    }
                    return value;
                default:
                    throw reader.BorkedIt();
            }
        }

        static byte[] ReadBytes(Stream stream, int length)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (length < 0) throw new ArgumentOutOfRangeException("length");
            byte[] buffer = new byte[length];
            int offset = 0, read;
            while (length > 0 && (read = stream.Read(buffer, offset, length)) > 0)
            {
                length -= read;
            }
            if (length > 0) throw new EndOfStreamException();
            return buffer;
        }
        private static int ReadByteOrThrow(Stream source)
        {
            int val = source.ReadByte();
            if (val < 0) throw new EndOfStreamException();
            return val;
        }
        /// <summary>
        /// Reads the length-prefix of a message from a stream without buffering additional data, allowing a fixed-length
        /// reader to be created.
        /// </summary>
        public static int ReadLengthPrefix(Stream source, bool expectHeader, PrefixStyle style, out int fieldNumber)
        {
            fieldNumber = 0;
            switch (style)
            {
                case PrefixStyle.None:                    
                    return int.MaxValue;
                case PrefixStyle.Base128:
                    uint val;
                    if (expectHeader)
                    {                        
                        if (ProtoReader.TryReadUInt32Variant(source, out val))
                        {
                            if ((val & 7) != (uint)WireType.String)
                            { // got a header, but it isn't a string
                                throw new InvalidOperationException();
                            }
                            fieldNumber = (int)(val >> 3);
                            if (!ProtoReader.TryReadUInt32Variant(source, out val))
                            { // got a header, but no length
                                throw new EndOfStreamException();
                            }
                            return (int)val;
                        }
                        else
                        { // no header
                            return -1;
                        }
                    }
                    // check for a length
                    return ProtoReader.TryReadUInt32Variant(source, out val)
                        ? (int)val : -1;

                case PrefixStyle.Fixed32:
                    {
                        int b = source.ReadByte();
                        return b < 0 ? -1 :
                            b
                             | (ReadByteOrThrow(source) << 8)
                             | (ReadByteOrThrow(source) << 16)
                             | (ReadByteOrThrow(source) << 24);
                    }
                case PrefixStyle.Fixed32BigEndian:
                    {
                        int b = source.ReadByte();
                        return b < 0 ? -1 :
                            (b << 24)
                            | (ReadByteOrThrow(source) << 16)
                            | (ReadByteOrThrow(source) << 8)
                            | ReadByteOrThrow(source);
                    }
                default:
                    throw new ArgumentOutOfRangeException("style");
            }
        }

        private static bool TryReadUInt32Variant(Stream source, out uint value)
        {
            value = 0;
            int b = source.ReadByte();
            if (b < 0) { return false; }
            value = (uint)b;
            if ((value & 0x80) == 0) return true;
            value &= 0x7F;

            b = source.ReadByte();
            if (b < 0) throw new EndOfStreamException();
            value |= ((uint)b & 0x7F) << 7;
            if ((b & 0x80) == 0) return true;

            b = source.ReadByte();
            if (b < 0) throw new EndOfStreamException();
            value |= ((uint)b & 0x7F) << 14;
            if ((b & 0x80) == 0) return true;

            b = source.ReadByte();
            if (b < 0) throw new EndOfStreamException();
            value |= ((uint)b & 0x7F) << 21;
            if ((b & 0x80) == 0) return true;

            b = source.ReadByte();
            if (b < 0) throw new EndOfStreamException();
            value |= (uint)b << 28; // can only use 4 bits from this chunk
            if ((b & 0xF0) == 0) return true;

            throw new OverflowException();
        }

        internal static void Seek(Stream source, int count, byte[] buffer)
        {
            if (source.CanSeek)
            {
                source.Seek(count, SeekOrigin.Current);
                count = 0;
            }
            else if (buffer != null)
            {
                int bytesRead;
                while(count > buffer.Length && (bytesRead = source.Read(buffer, 0, buffer.Length)) > 0) {
                    count -= bytesRead;
                }
                while (count > 0 && (bytesRead = source.Read(buffer, 0, count)) > 0) {
                    count -= bytesRead;
                }
            }
            else // borrow a buffer
            {
                buffer = BufferPool.GetBuffer();
                try
                {
                    int bytesRead;
                    while (count > buffer.Length && (bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        count -= bytesRead;
                    }
                    while (count > 0 && (bytesRead = source.Read(buffer, 0, count)) > 0)
                    {
                        count -= bytesRead;
                    }   
                }
                finally {
                    BufferPool.ReleaseBufferToPool(ref buffer);
                }
            }
            if (count > 0) throw new EndOfStreamException();
        }
    }
}
