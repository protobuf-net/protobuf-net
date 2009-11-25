using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Globalization;
#if NET_3_0 || REMOTING
using System.Runtime.Serialization;
using System.ComponentModel;
#endif

namespace ProtoBuf
{
    /// <summary>
    /// Provides protocol-buffer serialization capability for concrete, attributed types. 
    /// </summary>
    /// <remarks>
    /// Protocol-buffer serialization is a compact binary format, designed to take
    /// advantage of sparse data and knowledge of specific data types; it is also
    /// extensible, allowing a type to be deserialized / merged even if some data is
    /// not recognised.
    /// </remarks>
    public static partial class Serializer
    {
        const string OBSOLETE_MESSAGE_PREFIX_STYLE = "Please specify a PrefixStyle; existing code should use Base128 to retain wire-compatibility.";
        internal static void VerifyBytesWritten(int expected, int actual)
        {
            if (actual != expected)
            {
                throw new ProtoException(string.Format(
                    "Wrote {0} bytes, but expected to write {1}.", actual, expected));
            }
        }

        internal static readonly Type[] EmptyTypes = new Type[0];
        internal static bool IsEntityType(Type type)
        {
            return Entity.IsEntity(type);            
        }

        /// <summary>
        /// Supports various different property metadata patterns:
        /// [ProtoMember] is the most specific, allowing the data-format to be set.
        /// [DataMember], [XmlElement] are supported for compatibility.
        /// In any event, there must be a unique positive Tag/Order.
        /// </summary>
        internal static bool TryGetTag(MemberInfo member, out int tag, out string name, out DataFormat format, out MemberSerializationOptions options)
        {
            return TryGetTag(member, out tag, out name, false, out format, out options);
        }

        internal static IEnumerable<MemberInfo> GetProtoMembers(Type type)
        {
            foreach(MemberInfo member in type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                switch(member.MemberType) {
                    case MemberTypes.Property:
                    case MemberTypes.Field:
                        yield return member;
                        break;
                }
            }
        }

        static void AddImplicitByDeclaringType<T>(Type declaringType, List<MemberInfo> list, T[] members)
            where T : MemberInfo
        {
            int tag;
            string name;
            DataFormat fmt;
            MemberSerializationOptions options;
            foreach(T member in members)
            {
                if(member.DeclaringType == declaringType
                   && member.GetCustomAttributes(typeof(XmlIgnoreAttribute), false).Length == 0
                   && TryGetTag(member, out tag, out name, true, out fmt, out options) && tag < 1) list.Add(member);
            }
        }

