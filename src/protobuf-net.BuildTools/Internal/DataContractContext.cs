#nullable enable
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using ProtoBuf.BuildTools.Analyzers;
using ProtoBuf.Internal.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
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

                if (HasFlag(DataContractContextFlags.SkipConstructor) && OwnsSkipConstructorBlame(ref context))
                {
                    // no arrangement of the member makes this correct - the initializer PBN0024
                    // would ask for is exactly what SkipConstructor skips - so it is the pairing
                    // itself that gets reported, once, at the option responsible
                    var lost = _members
                        .Where(DeclaredDefaultLostToSkipConstructor)
                        .Select(candidate => "'" + candidate.MemberName + "'")
                        .ToList();
                    if (lost.Count != 0)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: DataContractAnalyzer.DeclaredDefaultUnderSkipConstructor,
                            location: Utils.PickLocation(ref context, _skipConstructorBlame),
                            messageArgs: new object[] { string.Join(", ", lost) },
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

        // Not for a nullable member either: a Nullable<T> is written whenever it has a value, zero
        // included, and a reference whenever it is not null, so IsRequired changes nothing on the
        // wire (probed) - and alongside [NullWrappedValue] it makes protobuf-net refuse the model
        // outright. What such a member can lose is null, which IsRequired does not help with either
        // (probed); that is PBN0029's business.
        /// <remarks>Ensure to validate member before (i.e. calculate default value of member)</remarks>
        private bool ShouldDeclareIsRequired(Member member)
            => !member.IsRequired && !IsCollectionLike(member.MemberType) && !IsNullableMember(member);

        // Nullable<T>, or a reference the author annotated `?`; an oblivious one has promised nothing
        private static bool IsNullableMember(Member member) => member.Symbol switch
        {
            IPropertySymbol property => IsNullable(property.Type, property.NullableAnnotation),
            IFieldSymbol field => IsNullable(field.Type, field.NullableAnnotation),
            _ => false,
        };

        private static bool IsNullable(ITypeSymbol type, NullableAnnotation annotation)
            => type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                || (type.IsReferenceType && annotation == NullableAnnotation.Annotated);

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
            // not run either; the fix this diagnostic implies would not actually fix anything, and
            // the pairing is reported once per contract as PBN0026 instead
            if (HasFlag(DataContractContextFlags.SkipConstructor)) return false;

            // protogen assigns the default in a constructor rather than in an initializer
            if (IsAssignedInInstanceConstructor(member)) return false;

            return true;
        }

        // A declared default that SkipConstructor makes unreachable. Unlike PBN0024 this does not
        // care whether an initializer is present: the initializer does not run either, so there is
        // no way to write the member that restores the value. Explicit presence is the exception -
        // it replaces the declared-default guard, so the value still goes to the wire - and a
        // default that is never applied at all cannot be lost, which is PBN0025's territory.
        private bool DeclaredDefaultLostToSkipConstructor(Member member)
        {
            if (!HasEffectiveDeclaredDefault(member)) return false;
            if (member.SymbolSpecialType is not { } specialType || !HonoursDeclaredDefault(specialType)) return false;
            if (HasShouldSerializeMethod(member) || HasSpecifiedProperty(member)) return false;
            if (DeclaredDefaultIsIgnored(member, out _)) return false;
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

            // "" counts as the default for a string even though the CLR default is null, so
            // [DefaultValue("")] never reports. Strictly it can lose data - a sender holding ""
            // writes nothing and the receiver is left at null - but null and "" are treated as
            // interchangeable often enough that a diagnostic here would confuse more than it
            // helps. Deliberate; do not tighten this without a reason to.
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
            => HasSpecifiedProperty(member.Symbol.ContainingType, member.MemberName);

        private static bool HasSpecifiedProperty(INamedTypeSymbol type, string memberName)
            => type.GetMembers(memberName + "Specified")
                .OfType<IPropertySymbol>()
                .Any(prop => prop.Type.SpecialType == SpecialType.System_Boolean);

        private static bool HasShouldSerializeMethod(Member member)
            => HasShouldSerializeMethod(member.Symbol.ContainingType, member.MemberName);

        private static bool HasShouldSerializeMethod(INamedTypeSymbol type, string memberName)
            => type.GetMembers("ShouldSerialize" + memberName)
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
                        if (TargetName(assignment.Left) == member.MemberName) return true;
                    }
                }
            }
            return false;
        }

        // What deserialization leaves in a member that the payload does not carry: whatever
        // construction left there, since nothing is written for an empty collection or a null value.
        // Three rules turn on that one question; docs/rules has each in full, and every runtime
        // behaviour relied on here was probed against RuntimeTypeModel.
        //
        // - PBN0028: a non-nullable collection left null - by SkipConstructor, by a struct contract,
        //   or by nothing assigning it in the constructor protobuf-net calls;
        // - PBN0030: any other non-nullable member left null. A non-null string or message is
        //   written even when empty, so this is only reported where no initializer can run at all;
        // - PBN0029: a nullable member whose initializer turns a null into a value.
        internal void ReportNullsOnDeserialize(SyntaxNodeAnalysisContext context, INamedTypeSymbol type)
        {
            // with a surrogate protobuf-net constructs the surrogate and converts, and with a
            // hand-written serializer that serializer does the constructing - never us, either way
            if (!HasFlag(DataContractContextFlags.IsProtoContract)
                || HasFlag(DataContractContextFlags.HasSurrogate)
                || HasFlag(DataContractContextFlags.HasSerializer)
                || type.TypeKind == TypeKind.Interface) return;

            // a type protobuf-net treats as a collection is serialized as that collection, and its
            // members are never read
            if (IsRepeated(type)) return;

            // null when the type is constructed as usual, otherwise why it is not. SkipConstructor
            // belongs to the concrete type being read, never to its bases: it is inert on an abstract
            // type, and a sub-type that sets it strips the members it inherits from here
            bool skipsItself = HasFlag(DataContractContextFlags.SkipConstructor) && !type.IsAbstract;
            var (skippingSubType, constructedSubType) = type.IsValueType ? (null, false) : WalkSubTypes(type);
            string? notConstructed = type.IsValueType
                ? "a struct contract is never constructed on deserialize, so no constructor or initializer runs"
                : skipsItself
                ? "SkipConstructor means no constructor or initializer runs on deserialize"
                : skippingSubType is not null
                ? $"SkipConstructor on the sub-type '{skippingSubType.Name}' means no constructor or initializer runs when one is deserialized"
                : null;

            // ...and whether an initializer runs on some path: this type constructed as usual, a
            // sub-type that is, or an abstract type whose sub-types cannot all be seen
            bool initializerRuns = !type.IsValueType
                && (constructedSubType || (type.IsAbstract ? skippingSubType is null : !skipsItself));

            // worked out at most once, and only for a type with a member that needs them
            HashSet<string>? assignedByConstructor = null, restored = null, resetBeforeRead = null;
            bool constructorsUnanswerable = false;

            foreach (var candidate in type.GetMembers())
            {
                if (candidate.IsStatic) continue; // const included
                ISymbol member;
                ITypeSymbol memberType;
                NullableAnnotation annotation;
                switch (candidate)
                {
                    // an auto-property's storage is its backing field, which is how a getter-only
                    // one and a positional record parameter are caught; reported as the property,
                    // since that is what the reader wrote
                    case IFieldSymbol { IsImplicitlyDeclared: true, AssociatedSymbol: IPropertySymbol property }:
                        (member, memberType, annotation) = (property, property.Type, property.NullableAnnotation);
                        break;
                    case IFieldSymbol { IsImplicitlyDeclared: false } field:
                        (member, memberType, annotation) = (field, field.Type, field.NullableAnnotation);
                        break;
                    default:
                        continue;
                }

                // an oblivious reference has promised nothing either way, and a plain value type
                // cannot be null
                bool nullable = IsNullable(memberType, annotation);
                if (!nullable && (!memberType.IsReferenceType || annotation != NullableAnnotation.NotAnnotated)) continue;

                // a partial type is visited once per declaration; the part declaring the member owns
                // it - tested before anything that walks interfaces or attributes, since every other
                // part of the type throws the member away here
                if (member.Locations.IsEmpty
                    || member.Locations[0] is not { SourceTree: not null } location
                    || location.SourceTree != context.Node.SyntaxTree
                    || !context.Node.FullSpan.Contains(location.SourceSpan)) continue;

                if (nullable) ReportIfInitializerLosesNull(member, memberType, location);
                else if (IsRepeated(memberType)) ReportIfCollectionLeftNull(member, location);
                else ReportIfLeftNull(member, location);
            }

            // PBN0029
            void ReportIfInitializerLosesNull(ISymbol member, ITypeSymbol memberType, Location location)
            {
                // only where the initializer runs, on a member that is written, and where nothing else
                // tracks presence: {Name}Specified does, being assigned on read, but ShouldSerialize{Name}
                // only decides the write. A collection that is not null-wrapped writes null and empty
                // alike - as nothing - so one comes back as the other with or without the initializer
                if (!initializerRuns || !HasProtoBufAttribute(member, nameof(ProtoMemberAttribute))
                    || (IsRepeated(memberType) && !HasProtoBufAttribute(member, nameof(NullWrappedCollectionAttribute)))
                    || !HasNonNullInitializer(member, context.SemanticModel, context.CancellationToken)
                    || HasSpecifiedProperty(type, member.Name)) return;

                // only a callback that runs before the fields are read can put the null back
                resetBeforeRead ??= AssignedInDeserializationCallbacks(type, beforeReadOnly: true, context.CancellationToken);
                if (resetBeforeRead.Contains(member.Name)) return;

                bool declaredDefault = HasDeclaredDefault(member);
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DataContractAnalyzer.NullableMemberInitialized,
                    location: location,
                    messageArgs: new object[]
                    {
                        member.Name,
                        memberType is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullableValue
                            && HasProtoBufAttribute(member, nameof(NullWrappedValueAttribute)) && WrapsZeroAsEmpty(nullableValue.TypeArguments[0])
                            ? ", as does a zero or false, which [NullWrappedValue] writes as an empty wrapper"
                            : "",
                        DescribeNullableFixes(declaredDefault, IsAssignableAfterConstruction(member)),
                    },
                    additionalLocations: null,
                    properties: declaredDefault
                        ? DiagnosticPropertiesBuilder.Create().Add(NullableMemberDeclaredDefaultKey, "true").Build()
                        : null
                ));
            }

            // PBN0030
            void ReportIfLeftNull(ISymbol member, Location location)
            {
                if (notConstructed is null) return;
                restored ??= AssignedInDeserializationCallbacks(type, beforeReadOnly: false, context.CancellationToken);
                if (restored.Contains(member.Name)) return;

                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DataContractAnalyzer.NonNullableMemberLeftNull,
                    location: location,
                    messageArgs: new object[]
                    {
                        member.Name,
                        notConstructed,
                        IsWritten(member) ? "it is null whenever the payload does not carry it" : "it is not serialized, so it is null after every deserialize",
                        DescribeFixes(constructed: false, hasProtoMember: false, IsAssignableAfterConstruction(member)),
                    },
                    additionalLocations: null,
                    // the same code fix serves it, and "not constructed" rules out initializing it
                    properties: DiagnosticPropertiesBuilder.Create()
                        .Add(CollectionLeftNullConstructedKey, "false")
                        .Build()
                ));
            }

            // PBN0028
            void ReportIfCollectionLeftNull(ISymbol member, Location location)
            {
                if (HasProtoBufAttribute(member, nameof(NullWrappedCollectionAttribute))) return;

                if (notConstructed is null)
                {
                    // constructed as usual, a member that is never written is whatever the constructor
                    // left, deserialized or not - the compiler's business, not ours
                    if (!IsWritten(member) || HasNonNullInitializer(member, context.SemanticModel, context.CancellationToken)) return;

                    // a type with no answer - a `: this(...)` chain, or no constructor to call - reports
                    // no collection that would rest on it
                    if (assignedByConstructor is null)
                    {
                        constructorsUnanswerable = !TryGetAssignedByConstructor(type, context.CancellationToken, out assignedByConstructor);
                    }
                    if (constructorsUnanswerable || assignedByConstructor.Contains(member.Name)) return;
                }

                restored ??= AssignedInDeserializationCallbacks(type, beforeReadOnly: false, context.CancellationToken);
                if (restored.Contains(member.Name)) return;

                // [NullWrappedCollection] is only offered where [ProtoMember] says the member is written:
                // conservative, since ImplicitFields and [DataMember] write it too
                bool hasProtoMember = HasProtoBufAttribute(member, nameof(ProtoMemberAttribute));
                bool constructed = notConstructed is null;
                context.ReportDiagnostic(Diagnostic.Create(
                    descriptor: DataContractAnalyzer.NonNullableCollectionLeftNull,
                    location: location,
                    messageArgs: new object[]
                    {
                        member.Name,
                        notConstructed ?? "nothing assigns it when protobuf-net constructs the instance",
                        DescribeFixes(constructed, hasProtoMember, IsAssignableAfterConstruction(member)),
                    },
                    additionalLocations: null,
                    properties: DiagnosticPropertiesBuilder.Create()
                        .Add(CollectionLeftNullConstructedKey, constructed ? "true" : "false")
                        .Add(CollectionLeftNullHasProtoMemberKey, hasProtoMember ? "true" : "false")
                        .Build()
                ));
            }
        }

        // whether protobuf-net writes the member: named by [ProtoMember] or [ProtoPartialMember], by
        // [DataMember] (the families mix), or perhaps by ImplicitFields, which is taken on trust
        private bool IsWritten(ISymbol member)
        {
            if (_members is not null && _members.Any(written => written.MemberName == member.Name)) return true;
            if (member.GetAttributes().Any(attrib => attrib.AttributeClass is { Name: "DataMemberAttribute" } ac
                && ac.InNamespace("System", "Runtime", "Serialization"))) return true;
            return !ShouldIgnore(member.Name)
                && GetProtoContract(member.ContainingType) is { } contract
                && contract.TryGetByName(nameof(ProtoContractAttribute.ImplicitFields), out var implicitFields)
                && implicitFields.Value is int mode && mode != 0;
        }

        // which remedies apply, for the message and for the code fix: an initializer only helps when
        // a constructor runs, null-wrapping only when the member is written, and a callback is the
        // one thing that works when nothing is constructed - provided the callback may assign it
        internal const string CollectionLeftNullConstructedKey = "Constructed";
        internal const string CollectionLeftNullHasProtoMemberKey = "HasProtoMember";

        private static string DescribeFixes(bool constructed, bool hasProtoMember, bool assignable)
        {
            var fixes = new List<string> { "declare it nullable" };
            if (constructed) fixes.Add("initialize it");
            if (hasProtoMember) fixes.Add("mark it [NullWrappedCollection] so that an empty one is written (this changes the wire format)");
            if (!constructed && assignable) fixes.Add("restore it in a deserialization callback");
            return fixes.Count == 1 ? fixes[0]
                : string.Join(", ", fixes.Take(fixes.Count - 1)) + ", or " + fixes[fixes.Count - 1];
        }

        // whether an ordinary method - which a callback is - may assign the member: not a readonly
        // field (CS0191), a getter-only property (CS0200), nor an init-only one (CS8852), which is
        // what a positional record's parameter becomes
        private static bool IsAssignableAfterConstruction(ISymbol member) => member switch
        {
            IFieldSymbol field => !field.IsReadOnly,
            IPropertySymbol property => property.SetMethod is { IsInitOnly: false },
            _ => false,
        };

        // PBN0029's remedies. A declared default stops its own value being written, so it has to go
        // too: without the initializer, or reset to null, the receiver would get null for it instead
        internal const string NullableMemberDeclaredDefaultKey = "DeclaredDefault";

        private static string DescribeNullableFixes(bool declaredDefault, bool assignable)
        {
            var remove = declaredDefault ? "remove the initializer and the [DefaultValue]" : "remove the initializer";
            if (!assignable) return remove;
            return remove + ", or " + (declaredDefault ? "remove the [DefaultValue] and " : "")
                + "reset it to null in a [ProtoBeforeDeserialization] callback";
        }

        // a [DefaultValue] that declares anything: null means "no declared default" (see
        // HasEffectiveDeclaredDefault), but on a nullable member even a zero is a value that is then
        // not written, so that is all that is ruled out here
        private static bool HasDeclaredDefault(ISymbol member)
        {
            foreach (var attrib in member.GetAttributes())
            {
                if (attrib.AttributeClass is not { Name: nameof(DefaultValueAttribute) } ac
                    || !ac.InNamespace(nameof(System), nameof(System.ComponentModel))) continue;
                var args = attrib.ConstructorArguments;
                return args.Length switch
                {
                    1 => !args[0].IsNull,
                    2 => !args[1].IsNull,
                    _ => false,
                };
            }
            return false;
        }

        // The concrete types below this one, through [ProtoInclude] at any depth: the first whose own
        // SkipConstructor means it is read without being constructed, and whether any is constructed
        // as usual. Only [ProtoInclude] is visible here: a sub-type added at run time with AddSubType
        // is not, and is missed.
        private static (INamedTypeSymbol? Skipping, bool AnyConstructed) WalkSubTypes(INamedTypeSymbol type)
        {
            INamedTypeSymbol? skipping = null;
            bool anyConstructed = false;
            var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default) { type };
            var pending = new Stack<INamedTypeSymbol>();
            pending.Push(type);
            while (pending.Count != 0 && (skipping is null || !anyConstructed))
            {
                foreach (var attrib in pending.Pop().GetAttributes())
                {
                    if (attrib.AttributeClass is not { Name: nameof(ProtoIncludeAttribute) } ac || !ac.InProtoBufNamespace()
                        || !attrib.TryGetTypeByName(nameof(ProtoIncludeAttribute.KnownType), out var known)
                        || known is not INamedTypeSymbol subType || !seen.Add(subType)) continue;
                    if (!subType.IsAbstract)
                    {
                        if (GetProtoContract(subType) is { } contract
                            && contract.TryGetBooleanByName(nameof(ProtoContractAttribute.SkipConstructor), out var skip) && skip)
                        {
                            skipping ??= subType;
                        }
                        else
                        {
                            anyConstructed = true;
                        }
                    }
                    pending.Push(subType);
                }
            }
            return (skipping, anyConstructed);
        }

        // The names assigned by whichever constructors can run when protobuf-net creates the type:
        // the parameterless one, or - for an abstract type, where the concrete type picks - any of
        // them. False when that cannot be answered, which stands the whole check down: no
        // constructor to call is PBN0015's error rather than ours, and a `: this(...)` chain could
        // assign anything (a positional record's parameterless constructor must have one).
        private static bool TryGetAssignedByConstructor(INamedTypeSymbol type, CancellationToken cancellationToken, out HashSet<string> assigned)
        {
            assigned = new HashSet<string>(StringComparer.Ordinal);
            bool any = false;
            foreach (var ctor in type.InstanceConstructors)
            {
                if (ctor.Parameters.Length != 0 && !type.IsAbstract) continue;
                any = true;
                foreach (var reference in ctor.DeclaringSyntaxReferences)
                {
                    if (reference.GetSyntax(cancellationToken) is not ConstructorDeclarationSyntax declaration)
                    {
                        // a primary constructor, declared by the type itself: a positional record's
                        // assigns its members from its parameters, which only an abstract type reaches
                        if (ctor.Parameters.Length != 0) return false;
                        continue;
                    }
                    if (declaration.Initializer?.IsKind(SyntaxKind.ThisConstructorInitializer) == true) return false;
                    CollectAssigned(declaration, type, assigned);
                }
            }
            return any;
        }

        // an initializer runs as part of every constructor, so anything but a spelled-out null counts
        private static bool HasNonNullInitializer(ISymbol member, SemanticModel model, CancellationToken cancellationToken)
        {
            foreach (var reference in member.DeclaringSyntaxReferences)
            {
                var value = reference.GetSyntax(cancellationToken) switch
                {
                    PropertyDeclarationSyntax property => property.Initializer?.Value,
                    VariableDeclaratorSyntax variable => variable.Initializer?.Value,
                    _ => null,
                };
                if (value is not null && !IsSpelledOutNull(value, model, cancellationToken)) return true;
            }
            return false;
        }

        // `null`, `default` or `default(T)`, with or without `!` or a cast: how CS8618 is routinely
        // silenced on a DTO, and it assigns nothing. Asked of the semantic model rather than read off
        // the syntax, since on a Nullable<T> `default(int)` is a zero and `(int?)null` is a null.
        // Shared with the code fix, which removes exactly what this treats as no initializer - were
        // the two to drift, it would leave one behind or take a real one away
        internal static bool IsSpelledOutNull(ExpressionSyntax value, SemanticModel model, CancellationToken cancellationToken)
        {
            while (true)
            {
                switch (value)
                {
                    case PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.SuppressNullableWarningExpression):
                        value = postfix.Operand;
                        continue;
                    case ParenthesizedExpressionSyntax parenthesized:
                        value = parenthesized.Expression;
                        continue;
                }
                break;
            }
            return value.SyntaxTree == model.SyntaxTree
                && model.GetOperation(value, cancellationToken) is { } operation
                && IsNull(operation);

            // a conversion to a non-nullable value type yields a value whatever it converts, and a
            // default is null exactly when its type can be
            static bool IsNull(IOperation operation) => operation switch
            {
                IConversionOperation conversion => CanBeNull(conversion.Type) && IsNull(conversion.Operand),
                IDefaultValueOperation @default => CanBeNull(@default.Type),
                _ => operation.ConstantValue is { HasValue: true, Value: null },
            };

            static bool CanBeNull(ITypeSymbol? type) => type is null || type.IsReferenceType
                || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                || type is ITypeParameterSymbol { IsValueType: false };
        }

        // What protobuf-net writes as a repeated field: an array or an IEnumerable<T>, bytes and
        // strings aside. IsRepeatedLike also takes the non-generic IEnumerable, which the runtime
        // treats as an ordinary message (probed), and so is an IgnoreListHandling type - and an
        // empty message *is* written. [NullWrappedCollection] is what makes an empty one written too
        private static bool IsRepeated(ITypeSymbol type)
            => IsRepeatedLike(type)
                && (type is IArrayTypeSymbol || IsEnumerableOfT(type) || type.AllInterfaces.Any(IsEnumerableOfT))
                && !(GetProtoContract(type) is { } contract
                    && contract.TryGetBooleanByName(nameof(ProtoContractAttribute.IgnoreListHandling), out var ignore) && ignore);

        private static bool IsEnumerableOfT(ITypeSymbol type)
            => type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T;

        private static AttributeData? GetProtoContract(ITypeSymbol type)
            => type.GetAttributes().FirstOrDefault(attrib => attrib.AttributeClass is { Name: nameof(ProtoContractAttribute) } ac
                && ac.InProtoBufNamespace());

        private static bool HasProtoBufAttribute(ISymbol member, string name)
            => member.GetAttributes().Any(attrib => attrib.AttributeClass is { } ac && ac.Name == name && ac.InProtoBufNamespace());

        // inside a [NullWrappedValue] wrapper the value is an ordinary field at 1, so a zero or false
        // is left out and the wrapper goes out empty - and an empty wrapper, read into an existing
        // value, leaves it alone (probed for bool, char, byte, short, int, uint, long, ulong, nint,
        // float and double). Not so for an enum, whose zero is written explicitly, nor a string,
        // which is written even empty
        private static bool WrapsZeroAsEmpty(ITypeSymbol valueType) => valueType.SpecialType switch
        {
            SpecialType.System_Boolean or SpecialType.System_Char
            or SpecialType.System_SByte or SpecialType.System_Byte
            or SpecialType.System_Int16 or SpecialType.System_UInt16
            or SpecialType.System_Int32 or SpecialType.System_UInt32
            or SpecialType.System_Int64 or SpecialType.System_UInt64
            or SpecialType.System_IntPtr or SpecialType.System_UIntPtr
            or SpecialType.System_Single or SpecialType.System_Double => true,
            _ => false,
        };

        // The names a deserialization callback assigns: the one thing that restores a member however
        // the instance was made, since it runs on the new instance before or after the fields are
        // read - or, with beforeReadOnly, only before, which is the one point where resetting a
        // member to null still lets the payload decide. Only the hierarchy root's callbacks run -
        // TypeSerializer checks IsRootType - so on a sub-type a method counts only by overriding one
        // the root declares, and its own attribute does nothing (probed). Matched by name, as
        // IsAssignedInInstanceConstructor is, since this only ever suppresses a warning; `Items ??=
        // new();` in an after-hook is the idiomatic form.
        private static HashSet<string> AssignedInDeserializationCallbacks(INamedTypeSymbol type, bool beforeReadOnly, CancellationToken cancellationToken)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            bool isSubType = IsProtoSubType(type);
            foreach (var method in type.GetMembers().OfType<IMethodSymbol>())
            {
                if (!(isSubType ? OverridesDeserializationCallback(method, beforeReadOnly) : IsDeserializationCallback(method, beforeReadOnly))) continue;
                foreach (var reference in method.DeclaringSyntaxReferences)
                {
                    CollectAssigned(reference.GetSyntax(cancellationToken), type, names);
                }
            }
            return names;
        }

        // below another contract in a hierarchy: a base class or interface whose [ProtoInclude] names
        // this type. A contract base that does *not* include it leaves this type a root of its own,
        // whose callbacks do run (probed) - PBN0013 is what reports that shape
        private static bool IsProtoSubType(INamedTypeSymbol type)
        {
            if (type.BaseType is { } baseType && Includes(baseType)) return true;
            foreach (var iface in type.AllInterfaces)
            {
                if (Includes(iface)) return true;
            }
            return false;

            bool Includes(INamedTypeSymbol candidate)
            {
                foreach (var attrib in candidate.GetAttributes())
                {
                    if (attrib.AttributeClass is { Name: nameof(ProtoIncludeAttribute) } ac && ac.InProtoBufNamespace()
                        && attrib.TryGetTypeByName(nameof(ProtoIncludeAttribute.KnownType), out var known)
                        && SymbolEqualityComparer.Default.Equals(known.OriginalDefinition, type.OriginalDefinition))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static bool OverridesDeserializationCallback(IMethodSymbol method, bool beforeReadOnly)
        {
            for (var overridden = method.OverriddenMethod; overridden is not null; overridden = overridden.OverriddenMethod)
            {
                if (IsDeserializationCallback(overridden, beforeReadOnly)) return true;
            }
            return false;
        }

        // both callback families MetaType honours, at the two points that run on the receiving end
        private static bool IsDeserializationCallback(IMethodSymbol method, bool beforeReadOnly)
        {
            foreach (var attrib in method.GetAttributes())
            {
                var ac = attrib.AttributeClass;
                if (ac is null) continue;
                switch (ac.Name)
                {
                    case nameof(ProtoBeforeDeserializationAttribute) when ac.InProtoBufNamespace():
                    case "OnDeserializingAttribute" when ac.InNamespace("System", "Runtime", "Serialization"):
                        return true;
                    case nameof(ProtoAfterDeserializationAttribute) when !beforeReadOnly && ac.InProtoBufNamespace():
                    case "OnDeserializedAttribute" when !beforeReadOnly && ac.InNamespace("System", "Runtime", "Serialization"):
                        return true;
                }
            }
            return false;
        }

        // What a constructor or callback body assigns, by name: `x = ...` and `this.x = ...` (so
        // `??=` too), each element of a tuple deconstruction, and whatever [MemberNotNull] promises
        // for a helper it calls or a property setter it assigns through - which is how nullable-aware
        // code initializes from a helper, and the compiler accepts it as initialized.
        private static void CollectAssigned(SyntaxNode body, INamedTypeSymbol type, HashSet<string> names)
        {
            foreach (var node in body.DescendantNodes())
            {
                switch (node)
                {
                    case AssignmentExpressionSyntax assignment:
                        AddTarget(assignment.Left);
                        break;
                    case InvocationExpressionSyntax invocation when TargetName(invocation.Expression) is { } called:
                        foreach (var method in type.GetMembers(called).OfType<IMethodSymbol>()) AddMemberNotNull(method);
                        break;
                }
            }

            void AddTarget(ExpressionSyntax target)
            {
                if (target is TupleExpressionSyntax tuple)
                {
                    foreach (var argument in tuple.Arguments) AddTarget(argument.Expression);
                }
                else if (TargetName(target) is { } name && names.Add(name))
                {
                    foreach (var property in type.GetMembers(name).OfType<IPropertySymbol>()) AddMemberNotNull(property.SetMethod);
                }
            }

            void AddMemberNotNull(IMethodSymbol? method)
            {
                if (method is null) return;
                foreach (var attrib in method.GetAttributes())
                {
                    if (attrib.AttributeClass is not { Name: "MemberNotNullAttribute" } ac
                        || !ac.InNamespace("System", "Diagnostics", "CodeAnalysis")
                        || attrib.ConstructorArguments.Length != 1) continue;
                    var arg = attrib.ConstructorArguments[0];
                    if (arg.Kind == TypedConstantKind.Array)
                    {
                        foreach (var value in arg.Values)
                        {
                            if (value.Value is string member) names.Add(member);
                        }
                    }
                    else if (arg.Value is string member)
                    {
                        names.Add(member);
                    }
                }
            }
        }

        // `x` or `this.x`; anything else is not recognisably ours
        private static string? TargetName(ExpressionSyntax expression) => expression switch
        {
            IdentifierNameSyntax id => id.Identifier.ValueText,
            MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax } access => access.Name.Identifier.ValueText,
            _ => null,
        };

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

            // a nullable member's initializer is written whenever it is set, and what it can lose is
            // null - which PBN0029 reports. A declared default would stop that value being written,
            // so a receiver that resets the member to null, as PBN0029 suggests, would lose it
            if (IsNullableMember(member))
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
            {
                _flags |= DataContractContextFlags.SkipConstructor;
                // PBN0026 is a fact about the contract rather than about any one member, so it is
                // reported once, against the option that causes it
                _skipConstructorBlame = NamedArgumentLocation(attrib, nameof(ProtoContractAttribute.SkipConstructor))
                    ?? attrib.GetLocation(blame);
            }
            if (attrib.TryGetBooleanByName(nameof(ProtoContractAttribute.IgnoreUnknownSubTypes), out val) && val)
                _flags |= DataContractContextFlags.IgnoreUnknownSubTypes;
            if (attrib.TryGetBooleanByName(nameof(ProtoContractAttribute.IgnoreListHandling), out val) && val)
                _flags |= DataContractContextFlags.IgnoreListHandling;
            foreach (var named in attrib.NamedArguments)
            {
                if (named.Key == nameof(ProtoContractAttribute.Surrogate) && named.Value.Value is not null)
                {
                    _flags |= DataContractContextFlags.HasSurrogate;
                }
                if (named.Key == nameof(ProtoContractAttribute.Serializer) && named.Value.Value is not null)
                {
                    _flags |= DataContractContextFlags.HasSerializer;
                }
            }
        }

        private Location? _skipConstructorBlame;

        // A partial type is visited once per declaration, and this is a fact about the type rather
        // than about any one part, so it must not be reported once per file. The part that actually
        // writes `SkipConstructor` is the one that owns it; if there is no syntax to go on, report
        // rather than stay silent, since a duplicate is a smaller failure than a miss.
        private bool OwnsSkipConstructorBlame(ref SyntaxNodeAnalysisContext context)
        {
            if (_skipConstructorBlame is not { } blame || blame.SourceTree is null) return true;
            return blame.SourceTree == context.Node.SyntaxTree
                && context.Node.FullSpan.Contains(blame.SourceSpan);
        }

        // the whole attribute is a serviceable fallback, but `SkipConstructor = true` is the thing
        // the reader has to decide about, so point at that argument where the syntax is available
        private static Location? NamedArgumentLocation(AttributeData attrib, string name)
        {
            if (attrib.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax syntax) return null;
            var arguments = syntax.ArgumentList?.Arguments;
            if (arguments is null) return null;
            foreach (var argument in arguments)
            {
                if (argument.NameEquals?.Name.Identifier.ValueText == name) return argument.GetLocation();
            }
            return null;
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
