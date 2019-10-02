using System;
using System.Collections.Generic;

namespace ProtoBuf.Internal
{
    internal sealed partial class PrimaryTypeProvider : ISerializerFactory
    {
        object ISerializerFactory.TryCreate(Type type)
        {
            if (type.IsEnum)
            {
                Type rawType = Enum.GetUnderlyingType(type);
                Type openType = Type.GetTypeCode(rawType) switch
                {
                    TypeCode.SByte => typeof(EnumSerializerSByte<>),
                    TypeCode.Int16 => typeof(EnumSerializerInt16<>),
                    TypeCode.Int32 => typeof(EnumSerializerInt32<>),
                    TypeCode.Int64 => typeof(EnumSerializerInt64<>),
                    TypeCode.Byte => typeof(EnumSerializerByte<>),
                    TypeCode.UInt16 => typeof(EnumSerializerUInt16<>),
                    TypeCode.UInt32 => typeof(EnumSerializerUInt32<>),
                    TypeCode.UInt64 => typeof(EnumSerializerUInt64<>),
                    _ => null
                };
                if (openType != null) return Activator.CreateInstance(
                        openType.MakeGenericType(type), nonPublic: true);
            }

            // check for T[]
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var vectorType = elementType.MakeArrayType();

                if (type == vectorType)
                {
                    return Activator.CreateInstance(
                        typeof(VectorSerializer<>).MakeGenericType(elementType), nonPublic: true);
                }
            }

            // check for List<T> (non-subclass)
            var list = TryGetListProvider(type, type);
            if (list != null) return list;

            return null;
        }

        internal static object TryGetListProvider(Type rootType, Type type)
        {
            // later we can axpand this Type itemType = TypeModel.GetListItemType(typeof(T));
            // for now: just handles List<T>

            if (type.IsGenericType)
            {
                var def = type.GetGenericTypeDefinition();

                if (def == typeof(Dictionary<,>))
                {
                    var args = type.GetGenericArguments();
                    return Activator.CreateInstance(
                        typeof(DictionarySerializer<,,>).MakeGenericType(rootType, args[0], args[1]), nonPublic: true);
                }

                if (def == typeof(List<>))
                {
                    var args = type.GetGenericArguments();
                    return Activator.CreateInstance(
                        typeof(ListSerializer<,>).MakeGenericType(rootType, args[0]), nonPublic: true);
                }
            }
            return null;
        }
    }
}
