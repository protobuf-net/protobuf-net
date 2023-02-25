using ProtoBuf.Meta;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class Issue964
    {
        [Fact]
        public void CannotAddFieldsToAutoTuple()
        {
            var mt = RuntimeTypeModel.Create().Add(typeof(TestClass), applyDefaultBehaviour: true);
            var ioe = Assert.Throws<InvalidOperationException>(() =>
            {
                mt.Add(1, nameof(TestClass.Expiry));
            });
            Assert.Equal("This operation is not supported for tuple-like types; to disable tuple-like type discovery, use applyDefaultBehaviour: false when first adding the type to the model.", ioe.Message);
        }

        [Fact]
        public void CanAddFieldsToAutoTupleWithDiscoveryDisabled()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TestClass), applyDefaultBehaviour: false)
                .Add(1, nameof(TestClass.Expiry))
                .Add(2, nameof(TestClass.Value));
        }

        [Fact]
        public void AssertDataEquivalent()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(TestClass), applyDefaultBehaviour: false)
                .Add(1, nameof(TestClass.Expiry))
                .Add(2, nameof(TestClass.Value));

            var expiry = new DateTime(2022, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var memoryStreamA = new MemoryStream();
            model.Serialize(memoryStreamA, new TestClass("some data", expiry));
            var memoryStreamB = new MemoryStream();
            model.Serialize(memoryStreamB, new ContractClass("some data", expiry));

            memoryStreamA.Seek(0, SeekOrigin.Begin);
            memoryStreamB.Seek(0, SeekOrigin.Begin);

            var a = memoryStreamA.ToArray();
            var b = memoryStreamB.ToArray();

            Assert.True(a.SequenceEqual(b));
        }


        class TestClass
        {
            public DateTime Expiry { get; }
            public string Value { get; }

            public TestClass(string value, DateTime expiry)
            {
                Value = value;
                Expiry = expiry;
            }
        }

        [ProtoContract]
        class ContractClass
        {
            [ProtoMember(1)]
            public DateTime Expiry { get; }
            [ProtoMember(2)]
            public string Value { get; }

            public ContractClass(string value, DateTime expiry)
            {
                Value = value;
                Expiry = expiry;
            }
        }
    }
}
