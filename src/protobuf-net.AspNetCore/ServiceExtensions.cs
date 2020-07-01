using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using ProtoBuf.Formatters;
using ProtoBuf.Meta;

namespace ProtoBuf
{
    /// <summary>
    /// Provides support methods for registering protobuf with the MVC
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Register protobuf-net formatters with MVC with all common content-types
        /// </summary>
        public static void RegisterProtobufFormatters(this MvcOptions options, TypeModel model = null, long maxWriteLength = ProtoOutputFormatter.DefaultMaxLength)
        {
            model ??= RuntimeTypeModel.Default;

            var output = new ProtoOutputFormatter(model, maxWriteLength);
            AddMediaTypes(output.SupportedMediaTypes);
            options.OutputFormatters.Add(output);

            var input = new ProtoInputFormatter(model);
            AddMediaTypes(input.SupportedMediaTypes);
            options.InputFormatters.Add(input);

            static void AddMediaTypes(MediaTypeCollection mediaTypes)
            {
                mediaTypes.Add("application/protobuf");
                mediaTypes.Add("application/x-protobuf");
                mediaTypes.Add("application/vnd.google.protobuf");
                // mediaTypes.Add("application/octet-stream");
            }
        }
    }
}
