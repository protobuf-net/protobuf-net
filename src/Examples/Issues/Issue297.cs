#if !COREFX
using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.Linq;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{

    
    public class Issue297
    {

        // also asked: http://stackoverflow.com/q/11134370
        [Fact]
        public void TestInt32()
        {
            Node<int> tree = new Node<int>.RootNode("abc",1), clone;
            var children = tree.GetChildren();
            children.Add(new Node<int>("abc/def", 2));
            children.Add(new Node<int>("abc/ghi", 3));
            Assert.Equal(2, tree.GetChildren().Count);

            using(var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, tree);
                Assert.True(1 > 0); // I always get these args the wrong way around, so always check!
                Assert.True(ms.Length > 0);
                ms.Position = 0;
                clone = Serializer.Deserialize<Node<int>>(ms);
            }
            Assert.IsType<Node<int>.RootNode>(clone);
            Assert.Equal("abc", clone.Key);
            Assert.Equal(1, clone.Value);
            children = clone.GetChildren();
            Assert.Equal(2, children.Count);

            Assert.False(children[0].HasChildren);
            Assert.Equal("abc/def", children[0].Key);
            Assert.Equal(2, children[0].Value);

            Assert.False(children[1].HasChildren);
            Assert.Equal("abc/ghi", children[1].Key);
            Assert.Equal(3, children[1].Value);
        }

        static Issue297()
        {
            RuntimeTypeModel.Default.Add(typeof(Node<int>), true).AddSubType(4, typeof(Node<int>.RootNode));
            RuntimeTypeModel.Default.Add(typeof(Node<MyDto>), true).AddSubType(4, typeof(Node<MyDto>.RootNode));
            RuntimeTypeModel.Default.Add(typeof(Node<SomeNewType>), true).AddSubType(4, typeof(Node<SomeNewType>.RootNode));
        }

        [Fact]
        public void TestMyDTO()
        {
            Node<MyDto> tree = new Node<MyDto>.RootNode("abc", new MyDto { Value =  1}), clone;
            var children = tree.GetChildren();
            children.Add(new Node<MyDto>("abc/def", new MyDto { Value =  2}));
            children.Add(new Node<MyDto>("abc/ghi", new MyDto { Value =  3}));
            Assert.Equal(2, tree.GetChildren().Count);

            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, tree);
                Assert.True(1 > 0); // I always get these args the wrong way around, so always check!
                Assert.True(ms.Length > 0);
                ms.Position = 0;
                clone = Serializer.Deserialize<Node<MyDto>>(ms);
            }
            Assert.IsType<Node<MyDto>.RootNode>(clone);
            Assert.Equal("abc", clone.Key);
            Assert.Equal(1, clone.Value.Value);
            children = clone.GetChildren();
            Assert.Equal(2, children.Count);

            Assert.False(children[0].HasChildren);
            Assert.Equal("abc/def", children[0].Key);
            Assert.Equal(2, children[0].Value.Value);

            Assert.False(children[1].HasChildren);
            Assert.Equal("abc/ghi", children[1].Key);
            Assert.Equal(3, children[1].Value.Value);
        }

