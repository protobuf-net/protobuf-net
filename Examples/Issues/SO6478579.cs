using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using NUnit.Framework;
using ProtoBuf;

namespace test
{
    [DataContract]
    public class Coordinates
    {
        [DataContract]
        public struct CoOrd
        {
            public CoOrd(int x, int y, int z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
            [ProtoMember(1, DataFormat = DataFormat.FixedSize)]
            int x;
            [ProtoMember(2, DataFormat = DataFormat.FixedSize)]
            int y;
            [ProtoMember(3, DataFormat = DataFormat.FixedSize)]
            int z;
        }
        [DataMember(Order = 1)]
        public List<CoOrd> Coords = new List<CoOrd>();

        public void SetupTestArray()
        {
            Random r = new Random(123456);
            List<CoOrd> coordinates = new List<CoOrd>();
            for (int i = 0; i < 1000000; i++)
            {
                Coords.Add(new CoOrd(r.Next(10000), r.Next(10000), r.Next(10000)));
            }
        }
    }

    [TestFixture]
    public class SO6478579
    {
        
        [Test]
        public void TestMethod()
        {
            Coordinates c = new Coordinates();
            c.SetupTestArray();

            // Serialize to memory stream
            MemoryStream mStream = new MemoryStream();
            Serializer.Serialize(mStream, c);

            Assert.AreEqual(27, mStream.Length); 
            Console.WriteLine("Length : {0}", mStream.Length);
        }
    }
}

