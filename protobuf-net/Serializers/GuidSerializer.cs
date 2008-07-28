using System;

namespace ProtoBuf.Serializers
{
    internal sealed class GuidSerializer : ISerializer<Guid>
    {
        public static readonly GuidSerializer Default = new GuidSerializer();
        private GuidSerializer() { }
        Guid ISerializer<Guid>.Deserialize(Guid value, SerializationContext context)
        {
            BlobSerializer.ReadBlock(context, 17);
            if (context.Workspace[0] != 16) throw new ProtoException("Guid length of 16 bytes expected");
            byte[] buffer = new byte[16];
            Buffer.BlockCopy(context.Workspace, 1, buffer, 0, 16);
            return new Guid(buffer);
        }

        int ISerializer<Guid>.Serialize(Guid value, SerializationContext context)
        {
            if (value == Guid.Empty) return 0;
            byte[] buffer = value.ToByteArray();
            if (buffer.Length != 16) throw new ProtoException("Guid length of 16 bytes expected");
            context.Workspace[0] = 16;
            Buffer.BlockCopy(buffer, 0, context.Workspace, 1, 16);
            context.Stream.Write(context.Workspace, 0, 17);
            return 17;

        }

        int ISerializer<Guid>.GetLength(Guid value, SerializationContext context)
        {
            return value == Guid.Empty ? 0 : 17;
        }

        WireType ISerializer<Guid>.WireType
        {
            get { return WireType.String; }
        }

        string ISerializer<Guid>.DefinedType
        {
            get { return ProtoFormat.BYTES; }
        }
    }
}
