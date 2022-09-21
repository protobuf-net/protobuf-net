#nullable enable

using Google.Protobuf.Reflection;
using System;
using System.ComponentModel;

namespace ProtoBuf.Reflection.Internal.CodeGen;

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
    [DefaultValue(false)]
    public bool IsRepeated { get; set; }
    [DefaultValue(false)]
    public bool TrackFieldPresence { get; set; }
    [DefaultValue(false)]
    public bool AsReference { get; set; }
    [DefaultValue(false)]
    public bool AsDynamicType { get; set; }
    [DefaultValue(false)]
    public bool IsRequired { get; set; }
    [DefaultValue(false)]
    public bool IsPacked { get; set; }
    [DefaultValue(false)]
    public bool IsDeprecated { get; internal set; }
    [DefaultValue(Access.Public)]
    public Access Access { get; set; } = Access.Public;

    public bool ShouldSerializeOriginalName() => OriginalName != Name;
    public bool ShouldSerializeBackingName() => BackingName != Name;

    internal static CodeGenField Parse(FieldDescriptorProto field, CodeGenParseContext context)
    {
        var name = context.NameNormalizer.GetName(field);
        var newField = new CodeGenField(field.Number, name)
        {
            OriginalName = field.Name,
            Type = field.type switch
            {
                FieldDescriptorProto.Type.TypeString => CodeGenSimpleType.String,
                FieldDescriptorProto.Type.TypeFloat => CodeGenSimpleType.Float,
                FieldDescriptorProto.Type.TypeDouble => CodeGenSimpleType.Double,
                FieldDescriptorProto.Type.TypeInt32 => CodeGenSimpleType.Int32,
                FieldDescriptorProto.Type.TypeSint32 => CodeGenSimpleType.SInt32,
                FieldDescriptorProto.Type.TypeUint32=> CodeGenSimpleType.UInt32,
                FieldDescriptorProto.Type.TypeInt64 => CodeGenSimpleType.Int64,
                FieldDescriptorProto.Type.TypeSint64 => CodeGenSimpleType.SInt64,
                FieldDescriptorProto.Type.TypeUint64 => CodeGenSimpleType.UInt64,
                FieldDescriptorProto.Type.TypeBool => CodeGenSimpleType.Boolean,
                FieldDescriptorProto.Type.TypeBytes => CodeGenSimpleType.Bytes,
                FieldDescriptorProto.Type.TypeFixed32 => CodeGenSimpleType.Fixed32,
                FieldDescriptorProto.Type.TypeSfixed32 => CodeGenSimpleType.SFixed32,
                FieldDescriptorProto.Type.TypeFixed64 => CodeGenSimpleType.Fixed64,
                FieldDescriptorProto.Type.TypeSfixed64 => CodeGenSimpleType.SFixed64,
                FieldDescriptorProto.Type.TypeEnum or FieldDescriptorProto.Type.TypeMessage or FieldDescriptorProto.Type.TypeGroup
                    => context.GetContractType(field.TypeName),
                _ => throw new NotImplementedException($"type not handled: {field.type}"),
            },
        };

        return newField;
    }
}
