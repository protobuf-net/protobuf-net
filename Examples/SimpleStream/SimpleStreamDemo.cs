using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Text;
using System.Xml.Serialization;
using ProtoBuf;
using System.Runtime.Serialization;
#if NET_3_0
using System.ServiceModel;
using ProtoBuf.ServiceModel;
#endif
#if NET_3_5
using System.Runtime.Serialization.Json;
using NUnit.Framework;
using Examples.DesignIdeas;
using System.Collections.Generic;
using ProtoSharp.Core;
using Serializer = ProtoBuf.Serializer;
#endif

namespace Examples.SimpleStream
{
    
    [TestFixture]
    public class SimpleStreamDemo
    {

        [Test]
        public void FirstSample()
        {
            Test1 t1 = new Test1 { A = 150 };
            Assert.IsTrue(Program.CheckBytes(t1, 0x08, 0x96, 0x01));
        }
        [Test]
        public void StringSample()
        {
            Test2 t2 = new Test2 { B = "testing" };
            Assert.IsTrue(Program.CheckBytes(t2, 0x12, 0x07, 0x74, 0x65, 0x73, 0x74, 0x69, 0x6e, 0x67));
        }
        [Test]
        public void MultiByteUTF8()
        {
            Test2 t2 = new Test2 { B = "Toms Spezialitäten" },
                clone = Serializer.DeepClone(t2);
            Assert.AreEqual(t2.B, clone.B);
            
        }

        [Test]
        public void EmbeddedMessageSample()
        {
            Test3 t3 = new Test3 { C = new Test1 { A = 150 } };
            Assert.IsTrue(Program.CheckBytes(t3, 0x1a, 0x03, 0x08, 0x96, 0x01));
        }

        public void PerfTestSimple(int count)
        {
            Test1 t1 = new Test1 { A = 150 };
            Assert.IsTrue(LoadTestItem(t1, count, count, true, true, true, true, 0x08, 0x96, 0x01));
        }
        public void PerfTestString(int count)
        {
            Test2 t2 = new Test2 { B = "testing" };
            Assert.IsTrue(LoadTestItem(t2, count, count, true, true, true, true, 0x12, 0x07, 0x74, 0x65, 0x73, 0x74, 0x69, 0x6e, 0x67));
        }
        public void PerfTestEmbedded(int count)
        {
            Test3 t3 = new Test3 { C = new Test1 { A = 150 } };
            Assert.IsTrue(LoadTestItem(t3, count, count, true, true, true, true, 0x1a, 0x03, 0x08, 0x96, 0x01));
        }

        [ProtoContract]
        class TwoFields
        {
            [ProtoMember(1)]
            public int Foo { get; set; }
            [ProtoMember(2)]
            public int Bar { get; set; }
        }
        [Test]
        public void FieldsWrongOrder()
        {
            TwoFields t1 = Program.Build<TwoFields>(0x08, 0x96, 0x01, 0x10, 0x82, 0x01);
            Assert.AreEqual(150, t1.Foo, "Foo, ascending");
            Assert.AreEqual(130, t1.Bar, "Bar, ascending");
            t1 = Program.Build<TwoFields>(0x10, 0x82, 0x01, 0x08, 0x96, 0x01);
            Assert.AreEqual(150, t1.Foo, "Foo, descending");
            Assert.AreEqual(130, t1.Bar, "Bar, descending");
        }
        
        [Test]
        public void MultipleSameField()
        {
            Test1 t1 = Program.Build<Test1>(0x08, 0x96, 0x01, 0x08, 0x82, 0x01);
            Assert.AreEqual(130, t1.A);
        }

        [ProtoContract]
        class ItemWithBlob
        {
            [ProtoMember(1)]
            public byte[] Foo { get; set; }
        }

