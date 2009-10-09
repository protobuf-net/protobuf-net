using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;

namespace ProtoBuf.Property
{
    /// <summary>
    /// Utility class for creating/initializing protobuf-net property
    /// wrappers.
    /// </summary>
    internal static class PropertyFactory
    {

        internal static bool CanPack(WireType wireType)
        {
            switch (wireType)
            {
                case WireType.Fixed32:
                case WireType.Fixed64:
                case WireType.Variant:
                    return true;
                default:
                    return false;
            }
        }
        /// <summary>
        /// Stores, per T, a pass-thru Getter&lt;T,T&gt; delegate.
        /// </summary>
        private static class PassThruCache<T>
        {
            public static readonly Getter<T, T> Default = Get;
            private static T Get(T value) { return value; }
        }

        /// <summary>
        /// Returns a Getter&lt;T,T&gt; delegate that simply returns
        /// the original value. This allows code re-use between
        /// different implementations.
        /// </summary>
        /// <remarks>Originally an anonymous method was used, but
        /// this proved problematic with the Mono 2.0 compiler.</remarks>
        public static Getter<T,T> GetPassThru<T>()
        {
            return PassThruCache<T>.Default;
        }

        /// <summary>
        /// Create a simple Property that can be used standalone
        /// to encode/decode values for the given type.
        /// </summary>
        public static Property<T,T> CreatePassThru<T>(int tag, ref DataFormat format) {
            Property<T,T> prop = (Property<T, T>)CreateProperty<T>(typeof(T), ref format, MemberSerializationOptions.None);
            prop.Init(tag, format, GetPassThru<T>(), null, false, null);
            return prop;
        }

        internal static bool HasOption(MemberSerializationOptions options, MemberSerializationOptions required)
        {
            return (options & required) == required;
        }

        /// <summary>
        /// Create a Property based around a class
        /// member (PropertyInfo/FieldInfo).
        /// </summary>
        public static Property<T> Create<T>(MemberInfo member)
        {
            if (member == null) throw new ArgumentNullException("member");
            int tag;
            string name;
            DataFormat format;
            MemberSerializationOptions options;
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
            if(type == null || !Serializer.TryGetTag(member, out tag, out name, out format, out options))
            {
                throw new InvalidOperationException("Cannot be treated as a proto member: " + member.Name);
            }
            Property<T> prop;
            PropertyInfo specifiedProp = HasOption(options, MemberSerializationOptions.Required) ? null : PropertySpecified.GetSpecified(typeof (T), member.Name);
            
            if (specifiedProp != null)
            {
                prop = PropertyUtil<T>.CreateTypedProperty("CreatePropertySpecified", type);
                ((IPropertySpecified)prop).InitFromProperty(specifiedProp);
            }
            else
            {
                prop = CreateProperty<T>(type, ref format, options);
            }
            prop.Init(member);
            return prop;
        }

