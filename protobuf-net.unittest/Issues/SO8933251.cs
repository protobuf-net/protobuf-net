using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO8933251
    {
        [Test]
        public void CheckTypeSpecificCompileInPlaceCascadesToBaseAndChildTypes()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model[typeof(B)].CompileInPlace();

            Assert.IsTrue(model.IsPrepared(typeof(B)), "B"); // self
            Assert.IsFalse(model.IsPrepared(typeof(D)), "D"); // sub-sub-type
            Assert.IsFalse(model.IsPrepared(typeof(C)), "C"); // sub-type
            Assert.IsFalse(model.IsPrepared(typeof(A)), "A"); // base-type
        }

        [Test]
        public void CheckGlobalCompileInPlaceCascadesToBaseAndChildTypes()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof (B), true); // give the model a clue!
            model.CompileInPlace();

            Assert.IsTrue(model.IsPrepared(typeof(D)), "D"); // sub-sub-type
            Assert.IsTrue(model.IsPrepared(typeof(C)), "C"); // sub-type
            Assert.IsTrue(model.IsPrepared(typeof(B)), "B"); // self
            Assert.IsTrue(model.IsPrepared(typeof(A)), "A"); // base-type
        }


        [ProtoContract, ProtoInclude(1, typeof(B))]
        public class A { }
        [ProtoContract, ProtoInclude(1, typeof(C))]
        public class B : A { }
        [ProtoContract, ProtoInclude(1, typeof(D))]
        public class C : B { }
        [ProtoContract]
        public class D : C { }
    }
}
