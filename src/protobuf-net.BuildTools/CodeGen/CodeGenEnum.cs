#nullable enable
using Google.Protobuf.Reflection;
using System;

namespace ProtoBuf.CodeGen;

internal class CodeGenEnum : CodeGenType
{
    public CodeGenEnum(string name, string fullyQualifiedPrefix) : base(name, fullyQualifiedPrefix) { }

    internal static CodeGenEnum Parse(EnumDescriptorProto message, string fullyQualifiedPrefix, CodeGenContext context, string package)
        => throw new NotImplementedException();
}
