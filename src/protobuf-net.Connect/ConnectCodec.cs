using System;
using System.Buffers;
using ProtoBuf.Meta;

namespace ProtoBuf.Connect;

/// <summary>
/// Turns messages into bytes and back. A codec is <em>payload only</em>: it knows nothing about framing,
/// which is chosen separately from the RPC's shape.
/// </summary>
/// <remarks>
/// The split is the protocol's, not ours. A content-type names both independently - <c>application/proto</c>
/// is this codec with no framing, <c>application/connect+proto</c> is the same codec with enveloped
/// framing - so keeping them apart here costs nothing and is what lets the streaming shapes be added
/// without disturbing unary.
/// <para>
/// The buffer-oriented forms are the primitives rather than <see cref="System.IO.Stream"/>, because
/// they are what both ends actually have: a <c>PipeWriter</c> is an <see cref="IBufferWriter{T}"/> and a
/// <c>PipeReader</c> yields a <see cref="ReadOnlySequence{T}"/>, and ASP.NET Core forbids synchronous
/// stream reads - so a stream-shaped codec would force a buffering step on the server that nothing needs.
/// </para>
/// </remarks>
public abstract class ConnectCodec
{
    /// <summary>
    /// The codec's name as it appears in a content-type: <c>proto</c> or <c>json</c>.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>Serializes a message, with no framing.</summary>
    public abstract void Write<T>(IBufferWriter<byte> destination, T value);

    /// <summary>
    /// Measures a message, where the codec can do so without serializing twice; <c>null</c> when it
    /// cannot. Used to set <c>Content-Length</c>, which a unary message can always state and a
    /// streaming one never can.
    /// </summary>
    public abstract long? Measure<T>(T value);

    /// <summary>Deserializes a whole message.</summary>
    public abstract T Read<T>(in ReadOnlySequence<byte> source);

    /// <summary>
    /// The content-type for an RPC of the given shape: <c>application/{name}</c> for unary, and
    /// <c>application/connect+{name}</c> for everything else.
    /// </summary>
    public string ContentTypeFor(ConnectMethodType type)
        => type == ConnectMethodType.Unary ? "application/" + Name : "application/connect+" + Name;
}

/// <summary>
/// The binary protobuf codec, backed by a protobuf-net <see cref="TypeModel"/>.
/// </summary>
/// <remarks>
/// The model is expected to be a build-time generated one (<c>[ProtoModel]</c>); nothing here requires
/// that, but nothing here provides a reflective fallback either - if the model has no serializer for a
/// type, <see cref="TypeModel"/>'s own "no serializer" throw is the backstop.
/// </remarks>
public sealed class ProtoConnectCodec : ConnectCodec
{
    private readonly TypeModel _model;

    /// <summary>Creates a codec over the given model.</summary>
    public ProtoConnectCodec(TypeModel model)
        => _model = model ?? throw new ArgumentNullException(nameof(model));

    /// <inheritdoc/>
    public override string Name => "proto";

    /// <inheritdoc/>
    public override long? Measure<T>(T value)
    {
        using var measured = ((IMeasuredProtoOutput<IBufferWriter<byte>>)_model).Measure(value);
        return measured.Length;
    }

    /// <inheritdoc/>
    public override void Write<T>(IBufferWriter<byte> destination, T value)
        => ((IProtoOutput<IBufferWriter<byte>>)_model).Serialize(destination, value);

    /// <inheritdoc/>
    public override T Read<T>(in ReadOnlySequence<byte> source)
        => ((IProtoInput<ReadOnlySequence<byte>>)_model).Deserialize<T>(source);
}
