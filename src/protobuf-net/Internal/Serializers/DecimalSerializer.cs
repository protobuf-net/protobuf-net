using System;
using System.Diagnostics;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class DecimalSerializer : IRuntimeProtoSerializerNode
    {
        bool IRuntimeProtoSerializerNode.IsScalar => _variant == Variant.String;

        private enum Variant
        {
            BclDecimal,
            String
        }

        private static DecimalSerializer s_BclDecimal, s_String;

        public static DecimalSerializer Create(CompatibilityLevel compatibilityLevel)
        {
            if (compatibilityLevel < CompatibilityLevel.Level300)
                return s_BclDecimal ??= new DecimalSerializer(Variant.BclDecimal);
            return s_String ??= new DecimalSerializer(Variant.String);
        }

        private readonly Variant _variant;
        private DecimalSerializer(Variant variant) => _variant = variant;

        private static readonly Type expectedType = typeof(decimal);

        public Type ExpectedType => expectedType;

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        public object Read(ref ProtoReader.State state, object value)
        {
            Debug.Assert(value is null); // since replaces
            return _variant switch
            {
                Variant.String => BclHelpers.ReadDecimalString(ref state),
                _ => BclHelpers.ReadDecimal(ref state),
            };
        }

        public void Write(ref ProtoWriter.State state, object value)
        {
            switch (_variant)
            {
                case Variant.String:
                    BclHelpers.WriteDecimalString(ref state, (decimal)value);
                    break;
                default:
                    BclHelpers.WriteDecimal(ref state, (decimal)value);
                    break;
            }
        }

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitStateBasedWrite(_variant switch
            {
                Variant.String => nameof(BclHelpers.WriteDecimalString),
                _ => nameof(BclHelpers.WriteDecimal),
            }, valueFrom, typeof(BclHelpers));
        }
        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ctx.EmitStateBasedRead(typeof(BclHelpers), _variant switch
            {
                Variant.String => nameof(BclHelpers.ReadDecimalString),
                _ => nameof(BclHelpers.ReadDecimal),
            }, ExpectedType);
        }
    }
}