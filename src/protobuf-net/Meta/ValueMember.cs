using ProtoBuf.Internal;
using ProtoBuf.Internal.Serializers;
using ProtoBuf.Serializers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Represents a member (property/field) that is mapped to a protobuf field
    /// </summary>
    public class ValueMember
    {

        private int _fieldNumber;
        /// <summary>
        /// The number that identifies this member in a protobuf stream
        /// </summary>
        public int FieldNumber
        {
            get => _fieldNumber;
            internal set
            {
                if (_fieldNumber != value)
                {
                    MetaType.AssertValidFieldNumber(value);
                    ThrowIfFrozen();
                    _fieldNumber = value;
                }
            }
        }

        private MemberInfo backingMember;
        /// <summary>
        /// Gets the member (field/property) which this member relates to.
        /// </summary>
        public MemberInfo Member { get; }

        /// <summary>
        /// Gets the backing member (field/property) which this member relates to
        /// </summary>
        public MemberInfo BackingMember
        {
            get { return backingMember; }
            set
            {
                if (backingMember != value)
                {
                    ThrowIfFrozen();
                    backingMember = value;
                }
            }
        }

        private object _defaultValue;

        /// <summary>
        /// Within a list / array / etc, the type of object for each item in the list (especially useful with ArrayList)
        /// </summary>
        public Type ItemType { get; }

        /// <summary>
        /// The underlying type of the member
        /// </summary>
        public Type MemberType { get; }

        /// <summary>
        /// For abstract types (IList etc), the type of concrete object to create (if required)
        /// </summary>
        public Type DefaultType { get; }

        /// <summary>
        /// The type the defines the member
        /// </summary>
        public Type ParentType { get; }

        /// <summary>
        /// The default value of the item (members with this value will not be serialized)
        /// </summary>
        public object DefaultValue
        {
            get { return _defaultValue; }
            set
            {
                if (_defaultValue != value)
                {
                    ThrowIfFrozen();
                    _defaultValue = value;
                }
            }
        }

        private CompatibilityLevel _compatibilityLevel;

        /// <summary>
        /// Gets or sets the <see cref="CompatibilityLevel"/> of this member; by default this is inherited from
        /// the type; when <see cref="CompatibilityLevel.Level200"/> is used with <see cref="DataFormat.WellKnown"/>,
        /// the member is considered <see cref="CompatibilityLevel.Level240"/>.
        /// </summary>
        public CompatibilityLevel CompatibilityLevel
        {
            get => _compatibilityLevel;
            set
            {
                if (_compatibilityLevel != value)
                {
                    ThrowIfFrozen();
                    CompatibilityLevelAttribute.AssertValid(value);
                    _compatibilityLevel = value;
                }
            }
        }

        internal static CompatibilityLevel GetEffectiveCompatibilityLevel(CompatibilityLevel compatibilityLevel, DataFormat dataFormat)
        {
            if (compatibilityLevel <= CompatibilityLevel.Level200)
            {
                return dataFormat switch
                {
#pragma warning disable CS0618
                    DataFormat.WellKnown => CompatibilityLevel.Level240,
#pragma warning restore CS0618
                    _ => CompatibilityLevel.Level200,
                };
            }
            return compatibilityLevel;
        }

        private readonly RuntimeTypeModel model;
        /// <summary>
        /// Creates a new ValueMember instance
        /// </summary>
        public ValueMember(RuntimeTypeModel model, Type parentType, int fieldNumber, MemberInfo member, Type memberType, Type itemType, Type defaultType, DataFormat dataFormat, object defaultValue)
            : this(model, fieldNumber, memberType, itemType, defaultType, dataFormat)
        {
            if (parentType is null) throw new ArgumentNullException(nameof(parentType));
            if (fieldNumber < 1 && !parentType.IsEnum) throw new ArgumentOutOfRangeException(nameof(fieldNumber));

            Member = member ?? throw new ArgumentNullException(nameof(member));
            ParentType = parentType;
            if (fieldNumber < 1 && !parentType.IsEnum) throw new ArgumentOutOfRangeException(nameof(fieldNumber));

            if (defaultValue is not null && (defaultValue.GetType() != memberType))
            {
                defaultValue = ParseDefaultValue(memberType, defaultValue);
            }
            _defaultValue = defaultValue;

#if FEAT_DYNAMIC_REF
            MetaType type = model.FindWithoutAdd(memberType);
            if (type is object)
            {
                AsReference = type.AsReferenceDefault;
            }
            else
            { // we need to scan the hard way; can't risk recursion by fully walking it
                AsReference = MetaType.GetAsReferenceDefault(memberType);
            }
#endif
        }
        /// <summary>
        /// Creates a new ValueMember instance
        /// </summary>
        internal ValueMember(RuntimeTypeModel model, int fieldNumber, Type memberType, Type itemType, Type defaultType, DataFormat dataFormat)
        {
            FieldNumber = fieldNumber;
            MemberType = memberType ?? throw new ArgumentNullException(nameof(memberType));
            ItemType = itemType;
            if (defaultType is null && itemType is not null)
            {   // reasonable default
                defaultType = memberType;
            }
            DefaultType = defaultType;

            this.model = model ?? throw new ArgumentNullException(nameof(model));
            this.dataFormat = dataFormat;
        }
        internal object GetRawEnumValue()
        {
            return ((FieldInfo)Member).GetRawConstantValue();
        }
        private static object ParseDefaultValue(Type type, object value)
        {
            {
                Type tmp = Nullable.GetUnderlyingType(type);
                if (tmp is not null) type = tmp;
            }
            if (value is string s)
            {
                if (type.IsEnum) return Enum.Parse(type, s, true);

                switch (Helpers.GetTypeCode(type))
                {
                    case ProtoTypeCode.Boolean: return bool.Parse(s);
                    case ProtoTypeCode.Byte: return byte.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    case ProtoTypeCode.Char: // char.Parse missing on CF/phone7
                        if (s.Length == 1) return s[0];
                        throw new FormatException("Single character expected: \"" + s + "\"");
                    case ProtoTypeCode.DateTime: return DateTime.Parse(s, CultureInfo.InvariantCulture);
                    case ProtoTypeCode.Decimal: return decimal.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case ProtoTypeCode.Double: return double.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case ProtoTypeCode.Int16: return short.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case ProtoTypeCode.Int32: return int.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case ProtoTypeCode.Int64: return long.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case ProtoTypeCode.SByte: return sbyte.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    case ProtoTypeCode.Single: return float.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case ProtoTypeCode.String: return s;
                    case ProtoTypeCode.UInt16: return ushort.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case ProtoTypeCode.UInt32: return uint.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case ProtoTypeCode.UInt64: return ulong.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture);
                    case ProtoTypeCode.TimeSpan: return TimeSpan.Parse(s);
                    case ProtoTypeCode.Uri: return s; // Uri is decorated as string
                    case ProtoTypeCode.Guid: return new Guid(s);
                    case ProtoTypeCode.IntPtr: return new IntPtr(long.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
                    case ProtoTypeCode.UIntPtr: return new UIntPtr(ulong.Parse(s, NumberStyles.Any, CultureInfo.InvariantCulture));
                }
            }

            if (type.IsEnum) return Enum.ToObject(type, value);
            try
            {
                return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException) when (type == typeof(IntPtr))
            {
                return new IntPtr((long)Convert.ChangeType(value, typeof(long), CultureInfo.InvariantCulture));
            }
            catch (InvalidCastException) when (type == typeof(UIntPtr))
            {
                return new UIntPtr((ulong)Convert.ChangeType(value, typeof(ulong), CultureInfo.InvariantCulture));
            }
        }

        private IRuntimeProtoSerializerNode serializer;
        internal IRuntimeProtoSerializerNode Serializer
        {
            get
            {
                return serializer ??= BuildSerializer();
            }
        }

        private DataFormat dataFormat;
        /// <summary>
        /// Specifies the rules used to process the field; this is used to determine the most appropriate
        /// wite-type, but also to describe subtypes <i>within</i> that wire-type (such as SignedVariant)
        /// </summary>
        public DataFormat DataFormat
        {
            get { return dataFormat; }
            set
            {
                if (value != dataFormat)
                {
                    ThrowIfFrozen();
                    this.dataFormat = value;
                }
            }
        }

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
        /// Indicates whether this field should *replace* existing values (the default is false, meaning *append*).
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

        /// <summary>
        /// Enables full object-tracking/full-graph support.
        /// </summary>
        public bool AsReference
        {
#if FEAT_DYNAMIC_REF
            get { return HasFlag(OPTIONS_AsReference); }
            set { SetFlag(OPTIONS_AsReference, value, true); }
#else
            get => false;
            [Obsolete(ProtoContractAttribute.ReferenceDynamicDisabled, true)]
            set { if (value != AsReference) ThrowHelper.ThrowNotSupportedException(); }
#endif
        }

        /// <summary>
        /// Embeds the type information into the stream, allowing usage with types not known in advance.
        /// </summary>
        public bool DynamicType
        {
#if FEAT_DYNAMIC_REF
            get { return HasFlag(OPTIONS_DynamicType); }
            set { SetFlag(OPTIONS_DynamicType, value, true); }
#else
            get => false;
            [Obsolete(ProtoContractAttribute.ReferenceDynamicDisabled, true)]
            set { if (value != DynamicType) ThrowHelper.ThrowNotSupportedException(); }
#endif
        }

        /// <summary>
        /// Indicates that the member should be treated as a protobuf Map
        /// </summary>
        public bool IsMap
        {
            get { return HasFlag(OPTIONS_IsMap); }
            set { SetFlag(OPTIONS_IsMap, value, true); }
        }

        private DataFormat mapKeyFormat, mapValueFormat;
        /// <summary>
        /// Specifies the data-format that should be used for the key, when IsMap is enabled
        /// </summary>
        public DataFormat MapKeyFormat
        {
            get { return mapKeyFormat; }
            set
            {
                if (mapKeyFormat != value)
                {
                    ThrowIfFrozen();
                    mapKeyFormat = value;
                }
            }
        }
        /// <summary>
        /// Specifies the data-format that should be used for the value, when IsMap is enabled
        /// </summary>
        public DataFormat MapValueFormat
        {
            get { return mapValueFormat; }
            set
            {
                if (mapValueFormat != value)
                {
                    ThrowIfFrozen();
                    mapValueFormat = value;
                }
            }
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
            if (this.getSpecified != getSpecified || this.setSpecified != setSpecified)
            {
                if (getSpecified is not null)
                {
                    if (getSpecified.ReturnType != typeof(bool)
                        || getSpecified.IsStatic
                        || getSpecified.GetParameters().Length != 0)
                    {
                        throw new ArgumentException("Invalid pattern for checking member-specified", nameof(getSpecified));
                    }
                }
                if (setSpecified is not null)
                {
                    ParameterInfo[] args;
                    if (setSpecified.ReturnType != typeof(void)
                        || setSpecified.IsStatic
                        || (args = setSpecified.GetParameters()).Length != 1
                        || args[0].ParameterType != typeof(bool))
                    {
                        throw new ArgumentException("Invalid pattern for setting member-specified", nameof(setSpecified));
                    }
                }

                ThrowIfFrozen();
                this.getSpecified = getSpecified;
                this.setSpecified = setSpecified;
            }
        }

        private void ThrowIfFrozen()
        {
            if (serializer is object) throw new InvalidOperationException("The type cannot be changed once a serializer has been generated");
        }

        internal static IRuntimeProtoSerializerNode CreateMap(RepeatedSerializerStub repeated, RuntimeTypeModel model, DataFormat dataFormat,
            CompatibilityLevel compatibilityLevel,
            DataFormat keyFormat, DataFormat valueFormat, bool asReference, bool dynamicType, bool isMap, bool overwriteList, int fieldNumber, ValueMember member)
        {
            static Type FlattenRepeated(RuntimeTypeModel model, Type type)
            {   // for the purposes of choosing features, we want to look inside things like arrays/lists/etc
                if (type is null) return type;
                var repeated = model is null ? RepeatedSerializers.TryGetRepeatedProvider(type) : model.TryGetRepeatedProvider(type);
                return repeated is null ? type : repeated.ItemType;
            }

            var keyCompatibilityLevel = GetEffectiveCompatibilityLevel(compatibilityLevel, keyFormat);
            var valueCompatibilityLevel = GetEffectiveCompatibilityLevel(compatibilityLevel, valueFormat);

            repeated.ResolveMapTypes(out var keyType, out var valueType);
            _ = TryGetCoreSerializer(model, keyFormat, keyCompatibilityLevel, FlattenRepeated(model, keyType), out var keyWireType, false, false, false, true);
            _ = TryGetCoreSerializer(model, valueFormat, valueCompatibilityLevel, FlattenRepeated(model, valueType), out var valueWireType, asReference, dynamicType, false, true);

            WireType rootWireType = dataFormat == DataFormat.Group ? WireType.StartGroup : WireType.String;
            SerializerFeatures features = rootWireType.AsFeatures(); // | SerializerFeatures.OptionReturnNothingWhenUnchanged;
            if (!isMap) features |= SerializerFeatures.OptionFailOnDuplicateKey;
            if (overwriteList) features |= SerializerFeatures.OptionClearCollection;

            member?.ComposeListFeatures(ref features);

            // transfer OptionWrappedValue and OptionWrappedValueGroup from the serializer features to the value features
            var valueFeatures = valueWireType.AsFeatures();
            valueFeatures |= features & (SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueGroup);
            features &= ~(SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueGroup);

            return MapDecorator.Create(repeated, keyType, valueType, fieldNumber, features,
                keyWireType.AsFeatures(), keyCompatibilityLevel, keyFormat, valueFeatures, valueCompatibilityLevel, valueFormat);
        }

        void ComposeListFeatures(ref SerializerFeatures listFeatures)
        {
            if (!IsPacked) listFeatures |= SerializerFeatures.OptionPackedDisabled;
            if (OverwriteList) listFeatures |= SerializerFeatures.OptionClearCollection;
#pragma warning disable CS0618
            if (SupportNull)
            {
                if (NullWrappedValue || NullWrappedCollection || IsPacked)
                {
                    ThrowHelper.ThrowNotSupportedException($"{nameof(SupportNull)} cannot be combined with {nameof(IsPacked)}, {nameof(NullWrappedValue)} or {nameof(NullWrappedCollection)}");
                }
                listFeatures |= SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueGroup;
            }
#pragma warning restore CS0618
            else
            {
                if (NullWrappedCollection)
                {
                    listFeatures |= SerializerFeatures.OptionWrappedCollection;
                    if (NullWrappedCollectionGroup) listFeatures |= SerializerFeatures.OptionWrappedCollectionGroup;
                }
                if (NullWrappedValue)
                {
                    listFeatures |= SerializerFeatures.OptionWrappedValue | SerializerFeatures.OptionWrappedValueFieldPresence;
                    if (NullWrappedValueGroup) listFeatures |= SerializerFeatures.OptionWrappedValueGroup;
                }
            }
        }
        private IRuntimeProtoSerializerNode BuildSerializer()
        {
            int opaqueToken = 0;
            try
            {
                model.TakeLock(ref opaqueToken);// check nobody is still adding this type
                var member = backingMember ?? Member;
                IRuntimeProtoSerializerNode ser;

                var repeated = model.TryGetRepeatedProvider(MemberType);

                if (repeated is not null)
                {
                    if (repeated.IsMap)
                    {
#if FEAT_DYNAMIC_REF
                        if (!AsReference)
                        {
                            AsReference = MetaType.GetAsReferenceDefault(valueType);
                        }
#endif
                        ser = CreateMap(repeated, model, DataFormat, CompatibilityLevel, MapKeyFormat, MapValueFormat, AsReference, DynamicType, IsMap, OverwriteList, FieldNumber, this);
                    }
                    else
                    {
                        _ = TryGetCoreSerializer(model, DataFormat, CompatibilityLevel, repeated.ItemType, out WireType wireType, AsReference, DynamicType, OverwriteList, true);

                        SerializerFeatures listFeatures = wireType.AsFeatures(); // | SerializerFeatures.OptionReturnNothingWhenUnchanged;
                        ComposeListFeatures(ref listFeatures);

                        ser = RepeatedDecorator.Create(repeated, FieldNumber, listFeatures, CompatibilityLevel, DataFormat);
                    }
                }
                else
                {
                    if (NullWrappedCollection) ThrowHelper.ThrowNotSupportedException($"{nameof(NullWrappedCollection)} can only be used with collection types");
                    ser = TryGetCoreSerializer(model, DataFormat, CompatibilityLevel, MemberType, out WireType wireType, AsReference, DynamicType, OverwriteList, allowComplexTypes: true);
                    if (ser is null) ThrowHelper.NoSerializerDefined(MemberType);
                    if (NullWrappedValue)
                    {
                        var valueFeatures = SerializerFeatures.OptionWrappedValue;
                        if (NullWrappedValueGroup) valueFeatures |= SerializerFeatures.OptionWrappedValueGroup;
                        if (MemberType.IsValueType && Nullable.GetUnderlyingType(MemberType) is null) ThrowHelper.ThrowNotSupportedException($"{nameof(NullWrappedValue)} cannot be used with non-nullable values");
                        if (_defaultValue is not null) ThrowHelper.ThrowNotSupportedException($"{nameof(NullWrappedValue)} cannot be used with default values");
                        if (IsRequired) ThrowHelper.ThrowNotSupportedException($"{nameof(NullWrappedValue)} cannot be used with required values");
                        if (IsPacked) ThrowHelper.ThrowNotSupportedException($"{nameof(NullWrappedValue)} cannot be used with packed values");
                        if (DataFormat != DataFormat.Default) ThrowHelper.ThrowNotSupportedException($"{nameof(NullWrappedValue)} can only be used with {nameof(DataFormat)}.{nameof(DataFormat.Default)}");
                        if (!ser.IsScalar) ThrowHelper.ThrowNotSupportedException($"{nameof(NullWrappedValue)} can only be used with scalar types, or in a collection");

                        // we now replace 'ser' with a serializer that uses read/write-any ([wrapped]), but it was
                        // useful to know that we can at least get a suitable serializer
                        ser = AnyTypeSerializer.Create(MemberType, valueFeatures, CompatibilityLevel, DataFormat);
                    }
                    ser = new TagDecorator(FieldNumber, wireType, IsStrict, ser);

                    if (_defaultValue is not null && !IsRequired && getSpecified is null)
                    {   // note: "ShouldSerialize*" / "*Specified" / etc ^^^^ take precedence over defaultValue,
                        // as does "IsRequired"
                        ser = new DefaultValueDecorator(_defaultValue, ser);
                    }
                    if (MemberType == typeof(Uri))
                    {
                        ser = new UriDecorator(ser);
                    }
                }
                if (member is not null)
                {
                    if (member is PropertyInfo prop)
                    {
                        ser = new PropertyDecorator(ParentType, prop, ser);
                    }
                    else if (member is FieldInfo fld)
                    {
                        ser = new FieldDecorator(ParentType, fld, ser);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }

                    if (getSpecified is not null || setSpecified is not null)
                    {
                        ser = new MemberSpecifiedDecorator(getSpecified, setSpecified, ser);
                    }
                }
                return ser;
            }
            finally
            {
                model.ReleaseLock(opaqueToken);
            }
        }

        private static WireType GetIntWireType(DataFormat format, int width)
        {
            switch (format)
            {
                case DataFormat.ZigZag: return WireType.SignedVarint;
                case DataFormat.FixedSize: return width == 32 ? WireType.Fixed32 : WireType.Fixed64;
                case DataFormat.TwosComplement:
#pragma warning disable CS0618
                case DataFormat.WellKnown: return WireType.Varint;
#pragma warning restore CS0618
                case DataFormat.Default: return WireType.Varint;
                default: throw new InvalidOperationException();
            }
        }
        private static WireType GetDateTimeWireType(DataFormat format)
        {
            switch (format)
            {
                case DataFormat.Group: return WireType.StartGroup;
                case DataFormat.FixedSize: return WireType.Fixed64;
#pragma warning disable CS0618
                case DataFormat.WellKnown:
#pragma warning restore CS0618
                case DataFormat.Default:
                    return WireType.String;
                default: throw new InvalidOperationException();
            }
        }

        internal static IRuntimeProtoSerializerNode TryGetCoreSerializer(RuntimeTypeModel model, DataFormat dataFormat, CompatibilityLevel compatibilityLevel, Type type, out WireType defaultWireType,
            bool asReference, bool dynamicType, bool overwriteList, bool allowComplexTypes)
        {
            compatibilityLevel = ValueMember.GetEffectiveCompatibilityLevel(compatibilityLevel, dataFormat);
            type = DynamicStub.GetEffectiveType(type);
            if (type.IsEnum)
            {
                if (allowComplexTypes && model is not null)
                {
                    // need to do this before checking the typecode; an int enum will report Int32 etc
                    defaultWireType = WireType.Varint;
                    return new EnumMemberSerializer(type);
                }
                else
                { // enum is fine for adding as a meta-type
                    defaultWireType = WireType.None;
                    return null;
                }
            }
            ProtoTypeCode code = Helpers.GetTypeCode(type);
            switch (code)
            {
                case ProtoTypeCode.Int32:
                    defaultWireType = GetIntWireType(dataFormat, 32);
                    return Int32Serializer.Instance;
                case ProtoTypeCode.UInt32:
                    defaultWireType = GetIntWireType(dataFormat, 32);
                    return UInt32Serializer.Instance;
                case ProtoTypeCode.Int64:
                    defaultWireType = GetIntWireType(dataFormat, 64);
                    return Int64Serializer.Instance;
                case ProtoTypeCode.UInt64:
                    defaultWireType = GetIntWireType(dataFormat, 64);
                    return UInt64Serializer.Instance;
                case ProtoTypeCode.String:
                    defaultWireType = WireType.String;
                    if (asReference)
                    {
#if FEAT_DYNAMIC_REF
                        return new NetObjectSerializer(typeof(string), BclHelpers.NetObjectOptions.AsReference);
#else
                        ThrowHelper.ThrowNotSupportedException(ProtoContractAttribute.ReferenceDynamicDisabled);
                        return default;
#endif
                    }
                    return StringSerializer.Instance;
                case ProtoTypeCode.Single:
                    defaultWireType = WireType.Fixed32;
                    return SingleSerializer.Instance;
                case ProtoTypeCode.Double:
                    defaultWireType = WireType.Fixed64;
                    return DoubleSerializer.Instance;
                case ProtoTypeCode.Boolean:
                    defaultWireType = WireType.Varint;
                    return BooleanSerializer.Instance;
                case ProtoTypeCode.DateTime:
                    defaultWireType = GetDateTimeWireType(dataFormat);
                    return DateTimeSerializer.Create(compatibilityLevel, model);
                case ProtoTypeCode.Decimal:
                    defaultWireType = WireType.String;
                    return DecimalSerializer.Create(compatibilityLevel);
                case ProtoTypeCode.Byte:
                    defaultWireType = GetIntWireType(dataFormat, 32);
                    return ByteSerializer.Instance;
                case ProtoTypeCode.SByte:
                    defaultWireType = GetIntWireType(dataFormat, 32);
                    return SByteSerializer.Instance;
                case ProtoTypeCode.Char:
                    defaultWireType = WireType.Varint;
                    return CharSerializer.Instance;
                case ProtoTypeCode.Int16:
                    defaultWireType = GetIntWireType(dataFormat, 32);
                    return Int16Serializer.Instance;
                case ProtoTypeCode.UInt16:
                    defaultWireType = GetIntWireType(dataFormat, 32);
                    return UInt16Serializer.Instance;
                case ProtoTypeCode.TimeSpan:
                    defaultWireType = GetDateTimeWireType(dataFormat);
                    return TimeSpanSerializer.Create(compatibilityLevel);
                case ProtoTypeCode.Guid:
                    defaultWireType = (dataFormat == DataFormat.Group && compatibilityLevel < CompatibilityLevel.Level300) ? WireType.StartGroup : WireType.String;
                    return GuidSerializer.Create(compatibilityLevel, dataFormat);
                case ProtoTypeCode.Uri:
                    defaultWireType = WireType.String;
                    return StringSerializer.Instance;
                case ProtoTypeCode.ByteArray:
                    defaultWireType = WireType.String;
                    return new BlobSerializer<byte[]>(overwriteList);
                case ProtoTypeCode.ByteArraySegment:
                    defaultWireType = WireType.String;
                    return new BlobSerializer<ArraySegment<byte>>(overwriteList);
                case ProtoTypeCode.ByteMemory:
                    defaultWireType = WireType.String;
                    return new BlobSerializer<Memory<byte>>(overwriteList);
                case ProtoTypeCode.ByteReadOnlyMemory:
                    defaultWireType = WireType.String;
                    return new BlobSerializer<ReadOnlyMemory<byte>>(overwriteList);
                case ProtoTypeCode.Type:
                    defaultWireType = WireType.String;
                    return SystemTypeSerializer.Instance;
                case ProtoTypeCode.IntPtr:
                    defaultWireType = GetIntWireType(dataFormat, 64);
                    return IntPtrSerializer.Instance;
                case ProtoTypeCode.UIntPtr:
                    defaultWireType = GetIntWireType(dataFormat, 64);
                    return UIntPtrSerializer.Instance;
#if NET6_0_OR_GREATER
                case ProtoTypeCode.DateOnly:
                    defaultWireType = WireType.Varint;
                    return DateOnlySerializer.Instance;
                case ProtoTypeCode.TimeOnly:
                    defaultWireType = WireType.Varint;
                    return TimeOnlySerializer.Instance;
#endif
            }
            IRuntimeProtoSerializerNode parseable = model.AllowParseableTypes ? ParseableSerializer.TryCreate(type) : null;
            if (parseable is object)
            {
                defaultWireType = WireType.String;
                return parseable;
            }
            if (allowComplexTypes && model is not null)
            {
                MetaType meta = null;
                if (model.IsDefined(type, compatibilityLevel))
                {
                    meta = model.FindWithAmbientCompatibility(type, compatibilityLevel);

                    if (dataFormat == DataFormat.Default && meta.IsGroup)
                    {
                        dataFormat = DataFormat.Group;
                    }
                }

                if (asReference || dynamicType)
                {
#if FEAT_DYNAMIC_REF
                    BclHelpers.NetObjectOptions options = BclHelpers.NetObjectOptions.None;
                    if (asReference) options |= BclHelpers.NetObjectOptions.AsReference;
                    if (dynamicType) options |= BclHelpers.NetObjectOptions.DynamicType;

                    if (meta is object)
                    { // exists
                        if (asReference && type.IsValueType)
                        {
                            string message = "AsReference cannot be used with value-types";

                            if (type.Name == "KeyValuePair`2")
                            {
                                message += "; please see https://stackoverflow.com/q/14436606/23354";
                            }
                            else
                            {
                                message += ": " + type.FullName;
                            }
                            throw new InvalidOperationException(message);
                        }

                        if (asReference && (meta.IsAutoTuple || meta.HasSurrogate)) options |= BclHelpers.NetObjectOptions.LateSet;
                        if (meta.UseConstructor) options |= BclHelpers.NetObjectOptions.UseConstructor;
                    }
                    defaultWireType = dataFormat == DataFormat.Group ? WireType.StartGroup : WireType.String;
                    return new NetObjectSerializer(type, options);
#else
                    ThrowHelper.ThrowNotSupportedException(ProtoContractAttribute.ReferenceDynamicDisabled);
                    defaultWireType = default;
                    return default;
#endif
                }
                if (meta is not null)
                {
                    IProtoTypeSerializer serializer;
                    if (meta.HasSurrogate && (serializer = meta.Serializer).Features.GetCategory() == SerializerFeatures.CategoryScalar)
                    {
                        dataFormat = meta.surrogateDataFormat;
                        // this checks for an overriding wire-type/data-format combo
                        if (TryGetCoreSerializer(model, dataFormat, meta.CompatibilityLevel, meta.surrogateType, out defaultWireType, false, false, false, false) is null)
                        {   // otherwise, defer to the serializer
                            defaultWireType = serializer.Features.GetWireType();
                        }
                        return serializer;
                    }
                    else
                    {
                        return SubItemSerializer.Create(type, meta, ref dataFormat, out defaultWireType);
                    }
                }
            }
            defaultWireType = WireType.None;
            return null;
        }

        private string name;
        internal void SetName(string name)
        {
            if (name != this.name)
            {
                ThrowIfFrozen();
                this.name = name;
            }
        }
        /// <summary>
        /// Gets the logical name for this member in the schema (this is not critical for binary serialization, but may be used
        /// when inferring a schema).
        /// </summary>
        public string Name
        {
            get { return string.IsNullOrEmpty(name) ? Member.Name : name; }
            set { SetName(value); }
        }

        private const ushort
           OPTIONS_IsStrict = 1 << 0,
           OPTIONS_IsPacked = 1 << 1,
           OPTIONS_IsRequired = 1 << 2,
           OPTIONS_OverwriteList = 1 << 3,
           OPTIONS_NullWrappedValue = 1 << 4,
           OPTIONS_NullWrappedValueGroup = 1 << 5,
#if FEAT_DYNAMIC_REF
           OPTIONS_AsReference = ,
           OPTIONS_DynamicType = ,
#endif
           OPTIONS_IsMap = 1 << 6,
           OPTIONS_NullWrappedCollection = 1 << 7,
           OPTIONS_NullWrappedCollectionGroup = 1 << 8,
           OPTIONS_SupportNull = 1 << 9;

        private ushort flags;
        private bool HasFlag(ushort flag) { return (flags & flag) == flag; }
        private void SetFlag(ushort flag, bool value, bool throwIfFrozen = true)
        {
            if (throwIfFrozen && HasFlag(flag) != value)
            {
                ThrowIfFrozen();
            }
            if (value)
                flags |= flag;
            else
                flags = (ushort)(flags & ~flag);
        }

        /// <summary>
        /// Should lists have extended support for null values? Note this makes the serialization less efficient.
        /// </summary>
        //[Obsolete("Please use " + nameof(NullWrappedValue) + " with " + nameof(NullWrappedValueGroup) + "; see the documentation for " + nameof(NullWrappedValueAttribute) + " for more information.")]
        public bool SupportNull
        {
            get { return HasFlag(OPTIONS_SupportNull); }
            set { SetFlag(OPTIONS_SupportNull, value); }
        }

        /// <see cref="NullWrappedValueAttribute"/>
        internal bool NullWrappedValue
        {
            get { return HasFlag(OPTIONS_NullWrappedValue); }
            set { SetFlag(OPTIONS_NullWrappedValue, value); }
        }

        /// <see cref="NullWrappedValueAttribute.AsGroup"/>
        internal bool NullWrappedValueGroup
        {
            get { return HasFlag(OPTIONS_NullWrappedValueGroup); }
            set { SetFlag(OPTIONS_NullWrappedValueGroup, value); }
        }

        /// <see cref="NullWrappedValueAttribute"/>
        internal bool NullWrappedCollection
        {
            get { return HasFlag(OPTIONS_NullWrappedCollection); }
            set { SetFlag(OPTIONS_NullWrappedCollection, value); }
        }

        /// <see cref="NullWrappedValueAttribute.AsGroup"/>
        internal bool NullWrappedCollectionGroup
        {
            get { return HasFlag(OPTIONS_NullWrappedCollectionGroup); }
            set { SetFlag(OPTIONS_NullWrappedCollectionGroup, value); }
        }

        /// <see href="https://github.com/protobuf-net/protobuf-net/blob/main/docs/nullwrappers.md"/>
        internal bool HasExtendedNullSupport() => SupportNull || NullWrappedValueGroup || NullWrappedValue;
        
        /// <see href="https://github.com/protobuf-net/protobuf-net/blob/main/docs/nullwrappers.md"/>
        /// <remarks>NullWrappers are needed **only** for items of collections</remarks>
        internal bool RequiresExtraLayerInSchema() => ItemType is not null && HasExtendedNullSupport();
        
        /// <summary>
        /// Requires `group` to be placed on original valueMember level
        /// </summary>
        internal bool RequiresGroupModifier => SupportNull || NullWrappedValueGroup;

        internal string GetSchemaTypeName(HashSet<Type> callstack, bool applyNetObjectProxy, HashSet<string> imports, out string altName, bool considerWrappersProtoTypes = false)
        {
            Type effectiveType = ItemType ?? MemberType;
            return model.GetSchemaTypeName(callstack, effectiveType, DataFormat, CompatibilityLevel, applyNetObjectProxy && AsReference, applyNetObjectProxy && DynamicType, imports, out altName, considerWrappersProtoTypes);
        }

        internal sealed class Comparer : System.Collections.IComparer, IComparer<ValueMember>
        {
            public static readonly Comparer Default = new Comparer();

            public int Compare(object x, object y)
            {
                return Compare(x as ValueMember, y as ValueMember);
            }

            public int Compare(ValueMember x, ValueMember y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x is null) return -1;
                if (y is null) return 1;

                return x.FieldNumber.CompareTo(y.FieldNumber);
            }
        }
    }
}