#if !COREFX
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples
{
    
    public class Pipeline
    {
        [Fact]
        public void TestEnumerable()
        {
            EnumWrapper obj = new EnumWrapper();
            EnumWrapper clone = Serializer.DeepClone(obj);

            // the source object should have been read once, but not had any data added
            Assert.Equal(1, obj.SubData.IteratorCount); //, "obj IteratorCount");
            Assert.Equal(0, obj.SubData.Count); //, "obj Count");
            Assert.Equal(0, obj.SubData.Sum); //, "obj Sum");

            // the destination object should never have been read, but should have
            // had 5 values added
            Assert.Equal(0, clone.SubData.IteratorCount); //, "clone IteratorCount");
            Assert.Equal(5, clone.SubData.Count); //, "clone Count");
            Assert.Equal(1 + 2 + 3 + 4 + 5, clone.SubData.Sum); //, "clone Sum");
        }

        [Fact]
        public void TestEnumerableProto()
        {

            string proto = Serializer.GetProto<EnumWrapper>();

            string expected = @"syntax = ""proto2"";
package Examples;

message EnumWrapper {
   repeated int32 SubData = 1;
}
";
            Assert.Equal(expected, proto);
        }
        [Fact]
        public void TestEnumerableGroup()
        {
            EnumParentGroupWrapper obj = new EnumParentGroupWrapper();
            EnumParentGroupWrapper clone = Serializer.DeepClone(obj);

            // the source object should have been read once, but not had any data added
            Assert.Equal(1, obj.Wrapper.SubData.IteratorCount); //, "obj IteratorCount");
            Assert.Equal(0, obj.Wrapper.SubData.Count); //, "obj Count");
            Assert.Equal(0, obj.Wrapper.SubData.Sum); //, "obj Sum");

            // the destination object should never have been read, but should have
            // had 5 values added
            Assert.Equal(0, clone.Wrapper.SubData.IteratorCount); //, "clone IteratorCount");
            Assert.Equal(5, clone.Wrapper.SubData.Count); //, "clone Count");
            Assert.Equal(1 + 2 + 3 + 4 + 5, clone.Wrapper.SubData.Sum); //, "clone Sum");
        }

        [Fact]
        public void TestEnumerableBinary()
        {
            EnumParentStandardWrapper obj = new EnumParentStandardWrapper();
            Assert.True(Program.CheckBytes(obj,
                0x0A, 0x0A,  // field 1: obj, 10 bytes
                0x08, 0x01,  // field 1: variant, 1
                0x08, 0x02,  // field 1: variant, 2
                0x08, 0x03,  // field 1: variant, 3
                0x08, 0x04,  // field 1: variant, 4
                0x08, 0x05)); // field 1: variant, 5
        }
        [Fact]
        public void TestEnumerableStandard()
        {
            EnumParentStandardWrapper obj = new EnumParentStandardWrapper();
            EnumParentStandardWrapper clone = Serializer.DeepClone(obj);

            // old: the source object should have been read twice
            // old: once to get the length-prefix, and once for the data
            // update: once only with buffering
            Assert.Equal(1, obj.Wrapper.SubData.IteratorCount); //, "obj IteratorCount");
            Assert.Equal(0, obj.Wrapper.SubData.Count); //, "obj Count");
            Assert.Equal(0, obj.Wrapper.SubData.Sum); //, "obj Sum");

            // the destination object should never have been read, but should have
            // had 5 values added
            Assert.Equal(0, clone.Wrapper.SubData.IteratorCount); //, "clone IteratorCount");
            Assert.Equal(5, clone.Wrapper.SubData.Count); //, "clone Count");
            Assert.Equal(1 + 2 + 3 + 4 + 5, clone.Wrapper.SubData.Sum); //, "clone Sum");
        }

        [Fact]
        public void TestEnumerableGroupProto()
        {

            string proto = Serializer.GetProto<EnumParentGroupWrapper>();

            string expected = @"syntax = ""proto2"";
package Examples;

message EnumParentWrapper {
   optional group EnumWrapper Wrapper = 1;
}
message EnumWrapper {
   repeated int32 SubData = 1;
}
";
            Assert.Equal(expected, proto);
        }

        [Fact]
        public void NWindPipeline()
        {
            DAL.Database masterDb = DAL.NWindTests.LoadDatabaseFromFile<DAL.Database>(RuntimeTypeModel.Default);
            int orderCount = masterDb.Orders.Count,
                lineCount = masterDb.Orders.Sum(o => o.Lines.Count),
                unitCount = masterDb.Orders.SelectMany(o => o.Lines).Sum(l => (int)l.Quantity);

            decimal freight = masterDb.Orders.Sum(order => order.Freight).GetValueOrDefault(),
                value = masterDb.Orders.SelectMany(o => o.Lines).Sum(l => l.Quantity * l.UnitPrice);

            Assert.Equal(830, orderCount);
            Assert.Equal(2155, lineCount);
            Assert.Equal(51317, unitCount);
            Assert.Equal(1354458.59M, value);

            DatabaseReader db = DAL.NWindTests.LoadDatabaseFromFile<DatabaseReader>(RuntimeTypeModel.Default);


            Assert.Equal(orderCount, db.OrderReader.OrderCount);
            Assert.Equal(lineCount, db.OrderReader.LineCount);
            Assert.Equal(unitCount, db.OrderReader.UnitCount);
            Assert.Equal(freight, db.OrderReader.FreightTotal);
            Assert.Equal(value, db.OrderReader.ValueTotal);
        }

        [ProtoContract]
        class DatabaseReader
        {
            public DatabaseReader() { OrderReader = new OrderReader(); }
            [ProtoMember(1)]
            public OrderReader OrderReader { get; private set; }
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

        [Fact]
        public void TestEnumerableStandardProto()
        {

            string proto = Serializer.GetProto<EnumParentStandardWrapper>();

            string expected = @"syntax = ""proto2"";
package Examples;

message EnumParentWrapper {
   optional EnumWrapper Wrapper = 1;
}
message EnumWrapper {
   repeated int32 SubData = 1;
}
";
            Assert.Equal(expected, proto);
        }
    }

    [ProtoContract(Name = "EnumParentWrapper")]
    class EnumParentGroupWrapper
    {
        public EnumParentGroupWrapper() { Wrapper = new EnumWrapper(); }
        [ProtoMember(1, DataFormat = DataFormat.Group)]
        public EnumWrapper Wrapper { get; private set; }
    }

    [ProtoContract(Name = "EnumParentWrapper")]
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
        public EnumData SubData { get; private set; }
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
#endif