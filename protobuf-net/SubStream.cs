using System;
using System.IO;

namespace ProtoBuf
{
    /// <summary>
    /// Describes a Stream that wraps an underlying stream but
    /// which limits the length. This is used for processing
    /// length-prefied messages (string wire-type) so that no
    /// complex code is required to manage the end of each
    /// object.
    /// </summary>
    internal sealed class SubStream : Stream
    {
        private Stream parent;
        private readonly long length;
        private bool closesParent;
        private long position;

        public SubStream(Stream parent, long length, bool closesParent)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            if (length < 0) throw new ArgumentOutOfRangeException("length");
            if (!parent.CanRead) throw new ArgumentException("The parent stream must be readable", "parent");
            this.parent = parent;
            this.length = length;
            this.closesParent = closesParent;
        }

        private void CheckDisposed()
        {
            if (parent == null) throw new ObjectDisposedException(GetType().Name);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && parent != null)
            {
                if (closesParent)
                { // close the parent completely
                    parent.Close();
                }
                else
                { // move the parent past this sub-data
                    long remaining = length - position, bytes;
                    if (remaining > 0)
                    {
                        if (CanSeek)
                        {   // seek the stream
                            parent.Seek(remaining, SeekOrigin.Current);
                        }
                        else
                        {   // burn up the stream
                            const int DEFAULT_SIZE = 4096;
                            byte[] buffer = new byte[remaining < DEFAULT_SIZE ? remaining : DEFAULT_SIZE];
                            while (remaining > 0 && (bytes = parent.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                remaining -= bytes;
                            }
                        }
                    }
                }

                parent = null;
            }

            base.Dispose(disposing);
        }

        public override bool CanRead
        {
            get { return parent != null; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanSeek
        {
            get 
            {
                return (parent != null) && parent.CanSeek;
            }
        }

        public override void Flush()
        {
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Length
        {
            get { return length; }
        }

        public override long Position
        {
            get
            {
                return position;
            }

            set
            {
                if (value < 0 || value >= length) throw new ArgumentOutOfRangeException("value", "Cannot seek outide of the Stream's bounds");
                parent.Position += (value - position);
                position = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
                default:
                    throw new ArgumentException("Unknown seek-origin", "origin");
            }

            return Position;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int ReadByte()
        {
            CheckDisposed();
            if (position >= length)
            {
                return -1;
            }

            int result = parent.ReadByte();
            if (result >= 0) position++;
            return result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            long remaining = length - position;
            if (count > remaining) count = (int)remaining;
            count = parent.Read(buffer, offset, count);
            if (count > 0)
            {
                position += count;
            }

            return count;
        }
    }
}
