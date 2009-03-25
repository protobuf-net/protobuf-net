using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using ProtoBuf.Property;
#if !SILVERLIGHT && !CF
using System.Runtime.Serialization;
#endif

namespace ProtoBuf
{
    internal static class Serializer<T> where T : class
    {
        private delegate void SerializationCallback(T instance);

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
                    if (prop.DefinedType == ProtoFormat.STRING)
                    {
                        // note; waiting on clarification over quote escape rules
                        sb.Append(" [default = \"").Append(defText).Append("\"]");
                    }
                    else
                    {
                        sb.Append(" [default = ").Append(defText).Append("]");
                    }
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
        private static KeyValuePair<Type, Property<T, T>>[] subclasses;
        private static SerializationCallback[] callbacks;
        private static readonly object lockToken = new object();
        
        internal static void Build()
        {
            if (readProps != null) return; // completely built and ready for use
#if CF // no wait option on CF
            if (!Monitor.TryEnter(lockToken))
#else // give it a few seconds on regular .NET
            if (!Monitor.TryEnter(lockToken, 5000))
#endif
            {
                throw new InvalidOperationException("Possible deadlock detected preparing serializer for " + typeof(T).Name + "; try using Serializer.PrepareSerializer to initialize this type at application startup.");
            }
            try
            {
                if (readProps != null || subclasses != null)
                {
                    // readProps != null : double-checked locking

                    // readProps == null, but subclasses != null :
                    // this scenario means that we are in the process of building the
                    // serializer; since we hold the lock, this must be re-entrancy, so simply ignore
                    //Trace.WriteLine("Re-entrant short-circuit: " + typeof(T).FullName, "ProtoBuf.Serializer:Build");
                    return; 
                }
                //Trace.WriteLine("Building: " + typeof(T).FullName, "ProtoBuf.Serializer:Build");
                // otherwise we are building the serializer for the first time; initialize
                // subclasses as a marker that we are in-progress
                subclasses = new KeyValuePair<Type, Property<T, T>>[0]; // use this to prevent recursion
                if (!Serializer.IsEntityType(typeof(T)))
                {
                    throw new InvalidOperationException("Only data-contract classes can be processed (error processing " + typeof(T).Name + ")");
                }
                BuildCallbacks();
                List<Property<T>> readPropList = new List<Property<T>>(), writePropList = new List<Property<T>>();
                List<int> tagsInUse = new List<int>();
                foreach (MemberInfo prop in Serializer.GetProtoMembers(typeof(T)))
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
                    if(tagsInUse.Contains(tag)) {
                        throw new InvalidOperationException(
                            string.Format("Duplicate tag {0} detected in {1}", tag, name));
                    }
                    tagsInUse.Add(tag);

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

#if CF2
                CrapSort(readPropList, delegate(Property<T> x, Property<T> y) { return x.FieldPrefix.CompareTo(y.FieldPrefix); });
                CrapSort(writePropList, delegate(Property<T> x, Property<T> y) { return x.FieldPrefix.CompareTo(y.FieldPrefix); });
#else
                readPropList.Sort(delegate(Property<T> x, Property<T> y) { return x.FieldPrefix.CompareTo(y.FieldPrefix); });
                writePropList.Sort(delegate(Property<T> x, Property<T> y) { return x.FieldPrefix.CompareTo(y.FieldPrefix); });
#endif
                
                List<KeyValuePair<Type, Property<T, T>>> subclassList = new List<KeyValuePair<Type, Property<T, T>>>();
                foreach (ProtoIncludeAttribute pia in Attribute.GetCustomAttributes(typeof(T), typeof(ProtoIncludeAttribute), false))
                {
                    Type subclassType = pia.KnownType;
                    if (subclassType == null)
                    {
                        throw new ProtoException("Unable to identify known-type for ProtoIncludeAttribute: " + pia.KnownTypeName);
                    }
                    if (subclassType.BaseType != typeof(T))
                    {
                        throw new ProtoException(string.Format(
                            "Known-type {0} for ProtoIncludeAttribute must be a direct subclass of {1}",
                            subclassType.Name, typeof(T).Name));
                    }
                    Property<T, T> prop;
                    switch (pia.DataFormat)
                    {
                        case DataFormat.Default:
                            prop = (Property<T, T>) PropertyUtil<T>.CreateTypedProperty("CreatePropertyMessageString", typeof(T), typeof(T), subclassType);
                            break;
                        case DataFormat.Group:
                            prop = (Property<T, T>)PropertyUtil<T>.CreateTypedProperty("CreatePropertyMessageGroup", typeof(T), typeof(T), subclassType);
                            break;
                        default:
                            throw new ProtoException("Invalid ProtoIncludeAttribute data-format: " + pia.DataFormat);
                    }
                    // check for duplicates
                    if (tagsInUse.Contains(pia.Tag))
                    {
                        throw new InvalidOperationException(
                            string.Format("Duplicate tag {0} detected in sub-type {1}", pia.Tag, subclassType.Name));
                    }
                    tagsInUse.Add(pia.Tag);
                    prop.Init(pia.Tag, pia.DataFormat, PropertyFactory.GetPassThru<T>(), null, true, null);
                    subclassList.Add(new KeyValuePair<Type, Property<T, T>>(subclassType, prop));
                }
                subclasses = subclassList.ToArray();
                writeProps = writePropList.ToArray();
                readProps = readPropList.ToArray(); // this must be last; this lets other threads see the data
            }
            catch (Exception ex)
            {
                readProps = writeProps = null;
                subclasses = null;
                Debug.WriteLine("Build() failed for type: " + typeof(T).AssemblyQualifiedName);
                Debug.WriteLine(ex);
                throw;
            }
            finally
            {
                Monitor.Exit(lockToken);
            }
        }

#if CF2
        static void CrapSort<TValue>(IList<TValue> list, Comparison<TValue> comparer)
        {
            int len = list.Count;
            for(int i = 0 ; i < len - 1; i++)
            {
                for(int j = 0 ; j < len - 1; j++)
                {
                    TValue x = list[j], y = list[j + 1];
                    if(comparer(x,y)>0)
                    { // swap
                        list[j] = y;
                        list[j + 1] = x;
                    }
                }
            }
#if DEBUG
            for(int i = 0 ; i < len - 1 ; i++)
            {
                if (comparer(list[i], list[i + 1]) > 0) throw new NotImplementedException("I messed up the sort...");
            }
#endif
        }
#endif
        internal static int SerializeChecked(T instance, SerializationContext destination)
        {
            if (readProps == null) Build();
            if (instance == null) throw new ArgumentNullException("instance");
            if (destination == null) throw new ArgumentNullException("destination");
            
            int len = Serialize(instance, destination);
            destination.CheckStackClean();
            destination.Flush();
            return len;
        }


