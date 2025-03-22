using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Meta;
using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf;

internal sealed class ProtoHybridCacheSerializerFactory : IHybridCacheSerializerFactory
{
    internal TypeModel Model { get; }
    public ProtoHybridCacheSerializerFactory([FromKeyedServices(typeof(HybridCache))] TypeModel? model = null)
    {
        Model = model ?? RuntimeTypeModel.Default;
    }

#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    public bool TryCreateSerializer<T>(
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
#if NETCOREAPP3_0_OR_GREATER
        [NotNullWhen(true)]
#endif
        out IHybridCacheSerializer<T>? serializer)
    {
        if (Model.CanSerializeContractType(typeof(T)))
        {
            serializer = new ProtoHybridCacheSerializer<T>(Model);
            return true;
        }
        serializer = null;
        return false;
    }
}

