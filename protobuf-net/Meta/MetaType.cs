#if !NO_RUNTIME
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
            return Add(fieldNumber, memberName, null, null);
        }
        public MetaType Add(int fieldNumber, string memberName, Type itemType, Type defaultType)
        {
            MemberInfo[] members = type.GetMember(memberName);
            if(members == null || members.Length != 1) throw new ArgumentException("Unable to determine member: " + memberName, "memberName");
            MemberInfo mi = members[0];
            Type miType;
            switch (mi.MemberType)
            {
                case MemberTypes.Field:
                    miType = ((FieldInfo)mi).FieldType; break;
                case MemberTypes.Property:
                    miType = ((PropertyInfo)mi).PropertyType; break;
                default:
                    throw new NotSupportedException();
            }
            if (itemType == null) { itemType = ListDecorator.GetItemType(miType); }

            if (itemType != null && defaultType == null)
            {
                if (miType.IsClass && !miType.IsAbstract && miType.GetConstructor(Helpers.EmptyTypes) != null)
                {
                    defaultType = miType;
                }
                if (defaultType == null)
                {
                    if (miType.IsInterface)
                    {
                        defaultType = typeof(System.Collections.Generic.List<>).MakeGenericType(itemType);
                    }
                }
                // verify that the default type is appropriate
                if (defaultType != null && !miType.IsAssignableFrom(defaultType)) defaultType = null;
            }
            ValueMember member = new ValueMember(type, fieldNumber, mi, miType, itemType, defaultType);
            lock (fields)
            {
                ThrowIfFrozen();
                fields.Add(member);
            }
            return this;
        }
        public ValueMember this[int fieldNumber]
        {
            get
            {
                foreach (ValueMember member in fields)
                {
                    if (member.FieldNumber == fieldNumber) return member;
                }
                return null;
            }
        }
        private readonly BasicList fields = new BasicList();

        //IEnumerable GetFields() { return fields; }

#if FEAT_COMPILER && !FX11
        internal void CompileInPlace()
        {
            serializer = CompiledSerializer.Wrap(Serializer);
        }
#endif
    }
}
#endif