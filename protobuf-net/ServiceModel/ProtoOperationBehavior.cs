#if NET_3_0 && !SILVERLIGHT
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel.Description;
using ProtoBuf.Property;

namespace ProtoBuf.ServiceModel
{
    internal sealed class ProtoOperationBehavior : DataContractSerializerOperationBehavior
    {
        public ProtoOperationBehavior(OperationDescription operation) : base(operation) { }
        //public ProtoOperationBehavior(OperationDescription operation, DataContractFormatAttribute dataContractFormat) : base(operation, dataContractFormat) { }

        internal static bool CanSerialize(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (type.IsValueType) return false;

            // serialize as item?
            if (Serializer.IsEntityType(type)) return true;

            // serialize as list?
            bool enumOnly;
            Type itemType = PropertyFactory.GetListType(type, out enumOnly);
            if (itemType != null
                && (!enumOnly || Serializer.HasAddMethod(type, itemType))
                && Serializer.IsEntityType(itemType)) return true;
            return false;
        }

        public override XmlObjectSerializer CreateSerializer(Type type, System.Xml.XmlDictionaryString name, System.Xml.XmlDictionaryString ns, IList<Type> knownTypes)
        {
            if (CanSerialize(type))
            {
                return (XmlObjectSerializer)typeof(XmlProtoSerializer<>)
                    .MakeGenericType(type)
                    .GetConstructor(Type.EmptyTypes)
                    .Invoke(null);
            }
            return base.CreateSerializer(type, name, ns, knownTypes);
        }
    }
}
#endif