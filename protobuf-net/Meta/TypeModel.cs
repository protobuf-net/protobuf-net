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
        private WireType GetDefaultWireType(TypeCode code, Type type, out bool isSubObject)
        {
            isSubObject = false;
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
                case TypeCode.DateTime:
                case TypeCode.Decimal:
                    return WireType.String;
            }
            if (type == typeof(byte[]) || type == typeof(TimeSpan)
                || type == typeof(Guid) || type == typeof(Uri)) return WireType.String;

            if (GetKey(type) >= 0)
            {
                isSubObject = true;
                return WireType.String;
            }
            return WireType.None;
        }
        /// <summary>
        /// Handles simple cases of types; in particular, simple values are mapped directly (we can't
        /// assume we have the runtime/decorator logic), and sub-objects are written from sequences
        /// </summary>
        private bool TrySerializeAuxiliaryType(ProtoWriter writer,  Type type, object value)
        {
            bool isSubObject;
            TypeCode typecode = Type.GetTypeCode(type);
            WireType wireType = GetDefaultWireType(typecode, type, out isSubObject);
            Helpers.DebugAssert(!isSubObject); // definitely shouldn't be a nested message if we recognise it
                                              // since mapped types should have already been handled
            if(wireType != WireType.None) {
                ProtoWriter.WriteFieldHeader(Serializer.ListItemTag, wireType, writer);
            }
            switch(typecode) {
                case TypeCode.Int16: ProtoWriter.WriteInt16((short)value, writer); return true;
                case TypeCode.Int32: ProtoWriter.WriteInt32((int)value, writer); return true;
                case TypeCode.Int64: ProtoWriter.WriteInt64((long)value, writer); return true;
                case TypeCode.UInt16: ProtoWriter.WriteUInt16((ushort)value, writer); return true;
                case TypeCode.UInt32: ProtoWriter.WriteUInt32((uint)value, writer); return true;
                case TypeCode.UInt64: ProtoWriter.WriteUInt64((ulong)value, writer); return true;
                case TypeCode.Boolean: ProtoWriter.WriteBoolean((bool)value, writer); return true;
                case TypeCode.SByte: throw new NotImplementedException(); 
                case TypeCode.Byte: throw new NotImplementedException(); 
                case TypeCode.Char: ProtoWriter.WriteUInt16((ushort)(char)value, writer); return true;
                case TypeCode.Double: ProtoWriter.WriteDouble((double)value, writer); return true;
                case TypeCode.Single: ProtoWriter.WriteSingle((float)value, writer); return true;
                case TypeCode.DateTime: BclHelpers.WriteDateTime((DateTime)value, writer); return true;
                case TypeCode.Decimal: BclHelpers.WriteDecimal((decimal)value, writer); return true;
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
                    type = item.GetType();
                    wireType = GetDefaultWireType(Type.GetTypeCode(type), type, out isSubObject);
                    if (wireType == WireType.None)
                    {
                        ThrowUnexpectedType(type);
                    }
                    ProtoWriter.WriteFieldHeader(Serializer.ListItemTag, wireType, writer);
                    if (isSubObject)
                    {   // needs a wrapping length etc
                        SubItemToken token = ProtoWriter.StartSubItem(item, writer);
                        SerializeCore(writer, item);
                        ProtoWriter.EndSubItem(token, writer);
                    }
                    else
                    {
                        SerializeCore(writer, item);
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
            int key = GetKey(type);
            if (key >= 0)
            {
                Serialize(key, value, writer);
            }
            else if (!TrySerializeAuxiliaryType(writer, type, value))
            {
                ThrowUnexpectedType(type);
            }
        }
        public void Serialize(Stream dest, object value)
        {
            using (ProtoWriter writer = new ProtoWriter(dest, this))
            {
                SerializeCore(writer, value);
                writer.Close();
            }
        }
        public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int fieldNumber)
        {
            return DeserializeWithLengthPrefix(source, value, type, style, fieldNumber, null);
        }
        public object DeserializeWithLengthPrefix(Stream source, object value, Type type, PrefixStyle style, int expectedField, Serializer.TypeResolver resolver)
        {
            bool skip;
            int len;
            do
            {
                int actualField;
                bool expectPrefix = expectedField > 0 || resolver != null;
                len = ProtoReader.ReadLengthPrefix(source, expectPrefix, style, out actualField);
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
                }
            } while (skip);

            int key = GetKey(type);
            if (key < 0) throw new InvalidOperationException();
            using (ProtoReader reader = new ProtoReader(source, this, len))
            {
                return Deserialize(key, value, reader);
            }

        }
        public void SerializeWithLengthPrefix(Stream dest, object value, Type type, PrefixStyle style, int fieldNumber)
        {
            if (type == null)
            {
                if(value == null) throw new ArgumentNullException("value");
                type = value.GetType();
            }
            int key = GetKey(type);
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
            int key = GetKey(type);
            if (key >= 0)
            {
                return Deserialize(key, value, reader);
            }
            else if (TryDeserializeAuxiliaryType(reader, type, ref value))
            {
                return value;
            }
            ThrowUnexpectedType(type);
            return null; // throws
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

            return candidates.Count == 2 ? (Type)candidates[1] : null;
        }
        private bool TryDeserializeAuxiliaryType(ProtoReader reader, Type type, ref object value)
        {
            Type itemType = null;
            TypeCode itemTypeCode = TypeCode.Empty, typecode = Type.GetTypeCode(type);
            bool isSubObject;
            WireType wiretype = GetDefaultWireType(typecode, type, out isSubObject);
            Helpers.DebugAssert(!isSubObject); // definitely shouldn't be a nested message if we recognise it
                                               // since mapped types should have already been handled
            bool found = false, handled = wiretype != WireType.None;
            if (!handled)
            {
                itemType = GetListItemType(type);
                if (itemType == null ||
                    GetDefaultWireType((itemTypeCode = Type.GetTypeCode(itemType)), itemType, out isSubObject) == WireType.None)
                {
                    ThrowUnexpectedType(type);
                }
                handled = true;
            }
            int fieldNumber;
            
            // to treat correctly, should read all values
            while ((fieldNumber = reader.ReadFieldHeader()) > 0)
            {
                if (fieldNumber == Serializer.ListItemTag)
                {
                    found = true;
                    switch (typecode)
                    {
                        case TypeCode.Int16: value = reader.ReadInt16(); continue;
                        case TypeCode.Int32: value = reader.ReadInt32(); continue;
                        case TypeCode.Int64: value = reader.ReadInt64(); continue;
                        case TypeCode.UInt16: value = reader.ReadUInt16(); continue;
                        case TypeCode.UInt32: value = reader.ReadUInt32(); continue;
                        case TypeCode.UInt64: value = reader.ReadUInt64(); continue;
                        case TypeCode.Boolean: value = reader.ReadBoolean(); continue;
                        case TypeCode.SByte: throw new NotImplementedException();
                        case TypeCode.Byte: throw new NotImplementedException();
                        case TypeCode.Char: value = (char)reader.ReadUInt16(); continue;
                        case TypeCode.Double: value = reader.ReadDouble(); continue;
                        case TypeCode.Single: value = reader.ReadSingle(); continue;
                        case TypeCode.DateTime: value = BclHelpers.ReadDateTime(reader); continue;
                        case TypeCode.Decimal: BclHelpers.ReadDecimal(reader); continue;
                    }
                    if (type == typeof(byte[])) { value = ProtoReader.AppendBytes((byte[])value,reader); continue; }
                    if (type == typeof(TimeSpan)) { value = BclHelpers.ReadTimeSpan(reader); continue; }
                    if (type == typeof(Guid)) { value = BclHelpers.ReadGuid(reader); continue; }
                    if (type == typeof(Uri)) { value = new Uri(reader.ReadString()); continue; }

                    if (itemType != null)
                    {
                        object newItem;
                        IList list = (IList)value;
                        if (isSubObject)
                        {
                            SubItemToken token = ProtoReader.StartSubItem(reader);
                            newItem = DeserializeCore(reader, itemType, null);
                            ProtoReader.EndSubItem(token, reader);
                        }
                        else
                        {
                            newItem = DeserializeCore(reader, itemType, null);
                        }
                        //todo: null lists
                        if (list == null)
                        {
                            Type concreteListType = type;
                            if(!type.IsClass || type.IsAbstract || type.GetConstructor(
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                    null, Helpers.EmptyTypes, null) == null)
                            {
                                concreteListType = typeof(System.Collections.Generic.List<>).MakeGenericType(itemType);
                            }
                            list = (IList)(value = Activator.CreateInstance(concreteListType));
                        }
                        list.Add(newItem);
                    }
                }
                reader.SkipField();
            }
            if (!found) value = Activator.CreateInstance(type);
            return handled;
        }

