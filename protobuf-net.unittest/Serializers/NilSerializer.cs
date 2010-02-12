using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf.Compiler;
using NUnit.Framework;
using ProtoBuf.unittest.Serializers;

namespace ProtoBuf.Serializers
{
    [TestFixture]
    public class NilTests
    {
        [Test]
        public void NilShouldAddNothing() {
            Util.Test("123", nil => nil, "");
        }
    }
    sealed class NilSerializer : IProtoSerializer
    {
        private readonly Type type;

        Type IProtoSerializer.ExpectedType
        {
            get { return type; }
        }
        public NilSerializer(Type type) { this.type = type; }
        void IProtoSerializer.Write(object value, ProtoWriter dest) { }

        void IProtoSerializer.Write(CompilerContext ctx)
        {
            ctx.DiscardValue();
        }
    }
}
