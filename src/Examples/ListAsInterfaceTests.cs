using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using ProtoBuf;

namespace Examples
{
    [ProtoContract]
    class DataWithListUninitialized : IDataWithList
    {
        [ProtoMember(1)]
        public IList<string> Data { get; internal set; } // non-public just for fun
    }
    [ProtoContract]
    class DataWithListInitialized : IDataWithList
    {
        private readonly IList<string> data = new List<string>();
        [ProtoMember(1)]
        public IList<string> Data { get { return data; } }
    }
    interface IDataWithList
    {
        IList<string> Data { get; }
    }

    
    public class ListAsInterfaceTests
    {
        [Fact]
        public void TestInitialized()
        {
            TestList<DataWithListInitialized>(new DataWithListInitialized());
        }

        [Fact]
        public void TestUninitialized()
        {
            TestList<DataWithListUninitialized>(new DataWithListUninitialized { Data = new List<string>() });
        }
        [Fact]
        public void TestDefaultListType()
        {
            IList<string> data = new List<string>();
            data.Add("abc");
            var clone = Serializer.DeepClone(data);
            Assert.NotNull(clone);
            Assert.NotSame(data, clone);
            Assert.Equal(1, clone.Count);
            Assert.Equal("abc", clone[0]);
        }

        static void TestList<T>(T original) where T : class, IDataWithList
        {
            Assert.NotNull(original); //, "original should be initialized");
            Assert.NotNull(original.Data); //, "original.Data should be initialized");
            Assert.Equal(0, original.Data.Count); //, "original.Data should be empty");

            original.Data.Add("abc");
            original.Data.Add("def");
            original.Data.Add("ghi");
            original.Data.Add("jkl");

            var clone = Serializer.DeepClone<T>(original);

            Assert.NotNull(clone); //, "clone");
            Assert.NotSame(original, clone);

            Assert.NotNull(clone.Data); //, "clone.Data");
            Assert.Equal(original.Data.Count, clone.Data.Count); //, "clone.Data.Count");
            Assert.True(Enumerable.SequenceEqual(original.Data, clone.Data)); //, "SequenceEqual");

        }
    }
}
