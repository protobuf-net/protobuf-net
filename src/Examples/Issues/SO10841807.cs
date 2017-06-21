#if !NO_WCF
using System;
using System.Text.RegularExpressions;
using Xunit;
using ProtoBuf.ServiceModel;

namespace Examples.Issues
{
    
    public class SO10841807
    {
        [Fact]
        public void Execute()
        {
            string aqn = typeof (ProtoBehaviorExtension).AssemblyQualifiedName;
            Assert.True(Regex.IsMatch(aqn, @"ProtoBuf\.ServiceModel\.ProtoBehaviorExtension, protobuf\-net, Version=[0-9.]+, Culture=neutral, PublicKeyToken=257b51d87d2e4d67"));
            Console.WriteLine("WCF AQN: " + aqn);
        }
    }
}
#endif