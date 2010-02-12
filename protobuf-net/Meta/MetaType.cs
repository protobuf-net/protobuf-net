using System;
using System.Collections;
using System.Reflection;
using ProtoBuf.Serializers;

namespace ProtoBuf.Meta
{
    public class MetaType
    {
        
        internal MetaType(Type type)
        {
            if (type == null) throw new ArgumentNullException("type");
            if (type.IsPrimitive) throw new ArgumentException("Not valid for primitive types", "type");
            this.type = type;
        }
        protected void ThrowIfFrozen()
        {
            if (serializer != null) throw new InvalidOperationException("The type cannot be changed once a serializer has been generated");
        }
        private readonly Type type;
        public Type Type { get { return type; } }
        private IProtoSerializer serializer;
        internal IProtoSerializer Serializer {
            get {
                if (serializer == null) serializer = BuildSerializer();
                return serializer;
            }
        }
        private IProtoSerializer BuildSerializer()
        {
            fields.Trim();
            int count = fields.Count;
            int[] fieldNumbers = new int[count];
            IProtoSerializer[] serializers = new IProtoSerializer[count];
            int i = 0;
            foreach(ValueMember member in fields) {
                fieldNumbers[i] = member.FieldNumber;
                serializers[i++] = member.Serializer;
            }
            return new TypeSerializer(type, fieldNumbers, serializers);
        }

        internal void ApplyAttributes()
        {
            throw new NotImplementedException();
        }

        public MetaType Add(int fieldNumber, string memberName)
        {
            MemberInfo[] members = type.GetMember(memberName);
            if(members == null || members.Length != 1) throw new ArgumentException("Unable to determine member: " + memberName, "memberName");
            ValueMember member = new ValueMember(type, fieldNumber, members[0]);
            lock (fields)
            {
                ThrowIfFrozen();
                fields.Add(member);
            }
            return this;
        }

        private readonly BasicList fields = new BasicList();

        IEnumerable GetFields() { return fields; }

#if !FX11
        internal void CompileInPlace()
        {
            serializer = CompiledSerializer.Wrap(Serializer);
        }
#endif
    }
}
