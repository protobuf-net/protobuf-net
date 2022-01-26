using Microsoft.FSharp.Collections;
using ProtoBuf.Serializers;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf.FSharp
{
    /// <summary>
    /// Serialisation provider for F# lists
    /// </summary>
    /// <typeparam name="T">content of list</typeparam>
    public sealed class FSharpListSerializer<T> : ExternalSerializer<FSharpList<T>, T>
    {
        /// <inheritdoc/>
        protected override FSharpList<T> AddRange(FSharpList<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            if (values == null || values.IsEmpty)
            {
                return ListModule.OfSeq(newValues);
            }
            return ListModule.Append(values, ListModule.OfSeq(newValues));
        }

        /// <inheritdoc/>
        protected override FSharpList<T> Clear(FSharpList<T> values, ISerializationContext context)
        {
            return ListModule.Empty<T>();
        }

        /// <inheritdoc/>
        protected override int TryGetCount(FSharpList<T> values)
        {
            return values.IsEmpty ? 0 : values.Length;
        }
    }
    /// <summary>
    /// Factory class to provide consistent idiom with in-build protobuf collections.
    /// This class is the reason for implementation in C# rather than F#: 
    ///     static classes in F# are module, but module does not allow typeof-module
    /// </summary>
    public static class FSharpListFactory
    {
        /// <summary>Create a map serializer that operates on FSharp Maps</summary>
        public static RepeatedSerializer<FSharpList<T>, T> Create<T>()
            => new FSharpListSerializer<T>();
    }

}
