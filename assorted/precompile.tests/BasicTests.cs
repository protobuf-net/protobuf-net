using Examples;
using NUnit.Framework;
using ProtoBuf.Precompile;
using System;
using System.Collections.Generic;
using System.IO;
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

        [Test]
        public void ExecuteNet45WithInternalTypes()
        {
            PreCompileContext ctx;
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] { @"..\..\..\Net45Dto\bin\release\Net45Dto.dll"
                , @"-o:..\..\..\Net45Dto\bin\release\Net45Serializer.dll", "-t:MySerializer" }, out ctx), "TryParse");
            Assert.IsTrue(ctx.SanityCheck(), "SanityCheck");
            Assert.IsTrue(ctx.Execute(), "Execute");
            PEVerify.AssertValid(@"..\..\..\Net45Dto\bin\release\Net45Serializer.dll");
        }

        [Test]
        public void ExecuteSigned()
        {
            PreCompileContext ctx;
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] { @"..\..\..\SignedDto\bin\release\SignedDto.dll"
                , @"-o:..\..\..\SignedDto\bin\release\SignedSerializer.dll",
                "-t:MySignedSerializer",
                @"-keyfile:..\..\..\SignedDto\ProtoBuf.snk"
            }, out ctx), "TryParse");
            Assert.IsTrue(ctx.SanityCheck(), "SanityCheck");
            Assert.IsTrue(ctx.Execute(), "Execute");
            PEVerify.AssertValid(@"..\..\..\SignedDto\bin\release\SignedSerializer.dll");
        }
    }
}
