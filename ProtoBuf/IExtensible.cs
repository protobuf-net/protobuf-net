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
        /// Requests a stream from the implementing class, to
        /// which unexpected fields can be stored as raw binary.
        /// </summary>
        /// <returns>The stream that the serializer should write into.</returns>
        Stream BeginAppendData();
        /// <summary>
        /// Indicates to the implementing class that the serializer
        /// has finished appending unexpected fields to the stream.
        /// The stream is still owned by the caller.
        /// </summary>
        /// <param name="stream">The stream instance that the
        /// implementing class supplied; the serializer leaves
        /// ownership of the stream with the implementing class,
        /// so it will not be automatically closed.</param>
        /// <param name="success">True if the serializer has completed
        /// successfully and the data should be committed; or false otherwise.</param>
        void EndAppendData(Stream stream, bool success);
        /// <summary>
        /// Requests the raw binary stream from the implementing class;
        /// the serializer assumes responsibility for the stream, and it
        /// will be closed/disposed by the serializer after use.
        /// </summary>
        /// <returns></returns>
        Stream ReadData();
        /// <summary>
        /// Requests the length of the raw binary stream; this is used
        /// when serializing sub-entities to indicate the expected size.
        /// </summary>
        /// <returns>The length of the binary stream representing unexpected data.</returns>
        int GetLength();

    }
}