#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip="Needs attention")]
#pragma warning restore xUnit1004 // Test methods should not be skipped
        public void TestListMyDTO()
        {
            Program.ExpectFailure<NullReferenceException>(() =>
            {
                Node<List<MyDto>> tree = new Node<List<MyDto>>.RootNode("abc", new List<MyDto> { new MyDto { Value = 1 } }), clone;
                var children = tree.GetChildren();
                children.Add(new Node<List<MyDto>>("abc/def", new List<MyDto> { new MyDto { Value = 2 } }));
                children.Add(new Node<List<MyDto>>("abc/ghi", new List<MyDto> { new MyDto { Value = 3 } }));
                Assert.Equal(2, tree.GetChildren().Count);

                using (var ms = new MemoryStream())
                {
                    Serializer.Serialize(ms, tree);
                    Assert.True(1 > 0); // I always get these args the wrong way around, so always check!
                    Assert.True(ms.Length > 0); //, "stream length");
                    ms.Position = 0;
                    clone = Serializer.Deserialize<Node<List<MyDto>>>(ms);
                }

                Assert.Equal("abc", clone.Key);
                Assert.Equal(1, clone.Value.Single().Value);
                children = clone.GetChildren();
                Assert.Equal(2, children.Count);

                Assert.False(children[0].HasChildren);
                Assert.Equal("abc/def", children[0].Key);
                Assert.Equal(2, children[0].Value.Single().Value);

                Assert.False(children[1].HasChildren);
                Assert.Equal("abc/ghi", children[1].Key);
                Assert.Equal(3, children[1].Value.Single().Value);
            }, "Nested or jagged lists and arrays are not supported");
        }

        [Fact]
        public void TestSomeNewType()
        {
            Node<SomeNewType> tree = new Node<SomeNewType>.RootNode("abc", new SomeNewType { Items = { new MyDto { Value = 1 } }}), clone;
            var children = tree.GetChildren();
            children.Add(new Node<SomeNewType>("abc/def", new SomeNewType { Items = { new MyDto { Value = 2 } } }));
            children.Add(new Node<SomeNewType>("abc/ghi", new SomeNewType { Items = { new MyDto { Value = 3 } } }));
            Assert.Equal(2, tree.GetChildren().Count);

            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, tree);
                Assert.True(1 > 0); // I always get these args the wrong way around, so always check!
                Assert.True(ms.Length > 0);
                ms.Position = 0;
                clone = Serializer.Deserialize<Node<SomeNewType>>(ms);
            }

            Assert.Equal("abc", clone.Key);
            Assert.Equal(1, clone.Value.Items.Single().Value);
            children = clone.GetChildren();
            Assert.Equal(2, children.Count);

            Assert.False(children[0].HasChildren);
            Assert.Equal("abc/def", children[0].Key);
            Assert.Equal(2, children[0].Value.Items.Single().Value);

            Assert.False(children[1].HasChildren);
            Assert.Equal("abc/ghi", children[1].Key);
            Assert.Equal(3, children[1].Value.Items.Single().Value);
        }

        [ProtoContract]
        class SomeNewType
        {
            [ProtoMember(1)]
            public List<MyDto> Items { get { return items; } }
            private readonly List<MyDto> items = new List<MyDto>();
        }

        [Serializable, ProtoContract]
        public class Node<T>
        {
            [ProtoMember(3)]
            private readonly List<Node<T>> children = new List<Node<T>>();
            [ProtoMember(1)]
            private readonly string key;
            [ProtoMember(2)]
            private T value;

            public string Key { get { return key; } }
            public T Value { get { return value; } }

            public Node(string key, T value)
            {
                this.key = key;
                this.value = value;
            }

            private Node()
            {
            }

            public bool HasChildren
            {
                get { return children.Count > 0; }
            }

            public void Insert(string key, T value)
            {
                var potentialChild = new Node<T>(key, value);
                Add(potentialChild);
            }

            private bool Add(Node<T> theNewChild)
            {
                if (Contains(theNewChild))
                    throw new ArgumentException(string.Format("Duplicate key: '{0}'", theNewChild.key));

                if (!IsParentOf(theNewChild))
                    return false;

                bool childrenObligedRequest = RequestChildrenToOwn(theNewChild);
                if (childrenObligedRequest) return true;

                AcceptAsOwnChild(theNewChild);
                return true;
            }

            private bool RequestChildrenToOwn(Node<T> newChild)
            {
                return
                    children.Exists(
                        existingChild =>
                        existingChild.MergeIfSameAs(newChild) || existingChild.Add(newChild) ||
                        ForkANewChildAndAddChildren(existingChild, newChild));
            }

            private bool MergeIfSameAs(Node<T> potentialChild)
            {
                if (!IsTheSameAs(potentialChild) || IsNotUnrealNode())
                    return false;

                value = potentialChild.value;
                return true;
            }

            private bool IsNotUnrealNode()
            {
                return !IsUnrealNode();
            }

            private void Disown(Node<T> existingChild)
            {
                children.Remove(existingChild);
            }

            private Node<T> AcceptAsOwnChild(Node<T> child)
            {
                if (NotItself(child)) children.Add(child);
                return this;
            }

            private bool NotItself(Node<T> child)
            {
                return !Equals(child);
            }

            private bool ForkANewChildAndAddChildren(Node<T> existingChild, Node<T> newChild)
            {
                if (existingChild.IsNotMySibling(newChild))
                    return false;

                var surrogateParent = MakeASurrogateParent(existingChild, newChild);
                if (surrogateParent.IsTheSameAs(this))
                    return false;

                SwapChildren(existingChild, newChild, surrogateParent);
                return true;
            }

            private bool IsNotMySibling(Node<T> newChild)
            {
                return !IsMySibling(newChild);
            }

            private void SwapChildren(Node<T> existingChild, Node<T> newChild, Node<T> surrogateParent)
            {
                surrogateParent.AcceptAsOwnChild(existingChild)
                    .AcceptAsOwnChild(newChild);

                AcceptAsOwnChild(surrogateParent);
                Disown(existingChild);
            }

            private Node<T> MakeASurrogateParent(Node<T> existingChild, Node<T> newChild)
            {
                string keyForNewParent = existingChild.CommonBeginningInKeys(newChild);
                keyForNewParent = keyForNewParent.Trim();
                var surrogateParent = new Node<T>(keyForNewParent, default(T));

                return surrogateParent.IsTheSameAs(newChild) ? newChild : surrogateParent;
            }

            private bool IsTheSameAs(Node<T> parent)
            {
                return Equals(parent);
            }

            private bool IsMySibling(Node<T> potentialSibling)
            {
                return CommonBeginningInKeys(potentialSibling).Length > 0;
            }

            private string CommonBeginningInKeys(Node<T> potentialSibling)
            {
                return "foo"; // return key.CommonBeginningWith(potentialSibling.key);
            }

            internal virtual bool IsParentOf(Node<T> potentialChild)
            {
                return potentialChild.key.StartsWith(key);
            }

            public bool Delete(string key)
            {
                Node<T> nodeToBeDeleted = children.Find(child => child.Find(key) != null);
                if (nodeToBeDeleted == null) return false;

                if (nodeToBeDeleted.HasChildren)
                {
                    nodeToBeDeleted.MarkAsUnreal();
                    return true;
                }

                children.Remove(nodeToBeDeleted);
                return true;
            }

            private void MarkAsUnreal()
            {
                value = default(T);
            }

            public T Find(string key)
            {
                var childBeingSearchedFor = new Node<T>(key, default(T));
                return Find(childBeingSearchedFor);
            }

            private T Find(Node<T> childBeingSearchedFor)
            {
                if (Equals(childBeingSearchedFor)) return value;
                T node = default(T);
                children.Find(child =>
                                  {
                                      node = child.Find(childBeingSearchedFor);
                                      return node != null;
                                  });
                if (node == null) return default(T);
                return node;
            }

            public bool Contains(string key)
            {
                return Contains(new Node<T>(key, default(T)));
            }

            private bool Contains(Node<T> child)
            {
                if (Equals(child) && IsUnrealNode()) return false;

                if (Equals(child)) return true;

                return children.Exists(node => node.Contains(child));
            }

            private bool IsUnrealNode()
            {
                return value == null;
            }

            public List<T> Search(string keyPrefix)
            {
                var nodeBeingSearchedFor = new Node<T>(keyPrefix, default(T));
                return Search(nodeBeingSearchedFor);
            }

            private List<T> Search(Node<T> nodeBeingSearchedFor)
            {
                if (IsTheSameAs(nodeBeingSearchedFor))
                    return MeAndMyDescendants();

                return SearchInMyChildren(nodeBeingSearchedFor);
            }

            private List<T> SearchInMyChildren(Node<T> nodeBeingSearchedFor)
            {
                List<T> searchResults = null;

                children.Exists(
                    existingChild => (searchResults = existingChild.SearchUpAndDown(nodeBeingSearchedFor)).Count > 0);

                return searchResults;
            }

            private List<T> SearchUpAndDown(Node<T> node)
            {
                if (node.IsParentOf(this))
                    return MeAndMyDescendants();

                return IsParentOf(node) ? Search(node) : new List<T>();

            }

            private List<T> MeAndMyDescendants()
            {
                var meAndMyDescendants = new List<T>();
                if (!IsUnrealNode())
                    meAndMyDescendants.Add(value);

                children.ForEach(child => meAndMyDescendants.AddRange(child.MeAndMyDescendants()));
                return meAndMyDescendants;
            }

            public long Size()
            {
                const long size = 0;
                return Size(size);
            }

            private long Size(long size)
            {
                if (!IsUnrealNode())
                    size++;

                children.ForEach(node => size += node.Size());
                return size;
            }

            public override string ToString()
            {
                return key;
            }

            public bool Equals(Node<T> other)
            {
                if (other is null) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(other.key, key);
            }

            public override bool Equals(object obj)
            {
                if (obj is null) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof (Node<T>)) return false;
                return Equals((Node<T>) obj);
            }

            public override int GetHashCode()
            {
                return (key != null ? key.GetHashCode() : 0);
            }

            public static Node<T> Root()
            {
                return new RootNode();
            }

            public List<Node<T>> GetChildren()
            {
                return children;
            }

            [Serializable, ProtoContract]
            public class RootNode : Node<T>
            {
                public RootNode() { }
                public RootNode(string key, T value) : base(key, value) {}
                internal override bool IsParentOf(Node<T> potentialChild)
                {
                    return true;
                }
            }

        }
        [ProtoContract]
        public class MyDto
        {
            [ProtoMember(1)]
            public int Value { get; set; }
        }
    }
}
#endif