//using System;
//using ProtoBuf.Serializers;
//using ProtoBuf.Meta;

//namespace ProtoBuf
//{
//    internal sealed class RuntimeSerializer : ProtoSerializer
//    {
//        internal RuntimeSerializer(IProtoSerializer head, TypeModel model) : base(model)
//        {
//            if (head == null) throw new ArgumentNullException("head");
//            this.head = head;
//        }
//        private readonly IProtoSerializer head;
//        protected override int Serialize(object obj, ProtoWriter dest)
//        {
//            return head.Write(obj, dest);
//        }
//    }
//}
