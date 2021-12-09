using ProtoBuf.Internal;
using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using ProtoBuf.WellKnownTypes;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ProtoBuf.WellKnownTypes
{
    /// <summary>
    /// Represents an arbitrary payload with embedded type information.
    /// </summary>
    [ProtoContract(Name = ".google.protobuf.Duration", Serializer = typeof(PrimaryTypeProvider), Origin = "google/protobuf/duration.proto")]
    [StructLayout(LayoutKind.Auto)]
    public readonly struct Any : IEquatable<Any>
    {
        /// <summary>
        /// Unique identifier for the embedded value.
        /// </summary>
        [ProtoMember(1, Name = "type_url")]
        public string TypeUrl { get; }

        /// <summary>
        /// The payload associated with this value.
        /// </summary>
        [ProtoMember(2, Name = "value")]
        public ReadOnlyMemory<byte> Value { get; }

        /// <summary>
        /// Create a new <see cref="Any"/> value.
        /// </summary>
        public Any(string typeUrl, ReadOnlyMemory<byte> value)
        {
            TypeUrl = typeUrl ?? throw new ArgumentNullException(nameof(typeUrl));
            Value = value;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => (TypeUrl ?? "").GetHashCode() ^ Value.GetHashCode();

        /// <inheritdoc/>
        public override bool Equals(object obj)
            => obj is Any other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(Any other)
            => TypeUrl == other.TypeUrl && Value.Equals(other.Value);

        /// <inheritdoc/>
        public override string ToString() => TypeUrl is null ? "(empty)" : $"'{TypeUrl}': {Value.Length} bytes";


        public T Unpack<T>(TypeModel model = null, object userState = null)
            => (model ?? TypeModel.DefaultModel).Deserialize<T>(Value, default, userState);

        public bool TryUnpack<T>(out T value, TypeModel model = null, object userState = null)
        {
            model ??= TypeModel.DefaultModel;
            var typeUrl = GetTypeUrl(model, typeof(T));
            if (TypeUrl == typeUrl)
            {
                value = model.Deserialize<T>(Value, default, userState);
                return true;
            }
            value = default;
            return false;
        }

        public static Any Pack<T>(T value, TypeModel model = null, object userState = null)
        {
            model ??= TypeModel.DefaultModel;
            var typeUrl = GetTypeUrl(model, typeof(T));

            using var ms = new MemoryStream(); // todo: do this better; maybe .Measure etc
            model.Serialize<T>(ms, value, userState);
            return new Any(typeUrl, ToROM(ms));
        }

        static ReadOnlyMemory<byte> ToROM(MemoryStream stream)
        {
            if (stream.TryGetBuffer(out var segment))
            {
                return segment;
            }
            return stream.ToArray();
        }

        public bool TryUnpack(ISomeNewRegistryApi registry, out object value, TypeModel model = null, object userState = null)
        {
            var type = TypeUrl is null ? null : registry.GetType(TypeUrl);
            if (type is null)
            {
                value = default;
                return false;
            }
            model ??= TypeModel.DefaultModel;
            value = model.Deserialize(type, Value, default, userState);
            return true;
        }

        public static Any Pack(ISomeNewRegistryApi registry, object value, TypeModel model = null, object userState = null)
        {
            if (value is null) return default;
            var typeUrl = registry.GetTypeUrl(value.GetType());

            using var ms = new MemoryStream(); // todo: do this better; maybe .Measure etc
            model.Serialize(ms, value, userState);
            return new Any(typeUrl, ToROM(ms));

        }
        public interface ISomeNewRegistryApi // do we actually need this? maybe we should have a new optional API on TypeModel instead?
        {
            string GetTypeUrl(Type type);
            Type GetType(string typeUrl);
        }

        private static string GetTypeUrl(TypeModel model, Type type)
        {   // need to check cross-ref Google bits here; may need to special case and only support full names?
            throw new NotImplementedException();
        }
    }
}

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider : ISerializer<Any>, ISerializer<Any?>
    {
        SerializerFeatures ISerializer<Any>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;
        SerializerFeatures ISerializer<Any?>.Features => SerializerFeatures.WireTypeString | SerializerFeatures.CategoryMessage;

        Any? ISerializer<Any?>.Read(ref ProtoReader.State state, Any? value)
            => ((ISerializer<Any>)this).Read(ref state, value.GetValueOrDefault());

        void ISerializer<Any?>.Write(ref ProtoWriter.State state, Any? value)
            => ((ISerializer<Any>)this).Write(ref state, value.Value);

        Any ISerializer<Any>.Read(ref ProtoReader.State state, Any value)
        {
            var typeUrl = value.TypeUrl;
            var bytes = value.Value;
            int fieldNumber;

            while ((fieldNumber = state.ReadFieldHeader()) > 0)
            {
                switch (fieldNumber)
                {
                    case 1:
                        typeUrl = state.ReadString();
                        break;
                    case 2:
                        bytes = state.AppendBytes(ReadOnlyMemory<byte>.Empty); // replace, not append
                        break;
                    default:
                        state.SkipField();
                        break;
                }
            }
            return new Any(typeUrl, bytes);
        }
        void ISerializer<Any>.Write(ref ProtoWriter.State state, Any value)
        {
            state.WriteString(1, value.TypeUrl);
            state.WriteFieldHeader(2, WireType.String);
            state.WriteBytes(value.Value);
        }
    }
}
