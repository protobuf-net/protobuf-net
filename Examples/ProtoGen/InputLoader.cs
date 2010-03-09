#warning excised
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using NUnit.Framework;
//using ProtoBuf.CodeGenerator;
//using google.protobuf;
//using System.IO;
//using System.Diagnostics;

//namespace Examples.ProtoGen
//{
//    [TestFixture]
//    public class InputLoader
//    {
//        [Test, ExpectedException(typeof(ArgumentNullException))]
//        public void TestNullFileSet()
//        {
//            InputFileLoader.Merge(null, "abc", Console.Error);
//        }

//        [Test, ExpectedException(typeof(ArgumentNullException))]
//        public void TestNullPath()
//        {
//            FileDescriptorSet files = new FileDescriptorSet();
//            InputFileLoader.Merge(files, null, Console.Error);
//        }

//        [Test, ExpectedException(typeof(ArgumentNullException))]
//        public void TestEmptyPath()
//        {
//            FileDescriptorSet files = new FileDescriptorSet();
//            InputFileLoader.Merge(files, "", Console.Error);
//        }

//        [Test, ExpectedException(typeof(ProtoParseException))]
//        public void TestMissingInput()
//        {
//            FileDescriptorSet files = new FileDescriptorSet();
//            InputFileLoader.Merge(files, @"ProtoGen\NoneSuch.bin", Console.Error);
//        }

//        [Test]
//        public void TestBinaryInput()
//        {
//            // compile .proto to .bin
//            Process.Start("protoc", @"--descriptor_set_out=ProtoGen\descriptor.bin --include_imports ProtoGen\descriptor.proto").WaitForExit();

//            // process .bin
//            FileDescriptorSet files = new FileDescriptorSet();
//            Assert.AreEqual(0, files.file.Count);
//            InputFileLoader.Merge(files, @"ProtoGen\descriptor.bin", Console.Error);
//            Assert.AreEqual(1, files.file.Count);
//        }

//        [Test]
//        public void TestProtoInput()
//        {
//            FileDescriptorSet files = new FileDescriptorSet();
//            Assert.AreEqual(0, files.file.Count);
//            InputFileLoader.Merge(files, @"ProtoGen\descriptor.proto", Console.Error);
//            Assert.AreEqual(1, files.file.Count);
//        }



//        [Test]
//        public void TestGarbageInput()
//        {
//            FileDescriptorSet files = new FileDescriptorSet();
//            try {
//                InputFileLoader.Merge(files, @"ProtoGen\InputLoader.cs", Console.Error);
//                Assert.Fail("Should have barfed");
//            }
//            catch(ProtoParseException ex)
//            {
//                bool sw = ex.Message.StartsWith("An error occurred parsing InputLoader.cs");
//                if (!sw) Assert.Fail("Expected message not found: " + ex.Message);
//            }
//            catch
//            {
//                Assert.Fail("Expected ArgumentException");
//            }

//        }
//    }
//}
