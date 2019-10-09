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
#pragma warning disable CS0618
            NonGenericBasic ngb = new NonGenericBasic { Value = 123 },
                clone = (NonGenericBasic) Serializer.NonGeneric.DeepClone(ngb);
#pragma warning restore CS0618

            Assert.NotSame(ngb, clone);
            Assert.Equal(ngb.Value, clone.Value);
        }

        [Fact]
        public void TestManualCloneViaSerializeDeserialize()
        {
            NonGenericBasic ngb = new NonGenericBasic { Value = 123 }, clone;
            using (MemoryStream ms = new MemoryStream())
            {
#pragma warning disable CS0618
                Serializer.NonGeneric.Serialize(ms, ngb);
                ms.Position = 0;
                clone = (NonGenericBasic)Serializer.NonGeneric.Deserialize(
                    ngb.GetType(), ms);
#pragma warning restore CS0618
            }
            Assert.NotSame(ngb, clone);
            Assert.Equal(ngb.Value, clone.Value);
        }


    }


}
