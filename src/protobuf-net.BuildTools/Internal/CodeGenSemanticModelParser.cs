#nullable enable
using Microsoft.CodeAnalysis;
using ProtoBuf.Reflection.Internal.CodeGen;
using System;
using System.Linq;
using System.Collections.Generic;

namespace ProtoBuf.Internal;

internal static partial class CodeGenSemanticModelParser
{
    [ThreadStatic]
    private static Stack<(ISymbol compilerSymbol, object codeGenType)> s_SpareQueue;

    private static Stack<(ISymbol compilerSymbol, object codeGenType)> GetStack() => s_SpareQueue ?? new();

    private static void RecycleStack(Stack<(ISymbol compilerSymbol, object codeGenType)> value)
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
        object? currentCodeGenType = null;
        
        var pending = GetStack(); // prevent deep thread-stack dive by using an explicit stack
        pending.Push((compilerSymbol: symbol, codeGenType: null)!);
        
        while (pending.Count > 0)
        {
            var (currentSymbol, parentGenType) = pending.Pop();
            if (currentSymbol is ITypeSymbol type)
            {
                switch (type.TypeKind)
                {
                    case TypeKind.Struct:
                    case TypeKind.Class:
                        if (file is null)
                        {
                            var firstPath = currentSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree?.FilePath;
                            if (string.IsNullOrWhiteSpace(firstPath)) firstPath = "unknown";

                            file = new CodeGenFile(firstPath!);
                            set.Files.Add(file);
                        }
                        
                        currentCodeGenType = ParseType(file, symbol, type);
                        break;
                }
            }

            if (currentSymbol is IPropertySymbol property)
            {
                currentCodeGenType = ParseProperty(parentGenType as CodeGenMessage, currentSymbol, property);
                break;
            }

            if (currentSymbol is INamespaceSymbol namespaceSymbol)
            {
                if (!string.IsNullOrEmpty(namespaceSymbol.Name))
                {
                    _namespaceName = namespaceSymbol.Name;
                }
            }

            if (currentSymbol is INamespaceOrTypeSymbol container)
            {
                foreach (var typeMember in container.GetMembers())
                {
                    switch (typeMember.Kind)
                    {
                        case SymbolKind.Namespace:
                        case SymbolKind.NamedType:
                        case SymbolKind.Property:
                            pending.Push((compilerSymbol: typeMember, codeGenType: currentCodeGenType)!);
                            break;
                    }

                }
            }
        }

        RecycleStack(pending);
    }
}
