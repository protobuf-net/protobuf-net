using ProtoBuf.Compiler;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;

namespace ProtoBuf.Internal.Serializers
{
    internal static class MapDecorator
    {
        public static IRuntimeProtoSerializerNode Create(RepeatedSerializerStub provider, Type keyType, Type valueType,
            int fieldNumber, SerializerFeatures features,
            SerializerFeatures keyFeatures, CompatibilityLevel keyCompatibilityLevel, DataFormat keyDataFormat,
            SerializerFeatures valueFeatures, CompatibilityLevel valueCompatibilityLevel, DataFormat valueDataFormat)
        {
            if (provider is null) ThrowHelper.ThrowArgumentNullException(nameof(provider));
            _ = provider.Serializer; // primes and validates
            return (IRuntimeProtoSerializerNode)Activator.CreateInstance(
                typeof(MapDecorator<,,>).MakeGenericType(provider.ForType, keyType, valueType),
                new object[] { fieldNumber, features,
                    keyFeatures, keyCompatibilityLevel, keyDataFormat,
                    valueFeatures, valueCompatibilityLevel, valueDataFormat,
                    provider });
        }
    }
    internal class MapDecorator<TCollection, TKey, TValue> : IRuntimeProtoSerializerNode, ICompiledSerializer
    {
        bool IRuntimeProtoSerializerNode.IsScalar => false;
        public MapDecorator(
            int fieldNumber, SerializerFeatures features,
            SerializerFeatures keyFeatures, CompatibilityLevel keyCompatibilityLevel, DataFormat keyDataFormat,
            SerializerFeatures valueFeatures, CompatibilityLevel valueCompatibilityLevel, DataFormat valueDataFormat,
            RepeatedSerializerStub provider)
        {
            _provider = provider;
            _features = features;
            _keyFeatures = keyFeatures;
            _keyCompatibilityLevel = keyCompatibilityLevel;
            _keyDataFormat = keyDataFormat;
            _valueFeatures = valueFeatures;
            _valueCompatibilityLevel = valueCompatibilityLevel;
            _valueDataFormat = valueDataFormat;
            _fieldNumber = fieldNumber;
        }
        private readonly int _fieldNumber;
        private readonly SerializerFeatures _features, _keyFeatures, _valueFeatures;
        private readonly CompatibilityLevel _keyCompatibilityLevel, _valueCompatibilityLevel;
        private readonly DataFormat _keyDataFormat, _valueDataFormat;

        private readonly RepeatedSerializerStub _provider;
        private MapSerializer<TCollection, TKey, TValue> Serializer => (MapSerializer<TCollection, TKey, TValue>)_provider.Serializer;

        public Type ExpectedType => typeof(TCollection);

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public bool RequiresOldValue => true;

        public object Read(ref ProtoReader.State state, object value)
            => Serializer.ReadMap(ref state, _features, (TCollection)value, _keyFeatures, _valueFeatures,
                TypeModel.GetInbuiltSerializer<TKey>(_keyCompatibilityLevel, _keyDataFormat),
                TypeModel.GetInbuiltSerializer<TValue>(_valueCompatibilityLevel, _valueDataFormat));

        public void Write(ref ProtoWriter.State state, object value)
            => Serializer.WriteMap(ref state, _fieldNumber, _features, (TCollection)value, _keyFeatures, _valueFeatures,
                TypeModel.GetInbuiltSerializer<TKey>(_keyCompatibilityLevel, _keyDataFormat),
                TypeModel.GetInbuiltSerializer<TValue>(_valueCompatibilityLevel, _valueDataFormat));

        public void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            _ = Serializer; // this is to force a type-check
            var method = typeof(MapSerializer<TCollection, TKey, TValue>).GetMethod(nameof(MapSerializer<TCollection, TKey, TValue>.WriteMap));

            using var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            _provider.EmitProvider(ctx);
            ctx.LoadState();
            ctx.LoadValue(_fieldNumber);
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadValue((int)_keyFeatures);
            ctx.LoadValue((int)_valueFeatures);
            ctx.LoadSelfAsService<ISerializer<TKey>, TKey>(_keyCompatibilityLevel, _keyDataFormat);
            ctx.LoadSelfAsService<ISerializer<TValue>, TValue>(_valueCompatibilityLevel, _valueDataFormat);
            ctx.EmitCall(method);
        }
        public void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            var method = typeof(MapSerializer<TCollection, TKey, TValue>).GetMethod(nameof(MapSerializer<TCollection, TKey, TValue>.ReadMap));

            using var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            _provider.EmitProvider(ctx);
            ctx.LoadState();
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadValue((int)_keyFeatures);
            ctx.LoadValue((int)_valueFeatures);
            ctx.LoadSelfAsService<ISerializer<TKey>, TKey>(_keyCompatibilityLevel, _keyDataFormat);
            ctx.LoadSelfAsService<ISerializer<TValue>, TValue>(_valueCompatibilityLevel, _valueDataFormat);
            ctx.EmitCall(method);
        }
    }
}