using System;
using System.IO;
using ProtoBuf;
using NUnit.Framework;

namespace Examples.TestNumbers
{
    [ProtoContract]
    class NumRig
    {
        [ProtoMember(1, DataFormat=DataFormat.Default)]
        public int Int32Default { get; set; }
        [ProtoMember(2, DataFormat = DataFormat.ZigZag)]
        public int Int32ZigZag { get; set; }
        [ProtoMember(3, DataFormat = DataFormat.TwosComplement)]
        public int Int32TwosComplement { get; set; }
        [ProtoMember(4, DataFormat = DataFormat.FixedSize)]
        public int Int32FixedSize { get; set; }

        [ProtoMember(5, DataFormat = DataFormat.Default)]
        public uint UInt32Default { get; set; }
        [ProtoMember(7, DataFormat = DataFormat.TwosComplement)]
        public uint UInt32TwosComplement { get; set; }
        [ProtoMember(8, DataFormat = DataFormat.FixedSize)]
        public uint UInt32FixedSize { get; set; }

        [ProtoMember(9, DataFormat = DataFormat.Default)]
        public long Int64Default { get; set; }
        [ProtoMember(10, DataFormat = DataFormat.ZigZag)]
        public long Int64ZigZag { get; set; }
        [ProtoMember(11, DataFormat = DataFormat.TwosComplement)]
        public long Int64TwosComplement { get; set; }
        [ProtoMember(12, DataFormat = DataFormat.FixedSize)]
        public long Int64FixedSize { get; set; }

        [ProtoMember(13, DataFormat = DataFormat.Default)]
        public ulong UInt64Default { get; set; }
        [ProtoMember(15, DataFormat = DataFormat.TwosComplement)]
        public ulong UInt64TwosComplement { get; set; }
        [ProtoMember(16, DataFormat = DataFormat.FixedSize)]
        public ulong UInt64FixedSize { get; set; }
        
        [ProtoMember(17)]
        public string Foo { get; set; }
    }

    [ProtoContract]
    class ZigZagInt32
    {
        [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
        public int Foo { get; set; }
    }
    [ProtoContract]
    class TwosComplementInt32
    {
        [ProtoMember(1, DataFormat = DataFormat.TwosComplement)]
        public int Foo { get; set; }
    }

    [ProtoContract]
    class TwosComplementUInt32
    {
        [ProtoMember(1, DataFormat = DataFormat.TwosComplement)]
        public uint Foo { get; set; }
    }
    
    [ProtoContract]
    class ZigZagInt64
    {
        [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
        public long Foo { get; set; }
    }


    [TestFixture]
    public class SignTests
    {
        [Test]
        public void RoundTripBigPosativeZigZagInt64()
        {
            ZigZagInt64 obj = new ZigZagInt64 { Foo = 123456789 },
                clone = Serializer.DeepClone(obj);
            Assert.AreEqual(obj.Foo, clone.Foo);
        }

        [Test]
        public void RoundTripBigPosativeZigZagInt64ForDateTime()
        {
            // this test to simulate a typical DateTime value
            ZigZagInt64 obj = new ZigZagInt64 { Foo = 1216669168515 },
                clone = Serializer.DeepClone(obj);
            Assert.AreEqual(obj.Foo, clone.Foo);
        }
        
        [Test]
        public void RoundTripBigNegativeZigZagInt64() {
            ZigZagInt64 obj = new ZigZagInt64 { Foo = -123456789 },
                clone = Serializer.DeepClone(obj);
            clone = Serializer.DeepClone(obj);
            Assert.AreEqual(obj.Foo, clone.Foo);
        }

