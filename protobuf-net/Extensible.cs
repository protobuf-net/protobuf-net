using System;
using System.IO;
using System.Runtime.Serialization;

namespace ProtoBuf
{
    /// <summary>
    /// Simple base class for supporting unexpected fields allowing
    /// for loss-less round-tips/merge, even if the data is not understod.
    /// The additional fields are stored in-memory in a buffer.
    /// </summary>
    /// <remarks>As an example of an alternative implementation, you might
    /// choose to use the file system (temporary files) as the back-end, tracking
    /// only the paths [such an object would ideally be IDisposable and use
    /// a finalizer to ensure that the files are removed].</remarks>
    [ProtoContract]
    public abstract class Extensible : IExtensible
    {
        private byte[] extendedData;

        int IExtensible.GetLength()
        {
            return Extensible.GetLength(extendedData);
        }
        /// <summary>
        /// Used to implement IExtensible.GetLength() for simple byte[]-based implementations;
        /// returns the length of the current buffer.
        /// </summary>
        /// <param name="buffer">The current buffer instance (can be null).</param>
        /// <returns>The length of the buffer, or 0 if null.</returns>
        public static int GetLength(byte[] buffer)
        {
            return buffer == null ? 0 : buffer.Length;
        }


        Stream IExtensible.BeginAppend()
        {
            return Extensible.BeginAppend();
        }
        /// <summary>
        /// Used to implement IExtensible.EndAppend() for simple byte[]-based implementations;
        /// obtains a new Stream suitable for storing data as a simple buffer.
        /// </summary>
        /// <returns>The stream for storing data.</returns>
        public static Stream BeginAppend()
        {
            return new MemoryStream();
        }


        void IExtensible.EndAppend(Stream stream, bool commit)
        {
            extendedData = Extensible.EndAppend(extendedData, stream, commit);
        }
        /// <summary>
        /// Used to implement IExtensible.EndAppend() for simple byte[]-based implementations;
        /// creates/resizes the buffer accordingly (copying any existing data), and places
        /// the new data at the end of the buffer.
        /// </summary>
        /// <param name="buffer">The current buffer instance (can be null).</param>
        /// <param name="stream">The stream previously obtained from BeginAppend.</param>
        /// <param name="commit">Should the data be stored? Or just close the stream?</param>
        /// <returns>The updated buffer.</returns>
        public static byte[] EndAppend(byte[] buffer, Stream stream, bool commit)
        {
            
            using (stream)
            {
                int len;
                if (commit && (len = (int)stream.Length)>0)
                {
                    MemoryStream ms = (MemoryStream)stream;
                    
                    int offset = buffer == null ? 0 : buffer.Length;
                    Array.Resize<byte>(ref buffer, offset + len);
                    byte[] raw = ms.GetBuffer();
                    Buffer.BlockCopy(raw, 0, buffer, offset, len);
                }
                return buffer;
            }
        }

        Stream IExtensible.BeginQuery()
        {
            return Extensible.BeginQuery(extendedData);
        }
        /// <summary>
        /// Used to implement ISerializable.BeginQuery() for simple byte[]-based implementations;
        /// returns a stream representation of the current buffer.
        /// </summary>
        /// <param name="buffer">The current buffer instance (can be null).</param>
        /// <returns>A stream representation of the buffer.</returns>
        public static Stream BeginQuery(byte[] buffer)
        {
            return buffer == null ? Stream.Null : new MemoryStream(buffer);
        }
        /// <summary>
        /// Used to implement ISerializable.BeginQuery() for simple byte[]-based implementations;
        /// closes the stream.</summary>
        /// <param name="stream">The stream previously obtained from BeginQuery.</param>
        void IExtensible.EndQuery(Stream stream)
        {
            Extensible.EndQuery(stream);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        public static void EndQuery(Stream stream)
        {
            using (stream) { }
        }

    }
}
