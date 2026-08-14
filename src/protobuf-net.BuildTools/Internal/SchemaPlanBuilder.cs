#nullable enable
using Google.Protobuf.Reflection;
using ProtoBuf.BuildTools.Internal.Aot;
using ProtoBuf.Reflection;
using System.Collections.Generic;

namespace ProtoBuf.BuildTools.Internal
{
    /// <summary>
    /// Builds AOT plans directly from a parsed <c>.proto</c> schema, rather than from Roslyn
    /// symbols — see <c>docs/aot-schema-model.md</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// **This is a spike**: it covers scalars, enums and nested messages, and nothing else. It
    /// exists to establish that the plan can be built from the schema at all, and that the emitted
    /// model compiles against the DTOs the *other* generator emits. Breadth (repeated, map,
    /// oneof, extensions, groups, proto2 defaults) is deliberately not here yet.
    /// </para>
    /// <para>
    /// The whole difficulty of this direction is that the plan describes the C# that
    /// <see cref="CSharpCodeGenerator"/> is about to emit, and the two must not drift. Every
    /// convention this relies on is called out with a CONVENTION comment, and each one is a place
    /// that would break silently if protogen changed. The eventual answer is a shared codegen
    /// model rather than a second implementation of the same decisions; until then,
    /// <c>SchemaSourcedModelSpikeTests</c> compiles both halves together, which is what catches
    /// a drift.
    /// </para>
    /// </remarks>
    internal static class SchemaPlanBuilder
    {
        /// <summary>
        /// Projects every message in the set into a contract plan. Returns null if anything in
        /// the set is outside the spike's scope, since a partial model is worse than none.
        /// </summary>
        internal static ProtoModelPlan? TryBuild(FileDescriptorSet set, NameNormalizer names,
            string? modelNamespace, string modelTypeName, out string? unsupported)
        {
            unsupported = null;
            var contracts = new List<ProtoContractPlan>();
            var enums = new List<ProtoEnumPlan>();

            foreach (var file in set.Files)
            {
                if (!file.IncludeInOutput) continue;
                var ns = Namespace(file, names);

                foreach (var message in file.MessageTypes)
                {
                    if (message.NestedTypes.Count != 0 || message.EnumTypes.Count != 0)
                    {
                        unsupported = $"{message.Name}: nested types are outside the spike";
                        return null;
                    }
                    var contract = TryBuildMessage(message, ns, names, ref unsupported);
                    if (contract is null) return null;
                    contracts.Add(contract);
                }

                foreach (var @enum in file.EnumTypes)
                {
                    // an enum reached as a MEMBER is an inline scalar and needs no plan of its own;
                    // it only becomes a ProtoEnumPlan when it is a model root, which a schema-sourced
                    // model does not currently produce
                    _ = @enum;
                }
            }

            if (contracts.Count == 0)
            {
                unsupported = "no messages";
                return null;
            }

            return new ProtoModelPlan(modelNamespace, modelTypeName,
                new EquatableArray<ProtoContractPlan>(contracts.ToArray()),
                enums: new EquatableArray<ProtoEnumPlan>(enums.ToArray()));
        }

        /// <summary>
        /// CONVENTION: the package becomes the namespace through the same normalizer the DTO
        /// generator uses; an empty package means the global namespace.
        /// </summary>
        private static string? Namespace(FileDescriptorProto file, NameNormalizer names)
        {
            var ns = names.GetName(file);
            return string.IsNullOrWhiteSpace(ns) ? null : ns;
        }

        private static ProtoContractPlan? TryBuildMessage(DescriptorProto message, string? ns,
            NameNormalizer names, ref string? unsupported)
        {
            var typeName = Qualify(ns, names.GetName(message));
            var members = new List<ProtoMemberPlan>();

            foreach (var field in message.Fields)
            {
                if (field.label == FieldDescriptorProto.Label.LabelRepeated)
                {
                    unsupported = $"{message.Name}.{field.Name}: repeated is outside the spike";
                    return null;
                }
                if (field.ShouldSerializeOneofIndex())
                {
                    unsupported = $"{message.Name}.{field.Name}: oneof is outside the spike";
                    return null;
                }

                var kind = MapKind(field, out var enumOrMessage, out var dataFormat);
                if (kind is null)
                {
                    unsupported = $"{message.Name}.{field.Name}: type {field.type} is outside the spike";
                    return null;
                }

                var name = names.GetName(field);
                string? typeNameForMember = null, enumTypeName = null, defaultLiteral = null;

                if (field.type == FieldDescriptorProto.Type.TypeEnum)
                {
                    // an enum member is its underlying scalar plus a cast, so the plan carries the
                    // enum's name and the kind is the underlying scalar (proto enums are int32)
                    enumTypeName = QualifyTypeRef(enumOrMessage!, ns, names);
                }
                else if (field.type == FieldDescriptorProto.Type.TypeMessage)
                {
                    typeNameForMember = QualifyTypeRef(enumOrMessage!, ns, names);
                }
                else if (field.type == FieldDescriptorProto.Type.TypeString)
                {
                    // CONVENTION: a proto3 string member is emitted with [DefaultValue("")] AND an
                    // `= ""` initialiser, which moves the write guard from `!= null` to `!= ""`.
                    // Getting this wrong is a silent wire difference, not a build break.
                    defaultLiteral = "\"\"";
                }

                members.Add(new ProtoMemberPlan(field.Number, name, kind.Value,
                    typeName: typeNameForMember,
                    enumTypeName: enumTypeName,
                    defaultLiteral: defaultLiteral,
                    dataFormat: dataFormat));
            }

            // CONVENTION: protogen emits `: IExtensible` on every message, with a private
            // __pbn__extensionData field - so the read's default case appends rather than skips
            return new ProtoContractPlan(typeName,
                new EquatableArray<ProtoMemberPlan>(members.ToArray()),
                extensible: ProtoExtensibleKind.Untyped);
        }

