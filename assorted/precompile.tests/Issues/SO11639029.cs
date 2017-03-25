using NUnit.Framework;
using ProtoBuf.Precompile;
using Examples;
namespace precompile.tests.Issues
{
    [TestFixture]
    public class SO11639029
    {
        [Test]
        public void TestHelpUsage()
        {
            PreCompileContext ctx;
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] {"-?"}, out ctx), "TryParse -?");
            Assert.IsTrue(ctx.Help, "Help -?");
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] { "-h" }, out ctx), "TryParse -h");
            Assert.IsTrue(ctx.Help, "Help -h");
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] { "-help" }, out ctx), "TryParse -help");
            Assert.IsTrue(ctx.Help, "Help -help");
        }

        [Test]
        public void TestOverallUsage()
        {
            PreCompileContext ctx;
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] { "a.dll", "-o:out.dll", "-t:Type" }, out ctx), "TryParse");
            Assert.AreEqual("Type", ctx.TypeName);
            Assert.AreEqual("out.dll", ctx.AssemblyName);
            Assert.AreEqual(1, ctx.Inputs.Count);
            Assert.AreEqual("a.dll", ctx.Inputs[0]);
        }

        [Test]
        public void TestFullPath()
        {
            PreCompileContext ctx;
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] { @"c:\a.dll", "b.dll", "-o:out.dll", "-t:Type" }, out ctx), "TryParse");
            Assert.AreEqual("Type", ctx.TypeName);
            Assert.AreEqual("out.dll", ctx.AssemblyName);
            Assert.AreEqual(2, ctx.Inputs.Count);
            Assert.AreEqual(@"c:\a.dll", ctx.Inputs[0]);
            Assert.AreEqual("b.dll", ctx.Inputs[1]);
        }

        [Test]
        public void ExecuteSilverDto()
        {
            PreCompileContext ctx;
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] { @"..\..\..\SilverDto\bin\release\SilverDto.dll"
                , "-o:SilverSerializer.dll", "-t:MySerializer" }, out ctx), "TryParse");
            Assert.IsTrue(ctx.SanityCheck(), "SanityCheck");
            Assert.IsTrue(ctx.Execute(), "Execute");
        }
    }
}
