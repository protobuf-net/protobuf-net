using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Runtime.Serialization;
using ProtoBuf;

namespace Examples.SimpleStream
{
    [TestFixture]
    public class NullTypes
    {
        [DataContract]
        class TypeWithNulls
        {
            [DataMember(Order = 1)]
            public int? Foo { get; set; }
        }
        [Test]
        public void TestNull()
        {
            TypeWithNulls twn = new TypeWithNulls { Foo = null },
                clone = Serializer.DeepClone(twn);
            Assert.IsNull(twn.Foo);
            Assert.IsTrue(Program.CheckBytes(twn, new byte[0]));
        }
        [Test]
        public void TestNotNull()
        {
            TypeWithNulls twn = new TypeWithNulls { Foo = 150 },
                clone = Serializer.DeepClone(twn);
            Assert.IsNotNull(twn.Foo);
            Assert.IsTrue(Program.CheckBytes(twn, 0x08, 0x96, 0x01));
        }
    }
}
