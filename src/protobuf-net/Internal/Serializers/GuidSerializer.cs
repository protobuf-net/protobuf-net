using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class GuidSerializer : IRuntimeProtoSerializerNode
    {
        private readonly bool _asString;
        private static GuidSerializer s_String, s_Legacy;
        internal static GuidSerializer Create(CompatibilityLevel compatibilityLevel)
            => compatibilityLevel >= CompatibilityLevel.Level240
            ? s_String ??= new GuidSerializer(true)
            : s_Legacy ??= new GuidSerializer(false);

        private GuidSerializer(bool asString) => _asString = asString;

        private static readonly Type expectedType = typeof(Guid);

        public Type ExpectedType { get { return expectedType; } }

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public void Write(ref ProtoWriter.State state, object value)
        {
            if (_asString)
            {
                BclHelpers.WriteGuidBytes(ref state, (Guid)value);
            }
            else
            {
                BclHelpers.WriteGuid(ref state, (Guid)value);
            }
        }

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value == null); // since replaces
            return _asString ? BclHelpers.ReadGuidBytes(ref state) : BclHelpers.ReadGuid(ref state);
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(
                _asString ? nameof(BclHelpers.WriteGuidBytes) : nameof(BclHelpers.WriteGuid), valueFrom, typeof(BclHelpers));
        }

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(typeof(BclHelpers),
                _asString ? nameof(BclHelpers.ReadGuidBytes) : nameof(BclHelpers.ReadGuid), ExpectedType);
        }
    }
}