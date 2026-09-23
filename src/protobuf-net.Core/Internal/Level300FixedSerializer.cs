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
        // the nullable forwarder is only reached when the value is PRESENT, so `!` asserts rather
        // than hides: GetValueOrDefault() here would silently write a zero. See the long-form note in
        // Internal/PrimaryTypeProvider.Primitives.cs.
        void ISerializer<Guid?>.Write(ref ProtoWriter.State state, Guid? value)
            => ((ISerializer<Guid>)this).Write(ref state, value!.Value);

        bool IValueChecker<Guid>.HasNonTrivialValue(Guid value) => !value.Equals(Guid.Empty);
        bool IValueChecker<Guid>.IsNull(Guid value) => false;
    }
}
