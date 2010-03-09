#warning excised

//using System.IO;
//using NUnit.Framework;

//namespace Examples.Issues
//{
//    [TestFixture]
//    public class Issue32
//    {
//        [Test]
//        public void TestGpsProtoGen()
//        {
//            string xml = Generator.GetCode(@"-i:ProtoGen\gps.proto", "-t:xml");
//            File.WriteAllText("gps.xml", xml);
//            string code = Generator.GetCode(@"-i:ProtoGen\gps.proto");
//            File.WriteAllText("gps.cs", code);

//            Generator.TestCompileCSharp(code);
//        }
//    }
//}
