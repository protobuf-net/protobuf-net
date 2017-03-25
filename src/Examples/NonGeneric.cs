using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;
using ProtoBuf;
using System;

namespace Examples
{
    [DataContract]
    public class NonGenericBasic {
        [DataMember(Order=1)]
        public int Value {get;set;}
    }
    [TestFixture]
    public class NonGeneric
    {
        [Test]
        public void TestDeepClone()
        {
            NonGenericBasic ngb = new NonGenericBasic { Value = 123 },
                clone = (NonGenericBasic) Serializer.NonGeneric.DeepClone(ngb);

            Assert.AreNotSame(ngb, clone);
            Assert.AreEqual(ngb.Value, clone.Value);
        }

        [Test]
        public void TestManualCloneViaSerializeDeserialize()
        {
            NonGenericBasic ngb = new NonGenericBasic { Value = 123 }, clone;
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.NonGeneric.Serialize(ms, ngb);
                ms.Position = 0;
                clone = (NonGenericBasic)Serializer.NonGeneric.Deserialize(
                    ngb.GetType(), ms);
            }
            Assert.AreNotSame(ngb, clone);
            Assert.AreEqual(ngb.Value, clone.Value);
        }


    }


}
