using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;

namespace Examples
{

    [ProtoContract]
    class SmallerObject : Extensible
    {
        [ProtoMember(1)]
        public string Bof { get; set; }

        [ProtoMember(99)]
        public string Eof { get; set; }
    }
    [ProtoContract]
    class BiggerObject
    {
        [ProtoMember(1)]
        public string Bof { get; set; }

        [ProtoMember(2)]
        public int SomeInt32 { get; set; }

        [ProtoMember(3)]
        public float SomeFloat { get; set; }

        [ProtoMember(4)]
        public double SomeDouble { get; set; }

        [ProtoMember(5)]
        public byte[] SomeBlob { get; set; }

        [ProtoMember(6)]
        public string SomeString { get; set; }

        [ProtoMember(99)]
        public string Eof { get; set; }
    }

    [TestFixture]
    public class Extensibility
    {
        internal static BiggerObject GetBigObject()
        {
            return new BiggerObject
            {
                Bof = "BOF",
                SomeBlob = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 },
                SomeDouble = double.MaxValue / 2,
                SomeFloat = float.MinValue / 2,
                SomeInt32 = (int.MaxValue / 3) * 2,
                SomeString = "abcdefghijklmnopqrstuvwxyz",
                Eof = "EOF"
            };
        }
        [Test]
        public void TestRoundtrip()
        {
            BiggerObject obj = GetBigObject();

            SmallerObject tmp = Serializer.ChangeType<BiggerObject, SmallerObject>(obj);

            Assert.AreEqual(obj.Bof, tmp.Bof, "dehydrate");
            Assert.AreEqual(obj.Eof, tmp.Eof, "dehydrate");

            BiggerObject clone = Serializer.ChangeType<SmallerObject, BiggerObject>(tmp);

            Assert.AreEqual(obj.Bof, clone.Bof, "rehydrate");
            Assert.AreEqual(obj.Eof, clone.Eof, "rehydrate");
            Assert.AreEqual(obj.SomeDouble, clone.SomeDouble, "rehydrate");
            Assert.AreEqual(obj.SomeFloat, clone.SomeFloat, "rehydrate");
            Assert.AreEqual(obj.SomeInt32, clone.SomeInt32, "rehydrate");
            Assert.AreEqual(obj.SomeString, clone.SomeString, "rehydrate");
            Assert.IsTrue(Program.ArraysEqual(obj.SomeBlob, clone.SomeBlob), "rehydrate");
        }

        [Test]
        public void TestReadExtended()
        {
            BiggerObject obj = GetBigObject();
            SmallerObject small = Serializer.ChangeType<BiggerObject, SmallerObject>(obj);

            float val;
            bool hasValue = Extensible.TryGetValue<float>(small, 3, out val);
            Assert.IsTrue(hasValue, "has value");
            Assert.AreEqual(obj.SomeFloat, val, "float value");

            hasValue = Extensible.TryGetValue<float>(small, 1000, out val);
            Assert.IsFalse(hasValue, "no value");
            Assert.AreEqual(default(float), val);
        }

        [Test]
        public void TestWriteExtended()
        {
            const float SOME_VALUE = 987.65F;
            SmallerObject obj = new SmallerObject();
            Extensible.AppendValue<float>(obj, 3, SOME_VALUE);

            float readBack = Extensible.GetValue<float>(obj, 3);
            Assert.AreEqual(SOME_VALUE, readBack, "read back");

            BiggerObject big = Serializer.ChangeType<SmallerObject, BiggerObject>(obj);

            Assert.AreEqual(SOME_VALUE, big.SomeFloat, "deserialize");
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestReadShouldUseProperty()
        {
            SmallerObject obj = new SmallerObject { Bof = "hi" };
            string hi = Extensible.GetValue<string>(obj,1);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestReadInvalidTag()
        {
            SmallerObject obj = new SmallerObject { Bof = "hi" };
            string hi = Extensible.GetValue<string>(obj, 0);
        }
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestReadNull()
        {
            string hi = Extensible.GetValue<string>(null, 1);
        }
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestWriteNull()
        {
            Extensible.AppendValue<string>(null, 1, "hi");
        }
    }
}
