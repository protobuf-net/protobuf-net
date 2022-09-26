#nullable enable

using Google.Protobuf.Reflection;
using ProtoBuf.Meta;
using System;
using System.ComponentModel;
using System.Text;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenField
{
    public CodeGenField(int fieldNumber, string name)
    {
        FieldNumber = fieldNumber;
        BackingName = OriginalName = Name = name?.Trim() ?? "";;
    }
    public const string PresenceTrackingFieldName = "__pbn_field_presence_";
    public int FieldNumber { get; }
    public CodeGenType Type { get; set; } = CodeGenUnknownType.Instance;

    public bool ShouldSerializeType() => false;

    public string? TypeName => Type?.Serialize();

    public string Name { get; set; }
    public string BackingName { get; set; } // the name to use when generating an explicit field
    public string OriginalName { get; set; } // the name in the schema (not used in code, except in the [ProtoMember(..., Name = ...)]

    [DefaultValue(RepeatedKind.Single)]
    public RepeatedKind Repeated { get; set; }

    public bool IsRepeated => Repeated != RepeatedKind.Single;

    public bool ShouldSerializeIsRepeated() => false;
    
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

    [DefaultValue(ConditionalKind.Always)]
    public ConditionalKind Conditional { get; set; } = ConditionalKind.Always;

    [DefaultValue(-1)]
    public int FieldPresenceIndex { get; set; } = -1;
    public string? DefaultValue { get; set; }
    [DefaultValue(false)]
    public bool IsGroup { get; set; }

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
            DefaultValue = field.DefaultValue,
        };
        
        switch (field.label)
        {
            case FieldDescriptorProto.Label.LabelRequired:
                newField.IsRequired = true;
                break;
            case FieldDescriptorProto.Label.LabelOptional when (field.Proto3Optional || IsProto2(field.Parent as IType)):
                newField.IsRequired = false;
                newField.Conditional = IsRefType(newField.Type) ? ConditionalKind.Always : ConditionalKind.FieldPresence;
                break;
            case FieldDescriptorProto.Label.LabelOptional:
                newField.IsRequired = false;
                newField.Conditional = IsRefType(newField.Type) ? ConditionalKind.Always : ConditionalKind.NonDefault;
                break;
            case FieldDescriptorProto.Label.LabelRepeated:
                newField.Repeated = RepeatedKind.List;
                if (newField.Type.IsWellKnownType(out var type))
                {
                    switch (field.type)
                    {
                        case FieldDescriptorProto.Type.TypeBool:
                        case FieldDescriptorProto.Type.TypeDouble:
                        case FieldDescriptorProto.Type.TypeFixed32:
                        case FieldDescriptorProto.Type.TypeFixed64:
                        case FieldDescriptorProto.Type.TypeFloat:
                        case FieldDescriptorProto.Type.TypeInt32:
                        case FieldDescriptorProto.Type.TypeInt64:
                        case FieldDescriptorProto.Type.TypeSfixed32:
                        case FieldDescriptorProto.Type.TypeSfixed64:
                        case FieldDescriptorProto.Type.TypeSint32:
                        case FieldDescriptorProto.Type.TypeSint64:
                        case FieldDescriptorProto.Type.TypeUint32:
                        case FieldDescriptorProto.Type.TypeUint64:
                            newField.Repeated = RepeatedKind.Array;
                            break;
                    }
                }
                
                break;
        }

        static bool IsRefType(CodeGenType type)
        {
            if (type.IsWellKnownType(out var known))
            {
                return known switch
                {
                    CodeGenWellKnownType.String or CodeGenWellKnownType.Bytes or CodeGenWellKnownType.NetObjectProxy => true,
                    _ => false,
                };
            }
            return type is CodeGenMessage msg && !msg.IsValueType;
        }

        return newField;

        static bool IsProto2(IType? obj)
        {
            while (obj != null)
            {
                if (obj is FileDescriptorProto fdp)
                {
                    return string.IsNullOrWhiteSpace(fdp.Syntax) || fdp.Syntax == FileDescriptorProto.SyntaxProto2;
                }
                obj = obj.Parent;
            }
            return false;
        }
    }

    internal bool TryGetPresence(out int fieldIndex, out int mask)
    {
        var fpi = FieldPresenceIndex;
        if (fpi >= 0)
        {
            fieldIndex = fpi >> 5;
            mask = 1 << (fpi & 0b11111);
            return true;
        }
        else
        {
            fieldIndex = mask = 0;
            return false;
        }
    }
}

internal enum ConditionalKind
{
    /// <summary>The value is always written (unless not possible)</summary>
    Always,
    /// <summary>The corresponding <c>ShouldSerialize*()</c> method is used for conditionality</summary>
    ShouldSerializeMethod,
    /// <summary>The value is written only if it differs from the default value</summary>
    NonDefault,
    /// <summary>Active assignment is tracked and used for conditionality</summary>
    FieldPresence,
    NullableT,
}

internal enum RepeatedKind
{
    Single,
    List,
    Array,
}