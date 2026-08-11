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

        /// <summary>
        /// The type carries a contract marker from another family — <c>[DataContract]</c> or
        /// <c>[XmlType]</c> — which <c>MetaType.GetContractFamily</c> treats as a contract in its own
        /// right.
        /// </summary>
        /// <remarks>
        /// The families **mix**: a <c>[DataContract]</c> type may pin one member with
        /// <c>[ProtoMember]</c> and let <c>[DataMember(Order)]</c> supply the rest, and protobuf-net
        /// honours both. So "has ProtoBuf annotations but no [ProtoContract]" is not on its own an
        /// error, which is what this flag exists to express.
        /// </remarks>
        HasOtherContractFamily = 1 << 4,
    }
}
