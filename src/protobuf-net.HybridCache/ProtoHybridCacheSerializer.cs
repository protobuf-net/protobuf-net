using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Meta;
using System.Buffers;

namespace ProtoBuf;

internal sealed class ProtoHybridCacheSerializer<T> : IHybridCacheSerializer<T>
{
    internal TypeModel Model { get; }
    public ProtoHybridCacheSerializer([FromKeyedServices(typeof(HybridCache))] TypeModel? model = null)
    {
        Model = model ?? RuntimeTypeModel.Default;
    }

    public T Deserialize(ReadOnlySequence<byte> source)
        => Model.Deserialize<T>(source);

    public void Serialize(T value, IBufferWriter<byte> target)
        => Model.Serialize<T>(target, value);
}