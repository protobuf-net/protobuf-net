using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace ProtoBuf
{

    internal static class Serializer<T> where T : class, new()
    {
        internal static string GetDefinedTypeName()
        {
            DataContractAttribute dc = AttributeUtils.GetAttribute<DataContractAttribute>(typeof(T));
            string name = dc == null ? null : dc.Name;
            if (string.IsNullOrEmpty(name)) name = InferName(typeof(T).Name);
            return name;
        }

        public static string GetProto()
        {
            StringBuilder sb = new StringBuilder();
            GetProto(sb, 0);
            return sb.ToString();
        }
        static string InferName(string name)
        {
            return name; // not implemented - but propose case, underscore etc
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
            foreach (var pair in props)
            {
                PropertyInfo prop = pair.Value.Property;
                DataMemberAttribute dm = AttributeUtils.GetAttribute<DataMemberAttribute>(prop);
                name = dm == null ? null : dm.Name;
                if (string.IsNullOrEmpty(name)) name = InferName(prop.Name);

                Indent(sb, nestLevel).Append(' ')
                    .Append(dm != null && dm.IsRequired ? "required " : "optional ")
                    .Append(pair.Value.DefinedType).Append(' ')
                    .Append(name).Append(" = ").Append(pair.Key);

                DefaultValueAttribute def = AttributeUtils.GetAttribute<DefaultValueAttribute>(prop);
                if (def != null)
                {
                    string defText = Convert.ToString(def.Value, CultureInfo.InvariantCulture);
                    sb.Append(" [default = ").Append(defText).Append(" ]");
                }
                sb.Append(";");
                desc = AttributeUtils.GetAttribute<DescriptionAttribute>(prop);
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


        static readonly SortedList<int, IProperty<T>> props;

        static Serializer()
        {
            if (!Serializer.IsEntityType(typeof(T)))
            {
                throw new InvalidOperationException("Only concrete data-contract classes can be processed");
            }
            props = new SortedList<int, IProperty<T>>();
            Type[] argsOnePropertyInfo = { typeof(PropertyInfo) };
            object[] argsOnePropertyVal = new object[1];

            foreach (PropertyInfo prop in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                int tag = Serializer.GetTag(prop);
                if (tag <= 0) continue; // didn't recognise this as a usable property

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
                props.Add(iProp.Tag, iProp);
            }
        }



        internal static int Serialize(T instance, Stream destination)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            if (destination == null) throw new ArgumentNullException("destination");
            SerializationContext ctx = new SerializationContext(destination);
            return Serialize(instance, ctx, null);
        }
        internal static int Serialize(T instance, SerializationContext context, IEnumerable<IProperty<T>> candidateProperties)
        {
            if (context == null) throw new ArgumentNullException("context");
            context.CheckSpace();
            int total = 0;
            foreach (IProperty<T> prop in (candidateProperties ?? props.Values))
            {
                // note that this serialization includes the headers...
                total += prop.Serialize(instance, context);
            }
            context.Stream.Flush();
            return total;
        }

        internal static int GetLength(T instance, SerializationContext context, IList<IProperty<T>> candidateProperties)
        {
            if (context == null) throw new ArgumentNullException("context");
            context.CheckSpace();
            int total = 0;
            foreach (IProperty<T> prop in props.Values)
            {
                int propLen = prop.GetLength(instance, context);
                total += propLen;
                if (propLen > 0 && candidateProperties != null) candidateProperties.Add(prop);
            }
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
            bool canSeek = source.CanSeek;
            int prefix;
            context.CheckSpace();
            
            while (Int32VariantSerializer.TryReadFromStream(context, out prefix))
            {
                WireType wireType = (WireType)(prefix & 7);
                int fieldIndex = prefix >> 3;
                if (fieldIndex <= 0)
                {
                    throw new SerializationException("Invalid tag: " + fieldIndex.ToString());
                }

                IProperty<T> prop;
                if (props.TryGetValue(fieldIndex, out prop))
                {
                    prop.Deserialize(instance, context);
                }
                else
                {
                    switch (wireType)
                    {
                        case WireType.Variant:
                            Base128Variant.Skip(source);
                            break;
                        case WireType.Fixed32:
                            if (canSeek) source.Seek(4, SeekOrigin.Current);
                            else BlobSerializer.ReadBlock(context, 4);
                            break;
                        case WireType.Fixed64:
                            if (canSeek) source.Seek(8, SeekOrigin.Current);
                            else BlobSerializer.ReadBlock(context, 8);
                            break;
                        case WireType.String:
                            int len = Int32VariantSerializer.ReadFromStream(context);
                            if (canSeek) source.Seek(len, SeekOrigin.Current);
                            else
                            {
                                int capacity = context.Workspace.Length - context.WorkspaceIndex;
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
                            throw new NotSupportedException();
                        default:
                            throw new SerializationException("Unknown wire-type " + wireType.ToString());
                    }
                }
            }
        }


    }

    
}
