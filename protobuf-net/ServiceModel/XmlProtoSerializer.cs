#if (FEAT_SERVICEMODEL && PLAT_XMLSERIALIZER) || (SILVERLIGHT && !PHONE7)
using System.IO;
using System.Runtime.Serialization;
using ProtoBuf.Meta;
using System;

namespace ProtoBuf.ServiceModel
{
    /// <summary>
    /// An xml object serializer that can embed protobuf data in a base-64 hunk (looking like a byte[])
    /// </summary>
    public sealed class XmlProtoSerializer : XmlObjectSerializer
    {
        private readonly TypeModel model;
        private readonly int key;
        internal XmlProtoSerializer(TypeModel model, int key)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (key < 0) throw new ArgumentOutOfRangeException("key");
            this.model = model;
            this.key = key;
        }
        /// <summary>
        /// Attempt to create a new serializer for the given model and type
        /// </summary>
        /// <returns>A new serializer instance if the type is recognised by the model; null otherwise</returns>
        public static XmlProtoSerializer TryCreate(TypeModel model, Type type)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (type == null) throw new ArgumentNullException("type");
            int key = GetKey(model, type);
            if (key < 0) return null;
            return new XmlProtoSerializer(model, key);
        }
        /// <summary>
        /// Creates a new serializer for the given model and type
        /// </summary>
        public XmlProtoSerializer(TypeModel model, Type type) : this(model, GetKey(model, type))
        {
        }
        static int GetKey(TypeModel model, Type type)
        {
            return model == null ? -1 : model.GetKey(ref type);
        }
        /// <summary>
        /// Ends an object in the output
        /// </summary>
        public override void WriteEndObject(System.Xml.XmlDictionaryWriter writer)
        {
            writer.WriteEndElement();
        }
        /// <summary>
        /// Begins an object in the output
        /// </summary>
        public override void WriteStartObject(System.Xml.XmlDictionaryWriter writer, object graph)
        {
            writer.WriteStartElement(PROTO_ELEMENT);
        }
        const string PROTO_ELEMENT = "proto";
        /// <summary>
        /// Writes the body of an object in the output
        /// </summary>
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
                    using (ProtoWriter protoWriter = new ProtoWriter(ms, model, null))
                    {
                        model.Serialize(key, graph, protoWriter);
                    }
                    byte[] buffer = ms.GetBuffer();
                    writer.WriteBase64(buffer, 0, (int)ms.Length);
                }
            }
        }
        /// <summary>
        /// Indicates whether this is the start of an object we are prepared to handle
        /// </summary>
        public override bool IsStartObject(System.Xml.XmlDictionaryReader reader)
        {
            return reader.NodeType == System.Xml.XmlNodeType.Element && reader.Name == PROTO_ELEMENT;
        }
        /// <summary>
        /// Reads the body of an object
        /// </summary>
        public override object ReadObject(System.Xml.XmlDictionaryReader reader, bool verifyObjectName)
        {
            if (reader.GetAttribute("nil") == "true") return null;
            reader.ReadStartElement(PROTO_ELEMENT);
            try
            {
                using (MemoryStream ms = new MemoryStream(reader.ReadContentAsBase64()))
                {
                    using (ProtoReader protoReader = new ProtoReader(ms, model, null))
                    { 
                        return model.Deserialize(key, null, protoReader);
                    }
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