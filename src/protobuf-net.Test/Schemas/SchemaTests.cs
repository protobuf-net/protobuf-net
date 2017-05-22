using Google.Protobuf.Reflection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
            var set = new FileDescriptorSet();
            set.Add(path);
            var schema = set.Files.Single();
            foreach (var msg in schema.MessageTypes)
            {
                WriteMessage(msg, 0);
            }
            var json = JsonConvert.SerializeObject(set, Formatting.Indented);
            
            var outPath = $"{Me()}.json";
            File.WriteAllText(outPath, json);
            _output.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), outPath));

        }

        [Theory]
        [InlineData(@"Schemas\descriptor.proto", @"Schemas\descriptor.bin")]
        public void ParsedDataSerializesIdentically(string schemaPath, string expectedBinaryPath)
        {
            var set = new FileDescriptorSet();
            set.Add(schemaPath);
            // need to tweak the name to get equivalence
            set.Files.Single().Name = Path.GetFileName(schemaPath);
            string expected = BitConverter.ToString(File.ReadAllBytes(expectedBinaryPath)), actual;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, set);
                actual = BitConverter.ToString(ms.ToArray());
            }
            // // cheat to debug
            // actual = "0A-D0" + actual.Substring(5);
            _output.WriteLine(expected);
            _output.WriteLine(actual);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanDeserializeCompiledSchema()
        {
            // to regenerate bin:
            // protoc --descriptor_set_out=descriptor.bin descriptor.proto
            // note that protoc is on maven: http://repo1.maven.org/maven2/com/google/protobuf/protoc/

            using (var file = File.OpenRead(@"Schemas\descriptor.bin"))
            {
                var set = Serializer.Deserialize<FileDescriptorSet>(file);
                var json = JsonConvert.SerializeObject(set, Formatting.Indented);

                var outPath = $"{Me()}.json";
                File.WriteAllText(outPath, json);
                _output.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), outPath));
            }
        }
        private static string Me([CallerMemberName] string caller = null) => caller;

        [Theory]
        [InlineData(@"Schemas\descriptor.proto", @"Schemas\descriptor_expected.cs", @"Schemas\descriptor_actual.cs")]
        public void CanGenerate(string schemaPath, string expectedPath, string actualPath)
        {
            var set = new FileDescriptorSet();
            set.Add(schemaPath);
            var schema = set.Files.Single();
            string code;
            using (var sw = new StringWriter())
            {
                schema.GenerateCSharp(sw);
                code = sw.ToString();
            }
            File.WriteAllText(actualPath, code);
            _output.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), actualPath));

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
