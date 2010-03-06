#if !NO_RUNTIME
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
        private readonly Type parentType, itemType, defaultType, memberType;
        public Type ItemType { get { return itemType; } }
        public Type MemberType { get { return memberType; } }
        public Type DefaultType { get { return defaultType; } }
        public Type ParentType { get { return parentType; } }
        private readonly RuntimeTypeModel model;
        public ValueMember(RuntimeTypeModel model, Type parentType, int fieldNumber, MemberInfo member, Type memberType, Type itemType, Type defaultType)
            
        {
            if (fieldNumber < 1) throw new ArgumentOutOfRangeException("fieldNumber");
            if (member == null) throw new ArgumentNullException("member");
            if (parentType == null) throw new ArgumentNullException("parentType");
            if (memberType == null) throw new ArgumentNullException("memberType");
            if (model == null) throw new ArgumentNullException("model");
            this.fieldNumber = fieldNumber;
            this.memberType = memberType;
            this.member = member;
            this.itemType = itemType;
            this.defaultType = defaultType;
            this.parentType = parentType;
            this.model = model;
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

        private DataFormat dataFormat;
        public DataFormat DataFormat
        {
            get { return dataFormat; }
            set { dataFormat = value; }
        }
        private IProtoSerializer BuildSerializer()
        {
            WireType wireType;
            IProtoSerializer ser = GetCoreSerializer(itemType ?? memberType, DataFormat, out wireType);
            // apply tags
            ser = new TagDecorator(fieldNumber, wireType, ser);
            // apply lists if appropriate
            if (itemType != null) { ser = new ListDecorator(memberType, defaultType, ser); }
            
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

        private IProtoSerializer GetCoreSerializer(Type type, DataFormat dataFormat, out WireType defaultWireType)
        {
#if !NO_GENERICS
            type = Nullable.GetUnderlyingType(type) ?? type;
#endif
            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Int32:
                    defaultWireType = GetIntWireType(dataFormat, 32);
                    return new Int32Serializer();
                case TypeCode.UInt32:
                    defaultWireType = GetIntWireType(dataFormat, 32);
                    return new UInt32Serializer();
                case TypeCode.Int64:
                    defaultWireType = GetIntWireType(dataFormat, 64);
                    return new Int64Serializer();
                case TypeCode.UInt64:
                    defaultWireType = GetIntWireType(dataFormat, 64);
                    return new Int64Serializer();
                case TypeCode.String:
                    defaultWireType = WireType.String;
                    return new StringSerializer();
                case TypeCode.Single:
                    defaultWireType = WireType.Fixed32;
                    return new SingleSerializer();
                case TypeCode.Double:
                    defaultWireType = WireType.Fixed64;
                    return new DoubleSerializer();
                case TypeCode.Boolean:
                    defaultWireType = WireType.Variant;
                    return new BooleanSerializer();
                case TypeCode.DateTime:
                    defaultWireType = WireType.String;
                    return new DateTimeSerializer();
                case TypeCode.Decimal:
                    defaultWireType = WireType.String;
                    return new DecimalSerializer();
                case TypeCode.Byte:
                    defaultWireType = WireType.Variant;
                    throw new NotImplementedException();
                case TypeCode.SByte:
                    defaultWireType = WireType.Variant;
                    throw new NotImplementedException();
                case TypeCode.Char:
                    defaultWireType = WireType.Variant;
                    return new CharSerializer();
                case TypeCode.Int16:
                    defaultWireType = WireType.Variant;
                    return new Int16Serializer();
                case TypeCode.UInt16:
                    defaultWireType = WireType.Variant;
                    return new UInt16Serializer();
            }
            if (type == typeof(TimeSpan))
            {
                defaultWireType = WireType.String;
                return new TimeSpanSerializer();
            }
            if (type == typeof(Guid))
            {
                defaultWireType = WireType.String;
                return new GuidSerializer();
            }
            int key = model.FindOrAddAuto(type, false);
            if (key >= 0)
            {
                defaultWireType = WireType.String;
                return new SubItemSerializer(type, key);
            }
            throw new NotSupportedException("No serializer defined for type: " + type.FullName);
        }

    }
}
#endif