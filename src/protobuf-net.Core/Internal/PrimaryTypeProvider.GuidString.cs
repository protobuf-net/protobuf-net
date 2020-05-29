using ProtoBuf.Serializers;
using System;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider : IMeasuringSerializer<GuidString>, IValueChecker<GuidString>
    {
        SerializerFeatures ISerializer<GuidString>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

        bool IValueChecker<GuidString>.HasNonTrivialValue(GuidString value) => !value.Value.Equals(Guid.Empty);

        bool IValueChecker<GuidString>.IsNull(GuidString value) => false;

        int IMeasuringSerializer<GuidString>.Measure(ISerializationContext context, WireType wireType, GuidString value) => GuidString.WRITE_LENGTH;

        unsafe GuidString ISerializer<GuidString>.Read(ref ProtoReader.State state, GuidString value)
            => GuidString.Read(ref state);

        void ISerializer<GuidString>.Write(ref ProtoWriter.State state, GuidString value)
            => value.Write(ref state);
    }
}
