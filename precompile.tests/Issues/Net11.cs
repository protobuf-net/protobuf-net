using System;
using NUnit.Framework;
using ProtoBuf.Precompile;

namespace precompile.tests.Issues
{
    [TestFixture]
    public class Net11
    {
        [Test]
        public void ExecuteNet11()
        {
            PreCompileContext ctx;
            //string framework = Environment.ExpandEnvironmentVariables(@"%windir%\Microsoft.NET\Framework\v1.1.4322");
            Assert.IsTrue(CommandLineAttribute.TryParse(new[] { @"..\..\..\Net11_Poco\bin\release\Net11_Poco.dll"
                //, "-f:" + framework
                , "-o:Net11Serializer.dll", "-t:MySerializer" }, out ctx), "TryParse");
            Assert.IsTrue(ctx.SanityCheck(), "SanityCheck");
            Assert.IsTrue(ctx.Execute(), "Execute");
        }
    }
}
