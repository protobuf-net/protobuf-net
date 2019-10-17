using ProtoBuf.Meta;
using ProtoBuf.Serializers;

namespace ProtoBuf.Reflection.Internal
{
    // exported and tweaked (naming, invalid C# etc)
    internal sealed partial class CustomProtogenSerializer : TypeModel
    {
        private CustomProtogenSerializer() { }
        internal static TypeModel Instance { get; } = new CustomProtogenSerializer();

        protected override ISerializer<T> GetSerializer<T>() =>
            SerializerCache.Get<CustomProtogenSerializerServices, T>();
    }
}