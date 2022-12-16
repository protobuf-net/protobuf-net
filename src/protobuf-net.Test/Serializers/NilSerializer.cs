#if !NO_INTERNAL_CONTEXT
using ProtoBuf.Compiler;
using ProtoBuf.Internal.Serializers;
using ProtoBuf.unittest.Serializers;
using System;
using Xunit;

namespace ProtoBuf.Serializers
{
    public class NilTests
    {
        [Fact]
        public void NilShouldAddNothing() {
            Util.Test("123", nil => nil, "");
        }
    }
    internal sealed class NilSerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => true;
        private readonly Type type;
        public bool ReturnsValue { get { return true; } }
        public bool RequiresOldValue { get { return true; } }
        public object Read(ref ProtoReader.State state, object value) { return value; }
        Type IRuntimeProtoSerializerNode.ExpectedType { get { return type; } }
        public NilSerializer(Type type) { this.type = type; }
        void IRuntimeProtoSerializerNode.Write(ref ProtoWriter.State state, object value) { }

        void IRuntimeProtoSerializerNode.EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            // burn the value off the stack if needed
            if (valueFrom == null) ctx.DiscardValue();
        }
        void IRuntimeProtoSerializerNode.EmitRead(CompilerContext ctx, Local entity)
        {
            throw new NotSupportedException();
        }
    }
}
#endif