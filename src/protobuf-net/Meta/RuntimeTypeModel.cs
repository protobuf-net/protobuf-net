using System;
using System.Collections;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

using ProtoBuf.Serializers;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ProtoBuf.Compiler;
using ProtoBuf.Internal;
using System.Linq;
using System.Collections.Generic;
using ProtoBuf.Internal.Serializers;

namespace ProtoBuf.Meta
{
    /// <summary>
    /// Provides protobuf serialization support for a number of types that can be defined at runtime
    /// </summary>
    public sealed class RuntimeTypeModel : TypeModel
    {
        private ushort options;
        private const ushort
           OPTIONS_InferTagFromNameDefault = 1,
           OPTIONS_IsDefaultModel = 2,
           OPTIONS_Frozen = 4,
           OPTIONS_AutoAddMissingTypes = 8,
           OPTIONS_AutoCompile = 16,
           OPTIONS_UseImplicitZeroDefaults = 32,
           OPTIONS_AllowParseableTypes = 64,
           OPTIONS_AutoAddProtoContractTypesOnly = 128,
           OPTIONS_IncludeDateTimeKind = 256,
           OPTIONS_InternStrings = 512;

        private bool GetOption(ushort option)
        {
            return (options & option) == option;
        }

        private void SetOption(ushort option, bool value)
        {
            if (value) options |= option;
            else options &= (ushort)~option;
        }

        internal CompilerContextScope Scope { get; } = CompilerContextScope.CreateInProcess();

        /// <summary>
        /// Global default that
        /// enables/disables automatic tag generation based on the existing name / order
        /// of the defined members. See <seealso cref="ProtoContractAttribute.InferTagFromName"/>
        /// for usage and <b>important warning</b> / explanation.
        /// You must set the global default before attempting to serialize/deserialize any
        /// impacted type.
        /// </summary>
        public bool InferTagFromNameDefault
        {
            get { return GetOption(OPTIONS_InferTagFromNameDefault); }
            set { SetOption(OPTIONS_InferTagFromNameDefault, value); }
        }

        /// <summary>
        /// Global default that determines whether types are considered serializable
        /// if they have [DataContract] / [XmlType]. With this enabled, <b>ONLY</b>
        /// types marked as [ProtoContract] are added automatically.
        /// </summary>
        public bool AutoAddProtoContractTypesOnly
        {
            get { return GetOption(OPTIONS_AutoAddProtoContractTypesOnly); }
            set { SetOption(OPTIONS_AutoAddProtoContractTypesOnly, value); }
        }

        /// <summary>
        /// <para>
        /// Global switch that enables or disables the implicit
        /// handling of "zero defaults"; meanning: if no other default is specified,
        /// it assumes bools always default to false, integers to zero, etc.
        /// </para>
        /// <para>
        /// If this is disabled, no such assumptions are made and only *explicit*
        /// default values are processed. This is enabled by default to 
        /// preserve similar logic to v1.
        /// </para>
        /// </summary>
        public bool UseImplicitZeroDefaults
        {
            get { return GetOption(OPTIONS_UseImplicitZeroDefaults); }
            set
            {
                if (!value && GetOption(OPTIONS_IsDefaultModel))
                {
                    throw new InvalidOperationException("UseImplicitZeroDefaults cannot be disabled on the default model");
                }
                SetOption(OPTIONS_UseImplicitZeroDefaults, value);
            }
        }

        /// <summary>
        /// Global switch that determines whether types with a <c>.ToString()</c> and a <c>Parse(string)</c>
        /// should be serialized as strings.
        /// </summary>
        public bool AllowParseableTypes
        {
            get { return GetOption(OPTIONS_AllowParseableTypes); }
            set { SetOption(OPTIONS_AllowParseableTypes, value); }
        }

        /// <summary>
        /// Global switch that determines whether DateTime serialization should include the <c>Kind</c> of the date/time.
        /// </summary>
        public bool IncludeDateTimeKind
        {
            get { return GetOption(OPTIONS_IncludeDateTimeKind); }
            set { SetOption(OPTIONS_IncludeDateTimeKind, value); }
        }

        /// <summary>
        /// Global switch that determines whether a single instance of the same string should be used during deserialization.
        /// </summary>
        /// <remarks>Note this does not use the global .NET string interner</remarks>
        public new bool InternStrings
        {
            get { return GetOption(OPTIONS_InternStrings); }
            set { SetOption(OPTIONS_InternStrings, value); }
        }

        /// <summary>
        /// Global switch that determines whether a single instance of the same string should be used during deserialization.
        /// </summary>
        protected internal override bool GetInternStrings() => InternStrings;

        /// <summary>
        /// Should the <c>Kind</c> be included on date/time values?
        /// </summary>
        protected internal override bool SerializeDateTimeKind()
        {
            return GetOption(OPTIONS_IncludeDateTimeKind);
        }

        private static class DeferredModelLoader
        {
            internal static readonly RuntimeTypeModel Instance = new RuntimeTypeModel(true, "(default)");
        }



        /// <summary>
        /// The default model, used to support ProtoBuf.Serializer
        /// </summary>
        public static RuntimeTypeModel Default
            => (DefaultModel as RuntimeTypeModel) ?? LoadDeferredModel();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RuntimeTypeModel LoadDeferredModel()
        {
            var model = DeferredModelLoader.Instance;
            SetDefaultModel(model);
            return model;
        }

        /// <summary>
        /// Returns a sequence of the Type instances that can be
        /// processed by this model.
        /// </summary>
        public IEnumerable GetTypes() => types;

        /// <summary>
        /// Suggest a .proto definition for the given type
        /// </summary>
        /// <param name="type">The type to generate a .proto definition for, or <c>null</c> to generate a .proto that represents the entire model</param>
        /// <returns>The .proto definition as a string</returns>
        /// <param name="syntax">The .proto syntax to use</param>
        public override string GetSchema(Type type, ProtoSyntax syntax)
        {
            syntax = Serializer.GlobalOptions.Normalize(syntax);
            var requiredTypes = new List<MetaType>();
            MetaType primaryType = null;
            bool isInbuiltType = false;
            if (type == null)
            { // generate for the entire model
                foreach (MetaType meta in types)
                {
                    MetaType tmp = meta.GetSurrogateOrBaseOrSelf(false);
                    if (!requiredTypes.Contains(tmp))
                    { // ^^^ note that the type might have been added as a descendent
                        requiredTypes.Add(tmp);
                        CascadeDependents(requiredTypes, tmp);
                    }
                }
            }
            else
            {
                Type tmp = Nullable.GetUnderlyingType(type);
                if (tmp != null) type = tmp;

                isInbuiltType = (ValueMember.TryGetCoreSerializer(this, DataFormat.Default, type, out var _, false, false, false, false) != null);
                if (!isInbuiltType)
                {
                    //Agenerate just relative to the supplied type
                    int index = FindOrAddAuto(type, false, false, false);
                    if (index < 0) throw new ArgumentException("The type specified is not a contract-type", nameof(type));

                    // get the required types
                    primaryType = ((MetaType)types[index]).GetSurrogateOrBaseOrSelf(false);
                    requiredTypes.Add(primaryType);
                    CascadeDependents(requiredTypes, primaryType);
                }
            }

            // use the provided type's namespace for the "package"
            StringBuilder headerBuilder = new StringBuilder();
            string package = null;

            if (!isInbuiltType)
            {
                IEnumerable<MetaType> typesForNamespace = primaryType == null ? types.Cast<MetaType>() : requiredTypes;
                foreach (MetaType meta in typesForNamespace)
                {
                    if (TryGetRepeatedProvider(meta.Type) != null) continue;

                    string tmp = meta.Type.Namespace;
                    if (!string.IsNullOrEmpty(tmp))
                    {
                        if (tmp.StartsWith("System.")) continue;
                        if (package == null)
                        { // haven't seen any suggestions yet
                            package = tmp;
                        }
                        else if (package == tmp)
                        { // that's fine; a repeat of the one we already saw
                        }
                        else
                        { // something else; have confliucting suggestions; abort
                            package = null;
                            break;
                        }
                    }
                }
            }
            switch (syntax)
            {
                case ProtoSyntax.Proto2:
                    headerBuilder.AppendLine(@"syntax = ""proto2"";");
                    break;
                case ProtoSyntax.Proto3:
                    headerBuilder.AppendLine(@"syntax = ""proto3"";");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(syntax));
            }

