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
        public void TestDateTime() {
            Primatives p = new Primatives { TestDateTime = NowToMillisecond };
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
            p.TestDecimalDefault = p.TestDecimalTwos = p.TestDecimalZigZag = 123456.789M;

            Primatives clone = Serializer.DeepClone(p);
            Assert.AreEqual(p.TestDecimalDefault,clone.TestDecimalDefault, "Default +ve");
            Assert.AreEqual(p.TestDecimalTwos, clone.TestDecimalTwos, "Twos +ve");
            Assert.AreEqual(p.TestDecimalZigZag, clone.TestDecimalZigZag, "ZigZag +ve");

            p.TestDecimalDefault = p.TestDecimalTwos = p.TestDecimalZigZag = -123456.789M;
            clone = Serializer.DeepClone(p);
            Assert.AreEqual(p.TestDecimalDefault, clone.TestDecimalDefault, "Default -ve");
            Assert.AreEqual(p.TestDecimalTwos, clone.TestDecimalTwos, "Twos -ve");
            Assert.AreEqual(p.TestDecimalZigZag, clone.TestDecimalZigZag, "ZigZag -ve");

        }
        [Test]
        public void TestZigZagNeg()
        {

            Primatives p = new Primatives { TestDecimalZigZag = -123456.789M },
                clone = Serializer.DeepClone(p);
            Assert.AreEqual(p.TestDecimalZigZag, clone.TestDecimalZigZag);
        }

        static DateTime NowToMillisecond
        {
            get
            {
                DateTime now = DateTime.Now;
                return new DateTime(now.Year, now.Month, now.Day,
                    now.Hour, now.Minute, now.Second, now.Millisecond);
            }
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
        [ProtoMember(4, DataFormat = DataFormat.TwosComplement)]
        public decimal TestDecimalTwos { get; set; }
        [ProtoMember(5, DataFormat = DataFormat.ZigZag)]
        public decimal TestDecimalZigZag { get; set; }
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
}
