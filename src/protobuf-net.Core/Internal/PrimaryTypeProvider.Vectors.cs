using ProtoBuf.Meta;
using System;
using System.Collections.Generic;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider
    {
        private sealed class VectorSerializer<T> : IRepeatedSerializer<T[]>
        {
            public SerializerFeatures Features => SerializerFeatures.CategoryRepeated;

            public T[] Read(ref ProtoReader.State state, T[] value)
            {
                int field;
                var serializer = TypeModel.GetSerializer<T>(state.Model);
                var features = serializer.Features;
                if (features.IsRepeated()) TypeModel.ThrowNestedListsNotSupported(typeof(T));
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case TypeModel.ListItemTag:
                            value = state.ReadRepeated<T>(features, value, serializer);
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                return value ?? Array.Empty<T>();
            }

            public void Write(ref ProtoWriter.State state, T[] value)
            {
                var serializer = TypeModel.GetSerializer<T>(state.Model);
                state.WriteRepeated(TypeModel.ListItemTag, serializer.Features, value, serializer);
            }
            public void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures features, T[] value)
            {
                var serializer = TypeModel.GetSerializer<T>(state.Model);
                state.WriteRepeated(fieldNumber, features, value, serializer);
            }
        }

        private sealed class ListSerializer<TList, T> : IRepeatedSerializer<TList>
            where TList : class, ICollection<T>
        {
            public SerializerFeatures Features => SerializerFeatures.CategoryRepeated;

            public TList Read(ref ProtoReader.State state, TList value)
            {
                int field;
                var serializer = TypeModel.GetSerializer<T>(state.Model);
                var features = serializer.Features;
                if (value is null) value = state.CreateInstance<TList>();
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case TypeModel.ListItemTag:
                            value = (TList)state.ReadRepeated(features, value, serializer);
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                return value;
            }

            public void Write(ref ProtoWriter.State state, TList value)
            {
                var serializer = TypeModel.GetSerializer<T>(state.Model);
                state.WriteRepeated(TypeModel.ListItemTag, serializer.Features, value, serializer);
            }

            public void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures features, TList value)
                => state.WriteRepeated(fieldNumber, features, value);
        }

        private sealed class DictionarySerializer<TDictionary, TKey, TValue>
            : IRepeatedSerializer<TDictionary>
            where TDictionary : class, IEnumerable<KeyValuePair<TKey, TValue>>
        {
            public SerializerFeatures Features => SerializerFeatures.CategoryRepeated;

            public TDictionary Read(ref ProtoReader.State state, TDictionary value)
            {
                int field;
                var keySerializer = TypeModel.GetSerializer<TKey>(state.Model);
                var valueSerializer = TypeModel.GetSerializer<TValue>(state.Model);
                if (value is null) value = state.CreateInstance<TDictionary>();
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case TypeModel.ListItemTag:
                            value = (TDictionary)state.ReadMap(default, keySerializer.Features, valueSerializer.Features, value, keySerializer, valueSerializer);
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                return value;
            }

            public void Write(ref ProtoWriter.State state, TDictionary value)
                => state.WriteMap(TypeModel.ListItemTag, value);

            public void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures features, TDictionary value)
                => state.WriteMap(TypeModel.ListItemTag, features, default, default, value, default, default);
        }
    }
}