            if (!string.IsNullOrEmpty(package))
            {
                headerBuilder.Append("package ").Append(package).Append(';').AppendLine();
            }

            var imports = CommonImports.None;
            StringBuilder bodyBuilder = new StringBuilder();
            // sort them by schema-name
            var callstack = new HashSet<Type>(); // for recursion detection
            MetaType[] metaTypesArr = new MetaType[requiredTypes.Count];
            requiredTypes.CopyTo(metaTypesArr, 0);
            Array.Sort(metaTypesArr, new MetaType.Comparer(callstack));

            // write the messages
            if (isInbuiltType)
            {
                bodyBuilder.AppendLine().Append("message ").Append(type.Name).Append(" {");
                MetaType.NewLine(bodyBuilder, 1).Append(syntax == ProtoSyntax.Proto2 ? "optional " : "").Append(GetSchemaTypeName(callstack, type, DataFormat.Default, false, false, ref imports))
                    .Append(" value = 1;").AppendLine().Append('}');
            }
            else
            {
                for (int i = 0; i < metaTypesArr.Length; i++)
                {
                    MetaType tmp = metaTypesArr[i];
                    if (tmp != primaryType && TryGetRepeatedProvider(tmp.Type) != null) continue;
                    tmp.WriteSchema(callstack, bodyBuilder, 0, ref imports, syntax);
                }
            }
            if ((imports & CommonImports.Bcl) != 0)
            {
                headerBuilder.Append("import \"protobuf-net/bcl.proto\"; // schema for protobuf-net's handling of core .NET types").AppendLine();
            }
            if ((imports & CommonImports.Protogen) != 0)
            {
                headerBuilder.Append("import \"protobuf-net/protogen.proto\"; // custom protobuf-net options").AppendLine();
            }
            if ((imports & CommonImports.Timestamp) != 0)
            {
                headerBuilder.Append("import \"google/protobuf/timestamp.proto\";").AppendLine();
            }
            if ((imports & CommonImports.Duration) != 0)
            {
                headerBuilder.Append("import \"google/protobuf/duration.proto\";").AppendLine();
            }
            return headerBuilder.Append(bodyBuilder).AppendLine().ToString();
        }

        [Flags]
        internal enum CommonImports
        {
            None = 0,
            Bcl = 1,
            Timestamp = 2,
            Duration = 4,
            Protogen = 8
        }

        private void CascadeRepeated(List<MetaType> list, RepeatedSerializerStub provider)
        {
            if (provider.IsMap)
            {
                provider.ResolveMapTypes(out var key, out var value);
                TryGetCoreSerializer(list, key);
                TryGetCoreSerializer(list, value);

                if (!provider.IsValidProtobufMap(this)) // add the KVP
                    TryGetCoreSerializer(list, provider.ItemType);
            }
            else
            {
                TryGetCoreSerializer(list, provider.ItemType);
            }
        }
        private void CascadeDependents(List<MetaType> list, MetaType metaType)
        {
            MetaType tmp;
            var repeated = TryGetRepeatedProvider(metaType.Type);
            if (repeated != null)
            {
                CascadeRepeated(list, repeated);
            }
            else
            {
                if (metaType.IsAutoTuple)
                {
                    if (MetaType.ResolveTupleConstructor(metaType.Type, out var mapping) != null)
                    {
                        for (int i = 0; i < mapping.Length; i++)
                        {
                            Type type = null;
                            if (mapping[i] is PropertyInfo) type = ((PropertyInfo)mapping[i]).PropertyType;
                            else if (mapping[i] is FieldInfo) type = ((FieldInfo)mapping[i]).FieldType;
                            TryGetCoreSerializer(list, type);
                        }
                    }
                }
                else
                {
                    foreach (ValueMember member in metaType.Fields)
                    {
                        repeated = TryGetRepeatedProvider(member.MemberType);
                        if (repeated != null)
                        {
                            CascadeRepeated(list, repeated);
                            if (repeated.IsMap && !member.IsMap) // include the KVP, then
                                TryGetCoreSerializer(list, repeated.ItemType);
                        }
                        else
                        {
                            TryGetCoreSerializer(list, member.MemberType);
                        }
                    }
                }
                foreach (var genericArgument in metaType.GetAllGenericArguments())
                {
                    repeated = TryGetRepeatedProvider(genericArgument);
                    if (repeated != null)
                    {
                        CascadeRepeated(list, repeated);
                    }
                    else
                    {
                        TryGetCoreSerializer(list, genericArgument);
                    }
                }
                if (metaType.HasSubtypes)
                {
                    foreach (SubType subType in metaType.GetSubtypes())
                    {
                        tmp = subType.DerivedType.GetSurrogateOrSelf(); // note: exclude base-types!
                        if (!list.Contains(tmp))
                        {
                            list.Add(tmp);
                            CascadeDependents(list, tmp);
                        }
                    }
                }
                tmp = metaType.BaseType;
                if (tmp != null) tmp = tmp.GetSurrogateOrSelf(); // note: already walking base-types; exclude base
                if (tmp != null && !list.Contains(tmp))
                {
                    list.Add(tmp);
                    CascadeDependents(list, tmp);
                }
            }
        }

        private void CheckNotNested(RepeatedSerializerStub repeated)
        {
            if (repeated == null) { } // fine
            else if (repeated.IsMap)
            {
                repeated.ResolveMapTypes(out var key, out var value);
                if (key == repeated.ForType || TryGetRepeatedProvider(key) != null) ThrowHelper.ThrowNestedDataNotSupported(repeated.ForType);
                if (value == repeated.ForType || TryGetRepeatedProvider(value) != null) ThrowHelper.ThrowNestedDataNotSupported(repeated.ForType);
            }
            else
            {
                if (repeated.ItemType == repeated.ForType || TryGetRepeatedProvider(repeated.ItemType) != null) ThrowHelper.ThrowNestedDataNotSupported(repeated.ForType);
            }
        }

        private void TryGetCoreSerializer(List<MetaType> list, Type itemType)
        {
            var coreSerializer = ValueMember.TryGetCoreSerializer(this, DataFormat.Default, itemType, out _, false, false, false, false);
            if (coreSerializer != null)
            {
                return;
            }
            int index = FindOrAddAuto(itemType, false, false, false);
            if (index < 0)
            {
                return;
            }
            var temp = ((MetaType)types[index]).GetSurrogateOrBaseOrSelf(false);
            if (list.Contains(temp))
            {
                return;
            }
            // could perhaps also implement as a queue, but this should work OK for sane models
            list.Add(temp);
            CascadeDependents(list, temp);
        }

