using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO17040488
    {
        [ProtoContract(UseProtoMembersOnly = true)]
        public class ProtoObjectDTO
        {
            [ProtoMember(1, DynamicType = true)]
            public object Value { get; set; }
            [ProtoMember(2)]
            public int Order { get; set; }
        }

        [ProtoContract]
        public class Foo
        {
            [ProtoMember(3)]
            public int A { get; set; }
        }
        [ProtoContract]
        public class Bar
        {
            [ProtoMember(4)]
            public string B { get; set; }
        }

        public void Execute()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;

            Execute(model, "Runtime");
            model.CompileInPlace();
            Execute(model, "CompileInPlace");
            Execute(model.Compile(), "Compile");
            model.Compile("SO17040488", "SO17040488.dll");
            PEVerify.AssertValid("SO17040488.dll");

        }

        private void Execute(TypeModel model, string caption)
        {
            var args = new[] {
                new ProtoObjectDTO { Order = 1, Value = new Foo { A = 123 }},
                new ProtoObjectDTO { Order = 2, Value = new Bar { B = "abc" }},
            };
            var clone = (ProtoObjectDTO[])model.DeepClone(args);
            Assert.AreEqual(2, clone.Length, caption + ":length");
            Assert.AreEqual(1, clone[0].Order, caption + ":order");
            Assert.AreEqual(2, clone[1].Order, caption + ":order");
            Assert.IsInstanceOfType(typeof(Foo), clone[0].Value, caption + ":type");
            Assert.IsInstanceOfType(typeof(Bar), clone[1].Value, caption + ":type");
            Assert.AreEqual(123, ((Foo)clone[0].Value).A, caption + ":value");
            Assert.AreEqual("abc", ((Bar)clone[1].Value).B, caption + ":value");
        }
    }
}
