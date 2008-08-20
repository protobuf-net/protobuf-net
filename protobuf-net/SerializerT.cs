using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using ProtoBuf.Property;

namespace ProtoBuf
{
    internal static class Serializer<T> where T : class, new()
    {
#if !CF
        public static string GetProto()
        {
            if (readProps == null) Build();
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
                if (type.IsEnum)
                {
                    typeof(Serializer<T>)
                        .GetMethod("AppendEnum")
                        .MakeGenericMethod(type)
                        .Invoke(null, new object[] { sb, 0 });
                }
                else
                {
                    typeof(Serializer<>)
                        .MakeGenericType(type)
                        .GetMethod("AppendProto")
                        .Invoke(null, new object[] { sb, 0 });
                }
            }
            return sb.ToString();
        }
        
        public static void WalkTypes(List<Type> knownTypes)
        {
            Type newType = typeof(T);
            if (knownTypes.Contains(newType)) return;
            knownTypes.Add(newType);
            foreach (Property<T> prop in writeProps)
            {
                bool dummy;
                Type propType = prop.PropertyType,
                    actualType = Nullable.GetUnderlyingType(propType)
                        ?? PropertyFactory.GetListType(propType, out dummy) ?? propType;

                if (Serializer.IsEntityType(actualType))
                {
                    typeof(Serializer<>)
                        .MakeGenericType(actualType)
                        .GetMethod("WalkTypes")
                        .Invoke(null, new object[] { knownTypes });
                }
                else if (actualType.IsEnum)
                {
                    if (!knownTypes.Contains(actualType))
                        knownTypes.Add(actualType);
                }
                

            }
        }

