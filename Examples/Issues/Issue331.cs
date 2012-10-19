using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Examples.Issues
{
    [TestFixture]
    public class Issue331
    {
        [ProtoContract]
        public class Tree
        {
            [ProtoMember(1, AsReference = true)]
            public TreeNode RootNode { get; set; }
        }

        [ProtoContract]
        public class TreeNode : TreeNodeBase
        {
            [ProtoMember(1, AsReference = true)]
            public IList<TreeNode> ChildNodes { get; set; }
        }

        [ProtoContract, ProtoInclude(99, typeof(TreeNode))]
        public class TreeNodeBase
        {
            [ProtoMember(101)]
            public int Id { get; set; }
        }

        static RuntimeTypeModel CreateModel(bool baseFirst)
        {
            var model = TypeModel.Create();
            model.AutoCompile = false;
            model.Add(typeof(Tree), true);
            if (baseFirst)
            {
                model.Add(typeof(TreeNodeBase), true);
                model.Add(typeof(TreeNode), true);
            }
            else
            {
                model.Add(typeof(TreeNode), true);
                model.Add(typeof(TreeNodeBase), true);
            }
            
                       
            model.AutoAddMissingTypes = false;
            return model;
        }

        [Test]
        public void CanCreateModel_DerivedFirst()
        {
            Assert.IsNotNull(CreateModel(false));
        }
        [Test]
        public void TestRuntime_DerivedFirst()
        {
            var model = CreateModel(false);
            CheckModel(model);
        }
        [Test]
        public void TestCompileInPlace_DerivedFirst()
        {
            var model = CreateModel(false);
            model.CompileInPlace();
            CheckModel(model);
        }
        [Test]
        public void TestCompile_DerivedFirst()
        {
            var model = CreateModel(false);
            CheckModel(model.Compile());
        }
        [Test]
        public void TestCompile_PEVerify_DerivedFirst()
        {
            var model = CreateModel(false);
            model.Compile("Issue331_DerivedFirst", "Issue331_DerivedFirst.dll");
            PEVerify.AssertValid("Issue331_DerivedFirst.dll");
            var type = Type.GetType("Issue331_DerivedFirst, Issue331_DerivedFirst");
            Assert.IsNotNull(type, "resolve type");
            var ser = (TypeModel)Activator.CreateInstance(type);
            CheckModel(ser);
        }

        [Test]
        public void CanCreateModel_BaseFirst()
        {
            Assert.IsNotNull(CreateModel(true));
        }
        [Test]
        public void TestRuntime_BaseFirst()
        {
            var model = CreateModel(true);
            CheckModel(model);
        }
        [Test]
        public void TestCompileInPlace_BaseFirst()
        {
            var model = CreateModel(true);
            model.CompileInPlace();
            CheckModel(model);
        }
        [Test]
        public void TestCompile_BaseFirst()
        {
            var model = CreateModel(true);
            CheckModel(model.Compile());
        }
        [Test]
        public void TestCompile_PEVerify_BaseFirst()
        {
            var model = CreateModel(true);
            model.Compile("Issue331_BaseFirst", "Issue331_BaseFirst.dll");
            PEVerify.AssertValid("Issue331_BaseFirst.dll");
            var type = Type.GetType("Issue331_BaseFirst, Issue331_BaseFirst");
            Assert.IsNotNull(type, "resolve type");
            var ser = (TypeModel)Activator.CreateInstance(type);
            CheckModel(ser);
        }

        static void CheckModel(TypeModel model)
        {
            var obj = new Tree
            {
                RootNode = new TreeNode
                {
                    Id = 1,
                    ChildNodes = new List<TreeNode>
                    {
                        new TreeNode { Id = 2 },
                        new TreeNode { Id = 3 }
                    }
                }
            };
            var clone = (Tree)model.DeepClone(obj);
            Assert.AreEqual(1, clone.RootNode.Id);
            Assert.IsNotNull(clone.RootNode.ChildNodes);
            Assert.AreEqual(2, clone.RootNode.ChildNodes.Count);
            Assert.AreEqual(2, clone.RootNode.ChildNodes[0].Id);
            Assert.AreEqual(3, clone.RootNode.ChildNodes[1].Id);
            Assert.IsNull(clone.RootNode.ChildNodes[0].ChildNodes);
            Assert.IsNull(clone.RootNode.ChildNodes[1].ChildNodes);
        }
    }
}
