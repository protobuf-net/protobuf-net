using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace ProtoBuf
{

    internal static class Serializer<T> where T : class, new()
    {


        public static string GetProto()
        {
            StringBuilder sb = new StringBuilder();
            GetProto(sb, 0);
            return sb.ToString();
        }

        internal static StringBuilder Indent(StringBuilder sb, int nestLevel)
        {
            return sb.Append(' ', nestLevel * 2);
        }
        internal static void GetProto(StringBuilder sb, int nestLevel)
        {
            string ns = typeof(T).Namespace;
            if (!string.IsNullOrEmpty(ns))
            {
                Indent(sb, nestLevel).Append("package ").Append(ns).AppendLine(";");
            }
            string descText, name = Serializer.GetDefinedTypeName<T>();
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
                    .Append(prop.IsRequired ? "required " : "optional ")
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


        static readonly IProperty<T>[] props;

        static Serializer()
        {
            if (!Serializer.IsEntityType(typeof(T)))
            {
                throw new InvalidOperationException("Only concrete data-contract classes can be processed");
            }
            List<IProperty<T>> propList = new List<IProperty<T>>();
            Type[] argsOnePropertyInfo = { typeof(PropertyInfo) };
            object[] argsOnePropertyVal = new object[1];

            foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                string name;
                DataFormat format;
                int tag;
                bool isRequired;
                if (!Serializer.TryGetTag(prop, out tag, out name, out format, out isRequired))
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

                IProperty<T> iProp;
                Type propType = prop.PropertyType, listItemType = null;

                if (!propType.IsArray) // don't treat arrays as lists
                {
                    foreach (Type interfaceType in propType.GetInterfaces())
                    {
                        if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition()
                            == typeof(IList<>))
                        {
                            listItemType = interfaceType.GetGenericArguments()[0];
                            break;
                        }
                    }
                }

                argsOnePropertyVal[0] = prop;
                if (listItemType != null)
                { // list
                    iProp = iProp = (IProperty<T>)typeof(Serializer<T>)
                        .GetMethod("CreateListProperty")
                        .MakeGenericMethod(typeof(T), prop.PropertyType, listItemType)
                        .Invoke(null, argsOnePropertyVal); 
                }
                else if (Serializer.IsEntityType(propType))
                { // entity
                    iProp = (IProperty<T>)typeof(Serializer<T>)
                        .GetMethod("CreateEntityProperty")
                        .MakeGenericMethod(typeof(T), prop.PropertyType)
                        .Invoke(null, argsOnePropertyVal);
                }
                else
                { // simple value
                    iProp = (IProperty<T>)typeof(Serializer<T>)
                        .GetMethod("CreateSimpleProperty")
                        .MakeGenericMethod(typeof(T), prop.PropertyType)
                        .Invoke(null, argsOnePropertyVal);
                }
                propList.Add(iProp);
            }
            propList.Sort(delegate(IProperty<T> x, IProperty<T> y) { return x.Tag.CompareTo(y.Tag); });
            props = propList.ToArray();
        }

        internal static int Serialize(T instance, Stream destination)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            if (destination == null) throw new ArgumentNullException("destination");
            SerializationContext ctx = new SerializationContext(destination);
            return Serialize(instance, ctx, null);
        }
        internal static int Serialize(T instance, SerializationContext context, IProperty<T>[] candidateProperties)
        {
            context.Push(instance);
            context.CheckSpace();
            int total = 0;
            if(candidateProperties == null) candidateProperties = props;
            for (int i = 0; i < candidateProperties.Length; i++)
            {
                // note that this serialization includes the headers...
                total += candidateProperties[i].Serialize(instance, context);
            }
            IExtensible extra = instance as IExtensible;
            int len;
            if (extra != null && (len = extra.GetLength()) > 0)
            {
                Stream extraStream = extra.BeginQuery();
                try {
                    SerializationContext tmpCtx = new SerializationContext(extraStream);
                    tmpCtx.ReadFrom(context);
                    BlitData(tmpCtx, context.Stream, len);
                    context.ReadFrom(tmpCtx);
                } finally {
                    extra.EndQuery(extraStream);
                }
                                
            }
            context.Stream.Flush();
            context.Pop(instance);
            return total;
        }

        internal static int GetLength(T instance, SerializationContext context, List<IProperty<T>> candidateProperties)
        {
            context.Push(instance);
            context.CheckSpace();
            int total = 0;
            for (int i = 0; i < props.Length; i++)
            {
                int propLen = props[i].GetLength(instance, context);
                total += propLen;
                if (propLen > 0 && candidateProperties != null) candidateProperties.Add(props[i]);
            }
            IExtensible extra = instance as IExtensible;
            if (extra != null) total += extra.GetLength();
            context.Pop(instance);
            return total;
       
        }
        internal static void Deserialize(T instance, Stream source)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            if (source == null) throw new ArgumentNullException("source");
            SerializationContext ctx = new SerializationContext(source);
            Deserialize(instance, ctx);
        }
        internal static void Deserialize(T instance, SerializationContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            int prefix, propCount = props.Length;
            context.CheckSpace();
            IExtensible extra = instance as IExtensible;
            SerializationContext extraData = null;
            IProperty<T> prop = propCount == 0 ? null : props[0];
            
            int lastIndex = prop == null ? int.MinValue : 0,
                lastTag = prop == null ? int.MinValue : prop.Tag;
            
            try
            {
                while (TwosComplementSerializer.TryReadInt32(context, out prefix))
                {
                    WireType wireType;
                    int fieldTag;
                    ParseFieldToken(prefix, out wireType, out fieldTag);
                    bool foundTag = fieldTag == lastTag;
                    if (!foundTag)
                    {
                        int index = lastIndex;
                        // start i at 1 as only need to check n-1 other properties
                        for (int i = 1; i < propCount; i++)
                        {
                            if (++index == propCount) { index = 0; }
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
                        if (prop.WireType != wireType)
                        {   // check that we are getting the wire-type we expected, so we
                            // don't read as a fixed-size when the data is a variant (etc)
                            throw new SerializationException(string.Format("Wire-type of {0} (tag {1}) did not match; expected {2}, received {3}",
                                prop.Name, prop.Tag, prop.WireType, wireType));
                        }
                        // recognised fields; use the property deserializer
                        prop.Deserialize(instance, context);
                    }
                    else if (extra != null)
                    {
                        if (extraData == null)
                        {
                            extraData = new SerializationContext(extra.BeginAppend());
                        }

                        // borrow the workspace, and copy the data
                        extraData.ReadFrom(context);
                        TwosComplementSerializer.WriteToStream(prefix, extraData);
                        ProcessExtraData(context, wireType, extraData);                        
                        context.ReadFrom(extraData);
                    }
                    else
                    {
                        // unexpected fields for an inextensible object; discard the data
                        SkipData(context, wireType);
                    }
                }
                if (extraData != null) extra.EndAppend(extraData.Stream, true);
            }
            catch
            {
                if (extraData != null) extra.EndAppend(extraData.Stream, false);
                throw;
            }
        }

        internal static void ParseFieldToken(int token, out WireType wireType, out int tag)
        {
            wireType = (WireType)(token & 7);
            tag = token >> 3;
            if (tag <= 0)
            {
                throw new SerializationException("Invalid tag: " + tag.ToString());
            }
        }

        private static void ProcessExtraData(SerializationContext read, WireType wireType, SerializationContext write)
        {
            int len;
            switch (wireType)
            {
                case WireType.Variant:
                    len = Base128Variant.ReadRaw(read);
                    write.Stream.Write(read.Workspace, read.WorkspaceIndex, len);
                    break;
                case WireType.Fixed32:
                    BlobSerializer.ReadBlock(read, 4);
                    write.Stream.Write(read.Workspace, read.WorkspaceIndex, 4);
                    break;
                case WireType.Fixed64:
                    BlobSerializer.ReadBlock(read, 8);
                    write.Stream.Write(read.Workspace, read.WorkspaceIndex, 8);
                    break;
                case WireType.String:
                    len = TwosComplementSerializer.ReadInt32(read);
                    TwosComplementSerializer.WriteToStream(len, write);
                    BlitData(read, write.Stream, len);
                    break;
                case WireType.EndGroup:
                case WireType.StartGroup:
                    throw new NotSupportedException(wireType.ToString());
                default:
                    throw new SerializationException("Unknown wire-type " + wireType.ToString());
            }
        }

        private static void BlitData(SerializationContext source, Stream destination, int length)
        {
            int capacity = CheckBufferForBlit(source, length);
            while (length > capacity)
            {
                BlobSerializer.ReadBlock(source, capacity);
                destination.Write(source.Workspace, source.WorkspaceIndex, capacity);
                length -= capacity;
            }
            if (length > 0)
            {
                BlobSerializer.ReadBlock(source, length);
                destination.Write(source.Workspace, source.WorkspaceIndex, length);
            }
        }

        internal static void SkipData(SerializationContext context, WireType wireType)
        {
            Stream source = context.Stream;
            switch (wireType)
            {
                case WireType.Variant:
                    Base128Variant.Skip(source);
                    break;
                case WireType.Fixed32:
                    if (source.CanSeek) source.Seek(4, SeekOrigin.Current);
                    else BlobSerializer.ReadBlock(context, 4);
                    break;
                case WireType.Fixed64:
                    if (source.CanSeek) source.Seek(8, SeekOrigin.Current);
                    else BlobSerializer.ReadBlock(context, 8);
                    break;
                case WireType.String:
                    int len = TwosComplementSerializer.ReadInt32(context);
                    if (source.CanSeek) source.Seek(len, SeekOrigin.Current);
                    else
                    {
                        int capacity = CheckBufferForBlit(context, len);
                        while (len > capacity)
                        {
                            BlobSerializer.ReadBlock(context, capacity);
                            len -= capacity;
                        }
                        if (len > 0)
                        {
                            BlobSerializer.ReadBlock(context, len);
                        }
                    }
                    break;
                case WireType.EndGroup:
                case WireType.StartGroup:
                    throw new NotSupportedException(wireType.ToString());
                default:
                    throw new SerializationException("Unknown wire-type " + wireType.ToString());
            }
        }

        private static int CheckBufferForBlit(SerializationContext context, int length)
        {
            const int BLIT_BUFFER_SIZE = 4096;
            int capacity = context.Workspace.Length - context.WorkspaceIndex;
            if (length > capacity && capacity < BLIT_BUFFER_SIZE)
            { // don't want to loop/blit with too small a buffer...
                context.CheckSpace(BLIT_BUFFER_SIZE);
                capacity = context.Workspace.Length - context.WorkspaceIndex;
            }
            return capacity;
        }

        
        public static IProperty<TEntity> CreateSimpleProperty<TEntity, TValue>(PropertyInfo property)
            where TEntity : class, new()
        {
            return new SimpleProperty<TEntity, TValue>(property);
        }

        public static IProperty<TEntity> CreateEntityProperty<TEntity, TValue>(PropertyInfo property)
            where TEntity : class, new()
            where TValue : class, new()
        {
            return new EntityProperty<TEntity, TValue>(property);
        }
        public static IProperty<TEntity> CreateListProperty<TEntity, TList, TValue>(PropertyInfo property)
            where TEntity : class, new()
            where TList : IList<TValue>
        {
            return new ListProperty<TEntity, TList, TValue>(property);
        }


        internal static void CheckTagNotInUse(int tag)
        {
            if (tag <= 0) throw new ArgumentOutOfRangeException("tag", "Tags must be positive integers.");
            foreach (IProperty<T> prop in props)
            {
                if (prop.Tag == tag) throw new ArgumentException(
                    string.Format("Tag {0} is in use; access the {1} property instead.",
                        tag, prop.Name), "tag");
            }
        }
    }
}
