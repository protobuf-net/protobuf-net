#if FEAT_DYNAMIC_REF

using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using Xunit;

namespace Examples.Issues
{

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
            var model = RuntimeTypeModel.Create();
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

        [Fact]
        public void CanCreateModel_DerivedFirst()
        {
            Assert.NotNull(CreateModel(false));
        }
        [Fact]
        public void TestRuntime_DerivedFirst()
        {
            var model = CreateModel(false);
            CheckModel(model);
        }
        [Fact]
        public void TestCompileInPlace_DerivedFirst()
        {
            var model = CreateModel(false);
            model.CompileInPlace();
            CheckModel(model);
        }
        [Fact]
        public void TestCompile_DerivedFirst()
        {
            var model = CreateModel(false);
            CheckModel(model.Compile());
        }
        [Fact]
        public void TestCompile_PEVerify_DerivedFirst()
        {
            var model = CreateModel(false);
            var ser = model.Compile("Issue331_DerivedFirst", "Issue331_DerivedFirst.dll");
            CheckModel(ser);

#if !COREFX
            PEVerify.AssertValid("Issue331_DerivedFirst.dll");
            var type = Type.GetType("Issue331_DerivedFirst, Issue331_DerivedFirst");
            Assert.NotNull(type); //, "resolve type");
            ser = (TypeModel)Activator.CreateInstance(type, nonPublic: true);
            CheckModel(ser);
#endif
        }

        [Fact]
        public void CanCreateModel_BaseFirst()
        {
            Assert.NotNull(CreateModel(true));
        }
        [Fact]
        public void TestRuntime_BaseFirst()
        {
            var model = CreateModel(true);
            CheckModel(model);
        }
        [Fact]
        public void TestCompileInPlace_BaseFirst()
        {
            var model = CreateModel(true);
            model.CompileInPlace();
            CheckModel(model);
        }
        [Fact]
        public void TestCompile_BaseFirst()
        {
            var model = CreateModel(true);
            CheckModel(model.Compile());
        }
        [Fact]
        public void TestCompile_PEVerify_BaseFirst()
        {
            var model = CreateModel(true);
            var ser = model.Compile("Issue331_BaseFirst", "Issue331_BaseFirst.dll");
            CheckModel(ser);

#if !COREFX
            PEVerify.AssertValid("Issue331_BaseFirst.dll");
            var type = Type.GetType("Issue331_BaseFirst, Issue331_BaseFirst");
            Assert.NotNull(type); //, "resolve type");
            ser = (TypeModel)Activator.CreateInstance(type, nonPublic: true);
            CheckModel(ser);
#endif
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
            Assert.Equal(1, clone.RootNode.Id);
            Assert.NotNull(clone.RootNode.ChildNodes);
            Assert.Equal(2, clone.RootNode.ChildNodes.Count);
            Assert.Equal(2, clone.RootNode.ChildNodes[0].Id);
            Assert.Equal(3, clone.RootNode.ChildNodes[1].Id);
            Assert.Null(clone.RootNode.ChildNodes[0].ChildNodes);
            Assert.Null(clone.RootNode.ChildNodes[1].ChildNodes);
        }
    }
}


#endif