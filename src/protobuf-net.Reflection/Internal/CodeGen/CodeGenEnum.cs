#nullable enable
using Google.Protobuf.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf.Reflection.Internal.CodeGen.Collections;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenEnum : CodeGenType
{
    [DefaultValue(CodeGenGenerate.All)]
    public CodeGenGenerate Emit { get; set; } = CodeGenGenerate.All;

    public CodeGenEnum(string name, string fullyQualifiedPrefix) : base(name, fullyQualifiedPrefix)
    {
        OriginalName = base.Name;
    }

    public string OriginalName { get; set; }

    private NonNullableList<CodeGenEnumValue>? _enumValues;
    public IList<CodeGenEnumValue> EnumValues => _enumValues ??= new();
    
    public CodeGenType Type { get; set; } = CodeGenSimpleType.Int32;
    public bool ShouldSerializeType() => false;

    public string? TypeName => Type?.Serialize();

    [DefaultValue(false)]
    public bool IsDeprecated { get; set; }

    [DefaultValue(Access.Public)]
    public Access Access { get; set; } = Access.Public;

    public bool ShouldSerializeEnumValues() => _enumValues is { Count: > 0 };
    public bool ShouldSerializeOriginalName() => OriginalName != Name;

    internal static CodeGenEnum Parse(EnumDescriptorProto @enum, string fullyQualifiedPrefix, CodeGenParseContext context, string package)
    {
        // note: remember context.Register(@enum.FullyQualifiedName, newEnum);
        var name = context.NameNormalizer.GetName(@enum);
        
        var newEnum = new CodeGenEnum(name, fullyQualifiedPrefix);
        context.Register(@enum.FullyQualifiedName, newEnum);
        if (@enum.Options?.Deprecated is not null)
        {
            newEnum.IsDeprecated = @enum.Options.Deprecated;    
        }
        
        if (@enum.Values.Count > 0)
        {
            foreach (var enumValue in @enum.Values)
            {
                newEnum.EnumValues.Add(CodeGenEnumValue.Parse(enumValue, context));
            }
        } 

        return newEnum;
    }
}
