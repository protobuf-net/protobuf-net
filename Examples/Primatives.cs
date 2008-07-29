using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Runtime.Serialization;
using ProtoBuf;
using System.IO;

namespace Examples
{
    [TestFixture]
    public class PrimativeTests {

        [Test]
        public void TestDateTimeZero()
        {
            Primatives p = new Primatives { TestDateTime = new DateTime(1970,1,1) };
            Assert.AreEqual(p.TestDateTime, Serializer.DeepClone(p).TestDateTime);
        }

        [Test]
        public void TestDateTimeOrigin()
        {
            // zero
            DateTime origin = new DateTime(1970, 1, 1);
            int len;

            Assert.AreEqual(origin, TestDateTime(origin, out len));
            Assert.AreEqual(2, len, "0 len");
            Assert.AreEqual(origin.AddDays(1), TestDateTime(origin.AddDays(1), out len));
            Assert.AreEqual(4, len, "+1 len");
            Assert.AreEqual(origin.AddDays(-1), TestDateTime(origin.AddDays(-1), out len));
            Assert.AreEqual(4, len, "-1 len");
        }

        [Test]
        public void TestTimeSpanZero()
        {
            int len;
            TimeSpan ts = TimeSpan.Zero;
            Assert.AreEqual(ts, TestTimeSpan(ts, out len));
            Assert.AreEqual(2, len, "0 len");
        }

        [Test]
        public void TestTimeSpan36Hours()
        {
            int len;
            TimeSpan ts = new TimeSpan(36,0,0);
            Assert.AreEqual(ts, TestTimeSpan(ts, out len));
            Assert.AreEqual(6, len, "+36 hour len");
        }

        [Test]
        public void TestTimeSpanMinus3Hours()
        {
            int len;
            TimeSpan ts = new TimeSpan(0,-3, 0);
            Assert.AreEqual(ts, TestTimeSpan(ts, out len));
            Assert.AreEqual(6, len, "-3 hour len");
        }

        [Test]
        public void TestTimeSpanMinValue()
        {
            int len;
            TimeSpan ts = TimeSpan.MinValue;
            Assert.AreEqual(ts, TestTimeSpan(ts, out len));
            Assert.AreEqual(6, len, "min len");
        }
        
        [Test]
        public void TestTimeSpanMaxValue()
        {
            int len;
            TimeSpan ts = TimeSpan.MaxValue;
            Assert.AreEqual(ts, TestTimeSpan(ts, out len));
            Assert.AreEqual(6, len, "max len");
        }
        [ProtoContract]
        class DateTimeOnly
        {
            [ProtoMember(1)]
            public DateTime When { get; set; }
        }
        [ProtoContract]
        class TimeSpanOnly
        {
            [ProtoMember(1)]
            public TimeSpan HowLong { get; set; }
        }

