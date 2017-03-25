#if !NO_INTERNAL_CONTEXT
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class SO8933251
    {
        [Fact]
        public void CheckTypeSpecificCompileInPlaceCascadesToBaseAndChildTypes()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model[typeof(B)].CompileInPlace();

            Assert.True(model.IsPrepared(typeof(B)), "B"); // self
            Assert.False(model.IsPrepared(typeof(D)), "D"); // sub-sub-type
            Assert.False(model.IsPrepared(typeof(C)), "C"); // sub-type
            Assert.False(model.IsPrepared(typeof(A)), "A"); // base-type
        }

        [Fact]
        public void CheckGlobalCompileInPlaceCascadesToBaseAndChildTypes()
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof (B), true); // give the model a clue!
            model.CompileInPlace();

            Assert.True(model.IsPrepared(typeof(D)), "D"); // sub-sub-type
            Assert.True(model.IsPrepared(typeof(C)), "C"); // sub-type
            Assert.True(model.IsPrepared(typeof(B)), "B"); // self
            Assert.True(model.IsPrepared(typeof(A)), "A"); // base-type
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
#endif