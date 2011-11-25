using System.IO;
using System.Threading;
using NUnit.Framework;
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
        void OnDeserialized() { History += ";OnDeserialized"; }
        [OnDeserializing]
        void OnDeserializing() { History += ";OnDeserializing"; }
        [OnSerialized]
        void OnSerialized() { History += ";OnSerialized"; }
        [OnSerializing]
        void OnSerializing() { History += ";OnSerializing"; }
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
    }

    [ProtoContract]
    class TestInheritedImplementedAtChildDerived : TestInheritedImplementedAtChild
    {
        [OnDeserialized]
        void OnDeserialized() { History += ";OnDeserialized"; }
        [OnDeserializing]
        void OnDeserializing() { History += ";OnDeserializing"; }
        [OnSerialized]
        void OnSerialized() { History += ";OnSerialized"; }
        [OnSerializing]
        void OnSerializing() { History += ";OnSerializing"; }

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

    [TestFixture]
    public class Callbacks
    {
        public static void Test<T, TCreate>(bool compile = false, params Type[] extraTypes)
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
        static void Test<T, TCreate>(TypeModel model, string mode)
            where TCreate : T, new()
            where T : ICallbackTest
        {
            try
            {
                mode = ":" + mode;
                TCreate cs = new TCreate();
                cs.Bar = "abc";
                string ctorExpected = typeof (TCreate).IsValueType ? null : "ctor";
                Assert.IsNotNull(cs, "orig" + mode);
                Assert.AreEqual(ctorExpected, cs.History, "orig before" + mode);
                Assert.AreEqual("abc", cs.Bar, "orig before" + mode);

                TCreate clone = (TCreate) model.DeepClone(cs);
                if (!typeof (TCreate).IsValueType)
                {
                    Assert.AreEqual(ctorExpected + ";OnSerializing;OnSerialized", cs.History, "orig after" + mode);
                }
                Assert.AreEqual("abc", cs.Bar, "orig after" + mode);

                Assert.IsNotNull(clone, "clone" + mode);
                Assert.AreNotSame(cs, clone, "clone" + mode);
                Assert.AreEqual(ctorExpected + ";OnDeserializing;OnDeserialized", clone.History, "clone after" + mode);
                Assert.AreEqual("abc", clone.Bar, "clone after" + mode);

                T clone2 = (T) model.DeepClone(cs);
                if (!typeof (TCreate).IsValueType)
                {
                    Assert.AreEqual(ctorExpected + ";OnSerializing;OnSerialized;OnSerializing;OnSerialized", cs.History,
                                    "orig after" + mode);
                }
                Assert.AreEqual("abc", cs.Bar, "orig after" + mode);

                Assert.IsNotNull(clone2, "clone2" + mode);
                Assert.AreNotSame(cs, clone2, "clone2" + mode);
                Assert.AreEqual(ctorExpected + ";OnDeserializing;OnDeserialized", clone2.History, "clone2 after" + mode);
                Assert.AreEqual("abc", clone2.Bar, "clone2 after" + mode);
            } catch(Exception ex)
            {
                Console.Error.WriteLine(ex.StackTrace);
                Assert.Fail(ex.Message + mode);
            }
        }

        [ProtoContract]
        class DuplicateCallbacks
        {
            [ProtoBeforeSerialization]
            void Foo() {}

            [ProtoBeforeSerialization]
            void Bar() { }
        }

        [Test]
        public void TestSimple()
        {
            Test<CallbackSimple, CallbackSimple>();
        }

        [Test]
        public void TestStructSimple()
        {
            int beforeSer = CallbackStructSimple.BeforeSerializeCount,
                afterSer = CallbackStructSimple.AfterSerializeCount;
            Test<CallbackStructSimple, CallbackStructSimple>(true, typeof(CallbackStructSimpleNoCallbacks));

            Assert.AreEqual(6, CallbackStructSimple.BeforeSerializeCount - beforeSer);
            Assert.AreEqual(6, CallbackStructSimple.AfterSerializeCount - afterSer);
        } 

        [Test]
        public void TestInheritedVirtualAtRoot()
        {
            Test<TestInheritedVirtualAtRoot, TestInheritedVirtualAtRootDerived>();
        }

        [Test]
        public void TestInheritedVirtualAtRootProtoAttribs()
        {
            Test<TestInheritedVirtualAtRootProtoAttribs, TestInheritedVirtualAtRootDerivedProtoAttribs>();
        }

        [Test]
        public void TestInheritedImplementedAtRoot()
        {
            Test<TestInheritedImplementedAtRoot, TestInheritedImplementedAtRootDerived>();
        }

        [Test] /* now supported */
        public void TestInheritedImplementedAtChild()
        {
            Test<TestInheritedImplementedAtChild, TestInheritedImplementedAtChildDerived>();
        }

        [Test, ExpectedException(typeof(ProtoException), ExpectedMessage = "Duplicate ProtoBuf.ProtoBeforeSerializationAttribute callbacks on Examples.Callbacks+DuplicateCallbacks")]
        public void TestDuplicateCallbacks()
        {
            Serializer.Serialize(Stream.Null, new DuplicateCallbacks());
        }

        [Test]
        public void CallbacksWithContext()
        {
            var model = TypeModel.Create();
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
            Assert.IsNull(orig.B.ReadState);
            Assert.IsNull(orig.B.WriteState);
#if REMOTING
            Assert.IsNull(orig.C.ReadState);
            Assert.IsNull(orig.C.WriteState);
#endif
            using (var ms = new MemoryStream())
            {
                SerializationContext ctx = new SerializationContext { Context = new object()};
                model.Serialize(ms, orig, ctx);
                Assert.IsNull(orig.B.ReadState);
                Assert.AreSame(ctx.Context, orig.B.WriteState);
#if REMOTING
                Assert.IsNull(orig.C.ReadState);
                Assert.AreSame(ctx.Context, orig.C.WriteState);
#endif
                ms.Position = 0;
                ctx = new SerializationContext { Context = new object() };
                clone = (CallbackWrapper)model.Deserialize(ms, null, typeof(CallbackWrapper), -1, ctx);
                Assert.AreSame(ctx.Context, clone.B.ReadState);
                Assert.IsNull(clone.B.WriteState);
#if REMOTING
                Assert.AreSame(ctx.Context, clone.C.ReadState);
                Assert.IsNull(clone.C.WriteState);
#endif
            }
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

        public void ManuallyWrittenSerializeCallbackStructSimple(CallbackStructSimple obj, ProtoWriter writer)
        {
            obj.OnSerializing();
            string bar = obj.Bar;
            if(bar != null)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.String, writer);
                ProtoWriter.WriteString(bar, writer);
            }
            obj.OnSerialized();
        }
    }
}
