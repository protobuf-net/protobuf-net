//using System;

//namespace ProtoBuf
//{
//    internal sealed class NullableSerializer<T> : ISerializer<T?>, IGroupSerializer<T?> where T : struct
//    {
//        private readonly ISerializer<T> innerSerializer;
//        private readonly IGroupSerializer<T> innerGroupSerializer;

//        public IGroupSerializer<T> GroupSerializer
//        {
//            get
//            {
//                if (innerGroupSerializer == null)
//                {
//                    throw new ProtoException("Cannot [de]serialize nullable-of-" + typeof(T).Name + " as a group");
//                }
//                return innerGroupSerializer;
//            }
//        }

//        public NullableSerializer(ISerializer<T> innerSerializer)
//        {
//            if (innerSerializer == null)
//            {
//                throw new ArgumentNullException("innerSerializer");
//            }
//            this.innerSerializer = innerSerializer;
//            this.innerGroupSerializer = innerSerializer as IGroupSerializer<T>;
//        }

//        public int GetLength(T? value, SerializationContext context)
//        {
//            return value.HasValue ?
//                innerSerializer.GetLength(value.GetValueOrDefault(), context) : 0;
//        }
//        public int GetLengthGroup(T? value, SerializationContext context)
//        {
//            return value.HasValue ?
//                GroupSerializer.GetLengthGroup(value.GetValueOrDefault(), context) : 0;
//        }
//        public int Serialize(T? value, SerializationContext context)
//        {
//            return value.HasValue ?
//                innerSerializer.Serialize(value.GetValueOrDefault(), context) : 0;
//        }
//        public int SerializeGroup(T? value, SerializationContext context)
//        {
//            return value.HasValue ?
//                GroupSerializer.SerializeGroup(value.GetValueOrDefault(), context) : 0;
//        }

//        public T? Deserialize(T? value, SerializationContext context)
//        {
//            return innerSerializer.Deserialize(value.GetValueOrDefault(), context);
//        }
//        public T? DeserializeGroup(T? value, SerializationContext context)
//        {
//            return GroupSerializer.DeserializeGroup(value.GetValueOrDefault(), context);
//        }

//        public WireType WireType
//        {
//            get { return innerSerializer.WireType; }
//        }

//        public string DefinedType
//        {
//            get { return innerSerializer.DefinedType; }
//        }
//    }
//}
