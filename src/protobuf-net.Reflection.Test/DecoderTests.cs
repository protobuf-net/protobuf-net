using Google.Protobuf.Reflection;
using Newtonsoft.Json;
using ProtoBuf.Internal;
using System;
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

        private FileDescriptorSet GetDummySchema()
        {
            var schemaSet = new FileDescriptorSet();
            // inspired from the encoding document
            schemaSet.Add("dummy.proto", source: new StringReader(@"
syntax=""proto3"";
message Test {
  optional int32 a = 1 [json_name=""ja""];
  repeated string b = 2 [json_name=""jb""];
  optional Test c = 3; // deliberately no json_name
  optional Blap d = 4 [json_name=""jd""];
  optional Blap e = 5; // deliberately no json_name
  repeated int32 f = 6 [packed=true, json_name=""jf""];
  int32 g = 7; // not presence-tracked; should apply default value (never specified)
  optional int32 h = 8; // presence-tracked; should *NOT* apply default value (never specified)
}
enum Blap {
   BLAB_X = 0;
   BLAB_Y = 1;
}"));
            schemaSet.Process();
            foreach (var error in schemaSet.GetErrors())
            {
                _log.WriteLine(error.Message);
            }
            return schemaSet;
        }

        private static MemoryStream GetDummyPayload(byte fCount = 0)
        {
            var ms = new MemoryStream();
            var buffer = new byte[] {
                0x08, 0x96, 0x01, // integer
                0x12, 0x07, 0x74, 0x65, 0x73, 0x74, 0x69, 0x6e, 0x67, // string
                0x1a, 0x03, 0x08, 0x96, 0x01, // sub-message
                0x20, 0x01, // enum mapped to defined value
                0x28, 0x05, // enum without defined value
            };
            ms.Write(buffer, 0, buffer.Length);
            if (fCount > 0)
            {
                // lazy; keep everything small so we can use single-byte logic
                if (fCount > 100) throw new ArgumentOutOfRangeException(nameof(fCount));
                ms.WriteByte(0x32); // field 6, length-prefixed
                ms.WriteByte(fCount); // single-byte, so: length==this value
                for (byte i = 0; i < fCount; i++)
                {
                    ms.WriteByte(i);
                }
            }
            ms.Position = 0;
            return ms;
        }

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
        public void BasicDecodeUsagePackedRepeated()
        {
            var schemaSet = GetDummySchema();
            using var ms = GetDummyPayload(fCount: 6);
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
6: f=[ (TypeInt32)
 #0=0
 #1=1
 #2=2
 #3=3
 #4=4
 #5=5
] // f, count: 6
", result, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void ObjectDecodeUsage()
        {
            var schemaSet = GetDummySchema();
            using var ms = GetDummyPayload();
            using var visitor = new ObjectDecodeVisitor();
            IDictionary<string,object> lookup = visitor.Visit(ms, schemaSet.Files[0], "Test");
            dynamic obj = lookup;
            // test via JSON
            string json = JsonConvert.SerializeObject((object)obj);
            _log.WriteLine(json);
            Assert.Equal(@"{""a"":150,""b"":[""testing""],""g"":0,""c"":{""a"":150},""d"":1,""e"":5}", json, ignoreLineEndingDifferences: true);

            // test dynamic access
            Assert.Equal(150, (int)obj.a);
            Assert.Equal("testing", ((List<string>)obj.b).Single());
            Assert.Equal(150, (int)obj.c.a);
            Assert.Equal(1, (int)obj.d);
            Assert.Equal(5, (int)obj.e);
            Assert.Equal(0, (int)obj.g);
            Assert.False(lookup.ContainsKey("h"));
        }


        [Fact]
        public void ObjectDecodeUsagePackedRepeated()
        {
            var schemaSet = GetDummySchema();
            using var ms = GetDummyPayload(fCount: 6);
            using var visitor = new ObjectDecodeVisitor();
            IDictionary<string, object> lookup = visitor.Visit(ms, schemaSet.Files[0], "Test");
            dynamic obj = lookup;

            // test via JSON
            string json = JsonConvert.SerializeObject((object)obj);
            _log.WriteLine(json);
            Assert.Equal(@"{""a"":150,""b"":[""testing""],""g"":0,""c"":{""a"":150},""d"":1,""e"":5,""f"":[0,1,2,3,4,5]}", json, ignoreLineEndingDifferences: true);

            // test dynamic access
            Assert.Equal(150, (int)obj.a);
            Assert.Equal("testing", ((List<string>)obj.b).Single());
            Assert.Equal(150, (int)obj.c.a);
            Assert.Equal(1, (int)obj.d);
            Assert.Equal(5, (int)obj.e);
            Assert.Equal("0,1,2,3,4,5", string.Join(",", (List<int>)obj.f));
            Assert.Equal(0, (int)obj.g);
            Assert.False(lookup.ContainsKey("h"));
        }

        [Fact]
        public void ObjectDecodeUsagePackedRepeatedJson()
        {
            var schemaSet = GetDummySchema();
            using var ms = GetDummyPayload(fCount: 6);
            using var visitor = ObjectDecodeVisitor.ForJson();
            IDictionary<string, object> lookup = visitor.Visit(ms, schemaSet.Files[0], "Test");
            dynamic obj = lookup;

            // test via JSON
            string json = JsonConvert.SerializeObject((object)obj);
            _log.WriteLine(json);
            Assert.Equal(@"{""ja"":150,""jb"":[""testing""],""g"":0,""c"":{""ja"":150},""jd"":""BLAB_Y"",""e"":5,""jf"":[0,1,2,3,4,5]}", json, ignoreLineEndingDifferences: true);

            // test dynamic access
            Assert.Equal(150, (int)obj.ja);
            Assert.Equal("testing", ((List<string>)obj.jb).Single());
            Assert.Equal(150, (int)obj.c.ja);
            Assert.Equal("BLAB_Y", (string)obj.jd);
            Assert.Equal(5, (int)obj.e);
            Assert.Equal("0,1,2,3,4,5", string.Join(",", (List<int>)obj.jf));
            Assert.Equal(0, (int)obj.g);
            Assert.False(lookup.ContainsKey("h"));
        }
    }
}
