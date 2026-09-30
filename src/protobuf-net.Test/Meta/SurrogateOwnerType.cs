using System.Linq;
using Xunit;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Meta
{
    public class SurrogateOwnerTypeTests
    {
        public class Root
        {
            public int Id { get; set; }
        }

        public class Derived : Root
        {
            public int Number { get; set; }
        }

        public class Leaf : Derived { }

        [ProtoContract]
        [ProtoInclude(10, typeof(DerivedSurrogate))]
        public class RootSurrogate
        {
            [ProtoMember(1)] public int Id { get; set; }

            public static implicit operator RootSurrogate(Root value) => value switch
            {
                null => null,
                Derived derived => new DerivedSurrogate { Id = derived.Id, Number = derived.Number },
                _ => new RootSurrogate { Id = value.Id },
            };

            public static implicit operator Root(RootSurrogate value) => value switch
            {
                null => null,
                DerivedSurrogate derived => new Derived { Id = derived.Id, Number = derived.Number },
                _ => new Root { Id = value.Id },
            };
        }

        [ProtoContract]
        public class DerivedSurrogate : RootSurrogate
        {
            [ProtoMember(1)] public int Number { get; set; }
        }

        public class Plain
        {
            public int Value { get; set; }
        }

        public class PlainBase { }

        public class PlainDerived : PlainBase { }

        public class NeverAdded { }

        static RuntimeTypeModel CreateModel()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            var root = model.Add(typeof(Root), false);
            root.AddSubType(10, typeof(Derived));
            model[typeof(Derived)].AddSubType(11, typeof(Leaf));
            root.SetSurrogate(typeof(RootSurrogate));

            model.Add(typeof(Plain), true);
            model.Add(typeof(PlainBase), true).AddSubType(10, typeof(PlainDerived));
            return model;
        }

        [Fact]
        public void DerivedType_ReturnsTheRootThatOwnsTheSurrogate()
        {
            var model = CreateModel();

            Assert.Equal(typeof(Root), model[typeof(Derived)].GetSurrogateOwnerType());
            Assert.Equal(typeof(Root), model.GetSurrogateOwnerType(typeof(Derived)));

            // two levels down the chain
            Assert.Equal(typeof(Root), model[typeof(Leaf)].GetSurrogateOwnerType());
            Assert.Equal(typeof(Root), model.GetSurrogateOwnerType(typeof(Leaf)));
        }

        [Fact]
        public void RootType_ReturnsItself()
        {
            var model = CreateModel();

            Assert.Equal(typeof(Root), model[typeof(Root)].GetSurrogateOwnerType());
            Assert.Equal(typeof(Root), model.GetSurrogateOwnerType(typeof(Root)));
        }

        [Fact]
        public void TypeWithoutSurrogate_ReturnsItself()
        {
            var model = CreateModel();

            Assert.Equal(typeof(Plain), model[typeof(Plain)].GetSurrogateOwnerType());
            Assert.Equal(typeof(Plain), model.GetSurrogateOwnerType(typeof(Plain)));

            // in a hierarchy, but no surrogate anywhere in the chain
            Assert.Equal(typeof(PlainDerived), model[typeof(PlainDerived)].GetSurrogateOwnerType());
            Assert.Equal(typeof(PlainDerived), model.GetSurrogateOwnerType(typeof(PlainDerived)));
        }

        [Fact]
        public void UnknownType_ReturnsItself_WithoutBeingAddedToTheModel()
        {
            var model = CreateModel();
            var before = model.GetTypes().Cast<MetaType>().Select(mt => mt.Type).ToArray();
            Assert.DoesNotContain(typeof(NeverAdded), before);

            Assert.Equal(typeof(NeverAdded), model.GetSurrogateOwnerType(typeof(NeverAdded)));

            var after = model.GetTypes().Cast<MetaType>().Select(mt => mt.Type).ToArray();
            Assert.Equal(before, after);
        }
    }
}
