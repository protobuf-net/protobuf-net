using System.IO;
using System.Runtime.Serialization;
using Xunit;
using ProtoBuf;
using System;

namespace Examples
{
    [DataContract]
    public class NonGenericBasic {
        [DataMember(Order=1)]
        public int Value {get;set;}
    }
    
    public class NonGeneric
    {
        [Fact]
        public void TestDeepClone()
        {
            NonGenericBasic ngb = new NonGenericBasic { Value = 123 },
                clone = (NonGenericBasic) Serializer.NonGeneric.DeepClone(ngb);

            Assert.NotSame(ngb, clone);
            Assert.Equal(ngb.Value, clone.Value);
        }

        [Fact]
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
            Assert.NotSame(ngb, clone);
            Assert.Equal(ngb.Value, clone.Value);
        }


    }


}
