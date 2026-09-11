using System;
using System.Buffers;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// Marshals one message type, for one method.
    /// </summary>
    /// <remarks>
    /// The per-method counterpart of <see cref="ConnectCodec"/>, which is per channel. Three different
    /// things want this seam, which is the reason it exists rather than a pre-resolved protobuf-net
    /// serializer:
    /// <list type="bullet">
    /// <item><b>hoisting</b> - a protobuf-net serializer resolved once at build time instead of per
    /// message, which <see cref="ProtoMessageCodec{T}"/> is;</item>
    /// <item><b>contract-first gRPC</b> - where the payload type is a Google.Protobuf message and the
    /// marshalling belongs to a generated <c>Grpc.Core.Marshaller&lt;T&gt;</c>, not to any
    /// <see cref="TypeModel"/>;</item>
    /// <item><b>JSON</b> - which is per message and, for contract-first, already written.</item>
    /// </list>
    /// <para>
    /// A method that carries none falls back to the channel's codec, which is the ordinary code-first
    /// path and stays the default.
    /// </para>
    /// </remarks>
    public interface IConnectMessageCodec<T>
    {
        /// <summary>
        /// The encoded length, where it can be known without encoding twice; <c>null</c> when it cannot.
        /// </summary>
        /// <remarks>
        /// Used to state <c>Content-Length</c> on a unary body and to size an envelope header. A codec
        /// that cannot measure forces the caller to buffer, which is why protobuf-net's ability to
        /// measure is worth keeping visible here.
        /// </remarks>
        long? Measure(T value);

        /// <summary>Encodes a message, with no framing.</summary>
        void Write(IBufferWriter<byte> destination, T value);

        /// <summary>Decodes a whole message.</summary>
        T Read(in ReadOnlySequence<byte> source);
    }

    /// <summary>
    /// An <see cref="IConnectMessageCodec{T}"/> over a protobuf-net serializer resolved once.
    /// </summary>
    /// <remarks>
    /// This is the hoist: <c>TypeModel.Deserialize&lt;T&gt;</c> would otherwise resolve the serializer
    /// on every message - a static field read, a virtual call and a second static field read. Small, but
    /// per message, and "the model is closed at compile time" ought to mean the binding is made then too.
    /// </remarks>
    public sealed class ProtoMessageCodec<T> : IConnectMessageCodec<T>
    {
        private readonly TypeModel _model;
        private readonly ISerializer<T> _serializer;

        /// <summary>Creates a codec over a model and a serializer it produced.</summary>
        public ProtoMessageCodec(TypeModel model, ISerializer<T> serializer)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <inheritdoc/>
        public long? Measure(T value)
        {
            using var measured = ((IMeasuredProtoOutput<IBufferWriter<byte>>)_model).Measure(value);
            return measured.Length;
        }

        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2091",
            Justification = "SerializeRoot's annotation exists for its 'serializer ?? GetSerializer<T>(Model)' "
                + "fallback, which a non-null serializer short-circuits; annotating instead would push the "
                + "demand across the whole surface.")]
        public void Write(IBufferWriter<byte> destination, T value)
        {
            using var state = ProtoWriter.State.Create(destination, _model);
            state.SerializeRoot(value, _serializer);
        }

        /// <inheritdoc/>
        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("Trimming", "IL2091",
            Justification = "As for Write.")]
        public T Read(in ReadOnlySequence<byte> source)
        {
            using var state = ProtoReader.State.Create(source, _model);
            return state.DeserializeRoot<T>(default!, _serializer);
        }
    }
}
