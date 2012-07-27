using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using System.IO;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO11656439
    {
        [Test]
        public void BasicStringListShouldRoundTrip()
        {
            var list = new List<string> {"abc"};
            var clone = Serializer.DeepClone(list);
            Assert.AreEqual(1, clone.Count);
            Assert.AreEqual("abc", clone[0]);
        }

        public class MyList : List<string>{}
        
        [Test]
        public void ListSubclassShouldRoundTrip()
        {
            var list = new MyList { "abc" };
            var clone = Serializer.DeepClone(list);
            Assert.AreEqual(1, clone.Count);
            Assert.AreEqual("abc", clone[0]);
        }

        [ProtoContract]
        public class MyContractList : List<string> { }

        [Test]
        public void ContractListSubclassShouldRoundTrip()
        {
            // this test is larger because it wasn't working; neeeded
            // to make it more granular
            var model = TypeModel.Create();
            model.AutoCompile = false;
            CheckContractSubclass(model, "Runtime");
            model.CompileInPlace();
            CheckContractSubclass(model, "CompileInPlace");
            CheckContractSubclass(model.Compile(), "CompileInPlace");
            model.Compile("ContractListSubclassShouldRoundTrip", "ContractListSubclassShouldRoundTrip.dll");
            PEVerify.AssertValid("ContractListSubclassShouldRoundTrip.dll");
        }

        private void CheckContractSubclass(TypeModel model, string caption)
        {
            var list = new MyContractList { "abc" };
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, list);
                Assert.Greater(2, 0, "sanity check:" + caption);
                Assert.Greater(ms.Length, 0, "data should be written:" + caption);
                ms.Position = 0;
                var clone = (MyContractList) model.Deserialize(ms,null, typeof(MyContractList));
                Assert.AreEqual(1, clone.Count, "count:" + caption);
                Assert.AreEqual("abc", clone[0], "payload:" + caption);
            }
        }

        [ProtoContract]
        public class ListWrapper
        {
            [ProtoMember(1)]
            public List<string> BasicList { get; set; }
            [ProtoMember(2)]
            public MyList MyList { get; set; }
            [ProtoMember(3)]
            public MyContractList MyContractList { get; set; }
        }

        [Test]
        public void TestBasicListAsMember()
        {
            var obj = new ListWrapper { BasicList = new List<string> { "abc" } };
            var clone = Serializer.DeepClone(obj);
            Assert.IsNull(clone.MyList);
            Assert.IsNull(clone.MyContractList);
            Assert.AreEqual(1, clone.BasicList.Count);
            Assert.AreEqual("abc", clone.BasicList[0]);
        }

        [Test]
        public void TestMyListAsMember()
        {
            var obj = new ListWrapper { MyList = new MyList { "abc" } };
            var clone = Serializer.DeepClone(obj);
            Assert.IsNull(clone.BasicList);
            Assert.IsNull(clone.MyContractList);
            Assert.AreEqual(1, clone.MyList.Count);
            Assert.AreEqual("abc", clone.MyList[0]);
        }

        [Test]
        public void TestMyContractListAsMember()
        {
            var obj = new ListWrapper { MyContractList = new MyContractList { "abc" } };
            var clone = Serializer.DeepClone(obj);
            Assert.IsNull(clone.BasicList);
            Assert.IsNull(clone.MyList);
            Assert.AreEqual(1, clone.MyContractList.Count);
            Assert.AreEqual("abc", clone.MyContractList[0]);
        }

        [Test]
        public void SanityCheckListWrapper()
        {
            var model = TypeModel.Create();
            model.Add(typeof (ListWrapper), true);
#pragma warning disable 0618
            var schema = model.GetSchema(null);
#pragma warning restore 0618
            Assert.AreEqual(@"package Examples.Issues;

message ListWrapper {
   repeated string BasicList = 1;
   repeated string MyList = 2;
   repeated string MyContractList = 3;
}
", schema);
            model.Compile("SanityCheckListWrapper", "SanityCheckListWrapper.dll");
            PEVerify.AssertValid("SanityCheckListWrapper.dll");
        }

    }
}
