using NUnit.Framework;
using ProtoBuf;
using System.Linq;
using System;
namespace Examples.Issues
{
    [TestFixture]
    public class Issue170
    {

        [Test]
        public void ArrayWithoutNullContentShouldClone()
        {
            var arr = new[] { "aaa","bbb" };
            Assert.IsTrue(Serializer.DeepClone(arr).SequenceEqual(arr));
        }
        [Test, ExpectedException(typeof(NullReferenceException))]
        public void ArrayWithNullContentShouldThrow()
        {
            var arr = new[] { "aaa", null, "bbb" };
            var arr2 = Serializer.DeepClone(arr);
        }
    }
}
