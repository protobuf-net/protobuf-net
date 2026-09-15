using System;
using System.Buffers;
using System.Text.Json;

namespace ProtoBuf.Connect
{
    /// <summary>
    /// The canonical protobuf JSON codec, backed by a generated <see cref="IJsonModel"/>.
    /// </summary>
    /// <remarks>
    /// This is the <strong>code-first</strong> JSON codec. A contract-first application uses
    /// <c>GoogleJsonConnectCodec</c> instead, which mines the method's own <c>Marshaller&lt;T&gt;</c>
    /// for a descriptor and hands the work to Google's <c>JsonFormatter</c>; there is exactly one
    /// correct implementation of the mapping per world, and neither is a reimplementation of the
    /// other.
    /// <para>
    /// Unlike that one there is <b>no transcode</b> here: the generated serializers write UTF-8 bytes
    /// straight into the destination through <see cref="Utf8JsonWriter"/>, where the contract-first
    /// path goes bytes → <see cref="string"/> → bytes.
    /// </para>
    /// </remarks>
    public sealed class JsonConnectCodec : ConnectCodec
    {
        private readonly IJsonModel _model;

        /// <summary>Creates a codec over a generated model that carries the JSON mapping.</summary>
        /// <param name="model">
        /// A <c>[ProtoModel]</c>-generated type. The generator emits <see cref="IJsonModel"/> onto it
        /// only when the consumer references this assembly, so a model that does not implement it was
        /// built without the JSON half rather than having failed at it.
        /// </param>
        public JsonConnectCodec(IJsonModel model)
            => _model = model ?? throw new ArgumentNullException(nameof(model));

        /// <inheritdoc/>
        public override string Name => "json";

        /// <inheritdoc/>
        /// <remarks>
        /// Always <c>null</c>: measuring would mean encoding twice, since JSON has no cheap
        /// length-only pass the way protobuf-net's <c>Measure</c> does. So a JSON body states no
        /// <c>Content-Length</c> and every enveloped message is buffered - the same cost the
        /// contract-first JSON codec pays, and a real difference from binary.
        /// </remarks>
        protected override long? MeasureCore<T>(T value, IConnectMessageCodec<T>? over) => null;

        /// <inheritdoc/>
        protected override void WriteCore<T>(IBufferWriter<byte> destination, T value, IConnectMessageCodec<T>? over)
        {
            // Dispose is what flushes into the destination; without it the body is empty
            using var writer = new Utf8JsonWriter(destination);
            Serializer<T>().Write(writer, value);
        }

        /// <inheritdoc/>
        protected override T ReadCore<T>(in ReadOnlySequence<byte> source, IConnectMessageCodec<T>? over)
        {
            var reader = new Utf8JsonReader(source);
            return Serializer<T>().Read(ref reader, default!);
        }

        /// <summary>
        /// The generated serializer for <typeparamref name="T"/>, or a throw that says why there is none.
        /// </summary>
        /// <remarks>
        /// A null here is not a failure of the generator: several shapes protobuf-net serializes
        /// perfectly well - a <c>[ProtoInclude]</c> hierarchy, an extensible contract, a level-200
        /// <c>DateTime</c> - have no canonical JSON form at all, and are reported at build time as
        /// PBN3005. So the message names the type and points at the diagnostic, rather than saying
        /// something went wrong.
        /// </remarks>
        private IJsonSerializer<T> Serializer<T>()
            => _model.GetJsonSerializer<T>() ?? throw new NotSupportedException(
                $"'{typeof(T).Name}' has no canonical protobuf JSON mapping, so it cannot be served over "
                + "application/json; the build reports PBN3005 saying which part of its shape has no JSON "
                + "form. Use the 'proto' codec for this method, or change the shape.");
    }
}
