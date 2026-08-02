#nullable enable
using System;

namespace ProtoBuf.BuildTools.Internal.Aot
{
    /// <summary>
    /// The kinds of member the AOT generator can currently emit. Anything not listed here causes
    /// the owning contract to be omitted from the model rather than guessed at.
    /// </summary>
    internal enum ProtoMemberKind
    {
        Bool,
        SByte,
        Byte,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Single,
        Double,

        /// <summary>A <c>char</c>; written as a <c>ushort</c> varint.</summary>
        Char,

        String,

        /// <summary>
        /// A <c>byte[]</c>. Length-prefixed like a string, but neither the null test nor the field
        /// header is done for us, and reads <em>append</em> rather than replace.
        /// </summary>
        Bytes,

        /// <summary>A nested contract, served by another serializer on the same services type.</summary>
        Message,

        // the four BCL types whose encoding is chosen by the compatibility level rather than by the
        // type alone; all of them are length-prefixed and go through BclHelpers

        DateTime,
        TimeSpan,
        Guid,
        Decimal,

        /// <summary>
        /// <c>nint</c>/<c>nuint</c>. An ordinary varint rather than a compatibility-level type; the
        /// wire width is fixed at 64 regardless of the platform, so the form does not vary.
        /// </summary>
        IntPtr,
        UIntPtr,

        /// <summary>
        /// net6.0+ only. Unlike the four compatibility-level types, these go through
        /// <c>BclHelpers</c> under a <b>varint</b> header rather than a length prefix.
        /// </summary>
        DateOnly,
        TimeOnly,

        /// <summary>
        /// A type serialized as a string via <c>ToString()</c> and <c>static T Parse(string)</c>.
        /// Opt-in only, matching <c>RuntimeTypeModel.AllowParseableTypes</c>, which is off by
        /// default — turning it on changes the wire form of every member whose type qualifies.
        /// </summary>
        Parseable,

        /// <summary>
        /// <c>System.Uri</c>, which protobuf-net has inbuilt behaviour for: a plain string on the wire,
        /// <c>OriginalString</c> out and <c>new Uri(s, UriKind.RelativeOrAbsolute)</c> back, with an
        /// empty string meaning null. Not a surrogate case - <c>SetSurrogate</c> refuses it outright.
        /// </summary>
        Uri,

        /// <summary>
        /// A dictionary. Unlike every other kind this says nothing about the member on its own —
        /// <see cref="ProtoMemberPlan.Map"/> carries both element types.
        /// </summary>
        Map,
    }

    /// <summary>
    /// The subset of <c>DataFormat</c> that changes what we emit.
    /// </summary>
    /// <remarks>
    /// <c>TwosComplement</c> is deliberately absent: for the types we handle it produces byte-for-byte
    /// the same output as <c>Default</c>, so it maps onto <see cref="Default"/>.
    /// </remarks>
    internal enum ProtoDataFormat
    {
        Default,

        /// <summary>Signed varint; the read also needs a <c>state.Hint</c>.</summary>
        ZigZag,

        /// <summary>Fixed32 or Fixed64, depending on the width of the member.</summary>
        FixedSize,

        /// <summary>Group encoding for a sub-message; affects the write only.</summary>
        Group,

        /// <summary>
        /// Only meaningful on the compatibility-level BCL types, where it promotes a level-200
        /// member to level 240; it is the older, single-step form of the same idea.
        /// </summary>
        WellKnown,
    }

    /// <summary>
    /// Which <c>RepeatedSerializer</c> factory serves a collection, and how it is shaped.
    /// </summary>
    /// <remarks>
    /// The factories come in two forms: <c>Create{X}&lt;TCollection, TElement&gt;()</c>, which needs
    /// the member's declared type, and <c>Create{X}&lt;TElement&gt;()</c>, where the collection type
    /// is fixed by the factory (arrays, <c>List&lt;T&gt;</c>, and the immutable family).
    /// </remarks>
    internal readonly struct ProtoRepeatedPlan : IEquatable<ProtoRepeatedPlan>
    {
        public ProtoRepeatedPlan(string? factory, bool takesCollectionType, bool isValueType)
        {
            Factory = factory;
            TakesCollectionType = takesCollectionType;
            IsValueType = isValueType;
        }

        /// <summary>e.g. <c>CreateVector</c>, <c>CreateEnumerable</c>, <c>CreateImmutableArray</c>.</summary>
        public string? Factory { get; }

        public bool TakesCollectionType { get; }

        /// <summary>
        /// <c>ImmutableArray&lt;T&gt;</c> is a struct, so neither side null-tests it.
        /// </summary>
        public bool IsValueType { get; }

        public bool Equals(ProtoRepeatedPlan other)
            => Factory == other.Factory && TakesCollectionType == other.TakesCollectionType
                && IsValueType == other.IsValueType;

        public override bool Equals(object? obj) => obj is ProtoRepeatedPlan other && Equals(other);

        public override int GetHashCode()
            => (Factory?.GetHashCode() ?? 0) ^ (TakesCollectionType ? 31 : 0) ^ (IsValueType ? 131 : 0);
    }

