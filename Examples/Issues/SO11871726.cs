using NUnit.Framework;
using ProtoBuf.Meta;
using System;
using System.Runtime.Serialization;

namespace Examples.Issues
{
    [TestFixture]
    public class SO11871726
    {
        [Test]
        public void ExecuteWithoutAutoAddProtoContractTypesOnlyShouldWork()
        {
            var model = TypeModel.Create();
            Assert.IsInstanceOfType(typeof(Foo), model.DeepClone(new Foo()));
        }
        [Test, ExpectedException(typeof(InvalidOperationException),
            ExpectedMessage = "Type is not expected, and no contract can be inferred: Examples.Issues.SO11871726+Foo")]
        public void ExecuteWithAutoAddProtoContractTypesOnlyShouldFail()
        {
            var model = TypeModel.Create();
            model.AutoAddProtoContractTypesOnly = true;
            Assert.IsInstanceOfType(typeof(Foo), model.DeepClone(new Foo()));
        }

        [DataContract]
        public class Foo { }
    }
}
