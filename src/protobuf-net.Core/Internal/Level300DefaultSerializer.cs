using ProtoBuf.Serializers;
using System;

namespace ProtoBuf.Internal
{
    internal class Level300DefaultSerializer : Level240DefaultSerializer,
        ISerializer<decimal>, ISerializer<decimal?>, IValueChecker<decimal>,
        ISerializer<Guid>, ISerializer<Guid?>, IValueChecker<Guid>
    {
        bool IValueChecker<decimal>.HasNonTrivialValue(decimal value) => !value.Equals(decimal.Zero);
        bool IValueChecker<decimal>.IsNull(decimal value) => false;
        bool IValueChecker<Guid>.HasNonTrivialValue(Guid value) => !value.Equals(Guid.Empty);
        bool IValueChecker<Guid>.IsNull(Guid value) => false;

        SerializerFeatures ISerializer<Guid>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;
        SerializerFeatures ISerializer<Guid?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

        Guid ISerializer<Guid>.Read(ref ProtoReader.State state, Guid value)
            => GuidHelper.Read(ref state);

        void ISerializer<Guid>.Write(ref ProtoWriter.State state, Guid value)
            => GuidHelper.Write(ref state, value, false);

        SerializerFeatures ISerializer<decimal>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;
        SerializerFeatures ISerializer<decimal?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryScalar;

        decimal ISerializer<decimal>.Read(ref ProtoReader.State state, decimal value)
            => BclHelpers.ReadDecimalString(ref state);

        void ISerializer<decimal>.Write(ref ProtoWriter.State state, decimal value)
            => BclHelpers.WriteDecimalString(ref state, value);

        decimal? ISerializer<decimal?>.Read(ref ProtoReader.State state, decimal? value)
            => ((ISerializer<decimal>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<decimal?>.Write(ref ProtoWriter.State state, decimal? value)
            => ((ISerializer<decimal>)this).Write(ref state, value.Value);

        Guid? ISerializer<Guid?>.Read(ref ProtoReader.State state, Guid? value)
            => ((ISerializer<Guid>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<Guid?>.Write(ref ProtoWriter.State state, Guid? value)
            => ((ISerializer<Guid>)this).Write(ref state, value.Value);
    }
}
