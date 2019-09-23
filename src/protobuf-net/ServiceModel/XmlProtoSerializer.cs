using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using ProtoBuf.Internal;
using ProtoBuf.Meta;

namespace ProtoBuf.ServiceModel
{
    /// <summary>
    /// An xml object serializer that can embed protobuf data in a base-64 hunk (looking like a byte[])
    /// </summary>
    public sealed class XmlProtoSerializer : XmlObjectSerializer
    {
        private readonly TypeModel model;
        private readonly int key;
        private readonly bool isList, isEnum;
        private readonly Type type;
        internal XmlProtoSerializer(TypeModel model, int key, Type type, bool isList)
        {
            if (key < 0) throw new ArgumentOutOfRangeException(nameof(key));
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.key = key;
            this.isList = isList;
            this.type = type ?? throw new ArgumentOutOfRangeException(nameof(type));
            this.isEnum = type.IsEnum;
        }
        /// <summary>
        /// Attempt to create a new serializer for the given model and type
        /// </summary>
        /// <returns>A new serializer instance if the type is recognised by the model; null otherwise</returns>
        public static XmlProtoSerializer TryCreate(TypeModel model, Type type)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (type == null) throw new ArgumentNullException(nameof(type));

            int key = GetKey(model, ref type, out bool isList);
            if (key >= 0)
            {
                return new XmlProtoSerializer(model, key, type, isList);
            }
            return null;
        }

        /// <summary>
        /// Creates a new serializer for the given model and type
        /// </summary>
        public XmlProtoSerializer(TypeModel model, Type type)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (type == null) throw new ArgumentNullException(nameof(type));

            key = GetKey(model, ref type, out isList);
            this.model = model;
            this.type = type;
            this.isEnum = type.IsEnum;
            if (key < 0) throw new ArgumentOutOfRangeException(nameof(type), "Type not recognised by the model: " + type.FullName);
        }

        private static int GetKey(TypeModel model, ref Type type, out bool isList)
        {
            if (model != null && type != null)
            {
                int key = model.GetKey(ref type);
                if (key >= 0)
                {
                    isList = false;
                    return key;
                }
                Type itemType = TypeModel.GetListItemType(type);
                if (itemType != null)
                {
                    key = model.GetKey(ref itemType);
                    if (key >= 0)
                    {
                        isList = true;
                        return key;
                    }
                }
            }

            isList = false;
            return -1;
        }

        /// <summary>
        /// Ends an object in the output
        /// </summary>
        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            writer.WriteEndElement();
        }

        /// <summary>
        /// Begins an object in the output
        /// </summary>
        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            writer.WriteStartElement(PROTO_ELEMENT);
        }

        private const string PROTO_ELEMENT = "proto";

        /// <summary>
        /// Writes the body of an object in the output
        /// </summary>
        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (graph == null)
            {
                writer.WriteAttributeString("nil", "true");
            }
            else
            {
                using MemoryStream ms = new MemoryStream();
                var state = ProtoWriter.State.Create(ms, model, null);
                try
                {
                    if (isList)
                    {
                        model.SerializeFallback(ref state, graph);
                    }
                    else
                    {

                        try
                        {
                            model.Serialize(ref state, key, graph);
                            state.Close();
                        }
                        catch
                        {
                            state.Abandon();
                            throw;
                        }
                    }
                }
                finally
                {
                    state.Dispose();
                }
                Helpers.GetBuffer(ms, out var segment);
                writer.WriteBase64(segment.Array, segment.Offset, segment.Count);
                
            }
        }

        /// <summary>
        /// Indicates whether this is the start of an object we are prepared to handle
        /// </summary>
        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            reader.MoveToContent();
            return reader.NodeType == XmlNodeType.Element && reader.Name == PROTO_ELEMENT;
        }

        /// <summary>
        /// Reads the body of an object
        /// </summary>
        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            reader.MoveToContent();
            bool isSelfClosed = reader.IsEmptyElement, isNil = reader.GetAttribute("nil") == "true";
            reader.ReadStartElement(PROTO_ELEMENT);

            // explicitly null
            if (isNil)
            {
                if (!isSelfClosed) reader.ReadEndElement();
                return null;
            }
            ProtoReader.State state;
            if (isSelfClosed) // no real content
            {
                state = ProtoReader.State.Create(Stream.Null, model, null, ProtoReader.TO_EOF);
                try
                {
                    if (isList || isEnum)
                    {
                        return state.DeserializeFallback(null, type);
                    }
                    else
                    {

                        return model.DeserializeCore(ref state, key, null);
                    }
                }
                finally
                {
                    state.Dispose();
                }
            }

            object result = null;
            Debug.Assert(reader.CanReadBinaryContent, "CanReadBinaryContent");
            ReadOnlyMemory<byte> payload = reader.ReadContentAsBase64();
            state = ProtoReader.State.Create(payload, model, null);
            try
            {
                {
                    if (DynamicStub.TryDeserialize(type, model, ref state, ref result))
                    { } // winning!
                    else if (isList || isEnum)
                    {
                        result = state.DeserializeFallback(null, type);
                    }
                    else
                    {
                        result = model.DeserializeCore(ref state, key, null);
                    }
                }
            }
            finally
            {
                state.Dispose();
            }
            reader.ReadEndElement();
            return result;
        }
    }
}