using Google.Protobuf.Reflection;
using ProtoBuf.Internal;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Reflection.Test
{
    public class DecoderTests
    {
        TextWriter _output;
        public DecoderTests(ITestOutputHelper log)
            => _output = new TestOutputShim(log);

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

        [Fact]
        public void BasicDecodeUsage()
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
            using var ms = new MemoryStream(new byte[] {
                0x08, 0x96, 0x01, // integer
                0x12, 0x07, 0x74, 0x65, 0x73, 0x74, 0x69, 0x6e, 0x67, // string
                0x1a, 0x03, 0x08, 0x96, 0x01, // sub-message
                0x20, 0x01, // enum mapped to defined value
                0x28, 0x05, // enum without defined value
            });
            new TextDecodeVisitor(_output).Visit(ms, schemaSet.Files[0], "Test");
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
    }
}
