using System;

namespace ProtoBuf.Internal
{
    sealed class TertiaryTypeProvider : ISerializerFactory
    {
        public object TryCreate(Type type)
        {
            // recognize things that look "repeated", as fallbacks
            return PrimaryTypeProvider.TryGetRepeatedProvider(type);
        }
    }
}
