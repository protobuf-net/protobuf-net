#nullable enable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProtoBuf.Internal.Roslyn.Extensions
{
    internal static class AttributeArgumentSyntaxExtensions
    {
        /// <summary>
        /// Returns passed `string` value to attribute argument
        /// </summary>
        public static bool TryParseStringArg(this AttributeArgumentSyntax attributeArgumentSyntax, SemanticModel semanticModel, out string? result)
        {
            var constantValue = semanticModel.GetConstantValue(attributeArgumentSyntax.Expression);
            if (!constantValue.HasValue)
            {
                result = null;
                return false;
            }
    
            if (constantValue.Value is not string stringValue)
            {
                result = null;
                return false;
            }
    
            result = stringValue;
            return true;
        }
        
        /// <summary>
        /// Returns passed `int` value to attribute argument
        /// </summary>
        public static bool TryParseIntArg(this AttributeArgumentSyntax attributeArgumentSyntax, SemanticModel semanticModel, out int result)
        {
            var constantValue = semanticModel.GetConstantValue(attributeArgumentSyntax.Expression);
            if (!constantValue.HasValue)
            {
                result = default;
                return false;
            }
    
            if (constantValue.Value is not int intValue)
            {
                result = default;
                return false;
            }
    
            result = intValue;
            return true;
        }    
    }
}