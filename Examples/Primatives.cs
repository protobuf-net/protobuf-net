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
    }
}
