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
        internal static bool TryDeepClone(Type type, TypeModel model, ref object value)
            => Get(type).TryDeepClone(model, ref value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsKnownType(Type type, TypeModel model)
            => Get(type).IsKnownType(model);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteMessage(Type type, TypeModel model, ref ProtoWriter.State state, object value)
            => Get(type).WriteMessage(model, type, ref state, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static object ReadMessage(Type type, TypeModel model, ref ProtoReader.State state, object value)
            => Get(type).ReadMessage(model, type, ref state, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DynamicStub Get(Type type) => (DynamicStub)s_byType[type] ?? SlowGet(type);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DynamicStub SlowGet(Type type)
        {
            DynamicStub obj = NilStub.Instance;
            if (type != null)
            {
                if (TypeHelper.UseFallback(type))
                {
                    if (type.IsEnum) obj = TryCreate(typeof(EnumLookup<>), type);
                }
                else
                {
                    obj = TryCreate(typeof(ConcreteStub<>), type);
                }

                lock (s_byType)
                {
                    s_byType[type] = obj;
                }
            }
            return obj;

            static DynamicStub TryCreate(Type openGenericType, Type type)
            {
                try
                {
                    return (DynamicStub)Activator.CreateInstance(openGenericType.MakeGenericType(type), nonPublic: true);
                }
                catch
                {
                    return NilStub.Instance;
                }
            }
        }

        protected abstract bool TryDeserialize(TypeModel model, ref ProtoReader.State state, ref object value);

        protected abstract bool TrySerialize(TypeModel model, ref ProtoWriter.State state, object value);

        protected abstract bool TryDeepClone(TypeModel model, ref object value);

        protected abstract bool IsKnownType(TypeModel model);

        protected virtual void WriteMessage(TypeModel model, Type type, ref ProtoWriter.State state, object value)
            => model.Serialize(ref state, type, value);

        protected virtual object ReadMessage(TypeModel model, Type type, ref ProtoReader.State state, object value)
            => model.Deserialize(ref state, type, value);

        private class NilStub : DynamicStub
        {
            protected NilStub() { }
            public static readonly NilStub Instance = new NilStub();

            protected override bool TryDeserialize(TypeModel model, ref ProtoReader.State state, ref object value)
                => false;
            protected override bool TrySerialize(TypeModel model, ref ProtoWriter.State state, object value)
                => false;

            protected override bool TryDeepClone(TypeModel model, ref object value)
                => false;

            protected override bool IsKnownType(TypeModel model)
                => false;
        }
        private sealed class EnumLookup<T> : NilStub where T : struct
        {
            private EnumLookup() { }
            protected override bool IsKnownType(TypeModel model)
                => model.IsKnownType<T>();
        }
        private sealed class ConcreteStub<T> : DynamicStub
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ISerializer<T> GetMessageSerializer(TypeModel model)
            {
                try { return TypeModel.GetSerializer<T>(model); }
                catch { return null; }
            }
            protected override bool TryDeserialize(TypeModel model, ref ProtoReader.State state, ref object value)
            {
                var serializer = GetMessageSerializer(model);
                if (serializer == null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                value = state.Deserialize<T>(TypeHelper<T>.FromObject(value), serializer);
                return true;
            }

            protected override void WriteMessage(TypeModel model, Type type, ref ProtoWriter.State state, object value)
            {
                var serializer = GetMessageSerializer(model);
                if (serializer != null)
                {
                    serializer.Write(ref state, TypeHelper<T>.FromObject(value)); 
                }
                else
                {
                    base.WriteMessage(model, type, ref state, value);
                }
            }

            protected override object ReadMessage(TypeModel model, Type type, ref ProtoReader.State state, object value)
            {
                var serializer = GetMessageSerializer(model);
                if (serializer != null)
                {
                    return serializer.Read(ref state, TypeHelper<T>.FromObject(value));
                }
                else
                {
                    return base.ReadMessage(model, type, ref state, value);
                }
            }

            protected override bool IsKnownType(TypeModel model) => model.IsKnownType<T>();

            protected override bool TrySerialize(TypeModel model, ref ProtoWriter.State state, object value)
            {
                var serializer = GetMessageSerializer(model);
                if (serializer == null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                state.Serialize<T>(TypeHelper<T>.FromObject(value), serializer);
                return true;
            }

            protected override bool TryDeepClone(TypeModel model, ref object value)
            {
                value = model.DeepClone<T>(TypeHelper<T>.FromObject(value));
                return true;
            }
        }
    }
}
