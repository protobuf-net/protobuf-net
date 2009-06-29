using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ProtoBuf
{
    internal abstract class UnexpectedDataException : Exception
    {
        private readonly uint prefix;
        public UnexpectedDataException(uint prefix)
            : base("Unexpected data found during deserialization")
            { this.prefix = prefix; }

        public int Serialize(IExtension extension)
        {
            if (extension == null) return 0;
            Stream stream = extension.BeginAppend();
            try
            {
                SerializationContext ctx = new SerializationContext(stream, null);
                int len = Serialize(ctx);
                ctx.Flush();
                extension.EndAppend(stream, true);
                return len;
            }
            catch
            {
                extension.EndAppend(stream, false);
                throw;
            }
        }
        public abstract int Serialize(SerializationContext context);
        protected int WritePrefix(SerializationContext context) {
            return prefix == 0 ? 0 : context.EncodeUInt32(prefix);
        }
    }
    internal sealed class UnexpectedEnumException : UnexpectedDataException
    {
        private readonly int wireValue;
        public UnexpectedEnumException(uint prefix, int wireValue)
            : base(prefix)
        {
            this.wireValue = wireValue;
        }
        public override int Serialize(SerializationContext context)
        {
            return WritePrefix(context) + context.EncodeInt32(wireValue);
        }
    }
}
