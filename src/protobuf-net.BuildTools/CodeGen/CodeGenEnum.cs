#nullable enable
using Google.Protobuf.Reflection;
using System;

namespace ProtoBuf.CodeGen;

internal class CodeGenEnum : CodeGenType
{
    public CodeGenEnum(string name, string fullyQualifiedPrefix) : base(name, fullyQualifiedPrefix) { }

    internal static CodeGenEnum Parse(EnumDescriptorProto @enum, string fullyQualifiedPrefix, CodeGenContext context, string package)
    {
        // note: remember context.Register(@enum.FullyQualifiedName, newEnum);
        throw new NotImplementedException();
    }
}
