using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
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
        internal static bool TryDeserializeRoot(Type type, TypeModel? model, ref ProtoReader.State state, ref object? value, bool autoCreate)
            => Get(type).TryDeserializeRoot(model, ref state, ref value, autoCreate);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TrySerializeRoot(Type type, TypeModel? model, ref ProtoWriter.State state, object value)
        {
            do
            {
                if (Get(type).TrySerializeRoot(model, ref state, value))
                {
                    return true;
                }
                // since we might be ignoring sub-types, we need to walk upwards and check all
                type = type.BaseType;
            } while (type is not null && type != typeof(object));
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeserialize(ObjectScope scope, Type type, TypeModel? model, ref ProtoReader.State state, ref object value)
            => Get(type).TryDeserialize(scope, model, ref state, ref value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TrySerializeAny(int fieldNumber, SerializerFeatures features, Type type, TypeModel? model, ref ProtoWriter.State state, object value)
        {
            do
            {
                if (Get(type).TrySerializeAny(fieldNumber, features, model, ref state, value))
                {
                    return true;
                }
                // since we might be ignoring sub-types, we need to walk upwards and check all
                type = type.BaseType;
            } while (type is not null && type != typeof(object));
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeepClone(Type type, TypeModel? model, ref object value)
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
            while (type is not null && type != typeof(object));
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsKnownType(Type type, TypeModel? model, CompatibilityLevel ambient)
            => Get(type).IsKnownType(model, ambient);

        internal static bool CanSerialize(Type type, TypeModel? model, out SerializerFeatures features)
            => Get(type).CanSerialize(model, out features);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DynamicStub Get(Type type) => (DynamicStub)s_byType[type] ?? SlowGet(type);

        /// <summary>
        /// Pre-seed the stub for a known type, from a call site that names it concretely.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the AOT fix, and the whole point is the CALL SITE.</b> <see cref="SlowGet"/>
        /// reaches <c>ConcreteStub&lt;T&gt;</c> through <c>MakeGenericType</c>, which ILC cannot
        /// generate for an instantiation nothing names statically - and <c>TryCreateConcrete</c>
        /// <i>catches</i> that failure and hands back a <c>NilStub</c>, so the non-generic,
        /// <see cref="Type"/>-based entry points degrade to "Type is not expected, and no contract
        /// can be inferred" rather than failing loudly. Calling this with a concrete
        /// <typeparamref name="T"/> makes the instantiation statically reachable, so ILC generates
        /// it and <c>MakeGenericType</c> is never needed.
        /// </para>
        /// <para>
        /// Idempotent, and locked on the same object the miss path writes under -
        /// <see cref="Hashtable"/> tolerates concurrent readers against a single writer, which is
        /// what the lock-on-write-only pattern here relies on.
        /// </para>
        /// </remarks>
        // NOTE no [DynamicallyAccessedMembers] here, deliberately: ConcreteStub<T> declares none
        // either, so demanding one would keep every registered contract fully reflectable for no
        // reason - which is precisely the mistake that cost 808 KB when it was made on the
        // transport type parameters (see AGENTS.md, "which axis they belong on")
        internal static void Register<T>() => Seed(typeof(T), new ConcreteStub<T>());

        /// <summary>Caches a stub against its type and hands it back.</summary>
        /// <remarks>
        /// Locked on write only: <see cref="Hashtable"/> tolerates concurrent readers against a
        /// single writer, which is what the lock-free <c>Get</c> relies on.
        /// </remarks>
        private static DynamicStub Seed(Type type, DynamicStub stub)
        {
            lock (s_byType)
            {
                s_byType[type] = stub;
            }
            return stub;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DynamicStub? SlowGet(Type type)
        {
            
            if (type is null) return NilStub.Instance;

#if PLAT_DYNAMIC_ACCESS_ATTR
            // gap B48: the rest of this method is the REFLECTIVE route to a stub, and under native
            // AOT it cannot work - MakeGenericType has no instantiation to find, ILC cannot see one
            // coming, and TryCreateConcrete quietly catches the failure and yields NilStub anyway.
            // So the arm is not merely unhelpful there, it is unreachable-in-effect; gating it lets
            // ILC substitute a constant and delete it BEFORE trim analysis, which is what removes
            // the demand rather than relocating it (see TypeModel.ResolveSerializer).
            //
            // Returning NilStub is EXACTLY today's AOT behaviour, and deliberately not a throw:
            // Get(type) legitimately answers NilStub in normal control flow - typeof(object) is
            // seeded to it, and TrySerializeRoot walks up base types expecting misses - so throwing
            // here would break working code. The caller's "Type is not expected, and no contract
            // can be inferred" is the error, and it is already reached.
            //
            // What a generated model needs instead is TypeModel.RegisterRootType<T>(), which seeds
            // the stub from a call site naming T concretely (gap B40).
            if (!RuntimeFeature.IsDynamicCodeSupported) return Seed(type, NilStub.Instance);
#endif
            
            DynamicStub? obj = null;
            Type? alt = null;
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
                if (alt is not null && alt != type) obj = Get(alt);
                obj ??= TryCreateConcrete(typeof(ConcreteStub<>), type);
            }
            return Seed(type, obj);

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
                string? fullName = type.FullName;
                if (fullName is not null && fullName.StartsWith("System.Data.Entity.DynamicProxies."))
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

        protected abstract bool TryDeserializeRoot(TypeModel? model, ref ProtoReader.State state, ref object value, bool autoCreate);
        protected abstract bool TryDeserialize(ObjectScope scope, TypeModel? model, ref ProtoReader.State state, ref object value);

        protected abstract bool TrySerializeRoot(TypeModel? model, ref ProtoWriter.State state, object value);
        protected abstract bool TrySerializeAny(int fieldNumber, SerializerFeatures features, TypeModel? model, ref ProtoWriter.State state, object value);

        protected abstract bool TryDeepClone(TypeModel? model, ref object value);

        protected abstract bool IsKnownType(TypeModel? model, CompatibilityLevel ambient);

        protected abstract bool CanSerialize(TypeModel? model, out SerializerFeatures features);

        private class NilStub : DynamicStub
        {
            protected NilStub() { }
            public static readonly NilStub Instance = new NilStub();

            protected override bool TryDeserializeRoot(TypeModel? model, ref ProtoReader.State state, ref object value, bool autoCreate)
                => false;
            protected override bool TryDeserialize(ObjectScope scope, TypeModel? model, ref ProtoReader.State state, ref object value)
                => false;
            protected override bool TrySerializeRoot(TypeModel? model, ref ProtoWriter.State state, object value)
                => false;
            protected override bool TrySerializeAny(int fieldNumber, SerializerFeatures features, TypeModel? model, ref ProtoWriter.State state, object value)
                => false;

            protected override bool TryDeepClone(TypeModel? model, ref object value)
                => false;

            protected override bool IsKnownType(TypeModel? model, CompatibilityLevel ambient)
                => false;

            protected override bool CanSerialize(TypeModel? model, out SerializerFeatures features)
            {
                features = default;
                return false;
            }

            protected override Type? GetEffectiveType() => null;
        }

        private sealed class ConcreteStub<T> : DynamicStub
        {
            protected override Type GetEffectiveType() => typeof(T);
            // gap B48: SerializeRoot<T>/DeserializeRoot<T> annotate T because their serializer
            // argument is OPTIONAL - they resolve one when it is omitted. Every call below has
            // already resolved it and returned false if it could not, so that fallback is
            // unreachable from here, and the demand it carries is not ours to satisfy.
            [UnconditionalSuppressMessage("Trimming", "IL2091",
                Justification = "The serializer is always supplied, so the annotated resolution inside SerializeRoot/DeserializeRoot is not reached; see gap B48.")]
            protected override bool TryDeserializeRoot(TypeModel? model, ref ProtoReader.State state, ref object value, bool autoCreate)
            {
                var serializer = TypeModel.TryResolveSerializer<T>(model);
                if (serializer is null) return false;
                // note FromObject is non-trivial; for value-type T it promotes the null to a default; we might not want that,
                // depending on the value of autoCreate

                bool resetToNullIfNotMoved = !autoCreate && value is null;
                var oldPos = state.GetPosition();
                value = state.DeserializeRoot<T>(TypeHelper<T>.FromObject(value), serializer);
                if (resetToNullIfNotMoved && oldPos == state.GetPosition()) value = null;
                return true;
            }
            // gap B48: SerializeRoot<T>/DeserializeRoot<T> annotate T because their serializer
            // argument is OPTIONAL - they resolve one when it is omitted. Every call below has
            // already resolved it and returned false if it could not, so that fallback is
            // unreachable from here, and the demand it carries is not ours to satisfy.
            [UnconditionalSuppressMessage("Trimming", "IL2091",
                Justification = "The serializer is always supplied, so the annotated resolution inside SerializeRoot/DeserializeRoot is not reached; see gap B48.")]
            protected override bool TryDeserialize(ObjectScope scope, TypeModel? model, ref ProtoReader.State state, ref object value)
            {
                var serializer = TypeModel.TryResolveSerializer<T>(model);
                if (serializer is null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                T? typed = TypeHelper<T>.FromObject(value);
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
            protected override bool IsKnownType(TypeModel? model, CompatibilityLevel ambient) => model is not null && model.IsKnownType<T>(ambient);

            protected override bool CanSerialize(TypeModel? model, out SerializerFeatures features)
            {
                ISerializer<T> ser;
                try
                {
                    ser = TypeModel.TryResolveSerializer<T>(model);
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

            // gap B48: SerializeRoot<T>/DeserializeRoot<T> annotate T because their serializer
            // argument is OPTIONAL - they resolve one when it is omitted. Every call below has
            // already resolved it and returned false if it could not, so that fallback is
            // unreachable from here, and the demand it carries is not ours to satisfy.
            [UnconditionalSuppressMessage("Trimming", "IL2091",
                Justification = "The serializer is always supplied, so the annotated resolution inside SerializeRoot/DeserializeRoot is not reached; see gap B48.")]
            protected override bool TrySerializeRoot(TypeModel? model, ref ProtoWriter.State state, object value)
            {
                var serializer = TypeModel.TryResolveSerializer<T>(model);
                if (serializer is null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                state.SerializeRoot<T>(TypeHelper<T>.FromObject(value), serializer);
                return true;
            }

            // gap B48: SerializeRoot<T>/DeserializeRoot<T> annotate T because their serializer
            // argument is OPTIONAL - they resolve one when it is omitted. Every call below has
            // already resolved it and returned false if it could not, so that fallback is
            // unreachable from here, and the demand it carries is not ours to satisfy.
            [UnconditionalSuppressMessage("Trimming", "IL2091",
                Justification = "The serializer is always supplied, so the annotated resolution inside SerializeRoot/DeserializeRoot is not reached; see gap B48.")]
            protected override bool TrySerializeAny(int fieldNumber, SerializerFeatures features, TypeModel? model, ref ProtoWriter.State state, object value)
            {
                var serializer = TypeModel.TryResolveSerializer<T>(model);
                if (serializer is null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                T? typed = TypeHelper<T>.FromObject(value);
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

            protected override bool TryDeepClone(TypeModel? model, ref object value)
            {
                // check feasability first (required because of sub-type skipping)
                if (TypeModel.TryResolveSerializer<T>(model) is null) return false;

                value = model.DeepClone<T>(TypeHelper<T>.FromObject(value));
                return true;
            }
        }

        internal static bool IsTypeEquivalent(Type expected, Type actual)
            => ReferenceEquals(expected, actual) // since SlowGet checks for proxies etc, we can
            || ReferenceEquals(Get(expected), Get(actual)); // just compare the results

        internal static Type? GetEffectiveType(Type type)
            => type is null ? null : Get(type).GetEffectiveType() ?? type;

        /// <summary>The effective type this stub stands for, or null when there is none.</summary>
        /// <remarks>Null is the NilStub answer, and <see cref="GetEffectiveType(Type)"/> coalesces it.</remarks>
        protected abstract Type? GetEffectiveType();
    }
}
