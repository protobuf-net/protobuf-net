using Microsoft.Extensions.Caching.Hybrid;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration methods for <see cref="HybridCache"/> serialization using protobuf-net
/// </summary>
public static class ProtobufNetServiceExtensions
{
    /// <summary>
    /// Register a specific serializer instance to use for <see cref="HybridCache"/> serialization.
    /// </summary>
    public static IServiceCollection AddProtobufNetHybridCachModel(this IServiceCollection services, TypeModel model)
    {
        services.AddKeyedSingleton<TypeModel>(typeof(HybridCache), model);
        return services;
    }

    /// <summary>
    /// Support all protobuf-net contract types for <see cref="HybridCache"/> serialization.
    /// </summary>
    public static IServiceCollection AddProtobufNetHybridCacheSerializer(this IServiceCollection services)
    {
        services.AddSingleton<IHybridCacheSerializerFactory, ProtoHybridCacheSerializerFactory>();
        return services;
    }

    /// <summary>
    /// Support a specific protobuf-net contract type for <see cref="HybridCache"/> serialization.
    /// </summary>
    public static IServiceCollection AddProtobufNetHybridCacheSerializer<T>(this IServiceCollection services)
    {
        services.AddSingleton<IHybridCacheSerializer<T>, ProtoHybridCacheSerializer<T>>();
        return services;
    }
}