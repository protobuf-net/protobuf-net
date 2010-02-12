
using System;
using System.IO;
using System.Diagnostics;
using ProtoBuf.Meta;
namespace ProtoBuf
{
    public sealed class ProtoReader : IDisposable
    {
        Stream source;
        byte[] ioBuffer;
        TypeModel model;

        internal ProtoReader(Stream source, TypeModel model)
        {
            if (source == null) throw new ArgumentNullException("dest");
            if (!source.CanRead) throw new ArgumentException("Cannot read from stream", "dest");
            if (model == null) throw new ArgumentNullException("model");
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
        private int TryReadUInt32WithoutMoving(out uint value)
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
        public bool TryPreviewUInt32(out uint value)
        {
            return TryReadUInt32WithoutMoving(out value) > 0;
        }
        public bool TryReadUInt32(out uint value)
        {
            int read = TryReadUInt32WithoutMoving(out value);
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
            uint value;
            int read = TryReadUInt32WithoutMoving(out value);
            if (read <= 0) throw new EndOfStreamException();
            ioIndex += read;
            available -= read;
            position += read;
            return value;
        }
        int ioIndex, position, available; // maxPosition
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
    }
}
