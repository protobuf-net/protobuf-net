using ProtoBuf.Compiler;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Reflection;

namespace ProtoBuf.Internal.Serializers
{
    internal static class RepeatedDecorator
    {
        public static IRuntimeProtoSerializerNode Create(RepeatedSerializerStub stub, int fieldNumber, SerializerFeatures features, CompatibilityLevel compatibilityLevel, DataFormat dataFormat)
        {
            if (stub is null) ThrowHelper.ThrowArgumentNullException(nameof(stub), $"No suitable repeated serializer resolved for {stub.ForType.NormalizeName()}");
            _ = stub.Serializer; // primes and validates
            return (IRuntimeProtoSerializerNode)Activator.CreateInstance(typeof(RepeatedDecorator<,>).MakeGenericType(stub.ForType, stub.ItemType),
                new object[] { fieldNumber, features, compatibilityLevel, dataFormat, stub });
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
        private readonly CompatibilityLevel _compatibilityLevel;
        private readonly DataFormat _dataFormat;

        bool IRuntimeProtoSerializerNode.IsScalar
        {
            get
            {
                var inbuilt = TypeModel.GetInbuiltSerializer<T>(_compatibilityLevel, _dataFormat);
                return inbuilt is not null && inbuilt.Features.IsScalar();
            }
        }

        private readonly RepeatedSerializerStub _stub;
        private RepeatedSerializer<TCollection, T> Serializer => (RepeatedSerializer<TCollection, T>)_stub.Serializer;

        public RepeatedDecorator(int fieldNumber, SerializerFeatures features, CompatibilityLevel compatibilityLevel, DataFormat dataFormat, RepeatedSerializerStub stub)
        {
            _stub = stub;
            _fieldNumber = fieldNumber;
            _features = features;
            _compatibilityLevel = ValueMember.GetEffectiveCompatibilityLevel(compatibilityLevel, dataFormat);
            _dataFormat = dataFormat;
        }

        public Type ExpectedType => typeof(TCollection);
        public bool RequiresOldValue => true;
        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
            => Serializer.ReadRepeated(ref state, _features, (TCollection)value, TypeModel.GetInbuiltSerializer<T>(_compatibilityLevel, _dataFormat));

        public void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            _ = Serializer; // this is to force a type-check
            var method = typeof(RepeatedSerializer<TCollection, T>).GetMethod(nameof(RepeatedSerializer<TCollection, T>.ReadRepeated));

            using var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            _stub.EmitProvider(ctx);
            ctx.LoadState();
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadSelfAsService<ISerializer<T>, T>(_compatibilityLevel, _dataFormat);
            ctx.EmitCall(method);
        }

        public void Write(ref ProtoWriter.State state, object value)
            => Serializer.WriteRepeated(ref state, _fieldNumber, _features, (TCollection)value, TypeModel.GetInbuiltSerializer<T>(_compatibilityLevel, _dataFormat));

        public void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            var method = typeof(RepeatedSerializer<TCollection, T>).GetMethod(nameof(RepeatedSerializer<TCollection, T>.WriteRepeated));

            using var loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            _stub.EmitProvider(ctx);
            ctx.LoadState();
            ctx.LoadValue(_fieldNumber);
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadSelfAsService<ISerializer<T>, T>(_compatibilityLevel, _dataFormat);
            ctx.EmitCall(method);
        }
    }
}