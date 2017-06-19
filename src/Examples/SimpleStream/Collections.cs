using System;
using System.Collections.Generic;
using Xunit;
using ProtoBuf;

namespace Examples.SimpleStream
{
    
    public class Collections
    {

        [ProtoContract]
        public class FooContainer
        {
            [ProtoMember(1)]
            public Foo Foo { get; set; }
        }
        [ProtoContract]
        public class Foo
        {
            [ProtoMember(1)]
            public List<Bar> Bars { get; set; }
        }
        [ProtoContract]
        public class Bar
        {
            [ProtoMember(1)]
            public int Value { get; set; }
        }

        static Foo GetFullFoo()
        {
            Foo foo = new Foo();
            foo.Bars = new List<Bar>();
            for (int i = Int16.MinValue; i <= Int16.MaxValue; i++)
            {
                foo.Bars.Add(new Bar { Value = i });
            }
            return foo;
        }

        [Fact]
        public void RunWrappedCollectionTest()
        {
            FooContainer fooContainer = new FooContainer(), cloneContainer;
            Foo foo = GetFullFoo(), clone;
            fooContainer.Foo = foo;

            cloneContainer = Serializer.DeepClone(fooContainer);
            clone = cloneContainer.Foo;
            Assert.NotNull(cloneContainer); //, "Clone Container");
            Assert.True(CompareFoos(foo, clone));
        }

        [Fact]
        public void RunNakedCollectionTest()
        {
            Foo foo = GetFullFoo(), clone = Serializer.DeepClone(foo);
            Assert.True(CompareFoos(foo, clone));            
        }

        private static bool CompareFoos(Foo original, Foo clone) {
            Assert.NotNull(original); //, "Original");
            Assert.NotNull(clone); //, "Clone");
            Assert.Equal(original.Bars.Count, clone.Bars.Count); //, "Item count");
            if (original == null || clone == null ||
                original.Bars.Count != clone.Bars.Count) return false;
            int count = clone.Bars.Count;
            for (int i = 0; i < count; i++)
            {
                Assert.Equal(original.Bars[i].Value, clone.Bars[i].Value); //, "Value mismatch");
                if (original.Bars[i].Value != clone.Bars[i].Value) return false;
            }
            return true;
        }
    }
}
