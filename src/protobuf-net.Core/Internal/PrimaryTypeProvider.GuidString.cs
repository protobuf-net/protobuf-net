using ProtoBuf.Serializers;
using System;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider : IMeasuringSerializer<GuidBytes>, IValueChecker<GuidBytes>
    {
        SerializerFeatures ISerializer<GuidBytes>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

        bool IValueChecker<GuidBytes>.HasNonTrivialValue(GuidBytes value) => !value.Value.Equals(Guid.Empty);

        bool IValueChecker<GuidBytes>.IsNull(GuidBytes value) => false;

        int IMeasuringSerializer<GuidBytes>.Measure(ISerializationContext context, WireType wireType, GuidBytes value) => GuidBytes.LENGTH;

        unsafe GuidBytes ISerializer<GuidBytes>.Read(ref ProtoReader.State state, GuidBytes value)
            => GuidBytes.Read(ref state);

        void ISerializer<GuidBytes>.Write(ref ProtoWriter.State state, GuidBytes value)
            => value.Write(ref state);
    }
}
