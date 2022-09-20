#nullable enable

using Google.Protobuf.Reflection;
using System;
using System.ComponentModel;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenEnumValue
{
    public CodeGenEnumValue(int value, string name)
    {
        Value = value;
        OriginalName = Name = name?.Trim() ?? "";;
    }

    public string Name { get; set; }
    
    public int Value { get; }
    
    public string OriginalName { get; set; } // the name in the schema (not used in code, except in the [ProtoMember(..., Name = ...)]
    
    [DefaultValue(Access.Public)]
    public Access Access { get; set; } = Access.Public;
    
    public bool ShouldSerializeOriginalName() => OriginalName != Name;
    
    internal static CodeGenEnumValue Parse(EnumValueDescriptorProto enumValue, CodeGenParseContext context)
    {
        var name = context.NameNormalizer.GetName(enumValue);
        var newEnumValue = new CodeGenEnumValue(enumValue.Number, name)
        {
            OriginalName = enumValue.Name
        };

        return newEnumValue;
    }
}
