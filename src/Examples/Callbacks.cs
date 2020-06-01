using System.IO;
using System.Threading;
using Xunit;
using ProtoBuf;
using System.Runtime.Serialization;
using System;
using ProtoBuf.Meta;
namespace Examples
{
    [ProtoContract]
    public class CallbackSimple : ICallbackTest
    {
        [ProtoMember(1)]
        public string Bar { get; set;}

        [OnDeserialized]
        public void OnDeserialized() { History += ";OnDeserialized"; }
        [OnDeserializing]
        public void OnDeserializing() { History += ";OnDeserializing"; }
        [OnSerialized]
        public void OnSerialized() { History += ";OnSerialized"; }
        [OnSerializing]
        public void OnSerializing() { History += ";OnSerializing"; }
        public CallbackSimple() { History = "ctor"; }
        public string History { get; private set; }
    }

    
    [ProtoContract]
    [ProtoInclude(1, typeof(TestInheritedImplementedAtRootDerived))]
    abstract class TestInheritedImplementedAtRoot : ICallbackTest
    {
        
        protected abstract string BarCore { get; set;}
        string ICallbackTest.Bar {get { return BarCore;} set { BarCore = value;}}

        [OnDeserialized]
        void OnDeserialized() { History += ";OnDeserialized"; }
        [OnDeserializing]
        void OnDeserializing() { History += ";OnDeserializing"; }
        [OnSerialized]
        void OnSerialized() { History += ";OnSerialized"; }
        [OnSerializing]
        void OnSerializing() { History += ";OnSerializing"; }
        protected TestInheritedImplementedAtRoot() { History = "ctor"; }
        public string History { get; private set; }
    }

    [ProtoContract]
    class TestInheritedImplementedAtRootDerived : TestInheritedImplementedAtRoot
    {
        protected override string BarCore
        {
            get { return Bar; }
            set { Bar = value; }
        }
        [ProtoMember(1)]
        public string Bar { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(1, typeof (TestInheritedVirtualAtRootDerived))]
    internal abstract class TestInheritedVirtualAtRoot : ICallbackTest
    {

        protected abstract string BarCore { get; set; }
        string ICallbackTest.Bar { get { return BarCore; } set { BarCore = value; } }

        [OnDeserialized]
        protected abstract void OnDeserialized();

        [OnDeserializing]
        protected abstract void OnDeserializing();

        [OnSerialized]
        protected abstract void OnSerialized();

        [OnSerializing]
        protected abstract void OnSerializing();

        protected TestInheritedVirtualAtRoot() { History = "ctor"; }
        public string History { get; protected set; }
    }

    [ProtoContract]
    class TestInheritedVirtualAtRootDerived : TestInheritedVirtualAtRoot
    {
        protected override string BarCore
        {
            get { return Bar; }
            set { Bar = value; }
        }
        [ProtoMember(1)]
        public string Bar { get; set; }

        protected override void OnDeserialized() {History += ";OnDeserialized";}
        protected override void OnSerialized() { History += ";OnSerialized"; }
        protected override void OnDeserializing() { History += ";OnDeserializing"; }
        protected override void OnSerializing() { History += ";OnSerializing"; }
    }

    [ProtoContract]
    [ProtoInclude(1, typeof(TestInheritedVirtualAtRootDerivedProtoAttribs))]
    internal abstract class TestInheritedVirtualAtRootProtoAttribs : ICallbackTest
    {

        protected abstract string BarCore { get; set; }
        string ICallbackTest.Bar { get { return BarCore; } set { BarCore = value; } }

        [ProtoAfterDeserialization]
        protected abstract void OnDeserialized();

        [ProtoBeforeDeserialization]
        protected abstract void OnDeserializing();

        [ProtoAfterSerialization]
        protected abstract void OnSerialized();

        [ProtoBeforeSerialization]
        protected abstract void OnSerializing();

        protected TestInheritedVirtualAtRootProtoAttribs() { History = "ctor"; }
        public string History { get; protected set; }
    }

