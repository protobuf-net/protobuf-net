using System;
using System.Diagnostics;
using System.IO;
using ProtoBuf;
using System.Runtime.Serialization;
using NUnit.Framework;

namespace Examples.SimpleStream
{
    [DataContract]
    public sealed class PerfTest
    {
        [DataMember(Order = 1)]
        public int Foo { get; set; }

        [DataMember(Order = 2)]
        public string Bar { get; set; }

        [DataMember(Order = 3)]
        public float Blip { get; set; }

        [DataMember(Order = 4)]
        public double Blop { get; set; }

    }
    [TestFixture]
    public class SimplePerfTests
    {
        [Test]
        public void RunSimplePerfTests()
        {
            PerfTest obj = new PerfTest
            {
                Foo = 12,  // 1: 8, 12
                Bar = "bar", // 2: 18, 3, 98, 97, 114
                Blip = 123.45F, // 3: 29, 102, 230, 246, 66
                Blop = 123.4567 // 4: 33, 83, 5, 163, 146, 58, 221, 94, 64
            };

            PerfTest clone;
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj);
                byte[] raw = ms.ToArray();
                ms.Position = 0;
                clone = Serializer.Deserialize<PerfTest>(ms);
                Assert.AreEqual(obj.Foo, clone.Foo, "Foo");
                Assert.AreEqual(obj.Bar, clone.Bar, "Bar");
                Assert.AreEqual(obj.Blop, clone.Blop, "Blop");
                Assert.AreEqual(obj.Blip, clone.Blip, "Blip");
            }
            

            const int LOOP = 500000;
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < LOOP; i++)
            {
                Serializer.Serialize(Stream.Null, obj);
            }
            Console.WriteLine("\tSerialized x{0} in {1}ms", 500000, watch.ElapsedMilliseconds);
        }
    }
}
