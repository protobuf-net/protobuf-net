#nullable enable

using Google.Protobuf.Reflection;
using ProtoBuf.Internal.CodeGen;
using System;
using System.ComponentModel;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenEnumValue : CodeGenEntity
{
    public CodeGenEnumValue(int value, string name, object? origin) : base(origin)
    {
        Value = value;
        OriginalName = Name = name?.Trim() ?? "";;
    }

    public string Name { get; set; }
    
    public int Value { get; }
    
    public string OriginalName { get; set; } // the name in the schema (not used in code, except in the [ProtoMember(..., Name = ...)]
    
    [DefaultValue(Access.Public)]
    public Access Access { get; set; } = Access.Public;
    [DefaultValue(false)]
    public bool IsDeprecated { get; set; }

    public bool ShouldSerializeOriginalName() => OriginalName != Name;
    
    internal static CodeGenEnumValue Parse(EnumValueDescriptorProto enumValue, CodeGenParseContext context)
    {
        var name = context.NameNormalizer.GetName(enumValue);
        var newEnumValue = new CodeGenEnumValue(enumValue.Number, name, enumValue)
        {
            OriginalName = enumValue.Name,
            IsDeprecated = enumValue.Options?.Deprecated ?? false,
        };

        return newEnumValue;
    }
}
