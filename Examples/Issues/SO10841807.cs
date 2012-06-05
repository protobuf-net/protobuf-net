using System;
using NUnit.Framework;
using ProtoBuf.ServiceModel;

namespace Examples.Issues
{
    [TestFixture]
    public class SO10841807
    {
        [Test]
        public void Execute()
        {
            string aqn = typeof (ProtoBehaviorExtension).AssemblyQualifiedName;
            Assert.AreEqual("ProtoBuf.ServiceModel.ProtoBehaviorExtension, protobuf-net, Version=2.0.0.480, Culture=neutral, PublicKeyToken=257b51d87d2e4d67", aqn);
            Console.WriteLine("WCF AQN: " + aqn);
        }
    }
}
