using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Examples
{
    [TestFixture]
    public class ExtendedTimeTests
    {
        [ProtoContract]
        class DateTimeFixed
        {
            [ProtoMember(1, DataFormat=DataFormat.FixedSize, IsRequired=true)]
            public DateTime When {get;set;}
        }
        [ProtoContract]
        class DateTimeGroup
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public DateTime When { get; set; }
        }
        [ProtoContract]
        class DateTimeString
        {
            [ProtoMember(1, DataFormat = DataFormat.Default)]
            public DateTime When { get; set; }
        }
        [ProtoContract]
        class TimeSpanFixed
        {
            [ProtoMember(1, DataFormat = DataFormat.FixedSize, IsRequired = true)]
            public TimeSpan When { get; set; }
        }
        [ProtoContract]
        class TimeSpanGroup
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public TimeSpan When { get; set; }
        }
        [ProtoContract]
        class TimeSpanString
        {
            [ProtoMember(1, DataFormat = DataFormat.Default)]
            public TimeSpan When { get; set; }
        }
        [ProtoContract]
        class Int64Fixed
        {
            [ProtoMember(1, DataFormat = DataFormat.FixedSize)]
            public long Value { get; set; }
        }

        static DateTime origin = new DateTime(1970,1,1);

        static DateTime KnownTimeWithTicks
        {
            get { return new DateTime(2008, 09, 15, 08, 19, 35).AddTicks(354); }
        }
        [Test]
        public void TickPrecisionTimeSpanTest()
        {
            DateTime dt = KnownTimeWithTicks;
            TimeSpan ts = KnownTimeWithTicks - new DateTime(2008, 1, 1);
            TimeSpanString val = new TimeSpanString { When = ts },
                clone = Serializer.DeepClone(val);
            Assert.AreEqual(ts, clone.When);
        }
        [Test]
        public void TickPrecisionDateTimeTest()
        {
            DateTime dt = KnownTimeWithTicks;

            DateTimeString val = new DateTimeString { When = dt },
                clone = Serializer.DeepClone(val);
            Assert.AreEqual(dt, clone.When);
        }

        [Test]
        public void TestDateTimeTicks()
        {
            TestDate(DateTime.Now);
            TestDate(DateTime.MinValue);
            TestDate(DateTime.MaxValue);
            Random rand = new Random();
            for (int i = 0; i < 500; i++)
            {
                DateTime dt = new DateTime(rand.Next(int.MaxValue));
            }
        }

        [Test]
        public void TestFixedOnly()
        {
            var when = new DateTime(2010, 05, 17, 8, 30, 0);

            DateTimeFixed val = new DateTimeFixed { When = when };
            var i64 = Serializer.ChangeType<DateTimeFixed, Int64Fixed>(val);
            long ticks = (when - origin).Ticks;
            Assert.AreEqual(ticks, i64.Value, "Wire value:" + when.ToString());
        }


        static void TestDate(DateTime when)
        {

            long ticks = (when - origin).Ticks;

            byte[] bits = BitConverter.GetBytes(ticks);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bits);
            }
            Array.Resize(ref bits, bits.Length + 1);
            Buffer.BlockCopy(bits, 0, bits, 1, bits.Length - 1);
            bits[0] = 9;

            Int64Fixed i64 = new Int64Fixed { Value = ticks };
            Assert.IsTrue(Program.CheckBytes(i64, bits));

            Int64Fixed i64Clone = Serializer.DeepClone(i64);
            Assert.AreEqual(ticks, i64Clone.Value, "Int64 roundtrip:" + ticks.ToString() + " (" + when.ToString() + ")");

            DateTimeFixed val = new DateTimeFixed { When = when},
                clone = ProtoBuf.Serializer.DeepClone(val);
            Assert.AreEqual(val.When, clone.When, "DateTime roundtrip:" + when.ToString());

            i64 = Serializer.ChangeType<DateTimeFixed, Int64Fixed>(val);
            
            Assert.AreEqual(ticks, i64.Value, "Wire value:" + when.ToString());

        }
    }
}
