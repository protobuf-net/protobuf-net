using ProtoBuf.Meta;
using System;
using System.Collections.Generic;

namespace ProtoBuf.Internal
{
    sealed class AuxiliaryTypeProvider : ISerializerFactory
    {
        public object TryCreate(Type type)
        {
            // recognize List<T> *subclasses*
            // (subclasses are "aux" because the model might disable list handling on the subclass)
            Type current = type;
            while (current != null && current != typeof(object))
            {
                var list = PrimaryTypeProvider.TryGetListProvider(type, current);
                if (list != null) return list;

                current = current.BaseType;
            }
            return null;
        }
    }
}
