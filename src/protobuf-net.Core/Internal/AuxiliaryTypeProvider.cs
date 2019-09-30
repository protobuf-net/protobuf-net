using ProtoBuf.Meta;
using System;
using System.Collections.Generic;

namespace ProtoBuf.Internal
{
    sealed class AuxiliaryTypeProvider : ISerializerFactory
    {
        public object TryCreate(Type type)
        {
            // recognize List<T> - later we can axpand this Type itemType = TypeModel.GetListItemType(typeof(T));

            Type current = type;
            while (current != null && current != typeof(object))
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var itemType = current.GetGenericArguments()[0];
                    return Activator.CreateInstance(
                        typeof(ListSerializer<,>).MakeGenericType(current, itemType), nonPublic: true);
                }
                current = current.BaseType;
            }
            return null;
        }

        private sealed class ListSerializer<TList, T> : ISerializer<TList>
            where TList : List<T>
        {
            public WireType DefaultWireType => WireType.None;

            public TList Read(ref ProtoReader.State state, TList value)
            {
                int field;
                ISerializer<T> serializer = null;
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case TypeModel.ListItemTag:
                            serializer ??= TypeModel.GetSerializer<T>(state.Model);
                            value = state.ReadRepeated<TList, T>(value, serializer);
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                return value;
            }

            public void Write(ref ProtoWriter.State state, TList value)
            {
                var serializer = TypeModel.GetSerializer<T>(state.Model);
                state.WriteRepeated<T>(TypeModel.ListItemTag, serializer.DefaultWireType, value, serializer);
            }
        }
    }
}
