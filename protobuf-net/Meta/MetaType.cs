#if !NO_RUNTIME
using System;
using System.Collections;
using System.Reflection;
using ProtoBuf.Serializers;
using System.Text.RegularExpressions;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Represents a type at runtime for use with protobuf, allowing the field mappings (etc) to be defined
    /// </summary>
    public class MetaType : ISerializerProxy
    {
        IProtoSerializer ISerializerProxy.Serializer { get { return Serializer; } }
        private MetaType baseType;
        private bool frozen, isPrivateOnApi;
        /// <summary>
        /// Gets the base-type for this type
        /// </summary>
        public MetaType BaseType {
            get { return baseType; }
        }

        /// <summary>
        /// When used to compile a model, should public serialization/deserialzation methods
        /// be included for this type?
        /// </summary>
        public bool IncludeSerializerMethod
        {   // negated to minimize common-case / initializer
            get { return !isPrivateOnApi; }
            set
            {
                ThrowIfFrozen();
                isPrivateOnApi = !value;
            }
        }

        private BasicList subTypes;
        /// <summary>
        /// Adds a known sub-type to the inheritance model
        /// </summary>
        public MetaType AddSubType(int fieldNumber, Type derivedType)
        {
            if (!type.IsClass || type.IsSealed) {
                throw new InvalidOperationException("Sub-types can only be adedd to non-sealed classes");
            }
            MetaType derivedMeta = model[derivedType];
            SubType subType = new SubType(fieldNumber, derivedMeta);
            ThrowIfFrozen();

            derivedMeta.SetBaseType(this); // includes ThrowIfFrozen
            if (subTypes == null) subTypes = new BasicList();
            subTypes.Add(subType);
            return this;
        }

        private void SetBaseType(MetaType baseType)
        {
            if (baseType == null) throw new ArgumentNullException("baseType");
            if (this.baseType == baseType) return;
            if (this.baseType != null) throw new InvalidOperationException("A type can only participate in one inheritance hierarchy");

            MetaType type = baseType;
            while (type != null)
            {
                if (ReferenceEquals(type, this)) throw new InvalidOperationException("Cyclic inheritance is not allowed");
                type = type.baseType;
            }
            this.baseType = baseType;
        }

        private CallbackSet callbacks;
        /// <summary>
        /// Indicates whether the current type has defined callbacks 
        /// </summary>
        public bool HasCallbacks
        {
            get { return callbacks != null && callbacks.NonTrivial; }
        }

        /// <summary>
        /// Indicates whether the current type has defined subtypes
        /// </summary>
        public bool HasSubtypes
        {
            get { return subTypes != null && subTypes.Count != 0; }
        }
        
        /// <summary>
        /// Obtains the subtypes that are defined for the current type
        /// </summary>
        public SubType[] GetSubtypes()
        {
            if (!HasSubtypes) return null;
            SubType[] arr = new SubType[subTypes.Count];
            subTypes.CopyTo(arr, 0);
            return arr;
        }

        /// <summary>
        /// Returns the set of callbacks defined for this type
        /// </summary>
        public CallbackSet Callbacks
        {
            get
            {
                if (callbacks == null && !type.IsValueType) callbacks = new CallbackSet(this);
                return callbacks;
            }
        }

        /// <summary>
        /// Assigns the callbacks to use during serialiation/deserialization.
        /// </summary>
        /// <param name="beforeSerialize">The method (or null) called before serialization begins.</param>
        /// <param name="afterSerialize">The method (or null) called when serialization is complete.</param>
        /// <param name="beforeDeserialize">The method (or null) called before deserialization begins (or when a new instance is created during deserialization).</param>
        /// <param name="afterDeserialize">The method (or null) called when deserialization is complete.</param>
        /// <returns>The set of callbacks.</returns>
        public MetaType SetCallbacks(MethodInfo beforeSerialize, MethodInfo afterSerialize, MethodInfo beforeDeserialize, MethodInfo afterDeserialize)
        {
            if (type.IsValueType) throw new InvalidOperationException();
            CallbackSet callbacks = Callbacks;
            callbacks.BeforeSerialize = beforeSerialize;
            callbacks.AfterSerialize = afterSerialize;
            callbacks.BeforeDeserialize = beforeDeserialize;
            callbacks.AfterDeserialize = afterDeserialize;
            return this;
        }
        /// <summary>
        /// Assigns the callbacks to use during serialiation/deserialization.
        /// </summary>
        /// <param name="beforeSerialize">The name of the method (or null) called before serialization begins.</param>
        /// <param name="afterSerialize">The name of the method (or null) called when serialization is complete.</param>
        /// <param name="beforeDeserialize">The name of the method (or null) called before deserialization begins (or when a new instance is created during deserialization).</param>
        /// <param name="afterDeserialize">The name of the method (or null) called when deserialization is complete.</param>
        /// <returns>The set of callbacks.</returns>
        public MetaType SetCallbacks(string beforeSerialize, string afterSerialize, string beforeDeserialize, string afterDeserialize)
        {
            if (type.IsValueType) throw new InvalidOperationException();
            CallbackSet callbacks = Callbacks;
            callbacks.BeforeSerialize = ResolveCallback(beforeSerialize);
            callbacks.AfterSerialize = ResolveCallback(afterSerialize);
            callbacks.BeforeDeserialize = ResolveCallback(beforeDeserialize);
            callbacks.AfterDeserialize = ResolveCallback(afterDeserialize);
            return this;
        }
        private MethodInfo ResolveCallback(string name)
        {
            return string.IsNullOrEmpty(name) ? null : type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
        private readonly RuntimeTypeModel model;
        internal MetaType(RuntimeTypeModel model, Type type)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (type == null) throw new ArgumentNullException("type");
            if (type.IsPrimitive) throw new ArgumentException("Not valid for primitive types", "type");
            this.type = type;
            this.model = model;
        }
        /// <summary>
        /// Throws an exception if the type has been made immutable
        /// </summary>
        protected internal void ThrowIfFrozen()
        {
            if (frozen) throw new InvalidOperationException("The type cannot be changed once a serializer has been generated");
        }
        internal void Freeze() { frozen = true;}

        private readonly Type type;
        /// <summary>
        /// The runtime type that the meta-type represents
        /// </summary>
        public Type Type { get { return type; } }
        private IProtoTypeSerializer serializer;
        internal IProtoTypeSerializer Serializer {
            get {
                if (serializer == null)
                {
                    frozen = true;
                    serializer = BuildSerializer();
                }
                return serializer;
            }
        }

        private IProtoTypeSerializer BuildSerializer()
        {
            if (surrogate != null) return new SurrogateSerializer(type, model[surrogate].Serializer);

            fields.Trim();
            int fieldCount = fields.Count;
            int subTypeCount = subTypes == null ? 0 : subTypes.Count;
            int[] fieldNumbers = new int[fieldCount + subTypeCount];
            IProtoSerializer[] serializers = new IProtoSerializer[fieldCount + subTypeCount];
            int i = 0;
            if (subTypeCount != 0)
            {
                foreach (SubType subType in subTypes)
                {
                    fieldNumbers[i] = subType.FieldNumber;
                    serializers[i++] = subType.Serializer;
                }
            }
            if (fieldCount != 0)
            {
                foreach (ValueMember member in fields)
                {
                    fieldNumbers[i] = member.FieldNumber;
                    serializers[i++] = member.Serializer;
                }
            }

            BasicList baseCtorCallbacks = null;
            MetaType tmp = BaseType;
            while (tmp != null)
            {
                MethodInfo method = tmp.HasCallbacks ? tmp.Callbacks.BeforeDeserialize : null;
                if (method != null)
                {
                    if (baseCtorCallbacks == null) baseCtorCallbacks = new BasicList();
                    baseCtorCallbacks.Add(method);
                }
                tmp = tmp.BaseType;
            }
            MethodInfo[] arr = null;
            if (baseCtorCallbacks != null)
            {
                arr = new MethodInfo[baseCtorCallbacks.Count];
                baseCtorCallbacks.CopyTo(arr, 0);
                Array.Reverse(arr);
            }
            return new TypeSerializer(type, fieldNumbers, serializers, arr, baseType == null, useConstructor, callbacks);
        }

        [Flags]
        internal enum AttributeFamily
        {
            None = 0, ProtoBuf = 1, DataContractSerialier = 2, XmlSerializer = 4
        }
        internal void ApplyDefaultBehaviour()
        {
            if (model.FindWithoutAdd(type.BaseType) == null
                && GetContractFamily(type.BaseType, null) != MetaType.AttributeFamily.None)
            {
                model.FindOrAddAuto(type.BaseType, true, false, false);
            }

            object[] typeAttribs = type.GetCustomAttributes(true);
            AttributeFamily family = GetContractFamily(type, typeAttribs);
            bool isEnum = type.IsEnum;
            if(family ==  AttributeFamily.None && !isEnum) return; // and you'd like me to do what, exactly?
            BasicList partialIgnores = null, partialMembers = null;
            for (int i = 0; i < typeAttribs.Length; i++)
            {
                if (!isEnum && typeAttribs[i] is ProtoIncludeAttribute)
                {
                    ProtoIncludeAttribute pia = (ProtoIncludeAttribute)typeAttribs[i];
                    AddSubType(pia.Tag, pia.KnownType);
                }
                if(typeAttribs[i] is ProtoPartialIgnoreAttribute)
                {
                    if(partialIgnores == null) partialIgnores = new BasicList();
                    partialIgnores.Add(((ProtoPartialIgnoreAttribute)typeAttribs[i]).MemberName);
                }
                if (!isEnum && typeAttribs[i] is ProtoPartialMemberAttribute)
                {
                    if (partialMembers == null) partialMembers = new BasicList();
                    partialMembers.Add(typeAttribs[i]);
                }
            }
            MethodInfo[] callbacks = null;
            foreach (MemberInfo member in type.GetMembers(isEnum ? BindingFlags.Public | BindingFlags.Static
                : BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (member.DeclaringType != type) continue;
                if (member.IsDefined(typeof(ProtoIgnoreAttribute), true)) continue;
                if (partialIgnores != null && partialIgnores.Contains(member.Name)) continue;

                switch (member.MemberType)
                {
                    case MemberTypes.Property:
                    case MemberTypes.Field:
                        ValueMember vm = ApplyDefaultBehaviour(isEnum, family, member, partialMembers);
                        if (vm != null)
                        {
                            Add(vm);
                        }
                        break;
                    case MemberTypes.Method:
                        if (isEnum) continue;
                        MethodInfo method = (MethodInfo)member;
                        object[] memberAttribs = Attribute.GetCustomAttributes(method);
                        if (memberAttribs != null && memberAttribs.Length > 0)
                        {
                            CheckForCallback(method, memberAttribs, "ProtoBuf.ProtoBeforeSerializationAttribute", ref callbacks, 0);
                            CheckForCallback(method, memberAttribs, "ProtoBuf.ProtoAfterSerializationAttribute", ref callbacks, 1);
                            CheckForCallback(method, memberAttribs, "ProtoBuf.ProtoBeforeDeserializationAttribute", ref callbacks, 2);
                            CheckForCallback(method, memberAttribs, "ProtoBuf.ProtoAfterDeserializationAttribute", ref callbacks, 3);
                            CheckForCallback(method, memberAttribs, "System.Runtime.Serialization.OnSerializingAttribute", ref callbacks, 4);
                            CheckForCallback(method, memberAttribs, "System.Runtime.Serialization.OnSerializedAttribute", ref callbacks, 5);
                            CheckForCallback(method, memberAttribs, "System.Runtime.Serialization.OnDeserializingAttribute", ref callbacks, 6);
                            CheckForCallback(method, memberAttribs, "System.Runtime.Serialization.OnDeserializedAttribute", ref callbacks, 7);
                        }
                        break;
                }
            }
            if (callbacks != null)
            {
                SetCallbacks(callbacks[0] ?? callbacks[4], callbacks[1] ?? callbacks[5],
                    callbacks[2] ?? callbacks[6], callbacks[3] ?? callbacks[7]);
            }
        }

        internal static AttributeFamily GetContractFamily(Type type, object[] attributes)
        {
            AttributeFamily family = AttributeFamily.None;
            if (attributes == null) attributes = type.GetCustomAttributes(true);
            for (int i = 0; i < attributes.Length; i++)
            {
                switch (attributes[i].GetType().FullName)
                {
                    case "ProtoBuf.ProtoContractAttribute": family |= AttributeFamily.ProtoBuf; break;
                    case "System.Xml.Serialization.XmlTypeAttribute": family |= AttributeFamily.XmlSerializer; break;
                    case "System.Runtime.Serialization.DataContractAttribute": family |= AttributeFamily.DataContractSerialier; break;
                }
            }
            return family;
        }
        private static void CheckForCallback(MethodInfo method, object[] attributes, string callbackTypeName, ref MethodInfo[] callbacks, int index)
        {
            for(int i = 0 ; i < attributes.Length ; i++)
            {
                if(attributes[i].GetType().FullName == callbackTypeName)
                {
                    if (callbacks == null) { callbacks = new MethodInfo[8]; }
                    else if (callbacks[index] != null)
                    {
                        throw new InvalidOperationException("Duplicate " + callbackTypeName + " callbacks on " + method.ReflectedType.FullName);
                    }
                    callbacks[index] = method;
                }
            }
        }
        private static bool HasFamily(AttributeFamily value, AttributeFamily required)
        {
            return (value & required) == required;
        }
        private ValueMember ApplyDefaultBehaviour(bool isEnum, AttributeFamily family, MemberInfo member, BasicList partialMembers)
        {
            if (member == null || (family == AttributeFamily.None && !isEnum)) return null; // nix
            Type effectiveType;
            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    effectiveType = ((FieldInfo)member).FieldType; break;
                case MemberTypes.Property:
                    effectiveType = ((PropertyInfo)member).PropertyType; break;
                default:
                    return null; // nothing doing
            }

            int fieldNumber = 0;
            string name = null;
            bool isRequired = false;
            Type itemType = null;
            Type defaultType = null;
            ResolveListTypes(effectiveType, ref itemType, ref defaultType);
            object[] attribs = member.GetCustomAttributes(true);
            Attribute attrib;
            DataFormat dataFormat = DataFormat.Default;
            bool ignore = false;
            object defaultValue = null;
            // implicit zero default
            switch (Type.GetTypeCode(effectiveType))
            {
                case TypeCode.Boolean: defaultValue = false; break;
                case TypeCode.Decimal: defaultValue = (decimal)0; break;
                case TypeCode.Single: defaultValue = (float)0; break;
                case TypeCode.Double: defaultValue = (double)0; break;
                case TypeCode.Byte: defaultValue = (byte)0;  break;
                case TypeCode.Char: defaultValue = (char)0; break;
                case TypeCode.Int16: defaultValue = (short)0; break;
                case TypeCode.Int32: defaultValue = (int)0; break;
                case TypeCode.Int64: defaultValue = (long)0; break;
                case TypeCode.SByte: defaultValue = (sbyte)0; break;
                case TypeCode.UInt16: defaultValue = (ushort)0; break;
                case TypeCode.UInt32: defaultValue = (uint)0; break;
                case TypeCode.UInt64: defaultValue = (ulong)0; break;
                default:
                    if (effectiveType == typeof(TimeSpan)) defaultValue = TimeSpan.Zero;
                    break;
            }
            bool done = false;
            if (isEnum)
            {
                attrib = GetAttribute(attribs, "ProtoBuf.ProtoIgnoreAttribute");
                if (attrib != null)
                {
                    ignore = true;
                }
                else
                {
                    attrib = GetAttribute(attribs, "ProtoBuf.ProtoEnumAttribute");
                    fieldNumber = Convert.ToInt32(((FieldInfo)member).GetValue(null));
                    if (attrib != null)
                    {
                        GetFieldName(ref name, attrib, "Name");
                        if ((bool)attrib.GetType().GetMethod("HasValue").Invoke(attrib, null))
                        {
                            fieldNumber = (int) GetMemberValue(attrib, "Value");
                        }
                    }
                        
                }
                done = true;
            }
            if (!ignore && !done) // always consider ProtoMember 
            {
                attrib = GetAttribute(attribs, "ProtoBuf.ProtoMemberAttribute");
                GetIgnore(ref ignore, attrib, attribs, "ProtoBuf.ProtoIgnoreAttribute");
                if (!ignore)
                {
                    GetFieldNumber(ref fieldNumber, attrib, "Tag");
                    GetFieldName(ref name, attrib, "Name");
                    GetFieldRequired(ref isRequired, attrib, "IsRequired");
                    GetDataFormat(ref dataFormat, attrib, "DataFormat");
                    done = fieldNumber > 0;
                }

                if (!done && partialMembers != null)
                {
                    foreach (ProtoPartialMemberAttribute ppma in partialMembers)
                    {
                        if (ppma.MemberName == member.Name)
                        {
                            GetFieldNumber(ref fieldNumber, ppma, "Tag");
                            GetFieldName(ref name, ppma, "Name");
                            GetFieldRequired(ref isRequired, ppma, "IsRequired");
                            GetDataFormat(ref dataFormat, ppma, "DataFormat");
                            if (done = fieldNumber > 0) break;                            
                        }
                    }
                }
            }
            if (!ignore && !done && HasFamily(family, AttributeFamily.DataContractSerialier))
            {
                attrib = GetAttribute(attribs, "System.Runtime.Serialization.DataMemberAttribute");
                GetFieldNumber(ref fieldNumber, attrib, "Order");
                GetFieldName(ref name, attrib, "Name");
                GetFieldRequired(ref isRequired, attrib, "IsRequired");
                done = fieldNumber > 0;
            }
            if (!ignore && !done && HasFamily(family, AttributeFamily.XmlSerializer))
            {
                attrib = GetAttribute(attribs, "System.Xml.Serialization.XmlElementAttribute");
                GetIgnore(ref ignore, attrib, attribs, "System.Xml.Serialization.XmlIgnoreAttribute");
                if (!ignore)
                {
                    GetFieldNumber(ref fieldNumber, attrib, "Order");
                    GetFieldName(ref name, attrib, "ElementName");
                }
                attrib = GetAttribute(attribs, "System.Xml.Serialization.XmlArrayAttribute");
                GetIgnore(ref ignore, attrib, attribs, "ProtoBuf.XmlIgnoreAttribute");
                if (!ignore)
                {
                    GetFieldNumber(ref fieldNumber, attrib, "Order");
                    GetFieldName(ref name, attrib, "ElementName");
                }
                done = fieldNumber > 0;
            }
            if (!ignore && (attrib = GetAttribute(attribs, "System.ComponentModel.DefaultValueAttribute")) != null)
            {
                defaultValue = GetMemberValue(attrib, "Value");
            }
            ValueMember vm = ((isEnum || fieldNumber > 0) && !ignore)
                ? new ValueMember(model, type, fieldNumber, member, effectiveType, itemType, defaultType, dataFormat, defaultValue)
                    : null;
            if (vm != null)
            {
                PropertyInfo prop = type.GetProperty(member.Name + "Specified", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                    null, typeof(bool), Helpers.EmptyTypes, null);
                if (prop != null)
                {
                    vm.SetSpecified(prop.GetGetMethod(true), prop.GetSetMethod(true));
                }
                else
                {
                    MethodInfo method = type.GetMethod("ShouldSerialize" + member.Name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                        null, Helpers.EmptyTypes, null);
                    if (method != null && method.ReturnType == typeof(bool))
                    {
                        vm.SetSpecified(method, null);
                    }
                }
                if(!Helpers.IsNullOrEmpty(name)) vm.SetName(name);
            }
            return vm;
        }

        private static void GetDataFormat(ref DataFormat value, Attribute attrib, string memberName)
        {
            if ((attrib == null) || (value != DataFormat.Default)) return;
            object obj = GetMemberValue(attrib, memberName);
            if (obj != null) value = (DataFormat)obj;
        }

        private static void GetIgnore(ref bool ignore, Attribute attrib, object[] attribs, string fullName)
        {
            if (ignore || attrib == null) return;
            ignore = GetAttribute(attribs, fullName) != null;
            return;
        }

        private static void GetFieldRequired(ref bool value, Attribute attrib, string memberName)
        {
            if (attrib == null || value) return;
            object obj = GetMemberValue(attrib, memberName);
            if (obj != null) value = (bool)obj;
        }

        private static void GetFieldNumber(ref int value, Attribute attrib, string memberName)
        {
            if (attrib == null || value > 0) return;
            object obj = GetMemberValue(attrib, memberName);
            if (obj != null) value = (int)obj;
        }
        private static void GetFieldName(ref string name, Attribute attrib, string memberName)
        {
            if (attrib == null || !Helpers.IsNullOrEmpty(name)) return;
            object obj = GetMemberValue(attrib, memberName);
            if (obj != null) name = (string)obj;
        }

        private static object GetMemberValue(Attribute attrib, string memberName)
        {
            MemberInfo[] members = attrib.GetType().GetMember(memberName, BindingFlags.Public | BindingFlags.Instance);
            if (members.Length != 1) return null;
            switch (members[0].MemberType)
            {
                case MemberTypes.Property:
                    return ((PropertyInfo)members[0]).GetValue(attrib, null);
                case MemberTypes.Field:
                    return ((FieldInfo)members[0]).GetValue(attrib);
                case MemberTypes.Method:
                    return ((MethodInfo)members[0]).Invoke(attrib, null);
            }
            return null;
        }

        private static Attribute GetAttribute(object[] attribs, string fullName)
        {
            for (int i = 0; i < attribs.Length; i++)
            {
                Attribute attrib = attribs[i] as Attribute;
                if (attrib != null && attrib.GetType().FullName == fullName) return attrib;
            }
            return null;
        }
        /// <summary>
        /// Adds a member (by name) to the MetaType
        /// </summary>        
        public MetaType Add(int fieldNumber, string memberName)
        {
            return Add(fieldNumber, memberName, null, null, null);
        }
        private bool useConstructor = true;
        /// <summary>
        /// Gets or sets whether the type should use a parameterless constructor (the default),
        /// or whether the type should skip the constructor completely. This option is not supported
        /// on compact-framework.
        /// </summary>
        public bool UseConstructor
        {
            get { return useConstructor; }
            set
            {
                ThrowIfFrozen();
            	useConstructor = value;
            }
        }
        /// <summary>
        /// Adds a member (by name) to the MetaType
        /// </summary>     
        public MetaType Add(string memberName)
        {
            Add(GetNextFieldNumber(), memberName);
            return this;
        }
        Type surrogate;
        /// <summary>
        /// Performs serialization of this type via a surrogate; all
        /// other serialization options are ignored and handled
        /// by the surrogate's configuration.
        /// </summary>
        public void SetSurrogate(Type type)
        {
            ThrowIfFrozen();
            this.surrogate = type;
            // no point in offering chaining; no options are respected
        }

        private int GetNextFieldNumber()
        {
            int maxField = 0;
            foreach (ValueMember member in fields)
            {
                if (member.FieldNumber > maxField) maxField = member.FieldNumber;
            }
            return maxField + 1;
        }
        /// <summary>
        /// Adds a set of members (by name) to the MetaType
        /// </summary>     
        public MetaType Add(params string[] memberNames)
        {
            int next = GetNextFieldNumber();
            for (int i = 0; i < memberNames.Length; i++)
            {
                Add(next++, memberNames[i]);
            }
            return this;
        }


        /// <summary>
        /// Adds a member (by name) to the MetaType
        /// </summary>        
        public MetaType Add(int fieldNumber, string memberName, object defaultValue)
        {
            return Add(fieldNumber, memberName, null, null, defaultValue);
        }

        private static void ResolveListTypes(Type type, ref Type itemType, ref Type defaultType) {
            if (type == null) return;
            // handle arrays
            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                {
                    throw new NotSupportedException("Multi-dimension arrays are supported");
                }
                itemType = type.GetElementType();
                if (itemType == typeof(byte)) {
                    defaultType = itemType = null;
                } else {
                    defaultType = Helpers.MakeArrayType(type);
                }
            }
            // handle lists
            if (itemType == null) { itemType = TypeModel.GetListItemType(type); }

            // check for nested data (not allowed)
            if (itemType != null)
            {
                Type nestedItemType = null, nestedDefaultType = null;
                ResolveListTypes(itemType, ref nestedItemType, ref nestedDefaultType);
                if (nestedItemType != null)
                {
                    throw new NotSupportedException("Nested or jagged lists and arrays are not supported");
                }
            }

            if (itemType != null && defaultType == null)
            {
                if (type.IsClass && !type.IsAbstract && type.GetConstructor(Helpers.EmptyTypes) != null)
                {
                    defaultType = type;
                }
                if (defaultType == null)
                {
                    if (type.IsInterface)
                    {
                        Type[] genArgs;
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IDictionary<,>)
                            && itemType == typeof(System.Collections.Generic.KeyValuePair<,>).MakeGenericType(genArgs = type.GetGenericArguments()))
                        {
                            defaultType = typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(genArgs);
                        }
                        else
                        {
                            defaultType = typeof(System.Collections.Generic.List<>).MakeGenericType(itemType);
                        }
                    }
                }
                // verify that the default type is appropriate
                if (defaultType != null && !type.IsAssignableFrom(defaultType)) { defaultType = null; }
            }
        }
        /// <summary>
        /// Adds a member (by name) to the MetaType, including an itemType and defaultType for representing lists
        /// </summary>
        public MetaType Add(int fieldNumber, string memberName, Type itemType, Type defaultType)
        {
            return Add(fieldNumber, memberName, itemType, defaultType, null);
        }
        
        private MetaType Add(int fieldNumber, string memberName, Type itemType, Type defaultType, object defaultValue)
        {
            MemberInfo[] members = type.GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if(members == null || members.Length != 1) throw new ArgumentException("Unable to determine member: " + memberName, "memberName");
            MemberInfo mi = members[0];
            Type miType;
            switch (mi.MemberType)
            {
                case MemberTypes.Field:
                    miType = ((FieldInfo)mi).FieldType; break;
                case MemberTypes.Property:
                    miType = ((PropertyInfo)mi).PropertyType; break;
                default:
                    throw new NotSupportedException(mi.MemberType.ToString());
            }

            ResolveListTypes(miType, ref itemType, ref defaultType);
            Add(new ValueMember(model, type, fieldNumber, mi, miType, itemType, defaultType, DataFormat.Default, defaultValue));
            return this;
        } 
        private void Add(ValueMember member) {
            lock (fields)
            {
                ThrowIfFrozen();
                fields.Add(member);
            }
        }
        /// <summary>
        /// Returns the ValueMember that matchs a given field number, or null if not found
        /// </summary>
        public ValueMember this[int fieldNumber]
        {
            get
            {
                foreach (ValueMember member in fields)
                {
                    if (member.FieldNumber == fieldNumber) return member;
                }
                return null;
            }
        }
        private readonly BasicList fields = new BasicList();

        //IEnumerable GetFields() { return fields; }

#if FEAT_COMPILER && !FX11
        internal void CompileInPlace()
        {
            serializer = CompiledSerializer.Wrap(Serializer);
        }
#endif

        internal bool IsDefined(int fieldNumber)
        {
            foreach (ValueMember field in fields)
            {
                if (field.FieldNumber == fieldNumber) return true;
            }
            return false;
        }

        internal int GetKey(bool demand, bool getBaseKey)
        {
            return model.GetKey(type, demand, getBaseKey);
        }



        internal EnumSerializer.EnumPair[] GetEnumMap()
        {
            if (enumPassthru) return null;
            EnumSerializer.EnumPair[] result = new EnumSerializer.EnumPair[fields.Count];
            for (int i = 0; i < result.Length; i++)
            {
                ValueMember member = (ValueMember) fields[i];
                int wireValue = member.FieldNumber;
                Enum value = member.GetEnumValue();
                result[i] = new EnumSerializer.EnumPair(wireValue, value);
            }
            return result;
        }

        private bool enumPassthru;

        public bool EnumPassthru
        {
            get { return enumPassthru; }
            set { enumPassthru = value; }
        }
    }
}
#endif