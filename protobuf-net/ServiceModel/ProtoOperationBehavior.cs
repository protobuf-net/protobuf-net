#if NET_3_0
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel.Description;

namespace ProtoBuf.ServiceModel
{
    internal sealed class ProtoOperationBehavior : DataContractSerializerOperationBehavior
    {
        public ProtoOperationBehavior(OperationDescription operation) : base(operation) { }
        //public ProtoOperationBehavior(OperationDescription operation, DataContractFormatAttribute dataContractFormat) : base(operation, dataContractFormat) { }

        public override XmlObjectSerializer CreateSerializer(Type type, System.Xml.XmlDictionaryString name, System.Xml.XmlDictionaryString ns, IList<Type> knownTypes)
        {
            if (Serializer.IsEntityType(type))
            {
                return (XmlObjectSerializer)typeof(XmlProtoSerializer<>)
                    .MakeGenericType(type)
                    .GetConstructor(Type.EmptyTypes)
                    .Invoke(null);
            }
            else
            {
                return base.CreateSerializer(type, name, ns, knownTypes);
            }
        }
    }
}
#endif