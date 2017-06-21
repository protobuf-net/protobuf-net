using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;

namespace Examples.Issues
{
    
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
        [Fact]
        public void Execute()
        {
            Serializer.PrepareSerializer<MyClass>();
            var obj = new MyClass {data = new[] {1, 2, 3}};
            var clone = Serializer.DeepClone(obj);
            Assert.Equal(3, clone.data.Length);
            Assert.Equal(1, clone.data[0]);
            Assert.Equal(2, clone.data[1]);
            Assert.Equal(3, clone.data[2]);
        }

    }
}