        /// <summary>
        /// Responsible for deciding how to encode/decode a given data-type; maybe
        /// not the most elegant solution, but it is simple and quick.
        /// </summary>
        private static Property<T> CreateProperty<T>(Type type, ref DataFormat format, MemberSerializationOptions options)
        {

            if (type.IsEnum)
            {
                if (format != DataFormat.Default && Attribute.IsDefined(type, typeof(FlagsAttribute)))
                {
                    type = Enum.GetUnderlyingType(type);
                }
                else
                {
                    format = DataFormat.TwosComplement;
                    return PropertyUtil<T>.CreateTypedProperty("CreatePropertyEnum", type);
                }
            }

            if (type == typeof(int))
            {
                switch (format)
                {
                    case DataFormat.Default:
                    case DataFormat.TwosComplement:
                        format = DataFormat.TwosComplement;
                        return new PropertyInt32Variant<T>();
                    case DataFormat.ZigZag:
                        return new PropertyInt32ZigZag<T>();
                    case DataFormat.FixedSize:
                        return new PropertyInt32Fixed<T>();
                }
            }
            if (type == typeof(short))
            {
                switch (format)
                {
                    case DataFormat.Default:
                    case DataFormat.TwosComplement:
                        format = DataFormat.TwosComplement;
                        return new PropertyInt16Variant<T>();
                    case DataFormat.ZigZag:
                        return new PropertyInt16ZigZag<T>();
                }
            }
            if (type == typeof(long))
            {
                switch (format)
                {
                    case DataFormat.Default:
                    case DataFormat.TwosComplement:
                        format = DataFormat.TwosComplement;
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
                        format = DataFormat.TwosComplement;
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
                        format = DataFormat.TwosComplement;
                        return new PropertyUInt64Variant<T>();
                    case DataFormat.FixedSize:
                        return new PropertyUInt64Fixed<T>();
                }
            }

            if (type == typeof(ushort))
            {
                switch (format)
                {
                    case DataFormat.Default:
                    case DataFormat.TwosComplement:
                        format = DataFormat.TwosComplement;
                        return new PropertyUInt16Variant<T>();
                }
            }

            if (type == typeof(byte[])) { format = DataFormat.Default; return new PropertyBlob<T>(); }
            if (type == typeof(byte)) { format = DataFormat.TwosComplement; return new PropertyByte<T>(); }
            if (type == typeof(sbyte)) { format = DataFormat.ZigZag; return new PropertySByte<T>();}
            if (type == typeof(char)) { format = DataFormat.TwosComplement; return new PropertyChar<T>();}      
            if (type == typeof(bool)) { format = DataFormat.TwosComplement; return new PropertyBoolean<T>();} 
            if (type == typeof(string)) { format = DataFormat.Default; return new PropertyString<T>();}
            if (type == typeof(float)) { format = DataFormat.FixedSize; return new PropertySingle<T>();}
            if (type == typeof(double)) { format = DataFormat.FixedSize; return new PropertyDouble<T>(); }
            if (type == typeof(Uri)) { format = DataFormat.Default; return new PropertyUri<T>();}

            if (type == typeof(Guid))
            {
                switch (format)
                {
                    case DataFormat.Group: return new PropertyGuidGroup<T>();
                    case DataFormat.Default: return new PropertyGuidString<T>();
                }
            }
            if (type == typeof(TimeSpan))
            {
                switch (format)
                {
                    case DataFormat.Group: return new PropertyTimeSpanGroup<T>();
                    case DataFormat.Default: return new PropertyTimeSpanString<T>();
                    case DataFormat.FixedSize: return new PropertyTimeSpanFixed<T>();
                }
            }
            if (type == typeof(DateTime))
            {
                switch (format)
                {
                    case DataFormat.Group: return new PropertyDateTimeGroup<T>();
                    case DataFormat.Default: return new PropertyDateTimeString<T>();
                    case DataFormat.FixedSize: return new PropertyDateTimeFixed<T>();
                }
            }
            if (type == typeof(decimal))
            {
                switch (format)
                {
                    case DataFormat.Group: return new PropertyDecimalGroup<T>();
                    case DataFormat.Default: return new PropertyDecimalString<T>();
                }
            }

            
            if (Serializer.IsEntityType(type))
            {
                Type baseType = type;
                while (Serializer.IsEntityType(baseType.BaseType))
                {
                    baseType = baseType.BaseType;
                }
                switch (format)
                {
                    case DataFormat.Default: return PropertyUtil<T>.CreateTypedProperty("CreatePropertyMessageString", type, baseType, baseType);
                    case DataFormat.Group: return PropertyUtil<T>.CreateTypedProperty("CreatePropertyMessageGroup", type, baseType, baseType);
                }
            }

            if (type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                return PropertyUtil<T>.CreateTypedProperty("CreatePropertyPairString", type.GetGenericArguments());
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

            if (listItemType != null && listItemType != typeof(byte[]))
            {
                bool dummy;
                if (GetListType(listItemType, out dummy) != null)
                {
                    throw new NotSupportedException("Nested (jagged) arrays/lists are not supported (except for byte[]); consider an array/list of a class-type with an inner array/list instead");
                }
            }


            if (type == typeof(byte[]))
            {   // want to treat byte[] as a special case
                listItemType = null;
            }
            if (type.IsArray && listItemType != null) // second check is for byte[]
            {
                return PropertyUtil<T>.CreateTypedProperty(
                    (PropertyFactory.HasOption(options, MemberSerializationOptions.Packed)
                        ? "CreatePropertyPackedArray" : "CreatePropertyArray"), listItemType);
            }

            if (listItemType != null)
            {
                if (isEnumerableOnly)
                {
                    if (GetAddMethod(type, listItemType) != null)
                    {
                        return PropertyUtil<T>.CreateTypedProperty(
                            (PropertyFactory.HasOption(options, MemberSerializationOptions.Packed)
                            ? "CreatePropertyPackedEnumerable" : "CreatePropertyEnumerable"), type, listItemType);
                    }
                }
                else
                {
                    return PropertyUtil<T>.CreateTypedProperty(
                        (PropertyFactory.HasOption(options, MemberSerializationOptions.Packed)
                        ? "CreatePropertyPackedList" : "CreatePropertyList"), type, listItemType);
                }
            }

            Type nullType = Nullable.GetUnderlyingType(type);
            if (nullType != null)
            {
                return PropertyUtil<T>.CreateTypedProperty("CreatePropertyNullable", nullType);
            }

            if (format == DataFormat.Default && GetParseMethod(type) != null)
            {
                return PropertyUtil<T>.CreateTypedProperty("CreatePropertyParseable", type);
            }

            throw Serializer.ThrowNoEncoder(format, type);

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
                    == typeof(ICollection<>))
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

        internal static void VerifyCanPack(WireType wireType)
        {
            if (!CanPack(wireType)) throw new InvalidOperationException("Only simple data-types can use packed encoding");
        }
    }
}
