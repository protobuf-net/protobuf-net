using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    /// <summary>
    /// Abstract API capable of serializing/deserializing
    /// </summary>
    public interface IProtoSerializer<T>
    {

        /// <summary>
        /// Deserialize an instance from the supplied writer
        /// </summary>
        T Read(ref ProtoReader.State state, T value);

        /// <summary>
        /// Serialize an instance to the supplied writer
        /// </summary>
        void Write(ref ProtoWriter.State state, T value);
    }

    /// <summary>
    /// Abstract API capable of serializing/deserializing objects as part of a type hierarchy
    /// </summary>
    public interface IProtoSubTypeSerializer<T> where T : class
    {
        /// <summary>
        /// Serialize an instance to the supplied writer
        /// </summary>
        void WriteSubType(ref ProtoWriter.State state, T value);

        /// <summary>
        /// Deserialize an instance from the supplied writer
        /// </summary>
        T ReadSubType(ref ProtoReader.State state, SubTypeState<T> value);
    }

    /// <summary>
    /// Represents the state of an inheritance deserialization operation
    /// </summary>
    public struct SubTypeState<T>
        where T : class
    {
        private readonly ISerializationContext _context;
        private readonly Func<ISerializationContext, object> _ctor;
        private object _value;
        private Action<T, ISerializationContext> _onBeforeDeserialize;

        /// <summary>
        /// Create a new value, using the provided concrete type if a new instance is required
        /// </summary>
        public static SubTypeState<T> Create<TValue>(ISerializationContext context, TValue value)
            where TValue : class, T
            => new SubTypeState<T>(context, TypeHelper<TValue>.Factory, value, null);

        private SubTypeState(ISerializationContext context, Func<ISerializationContext, object> ctor,
            object value, Action<T, ISerializationContext> onBeforeDeserialize)
        {
            _context = context;
            _ctor = ctor;
            _value = value;
            _onBeforeDeserialize = onBeforeDeserialize;
        }

        /// <summary>
        /// Gets or sets the current instance represented
        /// </summary>
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_value as T) ?? Cast();
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _value = value;
        }

        /// <summary>
        /// Ensures that the instance has a value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CreateIfNeeded() => _ = Value;

        internal object RawValue => _value;

        /// <summary>
        /// Indicates whether an instance currently exists
        /// </summary>
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
                var typed = ((_ctor as Func<ISerializationContext, T>) ?? TypeHelper<T>.Factory)(_context);
                _value = typed;
                _onBeforeDeserialize?.Invoke(typed, _context);
                return typed;
            }

            ThrowHelper.ThrowNotImplementedException("upcast");
            return default;
        }

        /// <summary>
        /// Parse the input as a sub-type of the instance
        /// </summary>
        public void ReadSubType<TSubType>(ref ProtoReader.State state, IProtoSubTypeSerializer<TSubType> serializer = null) where TSubType : class, T
        {
            var tok = state.StartSubItem();
            _value = (serializer ?? TypeModel.GetSubTypeSerializer<TSubType>(_context.Model)).ReadSubType(ref state,
                new SubTypeState<TSubType>(_context, _ctor, _value, _onBeforeDeserialize));
            state.EndSubItem(tok);
        }

        /// <summary>
        /// Specifies a serialization callback to be used when the item is constructed; if the item already exists, the callback is executed immediately
        /// </summary>
        public void OnBeforeDeserialize(Action<T, ISerializationContext> callback)
        {
            if (callback != null)
            {
                if (_value is T obj) callback.Invoke(obj, _context);
                else if (_onBeforeDeserialize is object) ThrowHelper.ThrowInvalidOperationException("Only one pending " + nameof(OnBeforeDeserialize) + " callback is supported");
                else _onBeforeDeserialize = callback;
            }
        }

        /// <summary>
        /// Performs a serialization callback on the instance, returning the instance; this is usually the last method in a deserializer
        /// </summary>
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
