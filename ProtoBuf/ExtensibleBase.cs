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
    [DataContract]
    public abstract class ExtensibleBase : IExtensible
    {
        private byte[] extendedData;

        int IExtensible.GetLength()
        {
            return extendedData == null ? 0 : (int)extendedData.Length;
        }
        Stream IExtensible.BeginAppendData()
        {
            return new MemoryStream();
        }

        void IExtensible.EndAppendData(Stream stream, bool success)
        {
            // we now own the stream again; be sure to dispose it...
            using (stream)
            {
                if (success && (stream != null))
                {
                    // copy the data into our buffer (resize as necessary)
                    MemoryStream ms = (MemoryStream)stream;
                    int oldSize = extendedData == null ? 0 : extendedData.Length;
                    Array.Resize<byte>(ref extendedData, oldSize + (int)ms.Length);
                    byte[] buffer = ms.GetBuffer();
                    Buffer.BlockCopy(buffer, 0, extendedData, oldSize, (int)ms.Length);
                }
            }
        }

        Stream IExtensible.ReadData()
        {
            return extendedData == null
                ? new MemoryStream() : new MemoryStream(extendedData);
        }
    }
}
