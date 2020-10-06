using ProtoBuf.Compiler;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Serializers
{
    internal sealed class RepeatedSerializerStub
    {
        internal static readonly RepeatedSerializerStub Empty = new RepeatedSerializerStub(null, null);

        public MemberInfo Provider { get; }
        public bool IsMap { get; }
        internal bool IsValidProtobufMap(RuntimeTypeModel model, CompatibilityLevel compatibilityLevel, DataFormat dataFormat)
        {
            if (!IsMap) return false;
            ResolveMapTypes(out var key, out var value);

            // the key must an any integral or string type (not floating point or bytes)
            if (!IsValidKey(key, compatibilityLevel, dataFormat)) return false;

            // the value cannot be repeated (neither can key, but we ruled that out above)
            var repeated = model is null ? RepeatedSerializers.TryGetRepeatedProvider(value) : model.TryGetRepeatedProvider(value);
            if (repeated is object) return false;

            return true;

            static bool IsValidKey(Type type, CompatibilityLevel compatibilityLevel, DataFormat dataFormat)
            {
                if (type is null) return false;
                if (type.IsEnum) return true;
                if (type == typeof(string)) return true;
                if (!type.IsValueType) return false;
                if (Nullable.GetUnderlyingType(type) is object) return false;
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Byte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                        return true;
                }
                if (compatibilityLevel >= CompatibilityLevel.Level300)
                {   // we'll allow guids as strings as keys
                    if (type == typeof(Guid) && dataFormat != DataFormat.FixedSize) return true;
                }
                return false;
            }
        }
        public bool IsEmpty => Provider is null;
        public object Serializer => _serializer ?? CreateSerializer();
        public Type ForType { get; }
        public Type ItemType { get; }
        private object _serializer;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private object CreateSerializer()
        {
            try
            {
                _serializer = RuntimeTypeModel.GetUnderlyingProvider(Provider, ForType) switch
                {
                    FieldInfo field when field.IsStatic => field.GetValue(null),
                    MethodInfo method when method.IsStatic => method.Invoke(null, null),
                    _ => null,
                };
                return _serializer;
            }
            catch(TargetInvocationException tie) when (tie.InnerException is object)
            {
                throw tie.InnerException;
            }
        }

        internal void EmitProvider(CompilerContext ctx) => EmitProvider(ctx.IL);
        private void EmitProvider(ILGenerator il)
        {
            var provider = RuntimeTypeModel.GetUnderlyingProvider(Provider, ForType);
            RuntimeTypeModel.EmitProvider(provider, il);
        }

        public static RepeatedSerializerStub Create(Type forType, MemberInfo provider)
            => provider is null ? Empty : new RepeatedSerializerStub(forType, provider);

        private RepeatedSerializerStub(Type forType, MemberInfo provider)
        {
            ForType = forType;
            Provider = provider;
            IsMap = CheckIsMap(provider, out Type itemType);
            ItemType = itemType;
        }
        private static bool CheckIsMap(MemberInfo provider, out Type itemType)
        {
            var type = provider switch
            {
                MethodInfo method => method.ReturnType,
                FieldInfo field => field.FieldType,
                PropertyInfo prop => prop.PropertyType,
                Type t => t,
                _ => null,
            };
            while (type is object && type != typeof(object))
            {
                if (type.IsGenericType)
                {
                    var genDef = type.GetGenericTypeDefinition();
                    if (genDef == typeof(MapSerializer<,,>))
                    {
                        var targs = type.GetGenericArguments();
                        itemType = typeof(KeyValuePair<,>).MakeGenericType(targs[1], targs[2]);
                        return true;
                    }
                    if (genDef == typeof(RepeatedSerializer<,>))
                    {
                        var targs = type.GetGenericArguments();
                        itemType = targs[1];
                        return false;
                    }
                }

                type = type.BaseType;
            }
            itemType = null;
            return false;
        }

        internal void ResolveMapTypes(out Type keyType, out Type valueType)
        {
            keyType = valueType = null;
            if (IsMap)
            {
                var targs = ItemType.GetGenericArguments();
                keyType = targs[0];
                valueType = targs[1];
            }
        }
    }
}
