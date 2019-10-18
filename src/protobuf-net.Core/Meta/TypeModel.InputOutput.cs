using System;
using System.IO;

namespace ProtoBuf.Meta
{
    partial class TypeModel :
        IProtoInput<Stream>,
        IProtoInput<ArraySegment<byte>>,
        IProtoInput<byte[]>,
        IProtoOutput<Stream>
    {
        T IProtoInput<Stream>.Deserialize<T>(Stream source, T value, object userState)
            => Deserialize<T>(source, value, userState);

        void IProtoOutput<Stream>.Serialize<T>(Stream destination, T value, object userState)
            => Serialize<T>(destination, value, userState);

        T IProtoInput<ArraySegment<byte>>.Deserialize<T>(ArraySegment<byte> source, T value, object userState)
            => Deserialize<T>(new ReadOnlyMemory<byte>(source.Array, source.Offset, source.Count), value, userState);

        T IProtoInput<byte[]>.Deserialize<T>(byte[] source, T value, object userState)
            => Deserialize<T>(new ReadOnlyMemory<byte>(source), value, userState);
    }
}
