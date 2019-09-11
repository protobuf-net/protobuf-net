using Xunit;
using ProtoBuf.Meta;
using System;
using System.Runtime.Serialization;

namespace Examples.Issues
{
    
    public class SO11871726
    {
        [Fact]
        public void ExecuteWithoutAutoAddProtoContractTypesOnlyShouldWork()
        {
            var model = RuntimeTypeModel.Create();
            Assert.IsType<Foo>(model.DeepClone(new Foo()));
        }
        [Fact]
        public void ExecuteWithAutoAddProtoContractTypesOnlyShouldFail()
        {
            Program.ExpectFailure<InvalidOperationException>(() =>
            {
                var model = RuntimeTypeModel.Create();
                model.AutoAddProtoContractTypesOnly = true;
                Assert.IsType<Foo>(model.DeepClone(new Foo()));
            }, "Type is not expected, and no contract can be inferred: Examples.Issues.SO11871726+Foo");
        }

        [DataContract]
        public class Foo { }
    }
}
