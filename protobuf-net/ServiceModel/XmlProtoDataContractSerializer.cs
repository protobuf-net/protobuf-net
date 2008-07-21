// REMOVED: this was for MTOM testing, but the simpler
//          XmlProtoSerializer seems to do the trick

//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Runtime.Serialization;

//namespace ProtoBuf
//{
//    sealed class XmlProtoDataContractSerializer<T> : XmlObjectSerializer
//        where T : class, new()
//    {
//        private readonly XmlObjectSerializer child;
//        public XmlProtoDataContractSerializer(System.Xml.XmlDictionaryString name, System.Xml.XmlDictionaryString ns, IList<Type> knownTypes)
//        {
//            child = new DataContractSerializer(typeof(byte[]), name, ns, knownTypes);
//        }
//        public override void WriteEndObject(System.Xml.XmlDictionaryWriter writer)
//        {
//            child.WriteEndObject(writer);
//        }
//        static readonly byte[] NON_EMPTY = new byte[1];
//        public override void WriteStartObject(System.Xml.XmlDictionaryWriter writer, object graph)
//        {
//            // pass down a sample byte[] in case of downstream casting etc
//            child.WriteStartObject(writer, graph == null ? null : NON_EMPTY);
//        }
//        public override void WriteObjectContent(System.Xml.XmlDictionaryWriter writer, object graph)
//        {
//            if (graph == null)
//            {
//                child.WriteObjectContent(writer, null);
//                return;
//            }
//            using (MemoryStream ms = new MemoryStream())
//            {
//                Serializer<T>.Serialize((T)graph, ms);
//                byte[] buffer = ms.ToArray();
//                child.WriteObjectContent(writer, buffer);
//            }
//        }
//        public override bool IsStartObject(System.Xml.XmlDictionaryReader reader)
//        {
//            return child.IsStartObject(reader);
//        }
//        public override object ReadObject(System.Xml.XmlDictionaryReader reader, bool verifyObjectName)
//        {
//            byte[] buffer = (byte[])child.ReadObject(reader, verifyObjectName);
//            if (buffer == null) return null;
//            using (MemoryStream ms = new MemoryStream(buffer))
//            {
//                return Serializer.Deserialize<T>(ms);
//            }
//        }
//    }
//}
