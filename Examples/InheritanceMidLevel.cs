using NUnit.Framework;
using ProtoBuf;

namespace Examples
{
    /// <summary>
    /// Tests the scenario where a class exposes a property that isn't the root - i.e. Child : Parent, and has
    /// a Child property
    /// </summary>
    [TestFixture]
    public class InheritanceMidLevel
    {
        static IMLChild CreateChild()
        {
            return new IMLChild {ChildProperty = 123, ParentProperty = 456, RootProperty = 789};
        }

        [Test]
        public void TestParent()
        {
            IMLTest test = new IMLTest {Parent = CreateChild()},
                    clone = Serializer.DeepClone(test);

            Assert.AreNotEqual(test.Parent, clone.Parent);
            Assert.IsInstanceOfType(typeof(IMLChild), clone.Parent);
            Assert.AreEqual(0, clone.Parent.RootProperty, "RootProperty");
            Assert.AreEqual(test.Parent.ParentProperty, clone.Parent.ParentProperty, "ParentProperty");
            Assert.AreEqual(((IMLChild)test.Parent).ChildProperty, ((IMLChild)clone.Parent).ChildProperty, "ChildProperty");
        }

        [Test, Ignore("work in progress")]
        public void TestChild()
        {
            IMLTest test = new IMLTest { Child = CreateChild() },
                    clone = Serializer.DeepClone(test);

            Assert.AreNotEqual(test.Child, clone.Child);
            Assert.IsInstanceOfType(typeof(IMLChild), clone.Child);
            Assert.AreEqual(0, clone.Child.RootProperty, "RootProperty");
            Assert.AreEqual(test.Child.ParentProperty, clone.Child.ParentProperty, "ParentProperty");
            Assert.AreEqual(test.Child.ChildProperty, clone.Child.ChildProperty, "ChildProperty");
        }
    }


    [ProtoContract]
    class IMLTest
    {
        [ProtoMember(1)]
        public IMLChild Child { get; set; }

        [ProtoMember(2)]
        public IMLParent Parent { get; set; }
    }
    [ProtoContract]
    class IMLChild : IMLParent
    {
        [ProtoMember(1)]
        public int ChildProperty { get; set; }
    }

    [ProtoContract]
    [ProtoInclude(2, typeof(IMLChild))]
    abstract class IMLParent : IMLRoot
    {
        [ProtoMember(1)]
        public int ParentProperty { get; set;}

        
    }

    abstract class IMLRoot
    {
        public int RootProperty { get; set; }
    }
}
