using ProtoBuf.Internal;
using ProtoBuf.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace ProtoBuf.Meta
{
    public class ProviderTests
    {
        [Fact]
        public void SerializerConfigBasicSetup()
        {
            var model = new MyModel();

            Assert.True(model.IsDefined(typeof(A)));
            Assert.False(model.IsDefined(typeof(B)));
            Assert.True(model.IsDefined(typeof(Haz)));
            Assert.False(model.IsDefined(typeof(HazNot)));
            Assert.True(model.IsDefined(typeof(Proxied)));

            var serType = TypeModel.TryGetSerializer<A>(model)?.GetType();
            Assert.Equal(typeof(EnumSerializerInt32<A>), serType);
            Assert.Equal(typeof(A), serType.GetGenericArguments().Single());

            serType = TypeModel.TryGetSerializer<B>(model)?.GetType();
            Assert.Null(serType);

            Assert.IsType<MyProvider>(TypeModel.TryGetSerializer<Haz>(model));
            Assert.Null(TypeModel.TryGetSerializer<HazNot>(model));
            Assert.IsType<ProxySerializer>(TypeModel.TryGetSerializer<Proxied>(model));

            // now let's look just via the model
            serType = model.GetSerializerCore<A>(default)?.GetType();
            Assert.Equal(typeof(EnumSerializerInt32<A>), serType);
            Assert.Equal(typeof(A), serType.GetGenericArguments().Single());

            // B *should not* work when accessed directly
            Assert.Null(model.GetSerializerCore<B>(default));
            Assert.IsType<MyProvider>(model.GetSerializerCore<Haz>(default));
            Assert.Null(model.GetSerializerCore<HazNot>(default));
            Assert.IsType<ProxySerializer>(model.GetSerializerCore<Proxied>(default));
        }

        [Fact]
        public void NullablesWorkButAreNotDefined()
        {
            var model = new MyModel();
            Assert.True(model.IsDefined(typeof(A)));
            Assert.True(model.IsDefined(typeof(A?)));
            Assert.False(model.IsDefined(typeof(B)));
            Assert.False(model.IsDefined(typeof(B?)));

            Assert.NotNull(model.GetSerializerCore<A>(default));
            Assert.NotNull(model.GetSerializerCore<A?>(default));
            Assert.Null(model.GetSerializerCore<B>(default));
            Assert.Null(model.GetSerializerCore<B?>(default));
        }

        class MyModel : TypeModel
        {
            protected override ISerializer<T> GetSerializer<T>()
                => GetSerializer<MyProvider, T>();
        }
        class MyProvider : ISerializer<Haz>, ISerializerProxy<A>, ISerializerProxy<A?>, ISerializerProxy<Proxied>
        {
            SerializerFeatures ISerializer<Haz>.Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;

            ISerializer<Proxied> ISerializerProxy<Proxied>.Serializer => SerializerCache.Get<ProxySerializer, Proxied>();

            ISerializer<A> ISerializerProxy<A>.Serializer => EnumSerializer.CreateInt32<A>();
            ISerializer<A?> ISerializerProxy<A?>.Serializer => EnumSerializer.CreateInt32<A>();

            Haz ISerializer<Haz>.Read(ref ProtoReader.State state, Haz value)
                => throw new NotImplementedException();

            void ISerializer<Haz>.Write(ref ProtoWriter.State state, Haz value)
                => throw new NotImplementedException();
        }

        class Proxied
        { }

        class ProxySerializer : ISerializer<Proxied>
        {
            SerializerFeatures ISerializer<Proxied>.Features => SerializerFeatures.CategoryMessage | SerializerFeatures.WireTypeString;

            Proxied ISerializer<Proxied>.Read(ref ProtoReader.State state, Proxied value)
                => throw new NotImplementedException();

            void ISerializer<Proxied>.Write(ref ProtoWriter.State state, Proxied value)
                => throw new NotImplementedException();
        }
        class Haz { }
        class HazNot { }

        enum A { }
        enum B { }
    }
}
