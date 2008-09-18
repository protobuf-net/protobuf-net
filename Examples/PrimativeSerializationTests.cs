using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;

namespace Examples
{
    [TestFixture]
    public class PrimativeSerializationTests
    {
        [Test]
        public void TestInt32()
        {
            Random rand = new Random(123456);
            for(int i = 0 ; i < 10000 ; i++) {
                int next = rand.Next(int.MinValue, int.MaxValue);
                Assert.AreEqual(next, Serializer.DeepClone(next), next.ToString());
            }
            
        }

        [Test]
        public void TestString()
        {
            Assert.AreEqual("foo", Serializer.DeepClone("foo"), "foo");
            Random rand = new Random(123456);
            for (int i = 0; i < 10000; i++)
            {
                string next = rand.Next(int.MinValue, int.MaxValue).ToString();
                Assert.AreEqual(next, Serializer.DeepClone(next), next.ToString());
            }
        }

        [Test]
        public void TestDouble()
        {
            Random rand = new Random(123456);
            for (int i = 0; i < 10000; i++)
            {
                double next = rand.NextDouble();
                Assert.AreEqual(next, Serializer.DeepClone(next), next.ToString());
            }
        }
    }
}