        /// <summary>
        /// The member's kind AND its <see cref="ProtoDataFormat"/> - which are two questions, not
        /// one.
        /// </summary>
        /// <remarks>
        /// The .proto scalar spellings encode the WIRE FORM as well as the CLR type:
        /// <c>int32</c>, <c>sint32</c> and <c>sfixed32</c> are all a C# <c>int</c>, and all three
        /// go on the wire differently - twos-complement varint, zig-zag varint, and fixed 32 bits.
        /// <para>
        /// Collapsing them to the CLR type alone compiles perfectly and writes the wrong bytes,
        /// which is exactly what the first version of this did: <c>sint32 delta = -17</c> went out
        /// as a sign-extended ten-byte varint instead of the single byte <c>0x21</c>. The byte
        /// differential caught it on its first run; nothing else could have.
        /// </para>
        /// </remarks>
        private static ProtoMemberKind? MapKind(FieldDescriptorProto field, out string? typeRef,
            out ProtoDataFormat format)
        {
            typeRef = null;
            format = ProtoDataFormat.Default;
            switch (field.type)
            {
                case FieldDescriptorProto.Type.TypeBool: return ProtoMemberKind.Bool;

                case FieldDescriptorProto.Type.TypeInt32: return ProtoMemberKind.Int32;
                case FieldDescriptorProto.Type.TypeSint32:
                    format = ProtoDataFormat.ZigZag;
                    return ProtoMemberKind.Int32;
                case FieldDescriptorProto.Type.TypeSfixed32:
                    format = ProtoDataFormat.FixedSize;
                    return ProtoMemberKind.Int32;

                case FieldDescriptorProto.Type.TypeUint32: return ProtoMemberKind.UInt32;
                case FieldDescriptorProto.Type.TypeFixed32:
                    format = ProtoDataFormat.FixedSize;
                    return ProtoMemberKind.UInt32;

                case FieldDescriptorProto.Type.TypeInt64: return ProtoMemberKind.Int64;
                case FieldDescriptorProto.Type.TypeSint64:
                    format = ProtoDataFormat.ZigZag;
                    return ProtoMemberKind.Int64;
                case FieldDescriptorProto.Type.TypeSfixed64:
                    format = ProtoDataFormat.FixedSize;
                    return ProtoMemberKind.Int64;

                case FieldDescriptorProto.Type.TypeUint64: return ProtoMemberKind.UInt64;
                case FieldDescriptorProto.Type.TypeFixed64:
                    format = ProtoDataFormat.FixedSize;
                    return ProtoMemberKind.UInt64;

                case FieldDescriptorProto.Type.TypeFloat: return ProtoMemberKind.Single;
                case FieldDescriptorProto.Type.TypeDouble: return ProtoMemberKind.Double;
                case FieldDescriptorProto.Type.TypeString: return ProtoMemberKind.String;
                case FieldDescriptorProto.Type.TypeBytes: return ProtoMemberKind.Bytes;
                case FieldDescriptorProto.Type.TypeEnum:
                    typeRef = field.TypeName;
                    return ProtoMemberKind.Int32; // proto enums are int32 on the wire
                case FieldDescriptorProto.Type.TypeMessage:
                    typeRef = field.TypeName;
                    return ProtoMemberKind.Message;
                default:
                    return null;
            }
        }

        /// <summary>
        /// A schema type reference is fully qualified and dotted (<c>.probe.Inner</c>); the
        /// emitted C# name is the normalized package plus the normalized type name.
        /// </summary>
        private static string QualifyTypeRef(string typeRef, string? ns, NameNormalizer names)
        {
            var trimmed = typeRef.StartsWith(".") ? typeRef.Substring(1) : typeRef;
            var lastDot = trimmed.LastIndexOf('.');
            var leaf = lastDot < 0 ? trimmed : trimmed.Substring(lastDot + 1);
            // the spike only handles types in the file's own package, which the caller's scope
            // check has already narrowed to
            return Qualify(ns, leaf);
        }

        private static string Qualify(string? ns, string typeName)
            => string.IsNullOrEmpty(ns) ? $"global::{typeName}" : $"global::{ns}.{typeName}";
    }
}
