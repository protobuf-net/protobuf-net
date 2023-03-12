using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.Internal;

namespace ProtoBuf.CodeFixes.DefaultValue.Abstractions
{
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
        
        protected static SeparatedSyntaxList<AttributeArgumentSyntax> BuildDefaultValueAttributeArguments(DiagnosticArguments diagnosticArguments) =>
            UseShortSyntax(diagnosticArguments.MemberSpecialType)
                ? BuildDefaultValueShortArgumentSyntax(diagnosticArguments)
                : BuildDefaultValueLongArgumentSyntax(diagnosticArguments);

        protected static bool TryBuildDiagnosticArguments(Diagnostic diagnostic, out DiagnosticArguments diagnosticArguments)
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
        
        protected struct DiagnosticArguments
        {
            public string DefaultValueStringRepresentation { get; set; }
            public string DefaultValueCalculated { get; set; }
            public SpecialType MemberSpecialType { get; set; }
        }
        
        /// <summary>
        /// Some of known types can not use easy "[<see cref="DefaultValueAttribute"/>(value)]" syntax
        /// and instead we can use <see cref="DefaultValueAttribute"/>(typeof(type), "rawValue") syntax
        /// </summary>
        private static bool UseShortSyntax(SpecialType specialType) => specialType switch
        {
            SpecialType.System_Decimal => false,
            _ => true
        };
        
        private static SeparatedSyntaxList<AttributeArgumentSyntax> BuildDefaultValueShortArgumentSyntax(DiagnosticArguments diagnosticArguments)
            => SyntaxFactory.SeparatedList(new []
            {
                SyntaxFactory.AttributeArgument(
                    default, default, SyntaxFactory.ParseExpression(diagnosticArguments.DefaultValueStringRepresentation))
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