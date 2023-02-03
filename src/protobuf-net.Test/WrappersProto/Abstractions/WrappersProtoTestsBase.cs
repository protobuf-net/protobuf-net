using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Google.Protobuf.Reflection;
using ProtoBuf.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test.WrappersProto.Abstractions
{
    public abstract class WrappersProtoTestsBase
    {
        private readonly ITestOutputHelper _log;
        protected WrappersProtoTestsBase(ITestOutputHelper log)
        {
            _log = log;
        }
        
        protected void AssertCSharpCodeGenerator(
            string protobufSchemaContent, 
            string generatedCode, 
            string fileName = "default.proto")
        {
            using var reader = new StringReader(protobufSchemaContent);
            var set = new FileDescriptorSet();
            set.Add(fileName, true, reader);
            set.Process();
            
            // act
            var result = CSharpCodeGenerator.Default.Generate(set, NameNormalizer.Default)?.ToArray();
            
            if (result is null || !result.Any()) Assert.Fail("No generation output found");
            if (result.Length > 1) Assert.Fail("Generated more than 1 file, however single file output was expected");

            var resultText = result.First().Text;
            _log.WriteLine("Generated such csharp code:");
            _log.WriteLine(resultText);
            _log.WriteLine("-----------------------------");;
            
            // remove any whitespaces between '\r\n' and any valuable symbol to not struggle with tabs in tests
            generatedCode = RemoveWhitespacesInLineStart(generatedCode);
            resultText = RemoveWhitespacesInLineStart(resultText);
            
            Assert.Equal(generatedCode.Trim(), resultText.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }

        string RemoveWhitespacesInLineStart(string str) => Regex.Replace(str, @"(?<=\n)\s+", "");
    }
}