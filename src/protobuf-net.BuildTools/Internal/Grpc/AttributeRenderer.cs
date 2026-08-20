#nullable enable
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Text;

namespace ProtoBuf.BuildTools.Internal.Grpc
{
    /// <summary>What can be done with one attribute when reconstructing endpoint metadata.</summary>
    internal enum AttributeRenderKind
    {
        /// <summary>Rendered to a C# expression constructing an equivalent instance.</summary>
        Rendered,

        /// <summary>Compiler-synthesised noise; deliberately dropped without comment.</summary>
        Skipped,

        /// <summary>Cannot be constructed from the consuming assembly; the caller must report it.</summary>
        Unsupported,
    }

    /// <summary>
    /// Turns an <see cref="AttributeData"/> back into source that constructs an equivalent instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists because gRPC endpoint metadata is how authorization is enforced - <c>[Authorize]</c>
    /// and friends reach ASP.NET Core as attribute <em>instances</em> - so reconstructing it at compile
    /// time means genuinely constructing those objects, not merely naming them.
    /// </para>
    /// <para>
    /// Exactness is not the bar, and chasing it is what stalled this work before. The runtime list also
    /// contains compiler-synthesised attributes (the <c>NullableContext</c> family), whose types Roslyn
    /// emits as <c>internal</c> per assembly and which are therefore unconstructable from anywhere else -
    /// and which ASP.NET Core does not consume, so dropping them loses nothing. The bar is: reproduce
    /// what carries meaning, and be <em>loud</em> about anything else.
    /// </para>
    /// </remarks>
    internal static class AttributeRenderer
    {
        /// <summary>
        /// Compiler-synthesised attributes, dropped silently.
        /// </summary>
        /// <remarks>
        /// Silent rather than reported: these ride along on almost every member carrying nullable
        /// annotations, so warning about them would train a reader to ignore the diagnostic that matters.
        /// </remarks>
        private static readonly HashSet<string> s_synthesised = new(System.StringComparer.Ordinal)
        {
            "System.Runtime.CompilerServices.NullableAttribute",
            "System.Runtime.CompilerServices.NullableContextAttribute",
            "System.Runtime.CompilerServices.NativeIntegerAttribute",
            "System.Runtime.CompilerServices.DynamicAttribute",
            "System.Runtime.CompilerServices.TupleElementNamesAttribute",
            "System.Runtime.CompilerServices.IsReadOnlyAttribute",
            "System.Runtime.CompilerServices.RefSafetyRulesAttribute",
            "System.Runtime.CompilerServices.CompilerGeneratedAttribute",
        };

        /// <summary>
        /// Whether a name is one of the compiler-synthesised attributes dropped silently.
        /// </summary>
        /// <remarks>
        /// Exposed so the metadata oracle can filter the <em>reflective</em> list by the same rule: at
        /// run time these are present, while Roslyn does not surface them at all (they are added at
        /// emit), so without this the two sides differ by construction. One list, two callers.
        /// </remarks>
        public static bool IsSynthesised(string fullName) => s_synthesised.Contains(fullName);

