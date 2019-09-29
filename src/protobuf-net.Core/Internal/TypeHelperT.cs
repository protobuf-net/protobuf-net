using ProtoBuf.Meta;
using ProtoBuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ProtoBuf.Internal
{
    internal static class TypeHelper
    {
        internal static string CSName(Type type)
        {
            if (type == null) return null;
            if (type.IsEnum) return type.Name;

            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null) return CSName(nullable) + "?";

            if (!type.IsGenericType)
            {
                return (Type.GetTypeCode(type)) switch
                {
                    TypeCode.Boolean => "bool",
                    TypeCode.Char => "char",
                    TypeCode.SByte => "sbyte",
                    TypeCode.Byte => "byte",
                    TypeCode.Int16 => "short",
                    TypeCode.UInt16 => "ushort",
                    TypeCode.Int32 => "int",
                    TypeCode.UInt32 => "uint",
                    TypeCode.Int64 => "long",
                    TypeCode.UInt64 => "ulong",
                    TypeCode.Single => "float",
                    TypeCode.Double => "double",
                    TypeCode.Decimal => "decimal",
                    TypeCode.String => "string",
                    _ => type.Name,
                };
            }

            var withTicks = type.Name;
            var index = withTicks.IndexOf('`');
            if (index < 0) return type.Name;

            var sb = new StringBuilder();
            sb.Append(type.Name.Substring(0, index)).Append('<');
            var args = type.GetGenericArguments();
            for (int i = 0; i < args.Length; i++)
            {
                if (i != 0) sb.Append(',');
                sb.Append(CSName(args[i]));
            }
            return sb.Append('>').ToString();
        }
    }
    internal static class TypeHelper<T>
    {
        public static bool IsReferenceType = !typeof(T).IsValueType;

        public static readonly bool CanBeNull = default(T) == null;

        public static readonly Func<ISerializationContext, T> Factory = ctx => TypeModel.CreateInstance<T>(ctx);

        // make sure we don't cast null value-types to NREs
        [MethodImpl(ProtoReader.HotPath)]
        public static T FromObject(object value) => value == null ? default : (T)value;

        public static readonly ISerializer<T> PrimarySerializer = (WellKnownSerializer.Instance as ISerializer<T>) ?? TryGetPrimarySerializer();

        public static readonly ISerializer<T> AuxiliarySerializer = PrimarySerializer == null ? TryGetAuxiliarySerializer() : null;
        static ISerializer<T> TryGetPrimarySerializer()
        {
            var type = typeof(T);
            if (!type.IsValueType) return null;
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
                if (openType != null)
                    return (ISerializer<T>)Activator.CreateInstance(
                        openType.MakeGenericType(type), nonPublic: true);
                return null;
            }
            var nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null)
            {
                // we're being asked for T=Foo? for some Foo; let's see what we have for TypeHelper<Foo>
                var inbuilt = typeof(TypeHelper<>).MakeGenericType(nullable).GetField(nameof(PrimarySerializer)).GetValue(null);
                if (inbuilt != null)
                {
                    // does it provide both?
                    if (inbuilt is ISerializer<T> dual) return dual;

                    // use a shim instead
                    return (ISerializer<T>)Activator.CreateInstance(
                        typeof(NullableSerializer<>).MakeGenericType(nullable),
                        args: new[] { inbuilt });
                }
            }
            return null;
        }

        static ISerializer<T> TryGetAuxiliarySerializer()
        {
            // recognize List<T> - later we can axpand this Type itemType = TypeModel.GetListItemType(typeof(T));

            Type current = typeof(T);
            while (current != null && current != typeof(object))
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var itemType = current.GetGenericArguments()[0];
                    return (ISerializer<T>)Activator.CreateInstance(
                        typeof(ListSerialializer<,>).MakeGenericType(current, itemType), nonPublic: true);
                }
                current = current.BaseType;
            }
        }


    }

    internal sealed class ListSerialializer<TList, T> : ISerializer<TList>
        where TList : List<T>
    {
        public WireType DefaultWireType => WireType.None;

        public TList Read(ref ProtoReader.State state, TList value)
        {
            int field;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                switch(field)
                {
                    case TypeModel.ListItemTag:
                        value = state.ReadList<TList, T>(value);
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
        }

        public void Write(ref ProtoWriter.State state, TList value)
            => state.WriteList<T>(TypeModel.ListItemTag, value);
    }
    internal sealed class NullableSerializer<T> : ISerializer<T?> where T : struct
    {
        private readonly ISerializer<T> _tail;
        public NullableSerializer(ISerializer<T> tail) => _tail = tail;

        public WireType DefaultWireType => _tail.DefaultWireType;

        public T? Read(ref ProtoReader.State state, T? value)
            => _tail.Read(ref state, value.GetValueOrDefault());

        public void Write(ref ProtoWriter.State state, T? value)
            => _tail.Write(ref state, value.Value);
    }
}
