using ProtoBuf.Compiler;
using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    internal static class MapDecorator
    {
        public static IRuntimeProtoSerializerNode Create(Type constructType, Type keyType, Type valueType,
            int fieldNumber, SerializerFeatures features,
            SerializerFeatures keyFeatures, SerializerFeatures valueFeatures, MemberInfo provider)
        {
            if (provider == null) ThrowHelper.ThrowArgumentNullException(nameof(provider));
            return (IRuntimeProtoSerializerNode)Activator.CreateInstance(
                typeof(MapDecorator<,,>).MakeGenericType(constructType, keyType, valueType),
                new object[] { fieldNumber, features, keyFeatures, valueFeatures, provider });
        }
    }
    internal class MapDecorator<TCollection, TKey, TValue> : IRuntimeProtoSerializerNode, ICompiledSerializer
        where TCollection : class, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        public MapDecorator(
            int fieldNumber, SerializerFeatures features,
            SerializerFeatures keyFeatures, SerializerFeatures valueFeatures, MemberInfo provider)
        {
            _provider = provider;
            _serializer = (MapSerializer<TCollection, TKey, TValue>)RepeatedDecorator.GetSerializer<TCollection>(provider);
            _features = features;
            _keyFeatures = keyFeatures;
            _valueFeatures = valueFeatures;
            _fieldNumber = fieldNumber;
        }
        private readonly int _fieldNumber;
        private readonly SerializerFeatures _features, _keyFeatures, _valueFeatures;

        private readonly MemberInfo _provider;
        private readonly MapSerializer<TCollection, TKey, TValue> _serializer;

        public Type ExpectedType => typeof(TCollection);

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public bool RequiresOldValue => true;

        public object Read(ref ProtoReader.State state, object value)
            => _serializer.ReadMap(ref state, _features, (TCollection)value, _keyFeatures, _valueFeatures);

        public void Write(ref ProtoWriter.State state, object value)
            => _serializer.WriteMap(ref state, _fieldNumber, _features, (TCollection)value, _keyFeatures, _valueFeatures);

        public void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            var method = typeof(MapSerializer<TCollection, TKey, TValue>).GetMethod(nameof(MapSerializer<TCollection, TKey, TValue>.WriteMap));

            using var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            RuntimeTypeModel.EmitProvider(_provider, ctx.IL);
            ctx.LoadState();
            ctx.LoadValue(_fieldNumber);
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadValue((int)_keyFeatures);
            ctx.LoadValue((int)_valueFeatures);
            ctx.LoadSelfAsService<ISerializer<TKey>, TValue>();
            ctx.LoadSelfAsService<ISerializer<TKey>, TValue>();
            ctx.EmitCall(method);
        }
        public void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            var method = typeof(MapSerializer<TCollection, TKey, TValue>).GetMethod(nameof(MapSerializer<TCollection, TKey, TValue>.ReadMap));

            using var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            RuntimeTypeModel.EmitProvider(_provider, ctx.IL);
            ctx.LoadState();
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadValue((int)_keyFeatures);
            ctx.LoadValue((int)_valueFeatures);
            ctx.LoadSelfAsService<ISerializer<TKey>, TValue>();
            ctx.LoadSelfAsService<ISerializer<TKey>, TValue>();
            ctx.EmitCall(method);
        }
    }
}