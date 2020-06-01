using ProtoBuf.Serializers;
using System;

namespace ProtoBuf.Internal
{
    internal class Level240DefaultSerializer :
        ISerializer<DateTime>, ISerializer<DateTime?>,
        ISerializer<TimeSpan>, ISerializer<TimeSpan?>
    {
        DateTime? ISerializer<DateTime?>.Read(ref ProtoReader.State state, DateTime? value)
            => ((ISerializer<DateTime>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<DateTime?>.Write(ref ProtoWriter.State state, DateTime? value)
            => ((ISerializer<DateTime>)this).Write(ref state, value.Value);

        TimeSpan? ISerializer<TimeSpan?>.Read(ref ProtoReader.State state, TimeSpan? value)
            => ((ISerializer<TimeSpan>)this).Read(ref state, value.GetValueOrDefault());
        void ISerializer<TimeSpan?>.Write(ref ProtoWriter.State state, TimeSpan? value)
            => ((ISerializer<TimeSpan>)this).Write(ref state, value.Value);

        DateTime ISerializer<DateTime>.Read(ref ProtoReader.State state, DateTime value)
            => PrimaryTypeProvider.ReadTimestamp(ref state, value);

        void ISerializer<DateTime>.Write(ref ProtoWriter.State state, DateTime value)
            => PrimaryTypeProvider.WriteTimestamp(ref state, value);

        TimeSpan ISerializer<TimeSpan>.Read(ref ProtoReader.State state, TimeSpan value)
            => PrimaryTypeProvider.ReadDuration(ref state, value);

        void ISerializer<TimeSpan>.Write(ref ProtoWriter.State state, TimeSpan value)
            => PrimaryTypeProvider.WriteDuration(ref state, value);

        SerializerFeatures ISerializer<TimeSpan>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
        SerializerFeatures ISerializer<TimeSpan?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
        SerializerFeatures ISerializer<DateTime>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
        SerializerFeatures ISerializer<DateTime?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
    }
}
