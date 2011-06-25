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
        private readonly MemberInfo member;
        /// <summary>
        /// Gets the member (field/property) which this member relates to.
        /// </summary>
        public MemberInfo Member { get { return member; } }
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
            : this(model, fieldNumber,memberType, itemType, defaultType, dataFormat)
        {
            if (member == null) throw new ArgumentNullException("member");
            if (parentType == null) throw new ArgumentNullException("parentType");
            if (fieldNumber < 1 && !parentType.IsEnum) throw new ArgumentOutOfRangeException("fieldNumber");

            this.member = member;
            this.parentType = parentType;
                        if (fieldNumber < 1 && !parentType.IsEnum) throw new ArgumentOutOfRangeException("fieldNumber");
            if (defaultValue != null && !memberType.IsInstanceOfType(defaultValue))
            {
                defaultValue = ParseDefaultValue(memberType, defaultValue);
            }
            this.defaultValue = defaultValue;
        }
        /// <summary>
        /// Creates a new ValueMember instance
        /// </summary>
        internal ValueMember(RuntimeTypeModel model, int fieldNumber, Type memberType, Type itemType, Type defaultType, DataFormat dataFormat) 
        {

            if (memberType == null) throw new ArgumentNullException("memberType");
            if (model == null) throw new ArgumentNullException("model");
            this.fieldNumber = fieldNumber;
            this.memberType = memberType;
            this.itemType = itemType;
            this.defaultType = defaultType;

            this.model = model;
            this.dataFormat = dataFormat;
        }
        internal Enum GetEnumValue()
        {
            return (Enum)((FieldInfo)member).GetValue(null);
        }
        private static object ParseDefaultValue(Type type, object value)
        {
            if (value is string)
            {
                string s = (string)value;
                if (type.IsEnum) return Enum.Parse(type, s, true);
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean: return bool.Parse(s);
                    case TypeCode.Byte: return byte.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    case TypeCode.Char: // char.Parse missing on CF/phone7
                        if (s.Length == 1) return s[0];
                        throw new FormatException("Single character expected: \"" + s + "\"");
                    case TypeCode.DateTime: return DateTime.Parse(s, CultureInfo.InvariantCulture);
                    case TypeCode.Decimal: return decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.Double: return double.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.Int16: return short.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.Int32: return int.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.Int64: return long.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.SByte: return sbyte.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    case TypeCode.Single: return float.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.String: return s;
                    case TypeCode.UInt16: return ushort.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.UInt32: return uint.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case TypeCode.UInt64: return ulong.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                }
                if (type == typeof(TimeSpan)) return TimeSpan.Parse(s);
                if (type == typeof(Uri)) return s; // Uri is decorated as string
                if (type == typeof(Guid)) return new Guid(s);
            }
            if (type.IsEnum) return Enum.ToObject(type, value);
            return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
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

        /// <summary>
        /// Indicates whether this field should follow strict encoding rules; this means (for example) that if a "fixed32"
        /// is encountered when "variant" is defined, then it will fail (throw an exception) when parsing. Note that
        /// when serializing the defined type is always used.
        /// </summary>
        public bool IsStrict
        {
            get { return HasFlag(OPTIONS_IsStrict); }
            set { SetFlag(OPTIONS_IsStrict, value, true); }
        }

        /// <summary>
        /// Indicates whether this field should use packed encoding (which can save lots of space for repeated primitive values).
        /// This option only applies to list/array data of primitive types (int, double, etc).
        /// </summary>
        public bool IsPacked
        {
            get { return HasFlag(OPTIONS_IsPacked); }
            set { SetFlag(OPTIONS_IsPacked, value, true); }
        }

        /// <summary>
        /// Indicates whether this field should *repace* existing values (the default is false, meaning *append*).
        /// This option only applies to list/array data.
        /// </summary>
        public bool OverwriteList
        {
            get { return HasFlag(OPTIONS_OverwriteList); }
            set { SetFlag(OPTIONS_OverwriteList, value, true); }
        }

        /// <summary>
        /// Indicates whether this field is mandatory.
        /// </summary>
        public bool IsRequired
        {
            get { return HasFlag(OPTIONS_IsRequired); }
            set { SetFlag(OPTIONS_IsRequired, value, true); }
        }

        private bool asReference;
        /// <summary>
        /// Enables full object-tracking/full-graph support.
        /// </summary>
        public bool AsReference
        {
            get { return asReference; }
            set { ThrowIfFrozen(); asReference = value; }
        }

        private bool dynamicType;
        /// <summary>
        /// Embeds the type information into the stream, allowing usage with types not known in advance.
        /// </summary>
        public bool DynamicType
        {
            get { return dynamicType; }
            set { ThrowIfFrozen(); dynamicType = value; }
        }

        private MethodInfo getSpecified, setSpecified;
        /// <summary>
        /// Specifies methods for working with optional data members.
        /// </summary>
        /// <param name="getSpecified">Provides a method (null for none) to query whether this member should
        /// be serialized; it must be of the form "bool {Method}()". The member is only serialized if the
        /// method returns true.</param>
        /// <param name="setSpecified">Provides a method (null for none) to indicate that a member was
        /// deserialized; it must be of the form "void {Method}(bool)", and will be called with "true"
        /// when data is found.</param>
        public void SetSpecified(MethodInfo getSpecified, MethodInfo setSpecified)
        {
            if (getSpecified != null)
            {
                if (getSpecified.ReturnType != typeof(bool)
                    || getSpecified.IsStatic
                    || getSpecified.GetParameters().Length != 0)
                {
                    throw new ArgumentException("Invalid pattern for checking member-specified", "getSpecified");
                }
            }
            if (setSpecified != null)
            {
                ParameterInfo[] args;
                if (setSpecified.ReturnType != typeof(void)
                    || setSpecified.IsStatic
                    || (args = setSpecified.GetParameters()).Length != 1
                    || args[0].ParameterType != typeof(bool))
                {
                    throw new ArgumentException("Invalid pattern for setting member-specified", "setSpecified");
                }
            }
            ThrowIfFrozen();
            this.getSpecified = getSpecified;
            this.setSpecified = setSpecified;
            
        }
        private void ThrowIfFrozen()
        {
            if (serializer != null) throw new InvalidOperationException("The type cannot be changed once a serializer has been generated");
        }
        private IProtoSerializer BuildSerializer()
        {
            bool lockTaken = false;
            try
            {
                model.TakeLock(ref lockTaken);// check nobody is still adding this type
                WireType wireType;
                Type finalType = itemType == null ? memberType : itemType;
                IProtoSerializer ser = TryGetCoreSerializer(model, dataFormat, finalType, out wireType, asReference, dynamicType);
                if (ser == null) throw new InvalidOperationException("No serializer defined for type: " + finalType.FullName);
                // apply tags
                ser = new TagDecorator(fieldNumber, wireType, IsStrict, ser);
                // apply lists if appropriate
                if (itemType != null)
                {
                    Helpers.DebugAssert(itemType == ser.ExpectedType, "Wrong type in the tail");
                    if (memberType.IsArray)
                    {
                        ser = new ArrayDecorator(ser, fieldNumber, IsPacked, wireType, memberType, OverwriteList);
                    }
                    else
                    {
                        ser = new ListDecorator(memberType, defaultType, ser, fieldNumber, IsPacked, wireType, member == null || PropertyDecorator.CanWrite(member), OverwriteList);
                    }
                }
                else if (defaultValue != null && !IsRequired)
                {
                    ser = new DefaultValueDecorator(defaultValue, ser);
                }
                if (memberType == typeof(Uri))
                {
                    ser = new UriDecorator(ser);
                }
                if (member != null)
                {
                    switch (member.MemberType)
                    {
                        case MemberTypes.Property:
                            ser = new PropertyDecorator(parentType, (PropertyInfo)member, ser); break;
                        case MemberTypes.Field:
                            ser = new FieldDecorator(parentType, (FieldInfo)member, ser); break;
                        default:
                            throw new InvalidOperationException();
                    }
                    if (getSpecified != null || setSpecified != null)
                    {
                        ser = new MemberSpecifiedDecorator(getSpecified, setSpecified, ser);
                    }
                }
                return ser;
            }
            finally
            {
                model.ReleaseLock(lockTaken);
            }
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
        private static WireType GetDateTimeWireType(DataFormat format)
        {
            switch (format)
            {
                case DataFormat.Group: return WireType.StartGroup;
                case DataFormat.FixedSize: return WireType.Fixed64;
                case DataFormat.Default: return WireType.String;
                default: throw new InvalidOperationException();
            }
        }

        internal static IProtoSerializer TryGetCoreSerializer(RuntimeTypeModel model, DataFormat dataFormat, Type type, out WireType defaultWireType, bool asReference, bool dynamicType)
        {
#if !NO_GENERICS
            type = Nullable.GetUnderlyingType(type) ?? type;
#endif
            if (type.IsEnum)
            {
                if (model != null)
                {
                    // need to do this before checking the typecode; an int enum will report Int32 etc
                    defaultWireType = WireType.Variant;
                    return new EnumSerializer(type, model.GetEnumMap(type));
                }
                else
                { // enum is fine for adding as a meta-type
                    defaultWireType = WireType.None;
                    return null;
                }
            }
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
                    return new UInt64Serializer();
                case TypeCode.String:
                    defaultWireType = WireType.String;
                    if (asReference)
                    {
                        return new NetObjectSerializer(typeof(string), 0, true, false);
                    }
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
                    defaultWireType = GetDateTimeWireType(dataFormat);
                    return new DateTimeSerializer();
                case TypeCode.Decimal:
                    defaultWireType = WireType.String;
                    return new DecimalSerializer();
                case TypeCode.Byte:
                    defaultWireType = GetIntWireType(dataFormat, 32);
                    return new ByteSerializer();
                case TypeCode.SByte:
                    defaultWireType = GetIntWireType(dataFormat, 32);
                    return new SByteSerializer();
                case TypeCode.Char:
                    defaultWireType = WireType.Variant;
                    return new CharSerializer();
                case TypeCode.Int16:
                    defaultWireType = GetIntWireType(dataFormat, 32);
                    return new Int16Serializer();
                case TypeCode.UInt16:
                    defaultWireType = GetIntWireType(dataFormat, 32);
                    return new UInt16Serializer();
            }
            if (type == typeof(TimeSpan))
            {
                defaultWireType = GetDateTimeWireType(dataFormat);
                return new TimeSpanSerializer();
            }
            if (type == typeof(Guid))
            {
                defaultWireType = WireType.String;
                return new GuidSerializer();
            }
            if (type == typeof(Uri))
            {
                defaultWireType = WireType.String;
                return new StringSerializer(); // treat as string; wrapped in decorator later
            }
            if (type == typeof(byte[]))
            {
                defaultWireType = WireType.String;
                return new BlobSerializer();
            }
            IProtoSerializer parseable = ParseableSerializer.TryCreate(type);
            if (parseable != null)
            {
                defaultWireType = WireType.String;
                return parseable;
            }
            if (model != null)
            {
                int key = model.GetKey(type, false, true);
                if (asReference || dynamicType)
                {
                    defaultWireType = WireType.String;
                    return new NetObjectSerializer(type, key, asReference, dynamicType);
                }
                if (key >= 0)
                {
                    defaultWireType = WireType.String;
                    return new SubItemSerializer(type, key, model[type], true);
                }
            }
            defaultWireType = WireType.None;
            return null;
        }


        private string name;
        internal void SetName(string name)
        {
            ThrowIfFrozen();
            this.name = name;
        }
        /// <summary>
        /// Gets the logical name for this member in the schema (this is not critical for binary serialization, but may be used
        /// when inferring a schema).
        /// </summary>
        public string Name
        {
            get { return Helpers.IsNullOrEmpty(name) ? member.Name : name; }
        }

        private const byte
           OPTIONS_IsStrict = 1,
           OPTIONS_IsPacked = 2,
           OPTIONS_IsRequired = 4,
           OPTIONS_OverwriteList = 8;

        private byte flags;
        private bool HasFlag(byte flag) { return (flags & flag) == flag; }
        private void SetFlag(byte flag, bool value, bool throwIfFrozen)
        {
            if (throwIfFrozen && HasFlag(flag) != value)
            {
                ThrowIfFrozen();
            }
            if (value)
                flags |= flag;
            else
                flags = (byte)(flags & ~flag);
        }
    }
}
#endif