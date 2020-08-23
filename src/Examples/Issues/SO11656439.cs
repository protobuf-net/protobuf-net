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
    
    public class SO11656439
    {
        [Fact]
        public void BasicStringListShouldRoundTrip()
        {
            var list = new List<string> {"abc"};
            var clone = Serializer.DeepClone(list);
            Assert.Single(clone);
            Assert.Equal("abc", clone[0]);
        }

        public class MyList : List<string>{}
        
        [Fact]
        public void ListSubclassShouldRoundTrip()
        {
            var list = new MyList { "abc" };
            var clone = Serializer.DeepClone(list);
            Assert.Single(clone);
            Assert.Equal("abc", clone[0]);
        }

        [ProtoContract]
        public class MyContractList : List<string> { }

        [Fact]
        public void ContractListSubclassShouldRoundTrip()
        {
            // this test is larger because it wasn't working; neeeded
            // to make it more granular
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            CheckContractSubclass(model, "Runtime");
            model.CompileInPlace();
            CheckContractSubclass(model, "CompileInPlace");
            CheckContractSubclass(model.Compile(), "CompileInPlace");
            model.Compile("ContractListSubclassShouldRoundTrip", "ContractListSubclassShouldRoundTrip.dll");
            PEVerify.AssertValid("ContractListSubclassShouldRoundTrip.dll");
        }

#pragma warning disable IDE0060
        private void CheckContractSubclass(TypeModel model, string caption)
#pragma warning restore IDE0060
        {
            var list = new MyContractList { "abc" };
            using var ms = new MemoryStream();
            model.Serialize(ms, list);
            Assert.True(2 > 0); //, "sanity check:" + caption);
            Assert.True(ms.Length > 0); //, "data should be written:" + caption);
            ms.Position = 0;
#pragma warning disable CS0618
            var clone = (MyContractList)model.Deserialize(ms, null, typeof(MyContractList));
#pragma warning restore CS0618
            Assert.Single(clone); //, "count:" + caption);
            Assert.Equal("abc", clone[0]); //, "payload:" + caption);
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

        [Fact]
        public void TestBasicListAsMember()
        {
            var obj = new ListWrapper { BasicList = new List<string> { "abc" } };
            var clone = Serializer.DeepClone(obj);
            Assert.Null(clone.MyList);
            Assert.Null(clone.MyContractList);
            Assert.Single(clone.BasicList);
            Assert.Equal("abc", clone.BasicList[0]);
        }

        [Fact]
        public void TestMyListAsMember()
        {
            var obj = new ListWrapper { MyList = new MyList { "abc" } };
            var clone = Serializer.DeepClone(obj);
            Assert.Null(clone.BasicList);
            Assert.Null(clone.MyContractList);
            Assert.Single(clone.MyList);
            Assert.Equal("abc", clone.MyList[0]);
        }

        [Fact]
        public void TestMyContractListAsMember()
        {
            var obj = new ListWrapper { MyContractList = new MyContractList { "abc" } };
            var clone = Serializer.DeepClone(obj);
            Assert.Null(clone.BasicList);
            Assert.Null(clone.MyList);
            Assert.Single(clone.MyContractList);
            Assert.Equal("abc", clone.MyContractList[0]);
        }

        [Fact]
        public void SanityCheckListWrapper()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof (ListWrapper), true);

            var schema = model.GetSchema(null, ProtoSyntax.Proto2);

            Assert.Equal(@"syntax = ""proto2"";
package Examples.Issues;

message ListWrapper {
   repeated string BasicList = 1;
   repeated string MyList = 2;
   repeated string MyContractList = 3;
}
", schema, ignoreLineEndingDifferences: true);
            model.Compile("SanityCheckListWrapper", "SanityCheckListWrapper.dll");
            PEVerify.AssertValid("SanityCheckListWrapper.dll");
        }

    }
}
