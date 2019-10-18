using System.IO;

namespace ProtoBuf.Meta
{
    partial class TypeModel : IProtoInput<Stream>, IProtoOutput<Stream>
    {
        T IProtoInput<Stream>.Deserialize<T>(Stream source, T value, object userState)
            => Deserialize<T>(source, value, userState);

        void IProtoOutput<Stream>.Serialize<T>(Stream destination, T value, object userState)
            => Serialize<T>(destination, value, userState);
    }
}
