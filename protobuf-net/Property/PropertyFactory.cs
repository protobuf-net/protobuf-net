using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf.Property
{
    internal static class PropertyFactory
    {
        static PropertyFactory()
        {

        }

        public static Getter<T,T> GetPassThru<T>()
        {
            return delegate(T value) { return value; };
        }

        public static Property<T, T> CreatePassThru<T>(MemberInfo member, bool isGroup)
        {
            if (member == null) throw new ArgumentNullException("member");
            int tag;
            string name;
            DataFormat format;
            bool isRequired, ignoreIsGroup;
            if (!Serializer.TryGetTag(member, out tag, out name, out format, out isRequired, out ignoreIsGroup))
            {
                throw new InvalidOperationException("Cannot be treated as a proto member: " + member.Name);
            }
            Property<T,T> prop = (Property<T, T>)CreateProperty<T>(typeof(T), format, isGroup);
            prop.Init(tag, GetPassThru<T>(), isGroup);
            return prop;
        }
        public static Property<T> Create<T>(MemberInfo member, out Property<T> alternative)
        {
            if (member == null) throw new ArgumentNullException("member");
            int tag;
            string name;
            DataFormat format;
            bool isRequired, isGroup;
            Type type;
            switch (member.MemberType)
            {
                case MemberTypes.Property:
                    type = ((PropertyInfo)member).PropertyType;
                    break;
                case MemberTypes.Field:
                    type = ((FieldInfo)member).FieldType;
                    break;
                default:
                    type = null;
                    break;
            }
            if(type == null || !Serializer.TryGetTag(member, out tag, out name, out format, out isRequired, out isGroup))
            {
                throw new InvalidOperationException("Cannot be treated as a proto member: " + member.Name);
            }

            Property<T> prop = CreateProperty<T>(type, format, isGroup);
            prop.Init(member, isGroup);
            alternative = null;
            try
            {
                alternative = CreateProperty<T>(type, format, !isGroup);
                alternative.Init(member, !isGroup);
                if (alternative.FieldPrefix == prop.FieldPrefix)
                {
                    alternative = null; // not interested unless it is different...
                }
            }
            catch (ProtoException) {
                //
            }
            
            return prop;
        }

        private static Property<T> CreateProperty<T>(Type type, DataFormat format, bool isGroup)
        {
            if (type == typeof(int))
            {
                switch (format)
                {
                    case DataFormat.Default:
                    case DataFormat.TwosComplement:
                        return new PropertyInt32Variant<T>();
                    case DataFormat.ZigZag:
                        return new PropertyInt32ZigZag<T>();
                    case DataFormat.FixedSize:
                        return new PropertyInt32Fixed<T>();
                }
            }
            if (type == typeof(long))
            {
                switch (format)
                {
                    case DataFormat.Default:
                    case DataFormat.TwosComplement:
                        return new PropertyInt64Variant<T>();
                    case DataFormat.ZigZag:
                        return new PropertyInt64ZigZag<T>();
                    case DataFormat.FixedSize:
                        return new PropertyInt64Fixed<T>();
                }
            }
            if (type == typeof(uint))
            {
                switch (format)
                {
                    case DataFormat.Default:
                    case DataFormat.TwosComplement:
                        return new PropertyUInt32Variant<T>();
                    case DataFormat.FixedSize:
                        return new PropertyUInt32Fixed<T>();
                }
            }
            if (type == typeof(ulong))
            {
                switch (format)
                {
                    case DataFormat.Default:
                    case DataFormat.TwosComplement:
                        return new PropertyUInt64Variant<T>();
                    case DataFormat.FixedSize:
                        return new PropertyUInt64Fixed<T>();
                }
            }
            if (type == typeof(short))
            {
                switch (format)
                {
                    case DataFormat.Default:
                    case DataFormat.TwosComplement:
                        return new PropertyInt16Variant<T>();
                    case DataFormat.ZigZag:
                        return new PropertyInt16ZigZag<T>();
                }
            }

            if (type == typeof(byte[])) return new PropertyBlob<T>();
            if (type == typeof(byte)) return new PropertyByte<T>();
            if (type == typeof(sbyte)) return new PropertySByte<T>();
            if (type == typeof(char)) return new PropertyChar<T>();            
            if (type == typeof(bool)) return new PropertyBoolean<T>();            
            if (type == typeof(string)) return new PropertyString<T>();
            if (type == typeof(float)) return new PropertySingle<T>();
            if (type == typeof(double)) return new PropertyDouble<T>();
            if (type == typeof(Uri)) return new PropertyUri<T>();

            if (type == typeof(Guid))
            {
                if (isGroup) return new PropertyGuidGroup<T>();
                else return new PropertyGuidString<T>();
            }
            if (type == typeof(TimeSpan))
            {
                if (isGroup) return new PropertyTimeSpanGroup<T>();
                else return new PropertyTimeSpanString<T>();
            }
            if (type == typeof(DateTime))
            {
                if (isGroup) return new PropertyDateTimeGroup<T>();
                else return new PropertyDateTimeString<T>();
            }
            if (type == typeof(decimal))
            {
                if (isGroup) return new PropertyDecimalGroup<T>();
                else return new PropertyDecimalString<T>();
            }

            
            if (Serializer.IsEntityType(type))
            {
                return PropertyUtil<T>.CreateTypedProperty(
                    isGroup ? "CreatePropertyMessageGroup" : "CreatePropertyMessageString", type);
            }

            if (type.IsEnum)
            {
                return PropertyUtil<T>.CreateTypedProperty("CreatePropertyEnum", type, Enum.GetUnderlyingType(type));
            }

            bool isEnumerableOnly;
            Type listItemType = GetListType(type, out isEnumerableOnly);

            if (type.IsArray)
            {
                // verify that we can handle it
                if (type.GetArrayRank() != 1)
                {
                    throw new NotSupportedException("Only 1-dimensional arrays can be used; consider an array/list of a class-type instead");
                }
            }

            if (listItemType != null)
            {
                bool dummy;
                if (GetListType(listItemType, out dummy) != null)
                {
                    throw new NotSupportedException("Nested (jagged) arrays/lists are not supported; consider an array/list of a class-type with an inner array/list instead");
                }
            }


            if (type == typeof(byte[]))
            {   // want to treat byte[] as a special case
                listItemType = null;
            }
            if (type.IsArray && listItemType != null) // second check is for byte[]
            {
                return PropertyUtil<T>.CreateTypedProperty("CreatePropertyArray", listItemType);
            }

            if (listItemType != null)
            {
                if (isEnumerableOnly)
                {
                    if (GetAddMethod(type, listItemType) != null)
                    {
                        return PropertyUtil<T>.CreateTypedProperty(
                            "CreatePropertyEnumerable", type, listItemType);
                    }
                }
                else
                {
                    return PropertyUtil<T>.CreateTypedProperty(
                        "CreatePropertyList", type, listItemType);
                }
            }

            Type nullType = Nullable.GetUnderlyingType(type);
            if (nullType != null)
            {
                return PropertyUtil<T>.CreateTypedProperty("CreatePropertyNullable", nullType);
            }

            if (GetParseMethod(type) != null)
            {
                return PropertyUtil<T>.CreateTypedProperty("CreatePropertyParseable", type);
            }

            throw Serializer.ThrowNoEncoder(format, type);

            /*
           argsOnePropertyVal[0] = prop;
           if (propType == typeof(byte[]))
           {   // want to treat byte[] as a special case
               listItemType = null;
           }
           if (propType.IsArray && listItemType != null) // second check is for byte[]
           {
               // array
               actualProp = (IProperty<T>)typeof(Serializer<T>)
                   .GetMethod("CreateArrayProperty")
                   .MakeGenericMethod(typeof(T), listItemType)
                   .Invoke(null, argsOnePropertyVal);
           }
           else if (listItemType != null)
           {
               // list / enumerable
               actualProp = (IProperty<T>)typeof(Serializer<T>)
                   .GetMethod(isEnumerableOnly ? "CreateEnumerableProperty" : "CreateListProperty")
                   .MakeGenericMethod(typeof(T), propType, listItemType)
                   .Invoke(null, argsOnePropertyVal);
           }
           else if (Serializer.IsEntityType(propType))
           { // entity
               actualProp = (IProperty<T>)typeof(Serializer<T>)
                   .GetMethod("CreateEntityProperty")
                   .MakeGenericMethod(typeof(T), propType)
                   .Invoke(null, argsOnePropertyVal);
           }
           else
           { // simple value
               string methodName;
               Type nullType = Nullable.GetUnderlyingType(propType);
               if (nullType != null)
               {
                   methodName = "CreateNullableProperty";
                   propType = nullType;
               }
               else if (propType.IsClass)
               {
                   methodName = "CreateClassProperty";
               }
               else if (IsSelfEquatable(propType))
               {
                   methodName = "CreateEquatableProperty";
               }
               else
               {
                   methodName = "CreateStructProperty";
               }
               actualProp = (IProperty<T>)typeof(Serializer<T>)
                   .GetMethod(methodName)
                   .MakeGenericMethod(typeof(T), propType)
                   .Invoke(null, argsOnePropertyVal);
           }*/
        }
        internal static MethodInfo GetParseMethod(Type type) {
            return type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static,
                null, new Type[] {typeof(string)}, null);
        }
        internal static MethodInfo GetAddMethod(Type listType, Type valueType)
        {
            return listType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public,
                null, new Type[] { valueType }, null);
        }
        internal static Type GetListType(Type type, out bool isEnumerableOnly)
        {
            isEnumerableOnly = false;
            if (type.IsArray)
            {
                return type.GetElementType();
            }
            foreach (Type interfaceType in type.GetInterfaces())
            {
                if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition()
                    == typeof(IList<>))
                {
                    return interfaceType.GetGenericArguments()[0];
                }
            }
            if (type != typeof(string))
            {
                foreach (Type interfaceType in type.GetInterfaces())
                {
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition()
                        == typeof(IEnumerable<>))
                    {
                        isEnumerableOnly = true;
                        return interfaceType.GetGenericArguments()[0];
                    }
                }
            }
            return null;
        }
    }
}
