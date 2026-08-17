using System.IO;
using System.Linq;
using Google.Protobuf.Reflection;
using ProtoBuf.Reflection;
using Xunit;

namespace ProtoBuf.Schemas
{
    public class EditionsCodeGenTests
    {
        // one member per editions feature with codegen impact
        const string Schema = @"
edition = ""2023"";

package editions.gen;

message Widget {
  int32 plain = 1;
  int32 implied = 2 [features.field_presence = IMPLICIT];
  int32 must = 3 [features.field_presence = LEGACY_REQUIRED];
  repeated int32 packed_default = 4;
  repeated int32 expanded = 5 [features.repeated_field_encoding = EXPANDED];
  Part part = 6;
  Part grouped = 7 [features.message_encoding = DELIMITED];
  Level level = 8;
  Mode mode = 9;
}

message Part {
  int32 value = 1;
}

enum Level {
  option features.enum_type = CLOSED;
  LEVEL_HIGH = 3;
  LEVEL_LOW = 4;
}

enum Mode {
  MODE_UNKNOWN = 0;
  MODE_ACTIVE = 1;
}
";

        private static string Generate(CodeGenerator generator)
        {
            var set = new FileDescriptorSet();
            Assert.True(set.Add("editions_gen.proto", true, new StringReader(Schema)));
            set.Process();
            Assert.Empty(set.GetErrors().Where(x => x.IsError));
            return generator.Generate(set).Single().Text;
        }

        [Fact]
        public void CSharpRespectsEditionsFeatures()
        {
            var code = Generate(CSharpCodeGenerator.Default);

            // explicit presence (the editions default) gets the conditional pattern
            Assert.Contains("public bool ShouldSerializePlain()", code);
            // implicit presence is a plain property
            Assert.Contains("public int Implied { get; set; }", code);
            Assert.DoesNotContain("ShouldSerializeImplied", code);
            // legacy-required maps to IsRequired, no conditional
            Assert.Contains(@"ProtoMember(3, Name = @""must"", IsRequired = true)", code);
            Assert.DoesNotContain("ShouldSerializeMust", code);
            // repeated: packed by default, expanded on request
            Assert.Contains(@"Name = @""packed_default"", IsPacked = true", code);
            Assert.Contains(@"ProtoMember(5, Name = @""expanded"")]", code);
            // message_encoding=DELIMITED is DataFormat.Group - the wire shape protobuf-net
            // has supported all along
            Assert.Contains(@"Name = @""grouped"", DataFormat = global::ProtoBuf.DataFormat.Group", code);
            Assert.Contains(@"ProtoMember(6, Name = @""part"")]", code); // and length-prefixed stays plain
            // a closed enum defaults to its first value; an open enum defaults to zero
            Assert.Contains("Level.LevelHigh", code);
            Assert.DoesNotContain("Mode.ModeUnknown", code);
        }

        [Fact]
        public void VisualBasicRespectsEditionsFeatures()
        {
            var code = Generate(VBCodeGenerator.Default);

            Assert.Contains(@"Name := ""must"", IsRequired := True", code);
            Assert.Contains(@"Name := ""packed_default"", IsPacked := True", code);
            Assert.Contains(@"Name := ""grouped"", DataFormat := Global.ProtoBuf.DataFormat.Group", code);
        }
    }
}
