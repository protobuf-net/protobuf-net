using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProtoBuf;
using System.IO;

namespace Examples
{
    /* used to keep test API unchanged while extensibility commented out...
    class Extensible {
        public static void AppendValue<T>(object obj, int tag, T value) { throw new NotImplementedException(); }
        public static bool TryGetValue<T>(object obj, int tag, out T value) { throw new NotImplementedException(); }
        public static T GetValue<T>(object obj, int tag) { throw new NotImplementedException(); }
    }*/
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

            byte[] raw = GetExtensionBytes(small);
            
            float val;
            bool hasValue = Extensible.TryGetValue<float>(small, 3, out val);
            Assert.IsTrue(hasValue, "has value");
            Assert.AreEqual(obj.SomeFloat, val, "float value");

            hasValue = Extensible.TryGetValue<float>(small, 1000, out val);
            Assert.IsFalse(hasValue, "no value");
            Assert.AreEqual(default(float), val);
        }

        static byte[] GetExtensionBytes(IExtensible obj)
        {
            Assert.IsNotNull(obj, "null extensible");
            IExtension extn = obj.GetExtensionObject(false);
            Assert.IsNotNull(extn, "no extension object");
            Stream s = extn.BeginQuery();
            try
            {
                using(MemoryStream ms = new MemoryStream()) {
                int b; // really lazy clone...
                while ((b = s.ReadByte()) >= 0) { ms.WriteByte((byte)b); }
                return ms.ToArray();
                }
            } finally {
                extn.EndQuery(s);
            }
        }

        [Test]
        public void TestWriteExtended()
        {
            const float SOME_VALUE = 987.65F;
            SmallerObject obj = new SmallerObject();
            Extensible.AppendValue<float>(obj, 3, SOME_VALUE);

            byte[] raw = GetExtensionBytes(obj);
            Assert.AreEqual(5, raw.Length, "Extension Length");
            Assert.AreEqual((3 << 3) | 5, raw[0], "Prefix (3 Fixed32)");
            byte[] tmp = BitConverter.GetBytes(SOME_VALUE);
            if (!BitConverter.IsLittleEndian) Array.Reverse(tmp);
            Assert.AreEqual(tmp[0], raw[1], "Float32 Byte 0");
            Assert.AreEqual(tmp[1], raw[2], "Float32 Byte 1");
            Assert.AreEqual(tmp[2], raw[3], "Float32 Byte 2");
            Assert.AreEqual(tmp[3], raw[4], "Float32 Byte 3");

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