        internal static bool TryGetTag(MemberInfo member, out int tag, out string name, bool callerIsTagInference, out DataFormat format, out MemberSerializationOptions options)
        {
            name = member.Name;
            format = DataFormat.Default;
            tag = -1;
            options = MemberSerializationOptions.None;

            // check for delegates (don't even try!)
            Type valueType;
            switch(member.MemberType)
            {
                case MemberTypes.Property:
                    valueType = ((PropertyInfo)member).PropertyType;
                    break;
                case MemberTypes.Field:
                    valueType = ((FieldInfo) member).FieldType;
                    break;
                default: // not sure what this is!
                    return false;
            }
            if (valueType.IsSubclassOf(typeof(Delegate))) return false;

            // check for exclusion
            if(AttributeUtils.GetAttribute<ProtoIgnoreAttribute>(member) != null
                  || AttributeUtils.GetAttribute<ProtoPartialIgnoreAttribute>(member.ReflectedType,
                     delegate( ProtoPartialIgnoreAttribute ppia)
                          { return ppia.MemberName == member.Name; }) != null) return false;
            
            // check against the property
            ProtoMemberAttribute pm = AttributeUtils.GetAttribute<ProtoMemberAttribute>(member);
            if (pm == null)
            { // check also against the type 
                pm = AttributeUtils.GetAttribute<ProtoPartialMemberAttribute>(member.ReflectedType,
                    delegate(ProtoPartialMemberAttribute ppma) { return ppma.MemberName == member.Name; });
            }
            if (pm != null)
            {
                format = pm.DataFormat;
                if (!string.IsNullOrEmpty(pm.Name)) name = pm.Name;
                tag = pm.Tag;
                options = pm.Options;
                return tag > 0;
            }

            ProtoContractAttribute pca = AttributeUtils.GetAttribute<ProtoContractAttribute>(member.DeclaringType);
            if(pca != null && pca.ImplicitFields != ImplicitFields.None)
            {
#if !SILVERLIGHT
                // skip [NonSerialized]
                if(AttributeUtils.GetAttribute<NonSerializedAttribute>(member) != null) return false;
#endif
                if(callerIsTagInference) return true; // short-circuit

                List<MemberInfo> members = new List<MemberInfo>();
                switch(pca.ImplicitFields)
                {
                    case ImplicitFields.AllFields:
                        AddImplicitByDeclaringType(member.DeclaringType, members,
                            member.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
                        break;
                    case ImplicitFields.AllPublic:
                        AddImplicitByDeclaringType(member.DeclaringType, members,
                            member.DeclaringType.GetFields(BindingFlags.Instance | BindingFlags.Public));
                        AddImplicitByDeclaringType(member.DeclaringType, members,
                            member.DeclaringType.GetProperties(BindingFlags.Instance | BindingFlags.Public));
                        break;
                    default:
                        throw new NotSupportedException("Unknown ImplicitFields option: " + pca.ImplicitFields);
                }
                members.Sort(delegate (MemberInfo x, MemberInfo y)
                {
                    return string.CompareOrdinal(x.Name, y.Name);
                });
                int index = members.IndexOf(member);
                if(index >= 0)
                {
                    tag = index + pca.ImplicitFirstTag;
                    return true;
                }
                return false;
            }

#if NET_3_0
            DataMemberAttribute dm = AttributeUtils.GetAttribute<DataMemberAttribute>(member);
            if (dm != null)
            {
                if (!string.IsNullOrEmpty(dm.Name)) name = dm.Name;
                tag = dm.Order;
                if (pca != null) tag += pca.DataMemberOffset;

                if(!callerIsTagInference) // avoid infinite recursion
                {
                    if (pca != null && pca.InferTagFromName)
                    {
                        // since the type has inference enabled, identify the members for the
                        // type and give each an order based on the Order and Name, then find
                        // where the current property comes in the list. This will be repeated
                        // once (or more) per property during initialization, but not during
                        // core runtime - so it is not a perfomance bottleneck (so not worth
                        // complicating the implementation by caching it anywhere).

                        // find all properties under consideration
                        List<KeyValuePair<string, int>> members = new List<KeyValuePair<string,int>>();
                        string tmpName; // use this also to cache the "out" name (not usable from lambda)
                        foreach(MemberInfo prop in GetProtoMembers(member.DeclaringType))
                        {
                            int tmpTag;
                            DataFormat tmpFormat;
                            MemberSerializationOptions tmpOptions;
                            if (TryGetTag(prop, out tmpTag, out tmpName, true, out tmpFormat, out tmpOptions))
                            {
                                members.Add(new KeyValuePair<string,int>(tmpName, tmpTag));
                            }
                        }
                        // sort by "Order, Name", where "Name" includes any renaming (i.e. not MemberInfo.Name)
                        members.Sort(delegate(KeyValuePair<string, int> x, KeyValuePair<string, int> y)
                        {
                            int result = x.Value.CompareTo(y.Value);
                            if (result == 0) result = string.CompareOrdinal(x.Key, y.Key);
                            return result;
                        });
                        // find the current item
                        // (can't use FindIndex because of Silverlight)
                        tmpName = name;
                        int index = 1;
                        tag = 0;
                        foreach (KeyValuePair<string, int> x in members)
                        {
                            if (x.Key == tmpName)
                            {
                                tag = index;
                                break;
                            }
                            index++;
                        }
                    }
                }
                if(dm.IsRequired) options |= MemberSerializationOptions.Required;
                return callerIsTagInference || tag > 0;
            }
#endif
            
            XmlElementAttribute xe = AttributeUtils.GetAttribute<XmlElementAttribute>(member);
            if (xe != null)
            {
                if (!string.IsNullOrEmpty(xe.ElementName)) name = xe.ElementName;
                tag = xe.Order;
                return tag > 0;
            }

            XmlArrayAttribute xa = AttributeUtils.GetAttribute<XmlArrayAttribute>(member);
            if (xa != null)
            {
                if (!string.IsNullOrEmpty(xa.ElementName)) name = xa.ElementName;
                tag = xa.Order;
                return tag > 0;
            }

            return false;
        }

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
        /// <returns>A new, initialized instance.</returns>
        public static T Deserialize<T>(Stream source)
        {
            return Deserialize<T>(new SerializationContext(source, null));
        }

        private static T Deserialize<T>(SerializationContext source)
        {
            T instance = default(T);
            try
            {
                SerializerProxy<T>.Default.Deserialize(ref instance, source);
            }
            catch (Exception ex)
            {
                ThrowInner(ex);
                throw; // if no inner (preserves stacktrace)
            }
            return instance;
        }


        /// <summary>
        /// Creates a new instance from a protocol-buffer stream that has a length-prefix
        /// on data (to assist with network IO).
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
        /// <returns>A new, initialized instance.</returns>
        [Obsolete(OBSOLETE_MESSAGE_PREFIX_STYLE, false)]
#if !SILVERLIGHT && !CF
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public static T DeserializeWithLengthPrefix<T>(Stream source)
        {
            return DeserializeWithLengthPrefix<T>(source, PrefixStyle.Base128);
        }
        /// <summary>
        /// The implicit tag used when serializing lists and other enumerable data.
        /// </summary>
        public const int ListItemTag = 1;

        /// <summary>
        /// Reads a sequence of consecutive length-prefixed items from a stream, using
        /// either base-128 or fixed-length prefixes. Base-128 prefixes with a tag
        /// are directly comparable to serializing multiple items in succession
        /// (use the <see cref="ListItemTag"/> tag to emulate the implicit behavior
        /// when serializing a list/array). When a tag is
        /// specified, any records with different tags are silently omitted. The
        /// tag is ignored. The tag is ignores for fixed-length prefixes.
        /// </summary>
        /// <typeparam name="T">The type of object to deserialize.</typeparam>
        /// <param name="source">The binary stream containing the serialized records.</param>
        /// <param name="style">The prefix style used in the data.</param>
        /// <param name="tag">The tag of records to return (if non-positive, then no tag is
        /// expected and all records are returned).</param>
        /// <returns>The sequence of deserialized objects.</returns>
        public static IEnumerable<T> DeserializeItems<T>(Stream source, PrefixStyle style, int tag)
        {
            if (source == null) throw new ArgumentNullException("source");
            switch (style)
            {
                case PrefixStyle.Fixed32:
                    if (tag > 0) throw new ArgumentException("A tag cannot be used with fixed-length prefixes.", "tag");
                    break;
                case PrefixStyle.Base128:
                    break;
                default:
                    throw new ArgumentException("Prefix style is not valid: " + style, "style");
            }
            return DeserializeItemsWithLengthPrefixChecked<T>(source, style, tag);
        }

        private static IEnumerable<T> DeserializeItemsWithLengthPrefixChecked<T>(Stream source, PrefixStyle style, int tag)
        {
            SerializationContext ctx = new SerializationContext(source, null);
            T item;
            while(TryDeserializeWithLengthPrefix(ctx, style, tag, out item))
            {
                yield return item;
            }
        }

        private static uint ReadPrefixLength(Stream source, PrefixStyle style)
        {
            uint value;
            if(!TryReadPrefixLength(source, style, 0, out value, null))
            {
                throw new EndOfStreamException();
            }
            return value;
        }

        private static bool TryReadPrefixLength(SerializationContext context, PrefixStyle style, int tag, out uint length)
        {
        MethodStart:
            switch (style)
            {
                case PrefixStyle.None:
                    length = uint.MaxValue;
                    return true;
                case PrefixStyle.Base128:
                    
                    if (tag <= 0) return context.TryDecodeUInt32(out length);
                    uint expected = GetFieldToken(tag, WireType.String), actual;
                    if (!context.TryDecodeUInt32(out actual))
                    {
                        length = 0;
                        return false;
                    }
                    if (expected == actual)
                    {
                        length = context.DecodeUInt32();
                        return true;
                    }
                    
                    WireType wireType;
                    int actualTag;
                    ParseFieldToken(actual, out wireType, out actualTag);
                    SkipData(context, actualTag, wireType);
                    goto MethodStart;
                case PrefixStyle.Fixed32:
                    return context.TryDecodeUInt32Fixed(out length);
                default:
                    throw new NotSupportedException("Invalid prefix style: " + style);
            }
        }

        private static bool TryReadPrefixLength(Stream source, PrefixStyle style, int tag, out uint length, Getter<int,bool> processField)
        {
            MethodStart:
            switch (style)
            {
                case PrefixStyle.None:
                    length = uint.MaxValue;
                    return true;
                case PrefixStyle.Base128:
                    if(tag <= 0) return SerializationContext.TryDecodeUInt32(source, out length);
                    uint expected = GetFieldToken(tag, WireType.String), actual;
                    if(!SerializationContext.TryDecodeUInt32(source, out actual))
                    {
                        length = 0;
                        return false;
                    }

                    WireType wireType;
                    int actualTag;
                    ParseFieldToken(actual, out wireType, out actualTag);

                    if (processField != null)
                    {
                        if (processField(actualTag))
                        {
                            length = SerializationContext.DecodeUInt32(source);
                            return true;
                        }
                    }
                    else if(expected == actual)
                    {
                        length = SerializationContext.DecodeUInt32(source);
                        return true;
                    }
                    
                    switch(wireType)
                    {
                        case WireType.String:
                            SerializationContext.SkipStringData(source);
                            goto MethodStart;
                        default:
                            throw new ProtoException("A record with a different tag could not be jumped because of the wire-type: " + wireType);
                    }
                case PrefixStyle.Fixed32:
                    return SerializationContext.TryDecodeUInt32Fixed(source, out length);
                default:
                    throw new NotSupportedException("Invalid prefix style: " + style);
            }
        }

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream that has a length-prefix
        /// on data (to assist with network IO).
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <returns>A new, initialized instance.</returns>
        public static T DeserializeWithLengthPrefix<T>(Stream source, PrefixStyle style)
        {
            return DeserializeWithLengthPrefix<T>(source, style, 0);
        }

        /// <summary>
        /// Creates a new instance from a protocol-buffer stream that has a length-prefix
        /// on data (to assist with network IO).
        /// </summary>
        /// <typeparam name="T">The type to be created.</typeparam>
        /// <param name="source">The binary stream to apply to the new instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="tag">The expected tag of the item (only used with base-128 prefix style).</param>
        /// <returns>A new, initialized instance.</returns>
        public static T DeserializeWithLengthPrefix<T>(Stream source, PrefixStyle style, int tag)
        {
            T item;
            if(!TryDeserializeWithLengthPrefix(source, style, tag, out item))
            {
                throw new EndOfStreamException();
            }
            return item;
        }



        private static bool TryDeserializeWithLengthPrefix<T>(SerializationContext context, PrefixStyle style, int tag, out T item)
        {
            uint len;
            if (!TryReadPrefixLength(context, style, tag, out len))
            {
                item = default(T);
                return false;
            }
            item = default(T);
            switch(len)
            {
                case 0: // nothing to do
                    break;
                case uint.MaxValue: // read to end
                    SerializerProxy<T>.Default.Deserialize(ref item, context);
                    break;
                default: // limit and read sub-stream
                    long restore = context.Limit(len);
                    SerializerProxy<T>.Default.Deserialize(ref item, context);
                    context.MaxReadPosition = restore;
                    break;
            }
            return true;
        }

        /// <summary>Indicates the number of bytes expected for the next message.</summary>
        /// <param name="source">The stream containing the data to investigate for a length.</param>
        /// <param name="style">The algorithm used to encode the length.</param>
        /// <param name="length">The length of the message, if it could be identified.</param>
        /// <returns>True if a length could be obtained, false otherwise.</returns>
        public static bool TryReadLengthPrefix(Stream source, PrefixStyle style, out int length)
        {
            uint len;
            bool result;
            switch (style)
            {
                case PrefixStyle.Fixed32:
                    result = SerializationContext.TryDecodeUInt32Fixed(source, out len);
                    break;
                case PrefixStyle.Base128:
                    result = SerializationContext.TryDecodeUInt32(source, out len);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("style", "Invalid prefix style: " + style);
            }
            length = (int)len;
            return result;
        }

        /// <summary>Indicates the number of bytes expected for the next message.</summary>
        /// <param name="buffer">The buffer containing the data to investigate for a length.</param>
        /// <param name="index">The offset of the first byte to read from the buffer.</param>
        /// <param name="count">The number of bytes to read from the buffer.</param>
        /// <param name="style">The algorithm used to encode the length.</param>
        /// <param name="length">The length of the message, if it could be identified.</param>
        /// <returns>True if a length could be obtained, false otherwise.</returns>
        public static bool TryReadLengthPrefix(byte[] buffer, int index, int count, PrefixStyle style, out int length)
        {
            using (Stream source = new MemoryStream(buffer, index, count))
            {
                return TryReadLengthPrefix(source, style, out length);
            }
        }

        private static bool TryDeserializeWithLengthPrefix<T>(Stream source, PrefixStyle style, int tag, out T item)
        {
            uint len;
            if(!TryReadPrefixLength(source, style, tag, out len, null))
            {
                item = default(T);
                return false;
            }

            if (len == uint.MaxValue)
            {
                item = Deserialize<T>(source);
            }
            else
            {
                using (SubStream subStream = new SubStream(source, len, false))
                {
                    item = Deserialize<T>(subStream);
                }
            }
            return true;
        }



        internal static Exception ThrowNoEncoder(DataFormat format, Type valueType)
        {
            throw new InvalidOperationException(string.Format(
                "No suitable {0} {1} encoding found.",
                format, valueType.Name));
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public static T Merge<T>(Stream source, T instance)
        {
            try
            {
                SerializerProxy<T>.Default.Deserialize(ref instance, source);
                return instance;
            }
            catch (Exception ex)
            {
                ThrowInner(ex);
                throw; // if no inner (preserves stacktrace)
            }
        }

                /// <summary>
        /// Applies a protocol-buffer stream to an existing instance, using length-prefixed
        /// data - useful with network IO.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        [Obsolete(OBSOLETE_MESSAGE_PREFIX_STYLE, false)]
#if !SILVERLIGHT && !CF
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public static T MergeWithLengthPrefix<T>(Stream source, T instance)
        {
            return MergeWithLengthPrefix<T>(source, instance, PrefixStyle.Base128);
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance, using length-prefixed
        /// data - useful with network IO.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public static T MergeWithLengthPrefix<T>(Stream source, T instance, PrefixStyle style)
        {
            uint len = ReadPrefixLength(source, style);
            if (len == uint.MaxValue) return Merge<T>(source, instance);
            using (SubStream subStream = new SubStream(source, len, false))
            {
                return Merge<T>(subStream, instance);
            }
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="destination">The destination stream to write to.</param>
        public static void Serialize<T>(Stream destination, T instance)
        {
            try
            {
                SerializerProxy<T>.Default.Serialize(instance, destination);
            }
            catch (Exception ex)
            {
                ThrowInner(ex);
                throw; // if no inner (preserves stacktrace)
            }
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream,
        /// with a length-prefix. This is useful for socket programming,
        /// as DeserializeWithLengthPrefix/MergeWithLengthPrefix can be used to read the single object back
        /// from an ongoing stream.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="destination">The destination stream to write to.</param>
        [Obsolete(OBSOLETE_MESSAGE_PREFIX_STYLE, false)]
#if !SILVERLIGHT && !CF
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        public static void SerializeWithLengthPrefix<T>(Stream destination, T instance)
        {
            SerializeWithLengthPrefix<T>(destination, instance, PrefixStyle.Base128, 0);
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream,
        /// with a length-prefix. This is useful for socket programming,
        /// as DeserializeWithLengthPrefix/MergeWithLengthPrefix can be used to read the single object back
        /// from an ongoing stream.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="destination">The destination stream to write to.</param>
        public static void SerializeWithLengthPrefix<T>(Stream destination, T instance, PrefixStyle style)
        {
            SerializeWithLengthPrefix<T>(destination, instance, style, 0);
        }

        internal static uint GetFieldToken(int tag, WireType wireType)
        {
            return (uint)((tag << 3) | ((int)wireType));            
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream,
        /// with a length-prefix. This is useful for socket programming,
        /// as DeserializeWithLengthPrefix/MergeWithLengthPrefix can be used to read the single object back
        /// from an ongoing stream.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="destination">The destination stream to write to.</param>
        /// <param name="tag">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
        public static void SerializeWithLengthPrefix<T>(Stream destination, T instance, PrefixStyle style, int tag)
        {
            if(style == PrefixStyle.None)
            {
                Serialize<T>(destination, instance);
                return;
            }
            using (MemoryStream ms = new MemoryStream())
            {
                Serialize<T>(ms, instance);
                byte[] tmp = new byte[20];
                int len;
                switch(style)
                {
                    case PrefixStyle.Base128:
                        len = 0;
                        if(tag > 0)
                        {
                            uint prefix = GetFieldToken(tag, WireType.String);
                            len += SerializationContext.EncodeUInt32(prefix, tmp, 0);
                        }
                        len += SerializationContext.EncodeUInt32((uint)ms.Length, tmp, len);
                        break;
                    case PrefixStyle.Fixed32:
                        SerializationContext.EncodeUInt32Fixed((uint)ms.Length, tmp, 0);
                        len = 4;
                        break;
                    default:
                        throw new NotSupportedException("Invalid prefix style: " + style);
                }
                destination.Write(tmp, 0, len);
                destination.Write(ms.GetBuffer(), 0, (int)ms.Length);
            }
            destination.Flush();
        }

        private const string ProtoBinaryField = "proto";

#if REMOTING

        /// <summary>
        /// Creates a new IFormatter that uses protocol-buffer [de]serialization.
        /// </summary>
        /// <typeparam name="T">The type of object to be [de]deserialized by the formatter.</typeparam>
        /// <returns>A new IFormatter to be used during [de]serialization.</returns>
        public static IFormatter CreateFormatter<T>()
        {
            return new Formatter<T>();
        }

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied SerializationInfo.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="info">The destination SerializationInfo to write to.</param>
        public static void Serialize<T>(SerializationInfo info, T instance) where T : class, ISerializable
        {
            // note: also tried byte[]... it doesn't perform hugely well with either (compared to regular serialization)
            if (info == null) throw new ArgumentNullException("info");
            if (instance == null) throw new ArgumentNullException("instance");
            if (instance.GetType() != typeof(T)) throw new ArgumentException("Incorrect type", "instance");
            using (MemoryStream ms = new MemoryStream())
            {
                Serialize<T>(ms, instance);
                info.AddValue(ProtoBinaryField, ms.ToArray());
                //string s = Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
                //info.AddValue(ProtoBinaryField, s);
            }
        }

        
        

        /// <summary>
        /// Applies a protocol-buffer from a SerializationInfo to an existing instance.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (cannot be null).</param>
        /// <param name="info">The SerializationInfo containing the data to apply to the instance (cannot be null).</param>
        public static void Merge<T>(SerializationInfo info, T instance) where T : class, ISerializable
        {
            // note: also tried byte[]... it doesn't perform hugely well with either (compared to regular serialization)
            if (info == null) throw new ArgumentNullException("info");
            if (instance == null) throw new ArgumentNullException("instance");
            if (instance.GetType() != typeof(T)) throw new ArgumentException("Incorrect type", "instance");

            //string s = info.GetString(ProtoBinaryField);
            //using (MemoryStream ms = new MemoryStream(Convert.FromBase64String(s)))
            byte[] buffer = (byte[])info.GetValue(ProtoBinaryField, typeof(byte[]));
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                T result = Merge<T>(ms, instance);
                if(!ReferenceEquals(result,instance))
                {
                    throw new ProtoException("Deserialization changed the instance; cannot succeed.");
                }
            }
        }
#endif

        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied XmlWriter.
        /// </summary>
        /// <typeparam name="T">The type being serialized.</typeparam>
        /// <param name="instance">The existing instance to be serialized (cannot be null).</param>
        /// <param name="writer">The destination XmlWriter to write to.</param>
        public static void Serialize<T>(System.Xml.XmlWriter writer, T instance) where T : IXmlSerializable
        {
            if (writer == null) throw new ArgumentNullException("writer");
            if (instance == null) throw new ArgumentNullException("instance");

            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, instance);
                writer.WriteBase64(ms.GetBuffer(), 0, (int)ms.Length);
            }
        }
        /// <summary>
        /// Applies a protocol-buffer from an XmlReader to an existing instance.
        /// </summary>
        /// <typeparam name="T">The type being merged.</typeparam>
        /// <param name="instance">The existing instance to be modified (cannot be null).</param>
        /// <param name="reader">The XmlReader containing the data to apply to the instance (cannot be null).</param>
        public static void Merge<T>(System.Xml.XmlReader reader, T instance) where T : IXmlSerializable
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (instance == null) throw new ArgumentNullException("instance");
        
            const int LEN = 4096;
            byte[] buffer = new byte[LEN];
            int read;
            using (MemoryStream ms = new MemoryStream())
            {
                while ((read = reader.ReadElementContentAsBase64(buffer, 0, LEN)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                ms.Position = 0;
                Serializer.Merge(ms, instance);
            }
        }

        /// <summary>
        /// Create a deep clone of the supplied instance; any sub-items are also cloned.
        /// </summary>
        /// <typeparam name="T">The type being cloned.</typeparam>
        /// <param name="instance">The existing instance to be cloned.</param>
        /// <returns>A new copy, cloned from the supplied instance.</returns>
        public static T DeepClone<T>(T instance)
        {
            return ChangeType<T, T>(instance);
        }

        /// <summary>
        /// Serializes a given instance and deserializes it as a different type;
        /// this can be used to translate between wire-compatible objects (where
        /// two .NET types represent the same data), or to promote/demote a type
        /// through an inheritance hierarchy.
        /// </summary>
        /// <remarks>No assumption of compatibility is made between the types.</remarks>
        /// <typeparam name="TOldType">The type of the object being copied.</typeparam>
        /// <typeparam name="TNewType">The type of the new object to be created.</typeparam>
        /// <param name="instance">The existing instance to use as a template.</param>
        /// <returns>A new instane of type TNewType, with the data from TOldType.</returns>
        public static TNewType ChangeType<TOldType, TNewType>(TOldType instance)
        {
            return ChangeType<TOldType, TNewType>(instance, null);
        }

        /// <summary>
        /// As per the public ChangeType, but allows for workspace-sharing to reduce buffer overhead.
        /// </summary>
        internal static TNewType ChangeType<TOldType, TNewType>(TOldType instance, SerializationContext context)
        {
            if (instance == null)
            {
                return default(TNewType); // GIGO
            } 

            using (MemoryStream ms = new MemoryStream())
            {
                SerializationContext tmpCtx = new SerializationContext(ms, context);
                Serialize<TOldType>(ms, instance);
                tmpCtx.Flush();

                ms.Position = 0;
                TNewType result = Deserialize<TNewType>(ms);
                if (context != null)
                {
                    context.ReadFrom(tmpCtx);
                }
                return result;
            }
        }
#if !CF
        /// <summary>
        /// Suggest a .proto definition for the given type
        /// </summary>
        /// <typeparam name="T">The type to generate a .proto definition for</typeparam>
        /// <returns>The .proto definition as a string</returns>
        public static string GetProto<T>() where T : class
        {
            try
            {
                return Serializer<T>.GetProto();
            }
            catch (Exception ex)
            {
                ThrowInner(ex);
                throw; // if no inner (preserves stacktrace)
            }
        }
#endif

        static void ThrowInner(Exception exception)
        {
            if (exception != null && exception.InnerException != null)
            {
                if (exception is TargetInvocationException
#if !CF
                    || exception is TypeInitializationException
#endif
                )
                {
                    ThrowInner(exception.InnerException);
                    throw exception.InnerException;
                }
            }
        }

        internal static string GetDefinedTypeName<T>()
        {
            Entity e = Entity.Get(typeof(T));
            return e == null ? typeof(T).Name : e.Name;
        }
        
        internal static int GetPrefixLength(int tag)
        {
            if ((tag & ~0x0000000F) == 0) return 1; // 4 bits
            if ((tag & ~0x000007FF) == 0) return 2; // 11 bits
            if ((tag & ~0x0003FFFF) == 0) return 3; // 18 bits
            if ((tag & ~0x01FFFFFF) == 0) return 4; // 25 bits
            return 5;            
        }

        internal static void ParseFieldToken(uint token, out WireType wireType, out int tag)
        {
            wireType = (WireType)(token & 7);
            tag = (int)(token >> 3);
            if (tag <= 0)
            {
                throw new ProtoException("Invalid tag: " + tag.ToString());
            }
        }

        internal static void SkipData(SerializationContext context, int fieldTag, WireType wireType)
        {

            switch (wireType)
            {
                case WireType.Variant:
                    context.ReadRawVariant();
                    break;
                case WireType.Fixed32:
                    context.ReadBlock(4);
                    break;
                case WireType.Fixed64:
                    context.ReadBlock(8);
                    break;
                case WireType.String:
                    int len = context.DecodeInt32();
                    context.WriteTo(Stream.Null, len);
                    break;
                case WireType.EndGroup:
                    throw new ProtoException("End-group not expected at this location");
                case WireType.StartGroup:
                    context.StartGroup(fieldTag); // will be ended internally
                    Serializer<UnknownType>.Build();
                    UnknownType ut = UnknownType.Default;
                    Serializer<UnknownType>.Deserialize<UnknownType>(ref ut, context);
                    break;
                default:
                    throw new ProtoException("Unknown wire-type " + wireType.ToString());
            }
        }

        internal static int WriteFieldToken(int tag, WireType wireType, SerializationContext context)
        {
            uint prefix = GetFieldToken(tag, wireType);
            return context.EncodeUInt32(prefix);
        }

        internal struct ProtoEnumValue<TEnum>
        {
            private readonly TEnum enumValue;
            private readonly int wireValue;
            private readonly string name;
            public TEnum EnumValue { get { return enumValue; } }
            public int WireValue { get { return wireValue; } }
            public string Name { get { return name; } }
            public ProtoEnumValue(TEnum enumValue, int wireValue, string name)
            {
                this.enumValue = enumValue;
                this.wireValue = wireValue;
                this.name = name;
            }
        }

        internal static IEnumerable<ProtoEnumValue<TEnum>> GetEnumValues<TEnum>()
        {
            List<ProtoEnumValue<TEnum>> list = new List<ProtoEnumValue<TEnum>>();
            foreach (FieldInfo enumField in typeof(TEnum).GetFields(BindingFlags.Static | BindingFlags.Public))
            {
                if (!enumField.IsLiteral)
                {
                    continue;
                }
                
                TEnum key = (TEnum)enumField.GetValue(null);
                ProtoEnumAttribute ea = AttributeUtils.GetAttribute<ProtoEnumAttribute>(enumField);
                int value;
                string name = (ea == null || string.IsNullOrEmpty(ea.Name)) ? enumField.Name : ea.Name;

                if (ea == null || !ea.HasValue())
                {
                    value = (int)Convert.ChangeType(key, typeof(int), CultureInfo.InvariantCulture);
                }
                else
                {
                    value = (int)ea.Value;
                }

                list.Add(new ProtoEnumValue<TEnum>(key, value, name));
            }
            list.Sort(delegate(ProtoEnumValue<TEnum> x, ProtoEnumValue<TEnum> y)
            {
                return x.WireValue.CompareTo(y.WireValue);
            });
            return list;
        }

        /// <summary>
        /// Ensures that the serialization algorithm has been prepared for
        /// the given type; this can be useful in highly threaded code to
        /// ensure that all types are ready ahead of time, avoiding deadlock
        /// scenarios.
        /// </summary>
        /// <typeparam name="T">The object type to prepare.</typeparam>
        public static void PrepareSerializer<T>() where T : class
        {
            Serializer<T>.Build();
        }
    }
}
