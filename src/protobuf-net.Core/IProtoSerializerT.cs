using ProtoBuf.Internal;
using ProtoBuf.Meta;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace ProtoBuf
{
    /// <summary>
    /// Indicates capabilities and behaviors of a serializer
    /// </summary>
    [Flags]
    public enum SerializerFeatures
    {
        /// <summary>
        /// Base-128 variable-length encoding
        /// </summary>
        WireTypeVarint = WireType.Varint,

        /// <summary>
        /// Fixed-length 8-byte encoding
        /// </summary>
        WireTypeFixed64 = WireType.Fixed64,

        /// <summary>
        /// Length-variant-prefixed encoding
        /// </summary>
        WireTypeString = WireType.String,

        /// <summary>
        /// Indicates the start of a group
        /// </summary>
        WireTypeStartGroup = WireType.StartGroup,

        /// <summary>
        /// Fixed-length 4-byte encoding
        /// </summary>10
        WireTypeFixed32 = WireType.Fixed32,

        /// <summary>
        /// Denotes a varint that should be interpreted using
        /// zig-zag semantics (so -ve numbers aren't a significant overhead)
        /// </summary>
        WireTypeSignedVarint = WireType.SignedVarint,

        /// <summary>
        /// Indicates that this data should be treated like a list/array
        /// </summary>
        CategoryRepeated = 1 << 4,

        /// <summary>
        /// Scalars are simple types such as integers, not messages; when written as
        /// a root message, a field-one wrapper is added
        /// </summary>
        CategoryScalar = 1 << 5,

        /// <summary>
        /// Indicates a type that is a message
        /// </summary>
        CategoryMessage = 1 << 6,

        /// <summary>
        /// Indicates a type that is both "message" and "scalar"; *at the root only* it will be a message wrapped like a scalar; otherwise, it is
        /// treated as a message; see: DateTime/TimeSpan
        /// </summary>
        CategoryMessageWrappedAtRoot = CategoryMessage | CategoryScalar,

        /// <summary>
        /// Explicitly disables packed encoding; normally, packed encoding is
        /// used by default when appropriate
        /// </summary>
        OptionPackedDisabled = 1 << 7,

        /// <summary>
        /// List-like values should *replace*, rather than append
        /// </summary>
        OptionOverwriteList = 1 << 8,

#if FEAT_NULL_LIST_ITEMS
        /// <summary>
        /// Nulls in lists should be preserved
        /// </summary>
        OptionListsSupportNull = 1 << 9,
#endif
    }

    internal static class SerializerFeaturesExtensions
    {
        [MethodImpl(ProtoReader.HotPath)]
        public static SerializerFeatures AsFeatures(this WireType wireType)
            => (SerializerFeatures)wireType;

        [MethodImpl(ProtoReader.HotPath)]
        public static SerializerFeatures GetCategory(this SerializerFeatures features)
        {
            const SerializerFeatures mask = SerializerFeatures.CategoryMessage
                | SerializerFeatures.CategoryRepeated
                | SerializerFeatures.CategoryScalar;
            return features & mask;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowInvalidCategory(this SerializerFeatures features)
        {
            var category = features.GetCategory();
            var msg = category == features
                ? $"The category {category} is not expected in this context"
                : $"The category {category} is not expected in this context (full features: {features})";
            ThrowHelper.ThrowInvalidOperationException(msg);
        }

        [MethodImpl(ProtoReader.HotPath)]
        public static bool IsPackedDisabled(this SerializerFeatures features)
            => (features & SerializerFeatures.OptionPackedDisabled) != 0;

        [MethodImpl(ProtoReader.HotPath)]
        public static WireType GetWireType(this SerializerFeatures features)
        {
            const SerializerFeatures mask = (SerializerFeatures)7
                | SerializerFeatures.CategoryRepeated; // this isn't an accident; this is so
            // fundamental that if someone is including the category: they've screwed up
            return (WireType)(int)(features & mask);
        }
    }

    /// <summary>
    /// Allows a provider to offer indirect access to serivces; note that this is a *secondary* API; the
    /// primary API is for the provider to implement ISerializer-T for the intended T; however, to offer
    /// indirect access to known serializers, when asked for a type, provide the appropriate ISerializer-T
    /// for that type. This method can (and often will) return null. The implementation can also return
    /// the input type to indicate that an enum is included, or return another provider type to use as a proxy.
    /// </summary>
    public interface ISerializerFactory
    {
        /// <summary>
        /// Attempt to obtain a provider or service for the required type
        /// </summary>
        object TryCreate(Type type);
    }

    /// <summary>
    /// Abstract API capable of serializing/deserializing messages or values
    /// </summary>
    public interface ISerializer<T>
    {
        /// <summary>
        /// Deserialize an instance from the supplied writer
        /// </summary>
        T Read(ref ProtoReader.State state, T value);

        /// <summary>
        /// Serialize an instance to the supplied writer
        /// </summary>
        void Write(ref ProtoWriter.State state, T value);

        /// <summary>
        /// Indicates the features (including the default wire-type) for this type/serializer
        /// </summary>
        SerializerFeatures Features { get; }
    }

    /// <summary>
    /// Abstract API capable of serializing/deserializing objects as part of a type hierarchy
    /// </summary>
    public interface ISubTypeSerializer<T> where T : class
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
            // pick the best available constructor; conside C : B : A, and we're currently deserializing
            // layer B at the point the object is first needed; the caller could have asked
            // for Deserialize<A>, in which case we'll choose B (because we're at that layer), but the
            // caller could have asked for Deserialize<C>, in which case we'll prefer C (because that's
            // what they asked for)
            var typed = ((_ctor as Func<ISerializationContext, T>) ?? TypeHelper<T>.Factory)(_context);

            if (_value != null) typed = Merge(_context, _value, typed);
            _onBeforeDeserialize?.Invoke(typed, _context);
            _value = typed;
            return typed;

            // this isn't especially efficient, but it should work
            static T Merge(ISerializationContext context, object value, T typed)
            {
                using var ms = new MemoryStream();
                // this <object> sneakily finds the correct base-type
                context.Model.Serialize<object>(ms, value, context.Context);
                ms.Position = 0;
                return context.Model.Deserialize<T>(ms, typed, context.Context);
            }
        }


        /// <summary>
        /// Parse the input as a sub-type of the instance
        /// </summary>
        public void ReadSubType<TSubType>(ref ProtoReader.State state, ISubTypeSerializer<TSubType> serializer = null) where TSubType : class, T
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
    }

    /// <summary>
    /// Abstract API capable of serializing/deserializing complex objects with inheritance
    /// </summary>
    public interface IFactory<T>
    {
        /// <summary>
        /// Create a new instance of the type
        /// </summary>
        T Create(ISerializationContext context);
    }
}
