using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Meta;
using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf;

internal sealed class ProtoSerializerFactory : IHybridCacheSerializerFactory
{
    private readonly TypeModel model;
    public ProtoSerializerFactory([FromKeyedServices(typeof(ProtobufNetServiceExtensions))] TypeModel? model = null)
    {
        this.model = model ?? TypeModel.DefaultModel;
    }
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
    public bool TryCreateSerializer<T>(
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
#if NETCOREAPP3_0_OR_GREATER
        [NotNullWhen(true)]
#endif
        out IHybridCacheSerializer<T>? serializer)
    {
        if (model.CanSerializeContractType(typeof(T)))
        {
            serializer = new ProtoSerializer<T>(model);
            return true;
        }
        serializer = null;
        return false;
    }
}

