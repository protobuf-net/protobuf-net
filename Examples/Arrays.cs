using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProtoBuf;

namespace Examples
{
    [ProtoContract]
    class Node
    {
        [ProtoMember(1)]
        public int Key { get; set; }

        [ProtoMember(2)]
        public Node[] Nodes { get; set; }        
    }

    [ProtoContract]
    class Prim
    {
        [ProtoMember(1)]
        public string[] Values { get; set; }
    }

    [ProtoContract]
    class ArrayArray
    {
        [ProtoMember(1)]
        public string[][] Values { get; set; }
    }

    [ProtoContract]
    class ArrayList
    {
        [ProtoMember(1)]
        public List<string>[] Values { get; set; }
    }
    [ProtoContract]
    class ListArray
    {
        [ProtoMember(1)]
        public List<string[]> Values { get; set; }
    }
    [ProtoContract]
    class ListList
    {
        [ProtoMember(1)]
        public List<List<string>> Values { get; set; }
    }

    [ProtoContract]
    class MultiDim
    {
        [ProtoMember(1)]
        public int[,] Values { get; set; }
    }
    
    [TestFixture]
    public class ArrayTests
    {

        [Test]
        public void TestPrimativeArray()
        {
            Prim p = new Prim { Values = new[] { "abc", "def", "ghi", "jkl" } },
                clone = Serializer.DeepClone(p);

            string[] oldArr = p.Values, newArr = clone.Values;
            Assert.AreEqual(oldArr.Length, newArr.Length);
            for (int i = 0; i < oldArr.Length; i++)
            {
                Assert.AreEqual(oldArr[i], newArr[i], "Item " + i.ToString());
            }
        }

        [Test, ExpectedException(typeof(NotSupportedException))]
        public void TestMultidimArray()
        {
            MultiDim md = new MultiDim { Values = new int[1, 2] { { 3, 4 } } };
            Serializer.DeepClone(md);
        }

        [Test, ExpectedException(typeof(NotSupportedException))]
        public void TestArrayArray()
        {
            Serializer.DeepClone(new ArrayArray());
        }
        [Test, ExpectedException(typeof(NotSupportedException))]
        public void TestArrayList()
        {
            Serializer.DeepClone(new ArrayList());
        }
        [Test, ExpectedException(typeof(NotSupportedException))]
        public void TestListArray()
        {
            Serializer.DeepClone(new ListArray());
        }
        [Test, ExpectedException(typeof(NotSupportedException))]
        public void TestListList()
        {
            Serializer.DeepClone(new ListList());
        }


        [Test]
        public void TestObjectArray()
        {
            Node node = new Node
            {
                Key = 27,
                Nodes = new[] {
                    new Node { Key = 1 },
                    new Node { Key = 3 } }
            };
            VerifyNodeTree(node);
        }

        // [Test] known variation...
        public void TestEmptyArray()
        {
            Node node = new Node
            {
                Key = 27,
                Nodes = new Node[0]
            };
            VerifyNodeTree(node);
        }

        [Test]
        public void TestNullArray()
        {
            Node node = new Node
            {
                Key = 27,
                Nodes = null
            };
            VerifyNodeTree(node);
        }

        [Test]
        public void TestDuplicateNonRecursive()
        {
            Node child = new Node { Key = 17 };
            Node parent = new Node { Nodes = new[] { child, child, child } };
            VerifyNodeTree(parent);
        }

        [Test, ExpectedException(typeof(ProtoException))]
        public void TestDuplicateRecursive()
        {
            Node child = new Node { Key = 17 };
            Node parent = new Node { Nodes = new[] { child, child, child } };
            child.Nodes = new[] { parent };
            VerifyNodeTree(parent);
        }

        [Test]
        public void TestNestedArray()
        {
            Node node = new Node
            {
                Key = 27,
                Nodes = new[] {
                    new Node {
                        Key = 19,
                        Nodes = new[] {
                            new Node {Key = 1},
                            new Node {Key = 14},
                        },
                    },
                    new Node {
                        Key = 3
                    },
                    new Node {
                        Key = 3,
                        Nodes = new[] {
                            new Node {Key = 234}
                        }
                    }
                }
            };
            VerifyNodeTree(node);
        }

        static void VerifyNodeTree(Node node) {
            Node clone = Serializer.DeepClone(node);
            string msg;
            bool eq = AreEqual(node, clone, out msg);
            Assert.IsTrue(eq, msg);
        }

        static bool AreEqual(Node x, Node y, out string msg)
        {
            // compare core
            if (ReferenceEquals(x, y)) { msg = ""; return true; }
            if (x == null || y == null) { msg = "1 node null"; return false; }
            if (x.Key != y.Key) { msg = "key"; return false; }

            Node[] xNodes = x.Nodes, yNodes = y.Nodes;
            if (ReferenceEquals(xNodes,yNodes))
            { // trivial
            }
            else
            {
                if (xNodes == null || yNodes == null) { msg = "1 Nodes null"; return false; }
                if (xNodes.Length != yNodes.Length) { msg = "Nodes length"; return false; }
                for (int i = 0; i < xNodes.Length; i++)
                {
                    bool eq = AreEqual(xNodes[i], yNodes[i], out msg);
                    if (!eq)
                    {
                        msg = i.ToString() + "; " + msg;
                        return false;
                    }
                }
            }
            // run out of things to be different!
            msg = "";
            return true;

        }
    }
}
