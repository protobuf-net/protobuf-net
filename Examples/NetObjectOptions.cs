using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples
{
    [TestFixture]
    public class NetObjectOptions
    {
        [ProtoContract]
        public class BasicReferenceTestOuter
        {
            [ProtoMember(1, AsReference=true)]
            public BasicReferenceTestInner Foo { get; set; }
            [ProtoMember(2, AsReference = true)]
            public BasicReferenceTestInner Bar { get; set; }

            
        }
        [ProtoContract]
        public class BasicReferenceTestInner {
            [ProtoMember(1, AsReference = true)]
            public BasicReferenceTestInner Self { get; set; }
        }


        [Test]
        public void BasicReferenceTest()
        {
            var outer = new BasicReferenceTestOuter();
            var inner = new BasicReferenceTestInner();
            outer.Foo = outer.Bar = inner;

            var model = TypeModel.Create();
            model.Add(typeof(BasicReferenceTestOuter), true);
            model.Add(typeof(BasicReferenceTestInner), true);

            Assert.IsNotNull(outer.Foo, "not null before");
            Assert.AreSame(outer.Foo, outer.Bar, "same before");

            var clone = (BasicReferenceTestOuter) model.DeepClone(outer);
            Assert.IsNotNull(clone, "clone exists (runtime)");
            Assert.AreNotSame(outer, clone, "clone is different (runtime)");
            Assert.IsNotNull(clone.Foo, "not null after (runtime)");
            Assert.AreSame(clone.Foo, clone.Bar, "same after (runtime)");

            model.CompileInPlace();
            clone = (BasicReferenceTestOuter)model.DeepClone(outer);
            Assert.IsNotNull(clone, "clone exists (compile in place)");
            Assert.AreNotSame(outer, clone, "clone is different (compile in place)");
            Assert.IsNotNull(clone.Foo, "not null after (compile in place)");
            Assert.AreSame(clone.Foo, clone.Bar, "same after (compile in place)");

            clone = (BasicReferenceTestOuter)model.Compile().DeepClone(outer);
            Assert.IsNotNull(clone, "clone exists (full compile)");
            Assert.AreNotSame(outer, clone, "clone is different (full compile)");
            Assert.IsNotNull(clone.Foo, "not null after (full compile)");
            Assert.AreSame(clone.Foo, clone.Bar, "same after (full compile)");
        }

        [Test]
        public void RecursiveReferenceTest()
        {
            var outer = new BasicReferenceTestOuter();
            var inner = new BasicReferenceTestInner();
            inner.Self = inner;
            outer.Foo = inner;

            var model = TypeModel.Create();
            model.Add(typeof(BasicReferenceTestOuter), true);
            model.Add(typeof(BasicReferenceTestInner), true);

            Assert.IsNotNull(outer.Foo, "not null before");
            Assert.AreSame(outer.Foo, outer.Foo.Self, "same before");

            var clone = (BasicReferenceTestOuter)model.DeepClone(outer);
            Assert.IsNotNull(clone, "clone exists (runtime)");
            Assert.AreNotSame(outer, clone, "clone is different (runtime)");
            Assert.IsNotNull(clone.Foo, "not null after (runtime)");
            Assert.AreSame(clone.Foo, clone.Foo.Self, "same after (runtime)");

            model.CompileInPlace();
            clone = (BasicReferenceTestOuter)model.DeepClone(outer);
            Assert.IsNotNull(clone, "clone exists (compile in place)");
            Assert.AreNotSame(outer, clone, "clone is different (compile in place)");
            Assert.IsNotNull(clone.Foo, "not null after (compile in place)");
            Assert.AreSame(clone.Foo, clone.Foo.Self, "same after (compile in place)");

            clone = (BasicReferenceTestOuter)model.Compile().DeepClone(outer);
            Assert.IsNotNull(clone, "clone exists (full compile)");
            Assert.AreNotSame(outer, clone, "clone is different (full compile)");
            Assert.IsNotNull(clone.Foo, "not null after (full compile)");
            Assert.AreSame(clone.Foo, clone.Foo.Self, "same after (full compile)");
        }

        [ProtoContract]
        class StringDynamicType
        {
	        [ProtoMember(1, DynamicType = true)]
	        public object Data { get; set; }
        }


        [Test]
        public void StringAsDynamic()
        {
            var obj = new StringDynamicType { Data = GetString() };

            var clone = Serializer.DeepClone(obj);
            Assert.AreEqual(GetString(), clone.Data);
        }

        [Test]
        public void StringInterned()
        {
            var obj = new StringInternedType { Foo = GetString(), Bar = GetString() };
            Assert.IsFalse(ReferenceEquals(obj.Foo, obj.Bar));
            var clone = Serializer.DeepClone(obj);
            Assert.AreEqual(obj.Foo, clone.Foo);
            Assert.AreEqual(obj.Bar, clone.Bar);
            Assert.IsTrue(ReferenceEquals(clone.Foo, clone.Bar));
        }

        [Test]
        public void StringAsReference()
        {
            var obj = new StringRefType { Foo = GetString(), Bar = GetString() };
            Assert.IsFalse(ReferenceEquals(obj.Foo, obj.Bar));
            var clone = Serializer.DeepClone(obj);
            Assert.AreEqual(obj.Foo, clone.Foo);
            Assert.AreEqual(obj.Bar, clone.Bar);
            Assert.IsTrue(ReferenceEquals(clone.Foo, clone.Bar));
        }
        static string GetString()
        {
            return new string('a', 5);
        }
        [ProtoContract]
        public class StringRefType
        {
            [ProtoMember(1, DynamicType=true, AsReference=true)]
            public object Foo { get; set; }

            [ProtoMember(2, DynamicType = true, AsReference = true)]
            public object Bar { get; set; }
        }
        [ProtoContract]
        public class StringInternedType
        {
            [ProtoMember(1)]
            public string Foo { get; set; }

            [ProtoMember(2)]
            public string Bar { get; set; }
        }

        [ProtoContract]
        public class BasicDynamicTestOuter
        {
            [ProtoMember(1, DynamicType = true)]
            public object Foo { get; set; }
        }
        [ProtoContract]
        public class BasicDynamicTestInner { }

        [Test]
        public void BasicDynamicTest()
        {
            var outer = new BasicDynamicTestOuter();
            var inner = new BasicDynamicTestInner();
            outer.Foo = inner;

            var model = TypeModel.Create();
            model.Add(typeof(BasicDynamicTestOuter), true);
            model.Add(typeof(BasicDynamicTestInner), true); // assume we can at least know candidates at runtime, for now

            Assert.IsNotNull(outer.Foo, "not null before");
            Assert.IsInstanceOfType(typeof(BasicDynamicTestInner), outer.Foo, "typed before");

            var clone = (BasicDynamicTestOuter)model.DeepClone(outer);
            Assert.IsNotNull(clone, "clone exists (runtime)");
            Assert.AreNotSame(outer, clone, "clone is different (runtime)");
            Assert.IsNotNull(clone.Foo, "not null after (runtime)");
            Assert.IsInstanceOfType(typeof(BasicDynamicTestInner), outer.Foo, "typed after (runtime)");

            model.CompileInPlace();
            clone = (BasicDynamicTestOuter)model.DeepClone(outer);
            Assert.IsNotNull(clone, "clone exists (compile in place)");
            Assert.AreNotSame(outer, clone, "clone is different (compile in place)");
            Assert.IsNotNull(clone.Foo, "not null after (compile in place)");
            Assert.IsInstanceOfType(typeof(BasicDynamicTestInner), outer.Foo, "typed after (compile in place)");

            clone = (BasicDynamicTestOuter)model.Compile().DeepClone(outer);
            Assert.IsNotNull(clone, "clone exists (full compile)");
            Assert.AreNotSame(outer, clone, "clone is different (full compile)");
            Assert.IsNotNull(clone.Foo, "not null after (full compile)");
            Assert.IsInstanceOfType(typeof(BasicDynamicTestInner), outer.Foo, "typed after (full compile)");
        }



        [ProtoContract]
        abstract class BaseType
        {
            [ProtoMember(1)]
            public string Foo { get; set; }
        }
        [ProtoContract]
        class Derived : BaseType
        {
            [ProtoMember(1)]
            public int Bar { get; set; }
        }
        [ProtoContract]
        class Wrapper
        {
            [ProtoMember(1, DynamicType = true)]
            public object Value { get; set; }
        }
        [Test] // this is failing currently; needs to handle base-type via dynamictype
        public void TestUnknownDerivedType()
        {
            var obj = new Wrapper { Value = new Derived { Bar = 123, Foo = "abc" } };

            var clone = Serializer.DeepClone(obj);
            Assert.IsInstanceOfType(typeof(Derived), clone.Value);
            Derived d = (Derived)clone.Value;
            Assert.AreEqual(123, d.Bar);
            Assert.AreEqual("abc", d.Foo);
        }

    }


    
}
