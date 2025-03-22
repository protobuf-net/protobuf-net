using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf.Meta;
using Xunit;

namespace ProtoBuf.Test;

public class HybridCacheTests
{
    [Fact]
    public void FactoryGetsDefaultModel()
    {
        var services = new ServiceCollection();
        services.AddProtobufNetHybridCacheSerializer();

        using var provider = services.BuildServiceProvider();
        var factory = Assert.IsType<ProtoHybridCacheSerializerFactory>(Assert.Single(provider.GetServices<IHybridCacheSerializerFactory>()));
        Assert.Same(RuntimeTypeModel.Default, factory.Model);

        Assert.True(factory.TryCreateSerializer<Foo>(out var serializer));
        var typed = Assert.IsType<ProtoHybridCacheSerializer<Foo>>(serializer);
        Assert.Same(RuntimeTypeModel.Default, typed.Model);
    }

    [Fact]
    public void FactoryGetsBespokeModel()
    {
        var model = RuntimeTypeModel.Create();
        var services = new ServiceCollection();
        services.AddProtobufNetHybridCachModel(model);
        services.AddProtobufNetHybridCacheSerializer();

        using var provider = services.BuildServiceProvider();
        var factory = Assert.IsType<ProtoHybridCacheSerializerFactory>(Assert.Single(provider.GetServices<IHybridCacheSerializerFactory>()));
        Assert.Same(model, factory.Model);

        Assert.True(factory.TryCreateSerializer<Foo>(out var serializer));
        var typed = Assert.IsType<ProtoHybridCacheSerializer<Foo>>(serializer);
        Assert.Same(model, typed.Model);
    }

    [ProtoContract]
    public class Foo { }
}
