using System;

namespace ProtoBuf.Compiler
{
    internal static class WriterUtil
    {
        internal static readonly Type ByRefStateType = typeof(ProtoWriter.State).MakeByRefType();
    }
}