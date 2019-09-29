using ProtoBuf.Meta;
using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Internal
{
    internal enum ObjectScope
    {
        Invalid,
        Message,
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
            => Get(type).TrySerializeRoot(model, ref state, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeserialize(ObjectScope scope, Type type, TypeModel model, ref ProtoReader.State state, ref object value)
            => Get(type).TryDeserialize(scope, model, ref state, ref value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TrySerialize(ObjectScope scope, Type type, TypeModel model, ref ProtoWriter.State state, object value)
            => Get(type).TrySerialize(scope, model, ref state, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeepClone(Type type, TypeModel model, ref object value)
            => Get(type).TryDeepClone(model, ref value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsKnownType(Type type, TypeModel model)
            => Get(type).IsKnownType(model);

        internal static ObjectScope CanSerialize(Type type, TypeModel model, out WireType defaultWireType)
            => Get(type).CanSerialize(model, out defaultWireType);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DynamicStub Get(Type type) => (DynamicStub)s_byType[type] ?? SlowGet(type);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DynamicStub SlowGet(Type type)
        {
            
            if (type == null) return NilStub.Instance;
            
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
            if (obj == null)
            {
                if (alt != null && alt != type) obj = Get(alt);
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
                if (type == null) return null;
                if (type.IsGenericParameter) return null;

                // EF POCO
                string fullName = type.FullName;
                if (fullName != null && fullName.StartsWith("System.Data.Entity.DynamicProxies."))
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
        protected abstract bool TrySerialize(ObjectScope scope, TypeModel model, ref ProtoWriter.State state, object value);

        protected abstract bool TryDeepClone(TypeModel model, ref object value);

        protected abstract bool IsKnownType(TypeModel model);

        protected abstract ObjectScope CanSerialize(TypeModel model, out WireType scalarWireType);

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
            protected override bool TrySerialize(ObjectScope scope, TypeModel model, ref ProtoWriter.State state, object value)
                => false;

            protected override bool TryDeepClone(TypeModel model, ref object value)
                => false;

            protected override bool IsKnownType(TypeModel model)
                => false;

            protected override ObjectScope CanSerialize(TypeModel model, out WireType scalarWireType)
            {
                scalarWireType = default;
                return ObjectScope.Invalid;
            }

            protected override Type GetEffectiveType() => null;
        }

        private sealed class ConcreteStub<T> : DynamicStub
        {
            protected override Type GetEffectiveType() => typeof(T);
            protected override bool TryDeserializeRoot(TypeModel model, ref ProtoReader.State state, ref object value, bool autoCreate)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer == null) return false;
                // note FromObject is non-trivial; for value-type T it promotes the null to a default; we might not want that,
                // depending on the value of autoCreate

                bool resetToNullIfNotMoved = !autoCreate && value == null;
                var oldPos = state.GetPosition();
                value = state.DeserializeRoot<T>(TypeHelper<T>.FromObject(value), serializer);
                if (resetToNullIfNotMoved && oldPos == state.GetPosition()) value = null;
                return true;
            }
            protected override bool TryDeserialize(ObjectScope scope, TypeModel model, ref ProtoReader.State state, ref object value)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer == null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                T typed = TypeHelper<T>.FromObject(value);
                switch(scope)
                {
                    case ObjectScope.LikeRoot:
                        typed = state.ReadAsObject<T>(typed, serializer);
                        break;
                    case ObjectScope.Scalar:
                    case ObjectScope.Message:
                        typed = serializer.Read(ref state, typed);
                        break;
                    case ObjectScope.WrappedMessage:
                        typed = state.ReadMessage<T>(typed, serializer);
                        break;
                    default:
                        return false;
                }
                value = typed;
                return true;
            }

            // note: in IsKnownType and CanSerialize we want to avoid asking for the serializer from
            // the model unless we actually need it, as that can cause re-entrancy loops
            protected override bool IsKnownType(TypeModel model) => model != null && model.IsKnownType<T>();

            protected override ObjectScope CanSerialize(TypeModel model, out WireType defaultWireType)
            {
                var ser = IsKnownType(model) ? model.GetSerializer<T>() : TypeModel.TryGetSerializer<T>(null);
                if (ser == null)
                {
                    defaultWireType = default;
                    return ObjectScope.Invalid;
                }
                defaultWireType = ser.DefaultWireType;
                if (ser is IScalarSerializer<T>)
                {
                    if (ser is IWrappedSerializer<T>) return ObjectScope.Invalid; // can't be both!
                    return ObjectScope.Scalar;
                }
                else if (ser is IWrappedSerializer<T>) return ObjectScope.LikeRoot;
                return ObjectScope.Message;
            }

            protected override bool TrySerializeRoot(TypeModel model, ref ProtoWriter.State state, object value)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer == null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                state.SerializeRoot<T>(TypeHelper<T>.FromObject(value), serializer);
                return true;
            }

            protected override bool TrySerialize(ObjectScope scope, TypeModel model, ref ProtoWriter.State state, object value)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer == null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                T typed = TypeHelper<T>.FromObject(value);
                switch(scope)
                {
                    case ObjectScope.LikeRoot:
                        state.WriteAsObject<T>(typed, serializer);
                        return true;
                    case ObjectScope.Scalar:
                    case ObjectScope.Message:
                        serializer.Write(ref state, typed);
                        return true;
                    case ObjectScope.WrappedMessage:
                        state.WriteMessage(typed, serializer);
                        return true;
                    default:
                        return false;
                }
            }

            protected override bool TryDeepClone(TypeModel model, ref object value)
            {
                value = model.DeepClone<T>(TypeHelper<T>.FromObject(value));
                return true;
            }
        }

        internal static bool IsTypeEquivalent(Type expected, Type actual)
            => ReferenceEquals(expected, actual) // since SlowGet checks for proxies etc, we can
            || ReferenceEquals(Get(expected), Get(actual)); // just compare the results

        internal static Type GetEffectiveType(Type type)
            => type == null ? null : Get(type).GetEffectiveType() ?? type;

        protected abstract Type GetEffectiveType();
    }
}
