#nullable enable
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.Internal.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using ProtoBuf.CodeFixes.DefaultValue.Abstractions;
using ProtoBuf.Internal;
using ProtoBuf.Internal.Roslyn.Extensions;

namespace ProtoBuf.BuildTools.Internal
{
    internal sealed class DataContractContext
    {
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

            // A generic base declares its [ProtoInclude] list once and shares it with every closed
            // construction, but each construction only ever matches the includes that actually derive
            // from it. So two includes may carry the same tag when they belong to *different*
            // constructions - Holder<T> naming both ShipHolder : Holder<Ship> and CrateHolder :
            // Holder<Crate> at tag 1 is legal, and ref-emit serializes it. The number is therefore
            // claimed per construction, keyed on the base each include really derives from; for a
            // non-generic declaring type that key is the same for all of them, so nothing changes.
            Dictionary<string, HashSet<int>>? perConstruction = null;

            void AssertAvailableNumber(int fieldNumber, Location? blame, string? construction = null)
            {
                uniqueFieldNumbers ??= new HashSet<int>();

                // The shared set is always claimed, so a member and an include still collide however
                // the includes are grouped - and note the includes are walked *before* the members,
                // so this cannot be done by snapshotting one set into the other. What the grouping
                // changes is only which collisions are *reported*: for an include, that is a clash
                // within its own construction.
                bool collides;
                if (construction is null)
                {
                    collides = !uniqueFieldNumbers.Add(fieldNumber);
                }
                else
                {
                    collides = !Claim(construction).Add(fieldNumber);
                    // claimed for the members' benefit, but a clash here is never reported *as* the
                    // include's: a sibling construction legitimately holds the same number
                    uniqueFieldNumbers.Add(fieldNumber);
                }

                if (collides)
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

            // an include's numbering space is the members' *plus* the other includes on the same
            // construction: a member and an include still cannot share a number
            HashSet<int> Claim(string construction)
            {
                perConstruction ??= new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
                if (!perConstruction.TryGetValue(construction, out var claimed))
                {
                    perConstruction[construction] = claimed = new HashSet<int>();
                }
                return claimed;
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

            // ...but only when there is no contract marker *at all*: with [DataContract] or [XmlType]
            // present the ProtoBuf annotations are honoured rather than ignored, so saying otherwise
            // is both wrong and, as an error, a build break for a shape that works
            if (!HasFlag(DataContractContextFlags.IsProtoContract)
                && !HasFlag(DataContractContextFlags.HasOtherContractFamily))
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
                        // the construction this include actually belongs to; only distinguishable
                        // when the declaring type is generic, which is the only case that needs it
                        var construction = type is INamedTypeSymbol { IsGenericType: true }
                            ? (include.Type.BaseType?.ToDisplayString() ?? "?") : "";
                        AssertAvailableNumber(include.FieldNumber, include.Blame, construction);
                    }

                    // an interface is a legal include root - protobuf-net treats implementing one
                    // exactly as deriving from a base class - so the base-type test alone reports a
                    // build *error* for a pattern that works perfectly well at runtime.
                    // Compared by *original definition*, because a generic root carries the attribute
                    // on the open type while the sub-type names a closed construction: the include on
                    // IBox<T> is inherited by every IBox<int>, and ref-emit resolves it happily
                    var linked = Linked(include.Type.BaseType)
                        || (type.TypeKind == TypeKind.Interface
                            && include.Type.AllInterfaces.Any(Linked));

                    bool Linked(INamedTypeSymbol? candidate)
                        => candidate is not null
                            && (SymbolEqualityComparer.Default.Equals(candidate, type)
                                || SymbolEqualityComparer.Default.Equals(
                                    candidate.OriginalDefinition, type.OriginalDefinition));

                    if (!linked)
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

                            if (DeclaredDefaultIsIgnored(member, out var ignoredReason))
                            {
                                // the declared default is inert here, so every nag below - each of
                                // which assumes it is load-bearing - has nothing useful to say
                                context.ReportDiagnostic(Diagnostic.Create(
                                    descriptor: DataContractAnalyzer.DeclaredDefaultIgnored,
                                    location: Utils.PickLocation(ref context, member.Blame),
                                    messageArgs: new object[] { member.MemberName, ignoredReason! },
                                    additionalLocations: null,
                                    properties: null
                                ));
                                continue;
                            }

                            switch (memberDefaultValueState)
                            {
                                case MemberInitValueKind.NotSet:
                                    if (DeclaredDefaultCannotRoundTrip(member))
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(
                                            descriptor: DataContractAnalyzer.DeclaredDefaultCannotRoundTrip,
                                            location: Utils.PickLocation(ref context, member.Blame),
                                            messageArgs: new object[] { member.MemberName },
                                            additionalLocations: null,
                                            properties: null
                                        ));
                                    }
                                    break;

