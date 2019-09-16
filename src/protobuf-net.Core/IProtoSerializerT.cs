using ProtoBuf.Meta;
using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    /// <summary>
    /// Abstract API capable of serializing/deserializing
    /// </summary>
    public interface IProtoSerializer<in T>
    {
        /// <summary>
        /// Serialize an instance to the supplied writer
        /// </summary>
        void Write(ProtoWriter writer, ref ProtoWriter.State state, T value);
    }

    public interface IProtoDeserializer<T>
    {
        /// <summary>
        /// Deserialize an instance from the supplied writer
        /// </summary>
        T Read(ProtoReader reader, ref ProtoReader.State state, T value);
    }

    /// <summary>
    /// Abstract API capable of serializing/deserializing objects as part of a type hierarchy
    /// </summary>
    public interface IProtoSubTypeSerializer<T> where T : class
    {
        /// <summary>
        /// Serialize an instance to the supplied writer
        /// </summary>
        void WriteSubType(ProtoWriter writer, ref ProtoWriter.State state, T value);

        /// <summary>
        /// Deserialize an instance from the supplied writer
        /// </summary>
        T ReadSubType(ProtoReader reader, ref ProtoReader.State state, SubTypeState<T> value);
    }

    public struct SubTypeState<T>
    {
        private readonly ISerializationContext _context;
        private readonly Func<ISerializationContext, object> _ctor;
        private object _value;
        private Action<T, ISerializationContext> _onBeforeDeserialize;

        internal SubTypeState(ISerializationContext context, Func<ISerializationContext, object> ctor,
            object value, Action<T, ISerializationContext> onBeforeDeserialize = null)
        {
            _context = context;
            _ctor = ctor;
            _value = value;
            _onBeforeDeserialize = onBeforeDeserialize;
        }

        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value is T typed ? typed : Cast();
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _value = value;
        }

        public void CreateIfNeeded()
        {
            if (_value is null) Cast();
        }

        internal object RawValue => _value;

        public bool HasValue => _value is object;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private T Cast()
        {
            if (_value == null)
            {
                // pick the best available constructor; conside C : B : A, and we're currently deserializing
                // layer B at the point the object is first needed; the caller could have asked
                // for Deserialize<A>, in which case we'll choose B (because we're at that layer), but the
                // caller could have asked for Deserialize<C>, in which case we'll prefer C (because that's
                // what they asked for)
                var typed = ((_ctor as Func<ISerializationContext, T>) ?? TypeHelper<T>.Instance)(_context);
                _value = typed;
                _onBeforeDeserialize?.Invoke(typed, _context);
                return typed;
            }

            throw new NotImplementedException("upcast");
        }

        public void ReadSubType<TSubType>(ProtoReader reader, ref ProtoReader.State state, IProtoSubTypeSerializer<TSubType> serializer = null) where TSubType : class, T
        {
            var tok = ProtoReader.StartSubItem(reader, ref state);
            _value = (serializer ?? TypeModel.GetSubTypeSerializer<TSubType>(_context.Model)).ReadSubType(reader, ref state,
                new SubTypeState<TSubType>(_context, _ctor, _value, _onBeforeDeserialize));
            ProtoReader.EndSubItem(tok, reader, ref state);
        }

        public void OnBeforeDeserialize(Action<T, ISerializationContext> callback)
        {
            if (callback != null)
            {
                if (_value is T obj) callback.Invoke(obj, _context);
                else if (_onBeforeDeserialize is object) throw new InvalidOperationException("Only one pending " + nameof(OnBeforeDeserialize) + " callback is supported");
                else _onBeforeDeserialize = callback;
            }
        }

        public T OnAfterDeserialize(Action<T, ISerializationContext> callback)
        {
            var obj = Value;
            callback?.Invoke(obj, _context);
            return obj;
        }
    }

    /// <summary>
    /// Abstract API capable of serializing/deserializing complex objects with inheritance
    /// </summary>
    public interface IProtoFactory<T>
    {
        /// <summary>
        /// Create a new instance of the type
        /// </summary>
        T Create(ISerializationContext context);
    }
}
