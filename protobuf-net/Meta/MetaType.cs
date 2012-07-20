#if !NO_RUNTIME
using System;
using System.Collections;
using ProtoBuf.Serializers;


#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#if FEAT_COMPILER
using IKVM.Reflection.Emit;
#endif
#else
using System.Reflection;
#if FEAT_COMPILER
using System.Reflection.Emit;
#endif
#endif


namespace ProtoBuf.Meta
{
    /// <summary>
    /// Represents a type at runtime for use with protobuf, allowing the field mappings (etc) to be defined
    /// </summary>
    public class MetaType : ISerializerProxy
    {
        internal class Comparer : IComparer
#if !NO_GENERICS
             , System.Collections.Generic.IComparer<MetaType>
#endif
        {
            public static readonly Comparer Default = new Comparer();
            public int Compare(object x, object y)
            {
                return Compare(x as MetaType, y as MetaType);
            }
            public int Compare(MetaType x, MetaType y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            }
        }
        /// <summary>
        /// Get the name of the type being represented
        /// </summary>
        public override string ToString()
        {
            return type.ToString();
        }
        IProtoSerializer ISerializerProxy.Serializer { get { return Serializer; } }
        private MetaType baseType;
        /// <summary>
        /// Gets the base-type for this type
        /// </summary>
        public MetaType BaseType {
            get { return baseType; }
        }
        internal TypeModel Model { get { return model; } }

        /// <summary>
        /// When used to compile a model, should public serialization/deserialzation methods
        /// be included for this type?
        /// </summary>
        public bool IncludeSerializerMethod
        {   // negated to minimize common-case / initializer
            get { return !HasFlag(OPTIONS_PrivateOnApi); }
            set { SetFlag(OPTIONS_PrivateOnApi, !value, true); }
        }

        /// <summary>
        /// Should this type be treated as a reference by default?
        /// </summary>
        public bool AsReferenceDefault
        { 
            get { return HasFlag(OPTIONS_AsReferenceDefault); }
            set { SetFlag(OPTIONS_AsReferenceDefault, value, true); }
        }

        private BasicList subTypes;
        private bool IsValidSubType(Type subType)
        {
#if WINRT
            return typeInfo.IsAssignableFrom(subType.GetTypeInfo());
#else
            return type.IsAssignableFrom(subType);
#endif
        }
        /// <summary>
        /// Adds a known sub-type to the inheritance model
        /// </summary>
        public MetaType AddSubType(int fieldNumber, Type derivedType)
        {
            return AddSubType(fieldNumber, derivedType, DataFormat.Default);
        }
        /// <summary>
        /// Adds a known sub-type to the inheritance model
        /// </summary>
        public MetaType AddSubType(int fieldNumber, Type derivedType, DataFormat dataFormat)
        {
            if (derivedType == null) throw new ArgumentNullException("derivedType");
            if (fieldNumber < 1) throw new ArgumentOutOfRangeException("fieldNumber");
#if WINRT
            if (!(typeInfo.IsClass || typeInfo.IsInterface) || typeInfo.IsSealed) {
#else
            if (!(type.IsClass || type.IsInterface) || type.IsSealed) {
#endif
                throw new InvalidOperationException("Sub-types can only be added to non-sealed classes");
            }
            if (!IsValidSubType(derivedType))
            {
                throw new ArgumentException(derivedType.Name + " is not a valid sub-type of " + type.Name, "derivedType");
            }
            MetaType derivedMeta = model[derivedType];
            ThrowIfFrozen();
            derivedMeta.ThrowIfFrozen();
            SubType subType = new SubType(fieldNumber, derivedMeta, dataFormat);
            ThrowIfFrozen();

            derivedMeta.SetBaseType(this); // includes ThrowIfFrozen
            if (subTypes == null) subTypes = new BasicList();
            subTypes.Add(subType);
            return this;
        }
#if WINRT
        internal static readonly TypeInfo ienumerable = typeof(IEnumerable).GetTypeInfo();
#else
        internal static readonly System.Type ienumerable = typeof(IEnumerable);
#endif
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
                if (callbacks == null) callbacks = new CallbackSet(this);
                return callbacks;
            }
        }

