using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProtoBuf;

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
        public void TestEnumerableBinary()
        {
            EnumParentStandardWrapper obj = new EnumParentStandardWrapper();
            Assert.IsTrue(Program.CheckBytes(obj,
                0x0A, 0x0A,  // field 1: obj, 10 bytes
                0x08, 0x01,  // field 1: variant, 1
                0x08, 0x02,  // field 1: variant, 2
                0x08, 0x03,  // field 1: variant, 3
                0x08, 0x04,  // field 1: variant, 4
                0x08, 0x05)); // field 1: variant, 5
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
        public void NWindPipeline()
        {
            DAL.Database masterDb = DAL.NWindTests.LoadDatabaseFromFile<DAL.Database>();
            int orderCount = masterDb.Orders.Count,
                lineCount = masterDb.Orders.Sum(o=>o.Lines.Count),
                unitCount = masterDb.Orders.SelectMany(o=>o.Lines).Sum(l=>(int)l.Quantity);

            decimal freight = masterDb.Orders.Sum(order => order.Freight).GetValueOrDefault(),
                value = masterDb.Orders.SelectMany(o => o.Lines).Sum(l => l.Quantity * l.UnitPrice);

            DatabaseReader db = DAL.NWindTests.LoadDatabaseFromFile<DatabaseReader>();

            Assert.AreEqual(830, orderCount);
            Assert.AreEqual(2155, lineCount);
            Assert.AreEqual(51317, unitCount);
            Assert.AreEqual(1354458.59M, value);

            Assert.AreEqual(orderCount, db.OrderReader.OrderCount);
            Assert.AreEqual(lineCount, db.OrderReader.LineCount);
            Assert.AreEqual(unitCount, db.OrderReader.UnitCount);
            Assert.AreEqual(freight, db.OrderReader.FreightTotal);
            Assert.AreEqual(value, db.OrderReader.ValueTotal);
        }

        [ProtoContract]
        class DatabaseReader
        {
            public DatabaseReader() { OrderReader = new OrderReader(); }
            [ProtoMember(1)]
            public OrderReader OrderReader {get;private set;}
        }

        class OrderReader : IEnumerable<DAL.Order>
        {
            public int OrderCount { get; private set; }
            public int LineCount { get; private set; }
            public int UnitCount { get; private set; }
            public decimal FreightTotal { get; private set; }
            public decimal ValueTotal { get; private set; }
            public void Add(DAL.Order order)
            {
                OrderCount++;
                LineCount += order.Lines.Count;
                UnitCount += order.Lines.Sum(l => l.Quantity);
                FreightTotal += order.Freight.GetValueOrDefault();
                ValueTotal += order.Lines.Sum(l => l.UnitPrice * l.Quantity);
            }

            IEnumerator<DAL.Order> IEnumerable<DAL.Order>.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
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
        [ProtoMember(1, DataFormat = DataFormat.Group)]
        public EnumWrapper Wrapper { get; private set; }
    }

    [ProtoContract(Name="EnumParentWrapper")]
    class EnumParentStandardWrapper
    {
        public EnumParentStandardWrapper() { Wrapper = new EnumWrapper(); }
        [ProtoMember(1, DataFormat = DataFormat.Default)]
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
