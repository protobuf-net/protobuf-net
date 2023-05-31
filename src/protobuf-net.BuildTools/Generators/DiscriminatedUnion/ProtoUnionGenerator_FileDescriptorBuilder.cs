#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.Internal.ProtoUnion;
using ProtoBuf.Internal.RoslynUtils;

namespace ProtoBuf.Generators.DiscriminatedUnion
{
    public sealed partial class ProtoUnionGenerator
    {
        private ICollection<ProtoUnionFileDescriptor> BuildProtoUnionFileDescriptors(ref GeneratorExecutionContext context, ClassDeclarationSyntax classSyntax)
        {
            var filename = Path.GetFileName(classSyntax.SyntaxTree.FilePath);
            if (!classSyntax.TryParseClassNamespaceName(out var @namespace))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DataContractAnalyzer.DiscriminatedUnionNamespaceNotFound,
                    location: Location.Create(classSyntax.SyntaxTree, classSyntax.Identifier.Span))
                );

                Log($"Namespace could not be found for {classSyntax}");
                return Array.Empty<ProtoUnionFileDescriptor>();
            }
            
            var unionsFieldsMap = GetUnionsProtoFieldsMap(ref context, classSyntax);
            if (!IsUnionsFieldsCollectionValid(ref context, classSyntax, unionsFieldsMap))
            {
                Log($"protoUnionsFields are not parsed correctly, codeFile generation is stopped for {classSyntax}");
                return Array.Empty<ProtoUnionFileDescriptor>();
            }

            var fileDescriptors = new List<ProtoUnionFileDescriptor>();
            foreach (var unionFieldsMap in unionsFieldsMap)
            {
                var unionType = CalculateUnionFieldsSharedType(unionFieldsMap.Value!);
                var fileDescriptor = new ProtoUnionFileDescriptor(
                    filename,
                    Class: classSyntax.Identifier.ToString(),
                    Namespace: @namespace,
                    UnionName: unionFieldsMap.Key,
                    UnionType: unionType,
                    UnionFields: unionFieldsMap.Value);
                
                fileDescriptors.Add(fileDescriptor);
            }

            return fileDescriptors;
        }
        
        private static DiscriminatedUnionType CalculateUnionFieldsSharedType(IEnumerable<ProtoUnionField> unionFields)
        {
            var count32 = false;
            var count64 = false;
            var count128 = false;
            var countReference = false;

            foreach (var field in unionFields)
            {
                switch (field.UnionType)
                {
                    case ProtoUnionField.PropertyUnionType.Is32:
                        count32 = true;
                        break;
                    case ProtoUnionField.PropertyUnionType.Is64:
                        count64 = true;
                        break;
                    case ProtoUnionField.PropertyUnionType.Is128:
                        count128 = true;
                        break;
                    case ProtoUnionField.PropertyUnionType.Reference:
                        countReference = true;
                        break;
                }
            }
                
            if (count128)
            {
                return countReference ? DiscriminatedUnionType.Object128 : DiscriminatedUnionType.Standard128;
            }
            if (count64)
            {
                return countReference ? DiscriminatedUnionType.Object64 : DiscriminatedUnionType.Standard64;
            }
            if (count32)
            {
                return countReference ? DiscriminatedUnionType.Object32 : DiscriminatedUnionType.Standard32;
            }
                
            return DiscriminatedUnionType.Object;
        }

        /// <summary>
        /// Reports recognized diagnostics and returns false if found.
        /// Returns true otherwise
        /// </summary>
        private bool IsUnionsFieldsCollectionValid(
            ref GeneratorExecutionContext context,
            ClassDeclarationSyntax classSyntax,
            IReadOnlyDictionary<string, ICollection<ProtoUnionField>> unionsFieldsMap)
        {
            string duplicateInfo;
            var isValid = true;
            var syntaxTree = classSyntax.SyntaxTree;

            if (unionsFieldsMap.Any() == false) return false;

            var fieldNumbers = unionsFieldsMap.Values.SelectMany(x => x.Select(y => y.FieldNumber));
            if (HasDuplicates(fieldNumbers, out duplicateInfo))
            {
                isValid = false;
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DataContractAnalyzer.DiscriminatedUnionFieldNumbersShouldBeUnique,
                    location: Location.Create(syntaxTree, classSyntax.Identifier.Span),
                    messageArgs: duplicateInfo)
                );
            }

            var memberNames = unionsFieldsMap.Values.SelectMany(x => x.Select(y => y.MemberName));
            if (HasDuplicates(memberNames, out duplicateInfo))
            {
                isValid = false;
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DataContractAnalyzer.DiscriminatedUnionMemberNamesShouldBeUnique,
                    location: Location.Create(syntaxTree, classSyntax.Identifier.Span),
                    messageArgs: duplicateInfo)
                );
            }

            return isValid;
            
            bool HasDuplicates<T>(IEnumerable<T> source, out string duplicateInfo)
            {
                duplicateInfo = string.Empty;
                var checkBuffer = new HashSet<T>(EqualityComparer<T>.Default);

                foreach (var item in source)
                {
                    if (!checkBuffer.Add(item))
                    {
                        duplicateInfo = item!.ToString();
                        return true;
                    }
                }

                return false;
            }
        }
    }
}