        internal RuntimeTypeModel(bool isDefault, string name)
        {
            AutoAddMissingTypes = true;
            UseImplicitZeroDefaults = true;
            SetOption(OPTIONS_IsDefaultModel, isDefault);
#if !DEBUG
            try
            {
                AutoCompile = EnableAutoCompile();
            }
            catch { } // this is all kinds of brittle on things like UWP
#endif
            if (!string.IsNullOrWhiteSpace(name)) _name = name;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool EnableAutoCompile()
        {
            try
            {
                var dm = new DynamicMethod("CheckCompilerAvailable", typeof(bool), new Type[] { typeof(int) });
                var il = dm.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4, 42);
                il.Emit(OpCodes.Ceq);
                il.Emit(OpCodes.Ret);
                var func = (Predicate<int>)dm.CreateDelegate(typeof(Predicate<int>));
                return func(42);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        /// <summary>
        /// Obtains the MetaType associated with a given Type for the current model,
        /// allowing additional configuration.
        /// </summary>
        public MetaType this[Type type] { get { return (MetaType)types[FindOrAddAuto(type, true, false, false)]; } }

        internal MetaType FindWithoutAdd(Type type)
        {
            // this list is thread-safe for reading
            type = DynamicStub.GetEffectiveType(type);
            foreach (MetaType metaType in types)
            {
                if (metaType.Type == type)
                {
                    if (metaType.Pending) WaitOnLock();
                    return metaType;
                }
            }
            return null;
        }

        private static readonly BasicList.MatchPredicate BasicTypeFinder = (value, ctx)
            => ((BasicType)value).Type == (Type)ctx;

        private static readonly BasicList.MatchPredicate MetaTypeFinder = (value, ctx)
            => ((MetaType)value).Type == (Type)ctx;

        private void WaitOnLock()
        {
            int opaqueToken = 0;
            try
            {
                TakeLock(ref opaqueToken);
            }
            finally
            {
                ReleaseLock(opaqueToken);
            }
        }

        private readonly BasicList types = new BasicList(), basicTypes = new BasicList();

        private sealed class BasicType
        {
            public Type Type { get; }

            public IRuntimeProtoSerializerNode Serializer { get; }

            public BasicType(Type type, IRuntimeProtoSerializerNode serializer)
            {
                Type = type;
                Serializer = serializer;
            }
        }

        internal IRuntimeProtoSerializerNode TryGetBasicTypeSerializer(Type type)
        {
            int idx = basicTypes.IndexOf(BasicTypeFinder, type);

            if (idx >= 0) return ((BasicType)basicTypes[idx]).Serializer;

            lock (basicTypes)
            { // don't need a full model lock for this
                // double-checked
                idx = basicTypes.IndexOf(BasicTypeFinder, type);
                if (idx >= 0) return ((BasicType)basicTypes[idx]).Serializer;

                MetaType.AttributeFamily family = MetaType.GetContractFamily(this, type, null);
                IRuntimeProtoSerializerNode ser = family == MetaType.AttributeFamily.None
                    ? ValueMember.TryGetCoreSerializer(this, DataFormat.Default, type, out WireType defaultWireType, false, false, false, false)
                    : null;

                if (ser != null) basicTypes.Add(new BasicType(type, ser));
                return ser;
            }
        }

        internal int FindOrAddAuto(Type type, bool demand, bool addWithContractOnly, bool addEvenIfAutoDisabled)
        {
            type = DynamicStub.GetEffectiveType(type);
            int key = types.IndexOf(MetaTypeFinder, type);
            MetaType metaType;

            // the fast happy path: meta-types we've already seen
            if (key >= 0)
            {
                metaType = (MetaType)types[key];
                if (metaType.Pending)
                {
                    WaitOnLock();
                }
                return key;
            }

            // the fast fail path: types that will never have a meta-type
            bool shouldAdd = AutoAddMissingTypes || addEvenIfAutoDisabled;

            if (!type.IsEnum && TryGetBasicTypeSerializer(type) != null)
            {
                if (shouldAdd && !addWithContractOnly) throw MetaType.InbuiltType(type);
                return -1; // this will never be a meta-type
            }

            // otherwise: we don't yet know

            if (key < 0)
            {
                int opaqueToken = 0;
                bool weAdded = false;
                try
                {
                    TakeLock(ref opaqueToken);
                    // try to recognise a few familiar patterns...
                    if ((metaType = RecogniseCommonTypes(type)) == null)
                    { // otherwise, check if it is a contract
                        MetaType.AttributeFamily family = MetaType.GetContractFamily(this, type, null);
                        if (family == MetaType.AttributeFamily.AutoTuple)
                        {
                            shouldAdd = addEvenIfAutoDisabled = true; // always add basic tuples, such as KeyValuePair
                        }

                        if (!shouldAdd || (
                            !type.IsEnum && addWithContractOnly && family == MetaType.AttributeFamily.None)
                            )
                        {
                            if (demand) ThrowUnexpectedType(type, this);
                            return key;
                        }

                        metaType = Create(type);
                    }

                    metaType.Pending = true;

                    // double-checked
                    int winner = types.IndexOf(MetaTypeFinder, type);
                    if (winner < 0)
                    {
                        ThrowIfFrozen();
                        key = types.Add(metaType);
                        weAdded = true;
                    }
                    else
                    {
                        key = winner;
                    }
                    if (weAdded)
                    {
                        metaType.ApplyDefaultBehaviour();
                        metaType.Pending = false;
                    }
                }
                finally
                {
                    ReleaseLock(opaqueToken);
                }
            }
            return key;
        }

#pragma warning disable RCS1163, IDE0060 // Unused parameter.
        private MetaType RecogniseCommonTypes(Type type)
#pragma warning restore RCS1163, IDE0060 // Unused parameter.
        {
            //            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
            //            {
            //                MetaType mt = new MetaType(this, type);

            //                Type surrogate = typeof (KeyValuePairSurrogate<,>).MakeGenericType(type.GetGenericArguments());

            //                mt.SetSurrogate(surrogate);
            //                mt.IncludeSerializerMethod = false;
            //                mt.Freeze();

            //                MetaType surrogateMeta = (MetaType)types[FindOrAddAuto(surrogate, true, true, true)]; // this forcibly adds it if needed
            //                if(surrogateMeta.IncludeSerializerMethod)
            //                { // don't blindly set - it might be frozen
            //                    surrogateMeta.IncludeSerializerMethod = false;
            //                }
            //                surrogateMeta.Freeze();
            //                return mt;
            //            }
            return null;
        }
        private MetaType Create(Type type)
        {
            ThrowIfFrozen();
            return new MetaType(this, type, defaultFactory);
        }

        /// <summary>
        /// Like the non-generic Add(Type); for convenience
        /// </summary>
        public MetaType Add<T>(bool applyDefaultBehaviour = true)
            => Add(typeof(T), applyDefaultBehaviour);

        /// <summary>
        /// Adds support for an additional type in this model, optionally
        /// applying inbuilt patterns. If the type is already known to the
        /// model, the existing type is returned **without** applying
        /// any additional behaviour.
        /// </summary>
        /// <remarks>Inbuilt patterns include:
        /// [ProtoContract]/[ProtoMember(n)]
        /// [DataContract]/[DataMember(Order=n)]
        /// [XmlType]/[XmlElement(Order=n)]
        /// [On{Des|S}erializ{ing|ed}]
        /// ShouldSerialize*/*Specified
        /// </remarks>
        /// <param name="type">The type to be supported</param>
        /// <param name="applyDefaultBehaviour">Whether to apply the inbuilt configuration patterns (via attributes etc), or
        /// just add the type with no additional configuration (the type must then be manually configured).</param>
        /// <returns>The MetaType representing this type, allowing
        /// further configuration.</returns>
        public MetaType Add(Type type, bool applyDefaultBehaviour = true)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (type == typeof(object))
                throw new ArgumentException("You cannot reconfigure " + type.FullName);
            type = DynamicStub.GetEffectiveType(type);
            MetaType newType = FindWithoutAdd(type);
            if (newType != null) return newType; // return existing
            int opaqueToken = 0;

            try
            {
                newType = RecogniseCommonTypes(type);
                if (newType != null)
                {
                    if (!applyDefaultBehaviour)
                    {
                        throw new ArgumentException(
                            "Default behaviour must be observed for certain types with special handling; " + type.FullName,
                            nameof(applyDefaultBehaviour));
                    }
                    // we should assume that type is fully configured, though; no need to re-run:
                    applyDefaultBehaviour = false;
                }
                if (newType == null) newType = Create(type);
                newType.Pending = true;
                TakeLock(ref opaqueToken);
                // double checked
                if (FindWithoutAdd(type) != null) throw new ArgumentException("Duplicate type", nameof(type));
                ThrowIfFrozen();
                types.Add(newType);
                if (applyDefaultBehaviour) { newType.ApplyDefaultBehaviour(); }
                newType.Pending = false;
            }
            finally
            {
                ReleaseLock(opaqueToken);
            }

            return newType;
        }

        /// <summary>
        /// Should serializers be compiled on demand? It may be useful
        /// to disable this for debugging purposes.
        /// </summary>
        public bool AutoCompile
        {
            get { return GetOption(OPTIONS_AutoCompile); }
            set { SetOption(OPTIONS_AutoCompile, value); }
        }

        /// <summary>
        /// Should support for unexpected types be added automatically?
        /// If false, an exception is thrown when unexpected types
        /// are encountered.
        /// </summary>
        public bool AutoAddMissingTypes
        {
            get { return GetOption(OPTIONS_AutoAddMissingTypes); }
            set
            {
                if (!value && GetOption(OPTIONS_IsDefaultModel))
                {
                    throw new InvalidOperationException("The default model must allow missing types");
                }
                ThrowIfFrozen();
                SetOption(OPTIONS_AutoAddMissingTypes, value);
            }
        }
        /// <summary>
        /// Verifies that the model is still open to changes; if not, an exception is thrown
        /// </summary>
        private void ThrowIfFrozen()
        {
            if (GetOption(OPTIONS_Frozen)) throw new InvalidOperationException("The model cannot be changed once frozen");
        }

        /// <summary>
        /// Prevents further changes to this model
        /// </summary>
        public void Freeze()
        {
            if (GetOption(OPTIONS_IsDefaultModel)) throw new InvalidOperationException("The default model cannot be frozen");
            SetOption(OPTIONS_Frozen, true);
        }

        /// <summary>Resolve a service relative to T</summary>
        protected internal override ISerializer<T> GetSerializer<T>()
            => GetServices<T>() as ISerializer<T>;

        /// <summary>Indicates whether a type is known to the model</summary>
        internal override bool IsKnownType<T>() // the point of this override is to avoid loops
                                                // when trying to *build* a model; we don't actually need the service (which may not exist yet);
                                                // we just need to know whether we should *expect one*
            => _serviceCache[typeof(T)] is object || FindOrAddAuto(typeof(T), false, true, false) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object GetServices<T>()
            => (_serviceCache[typeof(T)] ?? GetServicesSlow(typeof(T)));


        private readonly Hashtable _serviceCache = new Hashtable();
        internal void ResetServiceCache(Type type)
        {
            if (type != null)
            {
                lock (_serviceCache)
                {
                    _serviceCache.Remove(type);
                }
            }
        }

        private object GetServicesSlow(Type type)
        {
            if (type == null) return null; // GIGO
            object service;
            lock (_serviceCache)
            {   // once more, with feeling
                service = _serviceCache[type];
                if (service != null) return service;
            }
            service = GetServicesImpl();
            if (service != null)
            {
                try {
                    _ = this[type]; // if possible, make sure that we've registered it, so we export a proxy if needed
                } catch { }
                lock (_serviceCache)
                {
                    _serviceCache[type] = service;
                }
            }
            return service;

            object GetServicesImpl()
            {
                if (type.IsEnum) return EnumSerializers.GetSerializer(type);

                // rule out repeated (this has an internal cache etc)
                var repeated = TryGetRepeatedProvider(type); // this handles ignores, etc
                if (repeated != null) return repeated.Serializer;

                int typeIndex = FindOrAddAuto(type, false, true, false);
                if (typeIndex >= 0)
                {
                    var mt = (MetaType)types[typeIndex];
                    var serializer = mt.Serializer;
                    if (serializer is IExternalSerializer external)
                    {
                        return external.Service;
                    }
                    return serializer;
                }

                return null;
            }
            
        }

        /// <summary>
        /// See Object.ToString
        /// </summary>
        public override string ToString() => _name ?? base.ToString();

        // this is used by some unit-tests; do not remove
        internal Compiler.ProtoSerializer<TActual> GetSerializer<TActual>(IRuntimeProtoSerializerNode serializer, bool compiled)
        {
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));

