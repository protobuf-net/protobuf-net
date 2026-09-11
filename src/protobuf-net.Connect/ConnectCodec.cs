using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using ProtoBuf.Connect.Internal;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

namespace ProtoBuf.Connect
{
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
        /// <param name="destination">Where to write the encoded message.</param>
        /// <param name="value">The message.</param>
        /// <param name="over">
        /// A per-method codec to use instead of this one, where the method carries one. The branch lives
        /// here so that every call site is spared it.
        /// </param>
        public void Write<T>(IBufferWriter<byte> destination, T value, IConnectMessageCodec<T>? over = null)
        {
            if (over is not null) over.Write(destination, value);
            else WriteCore(destination, value);
        }

        /// <summary>Serializes a message using this codec's own marshalling.</summary>
        protected abstract void WriteCore<T>(IBufferWriter<byte> destination, T value);

        /// <summary>
        /// Measures a message, where the codec can do so without serializing twice; <c>null</c> when it
        /// cannot. Used to set <c>Content-Length</c>, which a unary message can always state and a
        /// streaming one never can.
        /// </summary>
        public long? Measure<T>(T value, IConnectMessageCodec<T>? over = null)
            => over is not null ? over.Measure(value) : MeasureCore(value);

        /// <summary>Measures using this codec's own marshalling.</summary>
        protected abstract long? MeasureCore<T>(T value);

        /// <summary>Deserializes a whole message.</summary>
        /// <param name="source">The whole encoded message, with no framing around it.</param>
        /// <param name="over">As for <see cref="Write"/>: a per-method codec, where the method has one.</param>
        public T Read<T>(in ReadOnlySequence<byte> source, IConnectMessageCodec<T>? over = null)
            => over is not null ? over.Read(source) : ReadCore<T>(source);

        /// <summary>Deserializes using this codec's own marshalling.</summary>
        protected abstract T ReadCore<T>(in ReadOnlySequence<byte> source);

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
        protected override long? MeasureCore<T>(T value)
        {
            // the one remaining per-message resolution: TypeModel.Measure<T> takes no over, so there
            // is nothing to hand it. It buys Content-Length, which is worth more than it costs.
            using var measured = ((IMeasuredProtoOutput<IBufferWriter<byte>>)_model).Measure(value);
            return measured.Length;
        }

        /// <inheritdoc/>
        [UnconditionalSuppressMessage("Trimming", "IL2091",
            Justification = "SerializeRoot's annotation exists for its 'serializer ?? TypeModel.GetSerializer<T>(Model)' "
                + "fallback; a non-null serializer short-circuits it, so nothing on this path reflects over T. "
                + "Suppressed here rather than annotated, because annotating would push the demand onto every "
                + "public generic on this assembly and thence onto every consumer's payload types - which is the "
                + "mistake AGENTS.md records against IConnectMessageCodec<T>.")]
        protected override void WriteCore<T>(IBufferWriter<byte> destination, T value)
            => ((IProtoOutput<IBufferWriter<byte>>)_model).Serialize(destination, value);

        /// <inheritdoc/>
        [UnconditionalSuppressMessage("Trimming", "IL2091",
            Justification = "As for Write: DeserializeRoot's annotation covers a fallback a non-null serializer "
                + "short-circuits, and annotating instead would propagate the demand across the whole surface.")]
        protected override T ReadCore<T>(in ReadOnlySequence<byte> source)
            => ((IProtoInput<ReadOnlySequence<byte>>)_model).Deserialize<T>(source);
    }

    /// <summary>
    /// A <c>proto</c> codec that carries no marshalling of its own, and serves only methods that supply
    /// their own.
    /// </summary>
    /// <remarks>
    /// This is what a <strong>contract-first</strong> application registers. Such an application has no
    /// protobuf-net <see cref="TypeModel"/> at all - its messages are Google.Protobuf types, which no
    /// <c>TypeModel</c> can serialize - and every method it serves already carries a
    /// <see cref="MarshallerMessageCodec{T}"/> taken from the descriptor <c>protoc</c> generated. So the
    /// channel-level codec has nothing left to do but name itself, which is still required: the name is
    /// what selects a codec from a content-type, on both sides.
    /// <para>
    /// Reaching the core methods means a method arrived without a per-method codec, which for this codec
    /// is a wiring mistake rather than a payload it cannot handle - hence a throw that says so, rather
    /// than a silent empty message.
    /// </para>
    /// </remarks>
    public sealed class MarshallerConnectCodec : ConnectCodec
    {
        /// <summary>A shared instance; the type holds no state.</summary>
        public static MarshallerConnectCodec Instance { get; } = new();

        /// <inheritdoc/>
        public override string Name => "proto";

        private static Exception NoCodec<T>()
            => new NotSupportedException(
                $"No codec was supplied for '{typeof(T).Name}'. {nameof(MarshallerConnectCodec)} marshals nothing itself; "
                + "it serves methods whose own codec comes from a Grpc.Core marshaller. A method reaching it without one "
                + "was not built from a generated descriptor.");

        /// <inheritdoc/>
        protected override long? MeasureCore<T>(T value) => throw NoCodec<T>();

        /// <inheritdoc/>
        protected override void WriteCore<T>(IBufferWriter<byte> destination, T value) => throw NoCodec<T>();

        /// <inheritdoc/>
        protected override T ReadCore<T>(in ReadOnlySequence<byte> source) => throw NoCodec<T>();
    }
}
