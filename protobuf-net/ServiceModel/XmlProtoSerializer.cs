#if NET_3_0
using System.IO;
using System.Runtime.Serialization;

namespace ProtoBuf.ServiceModel
{
    sealed class XmlProtoSerializer<T> : XmlObjectSerializer
    {
        public override void WriteEndObject(System.Xml.XmlDictionaryWriter writer)
        {
            writer.WriteEndElement();
        }
        public override void WriteStartObject(System.Xml.XmlDictionaryWriter writer, object graph)
        {
            writer.WriteStartElement(PROTO_ELEMENT);
        }
        const string PROTO_ELEMENT = "proto";
        public override void WriteObjectContent(System.Xml.XmlDictionaryWriter writer, object graph)
        {
            if (graph == null)
            {
                writer.WriteAttributeString("nil", "true");
            }
            else
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<T>(ms, (T)graph);
                    byte[] buffer = ms.GetBuffer();
                    writer.WriteBase64(buffer, 0, (int)ms.Length);
                }
            }
        }
        public override bool IsStartObject(System.Xml.XmlDictionaryReader reader)
        {
            return reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == PROTO_ELEMENT;
        }
        public override object ReadObject(System.Xml.XmlDictionaryReader reader, bool verifyObjectName)
        {
            if (reader.GetAttribute("nil") == "true") return null;
            reader.ReadStartElement(PROTO_ELEMENT);
            try
            {
                using (MemoryStream ms = new MemoryStream(reader.ReadContentAsBase64()))
                {
                    T val = Serializer.Deserialize<T>(ms);
                    return val;
                }
            }
            finally
            {
                reader.ReadEndElement();
            }
        }

    }
}
#endif