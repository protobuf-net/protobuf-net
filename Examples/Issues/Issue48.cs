#warning excised

//using System;
//using System.IO;
//using NUnit.Framework;

//namespace Examples.Issues
//{
//    [TestFixture]
//    public class Issue48
//    {

//        [Test]
//        public void ImportMxp()
//        {
//            string oldDir =Environment.CurrentDirectory;
//            string dir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
//            if (!string.IsNullOrEmpty(dir)
//                && dir[dir.Length - 1] != Path.DirectorySeparatorChar
//                && dir[dir.Length - 1] != Path.AltDirectorySeparatorChar)
//            {
//                dir += Path.DirectorySeparatorChar;
//            }
//            Environment.CurrentDirectory = Path.Combine(
//                dir,@"Issues\Issue48");
//            try {
//                string s = Generator.GetCode(
//                    @"-i:MXP.Common.proto",
//                    @"-o:MXP.Common.cs");
//                Assert.AreEqual("", s);
//                s = Generator.GetCode(
//                     @"-i:MXP.Common.proto",
//                     @"-o:MXP.Common.xml", "-t:xml");
//                Assert.AreEqual("", s);
                 
//                s = Generator.GetCode(
//                    @"-i:MXP.Extentions.OpenMetaverseFragments.proto",
//                    @"-o:MXP.Extentions.OpenMetaverseFragments.cs",
//                    @"-p:import=MXP.Common;System.Xml;");
//                Assert.AreEqual("", s);
//                s = Generator.GetCode(
//                    @"-i:MXP.Extentions.OpenMetaverseFragments.proto",
//                    @"-o:MXP.Extentions.OpenMetaverseFragments.xml", "-t:xml");
//                Assert.AreEqual("", s);
                                    
//            } finally {
//                Environment.CurrentDirectory = oldDir;
//            }
//        }
//    }


//}