    /// <summary>
    /// Which <c>MapSerializer</c> factory serves a dictionary, and what its key and value are.
    /// </summary>
    /// <remarks>
    /// A map is resolved by the same provider walk as any other collection, but it carries two
    /// element types rather than one, so it needs its own plan rather than reusing the member's
    /// <see cref="ProtoMemberPlan.Kind"/> for the element.
    /// </remarks>
    internal readonly struct ProtoMapPlan : IEquatable<ProtoMapPlan>
    {
        public ProtoMapPlan(string factory, bool takesCollectionType,
            ProtoMemberKind keyKind, string keyTypeName,
            ProtoMemberKind valueKind, string valueTypeName, bool isValidProtobufMap)
        {
            Factory = factory;
            TakesCollectionType = takesCollectionType;
            KeyKind = keyKind;
            KeyTypeName = keyTypeName;
            ValueKind = valueKind;
            ValueTypeName = valueTypeName;
            IsValidProtobufMap = isValidProtobufMap;
        }

        /// <summary>e.g. <c>CreateDictionary</c>, <c>CreateImmutableSortedDictionary</c>.</summary>
        public string? Factory { get; }

        /// <summary><c>Create{X}&lt;TCollection, TKey, TValue&gt;()</c> rather than <c>Create{X}&lt;TKey, TValue&gt;()</c>.</summary>
        public bool TakesCollectionType { get; }

        public ProtoMemberKind KeyKind { get; }

        public string? KeyTypeName { get; }

        public ProtoMemberKind ValueKind { get; }

        public string? ValueTypeName { get; }

        /// <summary>
        /// Whether this is expressible as a protobuf <c>map</c>: the key must be an integral, string
        /// or enum type, and the value must not itself be repeated. When it is *not*, protobuf-net
        /// adds <c>OptionFailOnDuplicateKey</c>, so the distinction is visible on the wire path.
        /// </summary>
        public bool IsValidProtobufMap { get; }

        public bool Equals(ProtoMapPlan other)
            => Factory == other.Factory && TakesCollectionType == other.TakesCollectionType
                && KeyKind == other.KeyKind && KeyTypeName == other.KeyTypeName
                && ValueKind == other.ValueKind && ValueTypeName == other.ValueTypeName
                && IsValidProtobufMap == other.IsValidProtobufMap;

        public override bool Equals(object? obj) => obj is ProtoMapPlan other && Equals(other);

        public override int GetHashCode()
            => (Factory?.GetHashCode() ?? 0) ^ (TakesCollectionType ? 31 : 0)
                ^ ((int)KeyKind * 397) ^ (KeyTypeName?.GetHashCode() ?? 0)
                ^ ((int)ValueKind * 131) ^ (ValueTypeName?.GetHashCode() ?? 0)
                ^ (IsValidProtobufMap ? 8191 : 0);
    }

