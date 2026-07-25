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
            ProtoDataFormat dataFormat = ProtoDataFormat.Default, bool isRequired = false,
            ProtoMapPlan map = default)
        {
            DataFormat = dataFormat;
            IsRequired = isRequired;
            DeclaredTypeName = declaredTypeName;
            Map = map;
            Repeated = repeated;
            ElementTypeName = elementTypeName;
            IsPacked = isPacked;
            OverwriteList = overwriteList;
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

        /// <summary>From <c>[ProtoMember(DataFormat = ...)]</c>; selects the wire type.</summary>
        public ProtoDataFormat DataFormat { get; }

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
                && DataFormat == other.DataFormat && IsRequired == other.IsRequired;

        public override bool Equals(object? obj) => obj is ProtoMemberPlan other && Equals(other);

        public override int GetHashCode()
            => (FieldNumber * 397) ^ ((int)Kind * 31) ^ Name.GetHashCode()
                ^ (TypeName?.GetHashCode() ?? 0) ^ (DefaultLiteral?.GetHashCode() ?? 0)
                ^ (IsNullable ? 8191 : 0) ^ (EnumTypeName?.GetHashCode() ?? 0);
    }

    /// <summary>
    /// One contract type that the model can serialize.
    /// </summary>
    internal sealed class ProtoContractPlan : IEquatable<ProtoContractPlan>
    {
        public ProtoContractPlan(string typeName, EquatableArray<ProtoMemberPlan> members,
            bool isValueType = false, bool skipConstructor = false, bool isTuple = false,
            bool isTupleLiteral = false)
        {
            TypeName = typeName;
            Members = members;
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
        /// A struct contract: it needs no construction or null test on read, and cannot have
        /// sub-types, so the <c>ThrowUnexpectedSubtype</c> guard does not apply.
        /// </summary>
        public bool IsValueType { get; }

        /// <summary>Fully-qualified, <c>global::</c>-prefixed type name.</summary>
        public string TypeName { get; }

        public EquatableArray<ProtoMemberPlan> Members { get; }

        public bool Equals(ProtoContractPlan? other)
            => other is not null && TypeName == other.TypeName && Members.Equals(other.Members)
                && IsValueType == other.IsValueType && SkipConstructor == other.SkipConstructor
                && IsTuple == other.IsTuple && IsTupleLiteral == other.IsTupleLiteral;

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
            bool annotateTrimming = false)
        {
            Namespace = nameSpace;
            TypeName = typeName;
            Contracts = contracts;
            AnnotateTrimming = annotateTrimming;
        }

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
