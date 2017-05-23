using Google.Protobuf.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        const string SchemaPath = "Schemas";
        public static IEnumerable<object[]> GetSchemas()
            => from file in Directory.GetFiles(SchemaPath, "*.proto")
               select new object[] { file };

        [Theory]
        [MemberData(nameof(GetSchemas))]
        public void CompareProtoToParser(string path)
        {
            _output.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), SchemaPath));
            Assert.True(File.Exists(path));
            var binPath = Path.ChangeExtension(path, "protoc.bin");
            int exitCode;
            using (var proc = Process.Start("protoc", $"--descriptor_set_out={binPath} {path}"))
            {
                proc.WaitForExit();
                exitCode = proc.ExitCode;
            }
            var protocHex = BitConverter.ToString(File.ReadAllBytes(binPath));
            FileDescriptorSet set;
            string json, jsonPath;
            using (var file = File.OpenRead(binPath))
            {
                set = Serializer.Deserialize<FileDescriptorSet>(file);
                json = JsonConvert.SerializeObject(set, Formatting.Indented);
                jsonPath = Path.ChangeExtension(path, "protoc.json");
                File.WriteAllText(jsonPath, json);
            }

            set = new FileDescriptorSet();
            set.Add(path);
            json = JsonConvert.SerializeObject(set, Formatting.Indented);
            jsonPath = Path.ChangeExtension(path, "parser.json");
            File.WriteAllText(jsonPath, json);

            binPath = Path.ChangeExtension(path, "parser.bin");
            using (var file = File.Create(binPath))
            {
                Serializer.Serialize(file, set);
            }

            if (exitCode == 0)
            {
                Assert.Equal(0, set.Errors.Count);
            }
            else
            {
                Assert.NotEqual(0, set.Errors.Count);
            }
            var parserHex = BitConverter.ToString(File.ReadAllBytes(binPath));
            
            Assert.Equal(protocHex, parserHex);
        }

        public SchemaTests(ITestOutputHelper output) => _output = output;

    }
}
