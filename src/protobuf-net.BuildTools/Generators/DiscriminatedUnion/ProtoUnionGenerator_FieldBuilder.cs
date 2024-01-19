#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.Internal.ProtoUnion;
using ProtoBuf.Internal.Roslyn.Extensions;
using ProtoBuf.Internal.RoslynUtils;

namespace ProtoBuf.Generators.DiscriminatedUnion
{
    public sealed partial class ProtoUnionGenerator
    {
        /// <summary>
        /// Build map of unionNames to protoFields of corresponding union
        /// </summary>
        private IReadOnlyDictionary<string, ICollection<ProtoUnionField>> GetUnionsProtoFieldsMap(ref GeneratorExecutionContext context, ClassDeclarationSyntax classSyntax)
        {
            var attributes = classSyntax.GetAttributeSyntaxesOfType(typeof(ProtoUnionAttribute<>));
            var unionsProtoFieldsMap = new Dictionary<string, ICollection<ProtoUnionField>>();
            foreach (var attributeSyntax in attributes)
            {
                if (!TryCreate(ref context, attributeSyntax, out var field))
                {
                    Log($"Failed to parse protoUnionField from {attributeSyntax.ToFullString()}");
                    continue;
                }

                var unionName = field!.UnionName;
                if (!unionsProtoFieldsMap.ContainsKey(unionName))
                    unionsProtoFieldsMap[unionName] = new List<ProtoUnionField>()!;
                unionsProtoFieldsMap[unionName].Add(field);
            }

            return unionsProtoFieldsMap;
        }

        private bool TryCreate(ref GeneratorExecutionContext context, AttributeSyntax attributeSyntax, out ProtoUnionField? protoUnionField)
        {
            if (attributeSyntax.Name.Arity == 0) return TryCreateNonGeneric(ref context, attributeSyntax, out protoUnionField);
            if (attributeSyntax.Name.Arity == 1) return TryCreateGeneric(ref context, attributeSyntax, out protoUnionField);
            
            throw new ArgumentException($"Could not recognize {attributeSyntax.ToFullString()}");
        }

        private bool TryCreateNonGeneric(ref GeneratorExecutionContext context, AttributeSyntax attributeSyntax, out ProtoUnionField? protoUnionField)
        {
            throw new NotImplementedException();
        }

        private bool TryCreateGeneric(ref GeneratorExecutionContext context, AttributeSyntax attributeSyntax, out ProtoUnionField? protoUnionField)
        {
            if (attributeSyntax.ArgumentList is null)
            {
                Log($"No arguments found for attribute '{attributeSyntax.ToFullString()}'");
                protoUnionField = null;
                return false;
            }

            var arguments = attributeSyntax.ArgumentList.Arguments;
            if (arguments.Count != 3)
            {
                Log($"Not proper arguments count found for attribute '{attributeSyntax.ToFullString()}'");
                protoUnionField = null;
                return false;
            }

            var semanticModel = context.Compilation.GetSemanticModel(attributeSyntax.SyntaxTree);
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
            
            protoUnionField = new ProtoUnionField(
                unionName: unionName,
                fieldNumber: fieldNumber,
                memberName: memberName,
                unionType: CalculateUnionType(genericTypeSymbol),
                unionUsageFieldName: CalculateUnionUsageFieldName(genericTypeSymbol),
                cSharpType: genericTypeSymbol.ToDisplayString());
            
            return IsProtoUnionFieldValid(ref context, attributeSyntax, protoUnionField);
        }

        private static string CalculateUnionUsageFieldName(ITypeSymbol typeSymbol) => typeSymbol switch
        {
            { Name: nameof(TimeSpan), TypeKind: TypeKind.Struct } => nameof(DiscriminatedUnion128Object.TimeSpan),
            { Name: nameof(DateTime), TypeKind: TypeKind.Struct } => nameof(DiscriminatedUnion128Object.DateTime),
            { Name: nameof(Guid), TypeKind: TypeKind.Struct } => nameof(DiscriminatedUnion128Object.Guid),
            _ => typeSymbol.SpecialType switch
            {
                SpecialType.System_Boolean => nameof(DiscriminatedUnion128Object.Boolean),
                SpecialType.System_Int32 => nameof(DiscriminatedUnion128Object.Int32),
                SpecialType.System_UInt32 => nameof(DiscriminatedUnion128Object.UInt32),
                SpecialType.System_Single => nameof(DiscriminatedUnion128Object.Single),
                SpecialType.System_Int64 => nameof(DiscriminatedUnion128Object.Int64),
                SpecialType.System_UInt64 => nameof(DiscriminatedUnion128Object.UInt64),
                SpecialType.System_Double => nameof(DiscriminatedUnion128Object.Double),
                SpecialType.System_DateTime => nameof(DiscriminatedUnion128Object.DateTime),
                _ => nameof(DiscriminatedUnion128Object.Object)
            }
        };

        private static ProtoUnionField.PropertyUnionType CalculateUnionType(ITypeSymbol typeSymbol) => typeSymbol switch
        {
            { Name: nameof(TimeSpan), TypeKind: TypeKind.Struct } => ProtoUnionField.PropertyUnionType.Is64,
            { Name: nameof(DateTime), TypeKind: TypeKind.Struct } => ProtoUnionField.PropertyUnionType.Is64,
            { Name: nameof(Guid), TypeKind: TypeKind.Struct } => ProtoUnionField.PropertyUnionType.Is128,
            _ => typeSymbol.SpecialType switch
            {
                SpecialType.System_Enum or SpecialType.System_Boolean or SpecialType.System_Int32
                    or SpecialType.System_UInt32
                    or SpecialType.System_Single => ProtoUnionField.PropertyUnionType.Is32,

                SpecialType.System_Double or SpecialType.System_DateTime or SpecialType.System_Int64
                    or SpecialType.System_UInt64 => ProtoUnionField.PropertyUnionType.Is64,

                _ => ProtoUnionField.PropertyUnionType.Reference
            }
        };

        /// <summary>
        /// Reports diagnostics, if values are not supported and returns false.
        /// Returns true otherwise.
        /// </summary>
        private bool IsProtoUnionFieldValid(ref GeneratorExecutionContext context, AttributeSyntax attributeSyntax, ProtoUnionField protoUnionField)
        {
            var isValid = true;
            var syntaxTree = attributeSyntax.SyntaxTree;

            if (string.IsNullOrWhiteSpace(protoUnionField.UnionName))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DataContractAnalyzer.DiscriminatedUnionNameShouldNotBeEmpty,
                    location: Location.Create(syntaxTree, attributeSyntax.Span))
                );
                isValid = false;
            }
            
            if (string.IsNullOrWhiteSpace(protoUnionField.MemberName))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DataContractAnalyzer.DiscriminatedUnionMemberNameShouldNotBeEmpty,
                    location: Location.Create(syntaxTree, attributeSyntax.Span))
                );
                isValid = false;
            }

            return isValid;
        }
    }
}