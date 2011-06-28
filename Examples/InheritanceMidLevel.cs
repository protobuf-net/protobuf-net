using System.Collections.Generic;
using NUnit.Framework;
using ProtoBuf;
using System;
using System.IO;
using System.Text;

namespace Examples
{
    /// <summary>
    /// Tests the scenario where a class exposes a property that isn't the root - i.e. Child : Parent, and has
    /// a Child property
    /// </summary>
    [TestFixture]
    public class InheritanceMidLevel
    {
        internal static IMLChild CreateChild(int rootProperty, int parentProperty, int childProperty)
        {
            return new IMLChild { ChildProperty = 123, ParentProperty = 456, RootProperty = 789 };
        }
        internal static IMLChild CreateChild()
        {
            return CreateChild(789, 456, 123);
        }

        internal static void CheckParent(IMLParent original, IMLParent clone)
        {
            CheckChild((IMLChild)original, (IMLChild)clone);
        }
        internal static void CheckChild(IMLChild original, IMLChild clone)
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
            Assert.AreEqual(1, parents.Count, "Original list (before)");
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, parents);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in ms.ToArray())
                {
                    sb.Append(b.ToString("x2"));
                }
                string s = sb.ToString();
                Assert.AreEqual("0a071202087b08c803", s);
                /* expected:
                 * field 1, WT string (instance in list)    0A
                 * length [x]                               07
                 * field 2, WT string (subclass, child)     12
                 * length [x]                               02
                 * field 1, WT variant (ChildProperty)      08
                 * value 123                                7B
                 * field 1, WT variant (ParentProperty)     08
                 * value 456                                C8 03
                */
            }
            var clone = Serializer.DeepClone(parents);
            Assert.AreEqual(1, parents.Count, "Original list (after)");
            Assert.AreEqual(1, clone.Count, "Cloned list");
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



        [Test]
        public void TestCloneAsRoot()
        { // newly supported in v2
            IMLRoot root = CreateChild();
            var orig = (IMLChild)root;
            var clone = (IMLChild)Serializer.DeepClone(root);


            Assert.AreEqual(orig.ChildProperty, clone.ChildProperty, "ChildProperty");
            Assert.AreEqual(orig.ParentProperty, clone.ParentProperty, "ParentProperty");
            Assert.AreEqual(0, clone.RootProperty, "RootProperty"); // RootProperty is not part of the contract
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