        internal static StringBuilder Indent(StringBuilder sb, int nestLevel)
        {
            return sb.Append(' ', nestLevel * 2);
        }
        public static void AppendEnum<TEnum>(StringBuilder sb, int nestLevel) where TEnum : struct
        {
            ProtoContractAttribute attrib = AttributeUtils.GetAttribute<ProtoContractAttribute>(typeof(TEnum));
            string name = attrib == null || string.IsNullOrEmpty(attrib.Name) ? typeof(TEnum).Name : attrib.Name;
            
            Indent(sb, nestLevel).Append("enum ").Append(name).Append(" {").AppendLine();
            foreach (Serializer.ProtoEnumValue<TEnum> value in Serializer.GetEnumValues<TEnum>())
            {
                Indent(sb, nestLevel + 1).Append(' ').Append(value.Name)
                    .Append(" = ").Append(value.WireValue).Append(";").AppendLine();
            }
            Indent(sb, nestLevel).Append("}").AppendLine();
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
            for (int i = 0; i < writeProps.Length; i++)
            {
                Property<T> prop = writeProps[i];
                Indent(sb, nestLevel).Append(' ')
                    .Append(prop.IsRepeated ? "repeated" :
                        (prop.IsOptional ? "optional" : "required"))
                    .Append(prop.IsGroup ? " group " : " ")
                    .Append(prop.DefinedType).Append(' ')
                    .Append(prop.Name).Append(" = ").Append(prop.Tag);

                object def = prop.DefaultValue;
                if (def != null)
                {
                    string defText = Convert.ToString(def, CultureInfo.InvariantCulture);
                    sb.Append(" [default = ").Append(defText).Append("]");
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
        private static Property<T>[] readProps, writeProps;

        internal static void Build()
        {
            if (readProps != null) return;
            try
            {
                readProps = new Property<T>[0]; // to prevent recursion
                if (!Serializer.IsEntityType(typeof(T)))
                {
                    throw new InvalidOperationException("Only concrete data-contract classes can be processed");
                }
                List<Property<T>> readPropList = new List<Property<T>>(), writePropList = new List<Property<T>>();

                foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    string name;
                    DataFormat format;
                    int tag;
                    bool isRequired; ;
                    if (!Serializer.TryGetTag(prop, out tag, out name, out format, out isRequired))
                    {
                        continue; // didn't recognise this as a usable property
                    }

                    // check for duplicates
                    foreach (Property<T> item in readPropList)
                    {
                        if (item.Tag == tag)
                        {
                            throw new InvalidOperationException(
                                string.Format("Duplicate tag {0} detected in {1}", tag, name));
                        }
                    }

                    Property<T> actualProp = PropertyFactory.Create<T>(prop);
                    writePropList.Add(actualProp);
                    readPropList.Add(actualProp);
                    foreach (Property<T> altProp in actualProp.GetCompatibleReaders())
                    {
                        if (altProp.Tag != actualProp.Tag)
                        {
                            throw new ProtoException("Alternative readers must handle the same tag");
                        }
                        foreach (Property<T> tmp in readPropList)
                        {
                            if (tmp.FieldPrefix == altProp.FieldPrefix)
                            {
                                throw new ProtoException("Alternative readers must handle different wire-types");
                            }
                        }
                        readPropList.Add(altProp);
                    }
                }
                readPropList.Sort(delegate(Property<T> x, Property<T> y) { return x.FieldPrefix.CompareTo(y.FieldPrefix); });
                writePropList.Sort(delegate(Property<T> x, Property<T> y) { return x.FieldPrefix.CompareTo(y.FieldPrefix); });
                readProps = readPropList.ToArray();
                writeProps = writePropList.ToArray();
            }
            catch
            {
                readProps = writeProps = null;
                throw;
            }
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

        


        internal static int Serialize(T instance, Stream destination)
        {
            if (readProps == null) Build();
            if (instance == null) throw new ArgumentNullException("instance");
            if (destination == null) throw new ArgumentNullException("destination");
            SerializationContext ctx = new SerializationContext(destination, null);
            int len = Serialize(instance, ctx);
            ctx.CheckStackClean();
            ctx.Flush();
            return len;
        }



        internal static int Serialize(T instance, SerializationContext context)
        {
            context.Push(instance);
            //context.CheckSpace();
            int total = 0, len;
            for (int i = 0; i < writeProps.Length; i++)
            {
                // note that this serialization includes the headers...
                total += writeProps[i].Serialize(instance, context);
            }
            IExtensible extensible = instance as IExtensible;
            IExtension extra = extensible == null ? null : extensible.GetExtensionObject(false);

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
            if (readProps == null) Build();
            if (instance == null) throw new ArgumentNullException("instance");
            if (source == null) throw new ArgumentNullException("source");
            SerializationContext ctx = new SerializationContext(source, null);
            Deserialize(instance, ctx);
            ctx.CheckStackClean();
        }
        internal static void Deserialize(T instance, SerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            context.Push();
            uint prefix;
            int propCount = readProps.Length;
            //context.CheckSpace();
            IExtensible extensible = instance as IExtensible;
            IExtension extn = null;
            SerializationContext extraData = null;
            Stream extraStream = null;
            Property<T> prop = propCount == 0 ? null : readProps[0];

            int lastIndex = prop == null ? -1 : 0;
            uint lastPrefix = prop == null ? uint.MaxValue : prop.FieldPrefix;
            
            try
            {
                while ((prefix = context.TryReadFieldPrefix()) > 0)
                {
                    // check for a lazy hit (mainly with collections)
                    if (prefix == lastPrefix)
                    {
                        prop.Deserialize(instance, context);
                        continue;
                    }

                    // scan for the correct property
                    bool foundTag = false;
                    if (prefix > lastPrefix)
                    {
                        for (int i = lastIndex + 1; i < propCount; i++)
                        {
                            if (readProps[i].FieldPrefix == prefix)
                            {
                                prop = readProps[i];
                                lastIndex = i;
                                lastPrefix = prefix;
                                foundTag = true;
                                break;
                            }
                            if (readProps[i].FieldPrefix > prefix) break; // too far
                        }
                    }
                    else
                    {
                        for (int i = lastIndex - 1; i >= 0; i--)
                        {
                            if (readProps[i].FieldPrefix == prefix)
                            {
                                prop = readProps[i];
                                lastIndex = i;
                                lastPrefix = prefix;
                                foundTag = true;
                                break;
                            }
                            if (readProps[i].FieldPrefix < prefix) break; // too far
                        }
                    }

                    if (foundTag)
                    { // found it by seeking; deserialize and continue
                        prop.Deserialize(instance, context);
                        continue;
                    }

                    WireType wireType;
                    int fieldTag;
                    Serializer.ParseFieldToken(prefix, out wireType, out fieldTag);
                    if (wireType == WireType.EndGroup)
                    {
                        context.EndGroup(fieldTag);
                        break; // this ends the entity, so stop the loop
                    }
                    
                    // so we couldn't find it...
                    if (extensible != null)
                    {
                        if (extn == null) extn = extensible.GetExtensionObject(true);
                        if (extraData == null && extn != null)
                        {    
                            extraStream = extn.BeginAppend();
                            extraData = new SerializationContext(extraStream, null);
                        }

                        // copy the data into the output stream
                        extraData.EncodeUInt32(prefix);
                        ProcessExtraData(context, fieldTag, wireType, extraData);
                    }
                    else
                    {
                        // unexpected fields for an inextensible object; discard the data
                        Serializer.SkipData(context, fieldTag, wireType);
                    }                    
                }
                if (extraStream != null)
                {
                    extraData.Flush();
                    extn.EndAppend(extraStream, true);
                }
            }
            catch
            {
                if (extraStream != null) extn.EndAppend(extraStream, false);
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
                    len = read.ReadRawVariant();
                    write.WriteBlock(read.Workspace, 0, len);
                    break;
                case WireType.Fixed32:
                    read.ReadBlock(4);
                    write.WriteBlock(read.Workspace, 0, 4);
                    break;
                case WireType.Fixed64:
                    read.ReadBlock(8);
                    write.WriteBlock(read.Workspace, 0, 8);
                    break;
                case WireType.String:
                    len = read.DecodeInt32();
                    write.EncodeInt32(len);
                    read.WriteTo(write, len);
                    break;
                case WireType.StartGroup:
                    read.StartGroup(fieldTag);
                    uint prefix;
                    while ((prefix = read.TryReadFieldPrefix()) > 0)
                    {
                        write.EncodeUInt32(prefix);
                        Serializer.ParseFieldToken(prefix, out wireType, out fieldTag);
                        if (wireType == WireType.EndGroup)
                        {
                            read.EndGroup(fieldTag);
                            break;
                        }
                        ProcessExtraData(read, fieldTag, wireType, write);
                    }
                    break;
                    /*using (CloneStream cloneStream = new CloneStream(read, write))
                    {
                        SerializationContext cloneCtx = new SerializationContext(cloneStream, null);
                        cloneCtx.StartGroup(fieldTag);
                        Serializer<UnknownType>.Build();
                        Serializer<UnknownType>.Deserialize(UnknownType.Default, cloneCtx);
                    }
                    break;*/
                case WireType.EndGroup:
                    throw new ProtoException("End-group not expected at this location");                
                default:
                    throw new ProtoException("Unknown wire-type " + wireType.ToString());
            }
        }

        
        

        
        /*
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
    */
        internal static void CheckTagNotInUse(int tag)
        {
            if (tag <= 0) throw new ArgumentOutOfRangeException("tag", "Tags must be positive integers.");
            if (readProps == null) Build();
            foreach (Property<T> prop in readProps)
            {
                if (prop.Tag == tag) throw new ArgumentException(
                    string.Format("Tag {0} is in use; access the {1} property instead.", tag, prop.Name), "tag");
            }
        }


    }
}
