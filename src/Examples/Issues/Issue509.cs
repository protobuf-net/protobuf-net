using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue509
    {

        public class Item
        {
            public int Value { get; set; }
            public Item Child1 { get; set; }
            public Item Child2 { get; set; }
        }

        [ProtoContract]
        public class ItemSurrogate
        {
            [ProtoMember(1)]
            public int Value { get; set; }

            [ProtoMember(2)]
            public Item Child1 { get; set; }

            [ProtoMember(3)]
            public Item Child2 { get; set; }

            public static implicit operator Item(ItemSurrogate s)
            {
                return s == null ? null : new Item { Value = s.Value, Child1 = s.Child1, Child2 = s.Child2 };
            }

            public static implicit operator ItemSurrogate(Item i)
            {
                return i == null ? null : new ItemSurrogate { Value = i.Value,  Child1 = i.Child1, Child2 = i.Child2 };
            }
        }


        [Fact]
        public void TestUncompiled() => Test(false);

        [Fact]
        public void TestCompiled() => Test(true);

        private void Test(bool compile)
        { 
            var model = RuntimeTypeModel.Create();
            var item = model.Add(typeof(Item), false);
            item.SetSurrogate(typeof(ItemSurrogate));
            item.AsReferenceDefault = true;
            var typeModel = compile ? model.Compile() : model;

            var item1 = new Item { Value = 1 };
            var item2 = new Item { Value = 2, Child1 = item1, Child2 = item1 };

            MemoryStream ms = new MemoryStream();
            typeModel.Serialize(ms, item2);
            ms.Seek(0, SeekOrigin.Begin);
            var newItem2 = (Item) typeModel.Deserialize(ms, null, typeof(Item));
            Assert.Equal(item2.Value, newItem2.Value);
            Assert.Equal(item1.Value, newItem2.Child1.Value);
            Assert.Same(newItem2.Child1, newItem2.Child2);
        }    
    }
}
