using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace ProtoBuf.AspNetCore.Formatters
{
    /// <summary>
    /// Implements a protobuf-net based input formatter
    /// </summary>
    public sealed class ProtoInputFormatter : InputFormatter
    {
        private readonly TypeModel _model;
        private readonly int _memoryBufferThreshold;
        private readonly bool _suppressBuffering;

        /// <summary>
        /// Create a new <see cref="ProtoInputFormatter"/> instance
        /// </summary>
        public ProtoInputFormatter(MvcProtoBufNetOptions options, MvcOptions mvcOptions)
        {
            _model = options.Model ?? RuntimeTypeModel.Default;
            _memoryBufferThreshold = options.ReadMemoryBufferThreshold;
            _suppressBuffering = mvcOptions.SuppressInputFormatterBuffering;
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
            // note: *today*, protobuf-net lacks fully async read, so this buffers in the pipe until
            // we get the Content-Length that was advertised by the caller

            if (context is null) throw new ArgumentNullException(nameof(context));
            var request = context.HttpContext.Request;

            var length = request.ContentLength ?? -1;
            if (length < 0 || length > _memoryBufferThreshold)
            {
                // use Stream-based read - either chunked or oversized
                // - if buffering is disabled, or if the caller has already buffered it fully (EnableRewind), go direct
                // - otherwise uses FileBufferingReadStream based on _memoryBufferThreshold
                return (_suppressBuffering || request.Body.CanSeek) ? DirectStreamAsync(context) : BufferedStreamAsync(context);
            }

            // otherwise, we can use the Pipe itself for in-memory buffering
            var reader = request.BodyReader;

            // try and read synchronously
            if (reader.TryRead(out var readResult) && ProcessReadBuffer(context.ModelType, reader, length, readResult, out var payload))
            {
                return InputFormatterResult.SuccessAsync(payload);
            }
            return ReadRequestBodyAsyncSlow(context.ModelType, reader, length);
        }

        private Task<InputFormatterResult> DirectStreamAsync(InputFormatterContext context)
        {
            var payload = _model.Deserialize(context.HttpContext.Request.Body, value: null, type: context.ModelType);
            return InputFormatterResult.SuccessAsync(payload);
        }
        private async Task<InputFormatterResult> BufferedStreamAsync(InputFormatterContext context)
        {
            using var readStream = new FileBufferingReadStream(context.HttpContext.Request.Body, _memoryBufferThreshold);
            await readStream.DrainAsync(CancellationToken.None);
            readStream.Position = 0;
            var payload = _model.Deserialize(readStream, value: null, type: context.ModelType);
            return InputFormatterResult.Success(payload);
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
                var readResult = await reader.ReadAsync();
                if (ProcessReadBuffer(type, reader, length, readResult, out var payload))
                {
                    return InputFormatterResult.Success(payload);
                }
            }
        }
        private bool ParseIfComplete(Type type, ref ReadOnlySequence<byte> buffer, long expectedLength, out object payload)
        {
            long availableLength = buffer.Length;
            if (availableLength < expectedLength)
            {   // not enough (yet, we can retry later)
                payload = default;
                return false;
            }
            else if (availableLength > expectedLength)
            {   // too much; cut it down to size
                buffer = buffer.Slice(0, expectedLength);
            }

            payload = _model.Deserialize(buffer, type: type);
            return true;
        }
    }
}
