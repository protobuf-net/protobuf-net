using System.Collections.Generic;
using NUnit.Framework;
using ProtoBuf;

namespace Examples.Issues
{
    [TestFixture]
    public class SO6127380
    {
        [ProtoContract]
        class Node
        {
            public override string ToString()
            {
                return "Node " + Data;
            }
            public Node()
            {
                Children = new List<Node>();
            }

            [ProtoMember(1, IsRequired = true)]
            public int Data { get; set; }

            [ProtoMember(2, IsRequired = true, AsReference = true)]
            public List<Node> Children { get; set; }

            public void AddChild(Node child)
            {
                Children.Add(child);
            }
        }

        [Test]
        public void Execute()
        {
            Node n = new Node { Data = 0 }, root = n;
            for (int i = 1; i < 15; i++)
            {
                Node child = new Node { Data = i };
                n.AddChild(child);
                n = child;
            }
            Node clone = Serializer.DeepClone(root);
        }
        [Test]
        public void ExecuteRecursive()
        {
            Node n = new Node { Data = 0 }, root = n;
            for (int i = 1; i < 15; i++)
            {
                Node child = new Node { Data = i };
                n.AddChild(child);
                n = child;
            }
            n.AddChild(root);
            Node clone = Serializer.DeepClone(root);
        }
        [Test]
        public void TestSelfRecursive()
        {
            Node orig = new Node();
            orig.AddChild(orig);
            Assert.AreEqual(1, orig.Children.Count);
            Assert.AreSame(orig, orig.Children[0]);

            var clone = Serializer.DeepClone(orig);
            Assert.AreEqual(1, clone.Children.Count);
            Assert.AreSame(clone, clone.Children[0]);
        }
    }
}
