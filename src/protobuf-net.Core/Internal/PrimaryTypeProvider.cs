using System;

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

            return null;
        }
    }
}
