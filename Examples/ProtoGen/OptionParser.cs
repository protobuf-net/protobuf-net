using NUnit.Framework;
using ProtoBuf.CodeGenerator;
using System;

namespace Examples.ProtoGen
{
    [TestFixture]
    public class OptionParser
    {
        [Test]
        public void TestEmptyCommandShowsHelp()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out,new string[0]);
            Assert.IsTrue(opt.ShowHelp);
        }

        [Test]
        public void TestSingleInputOnly()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, new[] { "-i:foo" });
            Assert.AreEqual(1, opt.InPaths.Count);
            Assert.AreEqual("foo", opt.InPaths[0]);
            Assert.IsFalse(opt.ShowHelp);
            Assert.AreEqual(CommandLineOptions.TemplateCSharp, opt.Template);
            Assert.AreEqual("", opt.OutPath);
        }

        [Test]
        public void TestSingleInputWithTemplate()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, new[] { "-i:foo", "-t:bespoke" });
            Assert.AreEqual(1, opt.InPaths.Count);
            Assert.AreEqual("foo", opt.InPaths[0]);
            Assert.IsFalse(opt.ShowHelp);
            Assert.AreEqual("bespoke", opt.Template);
            Assert.AreEqual("", opt.OutPath);
        }

        [Test]
        public void TestSingleInputWithSingleOutput()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, new[] { "-i:foo", "-o:bar" });
            Assert.AreEqual(1, opt.InPaths.Count);
            Assert.AreEqual("foo", opt.InPaths[0]);
            Assert.IsFalse(opt.ShowHelp);
            Assert.AreEqual(CommandLineOptions.TemplateCSharp, opt.Template);
            Assert.AreEqual("bar", opt.OutPath);
        }

        [Test]
        public void TestSingleInputWithMultipleOutput()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, new[] { "-i:foo", "-o:bar", "-o:blop" });
            Assert.IsTrue(opt.ShowHelp);
        }


        [Test]
        public void TestMultipleInputOnly()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, new[] { "-i:foo", "-i:bar", "-i:blop" });
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
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, new[] { "-i:foo", "/?" });
            Assert.IsTrue(opt.ShowHelp);
        }

        [Test]
        public void TestInputWithHelpDashH()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, new[] { "-i:foo", "-h" });
            Assert.IsTrue(opt.ShowHelp);
        }
        
        [Test]
        public void TestInputWithGibberish()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, new[] { "-i:foo", "-gibber" });
            Assert.IsTrue(opt.ShowHelp);
        }

        [Test]
        public void TestInputWithValuelessParameter()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, new[] { "-i:foo", "-p:abc" });
            Assert.AreEqual("true", opt.XsltOptions.GetParam("abc", ""));
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestInputWithRepeatedValuelessParameter()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, new[] { "-i:foo", "-p:abc", "-p:abc" });
            Assert.AreEqual("true", opt.XsltOptions.GetParam("abc", ""));
        }

        [Test]
        public void TestInputWithValuedParameter()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, new[] { "-i:foo", "-p:abc=def" });
            Assert.AreEqual("def", opt.XsltOptions.GetParam("abc", ""));
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestInputWithRepeatedValuedParameter()
        {
            CommandLineOptions opt = CommandLineOptions.Parse(Console.Out, new[] { "-i:foo", "-p:abc=def", "-p:abc=ghi" });
            Assert.AreEqual("ghi", opt.XsltOptions.GetParam("abc", ""));
        }
    }
}
