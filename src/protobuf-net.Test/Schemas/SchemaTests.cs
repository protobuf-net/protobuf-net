using Google.Protobuf.Reflection;
using Newtonsoft.Json;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Schemas
{
    public class SchemaTests
    {
        private ITestOutputHelper _output;
        public SchemaTests(ITestOutputHelper output) => _output = output;
        [Theory]
        [InlineData(@"Schemas\descriptor.proto")]        
        public void CanParse(string path)
        {
            FileDescriptorProto schema;
            using (var proto = File.OpenText(path))
            {
                schema = FileDescriptorProto.Parse(proto, out var errors);
            }
            foreach (var msg in schema.MessageTypes)
            {
                WriteMessage(msg, 0);
            }
        }

        [Fact]
        public void CanDeserializeCompiledSchema()
        {
            // to regenerate bin:
            // protoc --descriptor_set_out=descriptor.bin descriptor.proto
            // note that protoc is on maven: http://repo1.maven.org/maven2/com/google/protobuf/protoc/

            using (var file = File.OpenRead(@"Schemas\descriptor.bin"))
            {
                var obj = Serializer.Deserialize<FileDescriptorSet>(file);
                var json = JsonConvert.SerializeObject(obj);
                _output.WriteLine(json);
            }
        }

        [Theory]
        [InlineData(@"Schemas\descriptor.proto", @"Schemas\descriptor_expected.cs", @"Schemas\descriptor_actual.cs")]
        public void CanGenerate(string schemaPath, string expectedPath, string actualPath)
        {
            FileDescriptorProto schema;
            using (var proto = File.OpenText(schemaPath))
            {
                schema = FileDescriptorProto.Parse(proto, out var errors);
            }
            string code;
            using (var sw = new StringWriter())
            {
                schema.GenerateCSharp(sw);
                code = sw.ToString();
            }
            File.WriteAllText(actualPath, code);
            _output.WriteLine(actualPath);
            _output.WriteLine(Directory.GetCurrentDirectory());

            if (File.Exists(expectedPath))
            {
                Assert.Equal(File.ReadAllText(expectedPath), code);
            }
        }
        private string Indent(int count) => new string(' ', count);
        private void WriteMessage(DescriptorProto msg, int indent)
        {
            _output.WriteLine($"{Indent(indent++)}{msg}");
            foreach (var field in msg.Fields)
            {
                _output.WriteLine($"{Indent(indent)}{field.Number}: {field.TypeName} ({field.type})");
            }
            foreach (var res in msg.ReservedRanges)
            {
                _output.WriteLine($"{Indent(indent)}-{res.Start}-{res.End}");
            }
            foreach (var res in msg.ReservedNames)
            {               
                _output.WriteLine($"{Indent(indent)}-{res}");
            }
            foreach (var subMsg in msg.NestedTypes)
            {
                WriteMessage(subMsg, indent);
            }
        }
    }
}