    [ProtoContract]
    class TestInheritedVirtualAtRootDerivedProtoAttribs : TestInheritedVirtualAtRootProtoAttribs
    {
        protected override string BarCore
        {
            get { return Bar; }
            set { Bar = value; }
        }
        [ProtoMember(1)]
        public string Bar { get; set; }

        protected override void OnDeserialized() { History += ";OnDeserialized"; }
        protected override void OnSerialized() { History += ";OnSerialized"; }
        protected override void OnDeserializing() { History += ";OnDeserializing"; }
        protected override void OnSerializing() { History += ";OnSerializing"; }
    }



    [ProtoContract]
    [ProtoInclude(1, typeof(TestInheritedImplementedAtChildDerived))]
    abstract class TestInheritedImplementedAtChild : ICallbackTest
    {

        protected abstract string BarCore { get; set; }
        string ICallbackTest.Bar { get { return BarCore; } set { BarCore = value; } }

        protected TestInheritedImplementedAtChild() { History = "ctor"; }
        public string History { get; protected set; }

        [OnDeserialized]
        protected virtual void OnDeserialized() { }
        [OnDeserializing]
        protected virtual void OnDeserializing() { }
        [OnSerialized]
        protected virtual void OnSerialized() { }
        [OnSerializing]
        protected virtual void OnSerializing() { }
    }

    [ProtoContract]
    class TestInheritedImplementedAtChildDerived : TestInheritedImplementedAtChild
    {
        
        protected override void OnDeserialized() { History += ";OnDeserialized"; }

        protected override void OnDeserializing() { History += ";OnDeserializing"; }

        protected override void OnSerialized() { History += ";OnSerialized"; }

        protected override void OnSerializing() { History += ";OnSerializing"; }

        protected override string BarCore
        {
            get { return Bar; }
            set { Bar = value; }
        }
        [ProtoMember(1)]
        public string Bar { get; set; }
    }

    public interface ICallbackTest
    {
        string History { get; }
        string Bar { get; set; }
    }

    
    public class Callbacks
    {
        private static void Test<T, TCreate>(bool compile = false, params Type[] extraTypes)
            where TCreate : T, new()
            where T : ICallbackTest
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof (TCreate), true);
            if(extraTypes != null)
            {
                for (int i = 0; i < extraTypes.Length; i++) model.Add(extraTypes[i], true);
            }
            model.AutoCompile = false;
            
            Test<T, TCreate>(model, "Runtime");

            if(compile)
            {
                string name = typeof (TCreate).FullName + "Ser";
                model.Compile(name, name + ".dll");
                PEVerify.AssertValid(name + ".dll");
            }

            model.CompileInPlace();
            Test<T, TCreate>(model, "CompileInPlace");

            if (compile)
            {
                Test<T, TCreate>(model.Compile(), "Compile"); // <===== lots of private members etc
            }
        }
#pragma warning disable xUnit2002, xUnit2005 // it is convinced that TCreate is a value-type
#pragma warning disable IDE0060 // Remove unused parameter
        static void Test<T, TCreate>(TypeModel model, string mode)
#pragma warning restore IDE0060 // Remove unused parameter
            where TCreate : T, new()
            where T : ICallbackTest
        {
            //try
            {
                // mode = ":" + mode;
                TCreate cs = new TCreate
                {
                    Bar = "abc"
                };
                string ctorExpected = typeof(TCreate)._IsValueType() ? null : "ctor";
                Assert.NotNull(cs); //, "orig" + mode);
                Assert.Equal(ctorExpected, cs.History); //, "orig before" + mode);
                Assert.Equal("abc", cs.Bar); //, "orig before" + mode);

                TCreate clone = (TCreate)model.DeepClone<TCreate>(cs);
                if (!typeof(TCreate)._IsValueType())
                {
                    Assert.Equal(ctorExpected + ";OnSerializing;OnSerialized", cs.History); //, "orig after" + mode);
                }
                Assert.Equal("abc", cs.Bar); //, "orig after" + mode);

                Assert.NotNull(clone); //, "clone" + mode);
                Assert.NotSame(cs, clone); //, "clone" + mode);

                Assert.Equal(ctorExpected + ";OnDeserializing;OnDeserialized", clone.History); //, "clone after" + mode);
                Assert.Equal("abc", clone.Bar); //, "clone after" + mode);

                T clone2 = (T)model.DeepClone(cs);
                if (!typeof(TCreate)._IsValueType())
                {
                    Assert.Equal(ctorExpected + ";OnSerializing;OnSerialized;OnSerializing;OnSerialized", cs.History); //, "orig after" + mode);
                }
                Assert.Equal("abc", cs.Bar); //, "orig after" + mode);

                Assert.NotNull(clone2); //, "clone2" + mode);
                Assert.NotSame(cs, clone2); //, "clone2" + mode);
                Assert.Equal(ctorExpected + ";OnDeserializing;OnDeserialized", clone2.History); //, "clone2 after" + mode);
                Assert.Equal("abc", clone2.Bar); //, "clone2 after" + mode);
            }
            //catch(Exception ex)
            //{
            //    throw new InvalidOperationException(mode, ex);
            //}
            
        }
#pragma warning restore xUnit2002, xUnit2005

