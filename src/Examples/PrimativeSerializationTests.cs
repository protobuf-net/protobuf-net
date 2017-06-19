using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;

namespace Examples
{
    
    public class PrimativeSerializationTests
    {
        [Fact]
        public void TestInt32()
        {
            Random rand = new Random(123456);
            for(int i = 0 ; i < 10000 ; i++) {
                int next = rand.Next(int.MinValue, int.MaxValue);
                Assert.Equal(next, Serializer.DeepClone(next)); //, next.ToString());
            }
            
        }

        [Fact]
        public void TestString()
        {
            Assert.Equal("foo", Serializer.DeepClone("foo")); //, "foo");
            Random rand = new Random(123456);
            for (int i = 0; i < 10000; i++)
            {
                string next = rand.Next(int.MinValue, int.MaxValue).ToString();
                Assert.Equal(next, Serializer.DeepClone(next)); //, next.ToString());
            }
        }

        [Fact]
        public void TestDouble()
        {
            Random rand = new Random(123456);
            for (int i = 0; i < 10000; i++)
            {
                double next = rand.NextDouble();
                Assert.Equal(next, Serializer.DeepClone(next)); //, next.ToString());
            }
        }
    }
}
