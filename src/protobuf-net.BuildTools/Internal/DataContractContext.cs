#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.CodeFixes;
using ProtoBuf.Internal.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using ProtoBuf.Internal;

namespace ProtoBuf.BuildTools.Internal
{
    internal sealed class DataContractContext
    {
        // Trim handles the UL and LU compound suffixes. We can't trim D or F suffixes on hex literals.
        private static readonly char[] _trimmableNumericSuffixes = new[] { 'L', 'U', 'M', 'l', 'u', 'm', };
        private static readonly char[] _floatNumericSuffixes = new[] { 'D', 'F', 'd', 'f' };
        private static readonly HashSet<string> _nullCharLiterals = new(
            new char[] { '\0', '\u0000', '\x0', '\x00', '\x000', '\x0000' }.Select(ch => $"'{ch}'"));

        private List<Ignore>? _ignores;
        private List<Member>? _members;
        private List<Reservation>? _reservations;
        private List<Include>? _includes;
        private DataContractContextFlags _flags;

        internal void AddReserved(ISymbol blame, AttributeData attrib)
        {
            Reservation reservation;
            if (attrib.TryGetInt32ByName(nameof(ProtoReservedAttribute.From), out var from) && attrib.TryGetInt32ByName(nameof(ProtoReservedAttribute.To), out var to))
            {
                reservation = new Reservation(attrib.GetLocation(blame), from, to);
            }
            else if (attrib.TryGetInt32ByName("field", out from))
            {
                reservation = new Reservation(attrib.GetLocation(blame), from, from);
            }
            else if (attrib.TryGetStringByName("field", out string name))
            {
                reservation = new Reservation(attrib.GetLocation(blame), name);
            }
            else
            {
                return;
            }
            (_reservations ??= new List<Reservation>()).Add(reservation);
        }

