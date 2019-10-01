using ProtoBuf.Compiler;
using ProtoBuf.Internal;
using System;

namespace ProtoBuf.Serializers
{
    internal class EnumMemberSerializer : IRuntimeProtoSerializerNode, IDirectWriteNode
    {

        private readonly IRuntimeProtoSerializerNode _tail;
        public EnumMemberSerializer(Type enumType)
        {
            if (!enumType.IsEnum) ThrowHelper.ThrowInvalidOperationException("Expected an enum type; got " + enumType.NormalizeName());
            ExpectedType = enumType ?? throw new ArgumentNullException(nameof(enumType));
            _tail = Type.GetTypeCode(Enum.GetUnderlyingType(enumType)) switch
            {
                TypeCode.SByte => SByteSerializer.Instance,
                TypeCode.Int16 => Int16Serializer.Instance,
                TypeCode.Int32 => Int32Serializer.Instance,
                TypeCode.Int64 => Int64Serializer.Instance,
                TypeCode.Byte => ByteSerializer.Instance,
                TypeCode.UInt16 => UInt16Serializer.Instance,
                TypeCode.UInt32 => UInt32Serializer.Instance,
                TypeCode.UInt64 => UInt64Serializer.Instance,
                _ => default,
            };
            if (_tail == null) ThrowHelper.ThrowInvalidOperationException("Unable to resolve underlying enum type for " + enumType.NormalizeName());

        }

        private ProtoTypeCode GetTypeCode()
        {
            Type type = Nullable.GetUnderlyingType(ExpectedType) ?? ExpectedType;
            return Helpers.GetTypeCode(type);
        }

        public Type ExpectedType { get; }

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        private object EnumToWire(object value)
        {
            
            unchecked
            {
                return (GetTypeCode()) switch
                { // unbox then convert to int
                    ProtoTypeCode.Byte => (int)(byte)value,
                    ProtoTypeCode.SByte => (int)(sbyte)value,
                    ProtoTypeCode.Int16 => (int)(short)value,
                    ProtoTypeCode.Int32 => (int)value,
                    ProtoTypeCode.Int64 => (int)(long)value,
                    ProtoTypeCode.UInt16 => (int)(ushort)value,
                    ProtoTypeCode.UInt32 => (int)(uint)value,
                    ProtoTypeCode.UInt64 => (int)(ulong)value,
                    _ => throw new InvalidOperationException(),
                };
            }
        }

        public object Read(ref ProtoReader.State state, object value)
            => Enum.ToObject(ExpectedType, _tail.Read(ref state, value));

        public void Write(ref ProtoWriter.State state, object value)
            => _tail.Write(ref state, EnumToWire(value));

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
            => _tail.EmitWrite(ctx, valueFrom);

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
            => _tail.EmitRead(ctx, entity);

        bool IDirectWriteNode.CanEmitDirectWrite(WireType wireType) => _tail is IDirectWriteNode dw && dw.CanEmitDirectWrite(wireType);

        void IDirectWriteNode.EmitDirectWrite(int fieldNumber, WireType wireType, CompilerContext ctx, Local valueFrom)
            => ((IDirectWriteNode)_tail).EmitDirectWrite(fieldNumber, wireType, ctx, valueFrom);
    }
}