    /// <summary>
    /// One serialized member of a contract.
    /// </summary>
    internal readonly struct ProtoMemberPlan : IEquatable<ProtoMemberPlan>
    {
        public ProtoMemberPlan(int fieldNumber, string name, ProtoMemberKind kind,
            string? typeName = null, string? defaultLiteral = null, bool isNullable = false,
            string? enumTypeName = null, bool messageIsValueType = false, string? declaredTypeName = null,
            ProtoRepeatedPlan repeated = default, string? elementTypeName = null,
            bool isPacked = false, bool overwriteList = false,
            bool wrappedValue = false, bool wrappedValueGroup = false,
            bool wrappedCollection = false, bool wrappedCollectionGroup = false,
            ProtoDataFormat dataFormat = ProtoDataFormat.Default, bool isRequired = false,
            ProtoMapPlan map = default, bool usesAccessor = false, int compatibilityLevel = 200,
            bool isReadOnly = false, string? subSerializer = null,
            string? writeCondition = null, string? specifiedMember = null,
            string? accessorField = null,
            ProtoDataFormat mapKeyFormat = ProtoDataFormat.Default,
            ProtoDataFormat mapValueFormat = ProtoDataFormat.Default,
            bool disableMap = false, bool accessorReads = false)
        {
            AccessorReads = accessorReads;
            MapKeyFormat = mapKeyFormat;
            MapValueFormat = mapValueFormat;
            DisableMap = disableMap;
            AccessorField = accessorField;
            WriteCondition = writeCondition;
            SpecifiedMember = specifiedMember;
            SubSerializer = subSerializer;
            IsReadOnly = isReadOnly;
            CompatibilityLevel = compatibilityLevel;
            UsesAccessor = usesAccessor;
            DataFormat = dataFormat;
            IsRequired = isRequired;
            DeclaredTypeName = declaredTypeName;
            Map = map;
            Repeated = repeated;
            ElementTypeName = elementTypeName;
            IsPacked = isPacked;
            OverwriteList = overwriteList;
            WrappedValue = wrappedValue;
            WrappedValueGroup = wrappedValueGroup;
            WrappedCollection = wrappedCollection;
            WrappedCollectionGroup = wrappedCollectionGroup;
            FieldNumber = fieldNumber;
            Name = name;
            Kind = kind;
            TypeName = typeName;
            DefaultLiteral = defaultLiteral;
            IsNullable = isNullable;
            EnumTypeName = enumTypeName;
            MessageIsValueType = messageIsValueType;
        }

        /// <summary>
        /// For a <see cref="ProtoMemberKind.Message"/>, whether the nested contract is a struct -
        /// in which case it can never be null and neither side tests for it.
        /// </summary>
        public bool MessageIsValueType { get; }

        /// <summary>
        /// The member's own type, fully qualified — needed when a tuple read has to declare a local
        /// for it before the read loop. Null for members that never need one.
        /// </summary>
        public string? DeclaredTypeName { get; }

        /// <summary>
        /// When not <see cref="ProtoRepeatedKind.None"/>, this member is a collection — and
        /// <see cref="Kind"/>, <see cref="TypeName"/> and <see cref="EnumTypeName"/> then describe
        /// the *element*, not the member.
        /// </summary>
        public ProtoRepeatedPlan Repeated { get; }

        /// <summary>
        /// When its factory is set, this member is a dictionary; the plan carries both element types
        /// itself, so <see cref="Kind"/> says nothing useful.
        /// </summary>
        public ProtoMapPlan Map { get; }

        /// <summary>The element's own type, for the <c>RepeatedSerializer</c> type argument.</summary>
        public string? ElementTypeName { get; }

        /// <summary>From <c>[ProtoMember(IsPacked = true)]</c>: omits <c>OptionPackedDisabled</c>.</summary>
        public bool IsPacked { get; }

        /// <summary>From <c>[ProtoMember(OverwriteList = true)]</c>: adds <c>OptionClearCollection</c>.</summary>
        public bool OverwriteList { get; }

        /// <summary>
        /// From <c>[NullWrappedValue]</c>: an extra conceptual message layer around the value, so
        /// that null is expressible. On a collection or map it applies to each element, and adds
        /// <c>OptionWrappedValueFieldPresence</c> so a null is distinguishable from a zero.
        /// </summary>
        public bool WrappedValue { get; }

        /// <summary>From <c>[NullWrappedValue(AsGroup = true)]</c>, which is the v2 <c>SupportNull</c> encoding.</summary>
        public bool WrappedValueGroup { get; }

        /// <summary>
        /// From <c>[NullWrappedCollection]</c>: the same trick applied to the collection itself, so
        /// that a null collection and an empty one are distinguishable. Composes with
        /// <see cref="WrappedValue"/>, since the two apply at different scopes.
        /// </summary>
        public bool WrappedCollection { get; }

        /// <summary>From <c>[NullWrappedCollection(AsGroup = true)]</c>.</summary>
        public bool WrappedCollectionGroup { get; }

        /// <summary>From <c>[ProtoMember(DataFormat = ...)]</c>; selects the wire type.</summary>
        public ProtoDataFormat DataFormat { get; }

        /// <summary>
        /// The setter cannot be called directly from generated C#, so the read goes through an
        /// <c>[UnsafeAccessor]</c> helper. Two things land here: <c>init</c>-only properties, which
        /// C# forbids assigning after construction, and non-public setters.
        /// </summary>
        /// <remarks>
        /// IL has neither restriction, which is why ref-emit's runtime path simply calls the setter.
        /// Its *compiled* path refuses non-public ones — apparently to stay verifiable — and this is
        /// one of the few places we deliberately do better than it rather than matching it.
        /// </remarks>
        public bool UsesAccessor { get; }

        /// <summary>
        /// A property with a getter but no setter. The read runs exactly as it would otherwise —
        /// which is how a getter-only collection or sub-message is populated, since the existing
        /// instance is passed in and mutated — but the result is discarded rather than assigned.
        /// For a scalar that means the value is read and thrown away, which is what ref-emit does.
        /// </summary>
        public bool IsReadOnly { get; }

        /// <summary>
        /// The name of the field behind the property, when it could be identified exactly: the
        /// backing field of an auto-property, or the one a trivial getter returns. When set, the
        /// <c>[UnsafeAccessor]</c> targets the <i>field</i> rather than the setter.
        /// </summary>
        /// <remarks>
        /// This is what makes a getter-only member assignable at all, and it is the better answer
        /// for <c>init</c> and non-public setters too: there is no accessor call, and no reliance
        /// on a setter that may not exist.
        /// </remarks>
        public string? AccessorField { get; }

        /// <summary>
        /// The member cannot be *read* directly either, so the accessor serves both directions. True
        /// only for a non-public field (which <c>ImplicitFields.AllFields</c> takes): a property
        /// reached by backing field still has a public getter, and ref-emit reads through it.
        /// </summary>
        public bool AccessorReads { get; }

        /// <summary>
        /// From <c>[ProtoMap(KeyFormat = …, ValueFormat = …)]</c>: these select the key and value
        /// wire types, which the map serializer takes as arguments separate from the map's own
        /// features. Note the width comes from the *element* type, as it does for a scalar.
        /// </summary>
        public ProtoDataFormat MapKeyFormat { get; }

        /// <summary>See <see cref="MapKeyFormat"/>.</summary>
        public ProtoDataFormat MapValueFormat { get; }

        /// <summary>
        /// From <c>[ProtoMap(DisableMap = true)]</c>: duplicates throw rather than replacing, which
        /// is the same <c>OptionFailOnDuplicateKey</c> an invalid map shape already gets.
        /// </summary>
        /// <remarks>
        /// protobuf-net reads <c>KeyFormat</c>/<c>ValueFormat</c> only when this is <i>not</i> set,
        /// so the two do not compose — see <c>MetaType.ApplyDefaultBehaviour</c>.
        /// </remarks>
        public bool DisableMap { get; }

        /// <summary>
        /// What to pass as the sub-serializer for a message member: normally <c>this</c>, but a
        /// contract with a hand-written serializer needs that one handed over instead.
        /// </summary>
        public string? SubSerializer { get; }

        /// <summary>
        /// The <c>{Name}Specified</c> property or <c>ShouldSerialize{Name}()</c> call that decides
        /// whether to write this member, without the leading instance. It <em>replaces</em> the
        /// trivial-value guard rather than adding to it, and wraps the whole write.
        /// </summary>
        public string? WriteCondition { get; }

        /// <summary>
        /// The <c>{Name}Specified</c> property to set on read, if that is the convention in use;
        /// <c>ShouldSerialize</c> affects the write only.
        /// </summary>
        public string? SpecifiedMember { get; }

        /// <summary>
        /// The resolved compatibility level (200, 240 or 300), already through the
        /// <c>DataFormat.WellKnown</c> promotion. Only the BCL kinds consult it.
        /// </summary>
        public int CompatibilityLevel { get; }

        /// <summary>
        /// From <c>[ProtoMember(IsRequired = true)]</c>: the member is written unconditionally.
        /// Only observable for value-type scalars — reference types were already unguarded on write —
        /// and it does not affect the read at all.
        /// </summary>
        public bool IsRequired { get; }

        public int FieldNumber { get; }

        /// <summary>The C# member name on the contract type.</summary>
        public string Name { get; }

        public ProtoMemberKind Kind { get; }

        /// <summary>
        /// For <see cref="ProtoMemberKind.Message"/>, the fully-qualified type of the nested
        /// contract; null otherwise.
        /// </summary>
        public string? TypeName { get; }

        /// <summary>
        /// The C# literal this member is compared against to decide whether it is worth writing,
        /// from <c>[DefaultValue]</c>; null means "use the type's own default".
        /// </summary>
        public string? DefaultLiteral { get; }

        /// <summary>
        /// A <see cref="System.Nullable{T}"/> of <see cref="Kind"/>; presence, rather than value,
        /// decides whether it is written.
        /// </summary>
        public bool IsNullable { get; }

        /// <summary>
        /// When set, the member is an enum of this type whose wire form is <see cref="Kind"/>, the
        /// underlying scalar; the emitter casts between the two.
        /// </summary>
        public string? EnumTypeName { get; }

        public bool Equals(ProtoMemberPlan other)
            => FieldNumber == other.FieldNumber && Kind == other.Kind
                && Name == other.Name && TypeName == other.TypeName
                && DefaultLiteral == other.DefaultLiteral && IsNullable == other.IsNullable
                && EnumTypeName == other.EnumTypeName && MessageIsValueType == other.MessageIsValueType
                && DeclaredTypeName == other.DeclaredTypeName
                && Repeated.Equals(other.Repeated) && Map.Equals(other.Map)
                && ElementTypeName == other.ElementTypeName
                && IsPacked == other.IsPacked && OverwriteList == other.OverwriteList
                && WrappedValue == other.WrappedValue && WrappedValueGroup == other.WrappedValueGroup
                && WrappedCollection == other.WrappedCollection
                && WrappedCollectionGroup == other.WrappedCollectionGroup
                && DataFormat == other.DataFormat && IsRequired == other.IsRequired
                && UsesAccessor == other.UsesAccessor && CompatibilityLevel == other.CompatibilityLevel
                && IsReadOnly == other.IsReadOnly && SubSerializer == other.SubSerializer
                && AccessorField == other.AccessorField && AccessorReads == other.AccessorReads
                && MapKeyFormat == other.MapKeyFormat && MapValueFormat == other.MapValueFormat
                && DisableMap == other.DisableMap
                && WriteCondition == other.WriteCondition && SpecifiedMember == other.SpecifiedMember;

        public override bool Equals(object? obj) => obj is ProtoMemberPlan other && Equals(other);

        public override int GetHashCode()
            => (FieldNumber * 397) ^ ((int)Kind * 31) ^ Name.GetHashCode()
                ^ (TypeName?.GetHashCode() ?? 0) ^ (DefaultLiteral?.GetHashCode() ?? 0)
                ^ (IsNullable ? 8191 : 0) ^ (EnumTypeName?.GetHashCode() ?? 0);
    }

