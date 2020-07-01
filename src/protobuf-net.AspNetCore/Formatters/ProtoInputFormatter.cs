using Microsoft.AspNetCore.Mvc.Formatters;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace ProtoBuf.Formatters
{
    /// <summary>
    /// Implements a protobuf-net based input formatter
    /// </summary>
    public sealed class ProtoInputFormatter : InputFormatter
    {
        private readonly TypeModel _model;

        /// <summary>
        /// Create a new <see cref="ProtoInputFormatter"/> instance
        /// </summary>
        /// <param name="model">The type-model to use for deserialization</param>
        public ProtoInputFormatter(TypeModel model = null)
        {
            _model = model ?? RuntimeTypeModel.Default;
        }

        /// <inheritdoc/>
        protected override bool CanReadType(Type type)
            => _model.CanSerializeContractType(type);

        /// <inheritdoc/>
        public override Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            // default implementation does length checks; we *really* don't want those, since
            // zero length is a perfectly valid payload in protobuf
            return ReadRequestBodyAsync(context);
        }

        /// <inheritdoc/>
        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            var request = context.HttpContext.Request;
            var length = request.ContentLength ?? -1;
            if (length < 0) throw new InvalidOperationException("Invalid or missing content-length");
            var reader = request.BodyReader;
            var type = context.ModelType;

            // try and read synchronously
            if (reader.TryRead(out var readResult) && ProcessReadBuffer(type, reader, length, readResult, out var payload))
            {
                return InputFormatterResult.SuccessAsync(payload);
            }
            return ReadRequestBodyAsyncSlow(type, reader, length);
        }

        private bool ProcessReadBuffer(Type type, PipeReader reader, long length, in ReadResult readResult, out object payload)
        {
            if (readResult.IsCanceled) throw new OperationCanceledException();
            var buffer = readResult.Buffer;
            if (ParseIfComplete(type, ref buffer, length, out payload))
            {
                // mark consumed
                reader.AdvanceTo(buffer.End, buffer.End);
                return true;
            }
            else if (readResult.IsCompleted)
            {
                throw new InvalidOperationException($"Incomplete protobuf payload received; got {buffer.Length} of {length} bytes");
            }
            // not enough data; put it back by saying we looked at everything and took nothing
            reader.AdvanceTo(buffer.Start, buffer.End);
            return false;
        }

        private async Task<InputFormatterResult> ReadRequestBodyAsyncSlow(Type type, PipeReader reader, long length)
        {
            while (true)
            {
                if (!reader.TryRead(out var readResult))
                {
                    readResult = await reader.ReadAsync().ConfigureAwait(false);
                }
                if (ProcessReadBuffer(type, reader, length, readResult, out var payload))
                {
                    return InputFormatterResult.Success(payload);
                }
            }
        }
        private bool ParseIfComplete(Type type, ref ReadOnlySequence<byte> buffer, long expectedLength, out object payload)
        {
            long availableLength = buffer.Length;
            if (availableLength == expectedLength) { }
            else if (availableLength < expectedLength)
            {
                payload = default;
                return false;
            }
            else if (availableLength > expectedLength)
            {
                buffer = buffer.Slice(0, expectedLength);
            }

            payload = _model.Deserialize(buffer, type: type);
            return true;
        }
    }
}
