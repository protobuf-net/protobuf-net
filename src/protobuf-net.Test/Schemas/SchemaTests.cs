using Google.Protobuf.Reflection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Schemas
{
    [Trait("kind", "schema")]
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
            using (var proc = new Process())
            {
                var psi = proc.StartInfo;
                psi.FileName = "protoc";
                psi.Arguments = $"--descriptor_set_out={protocBinPath} {path}";
                psi.RedirectStandardError = psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                var output = new StringBuilder();
                proc.ErrorDataReceived += (sender, args) => { lock (output) { output.AppendLine($"stderr: {args.Data}"); } };
                proc.OutputDataReceived += (sender, args) => { lock (output) { output.AppendLine($"stdout: {args.Data}"); } };
                proc.Start();
                proc.WaitForExit();
                exitCode = proc.ExitCode;
                if(output.Length != 0)
                {
                    _output.WriteLine(output.ToString());
                }
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
            if(errors.Any())
            {
                _output.WriteLine("Parser errors:");
                foreach (var err in errors) _output.WriteLine(err.ToString());
            }

            _output.WriteLine("Protoc exited with code " + exitCode);

            var errorCount = errors.Count(x => x.IsError);
            if (exitCode == 0)
            {
                Assert.Equal(0, errorCount);
            }
            else
            {
                Assert.NotEqual(0, errorCount);
            }



            var parserHex = BitConverter.ToString(File.ReadAllBytes(parserBinPath));
            var protocHex = BitConverter.ToString(File.ReadAllBytes(protocBinPath));
            File.WriteAllText(Path.ChangeExtension(parserBinPath, "parser.hex"), parserHex);
            File.WriteAllText(Path.ChangeExtension(protocBinPath, "protoc.hex"), protocHex);

            // compare results
            Assert.Equal(protocJson, parserJson);
            Assert.Equal(protocHex, parserHex);
        }

        public SchemaTests(ITestOutputHelper output) => _output = output;

    }
}