    /// <summary>
    /// How a contract stores fields it does not recognise.
    /// </summary>
    internal enum ProtoExtensibleKind
    {
        /// <summary>Unknown fields are skipped and lost.</summary>
        None,

        /// <summary><c>IExtensible</c>: one bag for the whole instance.</summary>
        Untyped,

        /// <summary>
        /// <c>ITypedExtensible</c>: a bag per layer, keyed on the declaring type — which is what
        /// makes it work across an inheritance hierarchy, where the same field number can appear at
        /// more than one level with different meanings.
        /// </summary>
        Typed,
    }

    /// <summary>
    /// One <c>[ProtoInclude]</c> link: a directly-derived contract and the field it occupies.
    /// </summary>
    internal readonly struct ProtoSubTypePlan : IEquatable<ProtoSubTypePlan>
    {
        public ProtoSubTypePlan(int fieldNumber, string typeName)
        {
            FieldNumber = fieldNumber;
            TypeName = typeName;
        }

        public int FieldNumber { get; }

        /// <summary>Fully-qualified, <c>global::</c>-prefixed.</summary>
        public string TypeName { get; }

        public bool Equals(ProtoSubTypePlan other)
            => FieldNumber == other.FieldNumber && TypeName == other.TypeName;

        public override bool Equals(object? obj) => obj is ProtoSubTypePlan other && Equals(other);

        public override int GetHashCode() => (FieldNumber * 397) ^ (TypeName?.GetHashCode() ?? 0);
    }

