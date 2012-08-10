using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Precompile;
using System;

namespace precompile.tests.Issues
{
    [TestFixture]
    public class SO11895998
    {
        [Test]
        public void ExecutePhone7()
        {
            PreCompileContext ctx;
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] { @"..\..\..\SO11895998\bin\release\SO11895998.dll"
                , "-o:SO11895998_Serializer.dll", "-t:MySerializer" }, out ctx), "TryParse");
            Assert.IsTrue(ctx.SanityCheck(), "SanityCheck");
            Assert.IsTrue(ctx.Execute(), "Execute");
        }

        [Test]
        public void ExecutePortable()
        {
            PreCompileContext ctx;
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] { @"..\..\..\SO11895998_Portable\bin\release\SO11895998_Portable.dll"
                , "-o:SO11895998_PortableSerializer.dll", "-t:MySerializer" }, out ctx), "TryParse");
            Assert.IsTrue(ctx.SanityCheck(), "SanityCheck");
            Assert.IsTrue(ctx.Execute(), "Execute");
        }
    }
}
