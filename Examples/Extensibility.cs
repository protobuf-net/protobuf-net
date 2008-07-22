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
    }
}
