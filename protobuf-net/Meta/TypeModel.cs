using System;
using System.IO;
using System.Collections;
using System.Reflection;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Provides protobuf serialization support for a number of types
    /// </summary>
    public abstract class TypeModel
    {
        private WireType GetWireType(TypeCode code, DataFormat format, ref Type type, out int modelKey)
        {
            modelKey = -1;
            if (type.IsEnum) return WireType.None;
            switch (code)
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                case TypeCode.Char:
                    return WireType.Variant;
                case TypeCode.Double:
                    return WireType.Fixed64;
                case TypeCode.Single:
                    return WireType.Fixed32;
                case TypeCode.String:
                case TypeCode.DateTime:
                case TypeCode.Decimal:
                    return WireType.String;
            }
            if (type == typeof(byte[]) || type == typeof(TimeSpan)
                || type == typeof(Guid) || type == typeof(Uri)) return WireType.String;

            if ((modelKey = GetKey(ref type)) >= 0)
            {
                return WireType.String;
            }
            return WireType.None;
        }
        /// <summary>
        /// This is the more "complete" version of Serialize, which handles single instances of mapped types.
        /// The value is written as a complete field, including field-header and (for sub-objects) a
        /// length-prefix
        /// In addition to that, this provides support for:
        ///  - basic values; individual int / string / Guid / etc
        ///  - IEnumerable sequences of any type handled by TrySerializeAuxiliaryType
        ///  
        /// </summary>
        internal bool TrySerializeAuxiliaryType(ProtoWriter writer,  Type type, DataFormat format, int tag, object value)
        {
            if (type == null) { type = value.GetType(); }

            TypeCode typecode = Type.GetTypeCode(type);
            int modelKey;
            // note the "ref type" here normalizes against proxies
            WireType wireType = GetWireType(typecode, format, ref type, out modelKey);


            if (modelKey >= 0)
            {   // write the header, but defer to the model
                ProtoWriter.WriteFieldHeader(tag, wireType, writer);
                switch (wireType)
                {
                    case WireType.None:
                        throw ProtoWriter.CreateException(writer);
                    case WireType.StartGroup:
                    case WireType.String:
                        // needs a wrapping length etc
                        SubItemToken token = ProtoWriter.StartSubItem(value, writer);
                        Serialize(modelKey, value, writer);
                        ProtoWriter.EndSubItem(token, writer);
                        return true;
                    default:
                        Serialize(modelKey, value, writer);
                        return true;
                }                
            }
            
            if(wireType != WireType.None) {
                ProtoWriter.WriteFieldHeader(tag, wireType, writer);
            }
            switch(typecode) {
                case TypeCode.Int16: ProtoWriter.WriteInt16((short)value, writer); return true;
                case TypeCode.Int32: ProtoWriter.WriteInt32((int)value, writer); return true;
                case TypeCode.Int64: ProtoWriter.WriteInt64((long)value, writer); return true;
                case TypeCode.UInt16: ProtoWriter.WriteUInt16((ushort)value, writer); return true;
                case TypeCode.UInt32: ProtoWriter.WriteUInt32((uint)value, writer); return true;
                case TypeCode.UInt64: ProtoWriter.WriteUInt64((ulong)value, writer); return true;
                case TypeCode.Boolean: ProtoWriter.WriteBoolean((bool)value, writer); return true;
                case TypeCode.SByte: ProtoWriter.WriteSByte((sbyte)value, writer); return true;
                case TypeCode.Byte: ProtoWriter.WriteByte((byte)value, writer); return true;
                case TypeCode.Char: ProtoWriter.WriteUInt16((ushort)(char)value, writer); return true;
                case TypeCode.Double: ProtoWriter.WriteDouble((double)value, writer); return true;
                case TypeCode.Single: ProtoWriter.WriteSingle((float)value, writer); return true;
                case TypeCode.DateTime: BclHelpers.WriteDateTime((DateTime)value, writer); return true;
                case TypeCode.Decimal: BclHelpers.WriteDecimal((decimal)value, writer); return true;
                case TypeCode.String: ProtoWriter.WriteString((string)value, writer); return true;
            }
            if (type == typeof(byte[]))  {ProtoWriter.WriteBytes((byte[])value, writer); return true;}
            if (type == typeof(TimeSpan)) { BclHelpers.WriteTimeSpan((TimeSpan)value, writer); return true;}
            if (type == typeof(Guid))  { BclHelpers.WriteGuid((Guid)value, writer); return true;}
            if (type == typeof(Uri)) {  ProtoWriter.WriteString(((Uri)value).AbsoluteUri, writer); return true;}

            // by now, we should have covered all the simple cases; if we wrote a field-header, we have
            // forgotten something!
            Helpers.DebugAssert(wireType == WireType.None);

            // now attempt to handle sequences (including arrays and lists)
            IEnumerable sequence = value as IEnumerable;
            if (sequence != null)
            {
                foreach (object item in sequence) {
                    if (item == null) { throw new NullReferenceException(); }
                    if (!TrySerializeAuxiliaryType(writer, null, format, tag, item))
                    {
                        ThrowUnexpectedType(item.GetType());
                    }
                }
                return true;
            }
            return false;
        }
        private void SerializeCore(ProtoWriter writer, object value)
        {
            if (value == null) throw new ArgumentNullException("value");
            Type type = value.GetType();
            int key = GetKey(ref type);
            if (key >= 0)
            {
                Serialize(key, value, writer);
            }
            else if (!TrySerializeAuxiliaryType(writer, type, DataFormat.Default, Serializer.ListItemTag, value))
            {
                ThrowUnexpectedType(type);
            }
        }
        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="dest">The destination stream to write to.</param>
        public void Serialize(Stream dest, object value)
        {
            using (ProtoWriter writer = new ProtoWriter(dest, this))
            {
                SerializeCore(writer, value);
                writer.Close();
            }
        }
        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
        /// data - useful with network IO.
        /// </summary>
        /// <param name="type">The type being merged.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="fieldNumber">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int fieldNumber)
        {
            int bytesRead;
            return DeserializeWithLengthPrefix(source, value, type, style, fieldNumber, null, out bytesRead);
        }


        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
        /// data - useful with network IO.
        /// </summary>
        /// <param name="type">The type being merged.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="expectedField">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
        /// <param name="resolver">Used to resolve types on a per-field basis.</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int expectedField, Serializer.TypeResolver resolver)
        {
            int bytesRead;
            return DeserializeWithLengthPrefix(source, value, type, style, expectedField, resolver, out bytesRead);
        }

        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (or null), using length-prefixed
        /// data - useful with network IO.
        /// </summary>
        /// <param name="type">The type being merged.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="expectedField">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
        /// <param name="resolver">Used to resolve types on a per-field basis.</param>
        /// <param name="bytesRead">Returns the number of bytes consumed by this operation (includes length-prefix overheads and any skipped data).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int expectedField, Serializer.TypeResolver resolver, out int bytesRead)
        {
            bool skip;
            int len;
            int tmpBytesRead;
            bytesRead = 0;
            do
            {
                int actualField;
                bool expectPrefix = expectedField > 0 || resolver != null;
                len = ProtoReader.ReadLengthPrefix(source, expectPrefix, style, out actualField, out tmpBytesRead);
                bytesRead += tmpBytesRead;
                if (len < 0) return value;

                if (expectedField == 0 && type == null && resolver != null)
                {
                    type = resolver(actualField);
                    skip = type == null;
                }
                else { skip = expectedField != actualField; }

                if (skip)
                {
                    if (len == int.MaxValue) throw new InvalidOperationException();
                    ProtoReader.Seek(source, len, null);
                    bytesRead += len;
                }
            } while (skip);

            int key = GetKey(ref type);
            if (key < 0) throw new InvalidOperationException();
            using (ProtoReader reader = new ProtoReader(source, this, len))
            {
                object result = Deserialize(key, value, reader);
                bytesRead += reader.Position;
                return result;
            }

        }
        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream,
        /// with a length-prefix. This is useful for socket programming,
        /// as DeserializeWithLengthPrefix can be used to read the single object back
        /// from an ongoing stream.
        /// </summary>
        /// <param name="type">The type being serialized.</param>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="style">How to encode the length prefix.</param>
        /// <param name="dest">The destination stream to write to.</param>
        /// <param name="fieldNumber">The tag used as a prefix to each record (only used with base-128 style prefixes).</param>
        public void SerializeWithLengthPrefix(Stream dest, object value, Type type, PrefixStyle style, int fieldNumber)
        {
            if (type == null)
            {
                if(value == null) throw new ArgumentNullException("value");
                type = value.GetType();
            }
            int key = GetKey(ref type);
            using (ProtoWriter writer = new ProtoWriter(dest, this))
            {
                switch (style)
                {
                    case PrefixStyle.None:
                        Serialize(key, value, writer);
                        break;
                    case PrefixStyle.Base128:
                    case PrefixStyle.Fixed32:
                    case PrefixStyle.Fixed32BigEndian:
                        ProtoWriter.WriteObject(value, key, writer, style, fieldNumber);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("style");
                }
                writer.Close();
            }
        }
        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="type">The type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        public object Deserialize(Stream source, object value, Type type)
        {
            if (type == null)
            {
                if (value == null)
                {
                    throw new ArgumentNullException("type");
                } else {
                    type = value.GetType();
                }
            }
#if !NO_GENERICS
            type = Nullable.GetUnderlyingType(type) ?? type;
#endif
            using (ProtoReader reader = new ProtoReader(source, this))
            {
                return DeserializeCore(reader, type, value);
            }
        }
        private object DeserializeCore(ProtoReader reader, Type type, object value)
        {
            int key = GetKey(ref type);
            if (key >= 0)
            {
                return Deserialize(key, value, reader);
            }
            // this returns true to say we actively found something, but a value is assigned either way (or throws)
            TryDeserializeAuxiliaryType(reader, DataFormat.Default, Serializer.ListItemTag, type, ref value, true, false);
            return value;
        }
        internal static MethodInfo ResolveListAdd(Type listType, Type itemType, out bool isList)
        {
            isList = typeof(IList).IsAssignableFrom(listType);
            Type[] types = { itemType };
            MethodInfo add = listType.GetMethod("Add", types);
            if (add == null)
            {   // fallback: look for ICollection<T>'s Add(typedObject) method
                Type constuctedListType = typeof(System.Collections.Generic.ICollection<>).MakeGenericType(types);
                if (constuctedListType.IsAssignableFrom(listType))
                {
                    add = constuctedListType.GetMethod("Add", types);
                }
            }

            if (add == null)
            {   // fallback: look for a public list.Add(object) method
                types[0] = typeof(object);
                add = listType.GetMethod("Add", types);
            }
            if (add == null && isList)
            {   // fallback: look for IList's Add(object) method
                add = typeof(IList).GetMethod("Add", types);
            }
            return add;
        }
        internal static Type GetListItemType(Type listType)
        {
            Helpers.DebugAssert(listType != null);
            if (listType == typeof(string) || listType.IsArray
                || !typeof(IEnumerable).IsAssignableFrom(listType)) return null;

            BasicList candidates = new BasicList();
            candidates.Add(typeof(object));
            foreach (MethodInfo method in listType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (method.Name != "Add") continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 1 && !candidates.Contains(parameters[0].ParameterType))
                {
                    candidates.Add(parameters[0].ParameterType);
                }
            }
            foreach (Type iType in listType.GetInterfaces())
            {
                if (iType.IsGenericType && iType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.ICollection<>))
                {
                    Type[] iTypeArgs = iType.GetGenericArguments();
                    if (!candidates.Contains(iTypeArgs[0]))
                    {
                        candidates.Add(iTypeArgs[0]);
                    }
                }
            }
            // more convenient GetProperty overload not supported on all platforms
            foreach (PropertyInfo indexer in listType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (indexer.Name != "Item" || candidates.Contains(indexer.PropertyType)) continue;
                ParameterInfo[] args = indexer.GetIndexParameters();
                if (args.Length != 1 || args[0].ParameterType != typeof(int)) continue;
                candidates.Add(indexer.PropertyType);
            }

            switch (candidates.Count)
            {
                case 1:
                    return null;
                case 2:
                    return (Type)candidates[1];
                case 3:
                    if (CheckDictionaryAccessors((Type)candidates[1], (Type)candidates[2])) return (Type)candidates[1];
                    if (CheckDictionaryAccessors((Type)candidates[2], (Type)candidates[1])) return (Type)candidates[2];
                    break;
            }

            return null;
        }

        private static bool CheckDictionaryAccessors(Type pair, Type value)
        {
            return pair.IsGenericType && pair.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>)
                && pair.GetGenericArguments()[1] == value;
        }

        private bool TryDeserializeList(ProtoReader reader, DataFormat format, int tag, Type listType, Type itemType, ref object value)
        {
            bool isList;
            MethodInfo addMethod = TypeModel.ResolveListAdd(listType, itemType, out isList);
            if (addMethod == null) throw new NotSupportedException("Unknown list variant: " + listType.FullName);
            bool found = false;
            object nextItem = null;
            IList list = value as IList;
            object[] args = isList ? null : new object[1];
            while (TryDeserializeAuxiliaryType(reader, format, tag, itemType, ref nextItem, true, true))
            {
                found = true;
                if (value == null)
                {
                    Type concreteListType = listType;
                    if (!listType.IsClass || listType.IsAbstract || listType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                            null, Helpers.EmptyTypes, null) == null)
                    {
                        concreteListType = typeof(System.Collections.Generic.List<>).MakeGenericType(itemType);
                    }
                    value = (Activator.CreateInstance(concreteListType));
                    list = value as IList;
                }
                if (list == null)
                {
                    args[0] = nextItem;
                    addMethod.Invoke(value, args);
                }
                else
                {
                    list.Add(nextItem);
                }
                nextItem = null;
            }
            return found;
        }

        /// <summary>
        /// This is the more "complete" version of Deserialize, which handles single instances of mapped types.
        /// The value is read as a complete field, including field-header and (for sub-objects) a
        /// length-prefix..kmc  
        /// 
        /// In addition to that, this provides support for:
        ///  - basic values; individual int / string / Guid / etc
        ///  - IList sets of any type handled by TryDeserializeAuxiliaryType
        /// </summary>
        private bool TryDeserializeAuxiliaryType(ProtoReader reader, DataFormat format, int tag, Type type, ref object value, bool skipOtherFields, bool asListItem)
        {
            if (type == null) throw new ArgumentNullException("type");
            Type itemType = null;
            TypeCode typecode = Type.GetTypeCode(type);
            int modelKey;
            WireType wiretype = GetWireType(typecode, format, ref type, out modelKey);

            bool found = false;
            if (wiretype == WireType.None)
            {
                itemType = GetListItemType(type);

                if (itemType != null)
                {
                    return TryDeserializeList(reader, format, tag, type, itemType, ref value);                    
                }
                // otherwise, not a happy bunny...
                ThrowUnexpectedType(type);
            }
            
            // to treat correctly, should read all values

            while (true)
            {
                // for convenience (re complex exit conditions), additional exit test here:
                // if we've got the value, are only looking for one, and we aren't a list - then exit
                if (found && asListItem) break;

                // read the next item
                int fieldNumber = reader.ReadFieldHeader();
                if (fieldNumber <= 0) break;
                if (fieldNumber != tag)
                {
                    if (skipOtherFields)
                    {
                        reader.SkipField();
                        continue;
                    }
                    throw ProtoReader.AddErrorData(new InvalidOperationException(
                        "Expected field " + tag + ", but found " + fieldNumber), reader);
                }
                found = true;
                reader.Hint(wiretype); // handle signed data etc

                if (modelKey >= 0)
                {
                    switch (wiretype)
                    {
                        case WireType.String:
                        case WireType.StartGroup:
                            SubItemToken token = ProtoReader.StartSubItem(reader);
                            value = Deserialize(modelKey, value, reader);
                            ProtoReader.EndSubItem(token, reader);
                            continue;
                        default:
                            value = Deserialize(modelKey, value, reader);
                            continue;
                    }
                }
                switch (typecode)
                {
                    case TypeCode.Int16: value = reader.ReadInt16(); continue;
                    case TypeCode.Int32: value = reader.ReadInt32(); continue;
                    case TypeCode.Int64: value = reader.ReadInt64(); continue;
                    case TypeCode.UInt16: value = reader.ReadUInt16(); continue;
                    case TypeCode.UInt32: value = reader.ReadUInt32(); continue;
                    case TypeCode.UInt64: value = reader.ReadUInt64(); continue;
                    case TypeCode.Boolean: value = reader.ReadBoolean(); continue;
                    case TypeCode.SByte: value = reader.ReadSByte(); continue;
                    case TypeCode.Byte: value = reader.ReadByte(); continue;
                    case TypeCode.Char: value = (char)reader.ReadUInt16(); continue;
                    case TypeCode.Double: value = reader.ReadDouble(); continue;
                    case TypeCode.Single: value = reader.ReadSingle(); continue;
                    case TypeCode.DateTime: value = BclHelpers.ReadDateTime(reader); continue;
                    case TypeCode.Decimal: BclHelpers.ReadDecimal(reader); continue;
                    case TypeCode.String: value = reader.ReadString(); continue;
                }
                if (type == typeof(byte[])) { value = ProtoReader.AppendBytes((byte[])value, reader); continue; }
                if (type == typeof(TimeSpan)) { value = BclHelpers.ReadTimeSpan(reader); continue; }
                if (type == typeof(Guid)) { value = BclHelpers.ReadGuid(reader); continue; }
                if (type == typeof(Uri)) { value = new Uri(reader.ReadString()); continue; }

            }
            if (!found && !asListItem) { value = Activator.CreateInstance(type); }
            return found;
        }

