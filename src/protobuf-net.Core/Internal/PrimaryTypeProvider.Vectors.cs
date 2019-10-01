using ProtoBuf.Meta;
using System;
using System.Collections.Generic;

namespace ProtoBuf.Internal
{
    partial class PrimaryTypeProvider
    {
        private sealed class VectorSerializer<T> : ISerializer<T[]>
        {
            public SerializerFeatures Features => SerializerFeatures.CategoryRepeated;

            public T[] Read(ref ProtoReader.State state, T[] value)
            {
                int field;
                ISerializer<T> serializer = null;
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case TypeModel.ListItemTag:
                            serializer ??= TypeModel.GetSerializer<T>(state.Model);
                            value = state.ReadRepeated<T>(value, serializer);
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
        }

        private sealed class ListSerializer<TList, T> : ISerializer<TList>
            where TList : List<T>
        {
            public SerializerFeatures Features => SerializerFeatures.CategoryRepeated;

            public TList Read(ref ProtoReader.State state, TList value)
            {
                int field;
                ISerializer<T> serializer = null;
                while ((field = state.ReadFieldHeader()) > 0)
                {
                    switch (field)
                    {
                        case TypeModel.ListItemTag:
                            serializer ??= TypeModel.GetSerializer<T>(state.Model);
                            value = state.ReadRepeated<TList, T>(value, serializer);
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
        }
    }
}
