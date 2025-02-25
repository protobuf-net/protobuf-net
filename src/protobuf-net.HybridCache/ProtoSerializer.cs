using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Meta;
using System.Buffers;

namespace ProtoBuf;

internal sealed class ProtoSerializer<T> : IHybridCacheSerializer<T>
{
    private readonly TypeModel model;
    public ProtoSerializer([FromKeyedServices(typeof(ProtobufNetServiceExtensions))] TypeModel? model = null)
    {
        this.model = model ?? TypeModel.DefaultModel;
    }

    public T Deserialize(ReadOnlySequence<byte> source)
        => model.Deserialize<T>(source);

    public void Serialize(T value, IBufferWriter<byte> target)
        => model.Serialize<T>(target, value);
}