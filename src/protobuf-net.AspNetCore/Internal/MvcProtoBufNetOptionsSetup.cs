using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using ProtoBuf.AspNetCore.Formatters;

namespace ProtoBuf.AspNetCore.Internal
{
    internal sealed class MvcProtoBufNetOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly MvcProtoBufNetOptions _options;
        public MvcProtoBufNetOptionsSetup(IOptions<MvcProtoBufNetOptions> options)
            => _options = options.Value;

        void IConfigureOptions<MvcOptions>.Configure(MvcOptions options)
        {
            var output = new ProtoOutputFormatter(_options, options);
            AddMediaTypes(output.SupportedMediaTypes);
            options.OutputFormatters.Add(output);

            var input = new ProtoInputFormatter(_options, options);
            AddMediaTypes(input.SupportedMediaTypes);
            options.InputFormatters.Add(input);

            options.FormatterMappings.SetMediaTypeMappingForFormat("protobuf", "application/protobuf");

            static void AddMediaTypes(MediaTypeCollection mediaTypes)
            {
                mediaTypes.Add("application/protobuf");
                mediaTypes.Add("application/x-protobuf");
                mediaTypes.Add("application/vnd.google.protobuf");
            }
        }
    }
}
