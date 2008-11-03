using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf.CodeGenerator;
using google.protobuf;
using System.IO;
using System.Diagnostics;

namespace Examples.ProtoGen
{
    [TestFixture]
    public class InputLoader
    {
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNullFileSet()
        {
            InputFileLoader.Merge(null, "abc");
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNullPath()
        {
            FileDescriptorSet files = new FileDescriptorSet();
            InputFileLoader.Merge(files, null);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestEmptyPath()
        {
            FileDescriptorSet files = new FileDescriptorSet();
            InputFileLoader.Merge(files, "");
        }

        [Test, ExpectedException(typeof(FileNotFoundException))]
        public void TestMissingInput()
        {
            FileDescriptorSet files = new FileDescriptorSet();
            InputFileLoader.Merge(files, @"ProtoGen\NoneSuch.bin");
        }

        [Test]
        public void TestBinaryInput()
        {
            // compile .proto to .bin
            Process.Start("protoc", @"--descriptor_set_out=ProtoGen\descriptor.bin --include_imports ProtoGen\descriptor.proto").WaitForExit();

            // process .bin
            FileDescriptorSet files = new FileDescriptorSet();
            Assert.AreEqual(0, files.file.Count);
            InputFileLoader.Merge(files, @"ProtoGen\descriptor.bin");
            Assert.AreEqual(1, files.file.Count);
        }

        [Test]
        public void TestProtoInput()
        {
            FileDescriptorSet files = new FileDescriptorSet();
            Assert.AreEqual(0, files.file.Count);
            InputFileLoader.Merge(files, @"ProtoGen\descriptor.proto");
            Assert.AreEqual(1, files.file.Count);
        }



        [Test]
        public void TestGarbageInput()
        {
            FileDescriptorSet files = new FileDescriptorSet();
            try {
                InputFileLoader.Merge(files, @"ProtoGen\InputLoader.cs");
                Assert.Fail("Should have barfed");
            }
            catch(ArgumentException ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("The input file could not be parsed."));
            }
            catch
            {
                Assert.Fail("Expected ArgumentException");
            }

        }
    }
}