        static bool AssertLegalFieldNumber(ref SyntaxNodeAnalysisContext context, int fieldNumber, Location? blame)
        {
            var severity = fieldNumber switch
            {
                < 1 or > 536870911 => DiagnosticSeverity.Error, // legal range
                >= 19000 and <= 19999 => DiagnosticSeverity.Warning, // reserved range; it'll work, but is a bad idea
                _ => DiagnosticSeverity.Hidden,
            };
            if (severity != DiagnosticSeverity.Hidden)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DataContractAnalyzer.InvalidFieldNumber,
                    location: Utils.PickLocation(ref context, blame),
                    effectiveSeverity: severity,
                    messageArgs: new object[] { fieldNumber },
                    additionalLocations: null,
                    properties: null
                ));
                return false;
            }
            return true;
        }

        internal void ReportProblems(SyntaxNodeAnalysisContext context, ITypeSymbol type)
        {
            HashSet<int>? uniqueFieldNumbers = null;
            HashSet<string>? uniqueFieldNames = null;
            HashSet<string>? coveredMemberNames = null;

            void AssertAvailableNumber(int fieldNumber, Location? blame)
            {
                uniqueFieldNumbers ??= new HashSet<int>();
                if (!uniqueFieldNumbers.Add(fieldNumber))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: DataContractAnalyzer.DuplicateFieldNumber,
                        location: Utils.PickLocation(ref context, blame),
                        messageArgs: new object[] { fieldNumber },
                        additionalLocations: null,
                        properties: null
                    ));
                }
                if (OverlapsReservation(fieldNumber, out var reservation))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: DataContractAnalyzer.ReservedFieldNumber,
                        location: Utils.PickLocation(ref context, blame),
                        messageArgs: new object[] { reservation },
                        additionalLocations: null,
                        properties: null
                    ));
                }
            }

            void AssertAvailableName(string name, Location? blame)
            {
                uniqueFieldNames ??= new HashSet<string>();
                if (!uniqueFieldNames.Add(name))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: DataContractAnalyzer.DuplicateFieldName,
                        location: Utils.PickLocation(ref context, blame),
                        messageArgs: new object[] { name },
                        additionalLocations: null,
                        properties: null
                    ));
                }
                if (OverlapsReservation(name, out var reservation))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        descriptor: DataContractAnalyzer.ReservedFieldName,
                        location: Utils.PickLocation(ref context, blame),
                        messageArgs: new object[] { name },
                        additionalLocations: null,
                        properties: null
                    ));
                }
            }

            if (!(_members is not null || _includes is not null || _reservations is not null || _ignores is not null)) return;

            if (!HasFlag(DataContractContextFlags.IsProtoContract))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DataContractAnalyzer.ShouldBeProtoContract,
                    location: _members.FirstBlame() ?? _includes.FirstBlame()
                        ?? _reservations.FirstBlame() ?? _ignores.FirstBlame()
                        ?? Utils.PickLocation(ref context, type),
                    messageArgs: null,
                    additionalLocations: null,
                    properties: null
                ));;
            }

            if (_reservations is not null)
            {
                int current = 0;
                foreach (var reservation in _reservations)
                {
                    if (reservation.Name is null)
                    {
                        AssertLegalFieldNumber(ref context, reservation.From, reservation.Blame);
                        if (reservation.From != reservation.To)
                            AssertLegalFieldNumber(ref context, reservation.To, reservation.Blame);
                    }

                    for (int i = 0; i < current; i++)
                    {
                        if (reservation.Overlaps(_reservations[i]))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                descriptor: DataContractAnalyzer.DuplicateReservation,
                                location: Utils.PickLocation(ref context, reservation.Blame),
                                messageArgs: new object[] { reservation, _reservations[i] },
                                additionalLocations: null,
                                properties: null
                            ));
                            break;
                        }
                    }
                    current++;
                }
            }

            if (_includes is not null)
            {
                int current = 0;
                foreach (var include in _includes)
                {
                    if (AssertLegalFieldNumber(ref context, include.FieldNumber, include.Blame))
                    {
                        AssertAvailableNumber(include.FieldNumber, include.Blame);
                    }

                    if (!SymbolEqualityComparer.Default.Equals(include.Type.BaseType, type))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: DataContractAnalyzer.IncludeNonDerived,
                            location: Utils.PickLocation(ref context, include.Blame),
                            messageArgs: new object[] { include.Type.ToDisplayString() },
                            additionalLocations: null,
                            properties: null
                        ));
                    }

                    for (int i = 0; i < current; i++)
                    {
                        if (SymbolEqualityComparer.Default.Equals(include.Type, _includes[i].Type))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                descriptor: DataContractAnalyzer.DuplicateInclude,
                                location: Utils.PickLocation(ref context, include.Blame),
                                messageArgs: new object[] { include.Type.ToDisplayString() },
                                additionalLocations: null,
                                properties: null
                            ));
                            break;
                        }
                    }
                    current++;
                }
            }

            if (_members is not null)
            {
                foreach (var member in _members)
                {
                    if (AssertLegalFieldNumber(ref context, member.FieldNumber, member.Blame))
                    {
                        AssertAvailableNumber(member.FieldNumber, member.Blame);
                    }
                    AssertAvailableName(member.Name, member.Blame);

                    if (PropertyOrFieldExists(type, member.MemberName))
                    {
                        coveredMemberNames ??= new HashSet<string>();
                        if (!coveredMemberNames.Add(member.MemberName))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                descriptor: DataContractAnalyzer.DuplicateMemberName,
                                location: Utils.PickLocation(ref context, member.Blame),
                                messageArgs: new object[] { member.MemberName },
                                additionalLocations: null,
                                properties: null
                            ));
                        }
                        if (ShouldIgnore(member.MemberName))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                descriptor: DataContractAnalyzer.DeclaredAndIgnored,
                                location: Utils.PickLocation(ref context, member.Blame),
                                messageArgs: new object[] { member.MemberName },
                                additionalLocations: null,
                                properties: null
                            ));
                        }
                        else
                        {
                            var memberDefaultValueState = CalculateMemberInitialValue(context, member, out var memberInitSyntaxNode, out var memberInitValue);
                            var memberValueStringRepresentation = memberInitSyntaxNode?.ToString() ?? string.Empty;
                            
                            switch (memberDefaultValueState)
                            {
                                case MemberInitValueKind.NotSet:
                                    // no diagnostic
                                    break;

                                case MemberInitValueKind.ConstantExpression:
                                    if (ShouldDeclareDefault(member, memberInitValue))
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(
                                            descriptor: DataContractAnalyzer.ShouldDeclareDefault,
                                            location: Utils.PickLocation(ref context, member.Blame),
                                            messageArgs: new object[] { member.MemberName, memberValueStringRepresentation },
                                            additionalLocations: null,
                                            properties: DiagnosticPropertiesBuilder.Create()
                                                            .Add(ShouldDeclareDefaultCodeFixProvider.DefaultValueDiagnosticArgKey, memberValueStringRepresentation)
                                                            .Build()
                                        ));
                                    }
                                    else if (ShouldUpdateDefaultValueAttribute(context, member, memberInitValue))
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(
                                            descriptor: DataContractAnalyzer.ShouldUpdateDefault,
                                            location: Utils.PickLocation(ref context, member.Blame),
                                            messageArgs: new object[] { member.MemberName, memberValueStringRepresentation },
                                            additionalLocations: null,
                                            properties: DiagnosticPropertiesBuilder.Create()
                                                            .Add(ShouldUpdateDefaultValueCodeFixProvider.DefaultValueDiagnosticArgKey, memberValueStringRepresentation)
                                                            .Build()
                                        ));
                                    }
                                    break;

                                case MemberInitValueKind.NonConstantExpression:
                                    if (ShouldDeclareIsRequired(member))
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(
                                            descriptor: DataContractAnalyzer.ShouldDeclareIsRequired,
                                            location: Utils.PickLocation(ref context, member.Blame),
                                            messageArgs: new object[] { member.MemberName, memberValueStringRepresentation },
                                            additionalLocations: null,
                                            properties: null
                                        ));
                                    }
                                    break;
                            }                            
                        }
                    }
                    else
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: DataContractAnalyzer.MemberNotFound,
                            location: Utils.PickLocation(ref context, member.Blame),
                            messageArgs: new object[] { member.MemberName },
                            additionalLocations: null,
                            properties: null
                        ));
                    }
                }
            }
            static bool PropertyOrFieldExists(ITypeSymbol type, string name)
            {
                foreach (var member in type.GetMembers())
                {
                    switch (member)
                    {
                        case IFieldSymbol:
                        case IPropertySymbol:
                            if (member.Name == name)
                                return true;
                            break;
                    }
                }
                return false;
            }
        }

        private MemberInitValueKind CalculateMemberInitialValue(
            SyntaxNodeAnalysisContext context,
            Member member, 
            out CSharpSyntaxNode? initialValueSyntaxNode,
            out object? memberInitialValue)
        {
            var declaration = member.Symbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (declaration is null)
            {
                initialValueSyntaxNode = null;
                memberInitialValue = null;
                return MemberInitValueKind.NotSet;
            }

            var memberSpecialType = member.SymbolSpecialType;
            if (memberSpecialType is null)
            {
                initialValueSyntaxNode = null;
                memberInitialValue = null;
                return MemberInitValueKind.NotSet;
            }

            EqualsValueClauseSyntax? equalsValue = null;
            var memberNode = declaration.GetSyntax();
            if (memberNode.IsKind(SyntaxKind.PropertyDeclaration) || memberNode.IsKind(SyntaxKind.VariableDeclarator))
            {
                equalsValue = memberNode.ChildNodes().FirstOrDefault(node => node.IsKind(SyntaxKind.EqualsValueClause)) as EqualsValueClauseSyntax;
            }

            if (equalsValue is null)
            {
                initialValueSyntaxNode = null;
                memberInitialValue = null;
                return MemberInitValueKind.NotSet;
            }

            initialValueSyntaxNode = equalsValue?.ChildNodes().LastOrDefault() as CSharpSyntaxNode;
            if (initialValueSyntaxNode is null)
            {
                initialValueSyntaxNode = null;
                memberInitialValue = null;
                return MemberInitValueKind.NotSet;
            }
            
            // calculating the member initial value using semantic model 
            var semanticModel = context.SemanticModel;
            var constantValue = semanticModel.GetConstantValue(initialValueSyntaxNode!);
            if (!constantValue.HasValue || constantValue.Value is null)
            {
                initialValueSyntaxNode = null;
                memberInitialValue = null;
                return MemberInitValueKind.NotSet;
            }

            memberInitialValue = constantValue.Value;
            return memberSpecialType switch
            {
                SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_SByte
                or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 
                or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double 
                or SpecialType.System_IntPtr or SpecialType.System_UIntPtr 
                or SpecialType.System_Enum
                or SpecialType.System_String // we can check some of scenarios - for example '= "hello world"' 
                    => MemberInitValueKind.ConstantExpression,
                
                SpecialType.System_Decimal or _ 
                    => MemberInitValueKind.NonConstantExpression 
            };

            // // Enum calculation
            // if (checkType.TypeKind == TypeKind.Enum
            //     && valueNode is MemberAccessExpressionSyntax access
            //     && access.IsKind(SyntaxKind.SimpleMemberAccessExpression)
            //     && access.Expression.ToString() == checkType.Name)
            // {
            //     string fieldName = access.Name.Identifier.ValueText;
            //     IFieldSymbol field = checkType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(field => field.Name == fieldName);
            //     if (field.HasConstantValue && string.Format(CultureInfo.InvariantCulture, "{0}", field.ConstantValue) != "0") 
            //     {
            //         // defaultValue = $"{checkType.Name}.{fieldName}";
            //         return MemberInitValueKind.ConstantExpression;
            //     }
            // }
        }

        /// <remarks>Ensure to validate member before (i.e. calculate default value of member)</remarks>
        private bool ShouldUpdateDefaultValueAttribute(SyntaxNodeAnalysisContext context, Member member, object? memberInitValue)
        {
            var defaultValueAttrData = GetDefaultValueAttributeData(member);
            if (defaultValueAttrData is null) return false;

            // we are interested in the only single argument of [DefaultValue] attribute's constructor
            var constructorArg = defaultValueAttrData.ConstructorArguments.FirstOrDefault();
            if (constructorArg.IsNull && memberInitValue is not null) return true;
            if (constructorArg.Value is null && memberInitValue is not null) return true;
            if (constructorArg.Value is not null && memberInitValue is null) return true;

            // both of them are not null - lets compare using boxed interpretations of values
            return !constructorArg.Value!.Equals(memberInitValue);
        }

        /// <remarks>Ensure to validate member before (i.e. calculate default value of member)</remarks>
        private bool ShouldDeclareIsRequired(Member member) => !member.IsRequired;

        /// <remarks>Ensure to validate member before (i.e. calculate default value of member)</remarks>
        private bool ShouldDeclareDefault(Member member, object? memberInitValue)
        {
            var memberSpecialType = member.SymbolSpecialType;
            if (memberSpecialType is null)
            {
                return false;
            }

            if (member.IsRequired)
            {
                return false;
            }

            if(HasDefaultAttribute() || HasShouldSerializeMethod())
            {
                // we already have required attributes defined
                return false;
            }
            
            bool HasDefaultAttribute() => GetDefaultValueAttributeData(member) is not null;
            bool HasShouldSerializeMethod() => member.Symbol.ContainingType.GetMembers().OfType<IMethodSymbol>()
                .FirstOrDefault(method => method.Name == "ShouldSerialize" + member.Name && method.ReturnType.SpecialType == SpecialType.System_Boolean) != null;

            if (memberInitValue is null)
            {
                // can not compare the default values to null
                return false;
            }

            // we know, that there are none of the required attributes defined, however if value is default
            // (for example `int Prop {get;} = 0;`
            // then there is no need to place an attribute with the same default value as type's default

            var memberInitialValueIsTypeDefault = memberSpecialType switch
            {
                SpecialType.System_Boolean => memberInitValue.Equals(false),
                SpecialType.System_String => Equals(memberInitValue, string.Empty),
                
                // enums have underlying numeric types, and default one is 0
                SpecialType.System_Enum => memberInitValue.Equals(0), 

                // numbers
                SpecialType.System_Char or SpecialType.System_SByte
                or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 
                or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double 
                or SpecialType.System_IntPtr or SpecialType.System_UIntPtr 
                    => memberInitValue.Equals(0),

                // for other types the behavior of default value is unknown
                // so we dont need to report any diagnostic
                _ => false
            };

            return !memberInitialValueIsTypeDefault;
        }

        AttributeData? GetDefaultValueAttributeData(Member member) => member.Symbol.GetAttributes()
            .FirstOrDefault(attrib => attrib.AttributeClass != null
                && attrib.AttributeClass.Name == nameof(DefaultValueAttribute)
                && attrib.AttributeClass.InNamespace(nameof(System), nameof(System.ComponentModel))) as AttributeData;

        internal void SetContract(ISymbol blame, AttributeData attrib)
        {
            _ = blame;
            _flags |= DataContractContextFlags.IsProtoContract;
            if (attrib.TryGetBooleanByName(nameof(ProtoContractAttribute.SkipConstructor), out var val) && val)
                _flags |= DataContractContextFlags.SkipConstructor;
            if (attrib.TryGetBooleanByName(nameof(ProtoContractAttribute.IgnoreUnknownSubTypes), out val) && val)
                _flags |= DataContractContextFlags.IgnoreUnknownSubTypes;
        }

        public bool HasFlag(DataContractContextFlags flag)
            => (_flags & flag) != 0;


        private bool ShouldIgnore(string memberName)
        {
            if (_ignores is not null)
            {
                foreach (var ignore in _ignores)
                {
                    if (ignore.MemberName == memberName)
                        return true;
                }
            }
            return false;
        }

        internal void AddInclude(ISymbol blame, AttributeData attrib)
        {
            if (!attrib.TryGetInt32ByName(nameof(ProtoIncludeAttribute.Tag), out var tag)
                || !attrib.TryGetTypeByName(nameof(ProtoIncludeAttribute.KnownType), out var type))
                return;

            (_includes ??= new List<Include>()).Add(new Include(attrib.GetLocation(blame), tag, type));
        }

        internal void AddIgnore(ISymbol blame, AttributeData attrib, string? memberName)
        {
            if (memberName is null)
            {
                if (!(attrib.TryGetStringByName(nameof(ProtoPartialIgnoreAttribute.MemberName), out memberName)))
                    return;
            }
            (_ignores ??= new List<Ignore>()).Add(new Ignore(attrib.GetLocation(blame), memberName));
        }

        internal void AddMember(ISymbol blame, AttributeData attrib, string? memberName)
        {
            if (memberName is null)
            {
                if (!(attrib.TryGetStringByName(nameof(ProtoPartialMemberAttribute.MemberName), out memberName)))
                    return;
            }

            if (!(attrib.TryGetInt32ByName(nameof(ProtoPartialMemberAttribute.Tag), out var tag)))
                return;

            if (!(attrib.TryGetStringByName(nameof(ProtoPartialMemberAttribute.Name), out var name)))
                name = memberName;

            attrib.TryGetBooleanByName(nameof(ProtoPartialMemberAttribute.IsRequired), out bool isRequired);

            (_members ??= new List<Member>()).Add(new Member(attrib.GetLocation(blame), tag, memberName, name, blame, isRequired));
        }

        public bool OverlapsReservation(string name, out Reservation overlap)
        {
            if (_reservations is not null)
            {
                foreach (var existing in _reservations)
                {
                    if (existing.Includes(name))
                    {
                        overlap = existing;
                        return true;
                    }
                }
            }
            overlap = default;
            return false;
        }

        public bool OverlapsReservation(int number, out Reservation overlap)
        {
            if (_reservations is not null)
            {
                foreach (var existing in _reservations)
                {
                    if (existing.Includes(number))
                    {
                        overlap = existing;
                        return true;
                    }
                }
            }
            overlap = default;
            return false;
        }
    }
}