        internal static int Serialize(T instance, SerializationContext context)
        {
            // check for inheritance; if the instance is a subclass, then we
            // should serialize the sub-type first, allowing for more efficient
            // deserialization; note that we don't push the instance onto the
            // stack yet - we'll do that within each instance (otherwise deep
            // items could incorrectly count as cyclic).
            Type actualType = instance.GetType();
            int total = 0, len;

            Callback(CallbackType.BeforeSerialization, instance);

            if (actualType != typeof(T))
            {
                bool subclassFound = false;
                foreach (KeyValuePair<Type, Property<T,T>> subclass in subclasses)
                {
                    if (subclass.Key.IsAssignableFrom(actualType))
                    {
                        total += subclass.Value.Serialize(instance, context);
                        subclassFound = true;
                        break;
                    }
                }
                if (!subclassFound)
                {
                    throw new ProtoException("Unexpected type found during serialization; types must be included with ProtoIncludeAttribute; "
                        + "found " + actualType.Name + " passed as " + typeof(T).Name);
                }
            }

            context.Push(instance);
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
            Callback(CallbackType.AfterSerialization, instance);
            return total;
        }

        internal static void DeserializeChecked<TCreation>(ref T instance, SerializationContext source)
            where TCreation : class, T
        {
            if (readProps == null) Build();
            //if (instance == null) throw new ArgumentNullException("instance");
            if (source == null) throw new ArgumentNullException("source");
            Deserialize<TCreation>(ref instance, source);
            source.CheckStackClean();
        }
        
