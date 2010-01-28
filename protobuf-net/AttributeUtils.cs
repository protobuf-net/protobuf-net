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
		internal static Attribute GetAttribute(MemberInfo member, string fullName) {
			foreach(Attribute attrib in Attribute.GetCustomAttributes(member)) {
				if(attrib.GetType().FullName == fullName) {
					return attrib;	
				}
			}
			return null;			                                  
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

		public static T GetValue<T>(Attribute attribute, string propertyName)
		{
			PropertyInfo prop = attribute.GetType().GetProperty(propertyName);
			if(prop == null) throw new ArgumentException("Missing property: " + propertyName);
			return (T)prop.GetValue(attribute, null);
		}
    }
}
