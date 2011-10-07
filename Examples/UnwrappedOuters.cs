using System;
using System.Linq;
using NUnit.Framework;
using ProtoBuf;
using System.IO;

namespace Examples
{
    [TestFixture]
    public class UnwrappedOuters
    {
        [Test]
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
            Assert.IsTrue(data.SequenceEqual(clone));
        }

        [Test]
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
            
            Assert.IsTrue(data.SequenceEqual(clone));
        }
    }
}
