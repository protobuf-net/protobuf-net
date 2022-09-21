#nullable enable

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal enum CodeGenWellKnownType
{
    None,
    Boolean,
    Float,
    Double,
    Bytes,
    String,

    Int32,
    SInt32,
    UInt32,
    Fixed32,
    SFixed32,

    Int64,
    SInt64,
    UInt64,
    Fixed64,
    SFixed64,
    NetObjectProxy,
}
