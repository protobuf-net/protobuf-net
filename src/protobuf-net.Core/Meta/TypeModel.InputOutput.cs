using System;
using System.Buffers;
using System.IO;

namespace ProtoBuf.Meta
{
    partial class TypeModel :
        IProtoInput<Stream>,
        IProtoInput<ArraySegment<byte>>,
        IProtoInput<byte[]>,
        IProtoInput<ReadOnlyMemory<byte>>,
        IProtoInput<ReadOnlySequence<byte>>,
        IProtoOutput<Stream>,
        IProtoOutput<IBufferWriter<byte>>,
        IMeasuredProtoOutput<Stream>,
        IMeasuredProtoOutput<IBufferWriter<byte>>
    {
        T IProtoInput<Stream>.Deserialize<T>(Stream source, T value, object userState)
            => Deserialize<T>(source, value, userState);

        T IProtoInput<ArraySegment<byte>>.Deserialize<T>(ArraySegment<byte> source, T value, object userState)
            => Deserialize<T>(new ReadOnlyMemory<byte>(source.Array, source.Offset, source.Count), value, userState);

        T IProtoInput<byte[]>.Deserialize<T>(byte[] source, T value, object userState)
            => Deserialize<T>(new ReadOnlyMemory<byte>(source), value, userState);

        void IProtoOutput<Stream>.Serialize<T>(Stream destination, T value, object userState)
            => Serialize<T>(destination, value, userState);

        void IProtoOutput<IBufferWriter<byte>>.Serialize<T>(IBufferWriter<byte> destination, T value, object userState)
            => Serialize<T>(destination, value, userState);

        void IMeasuredProtoOutput<Stream>.Serialize<T>(MeasureState<T> measured, Stream destination)
            => measured.Serialize(destination);

        void IMeasuredProtoOutput<IBufferWriter<byte>>.Serialize<T>(MeasureState<T> measured, IBufferWriter<byte> destination)
            => measured.Serialize(destination);

        MeasureState<T> IMeasuredProtoOutput<Stream>.Measure<T>(T value, object userState)
            => Measure<T>(value, userState);

        MeasureState<T> IMeasuredProtoOutput<IBufferWriter<byte>>.Measure<T>(T value, object userState)
            => Measure<T>(value, userState);
    }
}
