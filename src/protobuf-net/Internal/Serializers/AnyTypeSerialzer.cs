using ProtoBuf.Serializers;
using System;
using System.Linq;
using System.Reflection;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class AnyTypeSerializer<T> : IRuntimeProtoSerializerNode, IDirectWriteNode, IDirectRuntimeWriteNode
    {
        private readonly SerializerFeatures _features;
        private readonly CompatibilityLevel _compatibilityLevel;
        private readonly DataFormat _dataFormat;

        bool IRuntimeProtoSerializerNode.IsScalar => _features.IsScalar();

        public AnyTypeSerializer(SerializerFeatures features,
            CompatibilityLevel compatibilityLevel, DataFormat dataFormat)
        {
            _features = features;
            _compatibilityLevel = compatibilityLevel;
            _dataFormat = dataFormat;
        }

        public Type ExpectedType => typeof(T);

        bool IRuntimeProtoSerializerNode.RequiresOldValue => true;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
            => state.ReadAny<T>(_features, (T)value);

        public void Write(ref ProtoWriter.State state, object value)
            => throw new NotSupportedException($"Only {nameof(IDirectRuntimeWriteNode.DirectWrite)} should be used");

        void IDirectRuntimeWriteNode.DirectWrite(int fieldNumber, WireType wireType, ref ProtoWriter.State state, object value)
            => state.WriteAny<T>(fieldNumber, _features | wireType.AsFeatures(), (T)value);

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => throw new NotSupportedException($"Only {nameof(IDirectWriteNode.EmitDirectWrite)} should be used");

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            using var loc = ctx.GetLocalWithValue(typeof(T), entity);
            ctx.LoadState();
            ctx.LoadValue((int)_features);
            ctx.LoadValue(loc);
            ctx.LoadSelfAsService<ISerializer<T>, T>(_compatibilityLevel, _dataFormat);
            ctx.EmitCall(ReadAnyT);
        }

        bool IDirectWriteNode.CanEmitDirectWrite(WireType wireType) => true;
        bool IDirectRuntimeWriteNode.CanDirectWrite(WireType wireType) => true;

        void IDirectWriteNode.EmitDirectWrite(int fieldNumber, WireType wireType, Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using var loc = ctx.GetLocalWithValue(typeof(T), valueFrom);
            ctx.LoadState();
            ctx.LoadValue(fieldNumber);
            ctx.LoadValue((int)(_features | wireType.AsFeatures()));
            ctx.LoadValue(loc);
            ctx.LoadSelfAsService<ISerializer<T>, T>(_compatibilityLevel, _dataFormat);
            ctx.EmitCall(WriteAnyT);
        }

        private static readonly MethodInfo
            ReadAnyT = AnyTypeSerializer.ReadAnyT.MakeGenericMethod(typeof(T)),
            WriteAnyT = AnyTypeSerializer.WriteAnyT.MakeGenericMethod(typeof(T));
    }
    internal static class AnyTypeSerializer
    {
        private static bool FindSerializerFeaturesMethodFilter(MemberInfo member, object state)
        {
            if (member is MethodInfo method && state is string name && member.Name == name)
            {
                foreach (var p in method.GetParameters())
                {
                    if (p.ParameterType == typeof(SerializerFeatures))
                        return true;
                }
            }
            return false;
        }
        private static MethodInfo FindSerializerFeaturesMethod(Type type, string name)
            => (MethodInfo)type.FindMembers(
                MemberTypes.Method, BindingFlags.Public | BindingFlags.Instance,
                FindSerializerFeaturesMethodFilter, name).Single();

        internal static readonly MethodInfo
            ReadAnyT = FindSerializerFeaturesMethod(typeof(ProtoReader.State), nameof(ProtoReader.State.ReadAny)),
            WriteAnyT = FindSerializerFeaturesMethod(typeof(ProtoWriter.State), nameof(ProtoWriter.State.WriteAny));

        internal static IRuntimeProtoSerializerNode Create(Type memberType, SerializerFeatures features, CompatibilityLevel compatibilityLevel, DataFormat dataFormat)
            => (IRuntimeProtoSerializerNode)Activator.CreateInstance(typeof(AnyTypeSerializer<>).MakeGenericType(memberType),
                args: new object[] { features, compatibilityLevel, dataFormat });
    }
}