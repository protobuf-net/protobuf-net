#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ProtoBuf.BuildTools.Internal;

internal static class CodeGenSemanticModelParser
{
    [ThreadStatic]
    private static Stack<ISymbol> s_SpareQueue;

    private static Stack<ISymbol> GetStack() => s_SpareQueue ?? new();
    private static void RecycleStack(Stack<ISymbol> value)
    {
        if (value is not null && s_SpareQueue is null)
        {
            value.Clear();
            s_SpareQueue = value;
        }
    }
    internal static void Parse(CodeGenSet set, ISymbol symbol)
    {
        CodeGenFile? file = null;
        var pending = GetStack(); // prevent deep thread-stack dive by using an explicit stack
        pending.Push(symbol);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (current is ITypeSymbol type)
            {
                Debug.WriteLine(type.Name);
                switch (type.TypeKind)
                {
                    case TypeKind.Struct:
                    case TypeKind.Class:
                        var attribs = type.GetAttributes();
                        if (IsProtoContract(attribs))
                        {
                            if (file is null)
                            {
                                var firstPath = symbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree?.FilePath;
                                if (string.IsNullOrWhiteSpace(firstPath)) firstPath = "unknown";

                                file = new CodeGenFile(firstPath!);
                                set.Files.Add(file);
                            }
                            ParseMessage(set, file, type, attribs);
                        }
                        break;
                }
            }

            if (current is INamespaceOrTypeSymbol container)
            {
                foreach (var tm in container.GetMembers())
                {
                    switch (tm.Kind)
                    {
                        case SymbolKind.Namespace:
                        case SymbolKind.NamedType:
                            pending.Push(tm);
                            break;
                    }
                    
                }
            }
        }
        RecycleStack(pending);
    }

    private static void ParseMessage(CodeGenSet set, CodeGenFile file, ITypeSymbol type, ImmutableArray<AttributeData> attribs)
    {
        var obj = new CodeGenMessage(type.Name, GetFullyQualifiedPrefix(type));
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
                        if (attr.AttributeConstructor?.Parameters is { Length:> 0 })
                        {
                            throw new InvalidOperationException("Unexpected parameters for " + ac.Name);
                        }
                        foreach (var namedArg in attr.NamedArguments)
                        {
                            switch (namedArg.Key)
                            {
                                case nameof(ProtoContractAttribute.Name) when namedArg.Value.TryGetString(out var s):
                                    obj.OriginalName = s;
                                    break;
                                default:
                                    throw new InvalidOperationException($"Unexpected named arg: {ac.Name}.{namedArg.Key}");
                            }
                        }
                        break;
                }
            }
        }
        file.Messages.Add(obj);
    }

    [ThreadStatic]
    private static Stack<(string Name, string Token)>? s_FQN_Stack;
    private static string GetFullyQualifiedPrefix(ITypeSymbol type)
    {
        static bool IsAnticipated(SymbolKind kind)
            => kind == SymbolKind.Namespace || kind == SymbolKind.NamedType;

        static string GetToken(SymbolKind kind) => kind == SymbolKind.Namespace ? "." : "+";

        var symbol = type?.ContainingSymbol;

        var stack = s_FQN_Stack ?? new();
        s_FQN_Stack = null; // in case of re-entrancy
        int len = 0;
        while (symbol is not null && IsAnticipated(symbol.Kind))
        {
            if (!string.IsNullOrWhiteSpace(symbol.Name))
            {
                stack.Push((symbol.Name, GetToken(symbol.Kind)));
            }
            len += symbol.Name.Length + 1;
            symbol = symbol.ContainingSymbol;
        }

        string result;
        switch (stack.Count)
        {
            case 0:
                result = "";
                break;
            case 1:
                var tmp = stack.Pop();
                result = tmp.Name + tmp.Token;
                break;
            default:
                var sb = new StringBuilder(len);
                while (stack.Count > 0)
                {
                    tmp = stack.Pop();
                    sb.Append(tmp.Name).Append(tmp.Token);
                }
                result = sb.ToString();
                break;
        }
        s_FQN_Stack = stack;
        return result;
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
