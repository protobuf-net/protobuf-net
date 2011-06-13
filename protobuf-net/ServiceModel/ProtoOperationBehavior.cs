#if FEAT_SERVICEMODEL && PLAT_XMLSERIALIZER && !NO_GENERICS
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel.Description;
using ProtoBuf.Meta;

namespace ProtoBuf.ServiceModel
{
    /// <summary>
    /// Describes a WCF operation behaviour that can perform protobuf serialization
    /// </summary>
    public sealed class ProtoOperationBehavior : DataContractSerializerOperationBehavior
    {
        private TypeModel model;
        /// <summary>
        /// The type-model that should be used with this behaviour
        /// </summary>
        public TypeModel Model
        {
            get { return model; }
            set {
                if (value == null) throw new ArgumentNullException("Model");
                model = value;
            }

        }
        /// <summary>
        /// Create a new ProtoOperationBehavior instance
        /// </summary>
        public ProtoOperationBehavior(OperationDescription operation) : base(operation) { model = RuntimeTypeModel.Default; }
        //public ProtoOperationBehavior(OperationDescription operation, DataContractFormatAttribute dataContractFormat) : base(operation, dataContractFormat) { }

        /// <summary>
        /// Creates a protobuf serializer if possible (falling back to the default WCF serializer)
        /// </summary>
        public override XmlObjectSerializer CreateSerializer(Type type, System.Xml.XmlDictionaryString name, System.Xml.XmlDictionaryString ns, IList<Type> knownTypes)
        {
            int key = model.GetKey(ref type);
            if (key >= 0)
            {
                return new XmlProtoSerializer(model, key);
            }
            return base.CreateSerializer(type, name, ns, knownTypes);
        }
    }
}
#endif