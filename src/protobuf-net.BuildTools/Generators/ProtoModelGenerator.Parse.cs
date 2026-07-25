#nullable enable
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProtoBuf.BuildTools.Internal.Aot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace ProtoBuf.BuildTools.Generators
{
    partial class ProtoModelGenerator
    {
        private const string ProtoContractAttributeName = "ProtoBuf.ProtoContractAttribute";
        private const string ProtoMemberAttributeName = "ProtoBuf.ProtoMemberAttribute";
        private const string DefaultValueAttributeName = "System.ComponentModel.DefaultValueAttribute";
        private const string DataContractAttributeName = "System.Runtime.Serialization.DataContractAttribute";
        private const string DataMemberAttributeName = "System.Runtime.Serialization.DataMemberAttribute";
        private const string XmlTypeAttributeName = "System.Xml.Serialization.XmlTypeAttribute";
        private const string XmlElementAttributeName = "System.Xml.Serialization.XmlElementAttribute";
        private const string XmlArrayAttributeName = "System.Xml.Serialization.XmlArrayAttribute";
        private const string XmlIgnoreAttributeName = "System.Xml.Serialization.XmlIgnoreAttribute";
        private const string NonSerializedAttributeName = "System.NonSerializedAttribute";
        private const string ProtoBufNamespace = "ProtoBuf";

        /// <summary>
        /// Project a <c>[ProtoModel]</c> declaration onto a plan, plus the diagnostics explaining
        /// anything that had to be left out.
        /// </summary>
        /// <remarks>
        /// Deliberately conservative: anything whose semantics this generator does not yet fully
        /// reproduce causes the contract to be dropped, because emitting a subtly wrong serializer
        /// is far worse than emitting none. Every drop must say why - a silently missing serializer
        /// surfaces much later as an unexplained "no serializer for type" throw.
        /// </remarks>
        private static ProtoParseResult? Parse(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            if (context.TargetSymbol is not INamedTypeSymbol model) return null;
            var compilation = context.SemanticModel.Compilation;

            // TODO: nested and generic model types
            if (model.ContainingType is not null || model.IsGenericType) return null;

            var diagnostics = new List<PlanDiagnostic>();
            var parsed = new Dictionary<string, ProtoContractPlan>(StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Queue<INamedTypeSymbol>();

            // locations are tracked here rather than on the plan: they move whenever anything above
            // them moves, and putting them in the plan would invalidate the cached emit step
            var locations = new Dictionary<string, PlanLocation>(StringComparer.Ordinal);

            foreach (var attribute in model.GetAttributes())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (attribute.AttributeClass?.ToDisplayString() != ProtoSerializableAttributeName) continue;
                if (attribute.ConstructorArguments.Length != 1) continue;
                if (attribute.ConstructorArguments[0].Value is INamedTypeSymbol seed) pending.Enqueue(seed);
            }

            // walk the transitive closure: a seed's members can reach further contracts, and the same
            // contract is commonly reachable by several paths (or seeded *and* reachable)
            while (pending.Count != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var type = pending.Dequeue();
                var key = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                if (!visited.Add(key)) continue;

                locations[key] = PlanLocation.From(type);

                var contract = ParseContract(compilation, type, diagnostics, out var reachable, cancellationToken);
                if (contract is not null) parsed.Add(key, contract);
                foreach (var next in reachable) pending.Enqueue(next);
            }

            DropUnsatisfiable(parsed, locations, diagnostics);

            ProtoModelPlan? plan = null;
            if (parsed.Count != 0)
            {
                var contracts = parsed.Values.OrderBy(static x => x.TypeName, StringComparer.Ordinal).ToArray();
                var nameSpace = model.ContainingNamespace is { IsGlobalNamespace: false } ns
                    ? ns.ToDisplayString() : null;
                plan = new ProtoModelPlan(nameSpace, model.Name, new(contracts));
            }

            return new ProtoParseResult(plan, new(diagnostics.ToArray()));
        }

        /// <summary>
        /// Remove any contract that references a contract we could not handle, to a fixed point.
        /// </summary>
        /// <remarks>
        /// Without this the emitted services type would call <c>ReadMessage&lt;T&gt;(..., this)</c>
        /// for a <c>T</c> it does not implement <c>ISerializer&lt;T&gt;</c> for, which would not
        /// compile. Dropping a contract can strand its referrers, hence the loop.
        /// </remarks>
        private static void DropUnsatisfiable(
            Dictionary<string, ProtoContractPlan> parsed,
            Dictionary<string, PlanLocation> locations,
            List<PlanDiagnostic> diagnostics)
        {
            bool removed;
            do
            {
                removed = false;
                foreach (var contract in parsed.Values.ToList())
                {
                    foreach (var member in contract.Members)
                    {
                        if (member.Kind != ProtoMemberKind.Message) continue;
                        if (member.TypeName is not null && parsed.ContainsKey(member.TypeName)) continue;

                        parsed.Remove(contract.TypeName);
                        locations.TryGetValue(contract.TypeName, out var location);
                        diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.OmittedCascade, location,
                            Simplify(contract.TypeName), Simplify(member.TypeName)));
                        removed = true;
                        break;
                    }
                }
            }
            while (removed);
        }

        private static ProtoContractPlan? ParseContract(
            Compilation compilation,
            INamedTypeSymbol type,
            List<PlanDiagnostic> diagnostics,
            out List<INamedTypeSymbol> reachable,
            CancellationToken cancellationToken)
        {
            reachable = new List<INamedTypeSymbol>();

            var at = PlanLocation.From(type);
            var name = type.ToDisplayString();

            // auto-tuple detection applies only when the type carries no contract family at all
            // (MetaType.GetContractFamily), and has to be tried before the shape checks below -
            // a tuple is commonly a closed generic, which they would otherwise reject
            if (!HasContractFamily(type))
            {
                if (ParseTuple(compilation, type, diagnostics, out reachable, cancellationToken) is { } tuple) return tuple;
                reachable = new List<INamedTypeSymbol>();
                return Contract(diagnostics, at, name,
                    "the type is not marked [ProtoContract], [DataContract] or [XmlType], and is not a tuple");
            }

            var isValueType = type.TypeKind == TypeKind.Struct;
            if (!isValueType && type.TypeKind != TypeKind.Class)
            {
                return Contract(diagnostics, at, name, "only classes and structs are supported");
            }
            if (type.IsAbstract) return Contract(diagnostics, at, name, "abstract types are not supported");
            if (type.IsGenericType) return Contract(diagnostics, at, name, "generic types are not supported");
            if (type.DeclaredAccessibility != Accessibility.Public)
            {
                // full ref-emit compilation only reaches public API, and we match that for now
                return Contract(diagnostics, at, name, "the type is not public");
            }

            // a struct is always constructible and can never have a base contract, so both of the
            // remaining checks are class-only
            if (!isValueType)
            {
                if (!type.InstanceConstructors.Any(static ctor
                    => ctor.Parameters.Length == 0 && ctor.DeclaredAccessibility == Accessibility.Public))
                {
                    return Contract(diagnostics, at, name, "there is no public parameterless constructor");
                }
                if (type.BaseType is { SpecialType: not SpecialType.System_Object })
                {
                    return Contract(diagnostics, at, name, "inheritance is not supported");
                }
            }

            bool isContract = false, isDataContract = false, isXmlType = false, skipConstructor = false;
            var dataMemberOffset = 0;
            foreach (var attribute in type.GetAttributes())
            {
                var attributeName = attribute.AttributeClass?.ToDisplayString();
                if (attributeName == ProtoContractAttributeName)
                {
                    if (attribute.ConstructorArguments.Length != 0)
                    {
                        return Option(diagnostics, at, name, "[ProtoContract] with constructor arguments");
                    }
                    foreach (var argument in attribute.NamedArguments)
                    {
                        // only options whose effect we reproduce are allowed through; everything
                        // else (ImplicitFields, Serializer, ...) still bails
                        switch (argument.Key)
                        {
                            case "SkipConstructor" when argument.Value.Value is bool skip:
                                skipConstructor = skip;
                                continue;
                            case "DataMemberOffset" when argument.Value.Value is int offset:
                                dataMemberOffset = offset;
                                continue;
                        }
                        return Option(diagnostics, at, name, $"[ProtoContract({argument.Key} = ...)]");
                    }
                    isContract = true;
                }
                // [DataContract] and [XmlType] are contract markers in their own right; their own
                // arguments (Name, Namespace) affect schema naming only
                else if (attributeName == DataContractAttributeName) isDataContract = true;
                else if (attributeName == XmlTypeAttributeName) isXmlType = true;
                else if (IsSignificantAttribute(attribute))
                {
                    return Option(diagnostics, at, name, $"[{AttributeName(attribute)}]");
                }
            }
            if (!isContract && !isDataContract && !isXmlType)
            {
                return Contract(diagnostics, at, name,
                    "the type is not marked [ProtoContract], [DataContract] or [XmlType]");
            }

            if (skipConstructor && isValueType)
            {
                // meaningless for a struct, and we have no ref-emit reference for the combination
                return Option(diagnostics, at, name, "[ProtoContract(SkipConstructor = true)] on a struct");
            }

            var members = new List<ProtoMemberPlan>();
            foreach (var symbol in type.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (symbol)
                {
                    case IFieldSymbol field when !field.IsImplicitlyDeclared:
                        if (field.GetAttributes().FirstOrDefault(IsSignificantAttribute) is { } onField)
                        {
                            return Option(diagnostics, PlanLocation.From(field), name,
                                $"[{AttributeName(onField)}] on fields");
                        }
                        break;

                    // serialization callbacks ([OnDeserialized] and friends) live on methods
                    case IMethodSymbol method when !method.IsImplicitlyDeclared && method.AssociatedSymbol is null:
                        if (method.GetAttributes().FirstOrDefault(IsSignificantAttribute) is { } onMethod)
                        {
                            return Option(diagnostics, PlanLocation.From(method), name,
                                $"[{AttributeName(onMethod)}] on methods");
                        }
                        break;
                }

                if (symbol is not IPropertySymbol property) continue;

                var atMember = PlanLocation.From(property);
                int? fieldNumber = null, dataMemberOrder = null, xmlOrder = null;
                var ignored = false;
                AttributeData? declaredDefault = null;
                foreach (var attribute in property.GetAttributes())
                {
                    var attributeName = attribute.AttributeClass?.ToDisplayString();
                    if (attributeName == DefaultValueAttributeName)
                    {
                        declaredDefault = attribute;
                    }
                    else if (attributeName == DataMemberAttributeName)
                    {
                        dataMemberOrder = GetNamedInt(attribute, "Order");
                    }
                    else if (attributeName is XmlElementAttributeName or XmlArrayAttributeName)
                    {
                        xmlOrder = GetNamedInt(attribute, "Order");
                    }
                    else if (attributeName is XmlIgnoreAttributeName or NonSerializedAttributeName)
                    {
                        ignored = true;
                    }
                    else if (attributeName == ProtoMemberAttributeName)
                    {
                        // DataFormat, IsRequired, AsReference, ... all change the wire format
                        if (attribute.NamedArguments.Length != 0)
                        {
                            return Option(diagnostics, atMember, name, "[ProtoMember] with named arguments");
                        }
                        if (attribute.ConstructorArguments.Length != 1
                            || attribute.ConstructorArguments[0].Value is not int number)
                        {
                            return Option(diagnostics, atMember, name, "this form of [ProtoMember]");
                        }
                        fieldNumber = number;
                    }
                    else if (IsSignificantAttribute(attribute))
                    {
                        return Option(diagnostics, atMember, name, $"[{AttributeName(attribute)}]");
                    }
                }
                if (ignored) continue;

                // precedence, per MetaType.ApplyDefaultBehaviour: [ProtoMember] first, then
                // [DataMember(Order)] - to which the offset applies - then [XmlElement]/[XmlArray],
                // to which it does not. An order below 1 means "not declared" (DataMember.Order
                // defaults to -1, and 0 is not a valid protobuf field number).
                fieldNumber ??= isDataContract && dataMemberOrder >= 1
                    ? dataMemberOrder + dataMemberOffset : null;
                fieldNumber ??= isXmlType && xmlOrder >= 1 ? xmlOrder : null;
                if (fieldNumber is null) continue;

                if (property.IsStatic) return Member(diagnostics, atMember, name, property.Name, "is static");
                if (property.DeclaredAccessibility != Accessibility.Public)
                {
                    return Member(diagnostics, atMember, name, property.Name, "is not public");
                }
                if (property.GetMethod is not { DeclaredAccessibility: Accessibility.Public })
                {
                    return Member(diagnostics, atMember, name, property.Name, "has no public getter");
                }
                if (property.SetMethod is not { DeclaredAccessibility: Accessibility.Public })
                {
                    return Member(diagnostics, atMember, name, property.Name, "has no public setter");
                }
                if (property.SetMethod.IsInitOnly)
                {
                    return Member(diagnostics, atMember, name, property.Name, "has an init-only setter");
                }

                if (GetConditionalPattern(type, property.Name) is { } conditional)
                {
                    return Member(diagnostics, atMember, name, property.Name,
                        $"is conditional via '{conditional}'");
                }

                if (GetMemberShape(compilation, property.Type) is not { } shape)
                {
                    return Member(diagnostics, atMember, name, property.Name,
                        $"has unsupported type '{property.Type.ToDisplayString()}'");
                }
                var kind = shape.Kind;
                var message = shape.Message;
                var isNullable = shape.IsNullable;
                var enumTypeName = shape.EnumType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                string? defaultLiteral = null;
                // a null declared default means "no declared default", exactly as ref-emit treats it
                if (declaredDefault is not null && !IsNullDefault(declaredDefault))
                {
                    if (kind == ProtoMemberKind.Message)
                    {
                        return Option(diagnostics, atMember, name, "[DefaultValue] on a message member");
                    }
                    defaultLiteral = GetDefaultLiteral(declaredDefault, kind, enumTypeName);
                    if (defaultLiteral is null)
                    {
                        return Option(diagnostics, atMember, name, "this form of [DefaultValue]");
                    }
                }

                if (kind == ProtoMemberKind.Message)
                {
                    // enqueued even if it turns out to be unsupported, so that it reports its own
                    // reason and this contract is dropped by cascade with a message that chains
                    reachable.Add(message!);
                    members.Add(new ProtoMemberPlan(fieldNumber.Value, property.Name, kind,
                        message!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        isNullable: isNullable, messageIsValueType: message.IsValueType));
                }
                else
                {
                    members.Add(new ProtoMemberPlan(fieldNumber.Value, property.Name, kind,
                        defaultLiteral: defaultLiteral, isNullable: isNullable,
                        enumTypeName: enumTypeName));
                }
            }

            if (members.Count == 0)
            {
                return Contract(diagnostics, at, name, "no [ProtoMember] properties were found");
            }
            members.Sort(static (x, y) => x.FieldNumber.CompareTo(y.FieldNumber));

            return new ProtoContractPlan(
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                new(members.ToArray()), isValueType, skipConstructor);
        }

        private static ProtoContractPlan? Contract(List<PlanDiagnostic> diagnostics, PlanLocation at, string type, string reason)
        {
            diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.UnsupportedContract, at, type, reason));
            return null;
        }

        private static ProtoContractPlan? Option(List<PlanDiagnostic> diagnostics, PlanLocation at, string type, string what)
        {
            diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.UnsupportedOption, at, type, what));
            return null;
        }

        private static ProtoContractPlan? Member(List<PlanDiagnostic> diagnostics, PlanLocation at, string type, string member, string reason)
        {
            diagnostics.Add(new PlanDiagnostic(ProtoDiagnosticKind.UnsupportedMember, at, type, member, reason));
            return null;
        }

        /// <summary>Strip the <c>global::</c> prefix, for use in messages.</summary>
        private static string Simplify(string? typeName)
            => typeName is null ? "?"
            : typeName.StartsWith("global::", StringComparison.Ordinal) ? typeName.Substring("global::".Length)
            : typeName;

        /// <summary>
        /// The <c>{Name}Specified</c> / <c>ShouldSerialize{Name}()</c> conventions, if present.
        /// </summary>
        /// <remarks>
        /// Inherited from the System.ComponentModel / XmlSerializer conventions, and matched by name
        /// rather than by attribute (see <c>MetaType.ApplyDefaultBehaviour</c>) - so no amount of
        /// attribute inspection would find them. They change both the write guard and the read path,
        /// so a member using one cannot be emitted from the member alone.
        /// </remarks>
        private static string? GetConditionalPattern(INamedTypeSymbol type, string memberName)
        {
            var specified = memberName + "Specified";
            var shouldSerialize = "ShouldSerialize" + memberName;

            foreach (var symbol in type.GetMembers())
            {
                switch (symbol)
                {
                    case IPropertySymbol property when property.Name == specified
                        && property.Type.SpecialType == SpecialType.System_Boolean:
                        return specified;

                    case IMethodSymbol method when method.Name == shouldSerialize
                        && !method.IsStatic && method.Parameters.Length == 0
                        && method.ReturnType.SpecialType == SpecialType.System_Boolean:
                        return shouldSerialize + "()";
                }
            }
            return null;
        }

        private static string AttributeName(AttributeData attribute)
        {
            var name = attribute.AttributeClass?.Name ?? "?";
            const string Suffix = "Attribute";
            return name.Length > Suffix.Length && name.EndsWith(Suffix, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - Suffix.Length) : name;
        }

        /// <summary>
        /// Does this attribute change how protobuf-net treats the thing it is applied to?
        /// </summary>
        /// <remarks>
        /// Not just the ProtoBuf namespace: <c>MetaType.ApplyDefaultBehaviour</c> also honours
        /// <c>[DataContract]</c>/<c>[DataMember]</c>, the <c>[OnDeserialized]</c> family of
        /// callbacks, the <c>System.Xml.Serialization</c> attributes, <c>[NonSerialized]</c> and
        /// <c>[DefaultValue]</c> - each of which can change the bytes we would emit.
        /// </remarks>
        private static int? GetNamedInt(AttributeData attribute, string name)
        {
            foreach (var argument in attribute.NamedArguments)
            {
                if (argument.Key == name && argument.Value.Value is int value) return value;
            }
            return null;
        }

        private static bool IsSignificantAttribute(AttributeData attribute)
        {
            var type = attribute.AttributeClass;
            if (type is null) return false;

            // these we understand and act on; anything else in the same namespaces still bails,
            // notably the [OnDeserialized] callback family
            switch (type.ToDisplayString())
            {
                case DataContractAttributeName:
                case DataMemberAttributeName:
                case XmlTypeAttributeName:
                case XmlElementAttributeName:
                case XmlArrayAttributeName:
                case XmlIgnoreAttributeName:
                case NonSerializedAttributeName:
                    return false;
            }

            switch (type.ContainingNamespace?.ToDisplayString())
            {
                case ProtoBufNamespace:
                case "System.Runtime.Serialization":
                case "System.Xml.Serialization":
                    return true;
            }

            return type.ToDisplayString() is "System.NonSerializedAttribute";
        }

        /// <summary>
        /// Render a <c>[DefaultValue]</c> argument as a C# literal of the member's own type, or null
        /// if we cannot do so faithfully.
        /// </summary>
        /// <remarks>
        /// Only the single-argument form is handled: <c>DefaultValue(Type, string)</c> defers to a
        /// TypeConverter at runtime, which we cannot evaluate here.
        /// </remarks>
        private static bool IsNullDefault(AttributeData attribute)
            => attribute.ConstructorArguments.Length == 1
                && attribute.ConstructorArguments[0].Value is null;

        private static string? GetDefaultLiteral(AttributeData attribute, ProtoMemberKind kind, string? enumTypeName)
        {
            if (attribute.ConstructorArguments.Length != 1) return null;
            if (attribute.ConstructorArguments[0].Value is not object raw) return null;

            // for an enum the constant is its underlying integral value, so render the underlying
            // literal and cast it back; parentheses matter for negatives
            if (enumTypeName is not null)
            {
                var underlying = GetDefaultLiteral(attribute, kind, enumTypeName: null);
                return underlying is null ? null : $"({enumTypeName})({underlying})";
            }

            try
            {
                var culture = CultureInfo.InvariantCulture;
                switch (kind)
                {
                    case ProtoMemberKind.Bool: return Convert.ToBoolean(raw, culture) ? "true" : "false";
                    case ProtoMemberKind.SByte: return Convert.ToSByte(raw, culture).ToString(culture);
                    case ProtoMemberKind.Byte: return Convert.ToByte(raw, culture).ToString(culture);
                    case ProtoMemberKind.Int16: return Convert.ToInt16(raw, culture).ToString(culture);
                    case ProtoMemberKind.UInt16: return Convert.ToUInt16(raw, culture).ToString(culture);
                    case ProtoMemberKind.Int32: return Convert.ToInt32(raw, culture).ToString(culture);
                    case ProtoMemberKind.UInt32: return Convert.ToUInt32(raw, culture).ToString(culture) + "U";
                    case ProtoMemberKind.Int64: return Convert.ToInt64(raw, culture).ToString(culture) + "L";
                    case ProtoMemberKind.UInt64: return Convert.ToUInt64(raw, culture).ToString(culture) + "UL";

                    case ProtoMemberKind.Single:
                        var single = Convert.ToSingle(raw, culture);
                        // NaN/infinity have no literal form, and NaN != NaN would break the guard
                        if (float.IsNaN(single) || float.IsInfinity(single)) return null;
                        return single.ToString("R", culture) + "F";

                    case ProtoMemberKind.Double:
                        var @double = Convert.ToDouble(raw, culture);
                        if (double.IsNaN(@double) || double.IsInfinity(@double)) return null;
                        return @double.ToString("R", culture) + "D";

                    case ProtoMemberKind.String:
                        return raw is string text ? SymbolDisplay.FormatLiteral(text, quote: true) : null;
                }
            }
            catch (Exception)
            {
                return null; // out of range, not convertible, ...
            }
            return null;
        }

        private readonly struct MemberShape
        {
            public MemberShape(ProtoMemberKind kind, bool isNullable = false,
                INamedTypeSymbol? message = null, INamedTypeSymbol? enumType = null)
            {
                Kind = kind;
                IsNullable = isNullable;
                Message = message;
                EnumType = enumType;
            }

            public ProtoMemberKind Kind { get; }
            public bool IsNullable { get; }
            public INamedTypeSymbol? Message { get; }
            public INamedTypeSymbol? EnumType { get; }
        }

        private static MemberShape? GetMemberShape(Compilation compilation, ITypeSymbol type)
        {
            var isNullable = false;
            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            {
                isNullable = true;
                type = nullable.TypeArguments[0];
            }

            if (type.TypeKind == TypeKind.Enum && type is INamedTypeSymbol { EnumUnderlyingType: { } underlying } enumType)
            {
                // an enum is its underlying scalar plus a cast; [Flags] makes no difference, and
                // [ProtoEnum] only renames for schema purposes - neither affects the wire form
                var kind = GetScalarKind(underlying);

                // the CLR permits char-backed enums even though C# cannot declare one; we have no way
                // to test that shape, so refuse it rather than emit something unverified
                if (kind is null or ProtoMemberKind.Char) return null;
                return new MemberShape(kind.Value, isNullable, enumType: enumType);
            }

            if (GetScalarKind(type) is { } scalar) return new MemberShape(scalar, isNullable);

            // erase tuple element names before anything looks at the type: they are decoration, and
            // leaving them on would key the same shape twice and emit a duplicate ISerializer<>
            type = EraseTupleNames(compilation, type);

            // a struct contract can be Nullable<T>; a reference-type one cannot
            if (GetMessageKind(type, out var message) is { } messageKind)
            {
                if (isNullable && message is not { IsValueType: true }) return null;
                return new MemberShape(messageKind, isNullable, message: message);
            }

            // a tuple-typed member is a sub-message too, even though it carries no contract attribute
            if (type is INamedTypeSymbol candidate && IsTupleCandidate(candidate))
            {
                if (isNullable && !candidate.IsValueType) return null;
                return new MemberShape(ProtoMemberKind.Message, isNullable, message: candidate);
            }

            if (isNullable) return null; // nothing else below here is a value type

            // byte[] is a bytes field, not a repeated byte; note the rank check, since byte[,] is not
            return type is IArrayTypeSymbol { Rank: 1, ElementType.SpecialType: SpecialType.System_Byte }
                ? new MemberShape(ProtoMemberKind.Bytes) : null;
        }

        private static ProtoMemberKind? GetMessageKind(ITypeSymbol type, out INamedTypeSymbol? message)
        {
            message = null;

            // anything marked [ProtoContract] counts as reachable, supported or not; whether it can
            // actually be handled is decided when the closure gets to it
            if (type is INamedTypeSymbol named && named.GetAttributes().Any(static a
                    => a.AttributeClass?.ToDisplayString() == ProtoContractAttributeName))
            {
                message = named;
                return ProtoMemberKind.Message;
            }
            return null;
        }

        private static ProtoMemberKind? GetScalarKind(ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean: return ProtoMemberKind.Bool;
                case SpecialType.System_SByte: return ProtoMemberKind.SByte;
                case SpecialType.System_Byte: return ProtoMemberKind.Byte;
                case SpecialType.System_Int16: return ProtoMemberKind.Int16;
                case SpecialType.System_UInt16: return ProtoMemberKind.UInt16;
                case SpecialType.System_Int32: return ProtoMemberKind.Int32;
                case SpecialType.System_UInt32: return ProtoMemberKind.UInt32;
                case SpecialType.System_Int64: return ProtoMemberKind.Int64;
                case SpecialType.System_UInt64: return ProtoMemberKind.UInt64;
                case SpecialType.System_Single: return ProtoMemberKind.Single;
                case SpecialType.System_Double: return ProtoMemberKind.Double;
                case SpecialType.System_Char: return ProtoMemberKind.Char;
                case SpecialType.System_String: return ProtoMemberKind.String;
            }
            return null;
        }
    }
}
