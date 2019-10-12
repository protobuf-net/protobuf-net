#if !NO_WCF
using Xunit;
using ProtoBuf.ServiceModel;
using Xunit.Abstractions;

namespace Examples.Issues
{
    public class SO10841807
    {
        private ITestOutputHelper Log { get; }
        public SO10841807(ITestOutputHelper _log) => Log = _log;

        [Fact]
        public void Execute()
        {
            string aqn = typeof (ProtoBehaviorExtension).AssemblyQualifiedName;
            Assert.Matches(@"ProtoBuf\.ServiceModel\.ProtoBehaviorExtension, protobuf\-net, Version=[0-9.]+, Culture=neutral, PublicKeyToken=257b51d87d2e4d67", aqn);
            Log.WriteLine("WCF AQN: " + aqn);
        }
    }
}
#endif