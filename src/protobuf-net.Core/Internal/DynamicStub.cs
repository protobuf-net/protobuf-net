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
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeserializeRoot(Type type, TypeModel model, ref ProtoReader.State state, ref object value)
            => Get(type).TryDeserializeRoot(model, ref state, ref value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TrySerializeRoot(Type type, TypeModel model, ref ProtoWriter.State state, object value)
            => Get(type).TrySerializeRoot(model, ref state, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeserializeRaw(Type type, TypeModel model, ref ProtoReader.State state, ref object value)
    => Get(type).TryDeserializeRaw(model, ref state, ref value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TrySerializeRaw(Type type, TypeModel model, ref ProtoWriter.State state, object value)
            => Get(type).TrySerializeRaw(model, ref state, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeepClone(Type type, TypeModel model, ref object value)
            => Get(type).TryDeepClone(model, ref value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsKnownType(Type type, TypeModel model)
            => Get(type).IsKnownType(model);

        internal static bool CanSerialize(Type type, TypeModel model, out bool isScalar)
            => Get(type).CanSerialize(model, out isScalar);

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
                if (!TypeHelper.UseFallback(type))
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

        protected abstract bool TryDeserializeRoot(TypeModel model, ref ProtoReader.State state, ref object value);
        protected abstract bool TryDeserializeRaw(TypeModel model, ref ProtoReader.State state, ref object value);

        protected abstract bool TrySerializeRoot(TypeModel model, ref ProtoWriter.State state, object value);
        protected abstract bool TrySerializeRaw(TypeModel model, ref ProtoWriter.State state, object value);

        protected abstract bool TryDeepClone(TypeModel model, ref object value);

        protected abstract bool IsKnownType(TypeModel model);

        protected abstract bool CanSerialize(TypeModel model, out bool isScalar);

        protected virtual void WriteMessage(TypeModel model, Type type, ref ProtoWriter.State state, object value)
            => model.Serialize(ref state, type, value);

        protected virtual object ReadMessage(TypeModel model, Type type, ref ProtoReader.State state, object value)
            => model.Deserialize(ref state, type, value);

        private class NilStub : DynamicStub
        {
            protected NilStub() { }
            public static readonly NilStub Instance = new NilStub();

            protected override bool TryDeserializeRoot(TypeModel model, ref ProtoReader.State state, ref object value)
                => false;
            protected override bool TryDeserializeRaw(TypeModel model, ref ProtoReader.State state, ref object value)
                => false;
            protected override bool TrySerializeRoot(TypeModel model, ref ProtoWriter.State state, object value)
                => false;
            protected override bool TrySerializeRaw(TypeModel model, ref ProtoWriter.State state, object value)
                => false;

            protected override bool TryDeepClone(TypeModel model, ref object value)
                => false;

            protected override bool IsKnownType(TypeModel model)
                => false;

            protected override bool CanSerialize(TypeModel model, out bool isScalar)
            {
                isScalar = false;
                return false;
            }
        }

        private sealed class ConcreteStub<T> : DynamicStub
        {
            protected override bool TryDeserializeRoot(TypeModel model, ref ProtoReader.State state, ref object value)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer == null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                value = state.DeserializeRoot<T>(TypeHelper<T>.FromObject(value), serializer);
                return true;
            }
            protected override bool TryDeserializeRaw(TypeModel model, ref ProtoReader.State state, ref object value)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer == null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                value = serializer.Read(ref state, TypeHelper<T>.FromObject(value));
                return true;
            }

            protected override void WriteMessage(TypeModel model, Type type, ref ProtoWriter.State state, object value)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
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
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer != null)
                {
                    return serializer.Read(ref state, TypeHelper<T>.FromObject(value));
                }
                else
                {
                    return base.ReadMessage(model, type, ref state, value);
                }
            }

            // note: in IsKnownType and CanSerialize we want to avoid asking for the serializer from
            // the model unless we actually need it, as that can cause re-entrancy loops
            protected override bool IsKnownType(TypeModel model) => model != null && model.IsKnownType<T>();

            protected override bool CanSerialize(TypeModel model, out bool isScalar)
            {
                var ser = IsKnownType(model) ? model.GetSerializer<T>() : TypeModel.TryGetSerializer<T>(null);
                isScalar = ser is IScalarSerializer<T>;
                return ser != null;
            }

            protected override bool TrySerializeRoot(TypeModel model, ref ProtoWriter.State state, object value)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer == null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                state.SerializeRoot<T>(TypeHelper<T>.FromObject(value), serializer);
                return true;
            }

            protected override bool TrySerializeRaw(TypeModel model, ref ProtoWriter.State state, object value)
            {
                var serializer = TypeModel.TryGetSerializer<T>(model);
                if (serializer == null) return false;
                // note this null-check is non-trivial; for value-type T it promotes the null to a default
                serializer.Write(ref state, TypeHelper<T>.FromObject(value));
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
