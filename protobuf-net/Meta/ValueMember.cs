#if !NO_RUNTIME
using System;
using System.Reflection;
using ProtoBuf.Serializers;
using System.Globalization;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Represents a member (property/field) that is mapped to a protobuf field
    /// </summary>
    public class ValueMember
    {
        private readonly int fieldNumber;
        /// <summary>
        /// The number that identifies this member in a protobuf stream
        /// </summary>
        public int FieldNumber { get { return fieldNumber; } }
        private MemberInfo member;
        private readonly Type parentType, itemType, defaultType, memberType;
        private readonly object defaultValue;
        /// <summary>
        /// Within a list / array / etc, the type of object for each item in the list (especially useful with ArrayList)
        /// </summary>
        public Type ItemType { get { return itemType; } }
        /// <summary>
        /// The underlying type of the member
        /// </summary>
        public Type MemberType { get { return memberType; } }
        /// <summary>
        /// For abstract types (IList etc), the type of concrete object to create (if required)
        /// </summary>
        public Type DefaultType { get { return defaultType; } }
        /// <summary>
        /// The type the defines the member
        /// </summary>
        public Type ParentType { get { return parentType; } }

        /// <summary>
        /// The default value of the item (members with this value will not be serialized)
        /// </summary>
        public object DefaultValue { get { return defaultValue; } }

        private readonly RuntimeTypeModel model;
        /// <summary>
        /// Creates a new ValueMember instance
        /// </summary>
        public ValueMember(RuntimeTypeModel model, Type parentType, int fieldNumber, MemberInfo member, Type memberType, Type itemType, Type defaultType, DataFormat dataFormat, object defaultValue) 
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
            this.dataFormat = dataFormat;
            if (defaultValue != null && !memberType.IsInstanceOfType(defaultValue))
            {
                defaultValue = ParseDefaultValue(memberType, defaultValue);
            }
            this.defaultValue = defaultValue;
        }

        private static object ParseDefaultValue(Type type, object value)
        {
            if (value is string)
            {
                string s = (string)value;
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean: return bool.Parse(s);
                    case TypeCode.Byte: return byte.Parse(s, CultureInfo.InvariantCulture);
                    case TypeCode.Char: return char.Parse(s);
                    case TypeCode.DateTime: return DateTime.Parse(s, CultureInfo.InvariantCulture);
                    case TypeCode.Decimal: return decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.Double: return double.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.Int16: return short.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.Int32: return int.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.Int64: return long.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.SByte: return sbyte.Parse(s, CultureInfo.InvariantCulture);
                    case TypeCode.Single: return float.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.String: return s;
                    case TypeCode.UInt16: return ushort.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.UInt32: return uint.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.UInt64: return ulong.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                }
                if (type == typeof(TimeSpan)) return TimeSpan.Parse(s);
                if (type.IsEnum) return Enum.Parse(type, s, true);
            }
            return Convert.ChangeType(value, type);
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

        private readonly DataFormat dataFormat;
        /// <summary>
        /// Specifies the rules used to process the field; this is used to determine the most appropriate
        /// wite-type, but also to describe subtypes <i>within</i> that wire-type (such as SignedVariant)
        /// </summary>
        public DataFormat DataFormat { get { return dataFormat; } }
        private bool isStrict;
        /// <summary>
        /// Indicates whether this field should follow strict encoding rules; this means (for example) that if a "fixed32"
        /// is encountered when "variant" is defined, then it will fail (throw an exception) when parsing. Note that
        /// when serializing the defined type is always used.
        /// </summary>
        public bool IsStrict
        {
            get { return isStrict; }
            set { isStrict = value; }
        }
        private IProtoSerializer BuildSerializer()
        {
            WireType wireType;
            IProtoSerializer ser = GetCoreSerializer(itemType ?? memberType, out wireType);
            // apply tags
            ser = new TagDecorator(fieldNumber, wireType, isStrict, ser);
            // apply lists if appropriate
            if(itemType != null)
            {
                Helpers.DebugAssert(itemType == ser.ExpectedType, "Wrong type in the tail");
                if (memberType.IsArray)
                {
                    ser = new ArrayDecorator(ser);
                }
                else
                {
                    ser = new ListDecorator(memberType, defaultType, ser);
                }
            }
            else if (defaultValue != null)
            {
                ser = new DefaultValueDecorator(defaultValue, ser);
            }

            
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

        private IProtoSerializer GetCoreSerializer(Type type, out WireType defaultWireType)
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
            if (type == typeof(byte[]))
            {
                defaultWireType = WireType.String;
                return new BlobSerializer();
            }
            int key = model.GetKey(type, false, true);
            if (key >= 0)
            {
                defaultWireType = WireType.String;
                return new SubItemSerializer(type, key, model[type]);
            }
            throw new NotSupportedException("No serializer defined for type: " + type.FullName);
        }

    }
}
#endif