            if (compiled) return Compiler.CompilerContext.BuildSerializer<TActual>(Scope, serializer, this);

            return new Compiler.ProtoSerializer<TActual>(
                (ref ProtoWriter.State state, TActual val) => serializer.Write(ref state, val));
        }

        /// <summary>
        /// Compiles the serializers individually; this is *not* a full
        /// standalone compile, but can significantly boost performance
        /// while allowing additional types to be added.
        /// </summary>
        /// <remarks>An in-place compile can access non-public types / members</remarks>
        public void CompileInPlace()
        {
            foreach (MetaType type in types)
            {
                type.CompileInPlace();
            }
        }

        private void BuildAllSerializers()
        {
            // note that types.Count may increase during this operation, as some serializers
            // bring other types into play
            for (int i = 0; i < types.Count; i++)
            {
                // the primary purpose of this is to force the creation of the Serializer
                MetaType mt = (MetaType)types[i];
                
                if (GetServicesSlow(mt.Type) == null) // respects enums, repeated, etc
                    throw new InvalidOperationException("No serializer available for " + mt.Type.NormalizeName());
            }
        }

        internal sealed class SerializerPair : IComparable
        {
            int IComparable.CompareTo(object obj)
            {
                if (obj == null) throw new ArgumentException("obj");
                SerializerPair other = (SerializerPair)obj;

                // we want to bunch all the items with the same base-type together, but we need the items with a
                // different base **first**.
                if (this.BaseKey == this.MetaKey)
                {
                    if (other.BaseKey == other.MetaKey)
                    { // neither is a subclass
                        return this.MetaKey.CompareTo(other.MetaKey);
                    }
                    else
                    { // "other" (only) is involved in inheritance; "other" should be first
                        return 1;
                    }
                }
                else
                {
                    if (other.BaseKey == other.MetaKey)
                    { // "this" (only) is involved in inheritance; "this" should be first
                        return -1;
                    }
                    else
                    { // both are involved in inheritance
                        int result = this.BaseKey.CompareTo(other.BaseKey);
                        if (result == 0) result = this.MetaKey.CompareTo(other.MetaKey);
                        return result;
                    }
                }
            }
            public readonly int MetaKey, BaseKey;
            public readonly MetaType Type;
            public readonly MethodBuilder Serialize, Deserialize;
            public readonly ILGenerator SerializeBody, DeserializeBody;
            public SerializerPair(int metaKey, int baseKey, MetaType type, MethodBuilder serialize, MethodBuilder deserialize,
                ILGenerator serializeBody, ILGenerator deserializeBody)
            {
                this.MetaKey = metaKey;
                this.BaseKey = baseKey;
                this.Serialize = serialize;
                this.Deserialize = deserialize;
                this.SerializeBody = serializeBody;
                this.DeserializeBody = deserializeBody;
                this.Type = type;
            }
        }

        internal static ILGenerator Override(TypeBuilder type, string name)
            => Override(type, name, out _);
        internal static ILGenerator Override(TypeBuilder type, string name, out Type[] genericArgs)
        {
            MethodInfo baseMethod;
            try
            {
                baseMethod = type.BaseType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Unable to resolve '{name}': {ex.Message}", nameof(name), ex);
            }

            var parameters = baseMethod.GetParameters();
            var paramTypes = new Type[parameters.Length];
            for (int i = 0; i < paramTypes.Length; i++)
            {
                paramTypes[i] = parameters[i].ParameterType;
            }
            MethodBuilder newMethod = type.DefineMethod(baseMethod.Name,
                (baseMethod.Attributes & ~MethodAttributes.Abstract) | MethodAttributes.Final, baseMethod.CallingConvention, baseMethod.ReturnType, paramTypes);
            if (baseMethod.IsGenericMethodDefinition)
            {
                genericArgs = baseMethod.GetGenericArguments();
                string[] names = Array.ConvertAll(genericArgs, x => x.Name);
                newMethod.DefineGenericParameters(names);
            }
            else
                genericArgs = Type.EmptyTypes;
            for (int i = 0; i < parameters.Length; i++)
            {
                newMethod.DefineParameter(i + 1, parameters[i].Attributes, parameters[i].Name);
            }
            ILGenerator il = newMethod.GetILGenerator();
            type.DefineMethodOverride(newMethod, baseMethod);
            return il;
        }

        /// <summary>
        /// Represents configuration options for compiling a model to 
        /// a standalone assembly.
        /// </summary>
        public sealed class CompilerOptions
        {
            /// <summary>
            /// Import framework options from an existing type
            /// </summary>
            public void SetFrameworkOptions(MetaType from)
            {
                if (from == null) throw new ArgumentNullException(nameof(from));
                AttributeMap[] attribs = AttributeMap.Create(from.Type.Assembly);
                foreach (AttributeMap attrib in attribs)
                {
                    if (attrib.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute")
                    {
                        if (attrib.TryGet("FrameworkName", out var tmp)) TargetFrameworkName = (string)tmp;
                        if (attrib.TryGet("FrameworkDisplayName", out tmp)) TargetFrameworkDisplayName = (string)tmp;
                        break;
                    }
                }
            }

            /// <summary>
            /// The TargetFrameworkAttribute FrameworkName value to burn into the generated assembly
            /// </summary>
            public string TargetFrameworkName { get; set; }

            /// <summary>
            /// The TargetFrameworkAttribute FrameworkDisplayName value to burn into the generated assembly
            /// </summary>
            public string TargetFrameworkDisplayName { get; set; }
            /// <summary>
            /// The name of the TypeModel class to create
            /// </summary>
            public string TypeName { get; set; }

#if PLAT_NO_EMITDLL
            internal const string NoPersistence = "Assembly persistence not supported on this runtime";
#endif
            /// <summary>
            /// The path for the new dll
            /// </summary>
#if PLAT_NO_EMITDLL
            [Obsolete(NoPersistence)]
#endif
            public string OutputPath { get; set; }
            /// <summary>
            /// The runtime version for the generated assembly
            /// </summary>
            public string ImageRuntimeVersion { get; set; }
            /// <summary>
            /// The runtime version for the generated assembly
            /// </summary>
            public int MetaDataVersion { get; set; }

            /// <summary>
            /// The acecssibility of the generated serializer
            /// </summary>
            public Accessibility Accessibility { get; set; }

            /// <summary>
            /// Implements a filter for use when generating models from assemblies
            /// </summary>
            public event Func<Type, bool> IncludeType;

            internal bool OnIncludeType(Type type)
            {
                var evt = IncludeType;
                return evt == null ? true : evt(type);
            }
        }

        /// <summary>
        /// Type accessibility
        /// </summary>
        public enum Accessibility
        {
            /// <summary>
            /// Available to all callers
            /// </summary>
            Public,
            /// <summary>
            /// Available to all callers in the same assembly, or assemblies specified via [InternalsVisibleTo(...)]
            /// </summary>
            Internal
        }

#if !PLAT_NO_EMITDLL
        /// <summary>
        /// Fully compiles the current model into a static-compiled serialization dll
        /// (the serialization dll still requires protobuf-net for support services).
        /// </summary>
        /// <remarks>A full compilation is restricted to accessing public types / members</remarks>
        /// <param name="name">The name of the TypeModel class to create</param>
        /// <param name="path">The path for the new dll</param>
        /// <returns>An instance of the newly created compiled type-model</returns>
        public TypeModel Compile(string name, string path)
        {
            var options = new CompilerOptions()
            {
                TypeName = name,
#pragma warning disable CS0618
                OutputPath = path,
#pragma warning restore CS0618
            };
            return Compile(options);
        }
#endif
        /// <summary>
        /// Fully compiles the current model into a static-compiled serialization dll
        /// (the serialization dll still requires protobuf-net for support services).
        /// </summary>
        /// <remarks>A full compilation is restricted to accessing public types / members</remarks>
        /// <returns>An instance of the newly created compiled type-model</returns>
        public TypeModel Compile(CompilerOptions options = null)
        {
            options ??= new CompilerOptions();
            string typeName = options.TypeName;
#pragma warning disable 0618
            string path = options.OutputPath;
#pragma warning restore 0618
            BuildAllSerializers();
            Freeze();
            bool save = !string.IsNullOrEmpty(path);
            if (string.IsNullOrEmpty(typeName))
            {
                if (save) throw new ArgumentNullException("typeName");
                typeName = "CompiledModel_" + Guid.NewGuid().ToString();
            }

            string assemblyName, moduleName;
            if (path == null)
            {
                assemblyName = typeName;
                moduleName = assemblyName + ".dll";
            }
            else
            {
                assemblyName = new System.IO.FileInfo(System.IO.Path.GetFileNameWithoutExtension(path)).Name;
                moduleName = assemblyName + System.IO.Path.GetExtension(path);
            }

#if PLAT_NO_EMITDLL
            AssemblyName an = new AssemblyName { Name = assemblyName };
            AssemblyBuilder asm = AssemblyBuilder.DefineDynamicAssembly(an,
                AssemblyBuilderAccess.Run);
            ModuleBuilder module = asm.DefineDynamicModule(moduleName);
#else
            AssemblyName an = new AssemblyName { Name = assemblyName };
            AssemblyBuilder asm = AppDomain.CurrentDomain.DefineDynamicAssembly(an,
                save ? AssemblyBuilderAccess.RunAndSave : AssemblyBuilderAccess.Run);
            ModuleBuilder module = save ? asm.DefineDynamicModule(moduleName, path)
                                        : asm.DefineDynamicModule(moduleName);
#endif
            var scope = CompilerContextScope.CreateForModule(this, module, true, assemblyName);
            WriteAssemblyAttributes(options, assemblyName, asm);


            var serviceType = WriteBasicTypeModel("<Services>"+ typeName, module, typeof(object), true);
            WriteSerializers(scope, serviceType);
            WriteEnumsAndProxies(serviceType);

#if PLAT_NO_EMITDLL
            var finalServiceType = serviceType.CreateTypeInfo().AsType();
#else
            var finalServiceType = serviceType.CreateType();
#endif

            var modelType = WriteBasicTypeModel(typeName, module, typeof(TypeModel),
                options.Accessibility == Accessibility.Internal);

            WriteConstructorsAndOverrides(modelType, finalServiceType);

#if PLAT_NO_EMITDLL
            Type finalType = modelType.CreateTypeInfo().AsType();
#else
            Type finalType = modelType.CreateType();
#endif
            if (!string.IsNullOrEmpty(path))
            {
#if PLAT_NO_EMITDLL
                throw new NotSupportedException(CompilerOptions.NoPersistence);
#else
                try
                {
                    asm.Save(path);
                }
                catch (IOException ex)
                {
                    // advertise the file info
                    throw new IOException(path + ", " + ex.Message, ex);
                }
                Debug.WriteLine("Wrote dll:" + path);
#endif
            }
            return (TypeModel)Activator.CreateInstance(finalType, nonPublic: true);
        }

        private void WriteConstructorsAndOverrides(TypeBuilder type, Type serviceType)
        {
            var il = Override(type, nameof(TypeModel.GetInternStrings));
            il.Emit(InternStrings ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ret);

            il = Override(type, nameof(TypeModel.SerializeDateTimeKind));
            il.Emit(IncludeDateTimeKind ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Ret);

            il = Override(type, nameof(TypeModel.GetSerializer), out var genericArgs);
            var genericT = genericArgs.Single();
            var method = typeof(SerializerCache).GetMethod(nameof(SerializerCache.Get)).MakeGenericMethod(serviceType, genericT);
            il.EmitCall(OpCodes.Call, method, null);
            il.Emit(OpCodes.Ret);

            type.DefineDefaultConstructor(MethodAttributes.Public);
        }

        private void WriteEnumsAndProxies(TypeBuilder type)
        {
            for (int index = 0; index < types.Count; index++)
            {
                var metaType = (MetaType)types[index];
                var runtimeType = metaType.Type;
                RepeatedSerializerStub repeated;
                if (runtimeType.IsEnum)
                {
                    var member = EnumSerializers.GetProvider(runtimeType);
                    AddProxy(type, runtimeType, member, true);
                }
                else if (metaType.SerializerType != null)
                {
                    AddProxy(type, runtimeType, metaType.SerializerType, false);
                }
                else if ((repeated = TryGetRepeatedProvider(runtimeType)) != null)
                {
                    AddProxy(type, runtimeType, repeated.Provider, false);
                }
            }
        }

        internal static MemberInfo GetUnderlyingProvider(MemberInfo provider, Type forType)
        {
            switch (provider)
            {   // properties are really a special-case of methods, via the getter
                case PropertyInfo property:
                    provider = property.GetGetMethod(true);
                    break;
                // types are really a short-hand for the singleton API
                case Type type when type.IsClass && !type.IsAbstract && type.GetConstructor(Type.EmptyTypes) != null:
                    provider = typeof(SerializerCache).GetMethod(nameof(SerializerCache.Get), BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(type, forType);
                    break;
            }
            return provider;
        }

        internal static void EmitProvider(MemberInfo provider, ILGenerator il)
        {
            // after GetUnderlyingProvider, all we *actually* need to implement is fields and methods
            switch (provider)
            {
                case FieldInfo field when field.IsStatic:
                    il.Emit(OpCodes.Ldsfld, field);
                    break;
                case MethodInfo method when method.IsStatic:
                    il.EmitCall(OpCodes.Call, method, null);
                    break;
                default:
                    ThrowHelper.ThrowInvalidOperationException($"Invalid provider: {provider}");
                    break;
            }
        }

        internal RepeatedSerializerStub TryGetRepeatedProvider(Type type)
        {
            if (type == null) return null;
            var repeated = RepeatedSerializers.TryGetRepeatedProvider(type);
            // but take it back if it is explicitly excluded
            if (repeated != null)
            { // looks like a list, but double check for IgnoreListHandling
                int idx = this.FindOrAddAuto(type, false, true, false);
                if (idx >= 0 && ((MetaType)types[idx]).IgnoreListHandling)
                {
                    return null;
                }
            }
            if (repeated == null && type.IsArray && type != typeof(byte[]))
                ThrowHelper.ThrowNotSupportedException("Multi-dimensional and non-vector arrays are not supported");
            CheckNotNested(repeated);
            return repeated;
        }

        private void AddProxy(TypeBuilder building, Type proxying, MemberInfo provider, bool includeNullable)
        {
            provider = GetUnderlyingProvider(provider, proxying);
            if (provider != null)
            {
                var iType = typeof(ISerializerProxy<>).MakeGenericType(proxying);
                building.AddInterfaceImplementation(iType);
                var il = CompilerContextScope.Implement(building, iType, "get_" + nameof(ISerializerProxy<string>.Serializer));
                EmitProvider(provider, il);
                il.Emit(OpCodes.Ret);

                if (includeNullable)
                {
                    iType = typeof(ISerializerProxy<>).MakeGenericType(typeof(Nullable<>).MakeGenericType(proxying));
                    building.AddInterfaceImplementation(iType);
                    il = CompilerContextScope.Implement(building, iType, "get_" + nameof(ISerializerProxy<string>.Serializer));
                    EmitProvider(provider, il);
                    il.Emit(OpCodes.Ret);
                }
            }
        }

        private void WriteSerializers(CompilerContextScope scope, TypeBuilder type)
        {
            for (int index = 0; index < types.Count; index++)
            {
                var metaType = (MetaType)types[index];
                var serializer = metaType.Serializer;
                var runtimeType = metaType.Type;
                ILGenerator il;
                if (runtimeType.IsEnum || metaType.SerializerType is object || TryGetRepeatedProvider(metaType.Type) != null)
                {   // we don't implement these
                    continue;
                }
                if (!IsFullyPublic(runtimeType, out var problem))
                {
                    ThrowHelper.ThrowInvalidOperationException("Non-public type cannot be used with full dll compilation: " + problem.NormalizeName());
                }

                Type inheritanceRoot = metaType.GetInheritanceRoot();

                // we always emit the serializer API
                var serType = typeof(ISerializer<>).MakeGenericType(runtimeType);
                type.AddInterfaceImplementation(serType);

                il = CompilerContextScope.Implement(type, serType, nameof(ISerializer<string>.Read));
                using (var ctx = new CompilerContext(scope, il, false, CompilerContext.SignatureType.ReaderScope_Input, this, runtimeType, nameof(ISerializer<string>.Read)))
                {
                    if (serializer.HasInheritance)
                    {
                        serializer.EmitReadRoot(ctx, ctx.InputValue);
                    }
                    else
                    {
                        serializer.EmitRead(ctx, ctx.InputValue);
                        ctx.LoadValue(ctx.InputValue);
                    }
                    ctx.Return();
                }

                il = CompilerContextScope.Implement(type, serType, nameof(ISerializer<string>.Write));
                using (var ctx = new CompilerContext(scope, il, false, CompilerContext.SignatureType.WriterScope_Input, this, runtimeType, nameof(ISerializer<string>.Write)))
                {
                    if (serializer.HasInheritance) serializer.EmitWriteRoot(ctx, ctx.InputValue);
                    else serializer.EmitWrite(ctx, ctx.InputValue);
                    ctx.Return();
                }

                il = CompilerContextScope.Implement(type, serType, "get_" + nameof(ISerializer<string>.Features));
                CompilerContext.LoadValue(il, (int)serializer.Features);
                il.Emit(OpCodes.Ret);

                // and we emit the sub-type serializer whenever inheritance is involved
                if (serializer.HasInheritance)
                {
                    serType = typeof(ISubTypeSerializer<>).MakeGenericType(runtimeType);
                    type.AddInterfaceImplementation(serType);

                    il = CompilerContextScope.Implement(type, serType, nameof(ISubTypeSerializer<string>.WriteSubType));
                    using (var ctx = new CompilerContext(scope, il, false, CompilerContext.SignatureType.WriterScope_Input, this,
                         runtimeType, nameof(ISubTypeSerializer<string>.WriteSubType)))
                    {
                        serializer.EmitWrite(ctx, ctx.InputValue);
                        ctx.Return();
                    }

                    il = CompilerContextScope.Implement(type, serType, nameof(ISubTypeSerializer<string>.ReadSubType));
                    using (var ctx = new CompilerContext(scope, il, false, CompilerContext.SignatureType.ReaderScope_Input, this,
                        typeof(SubTypeState<>).MakeGenericType(runtimeType),
                        nameof(ISubTypeSerializer<string>.ReadSubType)))
                    {
                        serializer.EmitRead(ctx, ctx.InputValue);
                        // note that EmitRead will unwrap the T for us on the stack
                        ctx.Return();
                    }
                }

                // if we're constructor skipping, provide a factory for that
                if (serializer.ShouldEmitCreateInstance)
                {
                    serType = typeof(IFactory<>).MakeGenericType(runtimeType);
                    type.AddInterfaceImplementation(serType);

                    il = CompilerContextScope.Implement(type, serType, nameof(IFactory<string>.Create));
                    using var ctx = new CompilerContext(scope, il, false, CompilerContext.SignatureType.Context, this,
                         typeof(ISerializationContext), nameof(IFactory<string>.Create));
                    serializer.EmitCreateInstance(ctx, false);
                    ctx.Return();
                }
            }
        }

        private TypeBuilder WriteBasicTypeModel(string typeName, ModuleBuilder module,
            Type baseType, bool @internal)
        {
            TypeAttributes typeAttributes = (baseType.Attributes & ~TypeAttributes.Abstract) | TypeAttributes.Sealed;
            if (@internal) typeAttributes &= ~TypeAttributes.Public;

            return module.DefineType(typeName, typeAttributes, baseType);
        }

        private void WriteAssemblyAttributes(CompilerOptions options, string assemblyName, AssemblyBuilder asm)
        {
            if (!string.IsNullOrEmpty(options.TargetFrameworkName))
            {
                // get [TargetFramework] from mscorlib/equivalent and burn into the new assembly
                Type versionAttribType = null;
                try
                { // this is best-endeavours only
                    versionAttribType = TypeModel.ResolveKnownType("System.Runtime.Versioning.TargetFrameworkAttribute", typeof(string).Assembly);
                }
                catch { /* don't stress */ }
                if (versionAttribType != null)
                {
                    PropertyInfo[] props;
                    object[] propValues;
                    if (string.IsNullOrEmpty(options.TargetFrameworkDisplayName))
                    {
                        props = Array.Empty<PropertyInfo>();
                        propValues = Array.Empty<object>();
                    }
                    else
                    {
                        props = new PropertyInfo[1] { versionAttribType.GetProperty("FrameworkDisplayName") };
                        propValues = new object[1] { options.TargetFrameworkDisplayName };
                    }
                    CustomAttributeBuilder builder = new CustomAttributeBuilder(
                        versionAttribType.GetConstructor(new Type[] { typeof(string) }),
                        new object[] { options.TargetFrameworkName },
                        props,
                        propValues);
                    asm.SetCustomAttribute(builder);
                }
            }

            // copy assembly:InternalsVisibleTo
            Type internalsVisibleToAttribType = null;

            try
            {
                internalsVisibleToAttribType = typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute);
            }
            catch { /* best endeavors only */ }

            if (internalsVisibleToAttribType != null)
            {
                List<string> internalAssemblies = new List<string>();
                List<Assembly> consideredAssemblies = new List<Assembly>();
                foreach (MetaType metaType in types)
                {
                    Assembly assembly = metaType.Type.Assembly;
                    if (consideredAssemblies.IndexOf(assembly) >= 0) continue;
                    consideredAssemblies.Add(assembly);

                    AttributeMap[] assemblyAttribsMap = AttributeMap.Create(assembly);
                    for (int i = 0; i < assemblyAttribsMap.Length; i++)
                    {
                        if (assemblyAttribsMap[i].AttributeType != internalsVisibleToAttribType) continue;

                        assemblyAttribsMap[i].TryGet("AssemblyName", out var privelegedAssemblyObj);
                        string privelegedAssemblyName = privelegedAssemblyObj as string;
                        if (privelegedAssemblyName == assemblyName || string.IsNullOrEmpty(privelegedAssemblyName)) continue; // ignore

                        if (internalAssemblies.IndexOf(privelegedAssemblyName) >= 0) continue; // seen it before
                        internalAssemblies.Add(privelegedAssemblyName);

                        CustomAttributeBuilder builder = new CustomAttributeBuilder(
                            internalsVisibleToAttribType.GetConstructor(new Type[] { typeof(string) }),
                            new object[] { privelegedAssemblyName });
                        asm.SetCustomAttribute(builder);
                    }
                }
            }
        }

        // note that this is used by some of the unit tests
        internal bool IsPrepared(Type type)
        {
            MetaType meta = FindWithoutAdd(type);
            return meta != null && meta.IsPrepared();
        }

        private int metadataTimeoutMilliseconds = 5000;
        /// <summary>
        /// The amount of time to wait if there are concurrent metadata access operations
        /// </summary>
        public int MetadataTimeoutMilliseconds
        {
            get { return metadataTimeoutMilliseconds; }
            set
            {
                if (value <= 0) throw new ArgumentOutOfRangeException("MetadataTimeoutMilliseconds");
                metadataTimeoutMilliseconds = value;
            }
        }

#if DEBUG
        private int lockCount;
        /// <summary>
        /// Gets how many times a model lock was taken
        /// </summary>
        public int LockCount { get { return lockCount; } }
#endif
        internal void TakeLock(ref int opaqueToken)
        {
            const string message = "Timeout while inspecting metadata; this may indicate a deadlock. This can often be avoided by preparing necessary serializers during application initialization, rather than allowing multiple threads to perform the initial metadata inspection; please also see the LockContended event";
            opaqueToken = 0;
            if (Monitor.TryEnter(types, metadataTimeoutMilliseconds))
            {
                opaqueToken = GetContention(); // just fetch current value (starts at 1)
            }
            else
            {
                AddContention();

                throw new TimeoutException(message);
            }

#if DEBUG // note that here, through all code-paths: we have the lock
            lockCount++;
#endif
        }

        private int contentionCounter = 1;

        private int GetContention()
        {
            return Interlocked.CompareExchange(ref contentionCounter, 0, 0);
        }
        private void AddContention()
        {
            Interlocked.Increment(ref contentionCounter);
        }

        internal void ReleaseLock(int opaqueToken)
        {
            if (opaqueToken != 0)
            {
                Monitor.Exit(types);
                if (opaqueToken != GetContention()) // contention-count changes since we looked!
                {
                    LockContentedEventHandler handler = LockContended;
                    if (handler != null)
                    {
                        // not hugely elegant, but this is such a far-corner-case that it doesn't need to be slick - I'll settle for cross-platform
                        string stackTrace;
                        try
                        {
                            throw new ProtoException();
                        }
                        catch (Exception ex)
                        {
                            stackTrace = ex.StackTrace;
                        }

                        handler(this, new LockContentedEventArgs(stackTrace));
                    }
                }
            }
        }

#pragma warning disable RCS1159 // Use EventHandler<T>.
        /// <summary>
        /// If a lock-contention is detected, this event signals the *owner* of the lock responsible for the blockage, indicating
        /// what caused the problem; this is only raised if the lock-owning code successfully completes.
        /// </summary>
        public event LockContentedEventHandler LockContended;
#pragma warning restore RCS1159 // Use EventHandler<T>.

        internal string GetSchemaTypeName(HashSet<Type> callstack, Type effectiveType, DataFormat dataFormat, bool asReference, bool dynamicType, ref CommonImports imports)
            => GetSchemaTypeName(callstack, effectiveType, dataFormat, asReference, dynamicType, ref imports, out _);
        internal string GetSchemaTypeName(HashSet<Type> callstack, Type effectiveType, DataFormat dataFormat, bool asReference, bool dynamicType, ref CommonImports imports, out string altName)
        {
            altName = null;
            effectiveType = DynamicStub.GetEffectiveType(effectiveType);

            if (effectiveType == typeof(byte[])) return "bytes";

            IRuntimeProtoSerializerNode ser = ValueMember.TryGetCoreSerializer(this, dataFormat, effectiveType, out var _, false, false, false, false);
            if (ser == null)
            {   // model type
                if (asReference || dynamicType)
                {
                    imports |= CommonImports.Bcl;
                    return ".bcl.NetObjectProxy";
                }

                var mt = this[effectiveType];

                var actual = mt.GetSurrogateOrBaseOrSelf(true).GetSchemaTypeName(callstack);
                if (mt.Type.IsEnum && !mt.IsValidEnum())
                {
                    altName = actual;
                    actual = GetSchemaTypeName(callstack, Enum.GetUnderlyingType(mt.Type), dataFormat, asReference, dynamicType, ref imports);
                }
                return actual;
            }
            else
            {
                if (ser is ParseableSerializer)
                {
                    if (asReference) imports |= CommonImports.Bcl;
                    return asReference ? ".bcl.NetObjectProxy" : "string";
                }

                switch (Helpers.GetTypeCode(effectiveType))
                {
                    case ProtoTypeCode.Boolean: return "bool";
                    case ProtoTypeCode.Single: return "float";
                    case ProtoTypeCode.Double: return "double";
                    case ProtoTypeCode.String:
                        if (asReference) imports |= CommonImports.Bcl;
                        return asReference ? ".bcl.NetObjectProxy" : "string";
                    case ProtoTypeCode.Byte:
                    case ProtoTypeCode.Char:
                    case ProtoTypeCode.UInt16:
                    case ProtoTypeCode.UInt32:
                        return dataFormat switch
                        {
                            DataFormat.FixedSize => "fixed32",
                            _ => "uint32",
                        };
                    case ProtoTypeCode.SByte:
                    case ProtoTypeCode.Int16:
                    case ProtoTypeCode.Int32:
                        return dataFormat switch
                        {
                            DataFormat.ZigZag => "sint32",
                            DataFormat.FixedSize => "sfixed32",
                            _ => "int32",
                        };
                    case ProtoTypeCode.UInt64:
                        return dataFormat switch
                        {
                            DataFormat.FixedSize => "fixed64",
                            _ => "uint64",
                        };
                    case ProtoTypeCode.Int64:
                        return dataFormat switch
                        {
                            DataFormat.ZigZag => "sint64",
                            DataFormat.FixedSize => "sfixed64",
                            _ => "int64",
                        };
                    case ProtoTypeCode.DateTime:
                        switch (dataFormat)
                        {
                            case DataFormat.FixedSize: return "sint64";
                            case DataFormat.WellKnown:
                                imports |= CommonImports.Timestamp;
                                return ".google.protobuf.Timestamp";
                            default:
                                imports |= CommonImports.Bcl;
                                return ".bcl.DateTime";
                        }
                    case ProtoTypeCode.TimeSpan:
                        switch (dataFormat)
                        {
                            case DataFormat.FixedSize: return "sint64";
                            case DataFormat.WellKnown:
                                imports |= CommonImports.Duration;
                                return ".google.protobuf.Duration";
                            default:
                                imports |= CommonImports.Bcl;
                                return ".bcl.TimeSpan";
                        }
                    case ProtoTypeCode.Decimal: imports |= CommonImports.Bcl; return ".bcl.Decimal";
                    case ProtoTypeCode.Guid: imports |= CommonImports.Bcl; return ".bcl.Guid";
                    case ProtoTypeCode.Type: return "string";
                    default: throw new NotSupportedException("No .proto map found for: " + effectiveType.FullName);
                }
            }
        }

        /// <summary>
        /// Designate a factory-method to use to create instances of any type; note that this only affect types seen by the serializer *after* setting the factory.
        /// </summary>
        public void SetDefaultFactory(MethodInfo methodInfo)
        {
            VerifyFactory(methodInfo, null);
            defaultFactory = methodInfo;
        }
        private MethodInfo defaultFactory;

        internal void VerifyFactory(MethodInfo factory, Type type)
        {
            if (factory != null)
            {
                if (type != null && type.IsValueType) throw new InvalidOperationException();
                if (!factory.IsStatic) throw new ArgumentException("A factory-method must be static", nameof(factory));
                if (type != null && factory.ReturnType != type && factory.ReturnType != typeof(object)) throw new ArgumentException("The factory-method must return object" + (type == null ? "" : (" or " + type.FullName)), nameof(factory));

                if (!CallbackSet.CheckCallbackParameters(factory)) throw new ArgumentException("Invalid factory signature in " + factory.DeclaringType.FullName + "." + factory.Name, nameof(factory));
            }
        }

        /// <summary>
        /// Creates a new runtime model, to which the caller
        /// can add support for a range of types. A model
        /// can be used "as is", or can be compiled for
        /// optimal performance.
        /// </summary>
        /// <param name="name">The logical name of this model</param>
        public static RuntimeTypeModel Create([CallerMemberName] string name = null)
        {
            return new RuntimeTypeModel(false, name);
        }

        private readonly string _name;

        /// <summary>
        /// Create a model that serializes all types from an
        /// assembly specified by type
        /// </summary>
        public static new TypeModel CreateForAssembly<T>()
            => CreateForAssembly(typeof(T).Assembly);

        /// <summary>
        /// Create a model that serializes all types from an
        /// assembly specified by type
        /// </summary>
        public static new TypeModel CreateForAssembly(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return CreateForAssembly(type.Assembly);
        }

        /// <summary>
        /// Create a model that serializes all types from an assembly
        /// </summary>
        public static new TypeModel CreateForAssembly(Assembly assembly)
            => CreateForAssembly(assembly, null);

        /// <summary>
        /// Create a model that serializes all types from an assembly
        /// </summary>
        public static TypeModel CreateForAssembly(Assembly assembly, CompilerOptions options)
        {
            if (assembly == null) ThrowHelper.ThrowArgumentNullException(nameof(assembly));
            if (options == null)
            {
                var obj = (TypeModel)s_assemblyModels[assembly];
                if (obj != null) return obj;
            }
            return CreateForAssemblyImpl(assembly, options);
        }

        private readonly static Hashtable s_assemblyModels = new Hashtable();

        internal static bool IsFullyPublic(Type type) => IsFullyPublic(type, out _);

        internal static bool IsFullyPublic(Type type, out Type cause)
        {
            Type originalType = type;
            while (type != null)
            {
                if (type.IsGenericType)
                {
                    var args = type.GetGenericArguments();
                    foreach(var arg in args)
                    {
                        if (!IsFullyPublic(arg))
                        {
                            cause = arg;
                            return false;
                        }
                    }
                }
                cause = type;
                if (type.IsNestedPublic)
                {
                    type = type.DeclaringType;
                }
                else
                {
                    return type.IsPublic;
                }
            }
            cause = originalType;
            return false;
        }

        private static TypeModel CreateForAssemblyImpl(Assembly assembly, CompilerOptions options)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            lock (assembly)
            {
                var found = (TypeModel)s_assemblyModels[assembly];
                if (found != null) return found;

                RuntimeTypeModel model = null;
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsGenericTypeDefinition) continue;
                    if (!IsFullyPublic(type)) continue;
                    if (!type.IsDefined(typeof(ProtoContractAttribute), true)) continue;

                    if (options != null && !options.OnIncludeType(type)) continue;

                    (model ?? (model = Create())).Add(type, true);
                }
                if (model == null)
                    throw new InvalidOperationException($"No types marked [ProtoContract] found in assembly '{assembly.GetName().Name}'");
                var compiled = model.Compile(options);
                s_assemblyModels[assembly] = compiled;
                return compiled;
            }
        }
    }

    /// <summary>
    /// Contains the stack-trace of the owning code when a lock-contention scenario is detected
    /// </summary>
    public sealed class LockContentedEventArgs : EventArgs
    {
        internal LockContentedEventArgs(string ownerStackTrace)
        {
            OwnerStackTrace = ownerStackTrace;
        }

        /// <summary>
        /// The stack-trace of the code that owned the lock when a lock-contention scenario occurred
        /// </summary>
        public string OwnerStackTrace { get; }
    }
    /// <summary>
    /// Event-type that is raised when a lock-contention scenario is detected
    /// </summary>
    public delegate void LockContentedEventHandler(object sender, LockContentedEventArgs args);
}