#if !NO_RUNTIME
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

            // NHibernate
            if (type.GetInterface("NHibernate.Proxy.INHibernateProxy", false) != null) return type.BaseType;

            return null;
        }


        protected internal int GetKey(Type type)
        {
            int key = GetKeyImpl(type);
            if (key < 0)
            {
                type = ResolveProxies(type);
                if (type != null) key = GetKeyImpl(type);
            }
            return key;
        }

        protected abstract int GetKeyImpl(Type type);
        protected internal abstract void Serialize(int key, object value, ProtoWriter dest);
        protected internal abstract object Deserialize(int key, object value, ProtoReader source);
        
        //internal ProtoSerializer Create(IProtoSerializer head)
        //{
        //    return new RuntimeSerializer(head, this);
        //}
        //internal ProtoSerializer Compile

        protected internal enum CallbackType
        {
            BeforeSerialize, AfterSerialize, BeforeDeserialize, AfterDeserialize
        }

        public object DeepClone(object value)
        {
            if (value == null) return null;
            Type type = value.GetType();
            int key = GetKey(type);

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
            bool isSubObject;
            if (type == typeof(byte[])) {
                byte[] orig = (byte[])value, clone = new byte[orig.Length];
                Helpers.BlockCopy(orig, 0, clone, 0, orig.Length);
                return clone;
            }
            else if (GetDefaultWireType(Type.GetTypeCode(type), type, out isSubObject) != WireType.None && !isSubObject)
            { // immutable; just return the original value
                return value;
            }
            using (MemoryStream ms = new MemoryStream())
            {
                using (ProtoWriter writer = new ProtoWriter(ms, this))
                {
                    if (!TrySerializeAuxiliaryType(writer, type, value)) ThrowUnexpectedType(type);
                    writer.Close();
                }
                ms.Position = 0;
                using (ProtoReader reader = new ProtoReader(ms, this))
                {
                    if (!TryDeserializeAuxiliaryType(reader, type, ref value)) ThrowUnexpectedType(type);
                    return value;
                }
            }
            

        }
        protected internal static void ThrowUnexpectedSubtype(Type expected, Type actual)
        {
            if (expected != TypeModel.ResolveProxies(actual))
            {
                throw new InvalidOperationException("Unexpected sub-type: " + actual.FullName);
            }
        }
        protected static void ThrowUnexpectedType(Type type)
        {
            throw new InvalidOperationException("Type is not expected, and no contract can be inferred: " + type.FullName);
        }
        protected internal static void ThrowCannotCreateInstance(Type type)
        {
            throw new InvalidOperationException("Cannot create an instance of type; no suitable constructor found: " + type.FullName);
        }
    }

}
