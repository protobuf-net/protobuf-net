using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using System.Diagnostics;
using ProtoBuf.ProtoBcl;

namespace ProtoBuf
{
    internal static class Serializer<T> where T : class, new()
    {
#if !CF
        public static string GetProto()
        {
            if (props == null) Build();
            List<Type> types = new List<Type>();
            WalkTypes(types);

            StringBuilder sb = new StringBuilder();
            string ns = typeof(T).Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                sb.Append("package ").Append(ns).AppendLine(";");
            }
            foreach (Type type in types)
            {
                typeof(Serializer<>)
                    .MakeGenericType(type)
                    .GetMethod("AppendProto")
                    .Invoke(null, new object[] { sb, 0 });
            }
            return sb.ToString();
        }
        
        public static void WalkTypes(List<Type> knownTypes)
        {
            Type newType = typeof(T);
            if (knownTypes.Contains(newType)) return;
            knownTypes.Add(newType);
            foreach (IProperty<T> prop in props)
            {
                bool dummy;
                Type propType = prop.PropertyType,
                    actualType = Nullable.GetUnderlyingType(propType)
                        ?? GetListType(propType, out dummy) ?? propType;

                //if (actualType == typeof(Guid))
                //{
                //    actualType = typeof(ProtoGuid);
                //}
                if (Serializer.IsEntityType(actualType))
                {
                    typeof(Serializer<>)
                        .MakeGenericType(actualType)
                        .GetMethod("WalkTypes")
                        .Invoke(null, new object[] { knownTypes });
                }
                

            }
        }

        internal static StringBuilder Indent(StringBuilder sb, int nestLevel)
        {
            return sb.Append(' ', nestLevel * 2);
        }

        public static void AppendProto(StringBuilder sb, int nestLevel)
        {
            string descText, name = Serializer.GetDefinedTypeName<T>();
            sb.AppendLine();
            Indent(sb, nestLevel).Append("message ").Append(name).Append(" {");

            DescriptionAttribute desc = AttributeUtils.GetAttribute<DescriptionAttribute>(typeof(T));
            descText = desc == null ? null : desc.Description;
            if (!string.IsNullOrEmpty(descText))
            {
                sb.Append(" //").Append(descText); // TODO: remove crlf
            }

            sb.AppendLine();
            nestLevel++;
            for (int i = 0; i < props.Length; i++)
            {
                IProperty<T> prop = props[i];
                Indent(sb, nestLevel).Append(' ')
                    .Append(prop.IsRepeated ? "repeated" :
                        (prop.IsRequired ? "required" : "optional"))
                    .Append(prop.IsGroup ? " group " : " ")
                    .Append(prop.DefinedType).Append(' ')
                    .Append(prop.Name).Append(" = ").Append(prop.Tag);

                object def = prop.DefaultValue;
                if (def != null)
                {
                    string defText = Convert.ToString(def, CultureInfo.InvariantCulture);
                    sb.Append(" [default = ").Append(defText).Append(" ]");
                }

                sb.Append(";");
                descText = prop.Description;
                if (!string.IsNullOrEmpty(descText))
                {
                    sb.Append(" //").Append(descText); // TODO: remove crlf
                }

                sb.AppendLine();
            }

            nestLevel--;
            Indent(sb, nestLevel).AppendLine("}");
        }

#endif
        private static IProperty<T>[] props;

