using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using System.Collections;

namespace Examples
{
    [TestFixture]
    public class Pipeline
    {
        [Test]
        public void TestEnumerable()
        {
            EnumWrapper obj = new EnumWrapper();
            EnumWrapper clone = Serializer.DeepClone(obj);

            // the source object should have been read once, but not had any data added
            Assert.AreEqual(1, obj.SubData.IteratorCount, "obj IteratorCount");
            Assert.AreEqual(0, obj.SubData.Count, "obj Count");
            Assert.AreEqual(0, obj.SubData.Sum, "obj Sum");

            // the destination object should never have been read, but should have
            // had 5 values added
            Assert.AreEqual(0, clone.SubData.IteratorCount, "clone IteratorCount");
            Assert.AreEqual(5, clone.SubData.Count, "clone Count");
            Assert.AreEqual(1 + 2 + 3 + 4 + 5, clone.SubData.Sum, "clone Sum");
        }

        [Test]
        public void TestEnumerableProto()
        {
            string proto = Serializer.GetProto<EnumWrapper>();
            string expected = @"package Examples;

message EnumWrapper {
   repeated int32 SubData = 1;
}
";
            Assert.AreEqual(expected, proto);
        }
        [Test]
        public void TestEnumerableGroup()
        {
            EnumParentGroupWrapper obj = new EnumParentGroupWrapper();
            EnumParentGroupWrapper clone = Serializer.DeepClone(obj);

            // the source object should have been read once, but not had any data added
            Assert.AreEqual(1, obj.Wrapper.SubData.IteratorCount, "obj IteratorCount");
            Assert.AreEqual(0, obj.Wrapper.SubData.Count, "obj Count");
            Assert.AreEqual(0, obj.Wrapper.SubData.Sum, "obj Sum");

            // the destination object should never have been read, but should have
            // had 5 values added
            Assert.AreEqual(0, clone.Wrapper.SubData.IteratorCount, "clone IteratorCount");
            Assert.AreEqual(5, clone.Wrapper.SubData.Count, "clone Count");
            Assert.AreEqual(1 + 2 + 3 + 4 + 5, clone.Wrapper.SubData.Sum, "clone Sum");
        }

        [Test]
        public void TestEnumerableStandard()
        {
            EnumParentStandardWrapper obj = new EnumParentStandardWrapper();
            EnumParentStandardWrapper clone = Serializer.DeepClone(obj);

            // old: the source object should have been read twice
            // old: once to get the length-prefix, and once for the data
            // update: once only with buffering
            Assert.AreEqual(1, obj.Wrapper.SubData.IteratorCount, "obj IteratorCount");
            Assert.AreEqual(0, obj.Wrapper.SubData.Count, "obj Count");
            Assert.AreEqual(0, obj.Wrapper.SubData.Sum, "obj Sum");

            // the destination object should never have been read, but should have
            // had 5 values added
            Assert.AreEqual(0, clone.Wrapper.SubData.IteratorCount, "clone IteratorCount");
            Assert.AreEqual(5, clone.Wrapper.SubData.Count, "clone Count");
            Assert.AreEqual(1 + 2 + 3 + 4 + 5, clone.Wrapper.SubData.Sum, "clone Sum");
        }

        [Test]
        public void TestEnumerableGroupProto()
        {
            string proto = Serializer.GetProto<EnumParentGroupWrapper>();
            string expected = @"package Examples;

message EnumParentWrapper {
   optional group EnumWrapper Wrapper = 1;
}

message EnumWrapper {
   repeated int32 SubData = 1;
}
";
            Assert.AreEqual(expected, proto);
        }

        [Test]
        public void TestEnumerableStandardProto()
        {
            string proto = Serializer.GetProto<EnumParentStandardWrapper>();
            string expected = @"package Examples;

message EnumParentWrapper {
   optional EnumWrapper Wrapper = 1;
}

message EnumWrapper {
   repeated int32 SubData = 1;
}
";
            Assert.AreEqual(expected, proto);
        }
    }

    [ProtoContract(Name = "EnumParentWrapper")]
    class EnumParentGroupWrapper
    {
        public EnumParentGroupWrapper() { Wrapper = new EnumWrapper(); }
        [ProtoMember(1, IsGroup = true)]
        public EnumWrapper Wrapper { get; private set; }
    }

    [ProtoContract(Name="EnumParentWrapper")]
    class EnumParentStandardWrapper
    {
        public EnumParentStandardWrapper() { Wrapper = new EnumWrapper(); }
        [ProtoMember(1, IsGroup = false)]
        public EnumWrapper Wrapper { get; private set; }
    }

    [ProtoContract]
    class EnumWrapper
    {
        public EnumWrapper() { SubData = new EnumData(); }
        [ProtoMember(1)]
        public EnumData SubData {get;private set;}
    }

    public class EnumData : IEnumerable<int>
    {
        public EnumData() { }
        public int IteratorCount { get; private set; }
        public int Sum { get; private set; }
        public int Count { get; private set; }
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }
        public IEnumerator<int> GetEnumerator()
        {
            IteratorCount++;
            yield return 1;
            yield return 2;
            yield return 3;
            yield return 4;
            yield return 5;
        }

        public void Add(int data)
        {
            Count++;
            Sum += data;
        }
    }
}