        internal static void Deserialize<TCreation>(ref T instance, SerializationContext context)
            where TCreation : class, T
        {
            uint prefix = 0;
            if (context == null) throw new ArgumentNullException("context");
#if !CF
            try
            {
#endif
                if(instance != null)
                {
                    Callback(CallbackType.BeforeDeserialization, instance);
                }

                context.Push();
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
                    while (context.TryReadFieldPrefix(out prefix))
                    {
                        // scan for the correct property
                        bool foundTag = false;
                        if (prefix == lastPrefix)
                        {
                            foundTag = true;
                        }
                        else if (prefix > lastPrefix)
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

                        if (!foundTag)
                        {
                            // check for subclass creation
                            foreach (KeyValuePair<Type, Property<T, T>> subclass in subclasses)
                            {
                                // deserialize the nested data
                                if (prefix == subclass.Value.FieldPrefix)
                                {
                                    foundTag = true;
                                    instance = subclass.Value.DeserializeImpl(instance, context);
                                    break;
                                }
                            }
                            if (foundTag) continue; // nothing more to do for this...
                        }

                        // not a sub-class, but *some* data there, so create an object
                        if (instance == null)
                        {
                            instance = ObjectFactory<TCreation>.Create();
                            Callback(CallbackType.ObjectCreation, instance);
                            extensible = instance as IExtensible;
                        }
                        if (foundTag)
                        {
                            // found it by seeking; deserialize and continue

                            // ReSharper disable PossibleNullReferenceException
                            prop.Deserialize(instance, context);
                            // ReSharper restore PossibleNullReferenceException
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
                            // ReSharper disable PossibleNullReferenceException
                            extraData.EncodeUInt32(prefix);
                            // ReSharper restore PossibleNullReferenceException
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

                // final chance to create an instance - this only gets invoked for empty
                // messages (otherwise instance should already be non-null)
                if (instance == null)
                {
                    instance = ObjectFactory<T>.Create();
                    Callback(CallbackType.ObjectCreation, instance);
                }
                context.Pop();
                Callback(CallbackType.AfterDeserialization, instance);
#if !CF
            } catch (Exception ex)
            {
                const string ErrorDataKey = "protoSource";
                if (!ex.Data.Contains(ErrorDataKey))
                {
                    ex.Data.Add(ErrorDataKey, string.Format("tag={0}; wire-type={1}; offset={2}; depth={3}; type={4}",
                            (int) (prefix >> 3), (WireType) (prefix & 7),
                            context.Position, context.Depth, typeof (T).FullName));}
                throw;
            }
#endif
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
                    while (read.TryReadFieldPrefix(out prefix))
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
                case WireType.EndGroup:
                    throw new ProtoException("End-group not expected at this location");                
                default:
                    throw new ProtoException("Unknown wire-type " + wireType);
            }
        }
      
        internal static void CheckTagNotInUse(int tag)
        {
            if (tag <= 0) throw new ArgumentOutOfRangeException("tag", "Tags must be positive integers.");
            if (readProps == null) Build();
// ReSharper disable PossibleNullReferenceException
            foreach (Property<T> prop in readProps)
// ReSharper restore PossibleNullReferenceException
            {
                if (prop.Tag == tag) throw new ArgumentException(
                    string.Format("Tag {0} is in use; access the {1} property instead.", tag, prop.Name), "tag");
            }
        }

        internal static TValueActual CheckSubType<TValueActual>(T instance)
            where TValueActual : class, T
        {
            TValueActual actual = instance as TValueActual;
            if (actual == null && instance != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Serializer.Serialize<T>(ms, instance);
                    actual = ObjectFactory<TValueActual>.Create();
                    // (note - don't use ObjectCreation callback here - Merge will own the callbacks)
                    ms.Position = 0;
                    Serializer.Merge<T>(ms, actual);
                }
            }
            return actual;
        }
        internal static void Callback(CallbackType callbackType, T instance)
        {
            if(callbacks != null)
            {
                SerializationCallback callback;
                if ((callback = callbacks[(int)callbackType]) != null) callback(instance);
            }
        }

