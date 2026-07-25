#nullable enable
using System;

namespace ProtoBuf.BuildTools.Internal
{
    [Flags]
    internal enum DataContractContextFlags
    {
        None = 0,
        IsProtoContract = 1 << 0,
        SkipConstructor = 1 << 1,
        IgnoreUnknownSubTypes = 1 << 2,

        /// <summary>
        /// The contract declares a surrogate, so protobuf-net never constructs the type itself —
        /// it constructs the surrogate and converts. Notably that makes a parameterless constructor
        /// unnecessary, which is the whole point of surrogating an immutable type.
        /// </summary>
        HasSurrogate = 1 << 3,
    }
}
