using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf.Property
{
    internal static class PropertyUtil<T>
    {
        public static Property<T> CreatePropertyMessageGroup<TValueBase, TValueActual>()
            where TValueBase : class
            where TValueActual : class, TValueBase
        {
            return new PropertyMessageGroup<T, TValueBase, TValueActual>();
        }
        public static Property<T> CreatePropertyMessageString<TValueBase, TValueActual>()
            where TValueBase : class
            where TValueActual : class, TValueBase
        {
            return new PropertyMessageString<T, TValueBase, TValueActual>();
        }
        
        public static Property<T> CreatePropertyNullable<TValue>() where TValue : struct
        {
            return new PropertyNullable<T, TValue>();
        }

        public static Property<T> CreatePropertyList<TList, TValue>()
            where TList : ICollection<TValue>
        {
            return new PropertyList<T, TList, TValue>();
        }

        public static Property<T> CreatePropertyEnumerable<TList, TValue>()
            where TList : IEnumerable<TValue>
        {
            return new PropertyEnumerable<T, TList, TValue>();
        }

        public static Property<T> CreatePropertyEnum<TEnum>()
            where TEnum : struct
        {
            return new PropertyEnum<T, TEnum>();
        }

        public static Property<T> CreatePropertyPairString<TKey,TValue>()
        {
            return new PropertyPairString<T, TKey, TValue>();
        }

        public static Property<T> CreatePropertyArray<TValue>()
        {
            return new PropertyArray<T, TValue>();
        }
        public static Property<T> CreatePropertyParseable<TValue>()
        {
            return new PropertyParseable<T, TValue>();
        }

        public static Property<T> CreateTypedProperty(string methodName, params Type[] typeArguments)
        {
            MethodInfo method = typeof(PropertyUtil<T>).GetMethod(methodName);
            if (method == null) throw new ArgumentException("Method not found: " + methodName, "methodName");
            return (Property<T>)method.MakeGenericMethod(typeArguments).Invoke(null, null);
        }
    }
}
