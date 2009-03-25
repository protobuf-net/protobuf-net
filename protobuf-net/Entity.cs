using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
#if NET_3_0
using System.Runtime.Serialization;
#endif

namespace ProtoBuf
{
    class Entity
    {
        private static Dictionary<Type, Entity> cache
            = new Dictionary<Type, Entity>();

        public static bool IsEntity(Type type)
        {
            if(!type.IsClass || type.IsArray
                || type == typeof(void) || type == typeof(string)) return false;

            Entity e = Get(type);
            return e != null;
        }
        public static Entity Get(Type type) {
            Entity e;
            if(cache.TryGetValue(type, out e)) return e;

            e = Load(type);
            // check whether another thread also did the work while we weren't looking
#if !CF2
            Thread.MemoryBarrier();
#endif
            Dictionary<Type, Entity> cacheCpy = cache;
            Entity eFromOtherThread;
            if (cacheCpy.TryGetValue(type, out eFromOtherThread)) return eFromOtherThread;

            cacheCpy = new Dictionary<Type,Entity>(cacheCpy);
            cacheCpy.Add(type, e);
            cache = cacheCpy;
            return e;
        }

        private static Entity Load(Type type) {
            Attribute[] attribs = Attribute.GetCustomAttributes(type);
            bool isEntity = false;
            string name = null;
            for(int i = 0 ; i < attribs.Length ; i++)
            {                
                if(attribs[i] is ProtoContractAttribute)
                {
                    ProtoContractAttribute pca = (ProtoContractAttribute)attribs[i];
                    name = pca.Name;
                    isEntity = true;
                    break;
                }
            }                
#if NET_3_0
            if(!isEntity) {
                for (int i = 0; i < attribs.Length; i++)
                {
                    if (attribs[i] is DataContractAttribute)
                    {
                        DataContractAttribute dca = (DataContractAttribute)attribs[i];
                        name = dca.Name;
                        isEntity = true;
                        break;
                    }
                }
            }
#endif
            if(!isEntity) {
                for (int i = 0; i < attribs.Length; i++)
                {
                    if (attribs[i] is XmlTypeAttribute)
                    {
                        XmlTypeAttribute xta = (XmlTypeAttribute)attribs[i];
                        name = xta.TypeName;
                        isEntity = true;
                        break;
                    }
                }
            }
            return isEntity ? new Entity(type, name, null) : null;
        }


        private readonly Type type;
        private readonly string name;
        private readonly IList<EntityMember> members;
        public string Name { get { return name; } }
        public Type Type { get { return type; } }
        public IList<EntityMember> Members { get { return members; } }
        public Entity(Type type, string name, EntityMember[] members)
        {
            if (type == null) throw new ArgumentNullException("type");
            this.type = type;
            if (string.IsNullOrEmpty(name)) name = type.Name;
            this.name = name;
            if (members == null) members = GetMembers(type);
            this.members = new ReadOnlyCollection<EntityMember>(members);
        }
        public static EntityMember[] GetMembers(Type type)
        {
            return new EntityMember[0];
        }        
    }

    sealed class EntityMember
    {
        private readonly int tag;
        public int Tag {get {return tag;}}
        public EntityMember(int tag)
        {
            this.tag = tag;
        }
    }
}
