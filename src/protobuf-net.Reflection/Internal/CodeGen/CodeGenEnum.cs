#nullable enable
using Google.Protobuf.Reflection;
using System;
using System.ComponentModel;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenEnum : CodeGenType
{
    public CodeGenEnum(string name, string fullyQualifiedPrefix) : base(name, fullyQualifiedPrefix)
    {
        OriginalName = base.Name;
    }

    public string OriginalName { get; set; }

    [DefaultValue(false)]
    public bool IsDeprecated { get; set; }

    [DefaultValue(Access.Public)]
    public Access Access { get; set; } = Access.Public;

    public bool ShouldSerializeOriginalName() => OriginalName != Name;

    internal static CodeGenEnum Parse(EnumDescriptorProto @enum, string fullyQualifiedPrefix, CodeGenParseContext context, string package)
    {
        // note: remember context.Register(@enum.FullyQualifiedName, newEnum);
        throw new NotImplementedException();
    }
}
