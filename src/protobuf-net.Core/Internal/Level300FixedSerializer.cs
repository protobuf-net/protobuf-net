using ProtoBuf.Serializers;
using System;

namespace ProtoBuf.Internal
{
    internal sealed class Level300FixedSerializer : ISerializer<Guid>, ISerializer<Guid?>, IValueChecker<Guid>
    {
        Guid ISerializer<Guid>.Read(ref ProtoReader.State state, Guid value)
            => GuidHelper.Read(ref state);

        void ISerializer<Guid>.Write(ref ProtoWriter.State state, Guid value)
            => GuidHelper.Write(ref state, value, true);

        SerializerFeatures ISerializer<Guid>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;
        SerializerFeatures ISerializer<Guid?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

        Guid? ISerializer<Guid?>.Read(ref ProtoReader.State state, Guid? value)
            => ((ISerializer<Guid>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<Guid?>.Write(ref ProtoWriter.State state, Guid? value)
            => ((ISerializer<Guid>)this).Write(ref state, value.Value);

        bool IValueChecker<Guid>.HasNonTrivialValue(Guid value) => !value.Equals(Guid.Empty);
        bool IValueChecker<Guid>.IsNull(Guid value) => false;
    }
}
