using ProtoBuf.Compiler;
using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Serializers
{
    internal class RepeatedSerializerStub
    {
        internal static readonly RepeatedSerializerStub Empty = new RepeatedSerializerStub(null, null);
        public MemberInfo Provider { get; }
        public bool IsMap { get; }
        public bool IsEmpty => Provider == null;
        public object Serializer => _serializer ?? CreateSerializer();
        public Type ForType { get; }
        public Type ItemType { get; }
        private object _serializer;
        [MethodImpl(MethodImplOptions.NoInlining)]
        private object CreateSerializer()
        {
            var provider = RuntimeTypeModel.GetUnderlyingProvider(Provider, ForType);
            _serializer = provider switch
            {
                FieldInfo field when field.IsStatic => field.GetValue(null),
                MethodInfo method when method.IsStatic => method.Invoke(null, null),
                _ => null,
            };
            return _serializer;
        }

        internal void EmitProvider(CompilerContext ctx) => EmitProvider(ctx.IL);
        private void EmitProvider(ILGenerator il)
        {
            var provider = RuntimeTypeModel.GetUnderlyingProvider(Provider, ForType);
            RuntimeTypeModel.EmitProvider(provider, il);
        }

        public static RepeatedSerializerStub Create(Type forType, MemberInfo provider)
            => provider == null ? Empty : new RepeatedSerializerStub(forType, provider);

        private RepeatedSerializerStub(Type forType, MemberInfo provider)
        {
            ForType = forType;
            Provider = provider;
            IsMap = CheckIsMap(provider, out Type itemType);
            ItemType = itemType;
        }
        private static bool CheckIsMap(MemberInfo provider, out Type itemType)
        {
            var type = provider switch
            {
                MethodInfo method => method.ReturnType,
                FieldInfo field => field.FieldType,
                PropertyInfo prop => prop.PropertyType,
                Type t => t,
                _ => null,
            };
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType)
                {
                    var genDef = type.GetGenericTypeDefinition();
                    if (genDef == typeof(MapSerializer<,,>))
                    {
                        var targs = type.GetGenericArguments();
                        itemType = typeof(KeyValuePair<,>).MakeGenericType(targs[1], targs[2]);
                        return true;
                    }
                    if (genDef == typeof(RepeatedSerializer<,>))
                    {
                        var targs = type.GetGenericArguments();
                        itemType = targs[1];
                        return false;
                    }
                }

                type = type.BaseType;
            }
            itemType = null;
            return false;
        }

        internal void ResolveMapTypes(out Type keyType, out Type valueType)
        {
            keyType = valueType = null;
            if (IsMap)
            {
                var targs = ItemType.GetGenericArguments();
                keyType = targs[0];
                valueType = targs[1];
            }
        }
    }
    // not quite ready to expose this yes
    internal static class RepeatedSerializers
    {
        private static readonly Hashtable s_providers;

        private static readonly Hashtable s_methodsPerDeclaringType = new Hashtable(), s_knownTypes = new Hashtable();
        private static MemberInfo Resolve(Type declaringType, string methodName, Type[] targs)
        {
            targs ??= Type.EmptyTypes;
            var methods = (MethodTuple[])s_methodsPerDeclaringType[declaringType];
            if (methods == null)
            {
                var declared = declaringType.GetMethods(BindingFlags.Static | BindingFlags.Public);
                methods = Array.ConvertAll(declared, m => new MethodTuple(m));
                lock (s_methodsPerDeclaringType)
                {
                    s_methodsPerDeclaringType[declaringType] = methods;
                }
            }
            foreach (var method in methods)
            {
                if (method.Name == methodName)
                {
                    if (targs.Length == method.GenericArgCount) return method.Construct(targs);
                }
            }
            return null;
        }

        readonly struct MethodTuple
        {
            public string Name => Method.Name;
            private MethodInfo Method { get; }
            public int GenericArgCount { get; }
            public MethodInfo Construct(Type[] targs)
                => GenericArgCount == 0 ? Method : Method.MakeGenericMethod(targs);
            public MethodTuple(MethodInfo method)
            {
                Method = method;
                GenericArgCount = method.IsGenericMethodDefinition
                    ? method.GetGenericArguments().Length : 0;
            }
        }

        private static readonly Registration s_Array = new Registration(0,
            (root, current, targs) => root == current ? Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateVector), targs) : null, true);

        static RepeatedSerializers()
        {
            s_providers = new Hashtable();

            // the orignal! the best! accept no substitutes!
            Add(typeof(List<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateList),
                root == current ? targs : new[] { root, targs[0] }), false);

            // note that the immutable APIs can look a lot like the non-immutable ones; need to have them with *higher* priority to ensure they get recognized correctly
            Add(typeof(ImmutableArray<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateImmutableArray), targs));
            Add(typeof(ImmutableDictionary<,>), (root, current, targs) => Resolve(typeof(MapSerializer), nameof(MapSerializer.CreateImmutableDictionary), targs));
            Add(typeof(ImmutableSortedDictionary<,>), (root, current, targs) => Resolve(typeof(MapSerializer), nameof(MapSerializer.CreateImmutableSortedDictionary), targs));
            Add(typeof(IImmutableDictionary<,>), (root, current, targs) => Resolve(typeof(MapSerializer), nameof(MapSerializer.CreateIImmutableDictionary), targs));

            // pretty normal stuff
            Add(typeof(Dictionary<,>), (root, current, targs) => Resolve(typeof(MapSerializer), nameof(MapSerializer.CreateDictionary), targs));
            Add(typeof(IDictionary<,>), (root, current, targs) => Resolve(typeof(MapSerializer), nameof(MapSerializer.CreateDictionary), new[] { root, targs[0], targs[1] }), false);
            Add(typeof(Queue<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateQueue), targs));
            Add(typeof(Stack<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateStack), targs));

            // fallbacks, these should be at the end
            Add(typeof(ICollection<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateCollection), new[] { root, targs[0] }), false);
            Add(typeof(IReadOnlyCollection<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateReadOnlyCollection), new[] { root, targs[0] }), false);
        }

        public static void Add(Type type, Func<Type, Type, Type[], MemberInfo> implementation, bool exactOnly = true)
        {
            if (type == null) ThrowHelper.ThrowArgumentNullException(nameof(type));
            lock (s_providers)
            {
                var reg = new Registration(s_providers.Count + 1, implementation, exactOnly);
                s_providers.Add(type, reg);
            }
            lock (s_knownTypes)
            {
                s_knownTypes.Clear();
            }
        }

        internal static RepeatedSerializerStub TryGetRepeatedProvider(Type type)
        {
            if (type == null) return null;

            var known = (RepeatedSerializerStub)s_knownTypes[type];
            if (known == null)
            {
                known = RepeatedSerializerStub.Create(type, GetProviderForType(type));
                lock (s_knownTypes)
                {
                    s_knownTypes[type] = known;
                }
            }

            return known.IsEmpty ? null : known;
        }

        private static MemberInfo GetProviderForType(Type type)
        {
            if (type == null) return null;

            if (type.IsArray)
            {
                // the fun bit here is checking we mean a *vector*
                if (type == typeof(byte[])) return null; // special-case, "bytes"
                return s_Array.Resolve(type, type.GetElementType().MakeArrayType());
            }

            MemberInfo bestMatch = null;
            int bestMatchPriority = int.MaxValue;
            void Consider(MemberInfo member, int priority)
            {
                if (priority < bestMatchPriority)
                {
                    bestMatch = member;
                    bestMatchPriority = priority;
                }
            }

            Type current = type;
            while (current != null && current != typeof(object))
            {
                if (TryGetProvider(type, current, out var found, out var priority)) Consider(found, priority);
                current = current.BaseType;
            }

            foreach (var iType in type.GetInterfaces())
            {
                if (TryGetProvider(type, iType, out var found, out var priority)) Consider(found, priority);
            }

            return bestMatch;
        }

        private static bool TryGetProvider(Type root, Type current, out MemberInfo member, out int priority)
        {
            var found = (Registration)s_providers[current];
            if (found == null && current.IsGenericType)
            {
                found = (Registration)s_providers[current.GetGenericTypeDefinition()];
            }

            if (found == null || (found.ExactOnly && root != current))
            {
                member = null;
                priority = default;
                return false;
            }
            member = found.Resolve(root, current);
            priority = found.Priority;
            return true;

        }

        private sealed class Registration
        {
            public MemberInfo Resolve(Type root, Type current)
            {
                Type[] targs;
                if (current.IsGenericType)
                    targs = current.GetGenericArguments();
                else if (current.IsArray)
                    targs = new[] { current.GetElementType() };
                else
                    targs = Type.EmptyTypes;

                return Implementation?.Invoke(root, current, targs);
            }
            public bool ExactOnly { get; }
            public int Priority { get; }
            private Func<Type, Type, Type[], MemberInfo> Implementation { get; }
            public Registration(int priority, Func<Type, Type, Type[], MemberInfo> implementation, bool exactOnly)
            {
                Priority = priority;
                Implementation = implementation;
                ExactOnly = exactOnly;
            }
        }
    }
}