    /// <summary>
    /// One contract type that the model can serialize.
    /// </summary>
    /// <summary>
    /// The four serialization callback points, in the order they fire.
    /// </summary>
    /// <remarks>
    /// protobuf-net's own <c>[ProtoBeforeSerialization]</c> family and the
    /// <c>System.Runtime.Serialization</c> <c>[OnSerializing]</c> family map onto the same four
    /// points; <c>MetaType</c> honours them identically.
    /// </remarks>
    internal enum ProtoCallbackKind
    {
        BeforeSerialize,
        AfterSerialize,
        BeforeDeserialize,
        AfterDeserialize,
    }

    /// <summary>
    /// A serialization callback: the method to call, and whether it takes a <c>StreamingContext</c>.
    /// </summary>
    internal readonly struct ProtoCallbackPlan : IEquatable<ProtoCallbackPlan>
    {
        public ProtoCallbackPlan(string methodName, bool takesContext)
        {
            MethodName = methodName;
            TakesContext = takesContext;
        }

        public string? MethodName { get; }

        /// <summary>
        /// The <c>System.Runtime.Serialization</c> spelling takes one, supplied as
        /// <c>SerializationContext.AsStreamingContext(state.Context)</c>.
        /// </summary>
        public bool TakesContext { get; }

        public bool Equals(ProtoCallbackPlan other)
            => MethodName == other.MethodName && TakesContext == other.TakesContext;

        public override bool Equals(object? obj) => obj is ProtoCallbackPlan other && Equals(other);

        public override int GetHashCode() => (MethodName?.GetHashCode() ?? 0) ^ (TakesContext ? 31 : 0);
    }

