using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class SO7654066
    {
        [ProtoContract(IgnoreListHandling = true)]
        public class MyClass : IEnumerable<int>
        {
            [ProtoMember(1, IsPacked = true)]
            public int[] data { get; set; }

            // Comment out this indexed property to prevent the crash
            public int this[int i] { get { return data[i]; } set { data[i] = value; } }

            public IEnumerator<int> GetEnumerator() { foreach (var x in data) yield return x; }
            IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }      
        }
        [Test]
        public void Execute()
        {
            Serializer.PrepareSerializer<MyClass>();
            var obj = new MyClass {data = new[] {1, 2, 3}};
            var clone = Serializer.DeepClone(obj);
            Assert.AreEqual(3, clone.data.Length);
            Assert.AreEqual(1, clone.data[0]);
            Assert.AreEqual(2, clone.data[1]);
            Assert.AreEqual(3, clone.data[2]);
        }

    }
}
