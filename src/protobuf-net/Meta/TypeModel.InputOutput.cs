using System.IO;

namespace ProtoBuf.Meta
{
    partial class TypeModel : IProtoInput<Stream>, IProtoOutput<Stream>
    {
        static SerializationContext CreateContext(object userState)
        {
            if (userState == null)
                return SerializationContext.Default;
            if (userState is SerializationContext ctx)
                return ctx;

            var obj = new SerializationContext { Context = userState };
            obj.Freeze();
            return obj;
        }
        T IProtoInput<Stream>.Deserialize<T>(Stream source, T value, object userState)
            => (T)Deserialize(source, value, typeof(T), CreateContext(userState));

        void IProtoOutput<Stream>.Serialize<T>(Stream destination, T value, object userState)
            => Serialize(destination, value, CreateContext(userState));
    }
}
