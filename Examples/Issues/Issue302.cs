using System;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue302
    {
        [Test]
        public void RoundTripUInt32EnumValue()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            var foo = new Foo {Value = StateEnum.Deleted};

            var clone = (Foo)model.DeepClone(foo);
            Assert.AreEqual(StateEnum.Deleted, clone.Value, "Runtime");

            model.Compile("Issue302", "Issue302.dll");
            PEVerify.AssertValid("Issue302.dll");

            model.CompileInPlace();
            clone = (Foo)model.DeepClone(foo);
            Assert.AreEqual(StateEnum.Deleted, clone.Value, "CompileInPlace");

            clone = (Foo)model.Compile().DeepClone(foo);
            Assert.AreEqual(StateEnum.Deleted, clone.Value, "Compile");
        }

        [ProtoContract]
        public class Foo
        {
            [ProtoMember(1)]
            public StateEnum Value { get; set; }
        }

        [Flags]
        public enum StateEnum : uint
        {
            Active = 0x00000001,
            Acknowledged = 0x00000002,
            Deleted = 0x80000000
        }
    }
}
