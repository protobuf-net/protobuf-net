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
                if ((features & SerializerFeatures.CategoryRepeated) != 0)
                    TypeModel.ThrowNestedListsNotSupported(typeof(T));
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
                state.WriteRepeated<T>(TypeModel.ListItemTag, serializer.Features, value, serializer);
            }
            public void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures features, T[] value)
            {
                var serializer = TypeModel.GetSerializer<T>(state.Model);
                state.WriteRepeated<T>(fieldNumber, features, value, serializer);
            }
        }

        private sealed class ListSerializer<TList, T> : IRepeatedSerializer<TList>
            where TList : List<T>
        {
            public SerializerFeatures Features => SerializerFeatures.CategoryRepeated;

            public TList Read(ref ProtoReader.State state, TList value)
            {
                int field;
                var serializer = TypeModel.GetSerializer<T>(state.Model);
                var features = serializer.Features;
                if ((features & SerializerFeatures.CategoryRepeated) != 0)
                    TypeModel.ThrowNestedListsNotSupported(typeof(T));
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case TypeModel.ListItemTag:
                            value = state.ReadRepeated<TList, T>(features, value, serializer);
                            break;
                        default:
                            state.SkipField();
                            break;
                    }
                }
                return value ?? state.CreateInstance<TList>();
            }

            public void Write(ref ProtoWriter.State state, TList value)
            {
                var serializer = TypeModel.GetSerializer<T>(state.Model);
                state.WriteRepeated<T>(TypeModel.ListItemTag, serializer.Features, value, serializer);
            }

            public void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures features, TList value)
            {
                var serializer = TypeModel.GetSerializer<T>(state.Model);
                state.WriteRepeated<T>(fieldNumber, features, value, serializer);
            }
        }
    }
}
