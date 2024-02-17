using System;
using System.Diagnostics;

namespace ProtoBuf;

/// <summary>
/// Specifies that the protobuf-net code-analysis based generator should emit serializer assistance for applicable contract types.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Module | AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
[Conditional("NEVER_INCLUDED_IN_COMPILED_CODE")]
public sealed class ProtoGenerateAttribute : Attribute { }
