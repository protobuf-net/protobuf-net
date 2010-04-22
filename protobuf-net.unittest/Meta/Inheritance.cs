using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Meta
{
    
    [TestFixture]
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

        [Test]
        public void CanCreateModel()
        {
            Assert.IsNotNull(CreateModel());
        }
        [Test]
        public void CanCompileModelInPlace()
        {
            CreateModel().CompileInPlace();
        }
        [Test]
        public void CanCompileModelFully()
        {
            CreateModel().Compile("InheritanceTests", "InheritanceTests.dll");
            PEVerify.Verify("InheritanceTests.dll");
        }
        [Test]
        public void CheckKeys()
        {
            var model = CreateModel();
            Assert.AreEqual(model.GetKey(typeof(SomeBase)), model.GetKey(typeof(SomeDerived)), "Runtime");

            TypeModel compiled = model.Compile();
            Assert.AreEqual(compiled.GetKey(typeof(SomeBase)), compiled.GetKey(typeof(SomeDerived)), "Compiled");
        }
        [Test]
        public void GetBackTheRightType_SomeBase()
        {
            var model = CreateModel();
            Assert.IsInstanceOfType(typeof(SomeBase), model.DeepClone(new SomeBase()), "Runtime");

            model.CompileInPlace();
            Assert.IsInstanceOfType(typeof(SomeBase), model.DeepClone(new SomeBase()), "In-Place");

            var compiled = model.Compile();
            Assert.IsInstanceOfType(typeof(SomeBase), compiled.DeepClone(new SomeBase()), "Compiled");
        }
        [Test]
        public void GetBackTheRightType_SomeDerived()
        {
            var model = CreateModel();
            Assert.IsInstanceOfType(typeof(SomeDerived), model.DeepClone(new SomeDerived()), "Runtime");

            model.CompileInPlace();
            Assert.IsInstanceOfType(typeof(SomeDerived), model.DeepClone(new SomeDerived()), "In-Place");

            var compiled = model.Compile();
            Assert.IsInstanceOfType(typeof(SomeDerived), compiled.DeepClone(new SomeDerived()), "Compiled");
        }

        [Test]
        public void GetBackTheRightType_AnotherDerived()
        {
            var model = CreateModel();
            Assert.IsInstanceOfType(typeof(AnotherDerived), model.DeepClone(new AnotherDerived()), "Runtime");

            model.CompileInPlace();
            Assert.IsInstanceOfType(typeof(AnotherDerived), model.DeepClone(new AnotherDerived()), "In-Place");

            var compiled = model.Compile();
            Assert.IsInstanceOfType(typeof(AnotherDerived), compiled.DeepClone(new AnotherDerived()), "Compiled");
        }

        [Test]
        public void GetBackTheRightType_NotInvolved()
        {
            var model = CreateModel();
            Assert.IsInstanceOfType(typeof(NotInvolved), model.DeepClone(new NotInvolved()), "Runtime");

            model.CompileInPlace();
            Assert.IsInstanceOfType(typeof(NotInvolved), model.DeepClone(new NotInvolved()), "In-Place");

            var compiled = model.Compile();
            Assert.IsInstanceOfType(typeof(NotInvolved), compiled.DeepClone(new NotInvolved()), "Compiled");
        }

    }

}
