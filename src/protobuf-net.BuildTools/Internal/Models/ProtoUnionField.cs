using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ProtoBuf.Internal.Models
{
    internal sealed class ProtoUnionField
    {
        public Type Type { get; set; }
        public string UnionName { get; set; }
        public int FieldNumber { get; set; }
        public string MemberName { get; set; }

        public static bool TryCreate(Compilation compilation, AttributeSyntax attributeSyntax, out ProtoUnionField protoUnionField)
        {
            if (attributeSyntax.Name.Arity == 0) return TryCreateNonGeneric(compilation, attributeSyntax, out protoUnionField);
            if (attributeSyntax.Name.Arity == 1) return TryCreateGeneric(compilation, attributeSyntax, out protoUnionField);

            protoUnionField = null;
            return false;
        }
        
        private static bool TryCreateNonGeneric(Compilation compilation, AttributeSyntax attributeSyntax, out ProtoUnionField protoUnionField)
        {
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
            
            var genericTypeSymbol = namedTypeSymbol.TypeArguments.First();
            var genericType = ResolveType(genericTypeSymbol);
            if (genericType is null)
            {
                protoUnionField = null;
                return false;
            }

            if (!TryParseStringArg(semanticModel, arguments[0], out var unionName) ||
                !TryParseIntArg(semanticModel, arguments[1], out var fieldNumber) ||
                !TryParseStringArg(semanticModel, arguments[2], out var memberName))
            {
                protoUnionField = null;
                return false;
            }

            protoUnionField = new ProtoUnionField
            {
                Type = genericType,
                UnionName = unionName,
                FieldNumber = fieldNumber,
                MemberName = memberName
            };
            return true;
        }

        static bool TryParseStringArg(SemanticModel semanticModel, AttributeArgumentSyntax attributeArgumentSyntax, out string result)
        {
            var constantValue = semanticModel.GetConstantValue(attributeArgumentSyntax.Expression);
            if (!constantValue.HasValue)
            {
                result = null;
                return false;
            }

            if (constantValue.Value is not string)
            {
                result = null;
                return false;
            }

            result = (string)constantValue.Value;
            return true;
        }
        
        static bool TryParseIntArg(SemanticModel semanticModel, AttributeArgumentSyntax attributeArgumentSyntax, out int result)
        {
            var constantValue = semanticModel.GetConstantValue(attributeArgumentSyntax.Expression);
            if (!constantValue.HasValue)
            {
                result = default;
                return false;
            }

            if (constantValue.Value is not int)
            {
                result = default;
                return false;
            }

            result = (int)constantValue.Value;
            return true;
        }

        private static Type ResolveType(ITypeSymbol typeSymbol)
        {
            return typeSymbol.SpecialType switch
            {
                SpecialType.System_Object => typeof(object),
                SpecialType.System_Boolean => typeof(bool),
                SpecialType.System_Char => typeof(char),
                SpecialType.System_SByte => typeof(sbyte),
                SpecialType.System_Byte => typeof(byte),
                SpecialType.System_Int16 => typeof(short),
                SpecialType.System_UInt16 => typeof(ushort),
                SpecialType.System_Int32 => typeof(int),
                SpecialType.System_UInt32 => typeof(uint),
                SpecialType.System_Int64 => typeof(long),
                SpecialType.System_UInt64 => typeof(ulong),
                SpecialType.System_Decimal => typeof(decimal),
                SpecialType.System_Single => typeof(float),
                SpecialType.System_Double => typeof(double),
                SpecialType.System_String => typeof(string),
                _ => null,
            };
        }

        private bool Equals(ProtoUnionField other)
        {
            return Type == other.Type 
                   && UnionName == other.UnionName
                   && FieldNumber == other.FieldNumber
                   && MemberName == other.MemberName;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is ProtoUnionField other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Type != null ? Type.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (UnionName != null ? UnionName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ FieldNumber;
                hashCode = (hashCode * 397) ^ (MemberName != null ? MemberName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}