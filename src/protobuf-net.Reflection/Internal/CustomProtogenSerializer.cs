using Google.Protobuf.Reflection;
using ProtoBuf.Meta;

#pragma warning disable PBN9001 // the [ProtoModel] trigger attributes are [Experimental]

namespace ProtoBuf.Reflection.Internal
{
    // The other half of this class - CustomProtogenSerializer.Generated.cs - is the output of
    // protobuf-net's own [ProtoModel] source generator over the seeds declared here, committed
    // as source so that projects which compile these files in directly (protobuf-net.BuildTools)
    // get a working serializer without running the generator. To regenerate: build this class in
    // a scratch project with protobuf-net.BuildTools attached as an analyzer and
    // <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>, then copy the emitted
    // *.ProtoModel.g.cs over the .Generated.cs file. The runtime-model equivalent is pinned by
    // EmitProtogenModel.CustomProtogenSerializer in protobuf-net.Test, and the protoc comparison
    // suite gates the bytes.
    [ProtoModel]
    [ProtoSerializable(typeof(FileDescriptorSet))]
    [ProtoSerializable(typeof(Access))]
    [ProtoSerializable(typeof(ProtogenFileOptions))]
    [ProtoSerializable(typeof(ProtogenMessageOptions))]
    [ProtoSerializable(typeof(ProtogenFieldOptions))]
    [ProtoSerializable(typeof(ProtogenEnumOptions))]
    [ProtoSerializable(typeof(ProtogenEnumValueOptions))]
    [ProtoSerializable(typeof(ProtogenServiceOptions))]
    [ProtoSerializable(typeof(ProtogenMethodOptions))]
    [ProtoSerializable(typeof(ProtogenOneofOptions))]
    internal sealed partial class CustomProtogenSerializer : TypeModel
    {
    }
}
