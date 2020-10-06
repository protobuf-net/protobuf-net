using ProtoBuf.Meta;
using System;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    internal static class EnumSerializers
    {
        internal static object GetSerializer(Type type)
            => RuntimeTypeModel.GetUnderlyingProvider(GetProvider(type), type) switch
            {
                FieldInfo field => field.GetValue(null),
                MethodInfo method => method.Invoke(null, null),
                _ => null,
            };
        internal static MemberInfo GetProvider(Type type)
        {
            if (type is null) return null;
            type = Nullable.GetUnderlyingType(type) ?? type;
            if (!type.IsEnum) return null;
            string name = Type.GetTypeCode(type) switch
            {
                TypeCode.SByte => nameof(EnumSerializer.CreateSByte),
                TypeCode.Int16 => nameof(EnumSerializer.CreateInt16),
                TypeCode.Int32 => nameof(EnumSerializer.CreateInt32),
                TypeCode.Int64 => nameof(EnumSerializer.CreateInt64),
                TypeCode.Byte => nameof(EnumSerializer.CreateByte),
                TypeCode.UInt16 => nameof(EnumSerializer.CreateUInt16),
                TypeCode.UInt32 => nameof(EnumSerializer.CreateUInt32),
                TypeCode.UInt64 => nameof(EnumSerializer.CreateUInt64),
                _ => null,
            };
            if (name is null) return null;
            return typeof(EnumSerializer).GetMethod(name, BindingFlags.Static | BindingFlags.Public)
                .MakeGenericMethod(type);
        }
    }
}
