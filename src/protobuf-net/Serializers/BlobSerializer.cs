using System;

namespace ProtoBuf.Serializers
{
    internal sealed class BlobSerializer : IRuntimeProtoSerializerNode
    {
        public Type ExpectedType { get { return expectedType; } }

        private static readonly Type expectedType = typeof(byte[]);

        public BlobSerializer(bool overwriteList)
        {
            this.overwriteList = overwriteList;
        }

        private readonly bool overwriteList;

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            return ProtoReader.AppendBytes(overwriteList ? null : (byte[])value, source, ref state);
        }

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteBytes((byte[])value, dest, ref state);
        }

        bool IRuntimeProtoSerializerNode.RequiresOldValue { get { return !overwriteList; } }
        bool IRuntimeProtoSerializerNode.ReturnsValue { get { return true; } }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteBytes", valueFrom, this);
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            if (overwriteList)
            {
                ctx.LoadNullRef();
            }
            else
            {
                ctx.LoadValue(entity);
            }
            ctx.LoadReader(true);
            ctx.EmitCall(typeof(ProtoReader)
               .GetMethod(nameof(ProtoReader.AppendBytes),
               new[] { typeof(byte[]), typeof(ProtoReader), Compiler.ReaderUtil.ByRefStateType}));
        }
    }
}