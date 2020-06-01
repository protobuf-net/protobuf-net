#if FEAT_DYNAMIC_REF
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples
{
    
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


        [Fact]
        public void BasicReferenceTest()
        {
            var outer = new BasicReferenceTestOuter();
            var inner = new BasicReferenceTestInner();
            outer.Foo = outer.Bar = inner;

            var model = RuntimeTypeModel.Create();
            model.Add(typeof(BasicReferenceTestOuter), true);
            model.Add(typeof(BasicReferenceTestInner), true);

            Assert.NotNull(outer.Foo); //, "not null before");
            Assert.Same(outer.Foo, outer.Bar); //, "same before");

            var clone = (BasicReferenceTestOuter) model.DeepClone(outer);
            Assert.NotNull(clone); //, "clone exists (runtime)");
            Assert.NotSame(outer, clone); //, "clone is different (runtime)");
            Assert.NotNull(clone.Foo); //, "not null after (runtime)");
            Assert.Same(clone.Foo, clone.Bar); //, "same after (runtime)");

            model.CompileInPlace();
            clone = (BasicReferenceTestOuter)model.DeepClone(outer);
            Assert.NotNull(clone); //, "clone exists (compile in place)");
            Assert.NotSame(outer, clone); //, "clone is different (compile in place)");
            Assert.NotNull(clone.Foo); //, "not null after (compile in place)");
            Assert.Same(clone.Foo, clone.Bar); //, "same after (compile in place)");

            clone = (BasicReferenceTestOuter)model.Compile().DeepClone(outer);
            Assert.NotNull(clone); //, "clone exists (full compile)");
            Assert.NotSame(outer, clone); //, "clone is different (full compile)");
            Assert.NotNull(clone.Foo); //, "not null after (full compile)");
            Assert.Same(clone.Foo, clone.Bar); //, "same after (full compile)");
        }

        [Fact]
        public void RecursiveReferenceTest()
        {
            var outer = new BasicReferenceTestOuter();
            var inner = new BasicReferenceTestInner();
            inner.Self = inner;
            outer.Foo = inner;

            var model = RuntimeTypeModel.Create();
            model.Add(typeof(BasicReferenceTestOuter), true);
            model.Add(typeof(BasicReferenceTestInner), true);

            Assert.NotNull(outer.Foo); //, "not null before");
            Assert.Same(outer.Foo, outer.Foo.Self); //, "same before");

            var clone = (BasicReferenceTestOuter)model.DeepClone(outer);
            Assert.NotNull(clone); //, "clone exists (runtime)");
            Assert.NotSame(outer, clone); //, "clone is different (runtime)");
            Assert.NotNull(clone.Foo); //, "not null after (runtime)");
            Assert.Same(clone.Foo, clone.Foo.Self); //, "same after (runtime)");

            model.CompileInPlace();
            clone = (BasicReferenceTestOuter)model.DeepClone(outer);
            Assert.NotNull(clone); //, "clone exists (compile in place)");
            Assert.NotSame(outer, clone); //, "clone is different (compile in place)");
            Assert.NotNull(clone.Foo); //, "not null after (compile in place)");
            Assert.Same(clone.Foo, clone.Foo.Self); //, "same after (compile in place)");

            clone = (BasicReferenceTestOuter)model.Compile().DeepClone(outer);
            Assert.NotNull(clone); //, "clone exists (full compile)");
            Assert.NotSame(outer, clone); //, "clone is different (full compile)");
            Assert.NotNull(clone.Foo); //, "not null after (full compile)");
            Assert.Same(clone.Foo, clone.Foo.Self); //, "same after (full compile)");
        }

        [ProtoContract]
        class StringDynamicType
        {
	        [ProtoMember(1, DynamicType = true)]
	        public object Data { get; set; }
        }


        [Fact]
        public void StringAsDynamic()
        {
            var obj = new StringDynamicType { Data = GetString() };

            var clone = Serializer.DeepClone(obj);
            Assert.Equal(GetString(), clone.Data);
        }

        [Fact]
        public void StringNotInterned()
        {
            var model = RuntimeTypeModel.Create();
            model.InternStrings = false;

            var obj = new StringInternedType { Foo = GetString(), Bar = GetString() };
            Assert.False(ReferenceEquals(obj.Foo, obj.Bar));
            var clone = (StringInternedType)model.DeepClone(obj);
            Assert.Equal(obj.Foo, clone.Foo);
            Assert.Equal(obj.Bar, clone.Bar);
            Assert.False(ReferenceEquals(clone.Foo, clone.Bar));
        }

        [Fact]
        public void StringInterned()
        {
            var model = RuntimeTypeModel.Create();
            model.InternStrings = true;

            var obj = new StringInternedType { Foo = GetString(), Bar = GetString() };
            Assert.False(ReferenceEquals(obj.Foo, obj.Bar));
            var clone = (StringInternedType)model.DeepClone(obj);
            Assert.Equal(obj.Foo, clone.Foo);
            Assert.Equal(obj.Bar, clone.Bar);
            Assert.True(ReferenceEquals(clone.Foo, clone.Bar));
        }

        [Fact]
        public void StringAsReference()
        {
            var obj = new StringRefType { Foo = GetString(), Bar = GetString() };
            Assert.False(ReferenceEquals(obj.Foo, obj.Bar));
            var clone = Serializer.DeepClone(obj);
            Assert.Equal(obj.Foo, clone.Foo);
            Assert.Equal(obj.Bar, clone.Bar);
            Assert.True(ReferenceEquals(clone.Foo, clone.Bar));
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

        [Fact]
        public void BasicDynamicTest()
        {
            var outer = new BasicDynamicTestOuter();
            var inner = new BasicDynamicTestInner();
            outer.Foo = inner;

            var model = RuntimeTypeModel.Create();
            model.Add(typeof(BasicDynamicTestOuter), true);
            model.Add(typeof(BasicDynamicTestInner), true); // assume we can at least know candidates at runtime, for now

            Assert.NotNull(outer.Foo); //, "not null before");
            Assert.IsType<BasicDynamicTestInner>(outer.Foo); //, "typed before");

            var clone = (BasicDynamicTestOuter)model.DeepClone(outer);
            Assert.NotNull(clone); //, "clone exists (runtime)");
            Assert.NotSame(outer, clone); //, "clone is different (runtime)");
            Assert.NotNull(clone.Foo); //, "not null after (runtime)");
            Assert.IsType<BasicDynamicTestInner>(outer.Foo); //, "typed after (runtime)");

            model.CompileInPlace();
            clone = (BasicDynamicTestOuter)model.DeepClone(outer);
            Assert.NotNull(clone); //, "clone exists (compile in place)");
            Assert.NotSame(outer, clone); //, "clone is different (compile in place)");
            Assert.NotNull(clone.Foo); //, "not null after (compile in place)");
            Assert.IsType<BasicDynamicTestInner>(outer.Foo); //, "typed after (compile in place)");

            clone = (BasicDynamicTestOuter)model.Compile().DeepClone(outer);
            Assert.NotNull(clone); //, "clone exists (full compile)");
            Assert.NotSame(outer, clone); //, "clone is different (full compile)");
            Assert.NotNull(clone.Foo); //, "not null after (full compile)");
            Assert.IsType<BasicDynamicTestInner>(outer.Foo); //, "typed after (full compile)");
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

#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip = "unsupported scenario")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        // this is failing currently; needs to handle base-type via dynamictype
        public void TestUnknownDerivedType()
        {
            var obj = new Wrapper { Value = new Derived { Bar = 123, Foo = "abc" } };

            var clone = Serializer.DeepClone(obj);
            Assert.IsType<Derived>(clone.Value);
            Derived d = (Derived)clone.Value;
            Assert.Equal(123, d.Bar);
            Assert.Equal("abc", d.Foo);
        }

    }


    
}
#endif