        [Test]
        public void Blob()
        {
            ItemWithBlob blob = new ItemWithBlob(), clone = Serializer.DeepClone(blob);
            Assert.IsTrue(Program.CheckBytes(blob, new byte[0]), "Empty serialization");
            Assert.IsTrue(Program.ArraysEqual(blob.Foo, clone.Foo), "Clone should be empty");

            blob.Foo = new byte[] { 0x01, 0x02, 0x03 };
            clone = Serializer.DeepClone(blob);
            Assert.IsTrue(Program.ArraysEqual(blob.Foo, clone.Foo), "Clone should match");

            Assert.IsTrue(Program.CheckBytes(blob, 0x0A, 0x03, 0x01, 0x02, 0x03), "Stream should match");
        }

        public enum SomeEnum
        {
            [ProtoEnum(Value = 3)]
            Foo= 1
        }
        [XmlType]
        public class SomeEnumEntity
        {
            [XmlElement(Order = 2)]
            public SomeEnum Enum { get; set; }
        }
        [Test]
        public void TestDuffEnum()
        {
            SomeEnumEntity dee = new SomeEnumEntity {Enum = SomeEnum.Foo};
            Assert.IsTrue(Program.CheckBytes(dee, 0x10, 0x03));
        }
        [Test, ExpectedException(ExceptionType = typeof(KeyNotFoundException))]
        public void TestSerializeUndefinedEnum()
        {
            SomeEnumEntity dee = new SomeEnumEntity { Enum = 0};
            Serializer.Serialize(Stream.Null, dee);
        }
        [Test, ExpectedException(ExceptionType = typeof(KeyNotFoundException))]
        public void TestDeserializeUndefinedEnum()
        {
            Program.Build<SomeEnumEntity>(0x10, 0x09);
        }

        public class NotAContract
        {
            public int X { get; set; }
        }
        [Test, ExpectedException(ExceptionType = typeof(InvalidOperationException))]
        public void TestNotAContract()
        {
            NotAContract nac = new NotAContract { X = 4 };
            Serializer.Serialize(Stream.Null, nac);
        }

        public static bool LoadTestItem<T>(T item, int count, int protoCount, bool testBinary, bool testXml, bool testProtoSharp, bool writeJson, params byte[] expected) where T : class, new()
        {
            bool pass = true;
            string name = typeof(T).Name;
            Console.WriteLine("\t{0}", name);
            Stopwatch serializeWatch, deserializeWatch;
            T pbClone, psClone = null;
            Console.WriteLine("\t(times based on {0} iterations ({1} for .proto))", count, protoCount);
            Console.WriteLine("||*Serializer*||*size*||*serialize*||*deserialize*||");
            byte[] pbnetBuffer;
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, item);
                ms.Position = 0;
                pbClone = Serializer.Deserialize<T>(ms);
                byte[] data = ms.ToArray();
                if (expected != null && !Program.ArraysEqual(data, expected))
                {
                    Console.WriteLine("\t*** serialization failure");
                    WriteBytes("Binary", data);
                }
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                serializeWatch = Stopwatch.StartNew();
                for (int i = 0; i < protoCount; i++)
                {
                    Serializer.Serialize(Stream.Null, item);
                }
                serializeWatch.Stop();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                deserializeWatch = Stopwatch.StartNew();
                for (int i = 0; i < protoCount; i++)
                {
                    ms.Position = 0;
                    Serializer.Deserialize<T>(ms);
                }
                deserializeWatch.Stop();
                Console.WriteLine("||protobuf-net||{0:###,###,###}||{1:###,###,###}||{2:###,###,###}||",
                    ms.Length, serializeWatch.ElapsedMilliseconds, deserializeWatch.ElapsedMilliseconds);

                pbnetBuffer = ms.ToArray();
            }
            if (testProtoSharp)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    ProtoSharp.Core.MessageWriter mw = new ProtoSharp.Core.MessageWriter(ms),
                        nullWriter = new ProtoSharp.Core.MessageWriter(Stream.Null);
                    mw.WriteMessage(item);
                    ms.Position = 0;

                    byte[] buffer = ms.ToArray();
                    
