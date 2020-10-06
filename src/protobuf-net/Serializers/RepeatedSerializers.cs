using ProtoBuf.Internal;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ProtoBuf.Serializers
{

    // not quite ready to expose this yes
    internal static partial class RepeatedSerializers
    {
        private static readonly Hashtable s_providers;

        private static readonly Hashtable s_methodsPerDeclaringType = new Hashtable(), s_knownTypes = new Hashtable();
        private static MemberInfo Resolve(Type declaringType, string methodName, Type[] targs)
        {
            targs ??= Type.EmptyTypes;
            var methods = (MethodTuple[])s_methodsPerDeclaringType[declaringType];
            if (methods is null)
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

        [StructLayout(LayoutKind.Auto)]
        private readonly struct MethodTuple
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
            Add(typeof(ImmutableList<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateImmutableList), targs));
            Add(typeof(IImmutableList<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateImmutableIList), targs));
            Add(typeof(ImmutableHashSet<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateImmutableHashSet), targs));
            Add(typeof(ImmutableSortedSet<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateImmutableSortedSet), targs));
            Add(typeof(IImmutableSet<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateImmutableISet), targs));
            Add(typeof(ImmutableQueue<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateImmutableQueue), targs));
            Add(typeof(IImmutableQueue<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateImmutableIQueue), targs));
            Add(typeof(ImmutableStack<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateImmutableStack), targs));
            Add(typeof(IImmutableStack<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateImmutableIStack), targs));

            // the concurrent set
            Add(typeof(ConcurrentDictionary<,>), (root, current, targs) => Resolve(typeof(MapSerializer), nameof(MapSerializer.CreateConcurrentDictionary), new[] { root, targs[0], targs[1] }), false);
            Add(typeof(ConcurrentBag<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateConcurrentBag), new[] { root, targs[0] }), false);
            Add(typeof(ConcurrentQueue<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateConcurrentQueue), new[] { root, targs[0] }), false);
            Add(typeof(ConcurrentStack<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateConcurrentStack), new[] { root, targs[0] }), false);
            Add(typeof(IProducerConsumerCollection<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateIProducerConsumerCollection), new[] { root, targs[0] }), false);

            // pretty normal stuff
            Add(typeof(Dictionary<,>), (root, current, targs) => Resolve(typeof(MapSerializer), nameof(MapSerializer.CreateDictionary), root == current ? targs : new[] { root, targs[0], targs[1] }), false);
            Add(typeof(IDictionary<,>), (root, current, targs) => Resolve(typeof(MapSerializer), nameof(MapSerializer.CreateDictionary), new[] { root, targs[0], targs[1] }), false);
            Add(typeof(Queue<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateQueue), new[] { root, targs[0] }), false);
            Add(typeof(Stack<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateStack), new[] { root, targs[0] }), false);

            // fallbacks, these should be at the end
            Add(typeof(IEnumerable<>), (root, current, targs) => Resolve(typeof(RepeatedSerializer), nameof(RepeatedSerializer.CreateEnumerable), new[] { root, targs[0] }), false);
        }

        public static void Add(Type type, Func<Type, Type, Type[], MemberInfo> implementation, bool exactOnly = true)
        {
            if (type is null) ThrowHelper.ThrowArgumentNullException(nameof(type));
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
            if (type is null || type == typeof(string)) return null;

            var known = (RepeatedSerializerStub)s_knownTypes[type];
            if (known is null)
            {
                Type genDef;
                if (type.IsGenericType && Array.IndexOf(NotSupportedFlavors, (genDef = type.GetGenericTypeDefinition())) >= 0)
                {
                    if (genDef == typeof(Span<>) || genDef == typeof(ReadOnlySpan<>))
                    {   // needs special handling because can't use Span<T> as a TSomething in a Foo<TSomething>
                        throw new NotSupportedException("Serialization cannot work with [ReadOnly]Span<T>; [ReadOnly]Memory<T> may be enabled later");
                    }
                    known = NotSupported(s_GeneralNotSupported, type, type.GetGenericArguments()[0]);
                }
                else
                {
                    var rawProvider = GetProviderForType(type);
                    if (rawProvider is null)
                    {
                        if (type.IsArray && type != typeof(byte[]))
                        {
                            // multi-dimensional
                            known = NotSupported(s_GeneralNotSupported, type, type.GetElementType());
                        }
                        else
                        {   // not repeated
                            known = RepeatedSerializerStub.Empty;
                        }
                    }
                    else
                    {
                        // check for nesting
                        known = RepeatedSerializerStub.Create(type, rawProvider);
                        if (TestIfNestedNotSupported(known))
                        {
                            known = NotSupported(s_NestedNotSupported, known.ForType, known.ItemType);
                        }
                    }
                }


                lock (s_knownTypes)
                {
                    s_knownTypes[type] = known;
                }
            }

            return known.IsEmpty ? null : known;
        }

        static readonly Type[] NotSupportedFlavors = new[]
        {   // see notes in /src/protobuf-net.Test/Serializers/Collections.cs for reasons and roadmap
            typeof(ArraySegment<>),
            typeof(Span<>),
            typeof(ReadOnlySpan<>),
            typeof(Memory<>),
            typeof(ReadOnlyMemory<>),
            typeof(ReadOnlySequence<>),
            typeof(IMemoryOwner<>),
        };

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static RepeatedSerializerStub NotSupported(MethodInfo kind, Type collectionType, Type itemType)
            => RepeatedSerializerStub.Create(collectionType, kind.MakeGenericMethod(collectionType, itemType));

        static readonly MethodInfo
            s_NestedNotSupported = typeof(RepeatedSerializer).GetMethod(nameof(RepeatedSerializer.CreateNestedDataNotSupported)),
            s_GeneralNotSupported = typeof(RepeatedSerializer).GetMethod(nameof(RepeatedSerializer.CreateNotSupported));

        private static bool TestIfNestedNotSupported(RepeatedSerializerStub repeated)
        {
            if (repeated?.ItemType is null) return false; // fine

            if (!repeated.IsMap) // we allow nesting on dictionaries, just not on arrays/lists etc
            {
                if (repeated.ItemType == repeated.ForType || TryGetRepeatedProvider(repeated.ItemType) is object) return true;
            }
            return false;
        }

        private static MemberInfo GetProviderForType(Type type)
        {
            if (type is null) return null;

            if (type.IsArray)
            {
                // the fun bit here is checking we mean a *vector*
                if (type == typeof(byte[])) return null; // special-case, "bytes"

                var vectorType = type.GetElementType().MakeArrayType();
                return vectorType == type ? s_Array.Resolve(type, vectorType) : null;
            }

            MemberInfo bestMatch = null;
            int bestMatchPriority = int.MaxValue;
            bool bestIsAmbiguous = false;
            void Consider(MemberInfo member, int priority)
            {
                if (priority < bestMatchPriority)
                {
                    bestMatch = member;
                    bestMatchPriority = priority;
                    bestIsAmbiguous = false;
                }
                else if (priority == bestMatchPriority)
                {
                    if (!Equals(bestMatch, member))
                        bestIsAmbiguous = true;
                }
            }

            Type current = type;
            while (current is object && current != typeof(object))
            {
                if (TryGetProvider(type, current, bestMatchPriority, out var found, out var priority)) Consider(found, priority);
                current = current.BaseType;
            }

            foreach (var iType in type.GetInterfaces())
            {
                if (TryGetProvider(type, iType, bestMatchPriority, out var found, out var priority)) Consider(found, priority);
            }

            return bestIsAmbiguous ? null : bestMatch;
        }

        private static bool TryGetProvider(Type root, Type current, int bestMatchPriority, out MemberInfo member, out int priority)
        {
            var found = (Registration)s_providers[current];
            if (found is null && current.IsGenericType)
            {
                found = (Registration)s_providers[current.GetGenericTypeDefinition()];
            }

            if (found is null
                || (found.Priority > bestMatchPriority)
                || (found.ExactOnly && root != current))
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