#if !NO_RUNTIME
        /// <summary>
        /// Creates a new runtime model, to which the caller
        /// can add support for a range of types. A model
        /// can be used "as is", or can be compiled for
        /// optimal performance.
        /// </summary>
        public static RuntimeTypeModel Create()
        {
            return new RuntimeTypeModel(false);
        }
#endif

        /// <summary>
        /// Applies common proxy scenarios, resolving the actual type to consider
        /// </summary>
        protected internal static Type ResolveProxies(Type type)
        {
            if (type == null) return null;
            
            // Nullable<T>
            Type tmp = Nullable.GetUnderlyingType(type);
            if (tmp != null) return tmp;

#if !CF
            // NHibernate
            Type[] interfaces = type.GetInterfaces();
            for(int i = 0 ; i < interfaces.Length ; i++)
            {
                if(interfaces[i].FullName == "NHibernate.Proxy.INHibernateProxy") return type.BaseType;
            }
#endif
            return null;
        }
        /// <summary>
        /// Indicates whether the supplied type is explicitly modelled by the model
        /// </summary>
        public bool IsDefined(Type type)
        {
            return GetKey(ref type) >= 0;
        }
        /// <summary>
        /// Provides the key that represents a given type in the current model.
        /// The type is also normalized for proxies at the same time.
        /// </summary>
        protected internal int GetKey(ref Type type)
        {
            int key = GetKeyImpl(type);
            if (key < 0)
            {
                Type normalized = ResolveProxies(type);
                if (normalized != null) {
                    type = normalized; // hence ref
                    key = GetKeyImpl(type);
                }
            }
            return key;
        }

        /// <summary>
        /// Provides the key that represents a given type in the current model.
        /// </summary>
        protected abstract int GetKeyImpl(Type type);
        /// <summary>
        /// Writes a protocol-buffer representation of the given instance to the supplied stream.
        /// </summary>
        /// <param name="key">Represents the type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be serialized (cannot be null).</param>
        /// <param name="dest">The destination stream to write to.</param>
        protected internal abstract void Serialize(int key, object value, ProtoWriter dest);
        /// <summary>
        /// Applies a protocol-buffer stream to an existing instance (which may be null).
        /// </summary>
        /// <param name="key">Represents the type (including inheritance) to consider.</param>
        /// <param name="value">The existing instance to be modified (can be null).</param>
        /// <param name="source">The binary stream to apply to the instance (cannot be null).</param>
        /// <returns>The updated instance; this may be different to the instance argument if
        /// either the original instance was null, or the stream defines a known sub-type of the
        /// original instance.</returns>
        protected internal abstract object Deserialize(int key, object value, ProtoReader source);
        
        //internal ProtoSerializer Create(IProtoSerializer head)
        //{
        //    return new RuntimeSerializer(head, this);
        //}
        //internal ProtoSerializer Compile

        /// <summary>
        /// Indicates the type of callback to be used
        /// </summary>
        protected internal enum CallbackType
        {
            /// <summary>
            /// Invoked before an object is serialized
            /// </summary>
            BeforeSerialize,
            /// <summary>
            /// Invoked after an object is serialized
            /// </summary>
            AfterSerialize,
            /// <summary>
            /// Invoked before an object is deserialized (or when a new instance is created)
            /// </summary>            
            BeforeDeserialize,
            /// <summary>
            /// Invoked after an object is deserialized
            /// </summary>
            AfterDeserialize
        }
        /// <summary>
        /// Create a deep clone of the supplied instance; any sub-items are also cloned.
        /// </summary>
        public object DeepClone(object value)
        {
            if (value == null) return null;
            Type type = value.GetType();
            int key = GetKey(ref type);

            if (key >= 0) {
                using (MemoryStream ms = new MemoryStream())
                {
                    using(ProtoWriter writer = new ProtoWriter(ms, this))
                    {
                        Serialize(key, value, writer);
                        writer.Close();
                    }
                    ms.Position = 0;
                    using (ProtoReader reader = new ProtoReader(ms, this))
                    {
                        return Deserialize(key, null, reader);
                    }
                }
            }
            int modelKey;
            if (type == typeof(byte[])) {
                byte[] orig = (byte[])value, clone = new byte[orig.Length];
                Helpers.BlockCopy(orig, 0, clone, 0, orig.Length);
                return clone;
            }
            else if (GetWireType(Type.GetTypeCode(type), DataFormat.Default, ref type, out modelKey) != WireType.None && modelKey < 0)
            {   // immutable; just return the original value
                return value;
            }
            using (MemoryStream ms = new MemoryStream())
            {
                using (ProtoWriter writer = new ProtoWriter(ms, this))
                {
                    if (!TrySerializeAuxiliaryType(writer, type, DataFormat.Default, Serializer.ListItemTag, value)) ThrowUnexpectedType(type);
                    writer.Close();
                }
                ms.Position = 0;
                using (ProtoReader reader = new ProtoReader(ms, this))
                {
                    value = null; // start from scratch!
                    TryDeserializeAuxiliaryType(reader, DataFormat.Default, Serializer.ListItemTag, type, ref value, true, false);
                    return value;
                }
            }
            

        }

        /// <summary>
        /// Indicates that while an inheritance tree exists, the exact type encountered was not
        /// specified in that hierarchy and cannot be processed.
        /// </summary>
        protected internal static void ThrowUnexpectedSubtype(Type expected, Type actual)
        {
            if (expected != TypeModel.ResolveProxies(actual))
            {
                throw new InvalidOperationException("Unexpected sub-type: " + actual.FullName);
            }
        }
        /// <summary>
        /// Indicates that the given type was not expected, and cannot be processed.
        /// </summary>
        protected static void ThrowUnexpectedType(Type type)
        {
            throw new InvalidOperationException("Type is not expected, and no contract can be inferred: " + type.FullName);
        }
        /// <summary>
        /// Indicates that the given type cannot be constructed; it may still be possible to 
        /// deserialize into existing instances.
        /// </summary>
        protected internal static void ThrowCannotCreateInstance(Type type)
        {
            throw new InvalidOperationException("Cannot create an instance of type; no suitable constructor found: " + type.FullName);
        }
    }

}
