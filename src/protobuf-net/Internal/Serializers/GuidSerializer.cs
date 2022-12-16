using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class GuidSerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => _variant switch
        {
            Variant.GuidString => true,
            Variant.GuidBytes => true,
            _ => false,
        };

        private enum Variant
        {
            BclGuid = 0,
            GuidString = 1,
            GuidBytes = 2,
        }
        private readonly Variant _variant;
        private static GuidSerializer s_Legacy, s_String, s_Bytes;

        internal static GuidSerializer Create(CompatibilityLevel compatibilityLevel, DataFormat dataFormat)
        {
            if (compatibilityLevel < CompatibilityLevel.Level300)
                return s_Legacy ??= new GuidSerializer(Variant.BclGuid);
            if (dataFormat == DataFormat.FixedSize)
                return s_Bytes ??= new GuidSerializer(Variant.GuidBytes);
            return s_String ??= new GuidSerializer(Variant.GuidString);
        }

        private GuidSerializer(Variant variant) => _variant = variant;

        private static readonly Type expectedType = typeof(Guid);

        public Type ExpectedType { get { return expectedType; } }

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public void Write(ref ProtoWriter.State state, object value)
        {
            switch (_variant)
            {
                case Variant.GuidString:
                    BclHelpers.WriteGuidString(ref state, (Guid)value);
                    break;
                case Variant.GuidBytes:
                    BclHelpers.WriteGuidBytes(ref state, (Guid)value);
                    break;
                default:
                    BclHelpers.WriteGuid(ref state, (Guid)value);
                    break;
            }
        }

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return _variant switch
            {
                Variant.GuidString => BclHelpers.ReadGuidString(ref state),
                Variant.GuidBytes => BclHelpers.ReadGuidBytes(ref state),
                _ => BclHelpers.ReadGuid(ref state),
            };
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(
                _variant switch {
                    Variant.GuidString => nameof(BclHelpers.WriteGuidString),
                    Variant.GuidBytes => nameof(BclHelpers.WriteGuidBytes),
                    _ => nameof(BclHelpers.WriteGuid),
                }, valueFrom, typeof(BclHelpers));
        }

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(typeof(BclHelpers),
                _variant switch
                {
                    Variant.GuidString => nameof(BclHelpers.ReadGuidString),
                    Variant.GuidBytes => nameof(BclHelpers.ReadGuidBytes),
                    _ => nameof(BclHelpers.ReadGuid),
                }, ExpectedType);
        }
    }
}