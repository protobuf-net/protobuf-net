#if !NO_RUNTIME
using System;

namespace ProtoBuf.Serializers
{
    internal sealed class CharSerializer : UInt16Serializer
    {
        private static readonly Type expectedType = typeof(char);

        public override Type ExpectedType => expectedType;

        public override void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            ProtoWriter.WriteUInt16((ushort)(char)value, dest, ref state);
        }

        public override object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // since replaces
            return (char)source.ReadUInt16(ref state);
        }

        // no need for any special IL here; ushort and char are
        // interchangeable as long as there is no boxing/unboxing
    }
}
#endif