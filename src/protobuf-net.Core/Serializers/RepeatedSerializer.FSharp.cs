using ProtoBuf.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.FSharp.Collections;

namespace ProtoBuf.Serializers
{
    public static partial class RepeatedSerializer
    {
        /// <summary>Create a serializer that operates on FSharp Lists</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<FSharpList<T>, T> CreateFSharpList<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<FSharpListSerializer <T>>.InstanceField;

        /// <summary>Create a serializer that operates on FSharp Sets</summary>
        [MethodImpl(ProtoReader.HotPath)]
        public static RepeatedSerializer<FSharpSet<T>, T> CreateFSharpSet<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] T>()
            => SerializerCache<FSharpSetSerializer<T>>.InstanceField;

    }

    sealed class FSharpListSerializer<T> : RepeatedSerializer<FSharpList<T>, T>
    {
        protected override FSharpList<T> Initialize(FSharpList<T> values, ISerializationContext context)
            => values == null || values.IsEmpty ? ListModule.Empty<T> (): values;
        protected override FSharpList<T> Clear(FSharpList<T> values, ISerializationContext context)
            => ListModule.Empty<T>();
        protected override FSharpList<T> AddRange(FSharpList<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            if (values.IsEmpty)
            {
                return ListModule.OfSeq(newValues);
            }
            return ListModule.Append(values, ListModule.OfSeq(newValues));
        }

        protected override int TryGetCount(FSharpList<T> values) => values.IsEmpty ? 0 : values.Length;

        internal override long Measure(FSharpList<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = ListModule.ToSeq(values).GetEnumerator();
            return Measure(ref iter, serializer, context, wireType);
        }

        internal override void WritePacked(ref ProtoWriter.State state, FSharpList<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = ListModule.ToSeq(values).GetEnumerator();
            WritePacked(ref state, ref iter, serializer, wireType);
        }

        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, FSharpList<T> values, ISerializer<T> serializer)
        {
            var iter = ListModule.ToSeq(values).GetEnumerator();
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer);
        }
    }

    sealed class FSharpSetSerializer<T> : RepeatedSerializer<FSharpSet<T>, T>
    {
        protected override FSharpSet<T> Initialize(FSharpSet<T> values, ISerializationContext context)
            => values ?? SetModule.Empty<T>();
        protected override FSharpSet<T> AddRange(FSharpSet<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            if (values == null || values.IsEmpty)
            {
                return SetModule.OfSeq(newValues);
            }
            if (newValues.Count == 1)
            {
                return SetModule.Add<T>(newValues.Singleton(), values);
            }
            return SetModule.Union(values, SetModule.OfSeq(newValues));
        }

        protected override FSharpSet<T> Clear(FSharpSet<T> values, ISerializationContext context)
            => SetModule.Empty<T>();
        protected override int TryGetCount(FSharpSet<T> values) => values is null ? 0 : values.Count;

        internal override long Measure(FSharpSet<T> values, IMeasuringSerializer<T> serializer, ISerializationContext context, WireType wireType)
        {
            var iter = SetModule.ToSeq(values).GetEnumerator();
            return Measure(ref iter, serializer, context, wireType);
        }
        internal override void Write(ref ProtoWriter.State state, int fieldNumber, SerializerFeatures category, WireType wireType, FSharpSet<T> values, ISerializer<T> serializer)
        {
            var iter = SetModule.ToSeq(values).GetEnumerator();
            Write(ref state, fieldNumber, category, wireType, ref iter, serializer);
        }
        internal override void WritePacked(ref ProtoWriter.State state, FSharpSet<T> values, IMeasuringSerializer<T> serializer, WireType wireType)
        {
            var iter = SetModule.ToSeq(values).GetEnumerator();
            WritePacked(ref state, ref iter, serializer, wireType);
        }
    }
}
