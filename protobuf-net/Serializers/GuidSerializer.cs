//using System;

//namespace ProtoBuf
//{
//    internal sealed class GuidSerializer : ISerializer<Guid>
//    {
//        public static readonly GuidSerializer Default = new GuidSerializer();
//        private GuidSerializer() { }
//        Guid ISerializer<Guid>.Deserialize(Guid value, SerializationContext context)
//        {
//            int len = Base128Variant.DecodeInt32(context);
//            switch (len)
//            {
//                case 0:
//                    return Guid.Empty;
//                case 16:
//                    BlobSerializer.ReadBlock(context, 16);
//                    byte[] buffer = new byte[16];
//                    Buffer.BlockCopy(context.Workspace, 0, buffer, 0, 16);
//                    return new Guid(buffer);
//                default:
//                    throw new ProtoException("Invalid Guid length: " + len.ToString());
//            }            
//        }

//        int ISerializer<Guid>.Serialize(Guid value, SerializationContext context)
//        {
//            if (value == Guid.Empty)
//            {
//                context.Stream.WriteByte(0);
//                return 1;
//            }
//            byte[] buffer = value.ToByteArray();
//            if (buffer.Length != 16) throw new ProtoException("Guid length of 16 bytes expected");
//            context.Workspace[0] = 16;
//            Buffer.BlockCopy(buffer, 0, context.Workspace, 1, 16);
//            context.Stream.Write(context.Workspace, 0, 17);
//            return 17;

//        }

//        int ISerializer<Guid>.GetLength(Guid value, SerializationContext context)
//        {
//            return value == Guid.Empty ? 1 : 17;
//        }

//        WireType ISerializer<Guid>.WireType
//        {
//            get { return WireType.String; }
//        }

//        string ISerializer<Guid>.DefinedType
//        {
//            get { return ProtoFormat.BYTES; }
//        }
//    }
//}
