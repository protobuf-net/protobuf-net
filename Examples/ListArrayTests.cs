using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Examples.Ppt;
using ProtoBuf;

namespace Examples
{
    [TestFixture]
    public class ListArrayTests
    {
        [Test]
        public void TestListEmpty()
        {
            List<Test1> list = new List<Test1>();
            List<Test1> clone = Serializer.DeepClone(list);

            Assert.AreNotSame(list, clone, "Same");
            Assert.AreEqual(list.Count, clone.Count, "Count");
        }
    }
}
