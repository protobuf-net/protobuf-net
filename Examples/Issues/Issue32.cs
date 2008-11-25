using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Examples.ProtoGen;
using System.IO;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue32
    {
        [Test]
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
