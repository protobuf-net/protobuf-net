using System;
using System.Reflection;

namespace ProtoBuf
{
    static class AttributeUtils
    {
        internal static T GetAttribute<T>(PropertyInfo property) where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(property, typeof(T));
        }
        internal static T GetAttribute<T>(Type type) where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(type, typeof(T));
        }
    }
}
