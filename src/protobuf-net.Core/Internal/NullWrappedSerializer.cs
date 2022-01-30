using ProtoBuf.Serializers;

namespace ProtoBuf.Internal
{
    internal static class NullWrappedSerializer
    {
        internal static ISerializer<T> TryWrap<T>(ISerializer<T> serializer)
        {
            if (serializer is null || TypeHelper<T>.CanBeNull) return null;

            switch (serializer.Features.GetCategory())
            {
                case SerializerFeatures.CategoryScalar:
                    // acceptable
                    break;
                default:
                    ThrowHelper.ThrowInvalidOperationException($"Unable to support nulls for category '{serializer.Features.GetCategory()}'");
                    break; // never reached
            }
            return serializer is Wrapped<T> ? serializer : new Wrapped<T>(serializer);
        }

        private sealed class Wrapped<T> : ISerializer<T>
        {
            private readonly ISerializer<T> _tail;
            public Wrapped(ISerializer<T> tail)
                => _tail = tail;

            SerializerFeatures ISerializer<T>.Features => SerializerFeatures.CategoryMessage;

            T ISerializer<T>.Read(ref ProtoReader.State state, T value)
            {
                int field;
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case 1:
                            var tok = state.StartSubItem();
                            value = _tail.Read(ref state, value);
                            state.EndSubItem(tok);
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                return value;
            }

            void ISerializer<T>.Write(ref ProtoWriter.State state, T value)
                // note that WriteMessage performs a null-check internally
                => state.WriteMessage(1, _tail.Features, value, _tail);
        }
    }
}
