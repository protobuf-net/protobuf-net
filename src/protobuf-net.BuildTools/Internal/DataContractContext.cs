#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Analyzers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

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
                        else if (ShouldDeclareDefault(member, out string? defaultValue) && defaultValue != null)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                descriptor: DataContractAnalyzer.ShouldDeclareDefault,
                                location: Utils.PickLocation(ref context, member.Blame),
                                messageArgs: new object[] { member.MemberName, defaultValue },
                                additionalLocations: null,
                                properties: null
                            ));
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

        private bool ShouldDeclareDefault(Member member, /*[MaybeNullWhen(false)]*/ out string? defaultValue)
        {
            ITypeSymbol? checkType = member.Symbol switch
            {
                IPropertySymbol propSym when ShouldCheck(propSym.Type) => propSym.Type,
                IFieldSymbol fieldSym when ShouldCheck(fieldSym.Type) => fieldSym.Type,
                _ => null,
            };

            bool ShouldCheck(ITypeSymbol type)
            {
                // Marc mentions [DefaultValue], [ProtoMember(IsRequired)], and ShouldSerializeXxx() as some ways to tell PB to send the value.
                // https://stackoverflow.com/a/3162253/1882616
                if (!member.IsRequired && (IsScalarValueType(type.SpecialType) || type.TypeKind == TypeKind.Enum))
                {
                    bool hasDefaultValueAttrib = member.Symbol.GetAttributes().FirstOrDefault(attrib => attrib.AttributeClass != null
                        && attrib.AttributeClass.Name == nameof(DefaultValueAttribute)
                        && attrib.AttributeClass.InNamespace(nameof(System), nameof(System.ComponentModel))) != null;
                    if (!hasDefaultValueAttrib)
                    {
                        string methodName = "ShouldSerialize" + member.Name;
                        bool hasShouldSerializeMethod = member.Symbol.ContainingType.GetMembers().OfType<IMethodSymbol>()
                            .FirstOrDefault(method => method.Name == methodName
                                && method.ReturnType.SpecialType == SpecialType.System_Boolean) != null;
                        return !hasShouldSerializeMethod;
                    }
                }

                return false;
            }

            bool IsScalarValueType(SpecialType type)
                => type switch
                {
                    SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_SByte or SpecialType.System_Byte
                    or SpecialType.System_Int16 or SpecialType.System_UInt16 or SpecialType.System_Int32 or SpecialType.System_UInt32
                    or SpecialType.System_Int64 or SpecialType.System_UInt64 or SpecialType.System_Decimal or SpecialType.System_Single
                    or SpecialType.System_Double or SpecialType.System_IntPtr or SpecialType.System_UIntPtr => true,
                    _ => false,
                };

            if (checkType != null)
            {
                EqualsValueClauseSyntax? equalsValue = null;
                SyntaxNode? memberNode = member.Symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                if (memberNode != null && (memberNode.IsKind(SyntaxKind.PropertyDeclaration) || memberNode.IsKind(SyntaxKind.VariableDeclarator)))
                {
                    equalsValue = (EqualsValueClauseSyntax?)memberNode.ChildNodes().FirstOrDefault(node => node.IsKind(SyntaxKind.EqualsValueClause));
                }

                SyntaxNode? valueNode = equalsValue?.ChildNodes().LastOrDefault();
                if (valueNode != null)
                {
                    SpecialType specialType = checkType.SpecialType;
                    if (specialType == SpecialType.System_Boolean && valueNode.IsKind(SyntaxKind.TrueLiteralExpression))
                    {
                        defaultValue = "true";
                        return true;
                    }
                    else if (specialType == SpecialType.System_Char && valueNode.IsKind(SyntaxKind.CharacterLiteralExpression))
                    {
                        defaultValue = valueNode.ToString();
                        return !_nullCharLiterals.Contains(defaultValue);
                    }
                    else if (specialType != SpecialType.None
                        && (valueNode.IsKind(SyntaxKind.NumericLiteralExpression) || valueNode.IsKind(SyntaxKind.UnaryMinusExpression)))
                    {
                        defaultValue = valueNode.ToString();

                        string literalValue = defaultValue.TrimEnd(_trimmableNumericSuffixes).Replace("_", string.Empty);
                        if (specialType == SpecialType.System_Single || specialType == SpecialType.System_Double)
                        {
                            literalValue = literalValue.TrimEnd(_floatNumericSuffixes);
                            return double.TryParse(literalValue, out double value) && value != 0;
                        }
                        else if (literalValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || literalValue.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
                        {
                            return literalValue.Skip(2).Any(ch => ch != '0');
                        }
                        else if (decimal.TryParse(literalValue, out decimal decimalValue))
                        {
                            return decimalValue != 0;
                        }
                    }
                    else if (checkType.TypeKind == TypeKind.Enum
                        && valueNode is MemberAccessExpressionSyntax access
                        && access.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                        && access.Expression.ToString() == checkType.Name)
                    {
                        string fieldName = access.Name.Identifier.ValueText;
                        IFieldSymbol field = checkType.GetMembers().OfType<IFieldSymbol>().FirstOrDefault(field => field.Name == fieldName);
                        if (field.HasConstantValue && string.Format(CultureInfo.InvariantCulture, "{0}", field.ConstantValue) != "0")
                        {
                            defaultValue = $"{checkType.Name}.{fieldName}";
                            return true;
                        }
                    }
                }
            }

            defaultValue = null;
            return false;
        }

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
