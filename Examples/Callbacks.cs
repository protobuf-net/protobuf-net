using NUnit.Framework;
using ProtoBuf;
namespace Examples
{
    [ProtoContract]
    public class CallbackSimple : ISerializerCallback, ICallbackTest
    {
        [ProtoMember(1)]
        public string Bar { get; set;}

        void ISerializerCallback.OnDeserialized() { History += ";OnDeserialized"; }
        void ISerializerCallback.OnDeserializing() { History += ";OnDeserializing"; }
        void ISerializerCallback.OnSerialized() { History += ";OnSerialized"; }
        void ISerializerCallback.OnSerializing() { History += ";OnSerializing"; }
        public CallbackSimple() { History = "ctor"; }
        public string History { get; private set; }
    }

    
    [ProtoContract]
    [ProtoInclude(1, typeof(TestInheritedImplementedAtRootDerived))]
    abstract class TestInheritedImplementedAtRoot : ISerializerCallback, ICallbackTest
    {
        
        protected abstract string BarCore { get; set;}
        string ICallbackTest.Bar {get { return BarCore;} set { BarCore = value;}}

        void ISerializerCallback.OnDeserialized() { History += ";OnDeserialized"; }
        void ISerializerCallback.OnDeserializing() { History += ";OnDeserializing"; }
        void ISerializerCallback.OnSerialized() { History += ";OnSerialized"; }
        void ISerializerCallback.OnSerializing() { History += ";OnSerializing"; }
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
    [ProtoInclude(1, typeof(TestInheritedImplementedAtChildDerived))]
    abstract class TestInheritedImplementedAtChild : ICallbackTest
    {

        protected abstract string BarCore { get; set; }
        string ICallbackTest.Bar { get { return BarCore; } set { BarCore = value; } }

        protected TestInheritedImplementedAtChild() { History = "ctor"; }
        public string History { get; protected set; }
    }

    [ProtoContract]
    class TestInheritedImplementedAtChildDerived : TestInheritedImplementedAtChild, ISerializerCallback
    {
        void ISerializerCallback.OnDeserialized() { History += ";OnDeserialized"; }
        void ISerializerCallback.OnDeserializing() { History += ";OnDeserializing"; }
        void ISerializerCallback.OnSerialized() { History += ";OnSerialized"; }
        void ISerializerCallback.OnSerializing() { History += ";OnSerializing"; }
        
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
            where TCreate : T, ISerializerCallback, new()
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

        [Test]
        public void TestSimple()
        {
            Test<CallbackSimple, CallbackSimple>();
        } 

        [Test]
        public void TestInheritedImplementedAtRoot()
        {
            Test<TestInheritedImplementedAtRoot, TestInheritedImplementedAtRootDerived>();
        }

        [Test]
        public void TestInheritedImplementedAtChild()
        {
            Test<TestInheritedImplementedAtChild, TestInheritedImplementedAtChildDerived>();
        }

    }
}
