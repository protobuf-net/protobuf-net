using System.IO;
using Xunit;
using ProtoBuf;

namespace Examples.Issues
{
    
    public class Issue264
    {
        [Fact]
        public void TestNakedDecimals()
        {
            Test(123.45M);
            Test(0M);
            Test(decimal.MinValue);
            Test(decimal.MaxValue);
        }
        [Fact]
        public void TestNakedDoubles()
        {
            Test(123.45D);
            Test(0D);
            Test(double.MinValue);
            Test(double.MaxValue);
        }
        [Fact]
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
            Assert.Equal(value, result); //, value.ToString() + ":DeepClone");
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize<T>(ms, value);
                ms.Position = 0;
                result = Serializer.Deserialize<T>(ms);
                Assert.Equal(value, result); //, value.ToString() + ":Serialize/Deserialize");
            }
        }

    }
}
