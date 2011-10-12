using System;
using System.ComponentModel;
using System.IO;
using NUnit.Framework;
using ProtoBuf;

namespace Examples
{
    interface IExtTest
    {
        string Bof { get; set; }
        string Eof { get; set; }
    }

    [ProtoContract]
    class InterfaceBased : object, ProtoBuf.IExtensible, IExtTest, System.ComponentModel.INotifyPropertyChanged
    {
        [ProtoMember(1)]
        public string Bof { get; set; }

        private string eof;
        [ProtoMember(99, DataFormat = ProtoBuf.DataFormat.Default)]
        public string Eof
        {
            get { return eof;}
            set
            {
                eof = value;
                OnPropertyChanged("Eof");
            }
        }

        private IExtension extensionObject;
        IExtension IExtensible.GetExtensionObject(bool createIfMissing)
        {
            return Extensible.GetExtensionObject(ref extensionObject, createIfMissing);
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if(PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    [ProtoContract]
    class SmallerObject : Extensible, IExtTest
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

    [TestFixture, Ignore("Not fully re-implemented")]
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
        public void TestRoundtripSmaller()
        {
            TestRoundTrip<SmallerObject>();
        }

        [Test]
        public void TestRoundtripInterfaceBased()
        {
            TestRoundTrip<InterfaceBased>();
        }

        static void TestRoundTrip<T>() where T : IExtTest, IExtensible, new() {

            BiggerObject obj = GetBigObject();

            T tmp = Serializer.ChangeType<BiggerObject, T>(obj);

            Assert.AreEqual(obj.Bof, tmp.Bof, "dehydrate");
            Assert.AreEqual(obj.Eof, tmp.Eof, "dehydrate");

            BiggerObject clone = Serializer.ChangeType<T, BiggerObject>(tmp);

            Assert.AreEqual(obj.Bof, clone.Bof, "rehydrate");
            Assert.AreEqual(obj.Eof, clone.Eof, "rehydrate");
            Assert.AreEqual(obj.SomeDouble, clone.SomeDouble, "rehydrate");
            Assert.AreEqual(obj.SomeFloat, clone.SomeFloat, "rehydrate");
            Assert.AreEqual(obj.SomeInt32, clone.SomeInt32, "rehydrate");
            Assert.AreEqual(obj.SomeString, clone.SomeString, "rehydrate");
            Assert.IsTrue(Program.ArraysEqual(obj.SomeBlob, clone.SomeBlob), "rehydrate");
        }

        [Test]
        public void TestReadExtendedSmallerObject()
        {
            TestReadExt<SmallerObject>();
        }

        [Test]
        public void TestReadExtendedInterfaceBased()
        {
            TestReadExt<InterfaceBased>();
        }

        static void TestReadExt<T>() where T : IExtTest, IExtensible, new()
        {
            BiggerObject obj = GetBigObject();
            
            T small = Serializer.ChangeType<BiggerObject, T>(obj);

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
        public void TestWriteExtendedSmaller()
        {
            TestWriteExt<SmallerObject>();
        }
        [Test]
        public void TestWriteExtendedInterfaceBased()
        {
            TestWriteExt<InterfaceBased>();
        }

        static void TestWriteExt<T>() where T : IExtTest, IExtensible, new() {
            const float SOME_VALUE = 987.65F;
            T obj = new T();
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

            BiggerObject big = Serializer.ChangeType<T, BiggerObject>(obj);

            Assert.AreEqual(SOME_VALUE, big.SomeFloat, "deserialize");
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestReadShouldUsePropertySmaller()
        {
            TestReadShouldUseProperty<SmallerObject>();
        }

        [Test, ExpectedException(typeof(ArgumentException))]
        public void TestReadShouldUsePropertyInterfaceBased()
        {
            TestReadShouldUseProperty<InterfaceBased>();
        }

        static void TestReadShouldUseProperty<T>() where T : IExtTest, IExtensible, new()
        {
            T obj = new T { Bof = "hi" };
            string hi = Extensible.GetValue<string>(obj,1);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestReadInvalidTagSmaller()
        {
            TestReadInvalidTag<SmallerObject>();
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestReadInvalidTagInterfaceBased()
        {
            TestReadInvalidTag<InterfaceBased>();
        }

        static void TestReadInvalidTag<T>() where T : IExtTest, IExtensible, new()
        {
            T obj = new T {Bof = "hi"};
            string hi = Extensible.GetValue<string>(obj, 0);
        }
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestReadNullSmaller()
        {
            TestReadNull<SmallerObject>();
        }
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestReadNullInterfaceBased()
        {
            TestReadNull<InterfaceBased>();
        }

        static void TestReadNull<T>() where T : IExtTest, IExtensible, new()
        {
            string hi = Extensible.GetValue<string>(null, 1);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestWriteNullSmaller()
        {
            TestWriteNull<SmallerObject>();
        }
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestWriteNullInterfaceBased()
        {
            TestWriteNull<InterfaceBased>();
        }
        static void TestWriteNull<T>() where T : IExtTest, IExtensible, new()
        {
            Extensible.AppendValue<string>(null, 1, "hi");
        }
    }
}
