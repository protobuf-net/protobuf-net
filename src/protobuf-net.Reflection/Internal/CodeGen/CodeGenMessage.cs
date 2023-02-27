#nullable enable
using Google.Protobuf.Reflection;
using System.Collections.Generic;
using System.ComponentModel;
using ProtoBuf.Reflection.Internal.CodeGen.Collections;
using ProtoBuf.Internal.CodeGen;
using System;

namespace ProtoBuf.Reflection.Internal.CodeGen;

[Flags]
internal enum CodeGenGenerate
{
    None = 0,

    DataContract = 1 << 0, // anything at all
    DataContractFeatures = 1 << 1, // attribs etc
    DataField = 1 << 2,
    DataSerializer = 1 << 3,
    DataConstructor = 1 << 4,

    ServiceContract = 1 << 16, // anything at all
    ServiceContractFeatures = 1 << 17, // attribs etc
    ServiceOperation = 1 << 18, // methods
    ServiceProxy = 1 << 19,

    All = ~None,
}
internal class CodeGenMessage : CodeGenLocatedType
{
    [DefaultValue(CodeGenGenerate.All)]
    public CodeGenGenerate Emit { get; set; } = CodeGenGenerate.All;
    internal CodeGenMessage(string name, string fullyQualifiedPrefix, object? origin) : base(name, fullyQualifiedPrefix, origin)
    {
        OriginalName = base.Name;
    }

    private NonNullableList<CodeGenMessage>? _messages;
    private NonNullableList<CodeGenEnum>? _enums;
    private NonNullableList<CodeGenField>? _fields;
    public ICollection<CodeGenMessage> Messages => _messages ??= new();
    public ICollection<CodeGenEnum> Enums => _enums ??= new();
    public ICollection<CodeGenField> Fields => _fields ??= new();

    public bool ShouldSerializeMessages() => _messages is { Count: > 0 };
    public bool ShouldSerializeEnums() => _enums is { Count: > 0 };
    public bool ShouldSerializeFields() => _fields is { Count: > 0 };

    public new string Name
    {   // make setter public
        get => base.Name;
        set => base.Name = value;
    }
    public string OriginalName { get; set; }
    public string Package { get; set; } = "";

    [DefaultValue(false)]
    public bool IsDeprecated { get; set; }

    [DefaultValue(false)]
    public bool IsValueType { get; set; }
    [DefaultValue(false)]
    public bool IsReadOnly { get; set; }

    [DefaultValue(Access.Public)]
    public Access Access { get; set; } = Access.Public;

    public bool ShouldSerializeOriginalName() => OriginalName != Name;
    public bool ShouldSerializePackage() => !string.IsNullOrWhiteSpace(Package);


    internal static CodeGenMessage Parse(DescriptorProto message, string fullyQualifiedPrefix, CodeGenParseContext context, string package)
    {
        var name = context.NameNormalizer.GetName(message);
        var newMessage = new CodeGenMessage(name, fullyQualifiedPrefix, message);
        context.Register(message.FullyQualifiedName, newMessage);
        newMessage.OriginalName = message.Name;
        newMessage.Package = package;

        if (message.Fields.Count > 0)
        {
            foreach (var field in message.Fields)
            {
                newMessage.Fields.Add(CodeGenField.Parse(field, context));
            }
        }

        if (message.NestedTypes.Count > 0 || message.EnumTypes.Count > 0)
        {
            var prefix = newMessage.FullyQualifiedPrefix + newMessage.Name + "+";
            foreach (var type in message.NestedTypes)
            {
                if (!context.AddMapEntry(type))
                {
                    newMessage.Messages.Add(CodeGenMessage.Parse(type, prefix, context, package));
                }
            }
            foreach (var type in message.EnumTypes)
            {
                newMessage.Enums.Add(CodeGenEnum.Parse(type, prefix, context, package));
            }
        }

        return newMessage;
    }

    internal void FixupPlaceholders(CodeGenParseContext context)
    {
        if (ShouldSerializeFields())
        {
            int nextTrackingIndex = 0;
            foreach (var field in Fields)
            {
                if (context.FixupPlaceholder(field.Type, out var found))
                {
                    field.Type = found;
                }
                if (field.Conditional == ConditionalKind.FieldPresence)
                {
                    if (field.Type is CodeGenMessage msg && !msg.IsValueType)
                    {
                        field.Conditional = ConditionalKind.Always; // uses null for tracking
                    }
                    else
                    {
                        field.FieldPresenceIndex = nextTrackingIndex++;
                    }
                }
                if (field.IsRepeated && field.Type is CodeGenMapEntryType)
                {
                    field.Repeated = RepeatedKind.Dictionary;
                }
                if (field.DefaultValue == "" && field.Type is CodeGenMessage)
                {
                    field.DefaultValue = null;
                    if (field.Conditional == ConditionalKind.NonDefault)
                    {
                        field.Conditional = ConditionalKind.Always; // uses null for tracking
                    }
                }
            }
        }
    }
}
