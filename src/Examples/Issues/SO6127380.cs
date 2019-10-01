#if FEAT_DYNAMIC_REF
using System.Collections.Generic;
using Xunit;
using ProtoBuf;

namespace Examples.Issues
{
    
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

        [Fact]
        public void Execute()
        {
            Node n = new Node { Data = 0 }, root = n;
            for (int i = 1; i < 15; i++)
            {
                Node child = new Node { Data = i };
                n.AddChild(child);
                n = child;
            }
            _ = Serializer.DeepClone(root);
        }
        [Fact]
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
            _ = Serializer.DeepClone(root);
        }
        [Fact]
        public void TestSelfRecursive()
        {
            Node orig = new Node();
            orig.AddChild(orig);
            Assert.Single(orig.Children);
            Assert.Same(orig, orig.Children[0]);

            var clone = Serializer.DeepClone(orig);
            Assert.Single(clone.Children);
            Assert.Same(clone, clone.Children[0]);
        }
    }
}
#endif