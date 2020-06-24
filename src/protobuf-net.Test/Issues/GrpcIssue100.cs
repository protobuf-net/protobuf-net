using ProtoBuf.Meta;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Issues
{
    // context: https://github.com/protobuf-net/protobuf-net.Grpc/issues/100
    public class GrpcIssue100
    {
        public GrpcIssue100(ITestOutputHelper log) => _log = log;
        private readonly ITestOutputHelper _log;
        private void Log(string message) => _log?.WriteLine(message);

        private static TestObject GetTestInstance() => new TestObject
        {
            Test = new TestThingy { SomeText = "abc", SomeText2 = "def" }
        };

        [Fact]
        public void MeasuredSerialize()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            MeasuredSerializeImpl(model);

            model.CompileInPlace();
            MeasuredSerializeImpl(model);

            MeasuredSerializeImpl(model.Compile());
        }
        private void MeasuredSerializeImpl(TypeModel model)
        {
            var obj = GetTestInstance();
            var ms = new MemoryStream();
            model.Serialize(ms, obj); // regular serialize
            Assert.Equal(14, ms.Length);
            var expected = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Log(expected);
            Assert.Equal("0A-0C-0A-05-0A-03-64-65-66-12-03-61-62-63", expected);

            /*
Field #1: 0A String Length = 12, Hex = 0C, UTF8 = "  defabc"
As sub-object :
  Field #1: 0A String Length = 5, Hex = 0C, UTF8 = " def"
  As sub-object :
    Field #1: 0A String Length = 3, Hex = 0C, UTF8 = "def"
  Field #2: 65 String Length = 3, Hex = 66, UTF8 = "abc"
             */


            // now try measured
            if ((object)RuntimeTypeModel.Default is IMeasuredProtoOutput<IBufferWriter<byte>> writer)
            {
                using var measured = writer.Measure(obj);
                Assert.Equal(ms.Length, measured.Length);

#if !NET462

                int hitsBefore = measured.GetLengthHits(out int missesBefore);

                var abw = new ArrayBufferWriter<byte>();
                writer.Serialize(measured, abw);
                Assert.Equal(ms.Length, abw.WrittenCount);

                var mem = abw.WrittenMemory;
                Assert.True(MemoryMarshal.TryGetArray(mem, out var segment));
                var actual = BitConverter.ToString(segment.Array, segment.Offset, segment.Count);
                Log(actual);
                Assert.Equal(expected, actual);

                int hitsAfter = measured.GetLengthHits(out int missesAfter);

                Log($"hits: {hitsAfter - hitsBefore}, misses: {missesAfter - missesBefore}");
                Assert.True(hitsAfter > hitsBefore, "got hits");
                Assert.Equal(0, missesBefore - missesBefore);
#endif
            }
        }

        [ProtoContract]
        public class TestObject
        {
            [ProtoMember(1)]
            public TestThingy Test { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(1, typeof(TestThingy))]
        public abstract class TestBase
        {
            [ProtoMember(2)]
            public string SomeText { get; set; }

            public abstract string SomeText2 { get; set; }
        }

        [ProtoContract]
        public class TestThingy : TestBase
        {
            [ProtoMember(1)]
            public override string SomeText2 { get; set; }
        }
    }
}
