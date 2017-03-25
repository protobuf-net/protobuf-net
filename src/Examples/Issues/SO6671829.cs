using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using NUnit.Framework;
using ProtoBuf.Meta;
using System.IO;

namespace Examples.Issues
{
    [TestFixture]
    public class SO6671829
    {
        [Test]
        public void Execute()
        {
            var model = TypeModel.Create();
            model.Add(typeof(hierarchy.B), false)
                .Add("prop1", "prop2");

            var hb = new hierarchy.B();
            hb.prop1 = "prop1";
            hb.prop2 = "prop2";

            var ms = new MemoryStream();

            model.Serialize(ms, hb);
            ms.Position = 0;
            var flatB = Serializer.Deserialize<flat.B>(ms);

            Assert.AreEqual("prop1", hb.prop1);
            Assert.AreEqual("prop2", hb.prop2);
            Assert.AreEqual("prop1", flatB.prop1);
            Assert.AreEqual("prop2", flatB.prop2);
            Assert.AreEqual("prop1=prop1, prop2=prop2", hb.ToString());
            Assert.AreEqual("prop1=prop1, prop2=prop2", flatB.ToString());
        }
        class hierarchy
        {

            [ProtoContract]
            public class A
            {
                [ProtoMember(1)]
                public string prop1 { get; set; }
            }

            [ProtoContract]
            public class B : A
            {
                public B()
                {
                }

                [ProtoMember(1)]
                public string prop2 { get; set; }

                public override string ToString()
                {
                    return "prop1=" + prop1 + ", prop2=" + prop2;
                }

            }
        }

        class flat
        {
            [ProtoContract]
            public class B
            {
                [ProtoMember(1)]
                public string prop1 { get; set; }

                [ProtoMember(2)]
                public string prop2 { get; set; }

                public override string ToString()
                {
                    return "prop1=" + prop1 + ", prop2=" + prop2;
                }
            }
        }
    }
}