        public static AttributeRenderKind TryRender(Compilation compilation, AttributeData attribute,
            out string? expression, out string? reason)
        {
            expression = reason = null;

            if (attribute.AttributeClass is not INamedTypeSymbol type)
            {
                reason = "its type could not be resolved";
                return AttributeRenderKind.Unsupported;
            }

            if (s_synthesised.Contains(type.ToDisplayString())) return AttributeRenderKind.Skipped;

            // IsSymbolAccessibleWithin answers the InternalsVisibleTo question for free: an internal
            // attribute the consumer *can* see passes, one it cannot is excluded, and neither needs a
            // special case.
            if (!compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
            {
                reason = $"'{type.ToDisplayString()}' is not accessible from this assembly";
                return AttributeRenderKind.Unsupported;
            }
            if (attribute.AttributeConstructor is not IMethodSymbol ctor
                || !compilation.IsSymbolAccessibleWithin(ctor, compilation.Assembly))
            {
                reason = $"the constructor used for '{type.ToDisplayString()}' is not accessible here";
                return AttributeRenderKind.Unsupported;
            }

            var sb = new StringBuilder("new ")
                .Append(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append('(');
            for (int i = 0; i < attribute.ConstructorArguments.Length; i++)
            {
                if (i != 0) sb.Append(", ");

                // the declared parameter type is passed so a null can be *typed*: a bare `null` does not
                // bind when the attribute has overloaded constructors, and the result is CS0121 in the
                // consumer's build - found by the metadata oracle, which compiles what it renders
                var parameterType = i < ctor.Parameters.Length ? ctor.Parameters[i].Type : null;
                if (!TryValue(compilation, attribute.ConstructorArguments[i], sb, out reason, parameterType))
                {
                    return AttributeRenderKind.Unsupported;
                }
            }
            sb.Append(')');

            if (attribute.NamedArguments.Length != 0)
            {
                sb.Append(" { ");
                for (int i = 0; i < attribute.NamedArguments.Length; i++)
                {
                    if (i != 0) sb.Append(", ");
                    sb.Append(attribute.NamedArguments[i].Key).Append(" = ");
                    if (!TryValue(compilation, attribute.NamedArguments[i].Value, sb, out reason))
                    {
                        return AttributeRenderKind.Unsupported;
                    }
                }
                sb.Append(" }");
            }

            expression = sb.ToString();
            return AttributeRenderKind.Rendered;
        }

        /// <summary>Renders one argument, or explains why it cannot be.</summary>
        private static bool TryValue(Compilation compilation, TypedConstant value, StringBuilder sb,
            out string? reason, ITypeSymbol? parameterType = null)
        {
            reason = null;

            // a null in *constructor* position is written as default(T), which both binds an overload and
            // works for a Nullable<T> parameter, where (T)null would not. A null named argument needs no
            // such help: assigning to a property involves no overload resolution.
            if (value.IsNull && parameterType is not null)
            {
                sb.Append("default(")
                  .Append(parameterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                  .Append(')');
                return true;
            }

            switch (value.Kind)
            {
                case TypedConstantKind.Error:
                    reason = "an argument could not be resolved";
                    return false;

                case TypedConstantKind.Type:
                    if (value.Value is not ITypeSymbol named)
                    {
                        sb.Append("null");
                        return true;
                    }
                    // typeof(X) is only usable if X can be named from here at all
                    if (!compilation.IsSymbolAccessibleWithin(named, compilation.Assembly))
                    {
                        reason = $"a typeof() argument names '{named.ToDisplayString()}', not accessible here";
                        return false;
                    }
                    sb.Append("typeof(")
                      .Append(named.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(')');
                    return true;

                case TypedConstantKind.Enum:
                    // the underlying value with a cast, rather than a member name: a flags combination
                    // may have no single name, and the cast is correct either way
                    sb.Append('(').Append(value.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                      .Append(')').Append(Convert.ToString(value.Value, CultureInfo.InvariantCulture));
                    return true;

                case TypedConstantKind.Array:
                    if (value.IsNull) { sb.Append("null"); return true; }
                    sb.Append("new ")
                      .Append(value.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).Append(" { ");
                    for (int i = 0; i < value.Values.Length; i++)
                    {
                        if (i != 0) sb.Append(", ");
                        if (!TryValue(compilation, value.Values[i], sb, out reason)) return false;
                    }
                    sb.Append(" }");
                    return true;

                case TypedConstantKind.Primitive:
                    if (value.IsNull) { sb.Append("null"); return true; }
                    sb.Append(Literal(value.Value!));
                    return true;

                default:
                    reason = "an argument is of an unsupported kind";
                    return false;
            }
        }

        /// <summary>
        /// A C# literal for a primitive.
        /// </summary>
        /// <remarks>
        /// Suffixes are not cosmetic: an attribute argument binds to a specific parameter type, and
        /// <c>1</c> where <c>1L</c> was meant can select a different overload.
        /// </remarks>
        private static string Literal(object value) => value switch
        {
            bool b => b ? "true" : "false",
            string s => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(s, quote: true),
            char c => Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(c, quote: true),
            float f => f.ToString("R", CultureInfo.InvariantCulture) + "f",
            double d => d.ToString("R", CultureInfo.InvariantCulture) + "d",
            decimal m => m.ToString(CultureInfo.InvariantCulture) + "m",
            long l => l.ToString(CultureInfo.InvariantCulture) + "L",
            ulong u => u.ToString(CultureInfo.InvariantCulture) + "UL",
            uint u => u.ToString(CultureInfo.InvariantCulture) + "U",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "default",
        };
    }
}