                    psClone = ProtoSharp.Core.MessageReader.Read<T>(buffer);
                    if (expected != null && !Program.ArraysEqual(buffer, expected))
                    {
                        Console.WriteLine("\t*** serialization failure");
                        WriteBytes("Binary", buffer);
                    }
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    serializeWatch = Stopwatch.StartNew();
                    for (int i = 0; i < protoCount; i++)
                    {
                        nullWriter.WriteMessage(item);
                    }
                    serializeWatch.Stop();
                    
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    deserializeWatch = Stopwatch.StartNew();
                    for (int i = 0; i < protoCount; i++)
                    {
                        ms.Position = 0;
                        ProtoSharp.Core.MessageReader.Read<T>(ms);
                    }
                    deserializeWatch.Stop();
                    Console.WriteLine("||[http://code.google.com/p/protosharp/ proto#]||{0:###,###,###}||{1:###,###,###}||{2:###,###,###}||",
                        buffer.Length, serializeWatch.ElapsedMilliseconds, deserializeWatch.ElapsedMilliseconds);
                }
            }
            if (testBinary)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, item);
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    serializeWatch = Stopwatch.StartNew();
                    for (int i = 0; i < count; i++)
                    {
                        bf.Serialize(Stream.Null, item);
                    }
                    serializeWatch.Stop();
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    deserializeWatch = Stopwatch.StartNew();
                    for (int i = 0; i < count; i++)
                    {
                        ms.Position = 0;
                        bf.Deserialize(ms);
                    }
                    deserializeWatch.Stop();
                    Console.WriteLine("||`BinaryFormatter`||{0:###,###,###}||{1:###,###,###}||{2:###,###,###}||",
                        ms.Length, serializeWatch.ElapsedMilliseconds, deserializeWatch.ElapsedMilliseconds);
                }
                using (MemoryStream ms = new MemoryStream())
                {
                    SoapFormatter sf = new SoapFormatter();
                    sf.Serialize(ms, item);
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    serializeWatch = Stopwatch.StartNew();
                    for (int i = 0; i < count; i++)
                    {
                        sf.Serialize(Stream.Null, item);
                    }
                    serializeWatch.Stop();
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    deserializeWatch = Stopwatch.StartNew();
                    for (int i = 0; i < count; i++)
                    {
                        ms.Position = 0;
                        sf.Deserialize(ms);
                    }
                    deserializeWatch.Stop();
                    Console.WriteLine("||`SoapFormatter`||{0:###,###,###}||{1:###,###,###}||{2:###,###,###}||",
                        ms.Length, serializeWatch.ElapsedMilliseconds, deserializeWatch.ElapsedMilliseconds);
                }
            }
            if (testXml)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    XmlSerializer xser = new XmlSerializer(typeof(T));
                    xser.Serialize(ms, item);
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    serializeWatch = Stopwatch.StartNew();
                    for (int i = 0; i < count; i++)
                    {
                        xser.Serialize(Stream.Null, item);
                    }
                    serializeWatch.Stop();
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                    deserializeWatch = Stopwatch.StartNew();
                    for (int i = 0; i < count; i++)
                    {
                        ms.Position = 0;
                        xser.Deserialize(ms);
                    }
                    deserializeWatch.Stop();
                    Console.WriteLine("||`XmlSerializer`||{0:###,###,###}||{1:###,###,###}||{2:###,###,###}||",
                        ms.Length, serializeWatch.ElapsedMilliseconds, deserializeWatch.ElapsedMilliseconds);
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractSerializer xser = new DataContractSerializer(typeof(T));
                xser.WriteObject(ms, item);
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                serializeWatch = Stopwatch.StartNew();
                for (int i = 0; i < count; i++)
                {
                    xser.WriteObject(Stream.Null, item);
                }
                serializeWatch.Stop();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                deserializeWatch = Stopwatch.StartNew();
                for (int i = 0; i < count; i++)
                {
                    ms.Position = 0;
                    xser.ReadObject(ms);
                }
                deserializeWatch.Stop();
                Console.WriteLine("||`DataContractSerializer`||{0:###,###,###}||{1:###,###,###}||{2:###,###,###}||",
                    ms.Length, serializeWatch.ElapsedMilliseconds, deserializeWatch.ElapsedMilliseconds);
            }