                                case MemberInitValueKind.ConstantExpression:
                                    // `null` is one of the constants we can land on, and it is the one
                                    // value that has no ToString() to offer the code fix
                                    var memberInitValueText = memberInitValue?.ToString() ?? "null";
                                    if (ShouldDeclareDefault(member, memberInitValue))
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(
                                            descriptor: DataContractAnalyzer.ShouldDeclareDefault,
                                            location: Utils.PickLocation(ref context, member.Blame),
                                            messageArgs: new object[] { member.MemberName, memberValueStringRepresentation },
                                            additionalLocations: null,
                                            properties: DiagnosticPropertiesBuilder.Create()
                                                            .Add(DefaultValueCodeFixProviderBase.DefaultValueStringRepresentationArgKey, memberValueStringRepresentation)
                                                            .Add(DefaultValueCodeFixProviderBase.DefaultValueCalculatedArgKey, memberInitValueText)
                                                            .Add(DefaultValueCodeFixProviderBase.MemberSpecialTypeArgKey, member.SymbolSpecialType.ToString())
                                                            .Build()
                                        ));
                                    }
                                    else if (ShouldUpdateDefaultValueAttribute(context, member, memberInitValue, out var blame))
                                    {
                                        context.ReportDiagnostic(Diagnostic.Create(
                                            descriptor: DataContractAnalyzer.ShouldUpdateDefault,
                                            location: Utils.PickLocation(ref context, blame),
                                            messageArgs: new object[] { member.MemberName, memberValueStringRepresentation },
                                            additionalLocations: null,
                                            properties: DiagnosticPropertiesBuilder.Create()
                                                            .Add(DefaultValueCodeFixProviderBase.DefaultValueStringRepresentationArgKey, memberValueStringRepresentation)
                                                            .Add(DefaultValueCodeFixProviderBase.DefaultValueCalculatedArgKey, memberInitValueText)
                                                            .Add(DefaultValueCodeFixProviderBase.MemberSpecialTypeArgKey, member.SymbolSpecialType.ToString())
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
                                            messageArgs: new object[] { member.MemberName },
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
            
            // `x!` is just x for our purposes; nullable-reference-types makes `= default!` and
            // `= null!` the routine way of writing "I know, and I don't care" on a non-nullable
            // member, and the suppression must not hide the initializer from us
            while (initialValueSyntaxNode is PostfixUnaryExpressionSyntax postfix
                && postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression))
            {
                initialValueSyntaxNode = postfix.Operand;
            }

            // calculating the member initial value using semantic model 
            var semanticModel = context.Compilation.GetSemanticModel(memberNode.SyntaxTree);
            var constantValue = semanticModel.GetConstantValue(initialValueSyntaxNode!);
            if (constantValue.HasValue && constantValue.Value is null)
            {
                // an initializer that only restates the type's own default - `= null`, `= default`,
                // `= (string)null` - is a constant like any other, and specifically the one that
                // makes IsRequired pointless: there is no value here that could be lost on the wire.
                // It still gets compared against [DefaultValue], which may claim otherwise.
                memberInitialValue = null;
                return MemberInitValueKind.ConstantExpression;
            }

            if (!constantValue.HasValue)
            {
                // static fields are not considered as `constantValue`,
                // so we can manually go over some known assignments (i.e. `string.Empty`)
                if (CalculateKnownMemberAssignment(out var valueKind, initialValueSyntaxNode, out memberInitialValue)
                    && valueKind != null)
                {
                    return valueKind.Value;
                }
                
                initialValueSyntaxNode = null;
                memberInitialValue = null;
                return MemberInitValueKind.NonConstantExpression; 
            }
            
            memberInitialValue = constantValue.Value;
            var memberType = memberSpecialType.Value.ToType();
            if (memberSpecialType.Value.IsPrimitiveValueType() && memberType != null)
            {
                memberInitialValue = Convert.ChangeType(memberInitialValue, memberType);
            }

            return memberSpecialType switch
            {
                SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_SByte
                or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 
                or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double
                or SpecialType.System_Enum 
                or SpecialType.System_Decimal // use long syntax for defaultValue to set
                or SpecialType.System_String // we can check some of scenarios - for example '= "hello world"' 
                    => MemberInitValueKind.ConstantExpression,
                
                SpecialType.System_Decimal or 
                SpecialType.System_IntPtr or SpecialType.System_UIntPtr // cant be used due to [DefaultValue] restrictions 
                or _ 
                    => MemberInitValueKind.NonConstantExpression 
            };

            bool CalculateKnownMemberAssignment(
                out MemberInitValueKind? valueKind,
                CSharpSyntaxNode initialValueSyntaxNode,
                out object? memberInitialValue)
            {
                if (memberSpecialType == SpecialType.System_String &&
                    initialValueSyntaxNode.ToString() == "string.Empty")
                {
                    valueKind = MemberInitValueKind.ConstantExpression;
                    memberInitialValue = string.Empty;
                    return true;
                }

                memberInitialValue = null;
                valueKind = MemberInitValueKind.NonConstantExpression;
                return false;
            }
        }

        /// <remarks>Ensure to validate member before (i.e. calculate default value of member)</remarks>
        private bool ShouldUpdateDefaultValueAttribute(SyntaxNodeAnalysisContext context, Member member, object? memberInitValue, out Location? defaultValueAttributeLocation)
        {
            defaultValueAttributeLocation = null;
            var defaultValueAttrData = GetDefaultValueAttributeData(member);
            if (defaultValueAttrData is null) return false;
            
            // set the blame location on attribute
            defaultValueAttributeLocation = defaultValueAttrData.GetLocation(defaultValueAttrData.AttributeClass);

            if (defaultValueAttrData.ConstructorArguments.Length == 1)
            {
                // we are interested in the only single argument of [DefaultValue] attribute's constructor
                var constructorArg = defaultValueAttrData.ConstructorArguments.FirstOrDefault();
                if (constructorArg.IsNull && memberInitValue is not null) return true;
                if (constructorArg.Value is null && memberInitValue is not null) return true;
                if (constructorArg.Value is not null && memberInitValue is null) return true;

                // both sides are the type's own default, so they agree; the comparisons below
                // dereference the attribute value and must not be reached when it is null
                if (constructorArg.Value is null && memberInitValue is null) return false;
                
                var memberSpecialType = member.SymbolSpecialType!.Value;
                if (memberSpecialType.IsPrimitiveValueType())
                {
                    // we have to ensure both boxed-values are of same type due to implementation details
                    // of primitive types .Equals()
                    var memberType = memberSpecialType.ToType();
                    var constructorArgValueCasted = Convert.ChangeType(constructorArg.Value, memberType!);
                    return !constructorArgValueCasted!.Equals(memberInitValue);
                }
                
                // compare using boxed interpretations of values
                return !constructorArg.Value!.Equals(memberInitValue);
            }
            
            if (defaultValueAttrData.ConstructorArguments.Length == 2)
            {
                var attrStrValue = defaultValueAttrData.ConstructorArguments[1];
                var attrTypeData = defaultValueAttrData.ConstructorArguments[0];
                var attrType = attrTypeData.GetUnderlyingType();
                
                object? defaultValueCompiledValue;
                try
                {
                    defaultValueCompiledValue = Utils.DynamicallyParseToValue(attrType!, (string)attrStrValue.Value!);
                }
                catch
                {
                    // if parsing input data fails - lets report a diagnostic
                    // note: it can happen, if i.e. member is short, and attribute has value of '6u'
                    // which can't be parsed by DefaultValue attribute implementation (TypeDescriptor.ConvertFromInvariantString())
                    // so it makes sense to report diagnostic to let user know we expect to change the value somehow.
                    return true;
                }

                return defaultValueCompiledValue != null && !defaultValueCompiledValue.Equals(memberInitValue);
            }
            
            // this is unexpected, because there is not such a ctor overload
            return false;
        }

        /// <remarks>Ensure to validate member before (i.e. calculate default value of member)</remarks>
        private bool ShouldDeclareIsRequired(Member member)
            => !member.IsRequired && !IsCollectionLike(member.MemberType);

        // A collection member is exempt from the IsRequired nag: an empty collection has no wire
        // presence to force (repeated fields write per element), initializing to an empty collection
        // is the standard pattern, and IsRequired is only observable for value-type scalars anyway.
        // This is deliberately broader than the runtime's TryGetRepeatedProvider walk, which decides
        // the *serializer*: suppressing the nag on an exotic type the runtime treats as a message is
        // harmless, where nagging on `List<T> x { get; } = [];` is not. Strings are IEnumerable but
        // are scalars, and are kept out explicitly.
        private static bool IsCollectionLike(ITypeSymbol? type)
        {
            if (type is null || type.SpecialType == SpecialType.System_String) return false;
            if (type is IArrayTypeSymbol) return true;
            if (type.SpecialType == SpecialType.System_Collections_IEnumerable) return true;
            foreach (var iface in type.AllInterfaces)
            {
                if (iface.SpecialType == SpecialType.System_Collections_IEnumerable) return true;
            }
            return false;
        }

        // protobuf-net applies a declared default by wrapping the member's serializer in a
        // DefaultValueDecorator, and ValueMember.BuildSerializer only reaches that line for a
        // non-repeated member:
        //
        //     if (_defaultValue is not null && !IsRequired && getSpecified is null)
        //
        // A repeated member never gets there at all - the decorator lives in the other arm of the
        // branch - and IsRequired is called out in the condition itself. The getSpecified arm is
        // deliberately NOT reported: pairing [DefaultValue("")] with a ShouldSerialize is precisely
        // how protogen expresses explicit presence, and there the override is the whole point.
        private bool DeclaredDefaultIsIgnored(Member member, out string? reason)
        {
            reason = null;
            if (!HasEffectiveDeclaredDefault(member)) return false;

            if (member.IsRequired)
            {
                reason = "the member declares IsRequired = true, so it is always written";
                return true;
            }

            if (IsRepeatedLike(member.MemberType))
            {
                reason = "a declared default is not applied to a repeated member";
                return true;
            }

            return false;
        }

        // The inverse of PBN0020/PBN0021: those ask whether an initializer needs a [DefaultValue],
        // this asks whether a [DefaultValue] needs an initializer. A member equal to its declared
        // default is not written, and deserialization only assigns fields that are present, so
        // without something to restore the value the two ends disagree about what "absent" means.
        private bool DeclaredDefaultCannotRoundTrip(Member member)
        {
            if (!HasEffectiveDeclaredDefault(member)) return false;

            // only the kinds CalculateMemberInitialValue can reason about; for anything else we do
            // not know what "initialized to the declared default" would even look like
            if (member.SymbolSpecialType is not { } specialType || !HonoursDeclaredDefault(specialType)) return false;

            // presence is tracked explicitly, so nothing rests on the initial value - this is the
            // shape protogen emits for a proto2 optional field, [DefaultValue("")] and all
            if (HasShouldSerializeMethod(member) || HasSpecifiedProperty(member)) return false;

            // under SkipConstructor the instance is never constructed, so a field initializer would
            // not run either; the fix this diagnostic implies would not actually fix anything
            if (HasFlag(DataContractContextFlags.SkipConstructor)) return false;

            // protogen assigns the default in a constructor rather than in an initializer
            if (IsAssignedInInstanceConstructor(member)) return false;

            return true;
        }

        // the set CalculateMemberInitialValue classifies as a constant expression: exactly the
        // members where a declared default is honoured and an initializer is the remedy
        private static bool HonoursDeclaredDefault(SpecialType specialType) => specialType switch
        {
            SpecialType.System_Boolean or SpecialType.System_Char or SpecialType.System_SByte
            or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16
            or SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64
            or SpecialType.System_UInt64 or SpecialType.System_Single or SpecialType.System_Double
            or SpecialType.System_Enum or SpecialType.System_Decimal or SpecialType.System_String => true,
            _ => false,
        };

        // Whether the member declares a default that actually declares anything.
        //
        // A null declared default means "no declared default" - ref-emit guards on
        // `_defaultValue is not null`, and the AOT generator says so in as many words. A default
        // equal to the member type's own default is inert for a different reason: the CLR already
        // gives every field that value, so there is nothing to restore and nothing to suppress.
        // protogen emits exactly that for a proto2 enum field defaulting to its zero member -
        // [DefaultValue(MessageKind.None)] with no initializer - which is entirely correct.
        private bool HasEffectiveDeclaredDefault(Member member)
        {
            var attr = GetDefaultValueAttributeData(member);
            if (attr is null) return false;

            var declared = attr.ConstructorArguments.Length switch
            {
                1 => attr.ConstructorArguments[0].Value,
                2 => attr.ConstructorArguments[1].Value,
                _ => null,
            };
            if (declared is null) return false;

            return !IsTypeDefaultValue(member.SymbolSpecialType, declared);
        }

        private static bool IsTypeDefaultValue(SpecialType? specialType, object value) => specialType switch
        {
            SpecialType.System_Boolean => value.Equals(false),
            SpecialType.System_String => value is string text && text.Length == 0,
            _ => IsNumericZero(value),
        };

        // the declared value arrives boxed as whatever the attribute argument was - an enum comes
        // through as its underlying type, which may be any integral width - so compare numerically
        // rather than by Equals against a literal 0
        private static bool IsNumericZero(object value)
        {
            try
            {
                return value switch
                {
                    float single => single == 0f,
                    double dbl => dbl == 0d,
                    decimal dec => dec == 0m,
                    string or bool => false,
                    _ => Convert.ToInt64(value, CultureInfo.InvariantCulture) == 0,
                };
            }
            catch (Exception)
            {
                // not a number at all; whatever it is, it is not the type's default
                return false;
            }
        }

        // narrower than IsCollectionLike, and deliberately so: that one over-matches on purpose,
        // because over-suppressing the IsRequired nag is harmless where over-claiming "your
        // declared default is ignored" is a false positive. A bytes-like member is a scalar to
        // protobuf-net - RepeatedSerializers.TryGetRepeatedProvider hands byte[] back as
        // not-repeated - so it keeps its declared default.
        private static bool IsRepeatedLike(ITypeSymbol? type)
            => IsCollectionLike(type) && !IsBytesLike(type);

        private static bool IsBytesLike(ITypeSymbol? type)
        {
            if (type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Byte }) return true;
            return type is INamedTypeSymbol { TypeArguments.Length: 1 } named
                && named.TypeArguments[0].SpecialType == SpecialType.System_Byte
                && named.ConstructedFrom is { } definition
                && definition.ContainingNamespace?.ToDisplayString() == "System"
                && definition.MetadataName is "Memory`1" or "ReadOnlyMemory`1" or "ArraySegment`1";
        }

        // the runtime probes for these against the *member* name - MetaType uses
        // member.Name + "Specified" where that member is the MemberInfo - which is why both use
        // MemberName rather than the possibly-renamed proto Name
        private static bool HasSpecifiedProperty(Member member)
            => member.Symbol.ContainingType.GetMembers(member.MemberName + "Specified")
                .OfType<IPropertySymbol>()
                .Any(prop => prop.Type.SpecialType == SpecialType.System_Boolean);

        private static bool HasShouldSerializeMethod(Member member)
            => member.Symbol.ContainingType.GetMembers("ShouldSerialize" + member.MemberName)
                .OfType<IMethodSymbol>()
                .Any(method => method.ReturnType.SpecialType == SpecialType.System_Boolean);

        // matched by name rather than by symbol: this only ever suppresses a warning, so a loose
        // match costs nothing, where dragging a semantic model in per constructor is not free
        private static bool IsAssignedInInstanceConstructor(Member member)
        {
            foreach (var ctor in member.Symbol.ContainingType.InstanceConstructors)
            {
                foreach (var reference in ctor.DeclaringSyntaxReferences)
                {
                    if (reference.GetSyntax() is not ConstructorDeclarationSyntax declaration) continue;
                    foreach (var assignment in declaration.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                    {
                        switch (assignment.Left)
                        {
                            case IdentifierNameSyntax id when id.Identifier.ValueText == member.MemberName:
                                return true;
                            case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access
                                when access.Name.Identifier.ValueText == member.MemberName:
                                return true;
                        }
                    }
                }
            }
            return false;
        }

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

            if (GetDefaultValueAttributeData(member) is not null || HasShouldSerializeMethod(member))
            {
                // we already have required attributes defined
                return false;
            }

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

        internal void SetOtherContractFamily() => _flags |= DataContractContextFlags.HasOtherContractFamily;

        internal void SetContract(ISymbol blame, AttributeData attrib)
        {
            _ = blame;
            _flags |= DataContractContextFlags.IsProtoContract;
            if (attrib.TryGetBooleanByName(nameof(ProtoContractAttribute.SkipConstructor), out var val) && val)
                _flags |= DataContractContextFlags.SkipConstructor;
            if (attrib.TryGetBooleanByName(nameof(ProtoContractAttribute.IgnoreUnknownSubTypes), out val) && val)
                _flags |= DataContractContextFlags.IgnoreUnknownSubTypes;
            foreach (var named in attrib.NamedArguments)
            {
                if (named.Key == nameof(ProtoContractAttribute.Surrogate) && named.Value.Value is not null)
                {
                    _flags |= DataContractContextFlags.HasSurrogate;
                }
            }
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
