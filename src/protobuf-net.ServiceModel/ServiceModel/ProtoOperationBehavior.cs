using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel.Description;
using System.Xml;
using ProtoBuf.Meta;

namespace ProtoBuf.ServiceModel
{
    /// <summary>
    /// Describes a WCF operation behaviour that can perform protobuf serialization
    /// </summary>
    public sealed class ProtoOperationBehavior : DataContractSerializerOperationBehavior
    {
        private TypeModel _model;

        /// <summary>
        /// Create a new ProtoOperationBehavior instance
        /// </summary>
        public ProtoOperationBehavior(OperationDescription operation) : base(operation)
        {
            _model = RuntimeTypeModel.Default;
        }

        /// <summary>
        /// The type-model that should be used with this behaviour
        /// </summary>
        public TypeModel Model
        {
            get { return _model; }
            set
            {
                _model = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        //public ProtoOperationBehavior(OperationDescription operation, DataContractFormatAttribute dataContractFormat) : base(operation, dataContractFormat) { }

        /// <summary>
        /// Creates a protobuf serializer if possible (falling back to the default WCF serializer)
        /// </summary>
        public override XmlObjectSerializer CreateSerializer(Type type, XmlDictionaryString name, XmlDictionaryString ns, IList<Type> knownTypes)
        {
            if (_model is null) throw new InvalidOperationException("No Model instance has been assigned to the ProtoOperationBehavior");
            return XmlProtoSerializer.TryCreate(_model, type) ?? base.CreateSerializer(type, name, ns, knownTypes);
        }
    }
}