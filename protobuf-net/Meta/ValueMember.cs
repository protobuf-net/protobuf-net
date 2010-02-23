using System;
using System.Reflection;
using ProtoBuf.Serializers;

namespace ProtoBuf.Meta
{
    public class ValueMember
    {
        private readonly int fieldNumber;
        public int FieldNumber { get { return fieldNumber; } }
        private MemberInfo member;
        private readonly Type expectedType;
        public ValueMember(Type expectedType, int fieldNumber, MemberInfo member)
        {
            if (fieldNumber < 1) throw new ArgumentOutOfRangeException("fieldNumber");
            if (member == null) throw new ArgumentNullException("member");
            if (expectedType == null) throw new ArgumentNullException("expectedType");
            this.fieldNumber = fieldNumber;
            this.member = member;
            this.expectedType = expectedType;
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
        private Type GetValueType()
        {
            switch (member.MemberType) {
                case MemberTypes.Field: return ((FieldInfo)member).FieldType;
                case MemberTypes.Property: return ((PropertyInfo)member).PropertyType;
                default: throw new NotSupportedException(member.MemberType.ToString());
            }
        }

        private DataFormat dataFormat;
        public DataFormat DataFormat
        {
            get { return dataFormat; }
            set { dataFormat = value; }
        }
        private IProtoSerializer BuildSerializer()
        {
            Type type = GetValueType();
            WireType wireType;
            IProtoSerializer ser = GetCoreSerializer(type, DataFormat, out wireType);
            ser = new TagDecorator(fieldNumber, wireType, ser);
            switch (member.MemberType)
            {
                case MemberTypes.Property:
                    ser = new PropertyDecorator((PropertyInfo)member, ser); break;
                case MemberTypes.Field:
                    ser = new FieldDecorator((FieldInfo)member, ser); break;
                default:
                    throw new InvalidOperationException();
            }

            return ser;
        }

        private static WireType GetIntWireType(DataFormat format, int width) {
            switch(format) {
                case DataFormat.ZigZag: return WireType.SignedVariant;
                case DataFormat.FixedSize: return width == 32 ? WireType.Fixed32 : WireType.Fixed64;
                case DataFormat.TwosComplement:
                case DataFormat.Default: return WireType.Variant;
                default: throw new InvalidOperationException();
            }
        }
        private IProtoSerializer GetCoreSerializer(Type type, DataFormat dataFormat, out WireType wireType)
        {
#if !FX11
            type = Nullable.GetUnderlyingType(type) ?? type;
#endif

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Int32:
                    wireType = GetIntWireType(dataFormat, 32);
                    return new Int32Serializer();
                case TypeCode.UInt32:
                    wireType = GetIntWireType(dataFormat, 32);
                    return new UInt32Serializer();
                case TypeCode.Int64:
                    wireType = GetIntWireType(dataFormat, 64);
                    return new Int64Serializer();
                case TypeCode.UInt64:
                    wireType = GetIntWireType(dataFormat, 64);
                    return new Int64Serializer();
                case TypeCode.String:
                    wireType = WireType.String;
                    return new StringSerializer();
                case TypeCode.Single:
                    wireType = WireType.Fixed32;
                    return new SingleSerializer();
                case TypeCode.Double:
                    wireType = WireType.Fixed64;
                    return new DoubleSerializer();
                case TypeCode.Boolean:
                    wireType = WireType.Variant;
                    return new BooleanSerializer();
            }
            throw new NotSupportedException("No serializer defined for type: " + type.FullName);
        }

    }
}
