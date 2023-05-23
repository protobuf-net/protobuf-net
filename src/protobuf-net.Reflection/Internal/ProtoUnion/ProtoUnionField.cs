using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.Internal.Roslyn.Extensions;

namespace ProtoBuf.Internal.ProtoUnion
{
    internal sealed class ProtoUnionField
    {
        /// <summary>
        /// Type as used in a code. For example 'int', 'bool', etc
        /// </summary>
        public string CSharpType { get; private set; }
        /// <summary>
        /// Name of corresponding union usage from <see cref="DiscriminatedUnion32"/> or analogous discriminated union
        /// </summary>
        public string UnionUsageFieldName { get; private set; }
        /// <summary>
        /// Specifies if property is reference\value and size (if is value type)
        /// </summary>
        public PropertyUnionType UnionType { get; private set; }

        /// <summary>
        /// Name of a union specified by user
        /// </summary>
        public string UnionName { get; private set; }
        /// <summary>
        /// Proto member number 
        /// </summary>
        public int FieldNumber { get; private set; }
        /// <summary>
        /// Name of a generated property
        /// </summary>
        public string MemberName { get; private set; }

        public static bool TryCreate(Compilation compilation, AttributeSyntax attributeSyntax, out ProtoUnionField protoUnionField)
        {
            if (attributeSyntax.Name.Arity == 0) return TryCreateNonGeneric(compilation, attributeSyntax, out protoUnionField);
            if (attributeSyntax.Name.Arity == 1) return TryCreateGeneric(compilation, attributeSyntax, out protoUnionField);
    
            protoUnionField = null;
            return false;
        }
        
        private static bool TryCreateNonGeneric(Compilation compilation, AttributeSyntax attributeSyntax, out ProtoUnionField protoUnionField)
        {
            throw new NotImplementedException();
            
            if (attributeSyntax.ArgumentList is null)
            {
                protoUnionField = null;
                return false;
            }
            
            var arguments = attributeSyntax.ArgumentList.Arguments;
            if (arguments.Count != 4)
            {
                protoUnionField = null;
                return false;
            }
            
            var semanticModel = compilation.GetSemanticModel(attributeSyntax.SyntaxTree);
    
            var typeArgSyntax = arguments.First();
            var typeArg = semanticModel.GetDeclaredSymbol(typeArgSyntax);
            var a = semanticModel.GetTypeInfo(typeArgSyntax);
            
            
            protoUnionField = null;
            return false;
        }
        
        private static bool TryCreateGeneric(Compilation compilation, AttributeSyntax attributeSyntax, out ProtoUnionField protoUnionField)
        {
            if (attributeSyntax.ArgumentList is null)
            {
                protoUnionField = null;
                return false;
            }
            
            var arguments = attributeSyntax.ArgumentList.Arguments;
            if (arguments.Count != 3)
            {
                protoUnionField = null;
                return false;
            }
            
            var semanticModel = compilation.GetSemanticModel(attributeSyntax.SyntaxTree);
            var attributeTypeInfo = semanticModel.GetTypeInfo(attributeSyntax);
            var namedTypeSymbol = (attributeTypeInfo.Type ?? attributeTypeInfo.ConvertedType) as INamedTypeSymbol;
            if (namedTypeSymbol is null)
            {
                protoUnionField = null;
                return false;
            }
            
            var genericTypeSymbol = namedTypeSymbol.TypeArguments.FirstOrDefault();
            if (genericTypeSymbol is null)
            {
                protoUnionField = null;
                return false;
            }
    
            if (!arguments[0].TryParseStringArg(semanticModel, out var unionName) ||
                !arguments[1].TryParseIntArg(semanticModel, out var fieldNumber) ||
                !arguments[2].TryParseStringArg(semanticModel, out var memberName))
            {
                protoUnionField = null;
                return false;
            }

            var unionType = CalculateUnionType(genericTypeSymbol);
            if (!unionType.HasValue)
            {
                protoUnionField = null;
                return false;
            }

            var unionUsageFieldName = CalculateUnionUsageFieldName(genericTypeSymbol);
            if (unionUsageFieldName is null)
            {
                protoUnionField = null;
                return false;
            }
    
            protoUnionField = new ProtoUnionField
            {
                CSharpType = genericTypeSymbol.ToDisplayString(),
                UnionName = unionName,
                FieldNumber = fieldNumber,
                MemberName = memberName,
                UnionType = unionType.Value,
                UnionUsageFieldName = unionUsageFieldName
            };
            return true;
        }

        private static string CalculateUnionUsageFieldName(ITypeSymbol typeSymbol) => typeSymbol.SpecialType switch
        {
            SpecialType.System_Int32 => nameof(DiscriminatedUnion128Object.Int32),
            SpecialType.System_UInt32 => nameof(DiscriminatedUnion128Object.UInt32),
            SpecialType.System_Int64 => nameof(DiscriminatedUnion128Object.Int64),
            SpecialType.System_String => nameof(DiscriminatedUnion128Object.Object),
            _ => null
        };

        private static PropertyUnionType? CalculateUnionType(ITypeSymbol typeSymbol) => typeSymbol.SpecialType switch
        {
            // SpecialType.System_Object => expr,
            // SpecialType.System_Enum => expr,
            // SpecialType.System_Boolean => expr,
            // SpecialType.System_Char => expr,
            // SpecialType.System_SByte => expr,
            // SpecialType.System_Byte => expr,
            // SpecialType.System_Int16 => expr,
            // SpecialType.System_UInt16 => expr,
            SpecialType.System_Int32 => PropertyUnionType.Is32,
            SpecialType.System_UInt32 => PropertyUnionType.Is32,
            // SpecialType.System_Int64 => expr,
            // SpecialType.System_UInt64 => expr,
            // SpecialType.System_Decimal => expr,
            // SpecialType.System_Single => expr,
            // SpecialType.System_Double => expr,
            // SpecialType.System_DateTime => expr,
            SpecialType.System_String => PropertyUnionType.Reference,
            _ => null
        };


        public enum PropertyUnionType
        {
            Is32,
            Is64,
            Is128,
            Reference
        }
    }
}