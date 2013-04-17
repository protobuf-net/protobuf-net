using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Examples.Issues
{
    [TestFixture]
    public class SO15794274
    {
        [Test, Ignore("this looks really painful; have tried sharded cache - cripples perf")]
        public void Execute()
        {
            Assert.IsTrue(Environment.Is64BitProcess, "x64");

            int numberOfTrees = 250;
            int nodesPrTree = 200000;

            var trees = CreateTrees(numberOfTrees, nodesPrTree);
            var forest = new Forest(trees);

            using (var file = File.Create("model.bin"))
            {
                var model = TypeModel.Create();
                var meta = model.Add(typeof(Forest), true);
                meta.CompileInPlace();
                model.CompileInPlace();
                //model.ForwardsOnly = true;
                
                model.Serialize(file, forest);
                file.Position = 0;
                
                var clone = (Forest) model.Deserialize(file, null, typeof(Forest));

                var graph = new HashSet<object>(RefComparer.Default);
                int origChk = 0;
                forest.AddGraph(graph, ref origChk);

                graph = new HashSet<object>(RefComparer.Default);
                int cloneChk = 0;
                clone.AddGraph(graph, ref cloneChk);

                Assert.AreEqual(origChk, cloneChk);
            }

            Console.ReadLine();
        }
        class RefComparer : IEqualityComparer<object>
        {
            private RefComparer() { }
            public static readonly RefComparer Default = new RefComparer();
            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                return x == y;
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }

        private static Tree[] CreateTrees(int numberOfTrees, int nodesPrTree)
        {
            var trees = new Tree[numberOfTrees];
            for (int i = 0; i < numberOfTrees; i++)
            {
                var root = new Node();
                CreateTree(root, nodesPrTree, 0);
                var binTree = new Tree(root);
                trees[i] = binTree;
            }
            return trees;
        }

        private static void CreateTree(INode tree, int nodesPrTree, int currentNumberOfNodes)
        {
            Queue<INode> q = new Queue<INode>();
            q.Enqueue(tree);
            while (q.Count > 0 && currentNumberOfNodes < nodesPrTree)
            {
                var n = q.Dequeue();
                n.Left = new Node();
                q.Enqueue(n.Left);
                currentNumberOfNodes++;

                n.Right = new Node();
                q.Enqueue(n.Right);
                currentNumberOfNodes++;
            }
        }

        private const bool AsRef = true;

        [ProtoContract]
        [ProtoInclude(1, typeof(Node), DataFormat = DataFormat.Group)]
        public interface INode
        {
            [ProtoMember(2, DataFormat = DataFormat.Group, AsReference = AsRef)]
            INode Parent { get; set; }
            [ProtoMember(3, DataFormat = DataFormat.Group, AsReference = AsRef)]
            INode Left { get; set; }
            [ProtoMember(4, DataFormat = DataFormat.Group, AsReference = AsRef)]
            INode Right { get; set; }

            void AddGraph(HashSet<object> graph, ref int chk);
        }

        [ProtoContract, Serializable]
        public class Node : INode
        {
            INode m_parent;
            INode m_left;
            INode m_right;

            //[ProtoMember(1, DataFormat = DataFormat.Group, AsReference = AsRef)]
            public INode Left
            {
                get
                {
                    return m_left;
                }
                set
                {
                    m_left = value;
                    m_left.Parent = null;
                    m_left.Parent = this;
                }
            }
            //[ProtoMember(2, DataFormat = DataFormat.Group, AsReference = AsRef)]
            public INode Right
            {
                get
                {
                    return m_right;
                }
                set
                {
                    m_right = value;
                    m_right.Parent = null;
                    m_right.Parent = this;
                }
            }

            //[ProtoMember(3, DataFormat = DataFormat.Group, AsReference = AsRef)]
            public INode Parent
            {
                get
                {
                    return m_parent;
                }
                set
                {
                    m_parent = value;
                }
            }
            public void AddGraph(HashSet<object> graph, ref int chk) {
                graph.Add(this);
                if (m_parent != null && graph.Add(m_parent))
                {
                    chk += 3;
                    m_parent.AddGraph(graph, ref chk);
                }
                if (m_left != null && graph.Add(m_left))
                {
                    chk += 9;
                    m_left.AddGraph(graph, ref chk);
                }
                if (m_right != null && graph.Add(m_right))
                {
                    chk += 4;
                    m_right.AddGraph(graph, ref chk);
                }
            }
        }

        [ProtoContract(SkipConstructor = true), Serializable]
        public class Tree
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public readonly INode Root;

            public Tree(INode root)
            {
                Root = root;
            }

            public void AddGraph(HashSet<object> graph, ref int chk)
            {
                graph.Add(this);
                if (Root != null && graph.Add(Root)) {
                    chk += 11;
                    Root.AddGraph(graph, ref chk);
                }
            }
        }

        [ProtoContract(SkipConstructor = true), Serializable]
        public class Forest
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public readonly Tree[] Trees;

            public Forest(Tree[] trees)
            {
                Trees = trees;
            }

            public void AddGraph(HashSet<object> graph, ref int chk)
            {
                graph.Add(this);
                if (Trees == null) return;
                for (int i = 0; i < Trees.Length; i++)
                {
                    var tree = Trees[i];
                    if (tree != null && graph.Add(tree)) {
                        chk += 17;
                        tree.AddGraph(graph, ref chk);
                    }
                }
            }
        }
    }
}