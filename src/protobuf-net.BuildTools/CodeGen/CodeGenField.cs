#nullable enable


using Google.Protobuf.Reflection;
using System;

namespace ProtoBuf.CodeGen;

internal class CodeGenField
{
    public CodeGenField(int fieldNumber, string name)
    {
        FieldNumber = fieldNumber;
        BackingName = OriginalName = Name = name?.Trim() ?? "";;
    }
    public int FieldNumber { get; }
    public CodeGenType Type { get; set; } = CodeGenUnknownType.Instance;

    public bool ShouldSerializeType() => false;

    public string? TypeName => Type?.Serialize();

    public string Name { get; set; }
    public string BackingName { get; set; } // the name to use when generating an explicit field
    public string OriginalName { get; set; } // the name in the schema (not used in code, except in the [ProtoMember(..., Name = ...)]

    public bool ShouldSerializeOriginalName() => OriginalName != Name;
    public bool ShouldSerializeBackingName() => BackingName != Name;

    internal static CodeGenField Parse(FieldDescriptorProto field, CodeGenContext context)
    {
        var name = context.NameNormalizer.GetName(field);
        var newField = new CodeGenField(field.Number, name)
        {
            OriginalName = field.Name,
            Type = field.type switch
            {
                FieldDescriptorProto.Type.TypeString => CodeGenSimpleType.String,
                FieldDescriptorProto.Type.TypeEnum or FieldDescriptorProto.Type.TypeMessage => context.GetContractType(field.TypeName),
                _ => throw new NotImplementedException($"type not handled: {field.type}"),
            },
        };

        return newField;
    }
}
