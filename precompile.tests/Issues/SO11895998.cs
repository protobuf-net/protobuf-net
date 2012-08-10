using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Precompile;

namespace precompile.tests.Issues
{
    [TestFixture]
    public class SO11895998
    {
        [Test]
        public void Execute()
        {
            PreCompileContext ctx;
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] { @"..\..\..\SO11895998\bin\release\SO11895998.dll"
                , "-o:SO11895998_Serializer.dll", "-t:MySerializer" }, out ctx), "TryParse");
            Assert.IsTrue(ctx.SanityCheck(), "SanityCheck");
            Assert.IsTrue(ctx.Execute(), "Execute");
        }
    }
}
