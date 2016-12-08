using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
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
    [ProtoContract]
    class DataWithNullableIList
    {
        [ProtoMember(1)]
        public IList<int?> Data { get; set; }
    }

    [TestFixture]
    public class ListAsInterfaceTests
    {
        [Test]
        public void TestInitialized()
        {
            TestList<DataWithListInitialized>(new DataWithListInitialized());
        }

        [Test]
        public void TestUninitialized()
        {
            TestList<DataWithListUninitialized>(new DataWithListUninitialized { Data = new List<string>() });
        }
        [Test]
        public void TestDefaultListType()
        {
            IList<string> data = new List<string>();
            data.Add("abc");
            var clone = Serializer.DeepClone(data);
            Assert.IsNotNull(clone);
            Assert.AreNotSame(data, clone);
            Assert.AreEqual(1, clone.Count);
            Assert.AreEqual("abc", clone[0]);
        }

        [Test]
        public void TestNullableIListWithoutData()
        {
            var original = new DataWithNullableIList();
            var clone = Serializer.DeepClone(original);
            Assert.IsNotNull(clone);
        }

        [Test]
        public void TestNullableIListWithData()
        {
            var original = new DataWithNullableIList { Data = new List<int?> { 4 } };
            var clone = Serializer.DeepClone(original);
            Assert.IsNotNull(clone);
            Assert.AreNotSame(original, clone);
            Assert.IsNotNull(clone.Data);
            Assert.AreEqual(1, clone.Data.Count);
            Assert.AreEqual(4, clone.Data[0]);
        }

        [Test]
        public void TestNullableIListWithNullData()
        {
            try
            {
                var original = new DataWithNullableIList { Data = new List<int?> { null, 4 } };
                Serializer.DeepClone(original);
                Assert.Fail();
            }
            catch (NullReferenceException) { /* expected */ }
        }

        [Test]
        public void TestNullableIListWithNullDataDontThrow()
        {
            var model = ProtoBuf.Meta.RuntimeTypeModel.Create();
            model.DontThrowNullReference = true;

            var original = new DataWithNullableIList { Data = new List<int?> { null, 4 } };
            var clone = (DataWithNullableIList)model.DeepClone(original);
            Assert.IsNotNull(clone);
            Assert.AreNotSame(original, clone);
            Assert.IsNotNull(clone.Data);
            Assert.AreEqual(1, clone.Data.Count);
            Assert.AreEqual(4, clone.Data[0]);
        }

        [Test]
        public void TestNullableIListWithNullDataSupportNull()
        {
            var model = ProtoBuf.Meta.RuntimeTypeModel.Create();
            model.Add(typeof(DataWithNullableIList), true)[1].SupportNull = true;

            var original = new DataWithNullableIList { Data = new List<int?> { null, 4 } };
            var clone = (DataWithNullableIList)model.DeepClone(original);
            Assert.IsNotNull(clone);
            Assert.AreNotSame(original, clone);
            Assert.IsNotNull(clone.Data);
            Assert.AreEqual(2, clone.Data.Count);
            Assert.AreEqual(null, clone.Data[0]);
            Assert.AreEqual(4, clone.Data[1]);
        }


        [Test]
        public void TestNullableIListWithNullDataSupportNullDontThrow()
        {
            var model = ProtoBuf.Meta.RuntimeTypeModel.Create();
            model.DontThrowNullReference = true;
            model.Add(typeof(DataWithNullableIList), true)[1].SupportNull = true;

            var original = new DataWithNullableIList { Data = new List<int?> { null, 4 } };
            var clone = (DataWithNullableIList)model.DeepClone(original);
            Assert.IsNotNull(clone);
            Assert.AreNotSame(original, clone);
            Assert.IsNotNull(clone.Data);
            Assert.AreEqual(2, clone.Data.Count);
            Assert.AreEqual(null, clone.Data[0]);
            Assert.AreEqual(4, clone.Data[1]);
        }

        static void TestList<T>(T original) where T : class, IDataWithList
        {
            Assert.IsNotNull(original, "original should be initialized");
            Assert.IsNotNull(original.Data, "original.Data should be initialized");
            Assert.AreEqual(0, original.Data.Count, "original.Data should be empty");

            original.Data.Add("abc");
            original.Data.Add("def");
            original.Data.Add("ghi");
            original.Data.Add("jkl");

            var clone = Serializer.DeepClone<T>(original);

            Assert.IsNotNull(clone, "clone");
            Assert.AreNotSame(original, clone);

            Assert.IsNotNull(clone.Data, "clone.Data");
            Assert.AreEqual(original.Data.Count, clone.Data.Count, "clone.Data.Count");
            Assert.IsTrue(Enumerable.SequenceEqual(original.Data, clone.Data), "SequenceEqual");

        }
    }
}
