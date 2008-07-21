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
        /// <summary>
        /// Used to implement IExtensible.GetLength() for simple byte[]-based implementations.
        /// </summary>
        /// <param name="buffer">The current buffer instance (can be null).</param>
        /// <returns>The length of the buffer, or 0 if null.</returns>
        public static int GetLength(byte[] buffer)
        {
            return buffer == null ? 0 : buffer.Length;
        }
        /// <summary>
        /// Used to implement IExtensible.Append() for simple byte[]-based implementations;
        /// creates/resizes the buffer accordingly (copying any existing data), and places
        /// the new data at the end of the buffer.
        /// </summary>
        /// <param name="buffer">The current buffer instance (can be null).</param>
        /// <param name="stream">The additional data passed by the serializer.</param>
        public static void Append(ref byte[] buffer, Stream stream)
        {

            int offset = buffer == null ? 0 : buffer.Length,
                remaining = (int) stream.Length, bytes;
            Array.Resize<byte>(ref buffer, offset + remaining);
            while(remaining > 0 && (bytes = stream.Read(buffer, offset, remaining)) > 0)
            {}
        }
        /// <summary>
        /// User to implement ISerializable.Read() for simple byte[]-based implementations;
        /// returns a stream representation of the current buffer.
        /// </summary>
        /// <param name="buffer">The current buffer instance (can be null).</param>
        /// <returns>A stream representation of the buffer.</returns>
        public static Stream Read(byte[] buffer)
        {
            return buffer == null ? Stream.Null : new MemoryStream(buffer);
        }
        private byte[] extendedData;

        int IExtensible.GetLength()
        {
            return Extensible.GetLength(extendedData);
        }
        void IExtensible.Append(Stream stream)
        {
            Extensible.Append(ref extendedData, stream);
        }
        Stream IExtensible.Read()
        {
            return Extensible.Read(extendedData);
        }
    }
}
