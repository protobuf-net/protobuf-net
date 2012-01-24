using System.IO;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue264
    {
        [Test]
        public void TestNakedDecimals()
        {
            Test(123.45M);
            Test(0M);
            Test(decimal.MinValue);
            Test(decimal.MaxValue);
        }
        [Test]
        public void TestNakedDoubles()
        {
            Test(123.45D);
            Test(0D);
            Test(double.MinValue);
            Test(double.MaxValue);
        }
        [Test]
        public void TestNakedFloats()
        {
            Test(123.45F);
            Test(0F);
            Test(float.MinValue);
            Test(float.MaxValue);
        }
        static void Test<T>(T value)
        {
            T result = Serializer.DeepClone<T>(value);
            Assert.AreEqual(value, result, value.ToString() + ":DeepClone");
            using(var ms = new MemoryStream())
            {
                Serializer.Serialize<T>(ms, value);
                ms.Position = 0;
                result = Serializer.Deserialize<T>(ms);
                Assert.AreEqual(value, result, value.ToString() + ":Serialize/Deserialize");
            }
        }

    }
}