    /// <summary>
    /// An enum that is a contract in its own right: the type name plus the scalar kind of its
    /// underlying type, which is all <c>EnumSerializer.Create{X}</c> needs.
    /// </summary>
    internal readonly struct ProtoEnumPlan : IEquatable<ProtoEnumPlan>
    {
        public ProtoEnumPlan(string typeName, ProtoMemberKind underlying)
        {
            TypeName = typeName;
            Underlying = underlying;
        }

        public string? TypeName { get; }

        public ProtoMemberKind Underlying { get; }

        public bool Equals(ProtoEnumPlan other)
            => TypeName == other.TypeName && Underlying == other.Underlying;

        public override bool Equals(object? obj) => obj is ProtoEnumPlan other && Equals(other);

        public override int GetHashCode() => (TypeName?.GetHashCode() ?? 0) ^ ((int)Underlying * 397);
    }

    internal sealed class ProtoContractPlan : IEquatable<ProtoContractPlan>
    {
        public ProtoContractPlan(string typeName, EquatableArray<ProtoMemberPlan> members,
            bool isValueType = false, bool skipConstructor = false, bool isTuple = false,
            bool isTupleLiteral = false, bool isSealed = false,
            string? rootTypeName = null, EquatableArray<ProtoSubTypePlan> subTypes = default,
            ProtoExtensibleKind extensible = ProtoExtensibleKind.None, string? surrogateTypeName = null,
            string? toSurrogate = null, string? toUnderlying = null,
            string? externalSerializerTypeName = null, string? surrogateSerializer = null,
            bool usesConstructorAccessor = false, EquatableArray<ProtoCallbackPlan> callbacks = default)
        {
            Callbacks = callbacks;
            UsesConstructorAccessor = usesConstructorAccessor;
            ExternalSerializerTypeName = externalSerializerTypeName;
            SurrogateSerializer = surrogateSerializer;
            SurrogateTypeName = surrogateTypeName;
            ToSurrogate = toSurrogate;
            ToUnderlying = toUnderlying;
            TypeName = typeName;
            Members = members;
            Extensible = extensible;
            RootTypeName = rootTypeName;
            SubTypes = subTypes;
            IsSealed = isSealed;
            IsValueType = isValueType;
            SkipConstructor = skipConstructor;
            IsTuple = isTuple;
            IsTupleLiteral = isTupleLiteral;
        }

        /// <summary>
        /// A C# tuple type, whose name renders as <c>(int, string)</c> — so it has to be built with
        /// a tuple literal, since <c>new (int, string)(...)</c> is not legal C#.
        /// </summary>
        public bool IsTupleLiteral { get; }

