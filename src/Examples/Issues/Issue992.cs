using System;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Issues
{
    public class Issue992
    {
        public Issue992(ITestOutputHelper log)
            => _log = log;

        private static readonly byte[] s_SharedPayload = new byte[] { 0, 1, 2, 3, 4 };
        private readonly ITestOutputHelper _log;

        [Fact]
        public void TestMemory() => Test(new Memory<byte>(s_SharedPayload, 1, 3), x => x.Span);

        [Fact]
        public void TestReadOnlyMemory() => Test(new ReadOnlyMemory<byte>(s_SharedPayload, 1, 3), x => x.Span);

        [Fact]
        public void TestArraySegment() => Test(new ArraySegment<byte>(s_SharedPayload, 1, 3), x => x.AsSpan());

        delegate ReadOnlySpan<byte> SpanAccessor<T>(T value);
        void Test<T>(T payload, SpanAccessor<T> spanAccessor)
        {
            var obj = new HazMemory<T> { Payload = payload };
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, obj);
            Assert.True(ms.TryGetBuffer(out var segment));
            var hex = BitConverter.ToString(segment.Array, segment.Offset, segment.Count);
            _log?.WriteLine(hex);
            Assert.Equal("0A-03-01-02-03", hex);
            ms.Position = 0;
            var clone = Serializer.Deserialize<HazMemory<T>>(ms);
            Assert.True(spanAccessor(obj.Payload).SequenceEqual(spanAccessor(clone.Payload)));

            var proto = Serializer.GetProto<HazMemory<T>>();
            _log?.WriteLine(proto);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Issues;

message Test {
   bytes Payload = 1;
}
", proto, ignoreLineEndingDifferences: true);
        }


        [ProtoContract(Name ="Test")]
        public class HazMemory<T>
        {
            [ProtoMember(1)]
            public T Payload { get; set; }
        }
    }


}
