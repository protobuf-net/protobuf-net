#nullable enable
using System;
using Google.Protobuf.Reflection;

namespace ProtoBuf.Reflection.Internal.CodeGen;

[Flags]
internal enum CodeGenServiceMethodParametersDescriptor
{
    /// <summary>
    /// None additional parameters are passed
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Among parameters 'Protobuf.Grpc.CallContext' exists
    /// </summary>
    HasCallContext = 1,
    
    /// <summary>
    /// Among parameters 'System.Threading.CancellationToken' exists
    /// </summary>
    HasCancellationToken = 2,
}
