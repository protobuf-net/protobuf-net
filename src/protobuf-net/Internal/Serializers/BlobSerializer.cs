using System;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class BlobSerializer<T> : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        public Type ExpectedType { get { return expectedType; } }

        private static readonly Type expectedType = typeof(T);

        public BlobSerializer(bool overwriteList)
        {
            this.overwriteList = overwriteList;
        }

        private readonly bool overwriteList;

        public object Read(ref ProtoReader.State state, object value)
        {
            return state.AppendBytes(overwriteList ? default : (T)value);
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            state.WriteBytes((T)value);
        }

        bool IRuntimeProtoSerializerNode.RequiresOldValue { get { return !overwriteList; } }
        bool IRuntimeProtoSerializerNode.ReturnsValue { get { return true; } }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(nameof(ProtoWriter.State.WriteBytes), valueFrom, argType: typeof(T));
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            using var tmp = overwriteList ? default : ctx.GetLocalWithValue(typeof(T), entity);
            ctx.LoadState();
            if (overwriteList)
            {
                ctx.LoadNullRef();
            }
            else
            {
                ctx.LoadValue(tmp);
            }
            ctx.EmitCall(typeof(ProtoReader.State)
               .GetMethod(nameof(ProtoReader.State.AppendBytes),
               new[] { typeof(T)}));
        }
    }
}