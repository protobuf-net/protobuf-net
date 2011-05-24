using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using System.IO;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO6109616
    {
        [ProtoContract]
        public class B
        {
            [ProtoMember(1)]
            public int Y;
        }

        [ProtoContract]
        public class C
        {
            [ProtoMember(1)]
            public int Y;
        }
        [Test]
        public void Execute()
        {
            TypeModel model = RuntimeTypeModel.Default;
            using (var ms = new MemoryStream())
            {
                var tagToType = new Dictionary<int, Type>
                {
                    {1, typeof(B)}, {2, typeof(C)}
                };
                var typeToTag = tagToType.ToDictionary(pair => pair.Value, pair => pair.Key);
                
                object b = new B { Y = 2 };
                object c = new C { Y = 4 };
                // in v1, comparable to Serializer.NonGeneric.SerializeWithLengthPrefix(ms, b, PrefixStyle.Base128, typeToTag[b.GetType()]);
                model.SerializeWithLengthPrefix(ms, b, null, PrefixStyle.Base128, typeToTag[b.GetType()]);
                model.SerializeWithLengthPrefix(ms, c, null, PrefixStyle.Base128, typeToTag[c.GetType()]);
                ms.Position = 0;

                // in v1, comparable to Serializer.NonGeneric.TryDeserializeWithLengthPrefix(ms, PrefixStyle.Base128, key => tagToType[key], out b2);
                object b2 = model.DeserializeWithLengthPrefix(ms, null, null, PrefixStyle.Base128, 0, key => tagToType[key]);
                object c2 = model.DeserializeWithLengthPrefix(ms, null, null, PrefixStyle.Base128, 0, key => tagToType[key]);
                
                Assert.AreEqual(((B)b).Y, ((B)b2).Y);
                Assert.AreEqual(((C)c).Y, ((C)c2).Y);
            }
        }
    }
}
