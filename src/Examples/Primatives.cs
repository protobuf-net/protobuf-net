using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples
{
    
    public class PrimativeTests {

        [Fact]
        public void TestDateTimeZero()
        {
            Primatives p = new Primatives { TestDateTime = new DateTime(1970,1,1) };
            Assert.Equal(p.TestDateTime, Serializer.DeepClone(p).TestDateTime);
        }

        [Fact]
        public void TestDateTimeOrigin()
        {
            // zero
            DateTime origin = new DateTime(1970, 1, 1);

            Assert.Equal(origin, TestDateTime(origin, out int len));
            Assert.Equal(2, len); //, "0 len");
            Assert.Equal(origin.AddDays(1), TestDateTime(origin.AddDays(1), out len));
            Assert.Equal(4, len); //, "+1 len");
            Assert.Equal(origin.AddDays(-1), TestDateTime(origin.AddDays(-1), out len));
            Assert.Equal(4, len); //, "-1 len");
        }

        [Fact]
        public void TestTimeSpanZero()
        {
            TimeSpan ts = TimeSpan.Zero;
            Assert.Equal(ts, TestTimeSpan(ts, out int len));
            Assert.Equal(0, len); //, "0 len");
        }

        [Fact]
        public void TestTimeSpan36Hours()
        {
            TimeSpan ts = new TimeSpan(36, 0, 0);
            Assert.Equal(ts, TestTimeSpan(ts, out int len));
            Assert.Equal(6, len); //, "+36 hour len");
        }

        [Fact]
        public void TestTimeSpanMinus3Hours()
        {
            TimeSpan ts = new TimeSpan(0, -3, 0);
            Assert.Equal(ts, TestTimeSpan(ts, out int len));
            Assert.Equal(6, len); //, "-3 hour len");
        }

        [Fact]
        public void TestTimeSpanMinValue()
        {
            TimeSpan ts = TimeSpan.MinValue;
            Assert.Equal(ts, TestTimeSpan(ts, out int len));
            Assert.Equal(6, len); //, "min len");
        }
        
        [Fact]
        public void TestTimeSpanMaxValue()
        {
            TimeSpan ts = TimeSpan.MaxValue;
            Assert.Equal(ts, TestTimeSpan(ts, out int len));
            Assert.Equal(6, len); //, "max len");
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

        [ProtoContract]
        class TimeSpanDefaulted
        {
            public TimeSpanDefaulted() {
                HowLong = new TimeSpan(0,1,0);
            }
            [ProtoMember(1), DefaultValue("00:01:00")]
            public TimeSpan HowLong { get; set; }
        }

        [Fact]
        public void TestTimeSpanDefaulted()
        {
            TimeSpanDefaulted def = new TimeSpanDefaulted(),
                clone = Serializer.DeepClone(def);
            Assert.Equal(def.HowLong, clone.HowLong);

            def.HowLong = new TimeSpan(0, 0, 0);
            clone = Serializer.DeepClone(def);
            Assert.Equal(def.HowLong, clone.HowLong);

            Serializer.PrepareSerializer<TimeSpanDefaulted>();
            def = new TimeSpanDefaulted();
            clone = Serializer.DeepClone(def);
            Assert.Equal(def.HowLong, clone.HowLong);

            def.HowLong = new TimeSpan(0, 0, 0);
            clone = Serializer.DeepClone(def);
            Assert.Equal(def.HowLong, clone.HowLong);
        }

        [Fact]
        public void TestValueTimeUnit()
        {
            TimeSpanOnly ts = Program.Build<TimeSpanOnly>(0x0A, 0x04, // tag 1 string, 4 bytes
                0x08, 0x08, // tag 1; value: 4 (zigzag)
                0x10, 0x01); // tag 2; unit: hour

            Assert.Equal(new TimeSpan(4, 0, 0), ts.HowLong);
        }
        [Fact]
        public void TestInvalidTimeUnit() {
            Program.ExpectFailure<ProtoException>(() =>
            {
                TimeSpanOnly ts = Program.Build<TimeSpanOnly>(0x0A, 0x04, // tag 1 string, 4 bytes
                    0x08, 0x08, // tag 1; value: 4 (zigzag)
                    0x10, 0x4A); // tag 2; unit: invalid
            });
        }
        [Fact]
        public void TestValidMinMax()
        {
            TimeSpanOnly ts = Program.Build<TimeSpanOnly>(0x0A, 0x04, // tag 1 string, 4 bytes
                    0x08, 0x02, // tag 1; value: 1 (zigzag)
                    0x10, 0x0F); // tag 2; min/max

            Assert.Equal(TimeSpan.MaxValue, ts.HowLong);

            ts = Program.Build<TimeSpanOnly>(0x0A, 0x04, // tag 1 string, 4 bytes
                0x08, 0x01, // tag 1; value: -1 (zigzag)
                0x10, 0x0F); // tag 2; min/max

            Assert.Equal(TimeSpan.MinValue, ts.HowLong);
        }
        [Fact]
        public void TestInvalidMinMax()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                TimeSpanOnly ts = Program.Build<TimeSpanOnly>(0x0A, 0x04, // tag 1 string, 4 bytes
                        0x08, 0x03, // tag 1; invalid
                        0x10, 0x0F); // tag 2; min/max
            });
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


        [Fact]
        public void TestDateTimeMinValue()
        {
            Primatives p = new Primatives { TestDateTime = DateTime.MinValue };
            Assert.Equal(p.TestDateTime, Serializer.DeepClone(p).TestDateTime);
        }
        [Fact]
        public void TestDateTimeMaxValue()
        {
            Primatives p = new Primatives { TestDateTime = DateTime.MaxValue };
            Assert.Equal(p.TestDateTime, Serializer.DeepClone(p).TestDateTime);
        }
        [Fact]
        public void TestDateTimeNowMillis() {
            Primatives p = new Primatives { TestDateTime = NowToMillisecond };
            Assert.Equal(p.TestDateTime, Serializer.DeepClone(p).TestDateTime);
        }
        [Fact]
        public void TestDateTimeNowMillisGrouped()
        {
            PrimativeGrouped p = new PrimativeGrouped { When = NowToMillisecond };
            Assert.Equal(p.When, Serializer.DeepClone(p).When);
        }

        [Fact]
        public void TestDateTimeNowMillisGroupedWrapped()
        {
            PrimativeGroupedWrapper p = new PrimativeGroupedWrapper { Child = { When = NowToMillisecond } };
            Assert.Equal(p.Child.When, Serializer.DeepClone(p).Child.When);
        }

        [Fact]
        public void TestDateTimeNowMillisNonGroupedWrapped()
        {
            PrimativeNonGroupedWrapper p = new PrimativeNonGroupedWrapper { Child = { When = NowToMillisecond } };
            Assert.Equal(p.Child.When, Serializer.DeepClone(p).Child.When);
        }

        [Fact]
        public void TestTimeSpanGrouped()
        {
            PrimativeGrouped p = new PrimativeGrouped { HowLong = TimeSpan.FromSeconds(123456)};
            Assert.Equal(p.HowLong, Serializer.DeepClone(p).HowLong);
        }

        [Fact]
        public void TestTimeSpanGroupedWrapped()
        {
            PrimativeGroupedWrapper p = new PrimativeGroupedWrapper { Child = { HowLong = TimeSpan.FromSeconds(123456) } };
            Assert.Equal(p.Child.HowLong, Serializer.DeepClone(p).Child.HowLong);
        }

        [Fact]
        public void TestTimeSpanNonGroupedWrapped()
        {
            PrimativeNonGroupedWrapper p = new PrimativeNonGroupedWrapper { Child = { HowLong = TimeSpan.FromSeconds(123456) } };
            Assert.Equal(p.Child.HowLong, Serializer.DeepClone(p).Child.HowLong);
        }

        [Fact]
        public void TestDecimalGrouped()
        {
            PrimativeGrouped p = new PrimativeGrouped { HowMuch = 123.4567M };
            Assert.Equal(p.HowMuch, Serializer.DeepClone(p).HowMuch);
        }

        [Fact]
        public void TestDecimalGroupedWrapped()
        {
            PrimativeGroupedWrapper p = new PrimativeGroupedWrapper { Child = { HowMuch = 123.4567M } };
            Assert.Equal(p.Child.HowMuch, Serializer.DeepClone(p).Child.HowMuch);
        }

        [Fact]
        public void TestDecimalNonGroupedWrapped()
        {
            PrimativeNonGroupedWrapper p = new PrimativeNonGroupedWrapper { Child = { HowMuch = 123.4567M } };
            Assert.Equal(p.Child.HowMuch, Serializer.DeepClone(p).Child.HowMuch);
        }

        [ProtoContract]
        public class PrimativeGrouped
        {
            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public DateTime When { get; set; }

            [ProtoMember(2, DataFormat = DataFormat.Group)]
            public TimeSpan HowLong { get; set; }

            [ProtoMember(3, DataFormat = DataFormat.Group)]
            public decimal HowMuch { get; set; }
        }

        [ProtoContract]
        public class PrimativeGroupedWrapper
        {
            public PrimativeGroupedWrapper() { Child = new PrimativeGrouped(); }

            [ProtoMember(1, DataFormat = DataFormat.Group)]
            public PrimativeGrouped Child { get; private set; }
        }
        [ProtoContract]
        public class PrimativeNonGroupedWrapper
        {
            public PrimativeNonGroupedWrapper() { Child = new PrimativeGrouped(); }

            [ProtoMember(1, DataFormat = DataFormat.Default)]
            public PrimativeGrouped Child { get; private set; }
        }

        [Fact]
        public void TestDateTimeNowSeconds()
        {
            Primatives p = new Primatives { TestDateTime = NowToSecond };
            Assert.Equal(p.TestDateTime, Serializer.DeepClone(p).TestDateTime);
        }

        [Fact]
        public void TestDateTimeToday()
        {
            Primatives p = new Primatives { TestDateTime = DateTime.Today };
            Assert.Equal(p.TestDateTime, Serializer.DeepClone(p).TestDateTime);
        }
        [Fact]
        public void TestBoolean()
        {
            Primatives p = new Primatives { TestBoolean = true };
            Assert.Equal(p.TestBoolean, Serializer.DeepClone(p).TestBoolean);
            p.TestBoolean = false;
            Assert.Equal(p.TestBoolean, Serializer.DeepClone(p).TestBoolean);
        }
        [Fact]
        public void TestString()
        {
            Primatives p = new Primatives();
            p.TestString = "";
            Assert.Equal(p.TestString, Serializer.DeepClone(p).TestString); //, "Empty");
            p.TestString = "foo";
            Assert.Equal(p.TestString, Serializer.DeepClone(p).TestString); //, "Non-empty");
            p.TestString = null;
            Assert.Equal(p.TestString, Serializer.DeepClone(p).TestString); //, "Null");
        }


        [Fact]
        public void TestDecimalUnits()
        {
            Primatives p = new Primatives { TestDecimalDefault = decimal.Zero};
            Assert.Equal(p.TestDecimalDefault, Serializer.DeepClone(p).TestDecimalDefault);

            p.TestDecimalDefault = decimal.MinusOne;
            Assert.Equal(p.TestDecimalDefault, Serializer.DeepClone(p).TestDecimalDefault);

            p.TestDecimalDefault = decimal.One;
            Assert.Equal(p.TestDecimalDefault, Serializer.DeepClone(p).TestDecimalDefault);

            p = Program.Build<Primatives>(0x1A, 0x00);
            Assert.Equal(decimal.Zero, p.TestDecimalDefault);

            p = Program.Build<Primatives>();
            Assert.Equal(29M, p.TestDecimalDefault);
        }

        [Fact]
        public void TestDecimalExtremes()
        {
            Primatives p = new Primatives(), clone;

            p.TestDecimalDefault = decimal.MaxValue;
            clone = Serializer.DeepClone(p);
            Assert.Equal(p.TestDecimalDefault, clone.TestDecimalDefault); //, "Max");

            p.TestDecimalDefault = decimal.MaxValue - 1234.5M;
            clone = Serializer.DeepClone(p);
            Assert.Equal(p.TestDecimalDefault, clone.TestDecimalDefault); //, "Nearly max");

            p.TestDecimalDefault = decimal.MinValue;
            clone = Serializer.DeepClone(p);
            Assert.Equal(p.TestDecimalDefault, clone.TestDecimalDefault); //, "Min");

            p.TestDecimalDefault = decimal.MinValue + 1234.5M;
            clone = Serializer.DeepClone(p);
            Assert.Equal(p.TestDecimalDefault, clone.TestDecimalDefault); //, "Nearly min");

            p.TestDecimalDefault = 0.00000000000000000000000123M;
            clone = Serializer.DeepClone(p);
            Assert.Equal(p.TestDecimalDefault, clone.TestDecimalDefault); //, "Very small +ve");

            p.TestDecimalDefault = -p.TestDecimalDefault;
            clone = Serializer.DeepClone(p);
            Assert.Equal(p.TestDecimalDefault, clone.TestDecimalDefault); //, "Very small -ve");
        }
        [Fact]
        public void TestDecimal()
        {
            Primatives p = new Primatives();
            p.TestDecimalDefault = 123456.789M; //p.TestDecimalTwos = p.TestDecimalZigZag = 

            Primatives clone = Serializer.DeepClone(p);
            Assert.Equal(p.TestDecimalDefault,clone.TestDecimalDefault); //, "Default +ve");
            //Assert.Equal(p.TestDecimalTwos, clone.TestDecimalTwos, "Twos +ve");
            //Assert.Equal(p.TestDecimalZigZag, clone.TestDecimalZigZag, "ZigZag +ve");

            p.TestDecimalDefault = -123456.789M; //p.TestDecimalTwos = p.TestDecimalZigZag = 
            clone = Serializer.DeepClone(p);
            Assert.Equal(p.TestDecimalDefault, clone.TestDecimalDefault); //, "Default -ve");
            //Assert.Equal(p.TestDecimalTwos, clone.TestDecimalTwos, "Twos -ve");
            //Assert.Equal(p.TestDecimalZigZag, clone.TestDecimalZigZag, "ZigZag -ve");

            p.TestDecimalDefault = 0; // p.TestDecimalTwos = p.TestDecimalZigZag = 0;
            clone = Serializer.DeepClone(p);
            Assert.Equal(p.TestDecimalDefault, clone.TestDecimalDefault); //, "Default 0");
            //Assert.Equal(p.TestDecimalTwos, clone.TestDecimalTwos, "Twos 0");
            //Assert.Equal(p.TestDecimalZigZag, clone.TestDecimalZigZag, "ZigZag 0");

            p.TestDecimalDefault = decimal.Parse("0.000", CultureInfo.InvariantCulture); // p.TestDecimalTwos = p.TestDecimalZigZag =
             clone = Serializer.DeepClone(p);
            Assert.Equal(p.TestDecimalDefault, clone.TestDecimalDefault); //, "Default 0.000");
            //Assert.Equal(p.TestDecimalTwos, clone.TestDecimalTwos, "Twos 0.000");
            //Assert.Equal(p.TestDecimalZigZag, clone.TestDecimalZigZag, "ZigZag 0.000");

            p.TestDecimalDefault = decimal.Parse("1.000", CultureInfo.InvariantCulture ); //p.TestDecimalTwos = p.TestDecimalZigZag = 
            clone = Serializer.DeepClone(p);
            Assert.Equal(p.TestDecimalDefault, clone.TestDecimalDefault); //, "Default 1.000");
            //Assert.Equal(p.TestDecimalTwos, clone.TestDecimalTwos, "Twos 1.000");
            //Assert.Equal(p.TestDecimalZigZag, clone.TestDecimalZigZag, "ZigZag 1.000");

        }
        /*
        [Fact]
        public void TestZigZagNeg()
        {

            Primatives p = new Primatives { TestDecimalZigZag = -123456.789M },
                clone = Serializer.DeepClone(p);
            Assert.Equal(p.TestDecimalZigZag, clone.TestDecimalZigZag);
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

        [Fact]
        public void TestChars()
        {
            for (char c = char.MinValue; c < char.MaxValue; c++)
            {
                Assert.Equal(c, TestChar(c));
            }
        }

        [Fact]
        public void TestEmptyUri()
        {
            Assert.Null(TestUri(null)); //, "null");

        }
        [Fact]
        public void TestNonEmptyUri() {
            Uri uri = new Uri("http://test.example.com/demo");
            Assert.Equal(uri, TestUri(uri)); //, "not null");
        }

        [Fact]
        public void TestNonEmptyUriAllCompilationModes()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(UriData), true);
            UriData test = new UriData { Foo = new Uri("http://test.example.com/demo") };

            UriData clone = (UriData) model.DeepClone(test);
            Assert.Equal(test.Foo, clone.Foo); //, "Runtime");

            var compiled = model.Compile("TestNonEmptyUriAllCompilationModes", "TestNonEmptyUriAllCompilationModes.dll");
            PEVerify.AssertValid("TestNonEmptyUriAllCompilationModes.dll");
            model.CompileInPlace();
            clone = (UriData)model.DeepClone(test);
            Assert.Equal(test.Foo, clone.Foo); //, "CompileInPlace");

            clone = (UriData)compiled.DeepClone(test);
            Assert.Equal(test.Foo, clone.Foo); //, "CompileIn");
        }

        [Fact]
        public void TestNonEmptyUriWithDefaultAllCompilationModes()
        {
            var model = RuntimeTypeModel.Create();
            model.Add(typeof(UriDataWithDefault), true);
            UriDataWithDefault test = new UriDataWithDefault { Foo = new Uri("http://test.example.com/demo") },
                defaulted = new UriDataWithDefault { Foo = new Uri("http://abc") };

            UriDataWithDefault clone = (UriDataWithDefault)model.DeepClone(test);
            Assert.Equal(test.Foo, clone.Foo); //, "Runtime");
            clone = (UriDataWithDefault)model.DeepClone(defaulted);
            Assert.Equal(defaulted.Foo, clone.Foo); //, "Runtime");

            var compiled = model.Compile("TestNonEmptyUriWithDefaultAllCompilationModes", "TestNonEmptyUriWithDefaultAllCompilationModes.dll");
            PEVerify.AssertValid("TestNonEmptyUriWithDefaultAllCompilationModes.dll");
            model.CompileInPlace();
            clone = (UriDataWithDefault)model.DeepClone(test);
            Assert.Equal(test.Foo, clone.Foo); //, "CompileInPlace");
            clone = (UriDataWithDefault)model.DeepClone(defaulted);
            Assert.Equal(defaulted.Foo, clone.Foo); //, "CompileInPlace");

            clone = (UriDataWithDefault)compiled.DeepClone(test);
            Assert.Equal(test.Foo, clone.Foo); //, "Compile");
            clone = (UriDataWithDefault)compiled.DeepClone(defaulted);
            Assert.Equal(defaulted.Foo, clone.Foo); //, "Compile");
        }

        [Fact]
        public void TestEncodedUri()
        {
            Uri uri = new Uri("http://www.example.com/for%2bbar");
            Assert.Equal(uri, TestUri(uri)); //, "null");
        }
        static Uri TestUri(Uri value)
        {
            return Serializer.DeepClone(new UriData { Foo = value }).Foo;
        }
        static char TestChar(char value)
        {
            return Serializer.DeepClone(new CharData { Foo = value }).Foo;
        }
        [Fact]
        public void TestByteTwos()
        {
            Assert.Equal(0, TestByteTwosImpl(0));
            byte value = 1;
            for (int i = 0; i < 8; i++)
            {
                Assert.Equal(value, TestByteTwosImpl(value));
                value <<= 1;
            }
        }

        [Fact]
        public void TestSByteTwos()
        {
            Assert.Equal(0, TestSByteTwoImpls(0));
            sbyte value = 1;
            for (int i = 0; i < 7; i++)
            {
                Assert.Equal(value, TestSByteTwoImpls(value));
                value <<= 1;
            }
            value = -1;
            for (int i = 0; i < 7; i++)
            {
                Assert.Equal(value, TestSByteTwoImpls(value));
                value <<= 1;
            }
        }
        [Fact]
        public void TestSByteZigZag()
        {
            Assert.Equal(0, TestSByteZigZagImpl(0));
            sbyte value = 1;
            for (int i = 0; i < 7; i++)
            {
                Assert.Equal(value, TestSByteZigZagImpl(value));
                value <<= 1;
            }
            value = -1;
            for (int i = 0; i < 7; i++)
            {
                Assert.Equal(value, TestSByteZigZagImpl(value));
                value <<= 1;
            }
        }

        static byte TestByteTwosImpl(byte value)
        {
            return Serializer.DeepClone(new BytePrimatives { ByteTwos = value }).ByteTwos;
        }
        static sbyte TestSByteTwoImpls(sbyte value)
        {
            return Serializer.DeepClone(new BytePrimatives { SByteTwos = value }).SByteTwos;
        }
        static sbyte TestSByteZigZagImpl(sbyte value)
        {
            return Serializer.DeepClone(new BytePrimatives { SByteZigZag = value }).SByteZigZag;
        }
    }
    [DataContract]
    class Primatives
    {
        public Primatives()
        {
            TestDecimalDefault = 29M;
            TestDateTime = new DateTime(2008, 1, 1);
        }
        [DataMember(Order=1)]

        public bool TestBoolean { get; set; }
        [DataMember(Order = 2)]
        [DefaultValue("01 Jan 2008")]
        public DateTime TestDateTime { get; set; }
        [ProtoMember(3, DataFormat = DataFormat.Default)]
        [DefaultValue(29)]
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
    public class UriData
    {
        [ProtoMember(1)]
        public Uri Foo { get; set; }
    }

    [ProtoContract]
    public class UriDataWithDefault
    {
        public UriDataWithDefault()
        {
            Foo = new Uri("http://abc");
        }
        [ProtoMember(1), DefaultValue("http://abc")]
        public Uri Foo { get; set; }
    }
}
