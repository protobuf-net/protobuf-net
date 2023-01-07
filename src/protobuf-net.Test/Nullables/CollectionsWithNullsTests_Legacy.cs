using ProtoBuf.Test.Nullables.Abstractions;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.Nullables
{
    public class CollectionsWithNullsTests_Legacy : CollectionsWithNullsTestsBase
    {
        public CollectionsWithNullsTests_Legacy(ITestOutputHelper log) 
            : base(log)
        {
        }

        [Fact]
        public void ExistingListsBehaviour()
        {
            using var ms = new MemoryStream(new byte[] { 0x08, 0x00, 0x08, 0x01, 0x08, 0x02 });
            var clone = Serializer.Deserialize<Foo>(ms);
            if (!ms.TryGetBuffer(out var buffer)) buffer = new ArraySegment<byte>(ms.ToArray());
            Assert.Equal("08-00-08-01-08-02", BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count));
        }

        [DataContract]
        public class Foo
        {
            [DataMember(Order = 1)]
            public List<int?> Items { get; } = new List<int?>();
        }
    }
}
