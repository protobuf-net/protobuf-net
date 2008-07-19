using System.IO;

namespace ProtoBuf
{
    /// <summary>
    /// Provides addition capability for supporting unexpected fields during
    /// protocol-buffer serialization/deserialization. This allows for loss-less
    /// round-trip/merge, even when the data is not fully understood.
    /// </summary>
    public interface IExtensible
    {
        void Append(Stream stream);
        Stream Read();

        /// <summary>
        /// Requests the length of the raw binary stream; this is used
        /// when serializing sub-entities to indicate the expected size.
        /// </summary>
        /// <returns>The length of the binary stream representing unexpected data.</returns>
        int GetLength();

    }
}
