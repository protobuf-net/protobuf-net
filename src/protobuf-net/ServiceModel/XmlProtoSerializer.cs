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
        private readonly bool isList, autoCreate;
        private readonly Type type;

        internal XmlProtoSerializer(TypeModel model, Type type, bool isList)
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.isList = isList;
            this.type = type ?? throw new ArgumentOutOfRangeException(nameof(type));
            this.autoCreate = TypeModel.PrepareDeserialize(null, ref type);
        }
        /// <summary>
        /// Attempt to create a new serializer for the given model and type
        /// </summary>
        /// <returns>A new serializer instance if the type is recognised by the model; null otherwise</returns>
        public static XmlProtoSerializer TryCreate(TypeModel model, Type type)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (IsKnownType(model, ref type, out bool isList))
            {
                return new XmlProtoSerializer(model, type, isList);
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

            bool known = IsKnownType(model, ref type, out isList);
            if (!known) throw new ArgumentOutOfRangeException(nameof(type), "Type not recognised by the model: " + type.FullName);
            this.model = model;
            this.autoCreate = TypeModel.PrepareDeserialize(null, ref type);
            this.type = type;
        }

        private static bool IsKnownType(TypeModel model, ref Type type, out bool isList)
        {
            if (model != null && type != null)
            {
                if (model.IsKnownType(ref type))
                {
                    isList = false;
                    return true;
                }
                Type itemType = TypeModel.GetListItemType(type);
                if (itemType != null)
                {
                    if (model.IsKnownType(ref itemType))
                    {
                        isList = true;
                        return true;
                    }
                }
            }

            isList = false;
            return false;
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
                        model.SerializeRootFallback(ref state, graph);
                    }
                    else
                    {

                        try
                        {
                            model.Serialize(ref state, type, graph);
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
                    if (isList)
                    {
                        return state.DeserializeRootFallback(null, type);
                    }
                    else
                    {

                        return model.Deserialize(ref state, type, null);
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
                if (DynamicStub.TryDeserializeRoot(type, model, ref state, ref result, autoCreate))
                { } // winning!
                else if (isList)
                {
                    result = state.DeserializeRootFallback(null, type);
                }
                else
                {
                    result = model.Deserialize(ref state, type, null);
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