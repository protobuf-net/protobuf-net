using ProtoBuf.Meta;
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Internal
{
    // bridge between the world of Type and the world of <T>, in a way that doesn't involve constant reflection
    internal abstract class DynamicStub
    {
        private static readonly Hashtable s_byType = new Hashtable
        {
            { typeof(object), NilStub.Instance },
            { typeof(byte[]), NilStub.Instance }
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeserialize(Type type, TypeModel model, ref ProtoReader.State state, ref object value)
            => Get(type).TryDeserialize(model, ref state, ref value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TrySerialize(Type type, TypeModel model, ref ProtoWriter.State state, object value)
            => Get(type).TrySerialize(model, ref state, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeepClone(TypeModel model, Type type, ref object value)
            => Get(type).TryDeepClone(model, ref value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DynamicStub Get(Type type) => (DynamicStub)s_byType[type] ?? SlowGet(type);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DynamicStub SlowGet(Type type)
        {
            var obj = NilStub.Instance;
            if (!TypeHelper.UseFallback(type))
            {
                try
                {
                    obj = (DynamicStub)Activator.CreateInstance(typeof(ConcreteStub<>).MakeGenericType(type), nonPublic: true);
                }
                catch { }
            }
            lock (s_byType)
            {
                s_byType[type] = obj;
            }
            return obj;
        }

        protected abstract bool TryDeserialize(TypeModel model, ref ProtoReader.State state, ref object value);

        protected abstract bool TrySerialize(TypeModel model, ref ProtoWriter.State state, object value);

        protected abstract bool TryDeepClone(TypeModel model, ref object value);


        private sealed class NilStub : DynamicStub
        {
            public static DynamicStub Instance { get; } = new NilStub();
            private NilStub() { }
            protected override bool TryDeserialize(TypeModel model, ref ProtoReader.State state, ref object value)
                => false;
            protected override bool TrySerialize(TypeModel model, ref ProtoWriter.State state, object value)
                => false;

            protected override bool TryDeepClone(TypeModel model, ref object value)
                => false;
        }
        private sealed class ConcreteStub<T> : DynamicStub
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static IProtoSerializer<T> GetSerializer(TypeModel model)
            {
                try { return TypeModel.GetSerializer<T>(model); }
                catch { return null; }
            }
            protected override bool TryDeserialize(TypeModel model, ref ProtoReader.State state, ref object value)
            {
                var serializer = GetSerializer(model);
                if (serializer == null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                value = state.Deserialize<T>(value == null ? default : (T)value, serializer);
                return true;
            }

            protected override bool TrySerialize(TypeModel model, ref ProtoWriter.State state, object value)
            {
                var serializer = GetSerializer(model);
                if (serializer == null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                state.Serialize<T>(value == null ? default : (T)value, serializer);
                return true;
            }

            protected override bool TryDeepClone(TypeModel model, ref object value)
            {
                value = model.DeepClone<T>((T)value);
                return true;
            }
        }
    }
}
