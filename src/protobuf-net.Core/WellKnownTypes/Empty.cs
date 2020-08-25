using ProtoBuf.Internal;
using ProtoBuf.Serializers;
using ProtoBuf.WellKnownTypes;
using System.Runtime.InteropServices;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider : ISerializer<Empty>, ISerializer<Empty?>
    {
        SerializerFeatures ISerializer<Empty>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
        SerializerFeatures ISerializer<Empty?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
        Empty ISerializer<Empty>.Read(ref ProtoReader.State state, Empty value)
        {
            state.SkipAllFields();
            return value;
        }

        void ISerializer<Empty>.Write(ref ProtoWriter.State state, Empty value) { }

        Empty? ISerializer<Empty?>.Read(ref ProtoReader.State state, Empty? value)
            => ((ISerializer<Empty>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<Empty?>.Write(ref ProtoWriter.State state, Empty? value)
            => ((ISerializer<Empty>)this).Write(ref state, value.Value);
    }
}

namespace ProtoBuf.WellKnownTypes
{
    /// <summary>
    /// A generic empty message that you can re-use to avoid defining duplicated empty messages in your APIs
    /// </summary>
    [ProtoContract(Name = ".google.protobuf.Empty", Serializer = typeof(PrimaryTypeProvider), Origin = "google/protobuf/empty.proto")]
    [StructLayout(LayoutKind.Explicit, Size = 1)]
    public readonly struct Empty
    {
    }
}