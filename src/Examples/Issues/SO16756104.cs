using NUnit.Framework;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Examples.Issues
{
    [TestFixture]
    public class SO16756104
    {
        [Test]
        public void TestNullableDoubleList()
        {
            Program.ExpectFailure<NullReferenceException>(() =>
            {
                var list = new List<double?> { 1, null, 2 };
                Serializer.DeepClone(list);
            });
        }

        [Test]
        public void TestNullableInt32List()
        {
            Program.ExpectFailure<NullReferenceException>(() =>
            {
                var list = new List<int?> { 1, null, 2 };
                Serializer.DeepClone(list);
            });
        }

        [Test]
        public void TestNullableStringList()
        {
            Program.ExpectFailure<NullReferenceException>(() =>
            {
                var list = new List<string> { "abc", null, "def" };
                Serializer.DeepClone(list);
            });
        }
    }
}
