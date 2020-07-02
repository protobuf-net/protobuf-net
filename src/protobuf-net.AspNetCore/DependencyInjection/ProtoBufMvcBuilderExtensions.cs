using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ProtoBuf.AspNetCore;
using ProtoBuf.AspNetCore.Internal;
using System;

namespace Microsoft.Extensions.DependencyInjection // guidance is to use this namespace
{
    /// <summary>
    /// Provides support methods for registering protobuf-net with ASP.NET Core
    /// </summary>
    public static class ProtoBufMvcBuilderExtensions
    {
        /// <summary>
        /// Register protobuf-net formatters with a <see cref="IMvcCoreBuilder"/> with all common content-types
        /// </summary>
        public static IMvcCoreBuilder AddProtoBufNet(this IMvcCoreBuilder builder, Action<MvcProtoBufNetOptions> setupAction = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, MvcProtoBufNetOptionsSetup>());
            if (setupAction is object) builder.Services.Configure(setupAction);
            return builder;
        }

        /// <summary>
        /// Register protobuf-net formatters with a <see cref="IMvcBuilder"/> with all common content-types
        /// </summary>
        public static IMvcBuilder AddProtoBufNet(this IMvcBuilder builder, Action<MvcProtoBufNetOptions> setupAction = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<MvcOptions>, MvcProtoBufNetOptionsSetup>());
            if (setupAction is object) builder.Services.Configure(setupAction);
            return builder;
        }
    }
}
