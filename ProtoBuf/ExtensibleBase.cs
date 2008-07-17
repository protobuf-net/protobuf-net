using System.Runtime.Serialization;
using System.IO;
using System;

namespace ProtoBuf
{
    /// <summary>
    /// Simple base class for supporting unexpected fields; the
    /// additional fields are stored in-memory in a buffer.
    /// </summary>
    [DataContract]
    public abstract class ExtensibleBase : IExtensible
    {
        private byte[] extendedData;
        
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
