using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Internal
{
    internal enum ObjectScope
    {
        Invalid, // not used
        NakedMessage,
        LikeRoot,
        WrappedMessage,
        Scalar,
    }
    // bridge between the world of Type and the world of <T>, in a way that doesn't involve constant reflection
    internal abstract class DynamicStub
    {
        
        private static readonly Hashtable s_byType = new Hashtable
        {
            { typeof(object), NilStub.Instance },
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeserializeRoot(Type type, TypeModel model, ref ProtoReader.State state, ref object value, bool autoCreate)
            => Get(type).TryDeserializeRoot(model, ref state, ref value, autoCreate);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TrySerializeRoot(Type type, TypeModel model, ref ProtoWriter.State state, object value)
        {
            do
            {
                if (Get(type).TrySerializeRoot(model, ref state, value))
                {
                    return true;
                }
                // since we might be ignoring sub-types, we need to walk upwards and check all
                type = type.BaseType;
            } while (type is object && type != typeof(object));
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeserialize(ObjectScope scope, Type type, TypeModel model, ref ProtoReader.State state, ref object value)
            => Get(type).TryDeserialize(scope, model, ref state, ref value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TrySerializeAny(int fieldNumber, SerializerFeatures features, Type type, TypeModel model, ref ProtoWriter.State state, object value)
        {
            do
            {
                if (Get(type).TrySerializeAny(fieldNumber, features, model, ref state, value))
                {
                    return true;
                }
                // since we might be ignoring sub-types, we need to walk upwards and check all
                type = type.BaseType;
            } while (type is object && type != typeof(object));
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeepClone(Type type, TypeModel model, ref object value)
        {
            do
            {
                if (Get(type).TryDeepClone(model, ref value))
                {
                    return true;
                }
                // since we might be ignoring sub-types, we need to walk upwards and check all
                type = type.BaseType;
            }
            while (type is object && type != typeof(object));
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsKnownType(Type type, TypeModel model, CompatibilityLevel ambient)
            => Get(type).IsKnownType(model, ambient);

        internal static bool CanSerialize(Type type, TypeModel model, out SerializerFeatures features)
            => Get(type).CanSerialize(model, out features);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DynamicStub Get(Type type) => (DynamicStub)s_byType[type] ?? SlowGet(type);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DynamicStub SlowGet(Type type)
        {
            
            if (type is null) return NilStub.Instance;
            
            DynamicStub obj = null;
            Type alt = null;
            if (type.IsGenericParameter)
            {
                obj = NilStub.Instance; // can't do a lot with that!
            }
            else if (type.IsValueType)
            {
                alt = Nullable.GetUnderlyingType(type);
            }
            else
            {
                alt = ResolveProxies(type);
            }

            // use indirection if possible
            if (obj is null)
            {
                if (alt is object && alt != type) obj = Get(alt);
                obj ??= TryCreateConcrete(typeof(ConcreteStub<>), type);
            }
            lock (s_byType)
            {
                s_byType[type] = obj;
            }

            return obj;

            static DynamicStub TryCreateConcrete(Type typeDef, params Type[] args)
            {
                try
                {
                    return (DynamicStub)Activator.CreateInstance(typeDef.MakeGenericType(args), nonPublic: true);
                }
                catch
                {
                    return NilStub.Instance;
                }
            }

            // Applies common proxy scenarios, resolving the actual type to consider
            static Type ResolveProxies(Type type)
            {
                if (type is null) return null;
                if (type.IsGenericParameter) return null;

                // EF POCO
                string fullName = type.FullName;
                if (fullName is object && fullName.StartsWith("System.Data.Entity.DynamicProxies."))
                {
                    return type.BaseType;
                }

                // NHibernate
                Type[] interfaces = type.GetInterfaces();
                foreach (Type t in interfaces)
                {
                    switch (t.FullName)
                    {
                        case "NHibernate.Proxy.INHibernateProxy":
                        case "NHibernate.Proxy.DynamicProxy.IProxy":
                        case "NHibernate.Intercept.IFieldInterceptorAccessor":
                            return type.BaseType;
                    }
                }
                return null;
            }
        }

        protected abstract bool TryDeserializeRoot(TypeModel model, ref ProtoReader.State state, ref object value, bool autoCreate);
        protected abstract bool TryDeserialize(ObjectScope scope, TypeModel model, ref ProtoReader.State state, ref object value);

        protected abstract bool TrySerializeRoot(TypeModel model, ref ProtoWriter.State state, object value);
        protected abstract bool TrySerializeAny(int fieldNumber, SerializerFeatures features, TypeModel model, ref ProtoWriter.State state, object value);

        protected abstract bool TryDeepClone(TypeModel model, ref object value);

        protected abstract bool IsKnownType(TypeModel model, CompatibilityLevel ambient);

        protected abstract bool CanSerialize(TypeModel model, out SerializerFeatures features);

        private class NilStub : DynamicStub
        {
            protected NilStub() { }
            public static readonly NilStub Instance = new NilStub();

            protected override bool TryDeserializeRoot(TypeModel model, ref ProtoReader.State state, ref object value, bool autoCreate)
                => false;
            protected override bool TryDeserialize(ObjectScope scope, TypeModel model, ref ProtoReader.State state, ref object value)
                => false;
            protected override bool TrySerializeRoot(TypeModel model, ref ProtoWriter.State state, object value)
                => false;
            protected override bool TrySerializeAny(int fieldNumber, SerializerFeatures features, TypeModel model, ref ProtoWriter.State state, object value)
                => false;

            protected override bool TryDeepClone(TypeModel model, ref object value)
                => false;

            protected override bool IsKnownType(TypeModel model, CompatibilityLevel ambient)
                => false;

            protected override bool CanSerialize(TypeModel model, out SerializerFeatures features)
            {
                features = default;
                return false;
            }

            protected override Type GetEffectiveType() => null;
        }

        private sealed class ConcreteStub<T> : DynamicStub
        {
            protected override Type GetEffectiveType() => typeof(T);
            protected override bool TryDeserializeRoot(TypeModel model, ref ProtoReader.State state, ref object value, bool autoCreate)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer is null) return false;
                // note FromObject is non-trivial; for value-type T it promotes the null to a default; we might not want that,
                // depending on the value of autoCreate

                bool resetToNullIfNotMoved = !autoCreate && value is null;
                var oldPos = state.GetPosition();
                value = state.DeserializeRoot<T>(TypeHelper<T>.FromObject(value), serializer);
                if (resetToNullIfNotMoved && oldPos == state.GetPosition()) value = null;
                return true;
            }
            protected override bool TryDeserialize(ObjectScope scope, TypeModel model, ref ProtoReader.State state, ref object value)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer is null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                T typed = TypeHelper<T>.FromObject(value);
                switch(scope)
                {
                    case ObjectScope.LikeRoot:
                        typed = state.ReadAsRoot<T>(typed, serializer);
                        break;
                    case ObjectScope.Scalar:
                    case ObjectScope.NakedMessage:
                        typed = serializer.Read(ref state, typed);
                        break;
                    case ObjectScope.WrappedMessage:
                        typed = state.ReadMessage<T>(default, typed, serializer);
                        break;
                    default:
                        return false;
                }
                value = typed;
                return true;
            }

            // note: in IsKnownType and CanSerialize we want to avoid asking for the serializer from
            // the model unless we actually need it, as that can cause re-entrancy loops
            protected override bool IsKnownType(TypeModel model, CompatibilityLevel ambient) => model is object && model.IsKnownType<T>(ambient);

            protected override bool CanSerialize(TypeModel model, out SerializerFeatures features)
            {
                ISerializer<T> ser;
                try
                {
                    ser = TypeModel.TryGetSerializer<T>(model);
                }
                catch // then definitely no!
                {
                    features = default;
                    return false;
                }
                if (ser is null)
                {
                    features = default;
                    return false;
                }
                features = ser.Features;
                return true;
            }

            protected override bool TrySerializeRoot(TypeModel model, ref ProtoWriter.State state, object value)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer is null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                state.SerializeRoot<T>(TypeHelper<T>.FromObject(value), serializer);
                return true;
            }

            protected override bool TrySerializeAny(int fieldNumber, SerializerFeatures features, TypeModel model, ref ProtoWriter.State state, object value)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer is null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                T typed = TypeHelper<T>.FromObject(value);
                CheckAnyAuxFlow(features, serializer);
                if ((features & SerializerFeatures.CategoryMessageWrappedAtRoot) == SerializerFeatures.CategoryMessageWrappedAtRoot)
                {
                    if (fieldNumber != TypeModel.ListItemTag) ThrowHelper.ThrowInvalidOperationException($"Special root-like wrapping is limited to field {TypeModel.ListItemTag}");
                    state.WriteAsRoot<T>(typed, serializer);
                }
                else
                {
                    state.WriteAny<T>(fieldNumber, features, typed, serializer);
                }
                return true;
            }

            static void CheckAnyAuxFlow(SerializerFeatures features, ISerializer<T> serializer)
            {
                if ((features & TypeModel.FromAux) != 0 && serializer.Features.GetCategory() == SerializerFeatures.CategoryMessageWrappedAtRoot)
                {
                    ThrowHelper.ThrowNotImplementedException($"Tell Marc: ambiguous category in an any/aux flow for {typeof(T).NormalizeName()}");
                }
            }

            protected override bool TryDeepClone(TypeModel model, ref object value)
            {
                // check feasability first (required because of sub-type skipping)
                if (TypeModel.TryGetSerializer<T>(model) is null) return false;

                value = model.DeepClone<T>(TypeHelper<T>.FromObject(value));
                return true;
            }
        }

        internal static bool IsTypeEquivalent(Type expected, Type actual)
            => ReferenceEquals(expected, actual) // since SlowGet checks for proxies etc, we can
            || ReferenceEquals(Get(expected), Get(actual)); // just compare the results

        internal static Type GetEffectiveType(Type type)
            => type is null ? null : Get(type).GetEffectiveType() ?? type;

        protected abstract Type GetEffectiveType();
    }
}
