using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using ProtoBuf.Meta;
using System;
using System.Threading.Tasks;

namespace ProtoBuf.AspNetCore.Formatters
{
    /// <summary>
    /// Implements a protobuf-net based output formatter
    /// </summary>
    public sealed class ProtoOutputFormatter : OutputFormatter
    {
        private readonly TypeModel _model;
        private readonly long _maxLength;
        private readonly bool _suppressBuffering;

        /// <summary>
        /// Create a new <see cref="ProtoOutputFormatter"/> instance
        /// </summary>
        public ProtoOutputFormatter(MvcProtoBufNetOptions options, MvcOptions mvcOptions)
        {
            _model = options.Model ?? RuntimeTypeModel.Default;
            _maxLength = options.WriteMaxLength;
            _suppressBuffering = mvcOptions.SuppressOutputFormatterBuffering;
        }

        /// <inheritdoc/>
        protected override bool CanWriteType(Type type)
            => _model.CanSerializeContractType(type);

        /// <inheritdoc/>
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            var response = context.HttpContext.Response;
            using (var measureState = _model.Measure<object>(context.Object, abortAfter: _maxLength))
            {
                response.ContentLength = measureState.Length;

                // do it for real (verified for variance automatically)
                if (_suppressBuffering)
                {
                    measureState.Serialize(response.Body);
                }
                else
                {
                    measureState.Serialize(response.BodyWriter);
                }
            }

            // and flush
            if (_suppressBuffering)
            {
                return response.Body.FlushAsync();
            }
            else
            {
                var flush = response.BodyWriter.FlushAsync();
                return flush.IsCompletedSuccessfully ? Task.CompletedTask : flush.AsTask();
            }
        }
    }
}
