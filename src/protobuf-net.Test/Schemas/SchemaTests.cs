using Google.Protobuf.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
               select new object[] { file.Replace('\\', '/') };

        [Theory]
        [MemberData(nameof(GetSchemas))]
        public void CompareProtoToParser(string path)
        {
            _output.WriteLine(Path.Combine(Directory.GetCurrentDirectory(), SchemaPath));
            Assert.True(File.Exists(path));
            var protocBinPath = Path.ChangeExtension(path, "protoc.bin");
            int exitCode;
            using (var proc = Process.Start("protoc", $"--descriptor_set_out={protocBinPath} {path}"))
            {
                proc.WaitForExit();
                exitCode = proc.ExitCode;
            }
            
            FileDescriptorSet set;
            string protocJson, jsonPath;
            using (var file = File.OpenRead(protocBinPath))
            {
                set = Serializer.Deserialize<FileDescriptorSet>(file);
                protocJson = JsonConvert.SerializeObject(set, Formatting.Indented);
                jsonPath = Path.ChangeExtension(path, "protoc.json");
                File.WriteAllText(jsonPath, protocJson);
            }

            set = new FileDescriptorSet();
            set.Add(path);
            var parserJson = JsonConvert.SerializeObject(set, Formatting.Indented);
            jsonPath = Path.ChangeExtension(path, "parser.json");
            File.WriteAllText(jsonPath, parserJson);

            var parserBinPath = Path.ChangeExtension(path, "parser.bin");
            using (var file = File.Create(parserBinPath))
            {
                Serializer.Serialize(file, set);
            }

            var errors = set.GetErrors();
            if (exitCode == 0)
            {
                Assert.Equal(0, errors.Length);
            }
            else
            {
                Assert.NotEqual(0, errors.Length);
            }

            // compare results
            Assert.Equal(protocJson, parserJson);

            var parserHex = BitConverter.ToString(File.ReadAllBytes(parserBinPath));
            var protocHex = BitConverter.ToString(File.ReadAllBytes(protocBinPath));
            Assert.Equal(protocHex, parserHex);
        }

        public SchemaTests(ITestOutputHelper output) => _output = output;

    }
}