        [ProtoContract]
        class DuplicateCallbacks
        {
#pragma warning disable IDE0051 // Remove unused private members
            [ProtoBeforeSerialization]
            void Foo() {}

            [ProtoBeforeSerialization]
            void Bar() { }
#pragma warning restore IDE0051 // Remove unused private members
        }

        [Fact]
        public void TestSimple()
        {
            Test<CallbackSimple, CallbackSimple>();
        }

        [Fact]
        public void TestStructSimple_Basic()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(CallbackStructSimple), true);
            model.Add(typeof(CallbackSimple), true);
            var compiled = model.Compile("TestStructSimple", "TestStructSimple.dll");
            PEVerify.AssertValid("TestStructSimple.dll");

            var obj = new CallbackStructSimple();
            var clone = compiled.DeepClone(obj);
            Assert.Equal(";OnDeserializing;OnDeserialized", clone.History);
        }

        [Fact]
        public void TestStructSimple()
        {
            int beforeSer = CallbackStructSimple.BeforeSerializeCount,
                afterSer = CallbackStructSimple.AfterSerializeCount;
            Test<CallbackStructSimple, CallbackStructSimple>(true, typeof(CallbackStructSimpleNoCallbacks));

            Assert.Equal(6, CallbackStructSimple.BeforeSerializeCount - beforeSer);
            Assert.Equal(6, CallbackStructSimple.AfterSerializeCount - afterSer);
        } 

        [Fact]
        public void TestInheritedVirtualAtRoot()
        {
            Test<TestInheritedVirtualAtRoot, TestInheritedVirtualAtRootDerived>();
        }

        [Fact]
        public void TestInheritedVirtualAtRootProtoAttribs()
        {
            Test<TestInheritedVirtualAtRootProtoAttribs, TestInheritedVirtualAtRootDerivedProtoAttribs>();
        }

        [Fact]
        public void TestInheritedImplementedAtRoot()
        {
            Test<TestInheritedImplementedAtRoot, TestInheritedImplementedAtRootDerived>();
        }

        [Fact] /* now supported */
        public void TestInheritedImplementedAtChild()
        {
            Test<TestInheritedImplementedAtChild, TestInheritedImplementedAtChildDerived>();
        }

        [Fact]
        public void TestDuplicateCallbacks()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                Serializer.Serialize(Stream.Null, new DuplicateCallbacks());
            }, "Duplicate ProtoBuf.ProtoBeforeSerializationAttribute callbacks on Examples.Callbacks+DuplicateCallbacks");
        }

        [Fact]
        public void CallbacksWithContext()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            Test(model);

            model.CompileInPlace();
            Test(model);

            Test(model.Compile());
        }
        static void Test(TypeModel model)
        {
            CallbackWrapper orig = new CallbackWrapper
            {
                A = new CallbackWithNoContext(),
                B = new CallbackWithProtoContext()
#if REMOTING
                ,C = new CallbackWithRemotingContext()
#endif
            }, clone;
            Assert.Null(orig.B.ReadState);
            Assert.Null(orig.B.WriteState);
#if REMOTING
            Assert.Null(orig.C.ReadState);
            Assert.Null(orig.C.WriteState);
#endif
            using var ms = new MemoryStream();
            SerializationContext ctx = new SerializationContext { Context = new object()};
            model.Serialize(ms, orig, context: ctx);
            Assert.Null(orig.B.ReadState);
            Assert.Same(ctx.Context, orig.B.WriteState);
#if REMOTING
            Assert.Null(orig.C.ReadState);
            Assert.Same(ctx.Context, orig.C.WriteState);
#endif
            ms.Position = 0;
            ctx = new SerializationContext { Context = new object() };
#pragma warning disable CS0618
            clone = (CallbackWrapper)model.Deserialize(ms, null, typeof(CallbackWrapper), -1, ctx);
#pragma warning restore CS0618
            Assert.Same(ctx.Context, clone.B.ReadState);
            Assert.Null(clone.B.WriteState);
#if REMOTING
            Assert.Same(ctx.Context, clone.C.ReadState);
            Assert.Null(clone.C.WriteState);
#endif
        }
        [ProtoContract]
        public class CallbackWrapper
        {
            [ProtoMember(1)]
            public CallbackWithNoContext A { get; set; }
            [ProtoMember(2)]
            public CallbackWithProtoContext B { get; set; }
#if REMOTING
            [ProtoMember(3)]
            public CallbackWithRemotingContext C { get; set; }
#endif
        }
        [ProtoContract]
        public class CallbackWithNoContext
        {
            [OnDeserialized]
            public void OnDeserialized()
            {}
        }

        [ProtoContract]
        public class CallbackWithProtoContext
        {
            [ProtoAfterDeserialization]
            public void OnDeserialized(SerializationContext context)
            {
                ReadState = context.Context;
            }
            [OnSerialized]
            public void OnSerialized(SerializationContext context)
            {
                WriteState = context.Context;
            }
            public object ReadState { get; set; }
            public object WriteState { get; set; }
        }