        private static void BuildCallbacks()
        {
            MethodInfo[] methods = typeof (T).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly);

            // protobuf-net specific
            FindCallback(methods, CallbackType.BeforeSerialization, typeof (ProtoBeforeSerializationAttribute));
            FindCallback(methods, CallbackType.AfterSerialization, typeof(ProtoAfterSerializationAttribute));
            FindCallback(methods, CallbackType.BeforeDeserialization, typeof(ProtoBeforeDeserializationAttribute));
            FindCallback(methods, CallbackType.AfterDeserialization, typeof(ProtoAfterDeserializationAttribute));

#if !SILVERLIGHT && !CF 
            // regular framework
            FindCallback(methods, CallbackType.BeforeSerialization, typeof (OnSerializingAttribute));
            FindCallback(methods, CallbackType.AfterSerialization, typeof(OnSerializedAttribute));
            FindCallback(methods, CallbackType.BeforeDeserialization, typeof(OnDeserializingAttribute));
            FindCallback(methods, CallbackType.AfterDeserialization, typeof(OnDeserializedAttribute));
#endif
            
            Type root = typeof (T).BaseType;
            bool isRoot = !Serializer.IsEntityType(root);
            if (!isRoot) {
                while (Serializer.IsEntityType(root.BaseType))
                {
                    root = root.BaseType;
                }
            }
            if(callbacks != null)
            {
                if (!isRoot)
                {
                    throw new ProtoException(
                        "Callbacks are only supported on the root contract type in an inheritance tree; consider implementing callbacks as virtual methods on " +
                        root.FullName);
                }
                // otherwise, use BeforeDeserialization for ObjectCreation:
                callbacks[(int) CallbackType.ObjectCreation] = callbacks[(int) CallbackType.BeforeDeserialization];
            } else
            {
                methods = root.GetMethods(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.DeclaredOnly);

                FindCallback(methods, CallbackType.ObjectCreation, typeof(ProtoBeforeDeserializationAttribute));
#if !SILVERLIGHT && !CF 
                FindCallback(methods, CallbackType.ObjectCreation, typeof(OnDeserializingAttribute));
#endif
            }

        }

        private static void FindCallback(MethodInfo[] methods, CallbackType callbackType, Type attributeType)
        {
            if(callbacks != null && callbacks[(int)callbackType] != null) return; // already found
            MethodInfo found = null;
            for(int i = 0 ; i < methods.Length ; i++)
            {
                if(Attribute.GetCustomAttribute(methods[i], attributeType) != null)
                {
                    if (found != null)
                    {
                        throw new ProtoException(
                            "Conflicting callback methods (decorated with " + attributeType.Name +
                            ") found for " + typeof(T).Name + ": " + found.Name + " and " + methods[i].Name);
                    }
                    found = methods[i];
                }
            }
            if(found != null)
            {
                ParameterInfo[] args = found.GetParameters();
                SerializationCallback callback;
                if(found.ReturnType == typeof(void) && args.Length == 0)
                {
#if CF2
                    callback = delegate(T instance) { found.Invoke(instance, null); };
#else
                    callback = (SerializationCallback)Delegate.CreateDelegate(
                        typeof(SerializationCallback), null, found);
#endif

                }
#if !SILVERLIGHT && !CF
                else if (found.ReturnType == typeof(void) && args.Length == 1
                    && args[0].ParameterType == typeof(StreamingContext))
                {
                    Setter<T, StreamingContext> inner = (Setter<T, StreamingContext>)
                        Delegate.CreateDelegate(typeof (Setter<T, StreamingContext>), null, found);
                    callback = delegate(T instance)
                    {
                        inner(instance, SerializationContext.EmptyStreamingContext);
                    };
                }
#endif
                else
                {
                    throw new ProtoException("Unexpected signature on callback: " + typeof(T).Name + "." + found.Name);
                }
                if(callbacks == null) callbacks = new SerializationCallback[5];
                callbacks[(int) callbackType] = callback;
            }
        }
    }
    internal enum CallbackType
    {
        BeforeSerialization = 0,
        AfterSerialization = 1,
        BeforeDeserialization = 2,
        AfterDeserialization = 3,
        ObjectCreation = 4
    }

}
