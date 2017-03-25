//using System;
//using System.IO;
//using System.Runtime.Serialization;
//using System.Xml;
//using System.Xml.Serialization;
//using ProtoBuf;

//namespace HttpClient {
//    [XmlType]
//    class Foo : IXmlSerializable {
//        public System.Xml.Schema.XmlSchema GetSchema() { return null; }
//        public void ReadXml(System.Xml.XmlReader reader) { Serializer.Merge(reader, this); }
//        public void WriteXml(System.Xml.XmlWriter writer) { Serializer.Serialize(writer, this); }

//        [XmlElement(Order=1)] public int Id {get;set;}
//        [XmlElement(Order = 2)] public float Value { get; set; }
//        [XmlElement(Order=3)] public string Name {get;set;}
//    }

//    static class Program {
//        static void Main() {
//            var dcs = new DataContractSerializer(typeof(Foo));
//            var obj = new Foo { Id = 123, Value = 123.45F, Name = "abc"};
//            // write to console
//            using (var writer = XmlWriter.Create(Console.Out)) { dcs.WriteObject(writer, obj); }
//            // round-trip in memory
//            Foo clone;
//            using (var ms = new MemoryStream()) {
//                dcs.WriteObject(ms, obj);
//                ms.Position = 0;
//                clone = (Foo)dcs.ReadObject(ms);
//            }
//            Console.WriteLine(clone.Id);
//            Console.WriteLine(clone.Value);
//            Console.WriteLine(clone.Name);
//        }
//    }
//}