        /// <summary>
        /// An "auto-tuple": members are reconstructed through a constructor at the end of the read
        /// rather than assigned, and every member is written unconditionally.
        /// </summary>
        /// <remarks>
        /// Members are ordered by constructor parameter, and their field numbers are 1..n in that
        /// same order, so the emitter can pass the locals straight through in member order.
        /// </remarks>
        public bool IsTuple { get; }

        /// <summary>
        /// From <c>[ProtoContract(SkipConstructor = true)]</c>: instances are created without running
        /// any constructor, and the serializer additionally acts as an <c>IFactory&lt;T&gt;</c>.
        /// </summary>
        public bool SkipConstructor { get; }

        /// <summary>
        /// The parameterless constructor exists but is not public, so it is reached through
        /// <c>[UnsafeAccessor]</c> rather than <c>new</c>.
        /// </summary>
        /// <remarks>
        /// This matches <c>RuntimeTypeModel</c>, which calls it by reflection. Ref-emit's *compiled*
        /// path refuses it outright — "Non-public member cannot be used with full dll compilation" —
        /// the same split as a non-public setter, and resolved the same way.
        /// </remarks>
        public bool UsesConstructorAccessor { get; }

        /// <summary>
        /// The serialization callbacks, indexed by <see cref="ProtoCallbackKind"/>; an entry with a
        /// null <c>MethodName</c> means that point has no callback.
        /// </summary>
        public EquatableArray<ProtoCallbackPlan> Callbacks { get; }

        /// <summary>
        /// A struct contract: it needs no construction or null test on read, and cannot have
        /// sub-types, so the <c>ThrowUnexpectedSubtype</c> guard does not apply.
        /// </summary>
        public bool IsValueType { get; }

        /// <summary>
        /// A sealed contract cannot have sub-types either, so ref-emit omits the
        /// <c>ThrowUnexpectedSubtype</c> guard for it just as it does for a struct.
        /// </summary>
        public bool IsSealed { get; }

        /// <summary>
        /// The top of this contract's <c>[ProtoInclude]</c> hierarchy, or null when it is not in one.
        /// </summary>
        /// <remarks>
        /// Every type in a hierarchy reads and writes through the <em>root's</em>
        /// <c>ISubTypeSerializer</c>, which is what threads the base type's members and the sub-type
        /// marker onto the wire. A contract is only in a hierarchy if the link is declared: a derived
        /// contract its base does not <c>[ProtoInclude]</c> is an independent contract that silently
        /// ignores its inherited members, which is ref-emit's behaviour too.
        /// </remarks>
        public string? RootTypeName { get; }

        /// <summary>The directly-derived contracts, in declaration order.</summary>
        public EquatableArray<ProtoSubTypePlan> SubTypes { get; }

        /// <summary>
        /// Whether unrecognised fields are kept, and in which shape: the read stores them instead of
        /// skipping, and the write appends them after every declared member.
        /// </summary>
        public ProtoExtensibleKind Extensible { get; }

        /// <summary>
        /// From <c>[ProtoContract(Surrogate = …)]</c>: the type that actually carries the wire shape.
        /// </summary>
        /// <remarks>
        /// The members on this plan are then the <em>surrogate's</em>, and the serializer is its body
        /// with a conversion at each end — which is exactly what ref-emit emits. Nothing changes for
        /// a member whose type is surrogated: it stays an ordinary sub-message.
        /// </remarks>
        /// <summary>
        /// From <c>[ProtoContract(Serializer = …)]</c>: the contract is served by a hand-written
        /// serializer, so we emit no body for it at all — the services type implements
        /// <c>ISerializerProxy&lt;T&gt;</c> and hands out that serializer instead.
        /// </summary>
        public string? ExternalSerializerTypeName { get; }

        public string? SurrogateTypeName { get; }

        /// <summary>
        /// When the surrogate has a serializer of its own, the expression yielding it: the body then
        /// <em>delegates</em> to that after converting, rather than inlining the surrogate's members
        /// (of which there may be none). This is how a well-known type serves as a surrogate.
        /// </summary>
        public string? SurrogateSerializer { get; }

        /// <summary>
        /// The fully-qualified static method converting the type to its surrogate, when the pairing
        /// names one; null means a plain cast, which is what an operator-based surrogate uses.
        /// </summary>
        public string? ToSurrogate { get; }

