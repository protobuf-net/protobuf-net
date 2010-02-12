using System;
using System.IO;

namespace ProtoBuf.Meta
{
    public abstract class TypeModel
    {
        public void Serialize(Stream dest, object value)
        {
            if (value == null) throw new ArgumentNullException("value");
            int key = GetKey(value.GetType());
            using (ProtoWriter writer = new ProtoWriter(dest, this))
            {
                Serialize(key, value, writer);
            }
        }
        public object Deserialize(Stream source, Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            int key = GetKey(type);
            using (ProtoReader reader = new ProtoReader(source, this))
            {
                return Deserialize(key, reader);
            }
        }
        public static RuntimeTypeModel Create(string name)
        {
            return new RuntimeTypeModel(name, false);
        }
        protected abstract int GetKey(Type type);
        protected internal abstract void Serialize(int key, object value, ProtoWriter dest);
        protected internal abstract object Deserialize(int key, ProtoReader source);

        //internal ProtoSerializer Create(IProtoSerializer head)
        //{
        //    return new RuntimeSerializer(head, this);
        //}
        //internal ProtoSerializer Compile
    }

}
