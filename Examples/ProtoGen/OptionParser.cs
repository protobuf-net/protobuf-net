using NUnit.Framework;
using ProtoBuf.CodeGenerator;
using System;

namespace Examples.ProtoGen
{
    [TestFixture]
    public class OptionParser
    {
        [Test]
        public void TestMainHelp()
        {
            Assert.AreNotEqual(0, CommandLineOptions.Main("/?"));
        }

        [Test]
        public void TestTemplateHelp()
        {
            Assert.AreNotEqual(0, CommandLineOptions.Main("-p:help"));
        }

        [Test]
        public void TestDescriptorSuccess()
        {
            Assert.AreEqual(0, CommandLineOptions.Main(@"-i:ProtoGen/descriptor.proto", "-o:descriptor.cs"));
        }

        

        [Test]
        public void TestMainStupid()
        {
            Assert.AreNotEqual(0, CommandLineOptions.Main("-t:notexist", "-p:help"));
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestNullWriter()
        {
            CommandLineOptions.Parse(null);
        }

        [Test]
        public void TestEmptyCommandShowsHelp()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out);
            Assert.IsTrue(opt.ShowHelp);
            Assert.IsTrue(opt.ShowLogo);
        }

        [Test]
        public void TestQuietCommand()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-q");
            Assert.IsTrue(opt.ShowHelp);
            Assert.IsFalse(opt.ShowLogo);
        }

        [Test]
        public void TestSingleInputOnly()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:foo" );
            Assert.AreEqual(1, opt.InPaths.Count);
            Assert.AreEqual("foo", opt.InPaths[0]);
            Assert.IsFalse(opt.ShowHelp);
            Assert.AreEqual(CommandLineOptions.TemplateCSharp, opt.Template);
            Assert.AreEqual("", opt.OutPath);
        }

        [Test]
        public void TestDefaultNamespace()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:a.proto");
            Assert.IsNull(opt.DefaultNamespace);

            opt = CommandLineOptions.Parse(Console.Out, "-i:a.proto", "-ns:");
            Assert.AreEqual("", opt.DefaultNamespace);

            opt = CommandLineOptions.Parse(Console.Out, "-i:a.proto", "-ns:Foo");
            Assert.AreEqual("Foo", opt.DefaultNamespace);
        }

        [Test]
        public void TestSingleInputWithTemplate()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-i:foo", "-t:bespoke" );
            Assert.AreEqual(1, opt.InPaths.Count);
            Assert.AreEqual("foo", opt.InPaths[0]);
            Assert.IsFalse(opt.ShowHelp);
            Assert.AreEqual("bespoke", opt.Template);
            Assert.AreEqual("", opt.OutPath);
        }

        [Test]
        public void TestSingleInputWithSingleOutput()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out,  "-i:foo", "-o:bar" );
            Assert.AreEqual(1, opt.InPaths.Count);
            Assert.AreEqual("foo", opt.InPaths[0]);
            Assert.IsFalse(opt.ShowHelp);
            Assert.AreEqual(CommandLineOptions.TemplateCSharp, opt.Template);
            Assert.AreEqual("bar", opt.OutPath);
        }

        [Test]
        public void TestSingleInputWithMultipleOutput()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out,  "-i:foo", "-o:bar", "-o:blop" );
            Assert.IsTrue(opt.ShowHelp);
        }


        [Test]
        public void TestMultipleInputOnly()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out,  "-i:foo", "-i:bar", "-i:blop" );
            Assert.AreEqual(3, opt.InPaths.Count);
            Assert.AreEqual("foo", opt.InPaths[0]);
            Assert.AreEqual("bar", opt.InPaths[1]);
            Assert.AreEqual("blop", opt.InPaths[2]);
            Assert.IsFalse(opt.ShowHelp);
            Assert.AreEqual(CommandLineOptions.TemplateCSharp, opt.Template);
            Assert.AreEqual("", opt.OutPath);
        }

        [Test]
        public void TestInputWithHelpQuestion()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out,  "-i:foo", "/?" );
            Assert.IsTrue(opt.ShowHelp);
        }

        [Test]
        public void TestInputWithTemplateHelp()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, "-p:help");
            try
            {
                opt.Execute();
                Assert.Fail();
            } catch
            {
                Assert.AreEqual(1, opt.MessageCount);
            }
        }

        [Test]
        public void TestInputWithHelpDashH()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out,  "-i:foo", "-h" );
            Assert.IsTrue(opt.ShowHelp);
        }
        
        [Test]
        public void TestInputWithGibberish()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out,  "-i:foo", "-gibber" );
            Assert.IsTrue(opt.ShowHelp);
        }

        [Test]
        public void TestInputWithValuelessParameter()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out,  "-i:foo", "-p:abc" );
            Assert.AreEqual("true", opt.XsltOptions.GetParam("abc", ""));
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestInputWithRepeatedValuelessParameter()
        {
            CommandLineOptions.Parse(Console.Out,  "-i:foo", "-p:abc", "-p:abc" );
        }

        [Test]
        public void TestInputWithValuedParameter()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out,  "-i:foo", "-p:abc=def" );
            Assert.AreEqual("def", opt.XsltOptions.GetParam("abc", ""));
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestInputWithRepeatedValuedParameter()
        {
            CommandLineOptions.Parse(Console.Out,  "-i:foo", "-p:abc=def", "-p:abc=ghi" );
        }
    }
}
