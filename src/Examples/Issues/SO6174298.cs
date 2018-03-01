using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;
using System.IO;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class SO6174298
    {
        [ProtoContract]
        [ProtoInclude(10, typeof(BinaryNode))]
        public class Node
        {
            public virtual int Count() { return 1; }
        }

        [ProtoContract]
        public class BinaryNode : Node
        {
            public override int Count()
            {
                int count = 1;
                if (Left != null) count += Left.Count();
                if (Right != null) count += Right.Count();
                return count;

            }
            [ProtoMember(1, IsRequired = true)]
            public Node Left { get; set; }
            [ProtoMember(2, IsRequired = true)]
            public Node Right { get; set; }
        }

        [Fact]
        public void Execute()
        {
            var model = TypeModel.Create();
            ExecuteImpl(model, "runtime");

            model.CompileInPlace();
            ExecuteImpl(model, "CompileInPlace");

            var pregen = model.Compile();
            ExecuteImpl(pregen, "Compile");
        }

        private void ExecuteImpl(TypeModel model, string caption)
        {
            BinaryNode head = new BinaryNode();
            BinaryNode node = head;
            // 13 is the magic limit that triggers recursion check        
            for (int i = 0; i < 13; ++i)
            {
                node.Left = new BinaryNode();
                node = (BinaryNode)node.Left;
            }

            var clone = (Node)model.DeepClone(head);
            Assert.Equal(head.Count(), clone.Count()); //, caption);
        }
    }
}
