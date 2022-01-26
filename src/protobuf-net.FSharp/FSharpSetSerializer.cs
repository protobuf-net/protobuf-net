using Microsoft.FSharp.Collections;
using ProtoBuf.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProtoBuf.FSharp
{
    /// <summary>
    /// Serialisation provider for F# Set unique collection
    /// </summary>
    /// <typeparam name="T">content of unique items within the Set</typeparam>
    public sealed class FSharpSetSerializer<T> : ExternalSerializer<FSharpSet<T>, T>
    {
        /// <inheritdoc/>
        protected override FSharpSet<T> AddRange(FSharpSet<T> values, ref ArraySegment<T> newValues, ISerializationContext context)
        {
            if (values == null || values.IsEmpty)
            {
                return SetModule.OfSeq(newValues);
            }
            if (newValues.Count == 1)
            {
                return SetModule.Add<T>(newValues.Array[newValues.Offset], values);
            }
            return SetModule.Union(values, SetModule.OfSeq(newValues));
        }

        /// <inheritdoc/>
        protected override FSharpSet<T> Clear(FSharpSet<T> values, ISerializationContext context)
        {
            return SetModule.Empty<T>();
        }

        /// <inheritdoc/>
        protected override int TryGetCount(FSharpSet<T> values)
        {
            return values is null ? 0 : values.Count;
        }
    }

    /// <summary>
    /// Factory class to provide consistent idiom with in-build protobuf collections.
    /// This class is the reason for implementation in C# rather than F#: 
    ///     static classes in F# are module, but module does not allow typeof-module
    /// </summary>
    public static class FSharpSetFactory
    {
        /// <summary>Create a map serializer that operates on FSharp Maps</summary>
        public static RepeatedSerializer<FSharpSet<T>, T> Create<T>()
            => new FSharpSetSerializer<T>();
    }
}
