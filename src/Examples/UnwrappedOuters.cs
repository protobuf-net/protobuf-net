using System;
using System.Linq;
using Xunit;
using ProtoBuf;
using System.IO;

namespace Examples
{
    
    public class UnwrappedOuters
    {
        [Fact]
        public void TestNakedByteArray()
        {
            Random rand = new Random(12345);
            byte[] data = new byte[100], clone;
            rand.NextBytes(data);
            using(var ms = new MemoryStream())
            {
                Serializer.Serialize<byte[]>(ms, data);
                ms.Position = 0;
                clone = Serializer.Deserialize<byte[]>(ms);
            }
            Assert.True(data.SequenceEqual(clone));
        }

        [Fact]
        public void TestNakedString()
        {
            Random rand = new Random(12345);
            byte[] data = new byte[100], clone;
            rand.NextBytes(data);
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize<string>(ms, Convert.ToBase64String(data));
                ms.Position = 0;
                clone = Convert.FromBase64String(Serializer.Deserialize<string>(ms));
            }
            
            Assert.True(data.SequenceEqual(clone));
        }
    }
}
