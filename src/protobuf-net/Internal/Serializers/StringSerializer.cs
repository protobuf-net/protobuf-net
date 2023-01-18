using System;
using System.Diagnostics;
using System.Reflection;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class StringSerializer : IRuntimeProtoSerializerNode, IDirectWriteNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private StringSerializer() { }
        internal static readonly StringSerializer Instance = new StringSerializer();

        private static readonly Type expectedType = typeof(string);

        public Type ExpectedType => expectedType;

        public void Write(ref ProtoWriter.State state, object value)
        {
            state.WriteString((string)value);
        }
        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return state.ReadString();
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using var loc = ctx.GetLocalWithValue(typeof(string), valueFrom);
            ctx.LoadState();
            ctx.LoadValue(loc);
            ctx.LoadNullRef(); // map
            ctx.EmitCall(typeof(ProtoWriter.State).GetMethod(nameof(ProtoWriter.State.WriteString), BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(string), typeof(StringMap) }, null));
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.LoadState();
            ctx.LoadNullRef(); // map
            ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.ReadString), BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(StringMap) }, null));
        }

        bool IDirectWriteNode.CanEmitDirectWrite(WireType wireType) => wireType == WireType.String;

        void IDirectWriteNode.EmitDirectWrite(int fieldNumber, WireType wireType, Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using var loc = ctx.GetLocalWithValue(typeof(string), valueFrom);
            ctx.LoadState();
            ctx.LoadValue(fieldNumber);
            ctx.LoadValue(loc);
            ctx.LoadNullRef(); // map
            ctx.EmitCall(typeof(ProtoWriter.State).GetMethod(nameof(ProtoWriter.State.WriteString), BindingFlags.Instance | BindingFlags.Public,
                null, new[] { typeof(int), typeof(string), typeof(StringMap) }, null));
        }
    }
}