        static DateTime TestDateTime(DateTime value, out int len) {
            DateTimeOnly p = new DateTimeOnly { When = value };
            using(MemoryStream ms = new MemoryStream()) {
                Serializer.Serialize(ms, p);
                ms.Position = 0;
                p = Serializer.Deserialize<DateTimeOnly>(ms);
                len = (int)ms.Length;
                return p.When;
            }
        }
        static TimeSpan TestTimeSpan(TimeSpan value, out int len)
        {
            TimeSpanOnly p = new TimeSpanOnly { HowLong = value };
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, p);
                ms.Position = 0;
                p = Serializer.Deserialize<TimeSpanOnly>(ms);
                len = (int)ms.Length;
                return p.HowLong;
            }
        }


        [Test]
        public void TestDateTimeMinValue()
        {
            Primatives p = new Primatives { TestDateTime = DateTime.MinValue };
            Assert.AreEqual(p.TestDateTime, Serializer.DeepClone(p).TestDateTime);
        }
        [Test]
        public void TestDateTimeMaxValue()
        {
            Primatives p = new Primatives { TestDateTime = DateTime.MaxValue };
            Assert.AreEqual(p.TestDateTime, Serializer.DeepClone(p).TestDateTime);
        }
        [Test]
        public void TestDateTimeNowMillis() {
            Primatives p = new Primatives { TestDateTime = NowToMillisecond };
            Assert.AreEqual(p.TestDateTime, Serializer.DeepClone(p).TestDateTime);
        }
        [Test]
        public void TestDateTimeNowSeconds()
        {
            Primatives p = new Primatives { TestDateTime = NowToSecond };
            Assert.AreEqual(p.TestDateTime, Serializer.DeepClone(p).TestDateTime);
        }

        [Test]
        public void TestDateTimeToday()
        {
            Primatives p = new Primatives { TestDateTime = DateTime.Today };
            Assert.AreEqual(p.TestDateTime, Serializer.DeepClone(p).TestDateTime);
        }
        [Test]
        public void TestBoolean()
        {
            Primatives p = new Primatives { TestBoolean = true };
            Assert.AreEqual(p.TestBoolean, Serializer.DeepClone(p).TestBoolean);
            p.TestBoolean = false;
            Assert.AreEqual(p.TestBoolean, Serializer.DeepClone(p).TestBoolean);
        }
        [Test]
        public void TestString()
        {
            Primatives p = new Primatives();
            p.TestString = "";
            Assert.AreEqual(p.TestString, Serializer.DeepClone(p).TestString, "Empty");
            p.TestString = "foo";
            Assert.AreEqual(p.TestString, Serializer.DeepClone(p).TestString, "Non-empty");
            p.TestString = null;
            Assert.AreEqual(p.TestString, Serializer.DeepClone(p).TestString, "Null");
        }
       
        [Test]
        public void TestDecimal()
        {
            Primatives p = new Primatives();
            p.TestDecimalDefault = 123456.789M; //p.TestDecimalTwos = p.TestDecimalZigZag = 

            Primatives clone = Serializer.DeepClone(p);
            Assert.AreEqual(p.TestDecimalDefault,clone.TestDecimalDefault, "Default +ve");
            //Assert.AreEqual(p.TestDecimalTwos, clone.TestDecimalTwos, "Twos +ve");
            //Assert.AreEqual(p.TestDecimalZigZag, clone.TestDecimalZigZag, "ZigZag +ve");

            p.TestDecimalDefault = -123456.789M; //p.TestDecimalTwos = p.TestDecimalZigZag = 
            clone = Serializer.DeepClone(p);
            Assert.AreEqual(p.TestDecimalDefault, clone.TestDecimalDefault, "Default -ve");
            //Assert.AreEqual(p.TestDecimalTwos, clone.TestDecimalTwos, "Twos -ve");
            //Assert.AreEqual(p.TestDecimalZigZag, clone.TestDecimalZigZag, "ZigZag -ve");

            p.TestDecimalDefault = 0; // p.TestDecimalTwos = p.TestDecimalZigZag = 0;
            clone = Serializer.DeepClone(p);
            Assert.AreEqual(p.TestDecimalDefault, clone.TestDecimalDefault, "Default 0");
            //Assert.AreEqual(p.TestDecimalTwos, clone.TestDecimalTwos, "Twos 0");
            //Assert.AreEqual(p.TestDecimalZigZag, clone.TestDecimalZigZag, "ZigZag 0");

            p.TestDecimalDefault = decimal.Parse("0.000"); // p.TestDecimalTwos = p.TestDecimalZigZag =
             clone = Serializer.DeepClone(p);
            Assert.AreEqual(p.TestDecimalDefault, clone.TestDecimalDefault, "Default 0.000");
            //Assert.AreEqual(p.TestDecimalTwos, clone.TestDecimalTwos, "Twos 0.000");
            //Assert.AreEqual(p.TestDecimalZigZag, clone.TestDecimalZigZag, "ZigZag 0.000");

            p.TestDecimalDefault = decimal.Parse("1.000"); //p.TestDecimalTwos = p.TestDecimalZigZag = 
            clone = Serializer.DeepClone(p);
            Assert.AreEqual(p.TestDecimalDefault, clone.TestDecimalDefault, "Default 1.000");
            //Assert.AreEqual(p.TestDecimalTwos, clone.TestDecimalTwos, "Twos 1.000");
            //Assert.AreEqual(p.TestDecimalZigZag, clone.TestDecimalZigZag, "ZigZag 1.000");

        }
        /*
        [Test]
        public void TestZigZagNeg()
        {

            Primatives p = new Primatives { TestDecimalZigZag = -123456.789M },
                clone = Serializer.DeepClone(p);
            Assert.AreEqual(p.TestDecimalZigZag, clone.TestDecimalZigZag);
        }
        */
        static DateTime NowToMillisecond
        {
            get
            {
                DateTime now = DateTime.Now;
                return new DateTime(now.Year, now.Month, now.Day,
                    now.Hour, now.Minute, now.Second, now.Millisecond);
            }
        }
        static DateTime NowToSecond
        {
            get
            {
                DateTime now = DateTime.Now;
                return new DateTime(now.Year, now.Month, now.Day,
                    now.Hour, now.Minute, now.Second, 0);
            }
        }

        [Test]
        public void TestChars()
        {
            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                Assert.AreEqual(c, TestChar(c));
            }
        }

        [Test]
        public void TestEmptyUri()
        {
            Assert.AreEqual(null, TestUri(null), "null");

        }
        [Test]
        public void TestNonEmptyUri() {
            Uri uri = new Uri("http://test.example.com/demo");
            Assert.AreEqual(uri, TestUri(uri), "not null");
        }
        static Uri TestUri(Uri value)
        {
            return Serializer.DeepClone(new UriData { Foo = value }).Foo;
        }
        static char TestChar(char value)
        {
            return Serializer.DeepClone(new CharData { Foo = value }).Foo;
        }
        [Test]
        public void TestByteTwos()
        {
            Assert.AreEqual(0, TestByteTwos(0));
            byte value = 1;
            for (int i = 0; i < 8; i++)
            {
                Assert.AreEqual(value, TestByteTwos(value));
                value <<= 1;
            }
        }

        [Test]
        public void TestSByteTwos()
        {
            Assert.AreEqual(0, TestSByteTwos(0));
            sbyte value = 1;
            for (int i = 0; i < 7; i++)
            {
                Assert.AreEqual(value, TestSByteTwos(value));
                value <<= 1;
            }
            value = -1;
            for (int i = 0; i < 7; i++)
            {
                Assert.AreEqual(value, TestSByteTwos(value));
                value <<= 1;
            }
        }
        [Test]
        public void TestSByteZigZag()
        {
            Assert.AreEqual(0, TestSByteZigZag(0));
            sbyte value = 1;
            for (int i = 0; i < 7; i++)
            {
                Assert.AreEqual(value, TestSByteZigZag(value));
                value <<= 1;
            }
            value = -1;
            for (int i = 0; i < 7; i++)
            {
                Assert.AreEqual(value, TestSByteZigZag(value));
                value <<= 1;
            }
        }

        static byte TestByteTwos(byte value)
        {
            return Serializer.DeepClone(new BytePrimatives { ByteTwos = value }).ByteTwos;
        }
        static sbyte TestSByteTwos(sbyte value)
        {
            return Serializer.DeepClone(new BytePrimatives { SByteTwos = value }).SByteTwos;
        }
        static sbyte TestSByteZigZag(sbyte value)
        {
            return Serializer.DeepClone(new BytePrimatives { SByteZigZag = value }).SByteZigZag;
        }
    }
    [DataContract]
    class Primatives
    {
        [DataMember(Order=1)]
        public bool TestBoolean { get; set; }
        [DataMember(Order = 2)]
        public DateTime TestDateTime { get; set; }
        [ProtoMember(3, DataFormat = DataFormat.Default)]
        public decimal TestDecimalDefault { get; set; }
        
        /*[ProtoMember(4, DataFormat = DataFormat.TwosComplement)]
        public decimal TestDecimalTwos { get; set; }
        [ProtoMember(5, DataFormat = DataFormat.ZigZag)]
        public decimal TestDecimalZigZag { get; set; }
         * */
        [ProtoMember(6)]
        public string TestString { get; set; }
    }

    [ProtoContract]
    class BytePrimatives
    {
        [ProtoMember(1, DataFormat = DataFormat.TwosComplement)]
        public byte ByteTwos { get; set; }

        [ProtoMember(2, DataFormat = DataFormat.TwosComplement)]
        public sbyte SByteTwos { get; set; }

        [ProtoMember(3, DataFormat = DataFormat.ZigZag)]
        public sbyte SByteZigZag { get; set; }
    }

    [ProtoContract]
    class CharData
    {
        [ProtoMember(1)]
        public char Foo { get; set; }
    }

    [ProtoContract]
    class UriData
    {
        [ProtoMember(1)]
        public Uri Foo { get; set; }
    }
}