        /// <summary>The converse; see <see cref="ToSurrogate"/>.</summary>
        public string? ToUnderlying { get; }

        /// <summary>Fully-qualified, <c>global::</c>-prefixed type name.</summary>
        public string TypeName { get; }

        public EquatableArray<ProtoMemberPlan> Members { get; }

        public bool Equals(ProtoContractPlan? other)
            => other is not null && TypeName == other.TypeName && Members.Equals(other.Members)
                && IsValueType == other.IsValueType && SkipConstructor == other.SkipConstructor
                && UsesConstructorAccessor == other.UsesConstructorAccessor
                && Callbacks.Equals(other.Callbacks)
                && IsTuple == other.IsTuple && IsTupleLiteral == other.IsTupleLiteral
                && IsSealed == other.IsSealed && RootTypeName == other.RootTypeName
                && SubTypes.Equals(other.SubTypes) && Extensible == other.Extensible
                && SurrogateTypeName == other.SurrogateTypeName
                && ToSurrogate == other.ToSurrogate && ToUnderlying == other.ToUnderlying
                && ExternalSerializerTypeName == other.ExternalSerializerTypeName
                && SurrogateSerializer == other.SurrogateSerializer;

        public override bool Equals(object? obj) => Equals(obj as ProtoContractPlan);

        public override int GetHashCode()
            => (TypeName.GetHashCode() * 397) ^ Members.GetHashCode() ^ (IsValueType ? 4093 : 0);
    }

    /// <summary>
    /// A user-declared <c>[ProtoModel]</c> type and everything it serializes.
    /// </summary>
    internal sealed class ProtoModelPlan : IEquatable<ProtoModelPlan>
    {
        public ProtoModelPlan(string? nameSpace, string typeName, EquatableArray<ProtoContractPlan> contracts,
            bool annotateTrimming = false, EquatableArray<ProtoEnumPlan> enums = default)
        {
            Namespace = nameSpace;
            TypeName = typeName;
            Contracts = contracts;
            AnnotateTrimming = annotateTrimming;
            Enums = enums;
        }

        /// <summary>
        /// Enums seeded directly by <c>[ProtoSerializable]</c>, which are served by the same
        /// <c>ISerializerProxy&lt;TEnum&gt;</c> a repeated enum member uses.
        /// </summary>
        /// <remarks>
        /// <c>[ProtoContract]</c>'s own <c>AttributeUsage</c> allows enums, and ref-emit's model
        /// serializes an enum root happily — it emits exactly these proxies and no
        /// <c>ISerializer&lt;TEnum&gt;</c> body, since <c>EnumSerializer</c> is the serializer.
        /// </remarks>
        public EquatableArray<ProtoEnumPlan> Enums { get; }

        /// <summary>
        /// Whether <c>[DynamicallyAccessedMembers]</c> is available to the consumer, and so whether
        /// the <c>GetSerializer&lt;T&gt;</c> override can restate the base's annotation.
        /// </summary>
        /// <remarks>
        /// Without it a native-AOT build reports IL2095: an override must repeat the annotation
        /// exactly. protobuf-net's own <c>DynamicAccess.ContractType</c> is internal, so the flags
        /// have to be spelled out; and the attribute itself only exists on net5+, hence the probe.
        /// </remarks>
        public bool AnnotateTrimming { get; }

        /// <summary>Null for the global namespace.</summary>
        public string? Namespace { get; }

        /// <summary>The simple name of the user's partial model class.</summary>
        public string TypeName { get; }

        public EquatableArray<ProtoContractPlan> Contracts { get; }

        public string HintName
            => (Namespace is null ? TypeName : Namespace + "." + TypeName) + ".ProtoModel.g.cs";

        public bool Equals(ProtoModelPlan? other)
            => other is not null && Namespace == other.Namespace && TypeName == other.TypeName
                && Contracts.Equals(other.Contracts) && AnnotateTrimming == other.AnnotateTrimming;

        public override bool Equals(object? obj) => Equals(obj as ProtoModelPlan);

        public override int GetHashCode()
            => ((Namespace?.GetHashCode() ?? 0) * 397) ^ (TypeName.GetHashCode() * 31) ^ Contracts.GetHashCode();
    }
}