        private bool IsValueType
        {
            get
            {
#if WINRT
                return typeInfo.IsValueType;
#else
                return type.IsValueType;
#endif
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
            if (IsValueType) throw new InvalidOperationException();
            CallbackSet callbacks = Callbacks;
            callbacks.BeforeSerialize = ResolveMethod(beforeSerialize, true);
            callbacks.AfterSerialize = ResolveMethod(afterSerialize, true);
            callbacks.BeforeDeserialize = ResolveMethod(beforeDeserialize, true);
            callbacks.AfterDeserialize = ResolveMethod(afterDeserialize, true);
            return this;
        }

        private string name;
        /// <summary>
        /// Gets or sets the name of this contract.
        /// </summary>
        public string Name
        {
            get
            {
                return Helpers.IsNullOrEmpty(name) ? type.Name : name;
            }
            set
            {
                ThrowIfFrozen();
                name = value;
            }
        }

        private MethodInfo factory;
        /// <summary>
        /// Designate a factory-method to use to create instances of this type
        /// </summary>
        public MetaType SetFactory(MethodInfo factory)
        {
            if(factory != null)
            {
                if(IsValueType) throw new InvalidOperationException();
                if(!factory.IsStatic) throw new ArgumentException("A factory-method must be static", "factory");
                if (factory.ReturnType != type) throw new ArgumentException("The factory-method must return " + type.FullName, "factory");

                if (!CallbackSet.CheckCallbackParameters(model, factory)) throw new ArgumentException("Invalid factory signature in " + factory.DeclaringType.FullName + "." + factory.Name, "factory");
            }
            ThrowIfFrozen();
            this.factory = factory;
            return this;
        }
        /// <summary>
        /// Designate a factory-method to use to create instances of this type
        /// </summary>
        public MetaType SetFactory(string factory)
        {
            return SetFactory(ResolveMethod(factory, false));
        }

        private MethodInfo ResolveMethod(string name, bool instance)
        {
            if (string.IsNullOrEmpty(name)) return null;
#if WINRT
            return instance ? Helpers.GetInstanceMethod(typeInfo, name) : Helpers.GetStaticMethod(typeInfo, name);
#else
            return instance ? Helpers.GetInstanceMethod(type, name) : Helpers.GetStaticMethod(type, name);
#endif
        }
        private readonly RuntimeTypeModel model;
        internal MetaType(RuntimeTypeModel model, Type type)
        {
            if (model == null) throw new ArgumentNullException("model");
            if (type == null) throw new ArgumentNullException("type");
            WireType defaultWireType;
            IProtoSerializer coreSerializer = ValueMember.TryGetCoreSerializer(model, DataFormat.Default, type, out defaultWireType, false, false, false, false);
            if (coreSerializer != null)
            {
                throw new ArgumentException("Data of this type has inbuilt behaviour, and cannot be added to a model in this way: " + type.FullName);
            }
            
            this.type = type;
#if WINRT
            this.typeInfo = type.GetTypeInfo();
#endif
            this.model = model;
            
            if (Helpers.IsEnum(type))
            {
#if WINRT
                EnumPassthru = typeInfo.IsDefined(typeof(FlagsAttribute), false);
#else
                EnumPassthru = type.IsDefined(model.MapType(typeof(FlagsAttribute)), false);
#endif
            }
        }
#if WINRT
        private readonly TypeInfo typeInfo;
#endif
        /// <summary>
        /// Throws an exception if the type has been made immutable
        /// </summary>
        protected internal void ThrowIfFrozen()
        {
            if ((flags & OPTIONS_Frozen)!=0) throw new InvalidOperationException("The type cannot be changed once a serializer has been generated for " + type.FullName);
        }
        internal void Freeze() { flags |= OPTIONS_Frozen; }

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
                    int opaqueToken = 0;
                    try
                    {
                        model.TakeLock(ref opaqueToken);
                        if (serializer == null)
                        { // double-check, but our main purpse with this lock is to ensure thread-safety with
                            // serializers needing to wait until another thread has finished adding the properties
                            SetFlag(OPTIONS_Frozen, true, false);
                            serializer = BuildSerializer();
#if FEAT_COMPILER && !FX11
                            if (model.AutoCompile) CompileInPlace();
#endif
                        }
                    }
                    finally
                    {
                        model.ReleaseLock(opaqueToken);
                    }
                }
                return serializer;
            }
        }

        private IProtoTypeSerializer BuildSerializer()
        {
            if (Helpers.IsEnum(type))
            {
                return new TagDecorator(ProtoBuf.Serializer.ListItemTag, WireType.Variant, false, new EnumSerializer(type, GetEnumMap()));
            }
            Type itemType = IgnoreListHandling ? null : TypeModel.GetListItemType(model, type);
            if (itemType != null)
            {
                if(surrogate != null)
                {
                    throw new ArgumentException("Repeated data (a list, collection, etc) has inbuilt behaviour and cannot use a surrogate");
                }
                if(subTypes != null && subTypes.Count != 0)
                {
                    throw new ArgumentException("Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be subclassed");
                }
                ValueMember fakeMember = new ValueMember(model, ProtoBuf.Serializer.ListItemTag, type, itemType, type, DataFormat.Default);
                return new TypeSerializer(model, type, new int[] { ProtoBuf.Serializer.ListItemTag }, new IProtoSerializer[] { fakeMember.Serializer }, null, true, true, null, constructType, factory);
            }
            if (surrogate != null)
            {
                MetaType mt = model[surrogate], mtBase;
                while ((mtBase = mt.baseType) != null) { mt = mtBase; }
                return new SurrogateSerializer(type, surrogate, mt.Serializer);
            }
            if (HasFlag(OPTIONS_AutoTuple))
            {
                MemberInfo[] mapping;
                ConstructorInfo ctor = ResolveTupleConstructor(type, out mapping);
                if(ctor == null) throw new InvalidOperationException();
                return new TupleSerializer(model, ctor, mapping);
            }
            

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
#if WINRT
                    if (!subType.DerivedType.IgnoreListHandling && ienumerable.IsAssignableFrom(subType.DerivedType.Type.GetTypeInfo()))
#else
                    if (!subType.DerivedType.IgnoreListHandling && model.MapType(ienumerable).IsAssignableFrom(subType.DerivedType.Type))
#endif
                    {
                        throw new ArgumentException("Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be used as a subclass");
                    }
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
            return new TypeSerializer(model, type, fieldNumbers, serializers, arr, baseType == null, UseConstructor, callbacks, constructType, factory);
        }

        [Flags]
        internal enum AttributeFamily
        {
            None = 0, ProtoBuf = 1, DataContractSerialier = 2, XmlSerializer = 4, AutoTuple = 8
        }
        static Type GetBaseType(MetaType type)
        {
#if WINRT
            return type.typeInfo.BaseType;
#else
            return type.type.BaseType;
#endif
        }
        internal void ApplyDefaultBehaviour()
        {
            Type baseType = GetBaseType(this);
            if (baseType != null && model.FindWithoutAdd(baseType) == null
                && GetContractFamily(model, baseType, null) != MetaType.AttributeFamily.None)
            {
                model.FindOrAddAuto(baseType, true, false, false);
            }

            AttributeMap[] typeAttribs = AttributeMap.Create(model, type, true);
            AttributeFamily family = GetContractFamily(model, type, typeAttribs);
            if(family == AttributeFamily.AutoTuple)
            {
                SetFlag(OPTIONS_AutoTuple, true, true);
            }
            bool isEnum = !EnumPassthru && Helpers.IsEnum(type);
            if(family ==  AttributeFamily.None && !isEnum) return; // and you'd like me to do what, exactly?
            BasicList partialIgnores = null, partialMembers = null;
            int dataMemberOffset = 0, implicitFirstTag = 1;
            bool inferTagByName = model.InferTagFromNameDefault;
            ImplicitFields implicitMode = ImplicitFields.None;
            string name = null;
            for (int i = 0; i < typeAttribs.Length; i++)
            {
                AttributeMap item = (AttributeMap)typeAttribs[i];
                object tmp;
                if (!isEnum && item.AttributeType.FullName == "ProtoBuf.ProtoIncludeAttribute")
                {
                    int tag = 0;
                    if (item.TryGet("tag", out tmp)) tag = (int)tmp;
                    DataFormat dataFormat = DataFormat.Default;
                    if(item.TryGet("DataFormat", out tmp))
                    {
                        dataFormat = (DataFormat)(int) tmp;
                    }
                    Type knownType = null;
                    try
                    {
                        if (item.TryGet("knownTypeName", out tmp)) knownType = model.GetType((string)tmp, type
#if WINRT
                            .GetTypeInfo()
#endif       
                            .Assembly);
                        else if (item.TryGet("knownType", out tmp)) knownType = (Type)tmp;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Unable to resolve sub-type of: " + type.FullName, ex);
                    }
                    if (knownType == null)
                    {
                        throw new InvalidOperationException("Unable to resolve sub-type of: " + type.FullName);
                    }
                    if(IsValidSubType(knownType)) AddSubType(tag, knownType, dataFormat);
                }
                
                if(item.AttributeType.FullName == "ProtoBuf.ProtoPartialIgnoreAttribute")
                {
                    if (item.TryGet("MemberName", out tmp) && tmp != null)
                    {
                        if (partialIgnores == null) partialIgnores = new BasicList();
                        partialIgnores.Add((string)tmp);
                    }
                }
                if (!isEnum && item.AttributeType.FullName == "ProtoBuf.ProtoPartialMemberAttribute")
                {
                    if (partialMembers == null) partialMembers = new BasicList();
                    partialMembers.Add(item);
                }
                
                if (item.AttributeType.FullName == "ProtoBuf.ProtoContractAttribute")
                {
                    if (item.TryGet("Name", out tmp)) name = (string) tmp;
                    if (!isEnum)
                    {
                        if (item.TryGet("DataMemberOffset", out tmp)) dataMemberOffset = (int) tmp;

#if !FEAT_IKVM
                        // IKVM can't access InferTagFromNameHasValue, but conveniently, InferTagFromName will only be returned if set via ctor or property
                        if (item.TryGet("InferTagFromNameHasValue", false, out tmp) && (bool) tmp)
#endif
                        {
                            if (item.TryGet("InferTagFromName", out tmp)) inferTagByName = (bool) tmp;
                        }

                        if (item.TryGet("ImplicitFields", out tmp))
                        {
                            if (tmp is ImplicitFields) implicitMode = (ImplicitFields) tmp;
                            else if (tmp is int) implicitMode = (ImplicitFields) (int) tmp;
                            else
                            {
                                throw new NotSupportedException(tmp.GetType().FullName);
                            }
                        }

                        if (item.TryGet("SkipConstructor", out tmp)) UseConstructor = !(bool) tmp;
                        if (item.TryGet("IgnoreListHandling", out tmp)) IgnoreListHandling = (bool) tmp;

                        if (item.TryGet("ImplicitFirstTag", out tmp) && (int) tmp > 0) implicitFirstTag = (int) tmp;
                    }
                }

                if (item.AttributeType.FullName == "System.Runtime.Serialization.DataContractAttribute")
                {
                    if (name == null && item.TryGet("Name", out tmp)) name = (string)tmp;
                }
                if (item.AttributeType.FullName == "System.Xml.Serialization.XmlTypeAttribute")
                {
                    if (name == null && item.TryGet("TypeName", out tmp)) name = (string)tmp;
                }
            }
            if (!Helpers.IsNullOrEmpty(name)) Name = name;
            if (implicitMode != ImplicitFields.None)
            {
                family &= AttributeFamily.ProtoBuf; // with implicit fields, **only** proto attributes are important
            }
            MethodInfo[] callbacks = null;

            BasicList members = new BasicList();

#if WINRT
            System.Collections.Generic.IEnumerable<MemberInfo> foundList;
            if(isEnum) {
                foundList = type.GetRuntimeFields();
            }
            else
            {
                System.Collections.Generic.List<MemberInfo> list = new System.Collections.Generic.List<MemberInfo>();
                foreach(PropertyInfo prop in type.GetRuntimeProperties()) {
                    MethodInfo getter = Helpers.GetGetMethod(prop, false);
                    if(getter != null && !getter.IsStatic) list.Add(prop);
                }
                foreach(FieldInfo fld in type.GetRuntimeFields()) if(fld.IsPublic && !fld.IsStatic) list.Add(fld);
                foreach(MethodInfo mthd in type.GetRuntimeMethods()) if(mthd.IsPublic && !mthd.IsStatic) list.Add(mthd);
                foundList = list;
            }
#else
            MemberInfo[] foundList = type.GetMembers(isEnum ? BindingFlags.Public | BindingFlags.Static
                : BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
#endif
                foreach (MemberInfo member in foundList)
            {
                if (member.DeclaringType != type) continue;
                if (member.IsDefined(model.MapType(typeof(ProtoIgnoreAttribute)), true)) continue;
                if (partialIgnores != null && partialIgnores.Contains(member.Name)) continue;

                bool forced = false, isPublic, isField;
                Type effectiveType;

                
                PropertyInfo property;
                FieldInfo field;
                MethodInfo method;
                if((property = member as PropertyInfo) != null)
                {
                    if (isEnum) continue; // wasn't expecting any props!

                    effectiveType = property.PropertyType;
                    isPublic = Helpers.GetGetMethod(property, false) != null;
                    isField = false;
                    ApplyDefaultBehaviour_AddMembers(model, family, isEnum, partialMembers, dataMemberOffset, inferTagByName, implicitMode, members, member, ref forced, isPublic, isField, ref effectiveType);
                } else if ((field = member as FieldInfo) != null)
                {
                    effectiveType = field.FieldType;
                    isPublic = field.IsPublic;
                    isField = true;
                    if (isEnum && !field.IsStatic)
                    { // only care about static things on enums; WinRT has a __value instance field!
                        continue;
                    }
                    ApplyDefaultBehaviour_AddMembers(model, family, isEnum, partialMembers, dataMemberOffset, inferTagByName, implicitMode, members, member, ref forced, isPublic, isField, ref effectiveType);
                } else if ((method = member as MethodInfo) != null)
                {
                    if (isEnum) continue;
                    AttributeMap[] memberAttribs = AttributeMap.Create(model, method, false);
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
                }
            }
            ProtoMemberAttribute[] arr = new ProtoMemberAttribute[members.Count];
            members.CopyTo(arr, 0);
            
            if (inferTagByName || implicitMode != ImplicitFields.None)
            {
                Array.Sort(arr);
                int nextTag = implicitFirstTag;
                foreach (ProtoMemberAttribute normalizedAttribute in arr)
                {
                    if (!normalizedAttribute.TagIsPinned) // if ProtoMember etc sets a tag, we'll trust it
                    {
                        normalizedAttribute.Rebase(nextTag++);
                    }
                }
            }

            foreach (ProtoMemberAttribute normalizedAttribute in arr)
            {
                ValueMember vm = ApplyDefaultBehaviour(isEnum, normalizedAttribute);
                if (vm != null)
                {
                    Add(vm);
                }
            }

            if (callbacks != null)
            {
                SetCallbacks(Coalesce(callbacks, 0, 4), Coalesce(callbacks, 1, 5),
                    Coalesce(callbacks, 2, 6), Coalesce(callbacks, 3, 7));
            }
        }

        private static void ApplyDefaultBehaviour_AddMembers(TypeModel model, AttributeFamily family, bool isEnum, BasicList partialMembers, int dataMemberOffset, bool inferTagByName, ImplicitFields implicitMode, BasicList members, MemberInfo member, ref bool forced, bool isPublic, bool isField, ref Type effectiveType)
        {
            switch (implicitMode)
            {
                case ImplicitFields.AllFields:
                    if (isField) forced = true;
                    break;
                case ImplicitFields.AllPublic:
                    if (isPublic) forced = true;
                    break;
            }

            // we just don't like delegate types ;p
#if WINRT
            if (effectiveType.GetTypeInfo().IsSubclassOf(typeof(Delegate))) effectiveType = null;
#else
            if (effectiveType.IsSubclassOf(model.MapType(typeof(Delegate)))) effectiveType = null;
#endif
            if (effectiveType != null)
            {
                ProtoMemberAttribute normalizedAttribute = NormalizeProtoMember(model, member, family, forced, isEnum, partialMembers, dataMemberOffset, inferTagByName);
                if (normalizedAttribute != null) members.Add(normalizedAttribute);
            }
        }


        static MethodInfo Coalesce(MethodInfo[] arr, int x, int y)
        {
            MethodInfo mi = arr[x];
            if (mi == null) mi = arr[y];
            return mi;
        }

        internal static AttributeFamily GetContractFamily(TypeModel model, Type type, AttributeMap[] attributes)
        {
            AttributeFamily family = AttributeFamily.None;

            if (attributes == null) attributes = AttributeMap.Create(model, type, false);

            for (int i = 0; i < attributes.Length; i++)
            {
                switch (attributes[i].AttributeType.FullName)
                {
                    case "ProtoBuf.ProtoContractAttribute":
                        bool tmp = false;
                        GetFieldBoolean(ref tmp, attributes[i], "UseProtoMembersOnly");
                        if (tmp) return AttributeFamily.ProtoBuf;
                        family |= AttributeFamily.ProtoBuf;
                        break;
                    case "System.Xml.Serialization.XmlTypeAttribute": family |= AttributeFamily.XmlSerializer; break;
                    case "System.Runtime.Serialization.DataContractAttribute": family |= AttributeFamily.DataContractSerialier; break;
                }
            }
            if(family == AttributeFamily.None)
            { // check for obvious tuples
                MemberInfo[] mapping;
                if(ResolveTupleConstructor(type, out mapping) != null)
                {
                    family |= AttributeFamily.AutoTuple;
                }
            }
            return family;
        }
        private static ConstructorInfo ResolveTupleConstructor(Type type, out MemberInfo[] mappedMembers)
        {
            mappedMembers = null;
            if(type == null) throw new ArgumentNullException("type");
#if WINRT
            TypeInfo typeInfo = type.GetTypeInfo();
            if (typeInfo.IsAbstract) return null; // as if!
            ConstructorInfo[] ctors = Helpers.GetConstructors(typeInfo, false);
#else
            if(type.IsAbstract) return null; // as if!
            ConstructorInfo[] ctors = Helpers.GetConstructors(type, false);
#endif
            // need to have an interesting constructor to bother even checking this stuff
            if(ctors.Length == 0 || (ctors.Length == 1 && ctors[0].GetParameters().Length == 0)) return null;

            MemberInfo[] fieldsPropsUnfiltered = Helpers.GetInstanceFieldsAndProperties(type, true);
            BasicList memberList = new BasicList();
            for (int i = 0; i < fieldsPropsUnfiltered.Length; i++)
            {
                PropertyInfo prop = fieldsPropsUnfiltered[i] as PropertyInfo;
                if (prop != null)
                {
                    if (!prop.CanRead) return null; // no use if can't read
                    if (prop.CanWrite && Helpers.GetSetMethod(prop, false) != null) return null; // don't allow a public set (need to allow non-public to handle Mono's KeyValuePair<,>)
                    memberList.Add(prop);
                }
                else
                {
                    FieldInfo field = fieldsPropsUnfiltered[i] as FieldInfo;
                    if (field != null)
                    {
                        if (!field.IsInitOnly) return null; // all public fields must be readonly to be counted a tuple
                        memberList.Add(field);
                    }
                }
            }
            if (memberList.Count == 0)
            {
                return null;
            }

            MemberInfo[] members = new MemberInfo[memberList.Count];
            memberList.CopyTo(members, 0);

            int[] mapping = new int[members.Length];
            int found = 0;
            ConstructorInfo result = null;
            mappedMembers = new MemberInfo[mapping.Length];
            for(int i = 0 ; i < ctors.Length ; i++)
            {
                ParameterInfo[] parameters = ctors[i].GetParameters();

                if (parameters.Length != members.Length) continue;

                // reset the mappings to test
                for (int j = 0; j < mapping.Length; j++) mapping[j] = -1;

                for(int j = 0 ; j < parameters.Length ; j++)
                {
                    string lower = parameters[j].Name.ToLower();
                    for(int k = 0 ; k < members.Length ; k++)
                    {
                        if (members[k].Name.ToLower() != lower) continue;
                        Type memberType = Helpers.GetMemberType(members[k]);
                        if (memberType != parameters[j].ParameterType) continue;

                        mapping[j] = k;
                    }
                }
                // did we map all?
                bool notMapped = false;
                for (int j = 0; j < mapping.Length; j++)
                {
                    if (mapping[j] < 0)
                    {
                        notMapped = true;
                        break;
                    }
                    mappedMembers[j] = members[mapping[j]];
                }

                if (notMapped) continue;
                found++;
                result = ctors[i];

            }
            return found == 1 ? result : null;
        }
        private static void CheckForCallback(MethodInfo method, AttributeMap[] attributes, string callbackTypeName, ref MethodInfo[] callbacks, int index)
        {
            for(int i = 0 ; i < attributes.Length ; i++)
            {
                if(attributes[i].AttributeType.FullName == callbackTypeName)
                {
                    if (callbacks == null) { callbacks = new MethodInfo[8]; }
                    else if (callbacks[index] != null)
                    {
#if WINRT || FEAT_IKVM
                        Type reflected = method.DeclaringType;
#else
                        Type reflected = method.ReflectedType;
#endif
                        throw new ProtoException("Duplicate " + callbackTypeName + " callbacks on " + reflected.FullName);
                    }
                    callbacks[index] = method;
                }
            }
        }
        private static bool HasFamily(AttributeFamily value, AttributeFamily required)
        {
            return (value & required) == required;
        }
        
        private static ProtoMemberAttribute NormalizeProtoMember(TypeModel model, MemberInfo member, AttributeFamily family, bool forced, bool isEnum, BasicList partialMembers, int dataMemberOffset, bool inferByTagName)
        {
            if (member == null || (family == AttributeFamily.None && !isEnum)) return null; // nix
            int fieldNumber = int.MinValue, minAcceptFieldNumber = inferByTagName ? -1 : 1;
            string name = null;
            bool isPacked = false, ignore = false, done = false, isRequired = false, asReference = false, dynamicType = false, tagIsPinned = false, overwriteList = false;
            DataFormat dataFormat = DataFormat.Default;
            if (isEnum) forced = true;
            AttributeMap[] attribs = AttributeMap.Create(model, member, true);
            AttributeMap attrib;

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
#if WINRT || PORTABLE
                    fieldNumber = Convert.ToInt32(((FieldInfo)member).GetValue(null));
#else
                    fieldNumber = Convert.ToInt32(((FieldInfo)member).GetRawConstantValue());
#endif
                    if (attrib != null)
                    {
                        GetFieldName(ref name, attrib, "Name");
#if !FEAT_IKVM // IKVM can't access HasValue, but conveniently, Value will only be returned if set via ctor or property
                        if ((bool)Helpers.GetInstanceMethod(attrib.AttributeType
#if WINRT
                             .GetTypeInfo()
#endif
                            ,"HasValue").Invoke(attrib.Target, null))
#endif
                        {
                            object tmp;
                            if(attrib.TryGet("Value", out tmp)) fieldNumber = (int)tmp;
                        }
                    }

                }
                done = true;
            }

            if (!ignore && !done) // always consider ProtoMember 
            {
                attrib = GetAttribute(attribs, "ProtoBuf.ProtoMemberAttribute");
                GetIgnore(ref ignore, attrib, attribs, "ProtoBuf.ProtoIgnoreAttribute");
                if (!ignore && attrib != null)
                {
                    GetFieldNumber(ref fieldNumber, attrib, "Tag");
                    GetFieldName(ref name, attrib, "Name");
                    GetFieldBoolean(ref isRequired, attrib, "IsRequired");
                    GetFieldBoolean(ref isPacked, attrib, "IsPacked");
                    GetFieldBoolean(ref overwriteList, attrib, "OverwriteList");
                    GetDataFormat(ref dataFormat, attrib, "DataFormat");
                    GetFieldBoolean(ref asReference, attrib, "AsReference");
                    GetFieldBoolean(ref dynamicType, attrib, "DynamicType");
                    done = tagIsPinned = fieldNumber > 0; // note minAcceptFieldNumber only applies to non-proto
                }

                if (!done && partialMembers != null)
                {
                    foreach (AttributeMap ppma in partialMembers)
                    {
                        object tmp;
                        if(ppma.TryGet("MemberName", out tmp) && (string)tmp == member.Name)
                        {
                            GetFieldNumber(ref fieldNumber, ppma, "Tag");
                            GetFieldName(ref name, ppma, "Name");
                            GetFieldBoolean(ref isRequired, ppma, "IsRequired");
                            GetFieldBoolean(ref isPacked, ppma, "IsPacked");
                            GetFieldBoolean(ref overwriteList, attrib, "OverwriteList");
                            GetDataFormat(ref dataFormat, ppma, "DataFormat");
                            GetFieldBoolean(ref asReference, ppma, "AsReference");
                            GetFieldBoolean(ref dynamicType, ppma, "DynamicType");
                            if (done = tagIsPinned = fieldNumber > 0) break; // note minAcceptFieldNumber only applies to non-proto
                        }
                    }
                }
            }

            if (!ignore && !done && HasFamily(family, AttributeFamily.DataContractSerialier))
            {
                attrib = GetAttribute(attribs, "System.Runtime.Serialization.DataMemberAttribute");
                if (attrib != null)
                {
                    GetFieldNumber(ref fieldNumber, attrib, "Order");
                    GetFieldName(ref name, attrib, "Name");
                    GetFieldBoolean(ref isRequired, attrib, "IsRequired");
                    done = fieldNumber >= minAcceptFieldNumber;
                    if (done) fieldNumber += dataMemberOffset; // dataMemberOffset only applies to DCS flags, to allow us to "bump" WCF by a notch
                }
            }
            if (!ignore && !done && HasFamily(family, AttributeFamily.XmlSerializer))
            {
                attrib = GetAttribute(attribs, "System.Xml.Serialization.XmlElementAttribute");
                if(attrib == null) attrib = GetAttribute(attribs, "System.Xml.Serialization.XmlArrayAttribute");
                GetIgnore(ref ignore, attrib, attribs, "System.Xml.Serialization.XmlIgnoreAttribute");
                if (attrib != null && !ignore)
                {
                    GetFieldNumber(ref fieldNumber, attrib, "Order");
                    GetFieldName(ref name, attrib, "ElementName");
                    done = fieldNumber >= minAcceptFieldNumber;
                }                
            }
            if (!ignore && !done)
            {
                if (GetAttribute(attribs, "System.NonSerializedAttribute") != null) ignore = true;
            }
            if (ignore || (fieldNumber < minAcceptFieldNumber && !forced)) return null;
            ProtoMemberAttribute result = new ProtoMemberAttribute(fieldNumber, forced || inferByTagName);
            result.AsReference = asReference;
            result.DataFormat = dataFormat;
            result.DynamicType = dynamicType;
            result.IsPacked = isPacked;
            result.OverwriteList = overwriteList;
            result.IsRequired = isRequired;
            result.Name = Helpers.IsNullOrEmpty(name) ? member.Name : name;
            result.Member = member;
            result.TagIsPinned = tagIsPinned;
            return result;
        }
        
        private ValueMember ApplyDefaultBehaviour(bool isEnum, ProtoMemberAttribute normalizedAttribute)
        {
            MemberInfo member;
            if (normalizedAttribute == null || (member = normalizedAttribute.Member) == null) return null; // nix

            Type effectiveType = Helpers.GetMemberType(member);

            
            Type itemType = null;
            Type defaultType = null;
            ResolveListTypes(model, effectiveType, ref itemType, ref defaultType);
            AttributeMap[] attribs = AttributeMap.Create(model, member, true);
            AttributeMap attrib;

            object defaultValue = null;
            // implicit zero default
            if (model.UseImplicitZeroDefaults)
            {
                switch (Helpers.GetTypeCode(effectiveType))
                {
                    case ProtoTypeCode.Boolean: defaultValue = false; break;
                    case ProtoTypeCode.Decimal: defaultValue = (decimal)0; break;
                    case ProtoTypeCode.Single: defaultValue = (float)0; break;
                    case ProtoTypeCode.Double: defaultValue = (double)0; break;
                    case ProtoTypeCode.Byte: defaultValue = (byte)0; break;
                    case ProtoTypeCode.Char: defaultValue = (char)0; break;
                    case ProtoTypeCode.Int16: defaultValue = (short)0; break;
                    case ProtoTypeCode.Int32: defaultValue = (int)0; break;
                    case ProtoTypeCode.Int64: defaultValue = (long)0; break;
                    case ProtoTypeCode.SByte: defaultValue = (sbyte)0; break;
                    case ProtoTypeCode.UInt16: defaultValue = (ushort)0; break;
                    case ProtoTypeCode.UInt32: defaultValue = (uint)0; break;
                    case ProtoTypeCode.UInt64: defaultValue = (ulong)0; break;
                    case ProtoTypeCode.TimeSpan: defaultValue = TimeSpan.Zero; break;
                    case ProtoTypeCode.Guid: defaultValue = Guid.Empty; break;
                }
            }
            if ((attrib = GetAttribute(attribs, "System.ComponentModel.DefaultValueAttribute")) != null)
            {
                object tmp;
                if(attrib.TryGet("Value", out tmp)) defaultValue = tmp;
            }
            ValueMember vm = ((isEnum || normalizedAttribute.Tag > 0))
                ? new ValueMember(model, type, normalizedAttribute.Tag, member, effectiveType, itemType, defaultType, normalizedAttribute.DataFormat, defaultValue)
                    : null;
            if (vm != null)
            {
#if WINRT
                TypeInfo finalType = typeInfo;
#else
                Type finalType = type;
#endif
                PropertyInfo prop = Helpers.GetProperty(finalType, member.Name + "Specified");
                MethodInfo getMethod = Helpers.GetGetMethod(prop, true);
                if (getMethod == null || getMethod.IsStatic) prop = null;
                if (prop != null)
                {
                    vm.SetSpecified(getMethod, Helpers.GetSetMethod(prop, true));
                }
                else
                {
                    MethodInfo method = Helpers.GetInstanceMethod(finalType, "ShouldSerialize" + member.Name, Helpers.EmptyTypes);
                    if (method != null && method.ReturnType == model.MapType(typeof(bool)))
                    {
                        vm.SetSpecified(method, null);
                    }
                }
                if (!Helpers.IsNullOrEmpty(normalizedAttribute.Name)) vm.SetName(normalizedAttribute.Name);
                vm.IsPacked = normalizedAttribute.IsPacked;
                vm.IsRequired = normalizedAttribute.IsRequired;
                vm.OverwriteList = normalizedAttribute.OverwriteList;
                vm.AsReference = normalizedAttribute.AsReference;
                vm.DynamicType = normalizedAttribute.DynamicType;
            }
            return vm;
        }

        private static void GetDataFormat(ref DataFormat value, AttributeMap attrib, string memberName)
        {
            if ((attrib == null) || (value != DataFormat.Default)) return;
            object obj;
            if (attrib.TryGet(memberName, out obj) && obj != null) value = (DataFormat)obj;
        }

        private static void GetIgnore(ref bool ignore, AttributeMap attrib, AttributeMap[] attribs, string fullName)
        {
            if (ignore || attrib == null) return;
            ignore = GetAttribute(attribs, fullName) != null;
            return;
        }

        private static void GetFieldBoolean(ref bool value, AttributeMap attrib, string memberName)
        {
            if (attrib == null || value) return;
            object obj;
            if (attrib.TryGet(memberName, out obj) && obj != null) value = (bool)obj;
        }

        private static void GetFieldNumber(ref int value, AttributeMap attrib, string memberName)
        {
            if (attrib == null || value > 0) return;
            object obj;
            if (attrib.TryGet(memberName, out obj) && obj != null) value = (int)obj;
        }
        private static void GetFieldName(ref string name, AttributeMap attrib, string memberName)
        {
            if (attrib == null || !Helpers.IsNullOrEmpty(name)) return;
            object obj;
            if (attrib.TryGet(memberName, out obj) && obj != null) name = (string)obj;
        }

        private static AttributeMap GetAttribute(AttributeMap[] attribs, string fullName)
        {
            for (int i = 0; i < attribs.Length; i++)
            {
                AttributeMap attrib = attribs[i];
                if (attrib != null && attrib.AttributeType.FullName == fullName) return attrib;
            }
            return null;
        }
        /// <summary>
        /// Adds a member (by name) to the MetaType
        /// </summary>        
        public MetaType Add(int fieldNumber, string memberName)
        {
            AddField(fieldNumber, memberName, null, null, null);
            return this;
        }
        /// <summary>
        /// Adds a member (by name) to the MetaType, returning the ValueMember rather than the fluent API.
        /// This is otherwise identical to Add.
        /// </summary>
        public ValueMember AddField(int fieldNumber, string memberName)
        {
            return AddField(fieldNumber, memberName, null, null, null);
        }
        /// <summary>
        /// Gets or sets whether the type should use a parameterless constructor (the default),
        /// or whether the type should skip the constructor completely. This option is not supported
        /// on compact-framework.
        /// </summary>
        public bool UseConstructor
        { // negated to have defaults as flat zero
            get { return !HasFlag(OPTIONS_SkipConstructor); }
            set { SetFlag(OPTIONS_SkipConstructor, !value, true); }
        }
        /// <summary>
        /// The concrete type to create when a new instance of this type is needed; this may be useful when dealing
        /// with dynamic proxies, or with interface-based APIs
        /// </summary>
        public Type ConstructType
        {
            get { return constructType; }
            set
            {
                ThrowIfFrozen();
                constructType = value;
            }
        }
        private Type constructType;
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
        public void SetSurrogate(Type surrogateType)
        {
            if (surrogateType == type) surrogateType = null;
            if (surrogateType != null)
            {
                // note that BuildSerializer checks the **CURRENT TYPE** is OK to be surrogated
                if (surrogateType != null && Helpers.IsAssignableFrom(model.MapType(typeof(IEnumerable)), surrogateType))
                {
                    throw new ArgumentException("Repeated data (a list, collection, etc) has inbuilt behaviour and cannot be used as a surrogate");
                }
            }
            ThrowIfFrozen();
            this.surrogate = surrogateType;
            // no point in offering chaining; no options are respected
        }
        
        private int GetNextFieldNumber()
        {
            int maxField = 0;
            foreach (ValueMember member in fields)
            {
                if (member.FieldNumber > maxField) maxField = member.FieldNumber;
            }
            if (subTypes != null)
            {
                foreach (SubType subType in subTypes)
                {
                    if (subType.FieldNumber > maxField) maxField = subType.FieldNumber;
                }
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
            AddField(fieldNumber, memberName, null, null, defaultValue);
            return this;
        }

        /// <summary>
        /// Adds a member (by name) to the MetaType, including an itemType and defaultType for representing lists
        /// </summary>
        public MetaType Add(int fieldNumber, string memberName, Type itemType, Type defaultType)
        {
            AddField(fieldNumber, memberName, itemType, defaultType, null);
            return this;
        }

        /// <summary>
        /// Adds a member (by name) to the MetaType, including an itemType and defaultType for representing lists, returning the ValueMember rather than the fluent API.
        /// This is otherwise identical to Add.
        /// </summary>
        public ValueMember AddField(int fieldNumber, string memberName, Type itemType, Type defaultType)
        {
            return AddField(fieldNumber, memberName, itemType, defaultType, null);
        }
        
        private ValueMember AddField(int fieldNumber, string memberName, Type itemType, Type defaultType, object defaultValue)
        {
            MemberInfo mi = null;
#if WINRT
            mi = Helpers.GetInstanceMember(type.GetTypeInfo(), memberName);

#else
            MemberInfo[] members = type.GetMember(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if(members != null && members.Length == 1) mi = members[0];
#endif
            if (mi == null) throw new ArgumentException("Unable to determine member: " + memberName, "memberName");

            Type miType;
#if WINRT || PORTABLE
            PropertyInfo pi = mi as PropertyInfo;
            if (pi == null)
            {
                FieldInfo fi = mi as FieldInfo;
                if (fi == null)
                {
                    throw new NotSupportedException(mi.GetType().Name);
                }
                else
                {
                    miType = fi.FieldType;
                }
            }
            else
            {
                miType = pi.PropertyType;
            }
#else   
            switch (mi.MemberType)
            {
                case MemberTypes.Field:
                    miType = ((FieldInfo)mi).FieldType; break;
                case MemberTypes.Property:
                    miType = ((PropertyInfo)mi).PropertyType; break;
                default:
                    throw new NotSupportedException(mi.MemberType.ToString());
            }
#endif
            ResolveListTypes(model, miType, ref itemType, ref defaultType);
            ValueMember newField = new ValueMember(model, type, fieldNumber, mi, miType, itemType, defaultType, DataFormat.Default, defaultValue);
            Add(newField);
            return newField;
        }

        internal static void ResolveListTypes(TypeModel model, Type type, ref Type itemType, ref Type defaultType)
        {
            if (type == null) return;
            // handle arrays
            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                {
                    throw new NotSupportedException("Multi-dimension arrays are supported");
                }
                itemType = type.GetElementType();
                if (itemType == model.MapType(typeof(byte)))
                {
                    defaultType = itemType = null;
                }
                else
                {
                    defaultType = type;
                }
            }
            // handle lists
            if (itemType == null) { itemType = TypeModel.GetListItemType(model, type); }

            // check for nested data (not allowed)
            if (itemType != null)
            {
                Type nestedItemType = null, nestedDefaultType = null;
                ResolveListTypes(model, itemType, ref nestedItemType, ref nestedDefaultType);
                if (nestedItemType != null)
                {
                    throw TypeModel.CreateNestedListsNotSupported();
                }
            }

            if (itemType != null && defaultType == null)
            {
#if WINRT
                TypeInfo typeInfo = type.GetTypeInfo();
                if (typeInfo.IsClass && !typeInfo.IsAbstract && Helpers.GetConstructor(typeInfo, Helpers.EmptyTypes, true) != null)
#else
                if (type.IsClass && !type.IsAbstract && Helpers.GetConstructor(type, Helpers.EmptyTypes, true) != null)
#endif
                {
                    defaultType = type;
                }
                if (defaultType == null)
                {
#if WINRT
                    if (typeInfo.IsInterface)
#else
                    if (type.IsInterface)
#endif
                    {
#if NO_GENERICS
                        defaultType = typeof(ArrayList);
#else
                        Type[] genArgs;
#if WINRT
                        if (typeInfo.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IDictionary<,>)
                            && itemType == typeof(System.Collections.Generic.KeyValuePair<,>).MakeGenericType(genArgs = typeInfo.GenericTypeArguments))
#else
                        if (type.IsGenericType && type.GetGenericTypeDefinition() == model.MapType(typeof(System.Collections.Generic.IDictionary<,>))
                            && itemType == model.MapType(typeof(System.Collections.Generic.KeyValuePair<,>)).MakeGenericType(genArgs = type.GetGenericArguments()))
#endif
                        {
                            defaultType = model.MapType(typeof(System.Collections.Generic.Dictionary<,>)).MakeGenericType(genArgs);
                        }
                        else
                        {
                            defaultType = model.MapType(typeof(System.Collections.Generic.List<>)).MakeGenericType(itemType);
                        }
#endif
                    }
                }
                // verify that the default type is appropriate
                if (defaultType != null && !Helpers.IsAssignableFrom(type, defaultType)) { defaultType = null; }
            }
        }

        private void Add(ValueMember member) {
            int opaqueToken = 0;
            try {
                model.TakeLock(ref opaqueToken);
                ThrowIfFrozen();
                fields.Add(member);
            } finally
            {
                model.ReleaseLock(opaqueToken);
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
        /// <summary>
        /// Returns the ValueMember that matchs a given member (property/field), or null if not found
        /// </summary>
        public ValueMember this[MemberInfo member]
        {
            get
            {
                if (member == null) return null;
                foreach (ValueMember x in fields)
                {
                    if (x.Member == member) return x;
                }
                return null;
            }
        }
        private readonly BasicList fields = new BasicList();

        //IEnumerable GetFields() { return fields; }

#if FEAT_COMPILER && !FX11

        /// <summary>
        /// Compiles the serializer for this type; this is *not* a full
        /// standalone compile, but can significantly boost performance
        /// while allowing additional types to be added.
        /// </summary>
        /// <remarks>An in-place compile can access non-public types / members</remarks>
        public void CompileInPlace()
        {
#if FEAT_IKVM
            // just no nothing, quietely; don't want to break the API
#else
            serializer = CompiledSerializer.Wrap(Serializer, model);
#endif
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
            if (HasFlag(OPTIONS_EnumPassThru)) return null;
            EnumSerializer.EnumPair[] result = new EnumSerializer.EnumPair[fields.Count];
            for (int i = 0; i < result.Length; i++)
            {
                ValueMember member = (ValueMember) fields[i];
                int wireValue = member.FieldNumber;
                object value = member.GetRawEnumValue();
                result[i] = new EnumSerializer.EnumPair(wireValue, value, member.MemberType);
            }
            return result;
        }


        /// <summary>
        /// Gets or sets a value indicating that an enum should be treated directly as an int/short/etc, rather
        /// than enforcing .proto enum rules. This is useful *in particul* for [Flags] enums.
        /// </summary>
        public bool EnumPassthru
        {
            get { return HasFlag(OPTIONS_EnumPassThru); }
            set { SetFlag(OPTIONS_EnumPassThru, value, true); }
        }

        /// <summary>
        /// Gets or sets a value indicating that this type should NOT be treated as a list, even if it has
        /// familiar list-like characteristics (enumerable, add, etc)
        /// </summary>
        public bool IgnoreListHandling
        {
            get { return HasFlag(OPTIONS_IgnoreListHandling); }
            set { SetFlag(OPTIONS_IgnoreListHandling, value, true); }
        }

        internal bool Pending
        {
            get { return HasFlag(OPTIONS_Pending); }
            set { SetFlag(OPTIONS_Pending, value, false); }
        }

        private const byte
            OPTIONS_Pending = 1,
            OPTIONS_EnumPassThru = 2,
            OPTIONS_Frozen = 4,
            OPTIONS_PrivateOnApi = 8,
            OPTIONS_SkipConstructor = 16,
            OPTIONS_AsReferenceDefault = 32,
            OPTIONS_AutoTuple = 64,
            OPTIONS_IgnoreListHandling = 128;

        private volatile byte flags;
        private bool HasFlag(byte flag) { return (flags & flag) == flag; }
        private void SetFlag(byte flag, bool value, bool throwIfFrozen)
        {
            if (throwIfFrozen && HasFlag(flag) != value)
            {
                ThrowIfFrozen();
            }
            if (value)
                flags |= flag;
            else
                flags = (byte)(flags & ~flag);
        }

        internal static MetaType GetRootType(MetaType source)
        {
           
            while (source.serializer != null)
            {
                MetaType tmp = source.baseType;
                if (tmp == null) return source;
                source = tmp; // else loop until we reach something that isn't generated, or is the root
            }

            // now we get into uncertain territory
            RuntimeTypeModel model = source.model;
            int opaqueToken = 0;
            try {
                model.TakeLock(ref opaqueToken);

                MetaType tmp;
                while ((tmp = source.baseType) != null) source = tmp;
                return source;

            } finally {
                model.ReleaseLock(opaqueToken);
            }
        }

        internal bool IsPrepared()
        {
            #if FEAT_COMPILER && !FEAT_IKVM
            return (serializer as CompiledSerializer) != null;
            #else
            return false;
            #endif
        }

        internal System.Collections.IEnumerable Fields { get { return this.fields; } }

        private System.Text.StringBuilder NewLine(System.Text.StringBuilder builder, int indent)
        {
            return builder.AppendLine().Append(' ', indent*3);

        }
        internal void WriteSchema(System.Text.StringBuilder builder, int indent)
        {
            ValueMember[] fieldsArr = new ValueMember[fields.Count];
            fields.CopyTo(fieldsArr, 0);
            Array.Sort(fieldsArr, ValueMember.Comparer.Default);
            if(Helpers.IsEnum(type))
            {
                NewLine(builder, indent).Append("enum ").Append(Name).Append(" {");
                foreach (ValueMember member in fieldsArr)
                {
                    NewLine(builder, indent + 1).Append(member.Name).Append(" = ").Append(member.FieldNumber).Append(';');
                }
                NewLine(builder, indent).Append('}');
            } else
            {
                NewLine(builder, indent).Append("message ").Append(Name).Append(" {");
                foreach (ValueMember member in fieldsArr)
                {
                    string ordinality = member.ItemType != null ? "repeated" : member.IsRequired ? "required" : "optional";
                    NewLine(builder, indent + 1).Append(ordinality).Append(' ');
                    if (member.DataFormat == DataFormat.Group) builder.Append("group ");
                    builder.Append(member.GetSchemaTypeName()).Append(" ")
                         .Append(member.Name).Append(" = ").Append(member.FieldNumber);
                    if(member.DefaultValue != null)
                    {
                        if (member.DefaultValue is string)
                        {
                            builder.Append(" [default = \"").Append(member.DefaultValue).Append("\"]");
                        }
                        else
                        {
                            builder.Append(" [default = ").Append(member.DefaultValue).Append(']');
                        }
                    }
                    if(member.ItemType != null && member.IsPacked)
                    {
                        builder.Append(" [packed=true]");
                    }
                    builder.Append(';');
                }
                NewLine(builder, indent).Append('}');
            }
        }
    }
}
#endif
