#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;

namespace ProtoBuf.BuildTools.Internal
{
    internal static class Utils
    {
        internal static ImmutableArray<DiagnosticDescriptor> GetDeclared(Type type)
        {
            var fields = type?.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (fields is null || fields.Length == 0) return ImmutableArray<DiagnosticDescriptor>.Empty;

            var builder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>(fields.Length);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(DiagnosticDescriptor) && field.GetValue(null) is DiagnosticDescriptor descriptor)
                {
                    builder.Add(descriptor);
                }
            }
            return builder.ToImmutable();
        }

        internal static Location PickLocation(ref SyntaxNodeAnalysisContext context, Location? preferred)
            => preferred ?? context.Node.GetLocation();

        internal static Location PickLocation(ref SyntaxNodeAnalysisContext context, ISymbol? preferred)
        {
            if (preferred is not null)
            {
                var locs = preferred.Locations;
                if (!locs.IsEmpty) return locs[0];
            }
            return context.Node.GetLocation();
        }

        internal const string ProtoBufNamespace = "ProtoBuf";

        internal static bool InProtoBufNamespace(this INamedTypeSymbol symbol)
            => InNamespace(symbol, ProtoBufNamespace);

        internal static bool InNamespace(this INamedTypeSymbol symbol, string ns0)
        {
            var cs = symbol.ContainingNamespace;
            return cs.Name == ns0 && cs.ContainingNamespace.IsGlobalNamespace;
        }

        internal static bool InNamespace(this INamedTypeSymbol symbol, string ns0, string ns1)
        {
            var ns = symbol.ContainingNamespace;
            if (ns.Name != ns1) return false;
            ns = ns.ContainingNamespace;
            return ns.Name == ns0 && ns.ContainingNamespace.IsGlobalNamespace;
        }

        internal static bool InNamespace(this INamedTypeSymbol symbol, string ns0, string ns1, string ns2)
        {
            var ns = symbol.ContainingNamespace;
            if (ns.Name != ns2) return false;
            ns = ns.ContainingNamespace;
            if (ns.Name != ns1) return false;
            ns = ns.ContainingNamespace;
            return ns.Name == ns0 && ns.ContainingNamespace.IsGlobalNamespace;
        }

        internal static Location? FirstBlame<T>(this IEnumerable<T>? source) where T : IBlame
        {
            if (source is not null)
            {
                foreach (var item in source)
                {
                    var blame = item?.Blame;
                    if (blame is not null) return blame;
                }
            }
            return null;
        }
        internal static bool TryGetByName(this AttributeData attributeData, string name, out TypedConstant value)
        {
            // because named args happen *after* the .ctor, they take precedence - check them first
            foreach (var pair in attributeData.NamedArguments)
            {
                if (string.Equals(pair.Key, name, StringComparison.InvariantCultureIgnoreCase))
                {
                    value = pair.Value;
                    return true;
                }
            }

            var args = attributeData.ConstructorArguments;
            if (args.Length != 0)
            {
                var ctor = attributeData.AttributeConstructor;
                if (ctor is not null)
                {
                    int i = 0;
                    foreach (var parameter in ctor.Parameters)
                    {
                        if (string.Equals(parameter.Name, name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            value = args[i];
                            return true;
                        }
                        i++;
                    }
                }
            }

            value = default;
            return false;
        }
        internal static bool TryGetStringByName(this AttributeData attributeData, string name, out string value)
        {
            if (TryGetByName(attributeData, name, out var raw) && raw.Kind == TypedConstantKind.Primitive && raw.Value is string s)
            {
                value = s;
                return true;
            }
            value = default!;
            return false;
        }

        internal static bool TryGetInt32ByName(this AttributeData attributeData, string name, out int value)
        {
            if (TryGetByName(attributeData, name, out var raw) && raw.Kind == TypedConstantKind.Primitive && raw.Value is int i)
            {
                value = i;
                return true;
            }
            value = 0;
            return false;
        }

        internal static bool TryGetBooleanByName(this AttributeData attributeData, string name, out bool value)
        {
            if (TryGetByName(attributeData, name, out var raw) && raw.Kind == TypedConstantKind.Primitive && raw.Value is bool b)
            {
                value = b;
                return true;
            }
            value = false;
            return false;
        }

        internal static bool TryGetTypeByName(this AttributeData attributeData, string name, out ITypeSymbol value)
        {
            if (TryGetByName(attributeData, name, out var raw) && raw.Kind == TypedConstantKind.Type && raw.Value is ITypeSymbol ts)
            {
                value = ts;
                return true;
            }
            value = default!;
            return false;
        }

        internal static Location? GetLocation(this AttributeData attribute, ISymbol? fallback)
        {
            var syntax = attribute.ApplicationSyntaxReference;
            if (syntax == null)
            {
                if (fallback is null) return null;
                var locs = fallback.Locations;
                return locs.IsEmpty ? null : locs[0];
            }
            return syntax.SyntaxTree.GetLocation(syntax.Span);
        }

        internal static string Qualified(this INamespaceSymbol ns, string type)
            => (ns is null || ns.IsGlobalNamespace) ? type : (ns.ToDisplayString() + "." + type);

        internal static Version? GetProtobufNetVersion(this Compilation compilation)
            => compilation.GetReferenceVersion("protobuf-net.Core")
            ?? compilation.GetReferenceVersion("protobuf-net")
            ?? compilation.GetReferenceVersion("protobuf-net.BuildTools"); // this last only used from tests

        internal static Version? GetReferenceVersion(this Compilation compilation, string name)
        {
            if (compilation is not null)
            {
                foreach (var ran in compilation.ReferencedAssemblyNames)
                {
                    if (string.Equals(name, ran.Name, StringComparison.InvariantCultureIgnoreCase))
                        return ran.Version;
                }
            }
            return null;
        }
    }
}
