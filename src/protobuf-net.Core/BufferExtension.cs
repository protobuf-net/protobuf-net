using System;
using System.IO;

namespace ProtoBuf
{
    /// <summary>
    /// Provides a simple buffer-based implementation of an <see cref="IExtension">extension</see> object.
    /// </summary>
    public sealed class BufferExtension : IExtension, IExtensionResettable
    {
        private ArraySegment<byte> _buffer;

        internal Type Type { get; private set; }
        internal BufferExtension Tail { get; private set; }

        internal void SetTail(Type type, BufferExtension tail)
        {
            Type = type;
            Tail = tail;
        }

        void IExtensionResettable.Reset()
        {
            _buffer = default;
        }

        int IExtension.GetLength()
        {
            return _buffer.Count;
        }

        Stream IExtension.BeginAppend()
        {
            return new MemoryStream();
        }

        void IExtension.EndAppend(Stream stream, bool commit)
        {
            using (stream)
            {
                if (commit && stream is MemoryStream ms && ms.TryGetBuffer(out var segment) && segment.Count != 0)
                {
                    if (_buffer.Count == 0)
                    {   // just assign
                        _buffer = segment;
                    }
                    else
                    {
                        int oldEnd = _buffer.Offset + _buffer.Count;
                        int space = _buffer.Array.Length - oldEnd;
                        if (space >= segment.Count)
                        {
                            // we can fit it into the current buffer
                            Buffer.BlockCopy(segment.Array, segment.Offset, _buffer.Array, oldEnd, segment.Count);
                            _buffer = new ArraySegment<byte>(_buffer.Array, _buffer.Offset, oldEnd + segment.Count);
                        }
                        else
                        {
                            byte[] tmp = new byte[_buffer.Count + segment.Count];
                            Buffer.BlockCopy(_buffer.Array, _buffer.Offset, tmp, 0, _buffer.Count);
                            Buffer.BlockCopy(segment.Array, segment.Offset, tmp, _buffer.Count, segment.Count);
                            _buffer = new ArraySegment<byte>(tmp, 0, _buffer.Count + segment.Count);
                        }
                    }
                }
            }
        }

        Stream IExtension.BeginQuery()
        {
            return _buffer.Count == 0 ? Stream.Null : new MemoryStream(_buffer.Array, _buffer.Offset, _buffer.Count, false, true);
        }

        void IExtension.EndQuery(Stream stream)
        {
            using (stream) { } // just clean up
        }
    }
}