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

    [TestFixture]
    public class SignTests
    {
        [Test]
        public void TestSignTwosComplementInt32()
        {
            Assert.IsTrue(Program.CheckBytes(new TwosComplementInt32 { Foo = 0 }, 0x08, 0x00), "0");
            Assert.IsTrue(Program.CheckBytes(new TwosComplementInt32 { Foo = 1 }, 0x08, 0x01), "+1");
            Assert.IsTrue(Program.CheckBytes(new TwosComplementInt32 { Foo = 2 }, 0x08, 0x02), "+2");
            Assert.IsTrue(Program.CheckBytes(new TwosComplementInt32 { Foo = -1 }, 0x08, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01), "-1");
            Assert.IsTrue(Program.CheckBytes(new TwosComplementInt32 { Foo = -2 }, 0x08, 0xFE, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01), "-2");
        }
        [Test]
        public void TestSignZigZagInt32()
        {
            Assert.IsTrue(Program.CheckBytes(new ZigZagInt32 { Foo = 0 }, 0x08, 0x00), "0");
            Assert.IsTrue(Program.CheckBytes(new ZigZagInt32 { Foo = 1 }, 0x08, 0x02), "+1");
            Assert.IsTrue(Program.CheckBytes(new ZigZagInt32 { Foo = 2 }, 0x08, 0x04), "+2");
            Assert.IsTrue(Program.CheckBytes(new ZigZagInt32 { Foo = -1 }, 0x08, 0x01), "-1");
            Assert.IsTrue(Program.CheckBytes(new ZigZagInt32 { Foo = -2 }, 0x08, 0x03), "-2");

            Assert.IsTrue(Program.CheckBytes(new ZigZagInt32 { Foo = 2147483647 }, 0x08, 0xFE, 0xFF, 0xFF, 0xFF, 0x0F), "2147483647");
            Assert.IsTrue(Program.CheckBytes(new ZigZagInt32 { Foo = -2147483648 }, 0x08, 0xFF, 0xFF, 0xFF, 0xFF, 0x0F), "-2147483648");
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
