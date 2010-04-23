using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf.Meta;

namespace ProtoBuf.unittest.Meta
{
    [TestFixture]
    public class AbstractListsWithInheritance
    {

        public abstract class SomeBaseType { public int BaseProp { get; set; } }
        public class ConcreteA : SomeBaseType { public int A { get; set; } }
        public class ConcreteB : SomeBaseType { public int B { get; set; } }
        public sealed class ConcreteC : ConcreteA { public int C { get; set; } }

        /* exists to expose different list types in a single type */
        public class Wrapper {
            public List<SomeBaseType> BaseList {get;set;}
            public List<ConcreteA> AList {get;set;}
            public List<ConcreteC> CList {get;set;}
            public IList<ConcreteA> AbstractAList { get; set; }
        }
        public class AList : List<ConcreteA> { }
        public static RuntimeTypeModel BuildModel()
        {
            var model = TypeModel.Create();
            model.Add(typeof(ConcreteC), false).Add("C");
            model.Add(typeof(ConcreteB), false).Add("B");
            model.Add(typeof(ConcreteA), false).Add("A").AddSubType(2, typeof(ConcreteC));
            model.Add(typeof(SomeBaseType), false).Add("BaseProp")
                .AddSubType(2, typeof(ConcreteA)).AddSubType(3, typeof(ConcreteB));
            model.Add(typeof(Wrapper), false).Add("BaseList", "AList", "CList")
                .Add(4, "AbstractAList", null, typeof(AList));
            return model;
        }
        [Test]
        public void CanBuildAndCompileModel()
        {
            BuildModel().Compile("AbstractListsWithInheritance", "AbstractListsWithInheritance.dll");
            PEVerify.Verify("AbstractListsWithInheritance.dll");
        }

        [Test]
        public void TestIntermediateListType()
        {
            var model = BuildModel();
            Wrapper wrap = new Wrapper
            {
                AList = new List<ConcreteA> {
                new ConcreteA { A = 12, BaseProp = 34},
                new ConcreteC { A = 56, BaseProp = 78, C = 90 }
            }
            }, clone;
            VerifyWrapperVerifyIntermediateResult(wrap, "Original");

            clone = (Wrapper)model.DeepClone(wrap);
            VerifyWrapperVerifyIntermediateResult(clone, "Runtime");

            model.CompileInPlace();
            clone = (Wrapper)model.DeepClone(wrap);
            VerifyWrapperVerifyIntermediateResult(clone, "CompileInPlace");

            clone = (Wrapper)model.Compile().DeepClone(wrap);
            VerifyWrapperVerifyIntermediateResult(clone, "Compile");
        }

        [Test]
        public void TestIntermediateAbstractListType()
        {
            var model = BuildModel();
            Wrapper wrap = new Wrapper
            {
                AbstractAList = new AList {
                new ConcreteA { A = 12, BaseProp = 34},
                new ConcreteC { A = 56, BaseProp = 78, C = 90 }
            }
            }, clone;
            VerifyWrapperVerifyAbstractIntermediateResult(wrap, "Original");

            clone = (Wrapper)model.DeepClone(wrap);
            VerifyWrapperVerifyAbstractIntermediateResult(clone, "Runtime");

            model.CompileInPlace();
            clone = (Wrapper)model.DeepClone(wrap);
            VerifyWrapperVerifyAbstractIntermediateResult(clone, "CompileInPlace");

            clone = (Wrapper)model.Compile().DeepClone(wrap);
            VerifyWrapperVerifyAbstractIntermediateResult(clone, "Compile");
        }

        void VerifyWrapperVerifyIntermediateResult(Wrapper wrapper, string message)
        {
            Assert.IsNotNull(wrapper, message + " wrapper");
            Assert.IsNull(wrapper.BaseList, message + " BaseList");
            Assert.IsNull(wrapper.CList, message + " CList");
            Assert.IsNull(wrapper.AbstractAList, message + " AbstractAList");
            Assert.IsNotNull(wrapper.AList, message + " AList");
            Assert.AreEqual(2, wrapper.AList.Count);
            Assert.AreEqual(typeof(ConcreteA), wrapper.AList[0].GetType());
            Assert.AreEqual(12, wrapper.AList[0].A);
            Assert.AreEqual(34, wrapper.AList[0].BaseProp);
            Assert.AreEqual(typeof(ConcreteC), wrapper.AList[1].GetType());
            Assert.AreEqual(56, wrapper.AList[1].A);
            Assert.AreEqual(78, wrapper.AList[1].BaseProp);
            Assert.AreEqual(90, ((ConcreteC)wrapper.AList[1]).C);
        }

        void VerifyWrapperVerifyAbstractIntermediateResult(Wrapper wrapper, string message)
        {
            Assert.IsNotNull(wrapper, message + " wrapper");
            Assert.IsNull(wrapper.BaseList, message + " BaseList");
            Assert.IsNull(wrapper.CList, message + " CList");
            Assert.IsNull(wrapper.AList, message + " AList");
            Assert.IsNotNull(wrapper.AbstractAList, message + " AbstractAList");
            Assert.AreEqual(typeof(AList), wrapper.AbstractAList.GetType(), message + " AbstractAList");
            Assert.AreEqual(2, wrapper.AbstractAList.Count);
            Assert.AreEqual(typeof(ConcreteA), wrapper.AbstractAList[0].GetType());
            Assert.AreEqual(12, wrapper.AbstractAList[0].A);
            Assert.AreEqual(34, wrapper.AbstractAList[0].BaseProp);
            Assert.AreEqual(typeof(ConcreteC), wrapper.AbstractAList[1].GetType());
            Assert.AreEqual(56, wrapper.AbstractAList[1].A);
            Assert.AreEqual(78, wrapper.AbstractAList[1].BaseProp);
            Assert.AreEqual(90, ((ConcreteC)wrapper.AbstractAList[1]).C);
        }
    }
}
