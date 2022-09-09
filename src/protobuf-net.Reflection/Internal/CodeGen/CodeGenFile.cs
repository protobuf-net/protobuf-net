#nullable enable
using System.Collections.Generic;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal class CodeGenFile
{
    public string Name { get; }
    public CodeGenFile(string name)
    {
        Name = name;
    }

    private List<CodeGenMessage>? _messages;
    private List<CodeGenEnum>? _enums;
    public List<CodeGenMessage> Messages => _messages ??= new();
    public List<CodeGenEnum> Enums => _enums ??= new();

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
    }
}
