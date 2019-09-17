using ProtoBuf.Meta;
using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Internal
{
    // bridge between the world of Type and the world of <T>, in a way that doesn't involve constant reflection
    internal abstract class DynamicStub
    {
        private static readonly Hashtable s_byType = new Hashtable
        {
            {typeof(object), NilStub.Instance }
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryDeserialize(Type type, TypeModel model, ProtoReader reader, ref ProtoReader.State state, ref object value)
            => Get(type).TryDeserialize(model, reader, ref state, ref value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static DynamicStub Get(Type type) => (DynamicStub)s_byType[type] ?? SlowGet(type);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DynamicStub SlowGet(Type type)
        {
            var obj = Activator.CreateInstance(typeof(ConcreteStub<>).MakeGenericType(type), nonPublic: true);
            lock (s_byType)
            {
                s_byType[type] = obj;
            }
            return (DynamicStub)obj;
        }

        protected abstract bool TryDeserialize(TypeModel model, ProtoReader reader, ref ProtoReader.State state, ref object value);

        private sealed class NilStub : DynamicStub
        {
            public static DynamicStub Instance { get; } = new NilStub();
            private NilStub() { }
            protected override bool TryDeserialize(TypeModel model, ProtoReader reader, ref ProtoReader.State state, ref object value)
                => false;
        }
        private sealed class ConcreteStub<T> : DynamicStub
        {
            protected override bool TryDeserialize(TypeModel model, ProtoReader reader, ref ProtoReader.State state, ref object value)
            {
                IProtoSerializer<T> serializer = null;
                try { serializer = TypeModel.GetSerializer<T>(model); }
                catch { }
                if (serializer == null) return false;
                value = reader.Deserialize<T>(ref state, (T)value, serializer);
                return true;
            }
        }
    }
}
