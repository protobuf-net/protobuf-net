using ProtoBuf.Meta;

namespace ProtoBuf.Reflection.Internal
{
    // The compile-time model for the descriptor DTO tree, populated by the protobuf-net source
    // generator: the seed closes over FileDescriptorSet, which reaches every descriptor and
    // options type protogen touches. This replaces a 2,953-line hand-maintained services file
    // that was ref-emit output "exported and tweaked" once and frozen - meaning no fix made
    // since the export ever reached it; the generated half (tracked under Generated/, emitted
    // on every build so drift is a visible diff) regenerates with the generator instead.
    //
    // The Generated/ output is deliberately COMMITTED: protobuf-net.BuildTools compiles this
    // project's sources in rather than referencing the assembly, so it needs both halves of
    // the partial on disk - the same reason the trigger attributes are matched by full name.
#pragma warning disable PBN9001 // experimental: this is the experiment
    [ProtoModel]
    [ProtoSerializable(typeof(global::Google.Protobuf.Reflection.FileDescriptorSet))]
    // the protobuf-net.proto option-extension DTOs are NOT reachable from the descriptor
    // closure - they are decoded FROM extension bytes (Extensible.GetValue against this
    // model), so each is a root in its own right; missing one fails only when a schema
    // uses that option, which is exactly how DetectSpecialMessageKind caught the first cut
    [ProtoSerializable(typeof(global::ProtoBuf.Reflection.ProtogenFileOptions))]
    [ProtoSerializable(typeof(global::ProtoBuf.Reflection.ProtogenMessageOptions))]
    [ProtoSerializable(typeof(global::ProtoBuf.Reflection.ProtogenFieldOptions))]
    [ProtoSerializable(typeof(global::ProtoBuf.Reflection.ProtogenEnumOptions))]
    [ProtoSerializable(typeof(global::ProtoBuf.Reflection.ProtogenEnumValueOptions))]
    [ProtoSerializable(typeof(global::ProtoBuf.Reflection.ProtogenServiceOptions))]
    [ProtoSerializable(typeof(global::ProtoBuf.Reflection.ProtogenMethodOptions))]
    [ProtoSerializable(typeof(global::ProtoBuf.Reflection.ProtogenOneofOptions))]
    // the constructor and Instance are the generator's to provide: it emits a private
    // parameterless constructor (the class is sealed) and the shared Instance accessor,
    // exactly as it would for any consumer's model
    internal sealed partial class CustomProtogenSerializer : TypeModel
    {
    }
#pragma warning restore PBN9001
}