#if REMOTING
        [ProtoContract]
        public class CallbackWithRemotingContext
        {
            [OnDeserialized]
            public void OnDeserialized(StreamingContext context)
            {
                ReadState = context.Context;
            }
            [ProtoAfterSerialization]
            public void OnSerialized(StreamingContext context)
            {
                WriteState = context.Context;
            }
            public object ReadState { get; set; }
            public object WriteState { get; set; }
        }
#endif

        [ProtoContract]
        public struct CallbackStructSimple : ICallbackTest
        {
            public static int BeforeSerializeCount
            {
                get { return Interlocked.CompareExchange(ref beforeSer, 0, 0); }
            }
            public static int AfterSerializeCount
            {
                get { return Interlocked.CompareExchange(ref afterSer, 0, 0); }
            }

            private static int beforeSer, afterSer;
            [ProtoMember(1)]
            public string Bar { get; set; }

            [OnDeserialized]
            public void OnDeserialized() { History += ";OnDeserialized"; }
            [OnDeserializing]
            public void OnDeserializing() { History += ";OnDeserializing"; }
            [OnSerialized]
            public void OnSerialized()
            {
                Interlocked.Increment(ref afterSer);
            }
            [OnSerializing]
            public void OnSerializing()
            {
                Interlocked.Increment(ref beforeSer);
            }
            
            public string History { get; private set; }
        }

        [ProtoContract] // this only exists for me to compare the IL to CallbackStructSimple
        public struct CallbackStructSimpleNoCallbacks
        {
            [ProtoMember(1)]
            public string Bar { get; set; }
        }

#pragma warning disable IDE0051 // Remove unused private members
        private void ManuallyWrittenSerializeCallbackStructSimple(CallbackStructSimple obj, ref ProtoWriter.State state)
#pragma warning restore IDE0051 // Remove unused private members
        {
            obj.OnSerializing();
            string bar = obj.Bar;
            if(bar != null)
            {
                state.WriteFieldHeader(1, WireType.String);
                state.WriteString(bar);
            }
            obj.OnSerialized();
        }
    }
}
