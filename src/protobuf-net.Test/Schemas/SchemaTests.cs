using Google.Protobuf.Reflection;
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
                schema = FileDescriptorProto.Parse(proto);
            }
            foreach (var msg in schema.message_type)
            {
                WriteMessage(msg, 0);
            }
        }

        [Theory]
        [InlineData(@"Schemas\descriptor.proto", @"Schemas\descriptor_expected.cs", @"Schemas\descriptor_actual.cs")]
        public void CanGenerate(string schemaPath, string expectedPath, string actualPath)
        {
            FileDescriptorProto schema;
            using (var proto = File.OpenText(schemaPath))
            {
                schema = FileDescriptorProto.Parse(proto);
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
            foreach (var field in msg.field)
            {
                _output.WriteLine($"{Indent(indent)}{field.number}: {field.type_name} ({field.type})");
            }
            foreach (var res in msg.reserved_range)
            {
                _output.WriteLine($"{Indent(indent)}-{res.start}-{res.end}");
            }
            foreach (var res in msg.reserved_name)
            {               
                _output.WriteLine($"{Indent(indent)}-{res}");
            }
            foreach (var subMsg in msg.nested_type)
            {
                WriteMessage(subMsg, indent);
            }
        }
    }
}
