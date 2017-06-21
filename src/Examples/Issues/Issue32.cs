#if !NO_CODEGEN
using System.IO;
using Xunit;
using Examples.ProtoGen;

namespace Examples.Issues
{
    
    public class Issue32
    {
        [Fact]
        public void TestGpsProtoGen()
        {
            string xml = Generator.GetCode(@"-i:ProtoGen\gps.proto", "-t:xml");
            File.WriteAllText("gps.xml", xml);
            string code = Generator.GetCode(@"-i:ProtoGen\gps.proto");
            File.WriteAllText("gps.cs", code);

            Generator.TestCompileCSharp(code);
        }
    }
}
#endif