// uncomment this to cause the source folder to be updated with the current outputs;
// this makes changes visible in the source repo, for comparison
// #define UPDATE_FILES

using Google.Protobuf.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProtoBuf.CodeGen;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace BuildToolsUnitTests.AOT;

public class AOTSchemaTests
{
    private readonly ITestOutputHelper _output;

    public AOTSchemaTests(ITestOutputHelper output)
        => _output = output;
    

    private const string SchemaPath = "AOT/Schemas";
    public static IEnumerable<object[]> GetSchemas()
    {
        foreach (var file in Directory.GetFiles(SchemaPath, "*.proto", SearchOption.AllDirectories))
        {
            yield return new object[] { Regex.Replace(file.Replace('\\', '/'), "^Schemas/", "")  };
        }
    }
    private static JsonSerializerSettings JsonSettings { get; } = new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
        TypeNameHandling = TypeNameHandling.None,
        Converters = { new StringEnumConverter() },
    };


    [Theory]
    [MemberData(nameof(GetSchemas))]
    public void ParseProtoToModel(string protoPath)
    {
        var schemaPath = Path.Combine(Directory.GetCurrentDirectory(), SchemaPath);
        _output.WriteLine(protoPath);
        var file = Path.GetFileName(protoPath);
        _output.WriteLine($"{file} in {Path.GetDirectoryName(Path.Combine(schemaPath, protoPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar))}");

        var fds = new FileDescriptorSet();
        fds.AddImportPath(schemaPath);
        Assert.True(fds.Add(file, true));
        fds.Process();
        var errors = fds.GetErrors();
        foreach (var error in errors)
        {
            _output.WriteLine($"{error.LineNumber}:{error.ColumnNumber}: {error.Message}");
        }
        Assert.Empty(errors);

        var context = new CodeGenContext();
        var parsed = CodeGenSet.Parse(fds, context);

        var json = JsonConvert.SerializeObject(parsed, JsonSettings);
        _output.WriteLine(json);

        var jsonPath = Path.ChangeExtension(protoPath, ".json");
#if UPDATE_FILES
        var target = Path.Combine(Path.GetDirectoryName(CallerFilePath())!, "..", jsonPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        _output.WriteLine($"updating {target}...");
        File.WriteAllText(target, json);
#else
        if (File.Exists(jsonPath))
        {
            var expectedJson = File.ReadAllText(jsonPath);
            Assert.Equal(expectedJson, json);
        }
        else
        {
            _output.WriteLine($"({jsonPath} found)");
        }
#endif


    }

    private string CallerFilePath([CallerFilePath] string path = null) => path;
}
