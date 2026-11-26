using Google.Protobuf.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ProtoBuf.Reflection.Test
{
    public class PreserveUnknownFieldsTests
    {
        private readonly ITestOutputHelper _log;

        public PreserveUnknownFieldsTests(ITestOutputHelper log)
            => _log = log;

        const string BasicSchema = @"
syntax = ""proto3"";
message Foo {
  int32 i = 1;
}";

        const string Proto2SchemaWithExtensionRanges = @"
syntax = ""proto2"";
message HasRanges {
  optional int32 i = 1;
  extensions 100 to 200;
}
message NoRanges {
  optional int32 i = 1;
}
extend HasRanges {
  optional int32 extra = 100;
}";

        private string Generate(CodeGenerator generator, string schema, string preserveUnknownFields = null)
        {
            var set = new FileDescriptorSet();
            Assert.True(set.Add("my.proto", source: new StringReader(schema)));
            set.Process();
            foreach (var error in set.GetErrors())
            {
                _log?.WriteLine(error.ToString(true));
            }
            Assert.Empty(set.GetErrors());
            Dictionary<string, string> options = null;
            if (preserveUnknownFields is not null)
            {
                options = new Dictionary<string, string>
                {
                    { "preserveunknownfields", preserveUnknownFields },
                };
            }
            var code = generator.Generate(set, options: options).Single().Text;
            _log?.WriteLine(code);
            return code;
        }

        [Fact]
        public void CSharpPreservesUnknownFieldsByDefault()
        {
            var code = Generate(CSharpCodeGenerator.Default, BasicSchema);
            Assert.Contains(" : global::ProtoBuf.IExtensible", code);
            Assert.Contains("__pbn__extensionData", code);
        }

        [Fact]
        public void CSharpCanOptOutOfUnknownFields()
        {
            var code = Generate(CSharpCodeGenerator.Default, BasicSchema, "false");
            Assert.DoesNotContain("IExtensible", code);
            Assert.DoesNotContain("__pbn__extensionData", code);
        }

        [Fact]
        public void VisualBasicPreservesUnknownFieldsByDefault()
        {
            var code = Generate(VBCodeGenerator.Default, BasicSchema);
            Assert.Contains("Implements Global.ProtoBuf.IExtensible", code);
            Assert.Contains("__pbn__extensionData", code);
        }

        [Fact]
        public void VisualBasicCanOptOutOfUnknownFields()
        {
            var code = Generate(VBCodeGenerator.Default, BasicSchema, "false");
            Assert.DoesNotContain("IExtensible", code);
            Assert.DoesNotContain("__pbn__extensionData", code);
        }

        [Fact]
        public void CSharpOptOutKeepsExtensibleWhenExtensionRangesAreDeclared()
        {
            // the generated extension accessors (Extensible.GetValue etc) require the
            // extendee to implement IExtensible, so the opt-out must not apply there
            var code = Generate(CSharpCodeGenerator.Default, Proto2SchemaWithExtensionRanges, "false");
            Assert.Contains("partial class HasRanges : global::ProtoBuf.IExtensible", code);
            Assert.Contains("GetExtra", code); // the accessor that needs it
            var noRanges = code.Substring(code.IndexOf("class NoRanges"));
            Assert.DoesNotContain("IExtensible", noRanges);
        }

        [Fact]
        public void VisualBasicOptOutKeepsExtensibleWhenExtensionRangesAreDeclared()
        {
            var code = Generate(VBCodeGenerator.Default, Proto2SchemaWithExtensionRanges, "false");
            Assert.Contains("Class HasRanges", code);
            var hasRanges = code.Substring(code.IndexOf("Class HasRanges"));
            var noRanges = hasRanges.Substring(hasRanges.IndexOf("Class NoRanges"));
            hasRanges = hasRanges.Substring(0, hasRanges.IndexOf("Class NoRanges"));
            Assert.Contains("Implements Global.ProtoBuf.IExtensible", hasRanges);
            Assert.DoesNotContain("IExtensible", noRanges);
        }
    }
}
