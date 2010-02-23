
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using ProtoBuf.Meta;

namespace ProtoBuf
{
    public sealed class ProtoReader : IDisposable
    {
        Stream source;
        byte[] ioBuffer;
        TypeModel model;

        private int fieldNumber;
        WireType wireType = WireType.Error;
        internal int FieldNumber { get { return fieldNumber; } }

        internal ProtoReader(Stream source, TypeModel model)
        {
            if (source == null) throw new ArgumentNullException("dest");
            if (!source.CanRead) throw new ArgumentException("Cannot read from stream", "dest");
            this.source = source;
            this.ioBuffer = BufferPool.GetBuffer();
            this.model = model;
        }
        public void Dispose()
        {
            // importantly, this does **not** own the stream, and does not dispose it
            source = null;
            model = null;
            BufferPool.ReleaseBufferToPool(ref ioBuffer);
        }
        private int TryReadUInt32VariantWithoutMoving(out uint value)
        {
            if(available < 5) Ensure(5, false);
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

            throw new OverflowException();
        }
        public bool TryPreviewUInt32Variant(out uint value)
        {
            return TryReadUInt32VariantWithoutMoving(out value) > 0;
        }
        private uint ReadUInt32Variant()
        {
            uint value;
            int read = TryReadUInt32VariantWithoutMoving(out value);
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
            int read = TryReadUInt32VariantWithoutMoving(out value);
            if (read > 0)
            {
                ioIndex += read;
                available -= read;
                position += read;
                return true;
            }
            return false;
        }
        public uint ReadUInt32()
        {
            switch (wireType)
            {
                case WireType.Variant:
                    return ReadUInt32Variant();
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
        public int Position { get { return position; } }
        internal void Ensure(int count, bool strict)
        {
            Debug.Assert(count <= available, "Asking for data without checking first");
            Debug.Assert(count <= ioBuffer.Length, "Asking for too much data");

            if (ioIndex + count >= ioBuffer.Length)
            {
                // need to shift the buffer data to the left to make space
                Buffer.BlockCopy(ioBuffer, ioIndex, ioBuffer, 0, available);
                ioIndex = 0;
            }
            count -= available;
            int writePos = ioIndex + available, bytesRead;
            while (count > 0 && (bytesRead = source.Read(ioBuffer, writePos, count)) > 0)
            {
                available += bytesRead;
                count -= bytesRead;
                writePos += bytesRead;
            }
            if (strict && count > 0)
            {
                throw new EndOfStreamException();
            }

        }

        public int ReadInt32()
        {
            switch (wireType)
            {
                case WireType.Variant:
                    return (int)ReadUInt32Variant();
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
                    return Zag(ReadUInt32Variant());
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
        public long ReadInt64()
        {
            throw new NotImplementedException();
        }

        static readonly UTF8Encoding encoding = new UTF8Encoding(false);
        public string ReadString()
        {
            int bytes = (int)ReadUInt32Variant();
            if (bytes == 0) return "";
            if (available < bytes) Ensure(bytes, true);
            string s = encoding.GetString(ioBuffer, ioIndex, bytes);
            available -= bytes;
            position += bytes;
            return s;
        }
        private Exception BorkedIt()
        {
            throw new ProtoException();
        }
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

        public object ReadObject(object value, int key)
        {
            if (model == null)
            {
                throw new InvalidOperationException("Cannot deserialize sub-objects unless a model is provided");
            }
            int token = StartSubItem(value);
            value = model.Deserialize(key, value, this);
            EndSubItem(token);
            return value;
        }

        private void EndSubItem(int token)
        {
            throw new NotImplementedException();
        }

        private int StartSubItem(object value)
        {
            throw new NotImplementedException();
        }

        public int ReadFieldHeader()
        {
            uint tag;
            if (TryReadUInt32Variant(out tag))
            {
                wireType = (WireType)(tag & 7);
                fieldNumber = (int)(tag >> 3);
            } else {
                wireType = WireType.Error;
                fieldNumber = 0;
            }
            return fieldNumber;
        }

        public void SetSignedVariant()
        {
            if (wireType == WireType.Variant) { wireType = WireType.SignedVariant; }
        }

        public void SkipField()
        {
            throw new NotImplementedException();
        }

        public ulong ReadUInt64()
        {
            throw new NotImplementedException();
        }

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
                        if (float.IsInfinity(f)
                            && !double.IsInfinity(value))
                        {
                            throw new OverflowException();
                        }
                        return f;
                    }
                default:
                    throw BorkedIt();
            }
        }

        public bool ReadBoolean()
        {
            switch (ReadUInt32())
            {
                case 0: return false;
                case 1: return true;
                default: throw BorkedIt();
            }
        }
    }
}
