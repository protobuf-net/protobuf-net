#nullable enable
using System.Collections.Generic;
using ProtoBuf.Reflection.Internal.CodeGen.Collections;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenFile
{
    public string Name { get; internal set; }
    public CodeGenFile(string name)
    {
        Name = name;
    }

    private NonNullableList<CodeGenService>? _services;
    private NonNullableList<CodeGenMessage>? _messages;
    private NonNullableList<CodeGenEnum>? _enums;

    public ICollection<CodeGenService> Services => _services ??= new();
    public ICollection<CodeGenMessage> Messages => _messages ??= new();
    public ICollection<CodeGenEnum> Enums => _enums ??= new();

    public bool ShouldSerializeServices() => _services is { Count: > 0 };
    public bool ShouldSerializeMessages() => _messages is { Count: > 0 };
    public bool ShouldSerializeEnums() => _enums is { Count: > 0 };

    internal void FixupPlaceholders(CodeGenParseContext context)
    {
        if (ShouldSerializeMessages())
        {
            foreach (var message in Messages)
            {
                message.FixupPlaceholders(context);
            }
        }
        
        if (ShouldSerializeServices())
        {
            foreach (var service in Services)
            {
                service.FixupPlaceholders(context);
            }
        }
    }
}
