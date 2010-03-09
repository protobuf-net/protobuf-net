using System;
using System.IO;

namespace ProtoBuf
{
    /// <summary>
    /// Provides a simple buffer-based implementation of an <see cref="IExtension">extension</see> object.
    /// </summary>
    public sealed class BufferExtension : IExtension
    {
        private byte[] buffer;

        int IExtension.GetLength()
        {
            return buffer == null ? 0 : buffer.Length;
        }

        Stream IExtension.BeginAppend()
        {
            return new MemoryStream();
        }

        void IExtension.EndAppend(Stream stream, bool commit)
        {
            using (stream)
            {
                int len;
                if (commit && (len = (int)stream.Length) > 0)
                {
                    MemoryStream ms = (MemoryStream)stream;

                    if (buffer == null)
                    {   // allocate new buffer
                        buffer = ms.ToArray();
                    }
                    else
                    {   // resize and copy the data
                        // note: Array.Resize not available on CF
                        int offset = buffer.Length;
                        byte[] tmp = new byte[offset + len];
                        Buffer.BlockCopy(buffer, 0, tmp, 0, offset);
                        Buffer.BlockCopy(ms.GetBuffer(), 0, tmp, offset, len);
                        buffer = tmp;
                    }
                }
            }
        }

        Stream IExtension.BeginQuery()
        {
            return buffer == null ? Stream.Null : new MemoryStream(buffer);
        }

        void IExtension.EndQuery(Stream stream)
        {
            using (stream) { } // just clean up
        }
    }
}
