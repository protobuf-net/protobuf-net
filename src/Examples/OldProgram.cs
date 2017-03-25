//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Runtime.Serialization;
//using System.ServiceModel;


//namespace TestRig
//{
//    using ProtoBuf;
//    using ProtoBuf.ServiceModel;

//    [DataContract]
//    public class Test2
//    {
//        [DataMember(Name = "b", IsRequired = true, Order = 2)]
//        public string B { get; set; }
//    }
//    [DataContract] // note optional Name etc
//    public class Test1 { // keep original names in meta
//        [DataMember(IsRequired = true, Order = 1)]
//        public uint Id { get; set; }
//        /*
//        [DataMember(IsRequired = true, Order = 2)]
//        public int SecondId { get; set; }

//        [DataMember(IsRequired = true, Order = 3)]
//        public string Name { get; set; }

//        [DataMember(IsRequired = true, Order = 4)]
//        public bool Enabled { get; set; }

//        [DataMember(IsRequired = true, Order = 5)]
//        public bool Active { get; set; }

//        [DataMember(IsRequired = true, Order = 6)]
//        public float Value { get; set; }
//        */

//        [DataMember(Order = 7)]
//        public List<int> SubData { get; private set; }

//        public Test1()
//        {
//            SubData = new List<int>();
//        }
//    }
//    [DataContract]
//    public class Test3 {
//        [DataMember(Name="c", IsRequired = true, Order = 3)]
//        public Test1 C { get; set; }
//    }
//    [ServiceContract(Name="ProtoService")]
//    public interface IProtoService {
//        [OperationContract, ProtoBehavior]
//        Test3 Get(Test1 request);
//    }

//    static class Program
//    {
//        static T WriteBytes<T>(T instance) where T : class, new()
//        {
//            using (MemoryStream data = new MemoryStream())
//            {
//                Serializer.Serialize(instance, data);
//                Console.WriteLine(typeof(T).Name);
//                byte[] buffer = data.GetBuffer();
//                int len = (int)data.Length;
//                for (int i = 0; i < len; i++)
//                {
//                    Console.Write(buffer[i].ToString("X2"));
//                    Console.Write(' ');
//                }
//                Console.WriteLine();

//                // check we can read it back; heck, use xml serializer for convenience
//                data.Position = 0;
//                T newObj = Serializer.Deserialize<T>(data);
//                /* this works but is really, really slow... goes to show ;-p
//                XmlSerializer xs = new XmlSerializer(typeof(T));
//                using (StringWriter sw = new StringWriter())
//                {
//                    xs.Serialize(sw, newObj);
//                    Console.WriteLine(sw);
//                }*/
//                return newObj;
//            }            
//        }
//        static void Main()
//        {
            
//            string s = Serializer.GetProto<Test1>();
            
//            Test1 test1 = new Test1 { Id = UInt16.MaxValue };
//            WriteBytes(test1);

//            for (int i = 0; i < 50; i++)
//            {
//                test1.SubData.Add(i);
//            }

//            Test1 clone = WriteBytes(test1);
//            Console.WriteLine(clone.Id);
//            Console.WriteLine(clone.SubData.Count);
//            foreach (int i in clone.SubData)
//            {
//                Console.WriteLine(i);
//            }

//            Console.ReadLine();
           
//        }

//    }
//}
