using System.IO;
using NUnit.Framework;
using ProtoBuf;
using System.Runtime.Serialization;
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
        public static void Test<T, TCreate>()
            where TCreate : T, new()
            where T : class, ICallbackTest
        {
            TCreate cs = new TCreate();
            cs.Bar = "abc";
            Assert.IsNotNull(cs, "orig");
            Assert.AreEqual("ctor", cs.History, "orig before");
            Assert.AreEqual("abc", cs.Bar, "orig before");

            TCreate clone = Serializer.DeepClone<TCreate>(cs);
            Assert.AreEqual("ctor;OnSerializing;OnSerialized", cs.History, "orig after");
            Assert.AreEqual("abc", cs.Bar, "orig after");

            Assert.IsNotNull(clone, "clone");
            Assert.AreNotSame(cs, clone, "clone");
            Assert.AreEqual("ctor;OnDeserializing;OnDeserialized", clone.History, "clone after");
            Assert.AreEqual("abc", clone.Bar, "clone after");

            T clone2 = Serializer.DeepClone<TCreate>(cs);
            Assert.AreEqual("ctor;OnSerializing;OnSerialized;OnSerializing;OnSerialized", cs.History, "orig after");
            Assert.AreEqual("abc", cs.Bar, "orig after");

            Assert.IsNotNull(clone2, "clone2");
            Assert.AreNotSame(cs, clone2, "clone2");
            Assert.AreEqual("ctor;OnDeserializing;OnDeserialized", clone2.History, "clone2 after");
            Assert.AreEqual("abc", clone2.Bar, "clone2 after");

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

        [Test, ExpectedException(typeof(ProtoException), ExpectedMessage = "Callbacks are only supported on the root contract type in an inheritance tree; consider implementing callbacks as virtual methods on Examples.TestInheritedImplementedAtChild")]
        public void TestInheritedImplementedAtChild()
        {
            Test<TestInheritedImplementedAtChild, TestInheritedImplementedAtChildDerived>();
        }

        [Test, ExpectedException(typeof(ProtoException), ExpectedMessage = "Conflicting callback methods (decorated with ProtoBeforeSerializationAttribute) found for DuplicateCallbacks: Foo and Bar")]
        public void TestDuplicateCallbacks()
        {
            Serializer.Serialize(Stream.Null, new DuplicateCallbacks());
        }

    }
}
