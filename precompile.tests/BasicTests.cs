using NUnit.Framework;
using ProtoBuf.Precompile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace precompile.tests
{
    [TestFixture]
    public class BasicTests
    {
        [Test]
        public void ExecutePhone8()
        {
            PreCompileContext ctx;
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] { @"..\..\..\Phone8Dto\bin\x86\release\Phone8Dto.dll"
                , "-o:Phone8DtoSerializer.dll", "-t:MySerializer" }, out ctx), "TryParse");
            Assert.IsTrue(ctx.SanityCheck(), "SanityCheck");
            Assert.IsTrue(ctx.Execute(), "Execute");
        }
    }
}
