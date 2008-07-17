using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.IO;
using ProtoBuf;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Soap;
using System.Runtime.Serialization.Json;

namespace Examples.SimpleStream
{
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
    [Serializable, DataContract]
    public sealed class Test1
    {
        [DataMember(Name="a", Order=1, IsRequired=true)]
        [ProtoMember(1, Name = "a", IsRequired=true, DataFormat=DataFormat.TwosComplement)]
        public int A { get; set; }
    }
    [Serializable, DataContract]
    public sealed class Test2
    {
        [DataMember(Name = "b", Order = 2, IsRequired = true)]
        public string B { get; set; }
    }
    [Serializable, DataContract]
    public sealed class Test3
    {
        [DataMember(Name = "c", Order = 3, IsRequired = true)]
        public Test1 C { get; set; }
    }


    static class SimpleStreamDemo
    {
        public static void Run(int index)
        {
            Test1 t1 = new Test1 {A=150};
            TestItem(t1, 0x08, 0x96, 0x01);

            Test2 t2 = new Test2 { B = "testing" };
            TestItem(t2, 0x12, 0x07, 0x74, 0x65, 0x73, 0x74, 0x69, 0x6e, 0x67);

            Test3 t3 = new Test3 { C = t1 };
            TestItem(t3, 0x1a, 0x03, 0x08, 0x96, 0x01);
        }
        static void TestItem<T>(T item, params byte[] expected) where T : class, new()
        {
            string name = typeof(T).Name;
            Console.WriteLine("\t{0}", name);
            T clone;
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(item, ms);
                ms.Position = 0;
                clone = Serializer.Deserialize<T>(ms);
                byte[] data = ms.ToArray();

                if (data.Length != expected.Length)
                {
                    Console.WriteLine("\t*** serialization failure; expected {0}, got {1} (bytes)", expected.Length, data.Length);
                }
                else
                {
                    bool bad = false;
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i] != expected[i]) { bad = true; break; }
                    }
                    if (bad)
                    {
                        Console.WriteLine("\t*** serialization failure; byte stream mismatch");
                        WriteBytes("Expected", expected);
                    }
                    WriteBytes("Binary", data);
                }
                Console.WriteLine("\tProto: {0} bytes", ms.Length);
            }

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, item);
                Console.WriteLine("\tBinaryFormatter: {0} bytes", ms.Length);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                SoapFormatter sf = new SoapFormatter();
                sf.Serialize(ms, item);
                Console.WriteLine("\tSoapFormatter: {0} bytes", ms.Length);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                XmlSerializer xser = new XmlSerializer(typeof(T));
                xser.Serialize(ms, item);
                Console.WriteLine("\tXmlSerializer: {0} bytes", ms.Length);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractSerializer xser = new DataContractSerializer(typeof(T));
                xser.WriteObject(ms, item);
                Console.WriteLine("\tDataContractSerializer: {0} bytes", ms.Length);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer xser = new DataContractJsonSerializer(typeof(T));
                xser.WriteObject(ms, item);
                Console.WriteLine("\tDataContractJsonSerializer: {0} bytes", ms.Length);
                string originalJson = Encoding.UTF8.GetString(ms.ToArray()), cloneJson;

                using (MemoryStream ms2 = new MemoryStream())
                {
                    xser.WriteObject(ms2, clone);
                    cloneJson = Encoding.UTF8.GetString(ms.ToArray());
                }
                Console.WriteLine("\tJSON: {0}", originalJson);
                if (originalJson != cloneJson)
                {
                    Console.WriteLine("\t**** json comparison fails!");
                    Console.WriteLine("\tClone JSON: {0}", cloneJson);
                }
            }
            



            Console.WriteLine("\t[end {0}]", name);
            Console.WriteLine();
        }
        static void WriteBytes(string caption, byte[] data)
        {
            Console.Write("\t{0}\t",caption);
            foreach (byte b in data)
            {
                Console.Write(" " + b.ToString("X2"));
            }
            Console.WriteLine();
        }
    }

}
