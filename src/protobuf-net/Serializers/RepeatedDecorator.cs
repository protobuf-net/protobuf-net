using ProtoBuf.Compiler;
using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    internal static class RepeatedDecorator
    {
        public static IRuntimeProtoSerializerNode Create(Type collectionType, Type type, int fieldNumber, SerializerFeatures features, MemberInfo provider)
        {
            if (provider == null) ThrowHelper.ThrowArgumentNullException(nameof(provider), $"No suitable repeated serializer resolved for {collectionType.NormalizeName()}");
            return (IRuntimeProtoSerializerNode)Activator.CreateInstance(typeof(RepeatedDecorator<,>).MakeGenericType(collectionType, type),
                new object[] { fieldNumber, features, provider });
        }
        internal static IRepeatedSerializer<T> GetSerializer<T>(MemberInfo original)
        {
            var provider = RuntimeTypeModel.GetUnderlyingProvider(original, typeof(T));
            object obj = provider switch
            {
                FieldInfo field when field.IsStatic => field.GetValue(null),
                MethodInfo method when method.IsStatic => method.Invoke(null, null),
                _ => null,
            };
            if (obj is IRepeatedSerializer<T> serializer) return serializer;
            ThrowHelper.ThrowInvalidOperationException($"No suitable repeated serializer resolved for {typeof(T).NormalizeName()}");
            return default;
        }
    }
    internal sealed class RepeatedDecorator<TCollection, T> : IRuntimeProtoSerializerNode, ICompiledSerializer
    {
        private readonly int _fieldNumber;
        private readonly SerializerFeatures _features;

        private readonly MemberInfo _provider;
        private readonly RepeatedSerializer<TCollection, T> _serializer;

        public RepeatedDecorator(int fieldNumber, SerializerFeatures features, MemberInfo provider)
        {
            _provider = provider;
            _serializer = (RepeatedSerializer<TCollection, T>)RepeatedDecorator.GetSerializer<TCollection>(provider);
            _fieldNumber = fieldNumber;
            _features = features;
        }

        public Type ExpectedType => typeof(TCollection);
        public bool RequiresOldValue => true;
        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
            => _serializer.ReadRepeated(ref state, _features, (TCollection)value);

        public void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            var method = typeof(RepeatedSerializer<TCollection, T>).GetMethod(nameof(RepeatedSerializer<TCollection, T>.ReadRepeated));

            using var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            RuntimeTypeModel.EmitProvider(_provider, ctx.IL);
            ctx.LoadState();
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadSelfAsService<ISerializer<T>, T>();
            ctx.EmitCall(method);
        }

        public void Write(ref ProtoWriter.State state, object value)
            => _serializer.WriteRepeated(ref state, _fieldNumber, _features, (TCollection)value);

        public void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            var method = typeof(RepeatedSerializer<TCollection, T>).GetMethod(nameof(RepeatedSerializer<TCollection, T>.WriteRepeated));

            using var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            RuntimeTypeModel.EmitProvider(_provider, ctx.IL);
            ctx.LoadState();
            ctx.LoadValue(_fieldNumber);
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadSelfAsService<ISerializer<T>, T>();
            ctx.EmitCall(method);
        }
    }
}