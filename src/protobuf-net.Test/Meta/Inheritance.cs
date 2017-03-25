#if !NO_INTERNAL_CONTEXT
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Meta
{
    
    
    public class InheritanceTests
    {
        public class SomeBase { public int A { get; set; } }
        public class SomeDerived : SomeBase { public int B { get; set; } }
        public class AnotherDerived : SomeBase { public int C { get; set; } }
        public class NotInvolved { public int D { get; set; } }
        public class AlsoNotInvolved { public int E { get; set; } }

        static RuntimeTypeModel CreateModel() {
            var model = TypeModel.Create();
            model[typeof(NotInvolved)].Add(1, "D");
            model[typeof(SomeBase)]
                .Add(1, "A")
                .AddSubType(2, typeof(SomeDerived))
                .AddSubType(3, typeof(AnotherDerived));
            model[typeof(SomeDerived)].Add(1, "B");
            model[typeof(AnotherDerived)].Add(1, "C");
            model[typeof(AlsoNotInvolved)].Add(1, "E");
            return model;
        }

        [Fact]
        public void CanCreateModel()
        {
            Assert.NotNull(CreateModel());
        }
        [Fact]
        public void CanCompileModelInPlace()
        {
            CreateModel().CompileInPlace();
        }
        [Fact]
        public void CanCompileModelFully()
        {
            CreateModel().Compile("InheritanceTests", "InheritanceTests.dll");
            PEVerify.Verify("InheritanceTests.dll", 0, false);
        }
        [Fact]
        public void CheckKeys()
        {
            var model = CreateModel();
            Type someBase = typeof(SomeBase), someDerived = typeof(SomeDerived);
            Assert.Equal(model.GetKey(ref someBase), model.GetKey(ref someDerived)); //, "Runtime");

            TypeModel compiled = model.Compile();
            Assert.Equal(compiled.GetKey(ref someBase), compiled.GetKey(ref someDerived)); //, "Compiled");
        }
        [Fact]
        public void GetBackTheRightType_SomeBase()
        {
            var model = CreateModel();
            Assert.IsType(typeof(SomeBase), model.DeepClone(new SomeBase())); //, "Runtime");

            model.CompileInPlace();
            Assert.IsType(typeof(SomeBase), model.DeepClone(new SomeBase())); //, "In-Place");

            var compiled = model.Compile();
            Assert.IsType(typeof(SomeBase), compiled.DeepClone(new SomeBase())); //, "Compiled");
        }
        [Fact]
        public void GetBackTheRightType_SomeDerived()
        {
            var model = CreateModel();
            Assert.IsType(typeof(SomeDerived), model.DeepClone(new SomeDerived())); //, "Runtime");

            model.CompileInPlace();
            Assert.IsType(typeof(SomeDerived), model.DeepClone(new SomeDerived())); //, "In-Place");

            var compiled = model.Compile();
            Assert.IsType(typeof(SomeDerived), compiled.DeepClone(new SomeDerived())); //, "Compiled");
        }

        [Fact]
        public void GetBackTheRightType_AnotherDerived()
        {
            var model = CreateModel();
            Assert.IsType(typeof(AnotherDerived), model.DeepClone(new AnotherDerived())); //, "Runtime");

            model.CompileInPlace();
            Assert.IsType(typeof(AnotherDerived), model.DeepClone(new AnotherDerived())); //, "In-Place");

            var compiled = model.Compile();
            Assert.IsType(typeof(AnotherDerived), compiled.DeepClone(new AnotherDerived())); //, "Compiled");
        }

        [Fact]
        public void GetBackTheRightType_NotInvolved()
        {
            var model = CreateModel();
            Assert.IsType(typeof(NotInvolved), model.DeepClone(new NotInvolved())); //, "Runtime");

            model.CompileInPlace();
            Assert.IsType(typeof(NotInvolved), model.DeepClone(new NotInvolved())); //, "In-Place");

            var compiled = model.Compile();
            Assert.IsType(typeof(NotInvolved), compiled.DeepClone(new NotInvolved())); //, "Compiled");
        }

    }

}
#endif