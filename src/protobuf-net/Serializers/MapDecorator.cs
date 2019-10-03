using ProtoBuf.Compiler;
using ProtoBuf.Internal;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace ProtoBuf.Serializers
{
    internal static class MapDecorator
    {
        public static IRuntimeProtoSerializerNode Create(Type constructType, Type keyType, Type valueType,
            int fieldNumber, SerializerFeatures features,
            SerializerFeatures keyFeatures, SerializerFeatures valueFeatures)
        {
            return (IRuntimeProtoSerializerNode)Activator.CreateInstance(
                typeof(MapDecorator<,,>).MakeGenericType(constructType, keyType, valueType),
                new object[] { fieldNumber, features, keyFeatures, valueFeatures });
        }

        // look for:  public IDictionary<TKey, TValue> ReadMap<TKey, TValue>(SerializerFeatures, SerializerFeatures, SerializerFeatures,
        // IDictionary<TKey, TValue>, ISerializer<TKey>, ISerializer<TValue>)
        internal static readonly MethodInfo s_ReadMap = (
            from method in typeof(ProtoReader.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            where method.Name == nameof(ProtoReader.State.ReadMap) && method.IsGenericMethodDefinition
            let targs = method.GetGenericArguments()
            where targs.Length == 2
            let args = method.GetParameters()
            where args.Length == 6
            && args[0].ParameterType == typeof(SerializerFeatures)
            && args[1].ParameterType == typeof(SerializerFeatures)
            && args[2].ParameterType == typeof(SerializerFeatures)
            && args[3].ParameterType == typeof(IDictionary<,>).MakeGenericType(targs)
            && args[4].ParameterType == typeof(ISerializer<>).MakeGenericType(targs[0])
            && args[5].ParameterType == typeof(ISerializer<>).MakeGenericType(targs[1])
            && method.ReturnType == args[3].ParameterType
            select method).SingleOrDefault();

        // look for: public void WriteMap<TKey, TValue>(int, SerializerFeatures, SerializerFeatures, SerializerFeatures,
        // IDictionary<TKey, TValue>, ISerializer<TKey>, ISerializer<TValue>)
        internal static readonly MethodInfo s_WriteMap = (
            from method in typeof(ProtoWriter.State).GetMethods(BindingFlags.Instance | BindingFlags.Public)
            where method.Name == nameof(ProtoWriter.State.WriteMap) && method.IsGenericMethodDefinition
            && method.ReturnType == typeof(void)
            let targs = method.GetGenericArguments()
            where targs.Length == 2
            let args = method.GetParameters()
            where args.Length == 7
            && args[0].ParameterType == typeof(int)
            && args[1].ParameterType == typeof(SerializerFeatures)
            && args[2].ParameterType == typeof(SerializerFeatures)
            && args[3].ParameterType == typeof(SerializerFeatures)
            && args[4].ParameterType == typeof(IDictionary<,>).MakeGenericType(targs)
            && args[5].ParameterType == typeof(ISerializer<>).MakeGenericType(targs[0])
            && args[6].ParameterType == typeof(ISerializer<>).MakeGenericType(targs[1])
            select method).SingleOrDefault();
    }
    internal class MapDecorator<TConstruct, TKey, TValue> : IRuntimeProtoSerializerNode
        where TConstruct : class, IDictionary<TKey, TValue>
    {
        static readonly MethodInfo s_ReadMap = MapDecorator.s_ReadMap.MakeGenericMethod(typeof(TKey), typeof(TValue));
        static readonly MethodInfo s_WriteMap = MapDecorator.s_WriteMap.MakeGenericMethod(typeof(TKey), typeof(TValue));

        public MapDecorator(
            int fieldNumber, SerializerFeatures features,
            SerializerFeatures keyFeatures, SerializerFeatures valueFeatures)
        {
            _features = features;
            _keyFeatures = keyFeatures;
            _valueFeatures = valueFeatures;
            _fieldNumber = fieldNumber;
        }
        private readonly int _fieldNumber;
        private readonly SerializerFeatures _features, _keyFeatures, _valueFeatures;

        public Type ExpectedType => typeof(IDictionary<TKey, TValue>);

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public bool RequiresOldValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            var typed = (IDictionary<TKey, TValue>)value;
            typed ??= state.CreateInstance<TConstruct>();
            return state.ReadMap(_features, _keyFeatures, _valueFeatures, typed);
        }

        public void Write(ref ProtoWriter.State state, object value)
            => state.WriteMap(_fieldNumber, _features, _keyFeatures, _valueFeatures, (IDictionary<TKey, TValue>)value);

        public void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            using var loc = ctx.GetLocalWithValue(typeof(IDictionary<TKey, TValue>), valueFrom);
            ctx.LoadState();
            ctx.LoadValue(_fieldNumber);
            ctx.LoadValue((int)_features);
            ctx.LoadValue((int)_keyFeatures);
            ctx.LoadValue((int)_valueFeatures);
            ctx.LoadValue(loc);
            ctx.LoadSelfAsService<ISerializer<TKey>, TValue>();
            ctx.LoadSelfAsService<ISerializer<TKey>, TValue>();
            ctx.EmitCall(s_WriteMap);
        }
        public void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            using var loc = ctx.GetLocalWithValue(typeof(IDictionary<TKey, TValue>), valueFrom);

            var notNull = ctx.DefineLabel();
            ctx.LoadValue(loc);
            ctx.BranchIfTrue(notNull, true);
            ctx.CreateInstance<TConstruct>();
            ctx.StoreValue(loc);
            ctx.MarkLabel(notNull);

            ctx.LoadState();
            ctx.LoadValue((int)_features);
            ctx.LoadValue((int)_keyFeatures);
            ctx.LoadValue((int)_valueFeatures);
            ctx.LoadValue(loc);
            ctx.LoadSelfAsService<ISerializer<TKey>, TValue>();
            ctx.LoadSelfAsService<ISerializer<TKey>, TValue>();
            ctx.EmitCall(s_ReadMap);
        }
    }
}