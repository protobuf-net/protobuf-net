using System.IO;
using Xunit;
using ProtoBuf;
using System.Collections.Generic;
using System.Linq;

namespace Examples
{
    [ProtoContract]
    public class RecursiveObject
    {
        [ProtoMember(1)]
        public RecursiveObject Yeuch { get; set; }
    }

    [ProtoContract]
    public class MySurrogate
    {
        [ProtoMember(1)]
        public TreeNode treeNode { get; set; }
        [ProtoConverter]
        public static INode From(MySurrogate surrogate)
        {
            return surrogate.treeNode;
        }
        [ProtoConverter]
        public static MySurrogate To(INode value)
        {
            var surrogate = new MySurrogate();
            if (value is TreeNode treeNode)
                surrogate.treeNode = treeNode;
            return surrogate;
        }
    }
    [ProtoContract(Surrogate = typeof(MySurrogate))]
    public interface INode
    {
        public string Name { get; }
    }
    [ProtoContract]
    public class TreeNode : INode
    {
        [ProtoMember(1)]
        public string Name { get; }
        [ProtoMember(2)]
        public List<INode> Children { get; }
        public TreeNode(string Name) : this()
        {
            this.Name = Name;
        }
        public TreeNode() { Children = new List<INode>(); }
        public string AsString() => Children != null ? $"{Name} => {{{string.Join(",", Children)}}}" : Name;
    }
    
    public class Recursion
    {
        [Fact]
        public void BlowUp()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                RecursiveObject obj = new RecursiveObject();
                obj.Yeuch = obj;
                Serializer.Serialize(Stream.Null, obj);
            });
        }

        [Fact]
        public void Tree_NoRecursionDetected()
        {
            var tree = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToArray().Reverse().Select(letter => new TreeNode(letter.ToString()))
            .Aggregate((prev, next) => { next.Children.Add(prev); return next; });
            //A => {B => {C => {D => {E => {F => {G => {H => {I => {J => {K => {L => {M => {N => {...}}}}}}}}}}}}}}
            var cloned = Serializer.DeepClone(tree);
            Assert.Equal(tree.AsString(), cloned.AsString());
        }

        [Fact]
        public void Graph_RecursionDetected()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                var graph = new TreeNode("A");
                graph.Children.Add(graph);
                Serializer.DeepClone(graph);
            });
        }
    }
}
