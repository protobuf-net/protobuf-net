using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace ProtoBuf.Internal
{
    internal sealed partial class PrimaryTypeProvider : ISerializerFactory
    {
        object ISerializerFactory.TryCreate(Type type)
        {
            if (type.IsValueType)
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


            }
            else
            {

                // some kinds of "repeated" data is trustworthy as primary serializers;
                // i.e. vectors, and things that are **exactly** List<T> / Dictionary<TKey, TValue>
                // (we can't detect all subclasses etc, because a type might do that but
                // disable list-handling; as such, they would be tertiary)


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
                // check for List<T> and Dictionary<TKey, TValue> (non-subclass)
                else if (type.IsGenericType)
                {
                    var def = type.GetGenericTypeDefinition();
                    if (def == typeof(Dictionary<,>) || def == typeof(List<>))
                    {
                        return TryGetRepeatedProvider(type);
                    }
                }
            }
            return null;
        }

        internal static object TryGetRepeatedProvider(Type type)
        {
            if (type.IsValueType || type.IsArray) return null;

            if (TypeHelper.ResolveUniqueEnumerableT(type, out var t))
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    var targs = t.GetGenericArguments();
                    return Activator.CreateInstance(
                        typeof(DictionarySerializer<,,>).MakeGenericType(type, targs[0], targs[1]), nonPublic: true);
                }

                return Activator.CreateInstance(
                        typeof(EnumerableSerializer<,>).MakeGenericType(type, t), nonPublic: true);
            }
            return null;
        }
    }
}
