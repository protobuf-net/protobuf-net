
using ProtoBuf;
using ProtoBuf.Meta;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("protobuf-net.Test, PublicKey="
    + "002400000480000094000000060200000024000052534131000400000100010009ed9caa457bfc"
    + "205716c3d4e8b255a63ddf71c9e53b1b5f574ab6ffdba11e80ab4b50be9c46d43b75206280070d"
    + "dba67bd4c830f93f0317504a76ba6a48243c36d2590695991164592767a7bbc4453b34694e31e2"
    + "0815a096e4483605139a32a76ec2fef196507487329c12047bf6a68bca8ee9354155f4d01daf6e"
    + "ec5ff6bc")]

[assembly: TypeForwardedTo(typeof(TypeModel))]
[assembly: TypeForwardedTo(typeof(ProtoReader))]
[assembly: TypeForwardedTo(typeof(ProtoWriter))]
[assembly: TypeForwardedTo(typeof(SerializationContext))]
[assembly: TypeForwardedTo(typeof(SubItemToken))]
[assembly: TypeForwardedTo(typeof(WireType))]
[assembly: TypeForwardedTo(typeof(PrefixStyle))]
[assembly: TypeForwardedTo(typeof(IExtensible))]
[assembly: TypeForwardedTo(typeof(IExtension))]
[assembly: TypeForwardedTo(typeof(IExtensionResettable))]
[assembly: TypeForwardedTo(typeof(TypeFormatEventArgs))]
[assembly: TypeForwardedTo(typeof(TypeFormatEventHandler))]
[assembly: TypeForwardedTo(typeof(ProtoTypeCode))]
[assembly: TypeForwardedTo(typeof(DataFormat))]
[assembly: TypeForwardedTo(typeof(ProtoException))]
[assembly: TypeForwardedTo(typeof(DiscriminatedUnion128))]
[assembly: TypeForwardedTo(typeof(DiscriminatedUnion128Object))]
[assembly: TypeForwardedTo(typeof(DiscriminatedUnion32))]
[assembly: TypeForwardedTo(typeof(DiscriminatedUnion32Object))]
[assembly: TypeForwardedTo(typeof(DiscriminatedUnion64))]
[assembly: TypeForwardedTo(typeof(DiscriminatedUnion64Object))]
[assembly: TypeForwardedTo(typeof(DiscriminatedUnionObject))]
[assembly: TypeForwardedTo(typeof(BclHelpers))]
[assembly: TypeForwardedTo(typeof(TimeSpanScale))]
[assembly: TypeForwardedTo(typeof(BufferExtension))]
[assembly: TypeForwardedTo(typeof(Extensible))]

[assembly: TypeForwardedTo(typeof(ProtoBeforeDeserializationAttribute))]
[assembly: TypeForwardedTo(typeof(ProtoBeforeSerializationAttribute))]
[assembly: TypeForwardedTo(typeof(ProtoAfterDeserializationAttribute))]
[assembly: TypeForwardedTo(typeof(ProtoAfterSerializationAttribute))]
[assembly: TypeForwardedTo(typeof(ProtoContractAttribute))]
[assembly: TypeForwardedTo(typeof(ProtoEnumAttribute))]
[assembly: TypeForwardedTo(typeof(ProtoConverterAttribute))]
[assembly: TypeForwardedTo(typeof(ProtoIncludeAttribute))]
[assembly: TypeForwardedTo(typeof(ProtoIgnoreAttribute))]
[assembly: TypeForwardedTo(typeof(ProtoMapAttribute))]
[assembly: TypeForwardedTo(typeof(ProtoMemberAttribute))]
[assembly: TypeForwardedTo(typeof(MemberSerializationOptions))]
[assembly: TypeForwardedTo(typeof(ProtoPartialMemberAttribute))]
[assembly: TypeForwardedTo(typeof(ProtoPartialIgnoreAttribute))]
[assembly: TypeForwardedTo(typeof(ImplicitFields))]
[assembly: TypeForwardedTo(typeof(IProtoInput<>))]
[assembly: TypeForwardedTo(typeof(IProtoOutput<>))]
[assembly: TypeForwardedTo(typeof(IMeasuredProtoOutput<>))]
[assembly: TypeForwardedTo(typeof(MeasureState<>))]
[assembly: TypeForwardedTo(typeof(CompatibilityLevel))]
[assembly: TypeForwardedTo(typeof(CompatibilityLevelAttribute))]
[assembly: TypeForwardedTo(typeof(ProtoSyntax))]

#if !NETSTANDARD2_0_OR_GREATER // see #1214
[module: SkipLocalsInit]
#endif