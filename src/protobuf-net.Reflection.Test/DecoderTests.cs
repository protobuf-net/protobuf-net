using Google.Protobuf.Reflection;
using Newtonsoft.Json;
using ProtoBuf.Internal;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Reflection.Test
{
    public class DecoderTests
    {
        private readonly ITestOutputHelper _log;
        private readonly TextWriter _output;
        public DecoderTests(ITestOutputHelper log)
            => _output = new TestOutputShim(_log = log);

        sealed class TestOutputShim : TextWriter // collect output *and* write to ITestOutputHelper
        {
            private readonly ITestOutputHelper _log;
            private readonly StringBuilder _total = new StringBuilder();
            public TestOutputShim(ITestOutputHelper log) => _log = log;
            public override Encoding Encoding => Encoding.Unicode;

            private readonly StringBuilder _backlog = new StringBuilder();
            public override void Write(string value)
            {
                _backlog.Append(value);
                _total.Append(value);
            }

            public override void WriteLine(string value)
            {
                if (_backlog.Length > 0)
                {
                    _backlog.Append(value);
                    _log.WriteLine(_backlog.ToString());
                    _backlog.Clear();
                }
                else
                {
                    _log.WriteLine(value);
                }
                _total.AppendLine(value);
            }
            

            public override void Flush()
            {
                if (_backlog.Length > 0)
                {
                    _log.WriteLine(_backlog.ToString());
                    _backlog.Clear();
                }
            }
            public override string ToString() => _total.ToString();
        }

        private static FileDescriptorSet GetDummySchema()
        {
            var schemaSet = new FileDescriptorSet();
            // inspired from the encoding document
            schemaSet.Add("dummy.proto", source: new StringReader(@"
message Test {
  optional int32 a = 1;
  repeated string b = 2;
  optional Test c = 3;
  optional Blap d = 4;
  optional Blap e = 5;
}
enum Blap {
   BLAB_X = 0;
   BLAB_Y = 1;
}"));
            schemaSet.Process();
            return schemaSet;
        }

        private static MemoryStream GetDummyPayload()
            => new MemoryStream(new byte[] {
                0x08, 0x96, 0x01, // integer
                0x12, 0x07, 0x74, 0x65, 0x73, 0x74, 0x69, 0x6e, 0x67, // string
                0x1a, 0x03, 0x08, 0x96, 0x01, // sub-message
                0x20, 0x01, // enum mapped to defined value
                0x28, 0x05, // enum without defined value
            });

        [Fact]
        public void BasicDecodeUsage()
        {
            var schemaSet = GetDummySchema();
            using var ms = GetDummyPayload();
            using var visitor = new TextDecodeVisitor(_output);
            visitor.Visit(ms, schemaSet.Files[0], "Test");
            var result = _output.ToString();
            Assert.Equal(@"1: a=150 (TypeInt32)
2: b=[ (TypeString)
 #0=testing
] // b, count: 1
3: c={
 1: a=150 (TypeInt32)
} // c
4: d=BLAB_Y (TypeEnum)
5: e=5 (TypeEnum)
", result, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void ObjectDecodeUsage()
        {
            var schemaSet = GetDummySchema();
            using var ms = GetDummyPayload();
            using var visitor = new ObjectDecodeVisitor();
            dynamic obj = visitor.Visit(ms, schemaSet.Files[0], "Test");

            // test dynamic access
            Assert.Equal(150, (int)obj.a);
            Assert.Equal("testing", ((List<string>)obj.b).Single());
            Assert.Equal(150, (int)obj.c.a);
            Assert.Equal(1, (int)obj.d);
            Assert.Equal(5, (int)obj.e);

            // and via JSON
            string json = JsonConvert.SerializeObject((object)obj);
            _log.WriteLine(json);
            Assert.Equal(@"{""a"":150,""b"":[""testing""],""c"":{""a"":150},""d"":1,""e"":5}", json, ignoreLineEndingDifferences: true);
        }
    }
}
