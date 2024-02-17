using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.Internal.Roslyn.Extensions;

namespace ProtoBuf.CodeFixes.DefaultValue.Abstractions
{
    /// <summary>
    /// Base functionality for DefaultValue related Roslyn code-fix implementations
    /// </summary>
    public abstract class DefaultValueCodeFixProviderBase : CodeFixProvider
    {
        /// <summary>
        /// Key of for a <see cref="KeyValuePair{TKey,TValue}"/> of diagnostic properties,
        /// containing <see cref="DefaultValueAttribute"/> constructor value to be inserted into code
        /// </summary>
        internal const string DefaultValueStringRepresentationArgKey = "DefaultValueStringRepresentationArgKey";
        
        /// <summary>
        /// 'object.ToString()' representation.
        /// </summary>
        internal const string DefaultValueCalculatedArgKey = "DefaultValueCalculatedArgKey";
        
        /// <summary>
        /// <see cref="SpecialType"/> value of member type. Helps to consider which syntax to use
        /// </summary>
        internal const string MemberSpecialTypeArgKey = "MemberSpecialTypeArgKey";

        /// <inheritdoc/>
        public abstract override FixAllProvider GetFixAllProvider();

        /// <summary>
        /// Build attribute syntax of different type according to diagnostic argument special type
        /// </summary>
        protected internal static SeparatedSyntaxList<AttributeArgumentSyntax> BuildDefaultValueAttributeArguments(DiagnosticArguments diagnosticArguments) =>
            UseShortSyntax(diagnosticArguments.MemberSpecialType)
                ? BuildDefaultValueShortArgumentSyntax(diagnosticArguments)
                : BuildDefaultValueLongArgumentSyntax(diagnosticArguments);

        /// <summary>
        /// Build input-model of code-fix
        /// </summary>
        protected internal static bool TryBuildDiagnosticArguments(Diagnostic diagnostic, out DiagnosticArguments diagnosticArguments)
        {
            if (diagnostic.Properties.Count == 0)
            {
                diagnosticArguments = default;
                return false;
            }

            if (!diagnostic.Properties.TryGetValue(DefaultValueStringRepresentationArgKey, out var defaultValueStringRepresentation))
            {
                diagnosticArguments = default;
                return false;
            }
            
            if (!diagnostic.Properties.TryGetValue(DefaultValueCalculatedArgKey, out var defaultValueCalculated))
            {
                diagnosticArguments = default;
                return false;
            }
            
            if (!diagnostic.Properties.TryGetValue(MemberSpecialTypeArgKey, out var memberSpecialTypeRaw)
                || !Enum.TryParse<SpecialType>(memberSpecialTypeRaw, out var memberSpecialType))
            {
                diagnosticArguments = default;
                return false;
            }

            diagnosticArguments = new()
            {
                DefaultValueStringRepresentation = defaultValueStringRepresentation,
                DefaultValueCalculated = defaultValueCalculated,
                MemberSpecialType = memberSpecialType
            };
            return true;
        }
        
        /// <summary>
        /// Input-model of code-fix
        /// </summary>
        protected internal struct DiagnosticArguments
        {
            /// <summary>
            /// Original string value of field definition 
            /// </summary>
            public string DefaultValueStringRepresentation { get; set; }
            
            /// <summary>
            /// Recalculated representation of default value
            /// </summary>
            public string DefaultValueCalculated { get; set; }
            
            /// <summary>
            /// Roslyn special type representation of a field
            /// </summary>
            public SpecialType MemberSpecialType { get; set; }
            
            /// <summary>
            /// Returns default value string representation with a cast to type included
            /// </summary>
            internal readonly string GetCastedRepresentation()
            {
                if (MemberSpecialType == SpecialType.System_Enum)
                {
                    return DefaultValueStringRepresentation;
                }
                
                return string.Format(
                    "({0}){1}",
                    MemberSpecialType.GetSpecialTypeCSharpKeyword(),
                    DefaultValueStringRepresentation);
            }
        }
        
        /// <summary>
        /// Some of known types can not use easy "[<see cref="DefaultValueAttribute"/>(value)]" syntax
        /// and instead we can use <see cref="DefaultValueAttribute"/>(typeof(type), "rawValue") syntax
        /// </summary>
        private static bool UseShortSyntax(SpecialType specialType)
        {
            return specialType switch
            {
                SpecialType.System_Decimal or SpecialType.System_UInt64 or 
                SpecialType.System_UInt32 or SpecialType.System_UInt16 or 
                SpecialType.System_SByte => false,
                _ => true
            };
        }

        private static SeparatedSyntaxList<AttributeArgumentSyntax> BuildDefaultValueShortArgumentSyntax(DiagnosticArguments diagnosticArguments)
            => SyntaxFactory.SeparatedList(new []
            {
                SyntaxFactory.AttributeArgument(
                    default, default, SyntaxFactory.ParseExpression(diagnosticArguments.GetCastedRepresentation()))
            });
        
        private static SeparatedSyntaxList<AttributeArgumentSyntax> BuildDefaultValueLongArgumentSyntax(DiagnosticArguments diagnosticArguments)
            => SyntaxFactory.SeparatedList(new []
            {
                SyntaxFactory.AttributeArgument(default, default,
                    SyntaxFactory.ParseExpression($"typeof({diagnosticArguments.MemberSpecialType.GetSpecialTypeCSharpKeyword()})")),
                    
                SyntaxFactory.AttributeArgument(default, default, 
                    SyntaxFactory.ParseExpression("\"" + diagnosticArguments.DefaultValueCalculated + "\""))
            });
    }
}