#nullable enable
using Google.Protobuf.Reflection;
using System.Collections.Generic;
using System.ComponentModel;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenMessage : CodeGenType
{
    internal CodeGenMessage(string name, string fullyQualifiedPrefix) : base(name, fullyQualifiedPrefix)
    {
        OriginalName = base.Name;
    }

    private List<CodeGenMessage>? _messages;
    private List<CodeGenEnum>? _enums;
    private List<CodeGenField>? _fields;
    public List<CodeGenMessage> Messages => _messages ??= new();
    public List<CodeGenEnum> Enums => _enums ??= new();
    public List<CodeGenField> Fields => _fields ??= new();

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

    [DefaultValue(Access.Public)]
    public Access Access { get; set; } = Access.Public;

    public bool ShouldSerializeOriginalName() => OriginalName != Name;
    public bool ShouldSerializePackage() => !string.IsNullOrWhiteSpace(Package);


    internal static CodeGenMessage Parse(DescriptorProto message, string fullyQualifiedPrefix, CodeGenParseContext context, string package)
    {
        var name = context.NameNormalizer.GetName(message);
        var newMessage = new CodeGenMessage(name, fullyQualifiedPrefix);
        context.Register(message.FullyQualifiedName, newMessage);
        newMessage.OriginalName = message.Name;
        newMessage.Package = package;

        if (message.Fields.Count > 0)
        {
            int nextFieldTrackingIndex = 0;
            foreach (var field in message.Fields)
            {
                var parsed = CodeGenField.Parse(field, context);
                if (parsed.Conditional == ConditionalKind.FieldPresence)
                {
                    parsed.FieldPresenceIndex = nextFieldTrackingIndex++;
                }
                newMessage.Fields.Add(parsed);
            }
        }

        if (message.NestedTypes.Count > 0 || message.EnumTypes.Count > 0)
        {
            var prefix = newMessage.FullyQualifiedPrefix + newMessage.Name + "+";
            foreach (var type in message.NestedTypes)
            {
                newMessage.Messages.Add(CodeGenMessage.Parse(type, prefix, context, package));
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
            foreach (var field in Fields)
            {
                if (context.FixupPlaceholder(field.Type, out var found))
                {
                    field.Type = found;
                }
            }
        }
    }
}
