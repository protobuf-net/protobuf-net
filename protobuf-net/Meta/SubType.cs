using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf.Serializers;

namespace ProtoBuf.Meta
{
    public class SubType
    {
        private readonly int fieldNumber;
        public int FieldNumber { get { return fieldNumber; } }
        public MetaType DerivedType { get { return derivedType; } }
        private readonly MetaType derivedType;
        public SubType(int fieldNumber, MetaType derivedType)
        {
            if (derivedType == null) throw new ArgumentNullException("derivedType");
            if (fieldNumber <= 0) throw new ArgumentOutOfRangeException("fieldNumber");
            this.fieldNumber = fieldNumber;
            this.derivedType = derivedType;
        }

        private IProtoSerializer serializer;
        internal IProtoSerializer Serializer
        {
            get
            {
                if (serializer == null) serializer = BuildSerializer();
                return serializer;
            }
        }

        private IProtoSerializer BuildSerializer()
        {
            IProtoSerializer ser = new SubItemSerializer(derivedType.Type, derivedType.GetKey(true, false));
            return new TagDecorator(fieldNumber, WireType.String, false, ser);
        }
    }
}
