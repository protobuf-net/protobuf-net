using System.Collections.Generic;
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

        static void CheckParent(IMLParent original, IMLParent clone)
        {
            CheckChild((IMLChild)original, (IMLChild)clone);
        }
        static void CheckChild(IMLChild original, IMLChild clone)
        {
            Assert.AreNotSame(original, clone);
            Assert.IsInstanceOfType(typeof(IMLChild), original, "Original type");
            Assert.IsInstanceOfType(typeof(IMLChild), clone, "Clone type");
            Assert.AreEqual(0, clone.RootProperty, "RootProperty"); // not serialized
            Assert.AreEqual(original.ParentProperty, clone.ParentProperty, "ParentProperty");
            Assert.AreEqual(original.ChildProperty, clone.ChildProperty, "ChildProperty");
        }

        [Test]
        public void TestParent()
        {
            IMLTest test = new IMLTest { Parent = CreateChild() },
                    clone = Serializer.DeepClone(test);

            CheckParent(test.Parent, clone.Parent);
        }
        [Test]
        public void TestChild()
        {
            IMLTest test = new IMLTest { Child = CreateChild() },
                    clone = Serializer.DeepClone(test);

            CheckChild(test.Child, clone.Child);
        }

        [Test]
        public void TestParents()
        {
            IMLTest test = new IMLTest() {Parents = {CreateChild()}},
                    clone = Serializer.DeepClone(test);

            Assert.AreEqual(1, test.Parents.Count);
            Assert.AreEqual(1, clone.Parents.Count);
            CheckParent(test.Parents[0], clone.Parents[0]);
        }

        [Test]
        public void TestChildren()
        {
            IMLTest test = new IMLTest() { Children = { CreateChild() } },
                    clone = Serializer.DeepClone(test);

            Assert.AreEqual(1, test.Children.Count);
            Assert.AreEqual(1, clone.Children.Count);
            CheckChild(test.Children[0], clone.Children[0]);
        }
    }


    [ProtoContract]
    class IMLTest
    {
        public IMLTest()
        {
            Parents = new List<IMLParent>();
            Children = new List<IMLChild>();
        }
        [ProtoMember(1)]
        public IMLChild Child { get; set; }

        [ProtoMember(2)]
        public IMLParent Parent { get; set; }

        [ProtoMember(3)]
        public List<IMLParent> Parents { get; private set; }

        [ProtoMember(4)]
        public List<IMLChild> Children { get; private set; }
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
