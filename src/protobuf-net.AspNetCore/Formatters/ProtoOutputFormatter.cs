using Microsoft.AspNetCore.Mvc.Formatters;
using ProtoBuf.Meta;
using System;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace ProtoBuf.Formatters
{
    /// <summary>
    /// Implements a protobuf-net based output formatter
    /// </summary>
    public sealed class ProtoOutputFormatter : OutputFormatter
    {
        private readonly TypeModel _model;
        private readonly long _maxLength;
        internal const long DefaultMaxLength = -1;

        /// <summary>
        /// Create a new <see cref="ProtoOutputFormatter"/> instance
        /// </summary>
        /// <param name="model">The type-model to use for serialization</param>
        /// <param name="maxLength">The maximum supported payload length to encode (no limit by default)</param>
        public ProtoOutputFormatter(TypeModel model = null, long maxLength = DefaultMaxLength)
        {
            _model = model ?? RuntimeTypeModel.Default;
            _maxLength = maxLength;
        }

        /// <inheritdoc/>
        protected override bool CanWriteType(Type type)
            => _model.CanSerializeContractType(type);

        /// <inheritdoc/>
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            long length;
            var response = context.HttpContext.Response;
            using (var measureState = _model.Measure<object>(context.Object, abortAfter: _maxLength))
            {
                response.ContentLength = length = measureState.Length;

                // do it for real (verified for variance automatically)
                measureState.Serialize(response.BodyWriter);
            }

            // flush etc
            var flush = response.BodyWriter.FlushAsync();
            return flush.IsCompletedSuccessfully ? Task.CompletedTask : Awaited(flush);

            static async Task Awaited(ValueTask<FlushResult> pending)
                => await pending.ConfigureAwait(false);
        }
    }
}
