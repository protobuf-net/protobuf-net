using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf.Internal;

internal static partial class CodeGenSemanticModelParser
{
    [ThreadStatic] private static Stack<(string Name, string Token)>? s_FQN_Stack;

    private static string GetFullyQualifiedPrefix(ISymbol type)
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
}