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
        internal static string GetDefinedTypeName()
        {
            string name = typeof(T).Name;
            ProtoContractAttribute pc = AttributeUtils.GetAttribute<ProtoContractAttribute>(typeof(T));
            if (pc != null)
            {
                if (!string.IsNullOrEmpty(pc.Name)) name = pc.Name;
                return name;
            }
#if NET_3_0
            DataContractAttribute dc = AttributeUtils.GetAttribute<DataContractAttribute>(typeof(T));
            if (dc != null)
            {
                if (!string.IsNullOrEmpty(dc.Name)) name = dc.Name;
                return name;
            }
#endif

            XmlTypeAttribute xt = AttributeUtils.GetAttribute<XmlTypeAttribute>(typeof(T));
            if (xt != null)
            {
                if (!string.IsNullOrEmpty(xt.TypeName)) name = xt.TypeName;
                return name;
            }

            return name;

        }

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
            string descText, name = GetDefinedTypeName();
            Indent(sb, nestLevel).Append("message ").Append(name).Append(" {");

            DescriptionAttribute desc = AttributeUtils.GetAttribute<DescriptionAttribute>(typeof(T));
            descText = desc == null ? null : desc.Description;
            if (!string.IsNullOrEmpty(descText))
            {
                sb.Append(" //").Append(descText); // TODO: remove crlf
            }

            sb.AppendLine();
            nestLevel++;
            for(int i = 0 ; i < props.Length ; i++) {
                IProperty<T> prop = props[i];
                Indent(sb, nestLevel).Append(' ')
                    .Append(prop.IsRequired ? "required " : "optional ")
                    .Append(prop.DefinedType).Append(' ')
                    .Append(prop.Name).Append(" = ").Append(prop.Tag);

                DefaultValueAttribute def = AttributeUtils.GetAttribute<DefaultValueAttribute>(prop.Property);
                if (def != null)
                {
                    string defText = Convert.ToString(def.Value, CultureInfo.InvariantCulture);
                    sb.Append(" [default = ").Append(defText).Append(" ]");
                }
                sb.Append(";");
                desc = AttributeUtils.GetAttribute<DescriptionAttribute>(prop.Property);
                descText = desc == null ? null : desc.Description;
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
                    iProp = (IProperty<T>)
                        typeof(ListProperty<,,>).MakeGenericType(
                            typeof(T), prop.PropertyType, listItemType)
                        .GetConstructor(argsOnePropertyInfo)
                        .Invoke(argsOnePropertyVal);
                }
                else if (Serializer.IsEntityType(propType))
                { // entity
                    iProp = (IProperty<T>)
                        typeof(EntityProperty<,>).MakeGenericType(
                            typeof(T), prop.PropertyType)
                        .GetConstructor(argsOnePropertyInfo)
                        .Invoke(argsOnePropertyVal);
                }
                else
                { // simple value
                    iProp = (IProperty<T>)
                        typeof(SimpleProperty<,>).MakeGenericType(
                            typeof(T), prop.PropertyType)
                        .GetConstructor(argsOnePropertyInfo)
                        .Invoke(argsOnePropertyVal);
                }
                propList.Add(iProp);
            }
            propList.Sort(delegate (IProperty<T> x, IProperty<T> y) {return x.Tag.CompareTo(y.Tag);});
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
            if (context == null) throw new ArgumentNullException("context");
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
                using(Stream extraStream = extra.Read()) {
                    SerializationContext tmpCtx = new SerializationContext(extraStream);
                    tmpCtx.ReadWorkspaceFrom(context);
                    BlitData(tmpCtx, context.Stream, len);
                    context.ReadWorkspaceFrom(tmpCtx);
                }                
            }
            context.Stream.Flush();
            return total;
        }

        internal static int GetLength(T instance, SerializationContext context, List<IProperty<T>> candidateProperties)
        {
            if (context == null) throw new ArgumentNullException("context");
            context.CheckSpace();
            int total = 0;
            for(int i = 0 ; i < props.Length ; i++) {
                int propLen = props[i].GetLength(instance, context);
                total += propLen;
                if (propLen > 0 && candidateProperties != null) candidateProperties.Add(props[i]);
            }
            IExtensible extra = instance as IExtensible;
            if (extra != null) total += extra.GetLength();
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
            Stream source = context.Stream;
            int prefix, propCount = props.Length;
            context.CheckSpace();
            IExtensible extra = instance as IExtensible;
            //SerializationContext extraData = null;
            IProperty<T> prop = propCount == 0 ? null : props[0];
            int lastIndex = prop == null ? int.MinValue : 0,
                lastTag = prop == null ? int.MinValue : prop.Tag;
                
            
            while (TwosComplementSerializer.TryReadInt32(context, out prefix))
            {
                WireType wireType = (WireType)(prefix & 7);
                int fieldTag = prefix >> 3;
                if (fieldTag <= 0)
                {
                    throw new SerializationException("Invalid tag: " + fieldTag.ToString());
                }
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
                    // recognised fields; use the property deserializer
                    prop.Deserialize(instance, context);
                }
                else if (extra != null)
                {
                    throw new NotImplementedException("Extensible objects");
                    //using(SubStream subStream = new SubStream(context.Stream, 
                    /*
                    //TODO: get a wrapped SubStream
                    // unexpected fields for an extensible object; store the data
                    Stream subStream = context.Stream; // !!! SWAP THIS
                    // just the appropriate length
                    extra.Append(subStream);
                    TwosComplementSerializer.WriteToStream(prefix, extraData);
                    //extraData = ProcessExtraData(context, wireType, extraData);
                     */
                }
                else
                {
                    // unexpected fields for an inextensible object; discard the data
                    SkipData(context, wireType);
                }
            }
        }

        private static SerializationContext ProcessExtraData(SerializationContext context, WireType wireType, SerializationContext extraData)
        {
            int len;
            switch (wireType)
            {
                case WireType.Variant:
                    len = Base128Variant.ReadRaw(context);
                    extraData.Stream.Write(context.Workspace, context.WorkspaceIndex, len);
                    break;
                case WireType.Fixed32:
                    BlobSerializer.ReadBlock(context, 4);
                    extraData.Stream.Write(context.Workspace, context.WorkspaceIndex, 4);
                    break;
                case WireType.Fixed64:
                    BlobSerializer.ReadBlock(context, 8);
                    extraData.Stream.Write(context.Workspace, context.WorkspaceIndex, 8);
                    break;
                case WireType.String:
                    len = TwosComplementSerializer.ReadInt32(context);
                    BlitData(context, extraData.Stream, len);
                    break;
                case WireType.EndGroup:
                case WireType.StartGroup:
                    throw new NotSupportedException(wireType.ToString());
                default:
                    throw new SerializationException("Unknown wire-type " + wireType.ToString());
            }
            return extraData;
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

        private static void SkipData(SerializationContext context, WireType wireType)
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


    }

    
}