        internal static void Build()
        {
            if (props != null) return;
            props = new IProperty<T>[0]; // to prevent recursion
            if (!Serializer.IsEntityType(typeof(T)))
            {
                throw new InvalidOperationException("Only concrete data-contract classes can be processed");
            }
            List<IProperty<T>> propList = new List<IProperty<T>>();
            object[] argsOnePropertyVal = new object[1];

            foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                string name;
                DataFormat format;
                int tag;
                bool isRequired, isGroup;
                if (!Serializer.TryGetTag(prop, out tag, out name, out format, out isRequired, out isGroup))
                {
                    continue; // didn't recognise this as a usable property
                }

                // check for duplicates
                foreach (IProperty<T> item in propList)
                {
                    if (item.Tag == tag)
                    {
                        throw new InvalidOperationException(
                            string.Format("Duplicate tag {0} detected in {1}", tag, name));
                    }
                }

                IProperty<T> actualProp;
                bool isEnumerableOnly;
                Type propType = prop.PropertyType, listItemType = GetListType(propType, out isEnumerableOnly);

                if (propType.IsArray)
                {
                    // verify that we can handle it
                    if (propType.GetArrayRank() != 1)
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
                }
                propList.Add(actualProp);
            }
            propList.Sort(delegate(IProperty<T> x, IProperty<T> y) { return x.Tag.CompareTo(y.Tag); });
            props = propList.ToArray();
        }

        private static bool IsSelfEquatable(Type type)
        {
            Type huntType = typeof(IEquatable<>).MakeGenericType(type);
            foreach (Type intType in type.GetInterfaces())
            {
                if (intType == huntType)
                {
                    return true;
                }
            }
            return false;
        }

        private static Type GetListType(Type type, out bool isEnumerableOnly)
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
        internal static int Serialize(T instance, Stream destination)
        {
            if (props == null) Build();
            if (instance == null) throw new ArgumentNullException("instance");
            if (destination == null) throw new ArgumentNullException("destination");
            SerializationContext ctx = new SerializationContext(destination);
            int len = Serialize(instance, ctx, null);
            ctx.CheckStackClean();
            destination.Flush();
            return len;
        }



        internal static int Serialize(T instance, SerializationContext context, IProperty<T>[] candidateProperties)
        {
            if (candidateProperties == null) candidateProperties = props;
            context.Push(instance);
            //context.CheckSpace();
            int total = 0, len;
            for (int i = 0; i < candidateProperties.Length; i++)
            {
                // note that this serialization includes the headers...
                total += candidateProperties[i].Serialize(instance, context);
            }
            IExtensible extra = instance as IExtensible;
            if (extra != null && (len = extra.GetLength()) > 0)
            {
                Stream extraStream = extra.BeginQuery();
                try
                {
                    context.WriteFrom(extraStream, len);
                    total += len;
                }
                finally
                {
                    extra.EndQuery(extraStream);
                }               
            }
            context.Pop(instance);
            return total;
        }

        internal static void Deserialize(T instance, Stream source)
        {
            if (props == null) Build();
            if (instance == null) throw new ArgumentNullException("instance");
            if (source == null) throw new ArgumentNullException("source");
            SerializationContext ctx = new SerializationContext(source);
            Deserialize(instance, ctx);
            ctx.CheckStackClean();
        }
        internal static void Deserialize(T instance, SerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            context.Push();
            int prefix, propCount = props.Length;
            //context.CheckSpace();
            IExtensible extra = instance as IExtensible;
            SerializationContext extraData = null;
            Stream extraStream = null;
            IProperty<T> prop = propCount == 0 ? null : props[0];
            
            int lastIndex = prop == null ? int.MinValue : 0,
                lastTag = prop == null ? int.MinValue : prop.Tag;
            
            try
            {
                while (context.IsDataAvailable && TwosComplementSerializer.TryReadInt32(context, out prefix))
                {
                    WireType wireType;
                    int fieldTag;
                    Serializer.ParseFieldToken(prefix, out wireType, out fieldTag);
                    if (wireType == WireType.EndGroup)
                    {
                        context.EndGroup(fieldTag);
                        break;
                    }
                    bool foundTag = fieldTag == lastTag;
                    if (!foundTag)
                    {
                        int index = lastIndex;

                        // start i at 1 as only need to check n-1 other properties
                        for (int i = 1; i < propCount; i++)
                        {
                            if (++index == propCount)
                            {
                                index = 0;
                            }

                            if (props[index].Tag == fieldTag)
                            {
                                prop = props[index];
                                lastIndex = index;
                                lastTag = fieldTag;
                                foundTag = true;
                                break;
                            }
                        }
                    }

                    if (foundTag)
                    {
                        if (wireType == WireType.StartGroup)
                        {
                            if (!prop.CanBeGroup)
                            {
                                throw new ProtoException("Group not expected: " + prop.Name);
                            }
                            // group-terminated; just deserialize
                            context.StartGroup(fieldTag);
                            prop.Deserialize(instance, context);
                        }
                        else if (prop.WireType == wireType)
                        {
                            if (wireType == WireType.String)
                            {
                                // length-prefixed; set the end of the stram and deserialize
                                int len = TwosComplementSerializer.ReadInt32(context);
                                long oldMaxPos = context.MaxReadPosition;
                                context.MaxReadPosition = context.Position + len;
                                prop.Deserialize(instance, context);
                                // restore the max-pos
                                context.MaxReadPosition = oldMaxPos;
                            }
                            else
                            {   // use the known serializer
                                prop.Deserialize(instance, context);
                            }
                        }
                        else {
                            // not what we were expecting!
                            throw new ProtoException(
                                string.Format(
                                    "Wire-type of {0} (tag {1}) did not match; expected {2}, received {3}",
                                    prop.Name,
                                    prop.Tag,
                                    prop.WireType,
                                    wireType));
                        }
                    }
                    else if (extra != null)
                    {
                        if (extraData == null)
                        {
                            extraStream = extra.BeginAppend();
                            extraData = new SerializationContext(extraStream);
                        }

                        // borrow the workspace, and copy the data
                        extraData.ReadFrom(context);
                        TwosComplementSerializer.WriteToStream(prefix, extraData);
                        ProcessExtraData(context, fieldTag, wireType, extraData);
                        context.ReadFrom(extraData);
                    }
                    else
                    {
                        // unexpected fields for an inextensible object; discard the data
                        Serializer.SkipData(context, fieldTag, wireType);
                    }
                }
                if (extraStream != null) extra.EndAppend(extraStream, true);
            }
            catch
            {
                if (extraStream != null) extra.EndAppend(extraStream, false);
                throw;
            }

            context.Pop();
        }


        

        private static void ProcessExtraData(SerializationContext read, int fieldTag, WireType wireType, SerializationContext write)
        {
            int len;
            switch (wireType)
            {
                case WireType.Variant:
                    len = Base128Variant.ReadRaw(read);
                    write.Write(read.Workspace, 0, len);
                    break;
                case WireType.Fixed32:
                    read.ReadBlock(4);
                    write.Write(read.Workspace, 0, 4);
                    break;
                case WireType.Fixed64:
                    read.ReadBlock(8);
                    write.Write(read.Workspace, 0, 8);
                    break;
                case WireType.String:
                    len = TwosComplementSerializer.ReadInt32(read);
                    TwosComplementSerializer.WriteToStream(len, write);
                    read.WriteTo(write, len);
                    break;
                case WireType.StartGroup:
                    using (CloneStream cloneStream = new CloneStream(read, write))
                    {
                        SerializationContext cloneCtx = new SerializationContext(cloneStream);
                        cloneCtx.ReadFrom(read);
                        cloneCtx.StartGroup(fieldTag);
                        UnknownType.Serializer.Deserialize(null, cloneCtx);
                        read.ReadFrom(cloneCtx);
                    }
                    break;
                case WireType.EndGroup:
                    throw new ProtoException("End-group not expected at this location");                
                default:
                    throw new ProtoException("Unknown wire-type " + wireType.ToString());
            }
        }

        
        

        

        public static IProperty<TEntity> CreateStructProperty<TEntity, TValue>(PropertyInfo property)
            where TEntity : class, new()
            where TValue : struct
        {
            return new StructProperty<TEntity, TValue>(property);
        }
        public static IProperty<TEntity> CreateEquatableProperty<TEntity, TValue>(PropertyInfo property)
            where TEntity : class, new()
            where TValue : struct, IEquatable<TValue>
        {
            return new EquatableProperty<TEntity, TValue>(property);
        }

        public static IProperty<TEntity> CreateClassProperty<TEntity, TValue>(PropertyInfo property)
            where TEntity : class, new()
            where TValue : class
        {
            return new ClassProperty<TEntity, TValue>(property);
        }

        public static IProperty<TEntity> CreateNullableProperty<TEntity, TValue>(PropertyInfo property)
            where TEntity : class, new()
            where TValue : struct
        {
            return new NullableProperty<TEntity, TValue>(property);
        }

        public static IProperty<TEntity> CreateEntityProperty<TEntity, TValue>(PropertyInfo property)
            where TEntity : class, new()
            where TValue : class, new()
        {
            return new EntityProperty<TEntity, TValue>(property);
        }
        public static IProperty<TEntity> CreateArrayProperty<TEntity, TValue>(PropertyInfo property)
            where TEntity : class, new()
        {
            return new ArrayProperty<TEntity, TValue>(property);
        }
        public static IProperty<TEntity> CreateEnumerableProperty<TEntity, TList, TValue>(PropertyInfo property)
            where TEntity : class, new()
            where TList : class, IEnumerable<TValue>
        {
            return new EnumerableProperty<TEntity, TList, TValue>(property);
        }
        public static IProperty<TEntity> CreateListProperty<TEntity, TList, TValue>(PropertyInfo property)
            where TEntity : class, new()
            where TList : class, IList<TValue>
        {
            return new ListProperty<TEntity, TList, TValue>(property);
        }

        internal static void CheckTagNotInUse(int tag)
        {
            if (tag <= 0) throw new ArgumentOutOfRangeException("tag", "Tags must be positive integers.");
            foreach (IProperty<T> prop in props)
            {
                if (prop.Tag == tag) throw new ArgumentException(
                    string.Format("Tag {0} is in use; access the {1} property instead.", tag, prop.Name), "tag");
            }
        }


    }
}
