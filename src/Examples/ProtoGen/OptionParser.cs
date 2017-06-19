#if !NO_CODEGEN
using Xunit;
using ProtoBuf.CodeGenerator;
using System;

namespace Examples.ProtoGen
{
    
    public class OptionParser
    {
        [Fact]
        public void TestMainHelp()
        {
            Assert.NotEqual(0, CommandLineOptions.Main("/?"));
        }

        [Fact]
        public void TestTemplateHelp()
        {
            Assert.NotEqual(0, CommandLineOptions.Main("-p:help"));
        }

        [Fact]
        public void TestCompileDescriptorAndWriteMainDescriptorCS_Success()
        {
            Assert.Equal(0, CommandLineOptions.Main(@"-i:ProtoGen/descriptor.proto", "-i:protobuf-net.proto", "-o:descriptor.cs"));
        }



        [Fact]
        public void TestMainStupid()
        {
            Assert.AreEqual(0, CommandLineOptions.Main("-t:notexist", "-p:help"));
        }

        [Fact, ExpectedException(typeof(ArgumentNullException))]
        public void TestNullWriter()
        {
            CommandLineOptions.Parse(null);
        }

        [Fact]
        public void TestEmptyCommandShowsHelp()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out);
            Assert.True(opt.ShowHelp);
            Assert.True(opt.ShowLogo);
        }

        [Fact]
        public void TestQuietCommand()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-q");
            Assert.True(opt.ShowHelp);
            Assert.False(opt.ShowLogo);
        }

        [Fact]
        public void TestSingleInputOnly()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:foo");
            Assert.Equal(1, opt.InPaths.Count);
            Assert.Equal("foo", opt.InPaths[0]);
            Assert.False(opt.ShowHelp);
            Assert.Equal(CommandLineOptions.TemplateCSharp, opt.Template);
            Assert.Equal("", opt.OutPath);
        }

        [Fact]
        public void TestDefaultNamespace()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:a.proto");
            Assert.Null(opt.DefaultNamespace);

            opt = CommandLineOptions.Parse(Console.Out, "-i:a.proto", "-ns:");
            Assert.Equal("", opt.DefaultNamespace);

            opt = CommandLineOptions.Parse(Console.Out, "-i:a.proto", "-ns:Foo");
            Assert.Equal("Foo", opt.DefaultNamespace);
        }

        [Fact]
        public void TestSingleInputWithTemplate()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:foo", "-t:bespoke");
            Assert.Equal(1, opt.InPaths.Count);
            Assert.Equal("foo", opt.InPaths[0]);
            Assert.False(opt.ShowHelp);
            Assert.Equal("bespoke", opt.Template);
            Assert.Equal("", opt.OutPath);
        }

        [Fact]
        public void TestSingleInputWithSingleOutput()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:foo", "-o:bar");
            Assert.Equal(1, opt.InPaths.Count);
            Assert.Equal("foo", opt.InPaths[0]);
            Assert.False(opt.ShowHelp);
            Assert.Equal(CommandLineOptions.TemplateCSharp, opt.Template);
            Assert.Equal("bar", opt.OutPath);
        }

        [Fact]
        public void TestSingleInputWithMultipleOutput()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:foo", "-o:bar", "-o:blop");
            Assert.True(opt.ShowHelp);
        }


        [Fact]
        public void TestMultipleInputOnly()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:foo", "-i:bar", "-i:blop");
            Assert.Equal(3, opt.InPaths.Count);
            Assert.Equal("foo", opt.InPaths[0]);
            Assert.Equal("bar", opt.InPaths[1]);
            Assert.Equal("blop", opt.InPaths[2]);
            Assert.False(opt.ShowHelp);
            Assert.Equal(CommandLineOptions.TemplateCSharp, opt.Template);
            Assert.Equal("", opt.OutPath);
        }

        [Fact]
        public void TestInputWithHelpQuestion()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:foo", "/?");
            Assert.True(opt.ShowHelp);
        }

        [Fact]
        public void TestInputWithTemplateHelp()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-p:help");
            try
            {
                opt.Execute();
                Assert.Fail();
            }
            catch
            {
                Assert.Equal(1, opt.MessageCount);
            }
        }

        [Fact]
        public void TestInputWithHelpDashH()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:foo", "-h");
            Assert.True(opt.ShowHelp);
        }

        [Fact]
        public void TestInputWithGibberish()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:foo", "-gibber");
            Assert.True(opt.ShowHelp);
        }

        [Fact]
        public void TestInputWithValuelessParameter()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:foo", "-p:abc");
            Assert.Equal("true", opt.XsltOptions.GetParam("abc", ""));
        }

        [Fact, ExpectedException(typeof(ArgumentException))]
        public void TestInputWithRepeatedValuelessParameter()
        {
            CommandLineOptions.Parse(Console.Out, "-i:foo", "-p:abc", "-p:abc");
        }

        [Fact]
        public void TestInputWithValuedParameter()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:foo", "-p:abc=def");
            Assert.Equal("def", opt.XsltOptions.GetParam("abc", ""));
        }

        [Fact, ExpectedException(typeof(ArgumentException))]
        public void TestInputWithRepeatedValuedParameter()
        {
            CommandLineOptions.Parse(Console.Out, "-i:foo", "-p:abc=def", "-p:abc=ghi");
        }
    }
}
#endif