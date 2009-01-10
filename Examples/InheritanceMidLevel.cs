using System.Collections.Generic;
using NUnit.Framework;
using ProtoBuf;
using System;

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

        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestRoot()
        {
            IMLTestRoot test = new IMLTestRoot {Root = CreateChild()},
                        clone = Serializer.DeepClone(test);
        }
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestRoots()
        {
            IMLTestRoots test = new IMLTestRoots { Roots = {CreateChild()} },
                        clone = Serializer.DeepClone(test);
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

        [Test]
        public void TestCloneAsChild()
        {
            IMLChild child = CreateChild(),
                     clone = Serializer.DeepClone(child);
            CheckChild(child, clone);
        }
        [Test]
        public void TestCloneAsParent()
        {
            IMLParent parent = CreateChild(),
                clone = Serializer.DeepClone(parent);
            CheckParent(parent, clone);
        }

        [Test]
        public void TestCloneAsChildList()
        {
            var children = new List<IMLChild> { CreateChild()};
            var clone = Serializer.DeepClone(children);
            Assert.AreEqual(1, children.Count);
            Assert.AreEqual(1, clone.Count);
            CheckChild(children[0], clone[0]);
        }
        [Test]
        public void TestCloneAsParentList()
        {
            var parents = new List<IMLParent> { CreateChild() };
            var clone = Serializer.DeepClone(parents);
            Assert.AreEqual(1, parents.Count);
            Assert.AreEqual(1, clone.Count);
            CheckParent(parents[0], clone[0]);
        }
        [Test]
        public void TestCloneAsChildArray()
        {
            IMLChild[] children = { CreateChild() };
            var clone = Serializer.DeepClone(children);
            Assert.AreEqual(1, children.Length);
            Assert.AreEqual(1, clone.Length);
            CheckChild(children[0], clone[0]);
        }

        [Test]
        public void TestCloneAsParentArray()
        {
            IMLParent[] parents = { CreateChild() };
            var clone = Serializer.DeepClone(parents);
            Assert.AreEqual(1, parents.Length);
            Assert.AreEqual(1, clone.Length);
            CheckParent(parents[0], clone[0]);
        }
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestCloneAsRootArray()
        {
            IMLRoot[] roots = { CreateChild() };
            var clone = Serializer.DeepClone(roots);
        }
        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestCloneAsRootList()
        {
            var roots = new List<IMLRoot> { CreateChild() };
            var clone = Serializer.DeepClone(roots);
        }



        [Test, ExpectedException(typeof(InvalidOperationException))]
        public void TestCloneAsRoot()
        {
            IMLRoot root = CreateChild(),
                clone = Serializer.DeepClone(root);
        }

    }

    [ProtoContract]
    class IMLTestRoot
    {
        [ProtoMember(1)]
        public IMLRoot Root {get; set;}
    }
    [ProtoContract]
    class IMLTestRoots
    {
        public IMLTestRoots() {Roots = new List<IMLRoot>();}
        [ProtoMember(1)]
        public List<IMLRoot> Roots { get; private set; }
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
