using System;
using System.Reflection;

namespace ProtoBuf
{
    internal static class AttributeUtils
    {
        internal static T GetAttribute<T>(MemberInfo member) where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(member, typeof(T));
        }

        internal static T GetAttribute<T>(Type type) where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(type, typeof(T));
        }
        internal static T GetAttribute<T>(Type type, Predicate<T> predicate) where T : Attribute
        {
            if (predicate == null) throw new ArgumentNullException("predicate");
            foreach (T attrib in Attribute.GetCustomAttributes(type, typeof(T)))
            {
                if (predicate(attrib)) return attrib;
            }
            return null;
        }
    }
}
