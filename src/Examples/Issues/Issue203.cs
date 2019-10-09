#if FEAT_DYNAMIC_REF

using ProtoBuf;
using System;
using System.Collections.Generic;
using Xunit;

namespace Examples.Issues
{

    public class Issue203
    {
        [ProtoContract(SkipConstructor = true)]
        public class SerializeClass
        {
            [ProtoMember(1, AsReference = true)]
            private SomeCollection _someList = null;
            public SomeCollection SomeList
            {
                get
                {
                    return _someList;
                }
                set
                {
                    _someList = value;
                }
            }
        }

        public class SomeCollection : List<SomeCollectionItem>
        { }

        [ProtoContract(SkipConstructor = true)]
        public class SomeCollectionItem
        {
            public SomeCollectionItem()
            {
                throw new InvalidOperationException("I should never be called");
            }

            public SomeCollectionItem(string init)
            {
                SomeField = init;
            }

            [ProtoMember(1)]
            public string SomeField;
        }

        [Fact]
        public void Execute()
        {
            for (int i = 0; i < 5; i++)
            {
                SerializeClass m = new SerializeClass();

                var u = new SomeCollectionItem("ABC");
                m.SomeList = new SomeCollection();
                m.SomeList.Add(u);
                m.SomeList.Add(u);

                var clone = Serializer.DeepClone(m);
                Assert.NotSame(m, clone);
            }
        }
    }
}


#endif