        [Test]
        public void TestSignTwosComplementInt32_0()
        {
            Assert.IsTrue(Program.CheckBytes(new TwosComplementInt32 { Foo = 0 }, 0x08, 0x00), "0");
        }
        [Test]
        public void TestSignTwosComplementInt32_1()
        {
            Assert.IsTrue(Program.CheckBytes(new TwosComplementInt32 { Foo = 1 }, 0x08, 0x01), "+1");
        }
        [Test]
        public void TestSignTwosComplementInt32_2()
        {
            Assert.IsTrue(Program.CheckBytes(new TwosComplementInt32 { Foo = 2 }, 0x08, 0x02), "+2");
        }
        [Test]
        public void TestSignTwosComplementInt32_m1()
        {
            Assert.IsTrue(Program.CheckBytes(new TwosComplementInt32 { Foo = -1 }, 0x08, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01), "-1");
        }
        [Test]
        public void TestSignTwosComplementInt32_m2()
        {
            Assert.IsTrue(Program.CheckBytes(new TwosComplementInt32 { Foo = -2 }, 0x08, 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01), "-2");
        }
        [Test]
        public void TestSignZigZagInt32_0()
        {
            Assert.IsTrue(Program.CheckBytes(new ZigZagInt32 { Foo = 0 }, 0x08, 0x00), "0");
        }
        [Test]
        public void TestSignZigZagInt32_1()
        {
            Assert.IsTrue(Program.CheckBytes(new ZigZagInt32 { Foo = 1 }, 0x08, 0x02), "+1");
        }
        [Test]
        public void TestSignZigZagInt32_2()
        {
            Assert.IsTrue(Program.CheckBytes(new ZigZagInt32 { Foo = 2 }, 0x08, 0x04), "+2");
        }
        [Test]
        public void TestSignZigZagInt32_m1()
        {
            Assert.IsTrue(Program.CheckBytes(new ZigZagInt32 { Foo = -1 }, 0x08, 0x01), "-1");
        }
        [Test]
        public void TestSignZigZagInt32_m2()
        {
            Assert.IsTrue(Program.CheckBytes(new ZigZagInt32 { Foo = -2 }, 0x08, 0x03), "-2");
        }        
        [Test]
        public void TestSignZigZagInt32_2147483647()
        {
            // encoding doc gives numbers in terms of uint equivalent
            ZigZagInt32 zz = new ZigZagInt32 { Foo = 2147483647 }, clone = Serializer.DeepClone(zz);
            Assert.AreEqual(zz.Foo, clone.Foo, "Roundtrip");
            TwosComplementUInt32 tc = Serializer.ChangeType<ZigZagInt32, TwosComplementUInt32>(zz);
            Assert.AreEqual(4294967294, tc.Foo);
        }
        [Test]
        public void TestSignZigZagInt32_m2147483648()
        {
            // encoding doc gives numbers in terms of uint equivalent
            ZigZagInt32 zz = new ZigZagInt32 { Foo = -2147483648 }, clone = Serializer.DeepClone(zz);
            Assert.AreEqual(zz.Foo, clone.Foo, "Roundtrip");
            TwosComplementUInt32 tc = Serializer.ChangeType<ZigZagInt32, TwosComplementUInt32>(zz);
            Assert.AreEqual(4294967295, tc.Foo);
        }

        [Test, ExpectedException(typeof(EndOfStreamException))]
        public void TestEOF()
        {
            Program.Build<ZigZagInt32>(0x08); // but no payload for field 1
        }

        [Test, ExpectedException(typeof(OverflowException))]
        public void TestOverflow()
        {
            Program.Build<ZigZagInt32>(0x08, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
        }

        [Test]
        public void SweepBitsInt32()
        {
            NumRig rig = new NumRig();
            const string SUCCESS = "bar";
            rig.Foo = SUCCESS; // to help test stream ending prematurely
            for (int i = 0; i < 32; i++)
            {
                int bigBit = i == 0 ? 0 : (1 << i - 1);
                for (int j = 0; j <= i; j++)
                {
                    int smallBit = 1 << j;
                    int val = bigBit | smallBit;
                    rig.Int32Default
                        = rig.Int32FixedSize
                        = rig.Int32TwosComplement
                        = rig.Int32ZigZag
                        = val;

                    NumRig clone = Serializer.DeepClone(rig);
                    Assert.AreEqual(val, rig.Int32Default);
                    Assert.AreEqual(val, rig.Int32FixedSize);
                    Assert.AreEqual(val, rig.Int32TwosComplement);
                    Assert.AreEqual(val, rig.Int32ZigZag);
                    Assert.AreEqual(SUCCESS, rig.Foo);
                }
            }
        }
        [Test]
        public void SweepBitsInt64()
        {
            NumRig rig = new NumRig();
            const string SUCCESS = "bar";
            rig.Foo = SUCCESS; // to help test stream ending prematurely
            for (int i = 0; i < 64; i++)
            {
                long bigBit = i == 0 ? 0 : (1 << i - 1);
                for (int j = 0; j <= i; j++)
                {
                    long smallBit = 1 << j;
                    long val = bigBit | smallBit;
                    rig.Int64Default
                        = rig.Int64FixedSize
                        = rig.Int64TwosComplement
                        = rig.Int64ZigZag
                        = val;

                    NumRig clone = Serializer.DeepClone(rig);
                    Assert.AreEqual(val, rig.Int64Default);
                    Assert.AreEqual(val, rig.Int64FixedSize);
                    Assert.AreEqual(val, rig.Int64ZigZag);
                    Assert.AreEqual(val, rig.Int64TwosComplement);
                    Assert.AreEqual(SUCCESS, rig.Foo);
                }
            }
        }
    }
}
