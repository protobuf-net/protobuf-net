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
        public static int GetLength(byte[] buffer)
        {
            return buffer == null ? 0 : buffer.Length;
        }
        public static void Append(ref byte[] buffer, Stream stream)
        {

            int offset = buffer == null ? 0 : buffer.Length,
                remaining = (int) stream.Length, bytes;
            Array.Resize<byte>(ref buffer, offset + remaining);
            while(remaining > 0 && (bytes = stream.Read(buffer, offset, remaining)) > 0)
            {}
        }
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
