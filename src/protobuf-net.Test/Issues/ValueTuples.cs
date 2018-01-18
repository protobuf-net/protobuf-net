using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Issues
{

    public class ValueTuples
    {
        [Fact]
        public void CanRoundTripValueTuples()
        {
            var foo = GetNamedTupleData();

            var bar = Serializer.DeepClone(foo);
            Assert.Equal(123, bar.a);
            Assert.Equal("def", bar.b);
            Assert.True(bar.c.d);
            Assert.Equal("abc,def", string.Join(",", bar.c.e));

        }

        [Fact]
        public void CheckLayoutEquivalence()
        {
            string hex;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, GetNamedTupleData());
                hex = BitConverter.ToString(ms.ToArray()); // GetBuffer() not available on all TFMs
            }
            Assert.Equal(@"08-7B-12-03-64-65-66-1A-0C-08-01-12-03-61-62-63-12-03-64-65-66", hex);
            // 08-7B: field 1: 123
            // 12-03: field 2, 3 bytes
            //   64-65-66: "abc"
            // 1A-0C: field 3, 12 bytes
            //   08-01: field 1: 1
            //   12-03: field 2, 3 bytes
            //     61-62-63: "abc"
            //   12-03: field 2, 3 bytes
            //     64-65-66: "def"
        }

        private (int a, string b, (bool d, string[] e) c) GetNamedTupleData()
            => (a: 123, b: "def", c: (d: true, e: new[] { "abc", "def" }));
    }
}
