using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using ProtoBuf.Internal;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;

namespace ProtoBuf.ServiceModel
{
    /// <summary>
    /// An xml object serializer that can embed protobuf data in a base-64 hunk (looking like a byte[])
    /// </summary>
    public sealed class XmlProtoSerializer : XmlObjectSerializer
    {
        private readonly TypeModel model;
        private readonly bool autoCreate;
        private readonly Type type;

#pragma warning disable IDE0060 // Remove unused parameter "isList"
        internal XmlProtoSerializer(TypeModel model, Type type, bool isList)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.type = type ?? throw new ArgumentOutOfRangeException(nameof(type));
            this.autoCreate = TypeModel.PrepareDeserialize(null, ref type);
        }
        /// <summary>
        /// Attempt to create a new serializer for the given model and type
        /// </summary>
        /// <returns>A new serializer instance if the type is recognised by the model; null otherwise</returns>
        public static XmlProtoSerializer TryCreate(TypeModel model, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type)
        {
            if (model is null) throw new ArgumentNullException(nameof(model));
            if (type is null) throw new ArgumentNullException(nameof(type));

            if (IsKnownType(model, type, out bool isList))
            {
                return new XmlProtoSerializer(model, type, isList);
            }
            return null;
        }

        /// <summary>
        /// Creates a new serializer for the given model and type
        /// </summary>
        public XmlProtoSerializer(TypeModel model, [DynamicallyAccessedMembers(DynamicAccess.ContractType)] Type type)
        {
            if (model is null) throw new ArgumentNullException(nameof(model));
            if (type is null) throw new ArgumentNullException(nameof(type));

            bool known = IsKnownType(model, type, out _);
            if (!known) throw new ArgumentOutOfRangeException(nameof(type), "Type not recognised by the model: " + type.FullName);
            this.model = model;
            this.autoCreate = TypeModel.PrepareDeserialize(null, ref type);
            this.type = type;
        }

        private static bool IsKnownType(TypeModel model, Type type, out bool isList)
        {
            if (model is object && type is object)
            {
                if (model.CanSerialize(type, true, true, true, out var category))
                {
                    isList = category.IsRepeated();
                    return true;
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
            if (writer is null) throw new ArgumentNullException(nameof(writer));
            writer.WriteEndElement();
        }

        /// <summary>
        /// Begins an object in the output
        /// </summary>
        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            if (writer is null) throw new ArgumentNullException(nameof(writer));
            writer.WriteStartElement(PROTO_ELEMENT);
        }

        private const string PROTO_ELEMENT = "proto";

        /// <summary>
        /// Writes the body of an object in the output
        /// </summary>
        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            if (writer is null) throw new ArgumentNullException(nameof(writer));
            if (graph is null)
            {
                writer.WriteAttributeString("nil", "true");
            }
            else
            {
                using MemoryStream ms = new MemoryStream();
                var state = ProtoWriter.State.Create(ms, model, null);
                try
                {

                    if (!DynamicStub.TrySerializeRoot(type, model, ref state, graph))
                        TypeModel.ThrowUnexpectedType(type, model);
                }
                catch
                {
                    state.Abandon();
                    throw;
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
            if (reader is null) throw new ArgumentNullException(nameof(reader));
            reader.MoveToContent();
            return reader.NodeType == XmlNodeType.Element && reader.Name == PROTO_ELEMENT;
        }

        /// <summary>
        /// Reads the body of an object
        /// </summary>
        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            if (reader is null) throw new ArgumentNullException(nameof(reader));
            reader.MoveToContent();
            bool isSelfClosed = reader.IsEmptyElement, isNil = reader.GetAttribute("nil") == "true";
            reader.ReadStartElement(PROTO_ELEMENT);

            // explicitly null
            if (isNil)
            {
                if (!isSelfClosed) reader.ReadEndElement();
                return null;
            }
            object ReadFrom(ReadOnlyMemory<byte> payload)
            {
                var state = ProtoReader.State.Create(payload, model, null);
                try
                {
                    object result = null;
                    if (!DynamicStub.TryDeserializeRoot(type, model, ref state, ref result, autoCreate))
                        TypeModel.ThrowUnexpectedType(type, model);
                    return result;
                }
                finally
                {
                    state.Dispose();
                }
            }

            if (isSelfClosed) // no real content
            {
                return ReadFrom(Array.Empty<byte>());
            }


            Debug.Assert(reader.CanReadBinaryContent, "CanReadBinaryContent");
            ReadOnlyMemory<byte> payload = reader.ReadContentAsBase64();
            reader.ReadEndElement();
            return ReadFrom(payload);
        }
    }
}