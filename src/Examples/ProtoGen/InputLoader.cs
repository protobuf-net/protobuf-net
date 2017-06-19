#if !NO_CODEGEN
using System;
using System.Diagnostics;
using google.protobuf;
using Xunit;
using ProtoBuf.CodeGenerator;

namespace Examples.ProtoGen
{
    
    public class InputLoader
    {
        [Fact, ExpectedException(typeof(ArgumentNullException))]
        public void TestNullFileSet()
        {
            InputFileLoader.Merge(null, "abc", Console.Error);
        }

        [Fact, ExpectedException(typeof(ArgumentNullException))]
        public void TestNullPath()
        {
            FileDescriptorSet files = new FileDescriptorSet();
            InputFileLoader.Merge(files, null, Console.Error);
        }

        [Fact, ExpectedException(typeof(ArgumentNullException))]
        public void TestEmptyPath()
        {
            FileDescriptorSet files = new FileDescriptorSet();
            InputFileLoader.Merge(files, "", Console.Error);
        }

        [Fact, ExpectedException(typeof(ProtoParseException))]
        public void TestMissingInput()
        {
            FileDescriptorSet files = new FileDescriptorSet();
            InputFileLoader.Merge(files, @"ProtoGen\NoneSuch.bin", Console.Error);
        }

        [Fact]
        public void TestBinaryInput()
        {
            // compile .proto to .bin
            Process.Start("protoc", @"--descriptor_set_out=ProtoGen\descriptor.bin --include_imports ProtoGen\descriptor.proto").WaitForExit();

            // process .bin
            FileDescriptorSet files = new FileDescriptorSet();
            Assert.Equal(0, files.file.Count);
            InputFileLoader.Merge(files, @"ProtoGen\descriptor.bin", Console.Error);
            Assert.Equal(1, files.file.Count);
        }

        [Fact]
        public void TestProtoInput()
        {
            FileDescriptorSet files = new FileDescriptorSet();
            Assert.Equal(0, files.file.Count);
            InputFileLoader.Merge(files, @"ProtoGen\descriptor.proto", Console.Error);
            Assert.Equal(1, files.file.Count);
        }



        [Fact]
        public void TestGarbageInput()
        {
            FileDescriptorSet files = new FileDescriptorSet();
            try
            {
                InputFileLoader.Merge(files, @"ProtoGen\InputLoader.cs", Console.Error);
                Assert.Fail("Should have barfed");
            }
            catch (ProtoParseException ex)
            {
                bool sw = ex.Message.StartsWith("An error occurred parsing InputLoader.cs");
                if (!sw) Assert.Fail("Expected message not found: " + ex.Message);
            }
            catch
            {
                Assert.Fail("Expected ArgumentException");
            }

        }
    }
}
#endif