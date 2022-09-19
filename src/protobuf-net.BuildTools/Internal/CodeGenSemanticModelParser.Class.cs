using Microsoft.CodeAnalysis;
using ProtoBuf.BuildTools.Internal;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;
using System.Collections.Immutable;

namespace ProtoBuf.Internal;

internal static partial class CodeGenSemanticModelParser
{
    private static object ParseType(CodeGenFile file, object parentGenType, ITypeSymbol typeSymbol)
    {
        var attribs = typeSymbol.GetAttributes();
        if (IsProtoContract(attribs))
        {
            var codeGenMessage = ParseMessage(typeSymbol, attribs);

            if (parentGenType is CodeGenMessage parentGenMessage)
            {
                parentGenMessage.Messages.Add(codeGenMessage);
            }
            else
            {
                file.Messages.Add(codeGenMessage);
            }
            
            return codeGenMessage;
        }

        return null;
    }

    private static CodeGenMessage ParseMessage(ITypeSymbol typeSymbol, ImmutableArray<AttributeData> attribs)
    {
        var codeGenMessage = new CodeGenMessage(typeSymbol.Name, GetFullyQualifiedPrefix(typeSymbol))
        {
            Package = _namespaceName 
        };
        
        foreach (var attr in attribs)
        {
            var ac = attr.AttributeClass;
            if (ac is null) continue;
            if (ac.InProtoBufNamespace())
            {
                // we expect to recognize and handle all of these; if not: bomb (or better: report an error cleanly)
                switch (ac.Name)
                {
                    case nameof(ProtoContractAttribute):
                        if (attr.AttributeConstructor?.Parameters is { Length: > 0 })
                        {
                            throw new InvalidOperationException("Unexpected parameters for " + ac.Name);
                        }
                        foreach (var namedArg in attr.NamedArguments)
                        {
                            switch (namedArg.Key)
                            {
                                case nameof(ProtoContractAttribute.Name) when namedArg.Value.TryGetString(out var s):
                                    codeGenMessage.OriginalName = s;
                                    break;
                                default:
                                    throw new InvalidOperationException($"Unexpected named arg: {ac.Name}.{namedArg.Key}");
                            }
                        }
                        break;
                }
            }
        }
        
        return codeGenMessage;
    }

    private static bool IsProtoContract(ImmutableArray<AttributeData> attribs)
    {
        foreach (var attr in attribs)
        {
            var ac = attr.AttributeClass;
            if (ac?.Name == nameof(ProtoContractAttribute) && ac.InProtoBufNamespace())
                return true;
        }
        return false;
    }
}
