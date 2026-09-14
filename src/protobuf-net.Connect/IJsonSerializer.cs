using System.Text.Json;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// Writes and reads one contract type in the <em>canonical protobuf JSON mapping</em>.
    /// </summary>
    /// <remarks>
    /// Deliberately not <c>System.Text.Json</c>'s serializer, and not a <c>JsonConverter</c>: the
    /// canonical mapping is specified against the <em>proto schema</em> rather than against the C#
    /// shape, so it is not something a POCO serializer can be configured into producing. See
    /// <c>notes/connect/findings.md</c> §46.
    /// <para>
    /// Implemented by generated code, over <see cref="Utf8JsonWriter"/> and
    /// <see cref="Utf8JsonReader"/> - the low-level primitives, which are AOT-safe.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The contract type.</typeparam>
    public interface IJsonSerializer<T>
    {
        /// <summary>Writes <paramref name="value"/> as a JSON object.</summary>
        void Write(Utf8JsonWriter writer, T value);

        /// <summary>
        /// Reads one JSON object, merging into <paramref name="value"/> where it is not null.
        /// </summary>
        /// <remarks>
        /// The reader is positioned on the value's first token. Merge semantics match the binary
        /// path's, so a caller supplying an existing instance gets the same behaviour either way.
        /// </remarks>
        T Read(ref Utf8JsonReader reader, T value);
    }

    /// <summary>
    /// Implemented by a generated <c>[ProtoModel]</c> that also carries the JSON mapping.
    /// </summary>
    /// <remarks>
    /// Separate from <c>TypeModel</c> because JSON is not part of protobuf-net's core surface and
    /// should not put <c>System.Text.Json</c> on protobuf-net.Core's dependency graph. The generator
    /// emits this half only when the consumer references this assembly, so a model that has never
    /// heard of Connect pays nothing.
    /// </remarks>
    public interface IJsonModel
    {
        /// <summary>
        /// The JSON serializer for <typeparamref name="T"/>, or <c>null</c> if this model has none -
        /// which is the normal answer for a contract whose shape has no canonical JSON mapping.
        /// </summary>
        IJsonSerializer<T>? GetJsonSerializer<T>();
    }
}