#if NET_3_5
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer xser = new DataContractJsonSerializer(typeof(T));
                xser.WriteObject(ms, item);
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                serializeWatch = Stopwatch.StartNew();
                for (int i = 0; i < count; i++)
                {
                    xser.WriteObject(Stream.Null, item);
                }
                serializeWatch.Stop();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                deserializeWatch = Stopwatch.StartNew();
                for (int i = 0; i < count; i++)
                {
                    ms.Position = 0;
                    xser.ReadObject(ms);
                }
                deserializeWatch.Stop();
                Console.WriteLine("||`DataContractJsonSerializer`||{0:###,###,###}||{1:###,###,###}||{2:###,###,###}||",
                    ms.Length, serializeWatch.ElapsedMilliseconds, deserializeWatch.ElapsedMilliseconds);

                string originalJson = Encoding.UTF8.GetString(ms.ToArray()), pbJson, psJson = null;

                using (MemoryStream ms2 = new MemoryStream())
                {
                    xser.WriteObject(ms2, pbClone);
                    pbJson = Encoding.UTF8.GetString(ms.ToArray());
                }
                if (testProtoSharp)
                {
                    using (MemoryStream ms3 = new MemoryStream())
                    {
                        xser.WriteObject(ms3, psClone);
                        psJson = Encoding.UTF8.GetString(ms.ToArray());
                    }
                }
                if (writeJson)
                {
                    Console.WriteLine("\tJSON: {0}", originalJson);
                }
                if (originalJson != pbJson)
                {
                    pass = false;
                    Console.WriteLine("\t**** json comparison fails (protobuf-net)!");
                    Console.WriteLine("\tClone JSON: {0}", pbJson);
                }
                if (testProtoSharp && (originalJson != psJson))
                {
                    pass = false;
                    Console.WriteLine("\t**** json comparison fails (proto#)!");
                    Console.WriteLine("\tClone JSON: {0}", psJson);
                }
            }
#endif          

            Console.WriteLine("\t[end {0}]", name);
            Console.WriteLine();
            return pass;
        }
        static void WriteBytes(string caption, byte[] data)
        {
            Console.Write("\t{0}\t", caption);
            foreach (byte b in data)
            {
                Console.Write(" " + b.ToString("X2"));
            }
            Console.WriteLine();
        }
    }
    // compare to the examples in the encoding spec
    // http://code.google.com/apis/protocolbuffers/docs/encoding.html

    /*
    message Test1 {
        required int32 a = 1;
    }
    message Test2 {
      required string b = 2;
    }
    message Test3 {
      required Test1 c = 3;
    }
    */
    [Serializable, ProtoContract]
    public sealed class Test1
    {
#if NET_3_0
        [DataMember(Name = "a", Order = 1, IsRequired = true)]
        [ProtoSharp.Core.Tag(1)]
#endif
        [ProtoMember(1, Name = "a", IsRequired = true, DataFormat = DataFormat.TwosComplement)]
        public int A { get; set; }
    }
    [Serializable, DataContract]
    public sealed class Test2
    {
        [DataMember(Name = "b", Order = 2, IsRequired = true)]
        [ProtoSharp.Core.Tag(2)]
        public string B { get; set; }
    }
    [Serializable, DataContract]
    public sealed class Test3
    {
        [DataMember(Name = "c", Order = 3, IsRequired = true)]
        [ProtoSharp.Core.Tag(3)]
        public Test1 C { get; set; }
    }

    [ServiceContract]
    public interface IFoo
    {
        [OperationContract]
#if NET_3_0
        [ProtoBehavior]
#endif
        Test3 Bar(Test1 value);
    }

}
