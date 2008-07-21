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
        /// <summary>
        /// The serializer (during deserialization/merge) has encountered unexpected
        /// fields that it cannot process; the implementing
        /// class should store this data verbatim. The serializer retains ownership
        /// of the stream: it is not necessary (but not harmful) to close it.
        /// The implementing class should be prepared to handle multiple different
        /// Append messages.
        /// </summary>
        /// <param name="stream">The additional data to store.</param>
        void Append(Stream stream);
        /// <summary>
        /// The serializer is requesting the additional data that has been previously
        /// stored; this method is only called once per serialized instance. The
        /// serializer assumes ownership of the stream, and is responsible for closing it.
        /// </summary>
        /// <returns></returns>
        Stream Read();

        /// <summary>
        /// Requests the length of the raw binary stream; this is used
        /// when serializing sub-entities to indicate the expected size.
        /// </summary>
        /// <returns>The length of the binary stream representing unexpected data.</returns>
        int GetLength();

    }
}
