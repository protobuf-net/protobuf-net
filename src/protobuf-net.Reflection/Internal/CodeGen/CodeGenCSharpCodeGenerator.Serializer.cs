using ProtoBuf.Internal.CodeGen;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal partial class CodeGenCSharpCodeGenerator
{
    static bool CanPack(CodeGenField field)
    {
        if (field.Type.IsWellKnownType(out var known))
        {
            switch (known)
            {
                case CodeGenWellKnownType.Boolean:
                case CodeGenWellKnownType.Byte:
                case CodeGenWellKnownType.Double:
                case CodeGenWellKnownType.Fixed32:
                case CodeGenWellKnownType.Fixed64:
                case CodeGenWellKnownType.Float:
                case CodeGenWellKnownType.Int32:
                case CodeGenWellKnownType.Int64:
                case CodeGenWellKnownType.SFixed32:
                case CodeGenWellKnownType.SFixed64:
                case CodeGenWellKnownType.SInt32:
                case CodeGenWellKnownType.SInt64:
                case CodeGenWellKnownType.UInt32:
                case CodeGenWellKnownType.UInt64:
                    return true;
            }
        }
        return false;
    }

    protected override void WriteMessageSerializer(CodeGenGeneratorContext ctx, CodeGenMessage message, ref object state)
    {

        // very much work-in-progress; not much of this is expected to work at all!
        // not yet implemented:
        // repeated
        // extension data
        // constructor usage
        // inheritance
        // down-level langver
        // consideration of value-swap for sub-messages

        const string NanoNS = "global::ProtoBuf.Nano";

        static ulong GetTag(CodeGenField field, WireType wireType)
            => ((ulong)field.FieldNumber << 3) | (ulong)wireType;

        static long HeaderLength(CodeGenField field)
        {
            var value = GetTag(field, WireType.Varint); // the actual wire type doesn't matter here
            if ((value & (~0UL << 7)) == 0) return 1;
            if ((value & (~0UL << 14)) == 0) return 2;
            if ((value & (~0UL << 21)) == 0) return 3;
            if ((value & (~0UL << 28)) == 0) return 4;
            if ((value & (~0UL << 35)) == 0) return 5;
            if ((value & (~0UL << 42)) == 0) return 6;
            if ((value & (~0UL << 49)) == 0) return 7;
            if ((value & (~0UL << 56)) == 0) return 8;
            if ((value & (~0UL << 63)) == 0) return 9;
            return 10;
        }

        static string GetSource(CodeGenField field, bool repeated = false)
        {
            if (field.Conditional == ConditionalKind.NullableT) return $"value.{field.BackingName}.GetValueOrDefault()";
            return $"value.{field.BackingName}";
        }
        static string GetIteratorName(CodeGenField field) => "__pbn_item";

        static bool IsConditional(CodeGenField field, CodeGenGeneratorContext ctx, CodeGenCSharpCodeGenerator @this)
        {
            bool isConditional = field.Conditional != ConditionalKind.Always;
            if (isConditional)
            {
                switch (field.Conditional)
                {
                    case ConditionalKind.NonDefault when field.Type is CodeGenSimpleType simple && simple.WellKnownType == CodeGenWellKnownType.Boolean
                        && TryParseBoolean(field.DefaultValue, out var bDefault):
                        ctx.WriteLine($"if ({(bDefault ? "!" : "")}value.{field.BackingName})");
                        break;
                    case ConditionalKind.NonDefault:
                        ctx.WriteLine($"if (value.{field.BackingName} != {@this.FormatDefaultValue(field)})");
                        break;
                    case ConditionalKind.ShouldSerializeMethod:
                        ctx.WriteLine($"if (value.ShouldSerialize{field.Name}())");
                        break;
                    case ConditionalKind.FieldPresence when field.TryGetPresence(out var fieldIndex, out var mask):
                        ctx.WriteLine($"if ((value.{CodeGenField.PresenceTrackingFieldName}{fieldIndex} & {mask}) != 0)");
                        break;
                    case ConditionalKind.NullableT:
                        ctx.WriteLine($"if (value.{field.BackingName}.HasValue)");
                        break;
                    default:
                        ctx.WriteLine($"#warning conditional mode for {field.Name} not supported: {field.Conditional}");
                        isConditional = false;
                        break;
                }
            }
            return isConditional;
        }

        ctx.WriteLine($"internal static void Write({Escape(message.Name)} value, ref {NanoNS}.Writer writer)").WriteLine("{").Indent();
        foreach (var field in message.Fields)
        {
            if (field.Repeated == RepeatedKind.Dictionary)
            {
                ctx.WriteLine($"#warning maps not implemented yet");
                continue;
            }

            field.Type.IsWellKnownType(out var knownType);

            bool isConditional = IsConditional(field, ctx, this);
            if (isConditional) // assume we've just written an "if"
            {
                ctx.WriteLine("{").Indent();
            }

            var source = GetSource(field);
            if (field.IsRepeated)
            {
                WriteForeach(field, ctx, ref source);
            }
            GetEnumSource(field, ctx, ref knownType, ref source);

            switch (knownType)
            {
                case CodeGenWellKnownType.None when field.IsGroup && (field.IsRepeated || IsValueType(field)): // no null-test
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.StartGroup)}); // field {field.FieldNumber}, group")
                       .WriteLine($"{Type(field.Type)}.Write({source}, ref writer);")
                       .WriteLine($"writer.WriteVarint({GetTag(field, WireType.EndGroup)}); // field {field.FieldNumber}, end-group");
                    break;
                case CodeGenWellKnownType.None when field.IsGroup:
                    ctx.WriteLine($"if ({source} is {Type(field.Type)} obj{field.FieldNumber})").WriteLine("{").Indent()
                        .WriteLine($"writer.WriteVarint({GetTag(field, WireType.StartGroup)}); // field {field.FieldNumber}, group")
                        .WriteLine($"{Type(field.Type)}.Write(obj{field.FieldNumber}, ref writer);")
                        .WriteLine($"writer.WriteVarint({GetTag(field, WireType.EndGroup)}); // field {field.FieldNumber}, end-group")
                        .Outdent().WriteLine("}");
                    break;
                case CodeGenWellKnownType.None when (field.IsRepeated || IsValueType(field)): // no null-test
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.String)}); // field {field.FieldNumber}, string")
                       .WriteLine($"writer.WriteVarint({Type(field.Type)}.Measure({source}));")
                       .WriteLine($"{Type(field.Type)}.Write({source}, ref writer);");
                    break;
                case CodeGenWellKnownType.None:
                    ctx.WriteLine($"if ({source} is {Type(field.Type)} obj{field.FieldNumber})").WriteLine("{").Indent()
                        .WriteLine($"writer.WriteVarint({GetTag(field, WireType.String)}); // field {field.FieldNumber}, string")
                        .WriteLine($"writer.WriteVarint({Type(field.Type)}.Measure(obj{field.FieldNumber}));")
                        .WriteLine($"{Type(field.Type)}.Write(obj{field.FieldNumber}, ref writer);")
                        .Outdent().WriteLine("}");
                    break;
                case CodeGenWellKnownType.String when field.IsRepeated: // no null/empty test
                case CodeGenWellKnownType.Bytes when field.IsRepeated: // no null/empty test
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.String)}); // field {field.FieldNumber}, string")
                       .WriteLine($"writer.WriteWithLengthPrefix({source});");
                    break;
                case CodeGenWellKnownType.String:
                case CodeGenWellKnownType.Bytes:
                    ctx.WriteLine($"if ({source} is {{ Length: > 0 }} obj{field.FieldNumber})").WriteLine("{").Indent()
                        .WriteLine($"writer.WriteVarint({GetTag(field, WireType.String)}); // field {field.FieldNumber}, string")
                        .WriteLine($"writer.WriteWithLengthPrefix(obj{field.FieldNumber});")
                        .Outdent().WriteLine("}");
                    break;
                case CodeGenWellKnownType.UInt32:
                case CodeGenWellKnownType.UInt64:
                case CodeGenWellKnownType.Int32:
                case CodeGenWellKnownType.Int64:
                case CodeGenWellKnownType.Boolean:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Varint)}); // field {field.FieldNumber}, varint")
                        .WriteLine($"writer.WriteVarint({source});");
                    break;
                case CodeGenWellKnownType.SInt32:
                case CodeGenWellKnownType.SInt64:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Varint)}); // field {field.FieldNumber}, varint")
                        .WriteLine($"writer.WriteVarintSigned({source});");
                    break;
                case CodeGenWellKnownType.Float:
                case CodeGenWellKnownType.Fixed32:
                case CodeGenWellKnownType.SFixed32:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Fixed32)}); // field {field.FieldNumber}, fixed32")
                        .WriteLine($"writer.WriteFixed32({source});");
                    break;
                case CodeGenWellKnownType.Double:
                case CodeGenWellKnownType.Fixed64:
                case CodeGenWellKnownType.SFixed64:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Fixed64)}); // field {field.FieldNumber}, fixed64")
                        .WriteLine($"writer.WriteFixed64({source});");
                    break;
                case CodeGenWellKnownType.NetObjectProxy:
                    ctx.WriteLine($"if ({source} is not null)").WriteLine("{").Indent()
                        .WriteLine(@"throw new global::System.NotSupportedException(""dynamic types/reference-tracking is not supported"");").Outdent().WriteLine("}");
                    break;
                default:
                    throw new NotImplementedException($"type not yet supported in {nameof(WriteMessageSerializer)}: {knownType}");
            }
            if (field.IsRepeated) // we need to close the "foreach"
            {
                ctx.Outdent().WriteLine("}");
            }
            if (isConditional) // we heed to close the "if"
            {
                ctx.Outdent().WriteLine("}");
            }
        }
        ctx.Outdent().WriteLine("}").WriteLine();

        long fixedLength = 0;
        ctx.WriteLine($"internal static ulong Measure({Escape(message.Name)} value)").WriteLine("{").Indent().WriteLine($"ulong len = 0;");

        foreach (var field in message.Fields)
        {
            if (field.Repeated == RepeatedKind.Dictionary)
            {
                ctx.WriteLine($"#warning maps not implemented yet");
                continue;
            }

            field.Type.IsWellKnownType(out var knownType);
            bool isConditional = IsConditional(field, ctx, this);
            if (isConditional) // assume we've just written an "if"
            {
                ctx.WriteLine("{").Indent();
            }
            TextWriter tw = null;
            bool addHeaderLength = false;

            var source = GetSource(field);

            if (field.IsRepeated)
            {
                WriteForeach(field, ctx, ref source);
            }
            GetEnumSource(field, ctx, ref knownType, ref source);

            long itemFixedLength = 0;
            switch (knownType)
            {
                case CodeGenWellKnownType.None when field.IsGroup && (field.IsRepeated || IsValueType(field)): // no null-test
                    ctx.WriteLine($"len += {2 * HeaderLength(field)} + {Type(field.Type)}.Measure({source});");
                    break;
                case CodeGenWellKnownType.None when (field.IsRepeated || IsValueType(field)): // no null-test
                    ctx.WriteLine($"len += {HeaderLength(field)} + {NanoNS}.Writer.MeasureWithLengthPrefix({Type(field.Type)}.Measure({source}));");
                    break;
                case CodeGenWellKnownType.None when field.IsGroup:
                    ctx.WriteLine($"if ({source} is {Type(field.Type)} obj{field.FieldNumber})").WriteLine("{").Indent()
                        .WriteLine($"len += {2 * HeaderLength(field)} + {Type(field.Type)}.Measure(obj{field.FieldNumber});")
                        .Outdent().WriteLine("}");
                    break;
                case CodeGenWellKnownType.None:
                    ctx.WriteLine($"if ({source} is {Type(field.Type)} obj{field.FieldNumber})").WriteLine("{").Indent()
                        .WriteLine($"len += {HeaderLength(field)} + {NanoNS}.Writer.MeasureWithLengthPrefix({Type(field.Type)}.Measure(obj{field.FieldNumber}));")
                        .Outdent().WriteLine("}");
                    break;
                case CodeGenWellKnownType.String when field.IsRepeated:
                case CodeGenWellKnownType.Bytes when field.IsRepeated:
                    ctx.WriteLine($"len += {HeaderLength(field)} + {NanoNS}.Writer.MeasureWithLengthPrefix({source});");
                    break;
                case CodeGenWellKnownType.String: // no null/empty test
                case CodeGenWellKnownType.Bytes: // no null/empty test
                    ctx.WriteLine($"if ({source} is {{ Length: > 0 }} obj{field.FieldNumber})").WriteLine("{").Indent()
                        .WriteLine($"len += {HeaderLength(field)} + {NanoNS}.Writer.MeasureWithLengthPrefix(obj{field.FieldNumber});")
                        .Outdent().WriteLine("}");
                    break;
                case CodeGenWellKnownType.UInt32:
                case CodeGenWellKnownType.UInt64:
                case CodeGenWellKnownType.Int32:
                case CodeGenWellKnownType.Int64:
                    tw = ctx.Write($"len += {NanoNS}.Writer.MeasureVarint({source})");
                    addHeaderLength = true;
                    break;
                case CodeGenWellKnownType.SInt32:
                case CodeGenWellKnownType.SInt64:
                    tw = ctx.Write($"len += {NanoNS}.Writer.MeasureVarintSigned({source})");
                    addHeaderLength = true;
                    break;
                case CodeGenWellKnownType.Boolean when isConditional:
                    ctx.WriteLine($"len += {HeaderLength(field) + 1};");
                    break;
                case CodeGenWellKnownType.Boolean:
                    itemFixedLength += HeaderLength(field) + 1;
                    break;
                case CodeGenWellKnownType.Float when isConditional:
                case CodeGenWellKnownType.Fixed32 when isConditional:
                case CodeGenWellKnownType.SFixed32 when isConditional:
                    ctx.WriteLine($"len += {HeaderLength(field) + 4};");
                    break;
                case CodeGenWellKnownType.Float:
                case CodeGenWellKnownType.Fixed32:
                case CodeGenWellKnownType.SFixed32:
                    itemFixedLength += HeaderLength(field) + 4;
                    break;
                case CodeGenWellKnownType.Double when isConditional:
                case CodeGenWellKnownType.Fixed64 when isConditional:
                case CodeGenWellKnownType.SFixed64 when isConditional:
                    ctx.WriteLine($"len += {HeaderLength(field) + 8};");
                    break;
                case CodeGenWellKnownType.Double:
                case CodeGenWellKnownType.Fixed64:
                case CodeGenWellKnownType.SFixed64:
                    itemFixedLength += HeaderLength(field) + 8;
                    break;
                case CodeGenWellKnownType.NetObjectProxy when field.IsRepeated: // no null-test
                    ctx.WriteLine(@"throw new global::System.NotSupportedException(""dynamic types/reference-tracking is not supported"");");
                    break;
                case CodeGenWellKnownType.NetObjectProxy:
                    ctx.WriteLine($"if ({source} is not null)").WriteLine("{").Indent()
                        .WriteLine(@"throw new global::System.NotSupportedException(""dynamic types/reference-tracking is not supported"");").Outdent().WriteLine("}");
                    break;
                default:
                    throw new NotImplementedException($"type not yet supported in {nameof(WriteMessageSerializer)}: {knownType}");
            }
            if (addHeaderLength)
            {
                if (isConditional || field.IsRepeated)
                {
                    tw.Write($" + {HeaderLength(field)}");
                }
                else
                {
                    fixedLength += HeaderLength(field);
                }
            }
            else
            {
                if (isConditional || field.IsRepeated)
                {
                    ctx.WriteLine($"len += {itemFixedLength};");
                }
                else
                {
                    fixedLength += itemFixedLength;
                }
            }
            tw?.WriteLine(";");
            if (field.IsRepeated) // we need to close the "foreach"
            {
                ctx.Outdent().WriteLine("}");
            }
            if (isConditional) // we heed to close the "if"
            {
                ctx.Outdent().WriteLine("}");
            }
        }
        if (fixedLength == 0)
        {
            ctx.WriteLine("return len;");
        }
        else
        {
            ctx.WriteLine($"return len + {fixedLength};");
        }
        ctx.Outdent().WriteLine("}").WriteLine();

        ctx.WriteLine($"internal static {Escape(message.Name)} Merge({Escape(message.Name)} value, ref {NanoNS}.Reader reader)").WriteLine("{").Indent();
        bool needOldEnd = false, needPacked = false;

        foreach (var field in message.Fields)
        {
            if (field.Type is CodeGenMessage or CodeGenMapEntryType) needOldEnd = true;
            else if (field.IsRepeated && CanPack(field) && (field.IsPacked || !ctx.Strict)) needPacked = true;
        }
        if (needOldEnd && needPacked)
        {
            ctx.WriteLine("ulong oldEnd, packed;");
        }
        else
        {
            if (needOldEnd) ctx.WriteLine("ulong oldEnd;");
            if (needPacked) ctx.WriteLine("ulong packed;");
        }

        if (!message.IsValueType)
        {
            ctx.WriteLine($"if (value is null) value = new();");
        }
        ctx.WriteLine("uint tag;").WriteLine("while ((tag = reader.ReadTag()) != 0)").WriteLine("{").Indent().WriteLine("switch (tag)").WriteLine("{").Indent();

        foreach (var field in message.Fields)
        {
            if (field.Repeated == RepeatedKind.Dictionary)
            {
                ctx.WriteLine($"#warning maps not implemented yet");
                continue;
            }

            field.Type.IsWellKnownType(out var knownType);
            static string Format(WireType wireType) => wireType switch
            {
                WireType.String => "string",
                WireType.Varint => "varint",
                WireType.Fixed32 => "fixed32",
                WireType.Fixed64 => "fixed64",
                WireType.StartGroup => "group",
                _ => wireType.ToString(),
            };
            static void WriteCase(CodeGenField field, WireType wireType, CodeGenGeneratorContext ctx, string value, CodeGenCSharpCodeGenerator @this, bool allowPacked = false)
            {
                var tag = GetTag(field, wireType);
                @this.GetEnumTarget(field, ctx, ref value);
                ctx.WriteLine($"case {tag}: // field {field.FieldNumber}, {Format(wireType)}").Indent();
                if (field.IsRepeated)
                {
                    ctx.WriteLine("do").WriteLine("{").Indent()
                       .WriteLine($"value.{field.BackingName}.Add({value});")
                       .Outdent().WriteLine("}").WriteLine($"while (reader.TryReadTag({tag}));");
                }
                else
                {
                    ctx.WriteLine($"value.{field.BackingName} = {value};");
                }
                ctx.WriteLine("break;").Outdent();

                if (allowPacked && field.IsRepeated && (field.IsPacked || !ctx.Strict))
                {
                    Debug.Assert(CanPack(field), "CanPack reported no");
                    tag = GetTag(field, WireType.String);
                    ctx.WriteLine($"case {tag}: // field {field.FieldNumber}, {Format(WireType.String)} (packed)").Indent();
                    switch (wireType)
                    {
                        case WireType.Varint:
                            ctx.WriteLine("packed = reader.ReadVarintUInt64() + reader.Position;")
                               .WriteLine("while (reader.Position < packed)").WriteLine("{").Indent()
                               .WriteLine($"value.{field.BackingName}.Add({value});")
                               .Outdent().WriteLine("}");
                            break;
                        case WireType.Fixed32:
                        case WireType.Fixed64:
                            ctx.WriteLine("packed = reader.ReadVarintUInt64();")
                                .WriteLine($"if ((packed & {(wireType == WireType.Fixed32 ? 3 : 7)}) != 0) reader.ThrowInvalidPackedLength(tag, packed);")
                                .WriteLine($"packed >>= {(wireType == WireType.Fixed32 ? 2 : 3)};")
                                .WriteLine("while (packed-- != 0)").WriteLine("{").Indent()
                                .WriteLine($"value.{field.BackingName}.Add({value});")
                                .Outdent().WriteLine("}");
                            break;
                        default:
                            ctx.WriteLine($"#error packed implementation missing for {field.Name}, wire-type {wireType}");
                            break;
                    }
                    ctx.WriteLine("break;").Outdent();
                }
            }

            static void WriteMessageCase(CodeGenField field, WireType wireType, CodeGenGeneratorContext ctx, string type)
            {
                var tag = GetTag(field, wireType);
                ctx.WriteLine($"case {tag}: // field {field.FieldNumber}, {Format(wireType)}").Indent();
                if (field.IsRepeated)
                {
                    ctx.WriteLine("do").WriteLine("{").Indent();
                }

                if (wireType == WireType.String)
                {
                    ctx.WriteLine($"oldEnd = reader.ConstrainByLengthPrefix();");
                }

                if (field.IsRepeated)
                {
                    ctx.WriteLine($"value.{field.BackingName}.Add({type}.Merge(default, ref reader));");
                }
                else
                {
                    ctx.WriteLine($"value.{field.BackingName} = {type}.Merge(value.{field.BackingName}, ref reader);");
                }

                if (wireType == WireType.String)
                {
                    ctx.WriteLine("reader.Unconstrain(oldEnd);");
                }
                else
                {
                    ctx.WriteLine($"reader.PopGroup({field.FieldNumber});");
                }


                if (field.IsRepeated)
                {
                    ctx.Outdent().WriteLine("}").WriteLine($"while (reader.TryReadTag({tag}));");
                }
                ctx.WriteLine("break;").Outdent();
            }

            string source = "";
            GetEnumSource(field, ctx, ref knownType, ref source);
            switch (knownType)
            {
                case CodeGenWellKnownType.String:
                    WriteCase(field, WireType.String, ctx, "reader.ReadString()", this);
                    break;
                case CodeGenWellKnownType.Bytes:
                    WriteCase(field, WireType.String, ctx, "reader.ReadBytes()", this);
                    break;
                case CodeGenWellKnownType.None when field.IsGroup:
                    WriteMessageCase(field, WireType.StartGroup, ctx, Type(field.Type));
                    if (!ctx.Strict) WriteMessageCase(field, WireType.String, ctx, Type(field.Type));
                    break;
                case CodeGenWellKnownType.None:
                    WriteMessageCase(field, WireType.String, ctx, Type(field.Type));
                    if (!ctx.Strict) WriteMessageCase(field, WireType.StartGroup, ctx, Type(field.Type));
                    break;
                case CodeGenWellKnownType.UInt32:
                    WriteCase(field, WireType.Varint, ctx, "reader.ReadVarintUInt32()", this, true);
                    if (!ctx.Strict)
                    {
                        WriteCase(field, WireType.Fixed32, ctx, "reader.ReadFixed32UInt32()", this);
                        WriteCase(field, WireType.Fixed64, ctx, "reader.ReadFixed64UInt32()", this);
                    }
                    break;
                case CodeGenWellKnownType.UInt64:
                    WriteCase(field, WireType.Varint, ctx, "reader.ReadVarintUInt64()", this, true);
                    if (!ctx.Strict)
                    {
                        WriteCase(field, WireType.Fixed32, ctx, "reader.ReadFixed32UInt64()", this);
                        WriteCase(field, WireType.Fixed64, ctx, "reader.ReadFixed64UInt64()", this);
                    }
                    break;
                case CodeGenWellKnownType.Int32:
                    WriteCase(field, WireType.Varint, ctx, "reader.ReadVarintInt32()", this, true);
                    if (!ctx.Strict)
                    {
                        WriteCase(field, WireType.Fixed32, ctx, "reader.ReadFixed32Int32()", this);
                        WriteCase(field, WireType.Fixed64, ctx, "reader.ReadFixed64Int32()", this);
                    }
                    break;
                case CodeGenWellKnownType.Int64:
                    WriteCase(field, WireType.Varint, ctx, "reader.ReadVarintInt64()", this, true);
                    if (!ctx.Strict)
                    {
                        WriteCase(field, WireType.Fixed32, ctx, "reader.ReadFixed32Int64()", this);
                        WriteCase(field, WireType.Fixed64, ctx, "reader.ReadFixed64Int64()", this);
                    }
                    break;
                case CodeGenWellKnownType.SInt32:
                    WriteCase(field, WireType.Varint, ctx, "reader.ReadVarintInt32Signed()", this, true);
                    break;
                case CodeGenWellKnownType.SInt64:
                    WriteCase(field, WireType.Varint, ctx, "reader.ReadVarintInt64Signed()", this, true);
                    break;
                case CodeGenWellKnownType.Float:
                    WriteCase(field, WireType.Fixed32, ctx, "reader.ReadFixed32Single()", this, true);
                    if (!ctx.Strict) WriteCase(field, WireType.Fixed64, ctx, "reader.ReadFixed64Single()", this);
                    break;
                case CodeGenWellKnownType.Double:
                    WriteCase(field, WireType.Fixed64, ctx, "reader.ReadFixed64Double()", this, true);
                    if (!ctx.Strict) WriteCase(field, WireType.Fixed32, ctx, "reader.ReadFixed64Double()", this);
                    break;
                case CodeGenWellKnownType.Boolean:
                    WriteCase(field, WireType.Varint, ctx, "reader.ReadVarintBoolean()", this, true);
                    if (!ctx.Strict)
                    {
                        WriteCase(field, WireType.Fixed32, ctx, "reader.ReadFixed32Boolean()", this);
                        WriteCase(field, WireType.Fixed64, ctx, "reader.ReadFixed64Boolean()", this);
                    }
                    break;
                case CodeGenWellKnownType.Fixed32:
                    WriteCase(field, WireType.Fixed32, ctx, "reader.ReadFixed32UInt32()", this, true);
                    if (!ctx.Strict)
                    {
                        WriteCase(field, WireType.Fixed64, ctx, "reader.ReadFixed64UInt32()", this);
                    }
                    break;
                case CodeGenWellKnownType.Fixed64:
                    WriteCase(field, WireType.Fixed64, ctx, "reader.ReadFixed64UInt64()", this, true);
                    if (!ctx.Strict)
                    {
                        WriteCase(field, WireType.Fixed32, ctx, "reader.ReadFixed32UInt64()", this);
                    }
                    break;
                case CodeGenWellKnownType.SFixed32:
                    WriteCase(field, WireType.Fixed32, ctx, "reader.ReadFixed32Int32()", this, true);
                    if (!ctx.Strict)
                    {
                        WriteCase(field, WireType.Fixed64, ctx, "reader.ReadFixed64Int32()", this);
                    }
                    break;
                case CodeGenWellKnownType.SFixed64:
                    WriteCase(field, WireType.Fixed64, ctx, "reader.ReadFixed64Int64()", this, true);
                    if (!ctx.Strict)
                    {
                        WriteCase(field, WireType.Fixed32, ctx, "reader.ReadFixed32Int64()", this);
                    }
                    break;
                case CodeGenWellKnownType.NetObjectProxy:
                    // we always write both string and group decoder, because protobuf-net has always been forgiving
                    if (!field.IsGroup || !ctx.Strict) ctx.WriteLine($"case {GetTag(field, WireType.String)}: // field {field.FieldNumber}, string");
                    if (field.IsGroup || !ctx.Strict) ctx.WriteLine($"case {GetTag(field, WireType.StartGroup)}: // field {field.FieldNumber}, group");
                    ctx.Indent().WriteLine(@"reader.ThrowNetObjectProxy();").WriteLine("break;").Outdent();
                    break;
                default:
                    throw new NotImplementedException($"type not yet supported in {nameof(WriteMessageSerializer)}: {knownType}");
            }
        }
        ctx.WriteLine("default:").Indent()
            .WriteLine($"if ((tag & 7) == {(int)WireType.EndGroup}) // end-group").WriteLine("{").Indent()
            .WriteLine("reader.PushGroup(tag);")
            .WriteLine("goto ExitLoop;").Outdent().WriteLine("}");

        if (message.ShouldSerializeFields())
        {
            ctx.WriteLine("switch (tag >> 3)").WriteLine("{").Indent();
            foreach (var field in message.Fields)
            {
                ctx.WriteLine($"case {field.FieldNumber}:");
            }
            ctx.Indent().WriteLine($"reader.ThrowUnhandledWireType(tag);").WriteLine("break;").Outdent().Outdent().WriteLine("}");
        }
        ctx.WriteLine("reader.Skip(tag);").WriteLine("break;").Outdent();
        ctx.Outdent().WriteLine("}").Outdent().WriteLine("}");

        ctx.Outdent().WriteLine("ExitLoop:").Indent()
            .WriteLine("return value;").Outdent().WriteLine("}").WriteLine();

        static bool IsValueType(CodeGenField field) => field.Type is CodeGenMessage msg && msg.IsValueType;
        static void WriteForeach(CodeGenField field, CodeGenGeneratorContext ctx, ref string source)
        {
            var iter = GetIteratorName(field);
            switch (field.Repeated)
            {
                case RepeatedKind.List when ctx.Supports(CSharp7_3) && field.Type is CodeGenMessage msg && msg.IsValueType:
                    ctx.Outdent().WriteLine("#if NET5_0_OR_GREATER").Indent()
                        .WriteLine($"foreach (ref readonly var {iter} in System.Runtime.InteropServices.CollectionsMarshal.AsSpan({source}))")
                        .Outdent().WriteLine("#else").Indent()
                        .WriteLine($"foreach (var {iter} in {source})")
                        .Outdent().WriteLine("#endif").Indent()
                        .WriteLine("{").Indent();
                    break;
                case RepeatedKind.List:
                    ctx.Outdent().WriteLine("#if NET5_0_OR_GREATER").Indent()
                        .WriteLine($"foreach (var {iter} in System.Runtime.InteropServices.CollectionsMarshal.AsSpan({source}))")
                        .Outdent().WriteLine("#else").Indent()
                        .WriteLine($"foreach (var {iter} in {source})")
                        .Outdent().WriteLine("#endif").Indent()
                        .WriteLine("{").Indent();
                    break;
                case RepeatedKind.Dictionary:
                    ctx.WriteLine($"#error maps not implemented yet")
                        .WriteLine($"foreach (var {iter} in {source})").WriteLine("{").Indent();
                    break;
                default:
                    ctx.WriteLine($"foreach (var {iter} in {source})").WriteLine("{").Indent();
                    break;
            }

            source = iter;
        }
    }

    private string FormatDefaultValue(CodeGenField field)
    {
        if (field.Type.IsWellKnownType(out var wellKnown))
        {
            if (!string.IsNullOrEmpty(field.DefaultValue))
            {
                switch (wellKnown)
                {
                    case CodeGenWellKnownType.Boolean when TryParseBoolean(field.DefaultValue, out var boolValue):
                        return boolValue ? "true" : "false";
                    case CodeGenWellKnownType.Double:
                        if (string.Equals(field.DefaultValue, "inf", StringComparison.OrdinalIgnoreCase)) return "double.PositiveInfinity";
                        if (string.Equals(field.DefaultValue, "-inf", StringComparison.OrdinalIgnoreCase)) return "double.NegativeInfinity";
                        if (string.Equals(field.DefaultValue, "nan", StringComparison.OrdinalIgnoreCase)) return "double.NaN";
                        return field.DefaultValue + "D";
                    case CodeGenWellKnownType.Float:
                        if (string.Equals(field.DefaultValue, "inf", StringComparison.OrdinalIgnoreCase)) return "float.PositiveInfinity";
                        if (string.Equals(field.DefaultValue, "-inf", StringComparison.OrdinalIgnoreCase)) return "float.NegativeInfinity";
                        if (string.Equals(field.DefaultValue, "nan", StringComparison.OrdinalIgnoreCase)) return "float.NaN";
                        return field.DefaultValue + "F";
                    case CodeGenWellKnownType.String:
                        return @"@""" + field.DefaultValue.Replace(@"""", @"""""") + @"""";
                    case CodeGenWellKnownType.Int32:
                    case CodeGenWellKnownType.SInt32:
                    case CodeGenWellKnownType.SFixed32:
                        return field.DefaultValue;
                    case CodeGenWellKnownType.Fixed32:
                    case CodeGenWellKnownType.UInt32:
                        return field.DefaultValue + "U";
                    case CodeGenWellKnownType.Int64:
                    case CodeGenWellKnownType.SInt64:
                    case CodeGenWellKnownType.SFixed64:
                        return field.DefaultValue + "L";
                    case CodeGenWellKnownType.Fixed64:
                    case CodeGenWellKnownType.UInt64:
                        return field.DefaultValue + "UL";
                }
                var x = 123;
            }
            else
            {
                switch (wellKnown)
                {
                    case CodeGenWellKnownType.Boolean: return "false";
                    case CodeGenWellKnownType.Double: return "0D";
                    case CodeGenWellKnownType.Float: return "0F";
                    case CodeGenWellKnownType.String: return @"""""";
                    case CodeGenWellKnownType.NetObjectProxy: return "null";
                    case CodeGenWellKnownType.Int32:
                    case CodeGenWellKnownType.SInt32:
                    case CodeGenWellKnownType.SFixed32:
                        return "0";
                    case CodeGenWellKnownType.Fixed32:
                    case CodeGenWellKnownType.UInt32:
                        return "OU";
                    case CodeGenWellKnownType.Int64:
                    case CodeGenWellKnownType.SInt64:
                    case CodeGenWellKnownType.SFixed64:
                        return "0L";
                    case CodeGenWellKnownType.Fixed64:
                    case CodeGenWellKnownType.UInt64:
                        return "0UL";
                }
            }
        }
        if (field.Type is CodeGenEnum enm && enm.EnumValues.Count != 0)
        {
            if (string.IsNullOrEmpty(field.DefaultValue))
            {
                return $"{Type(enm)}.{Escape(enm.EnumValues[0].Name)}";
            }
            foreach (var value in enm.EnumValues)
            {
                if (value.OriginalName == field.DefaultValue)
                {
                    return $"{Type(enm)}.{Escape(enm.EnumValues[0].Name)}";
                }
            }
        }
        return $"/* invalid type / value: {field.TypeName}={field.DefaultValue} */";
    }

    private static bool TryParseBoolean(string value, out bool result)
    {
        result = default;
        return !string.IsNullOrWhiteSpace(value) && bool.TryParse(value, out result);
    }

    string Type(CodeGenType type, CodeGenGeneratorContext? ctx = null)
    {
        if (type.IsWellKnownType(out var known))
        {
            return GetEscapedTypeName(ctx, type, out _);
        }
        return "global::" + type.ToString().Replace('+', '.'); // lazy
    }

    void GetEnumSource(CodeGenField field, CodeGenGeneratorContext ctx, ref CodeGenWellKnownType knownType, ref string source)
    {
        if (field.Type is CodeGenEnum enm)
        {
            if (enm.Type.IsWellKnownType(out knownType))
            {
                source = $"({Type(enm.Type, ctx)}){source}";
            }
        }
    }
    private void GetEnumTarget(CodeGenField field, CodeGenGeneratorContext ctx, ref string target)
    {
        if (field.Type is CodeGenEnum enm)
        {
            target = $"({Type(enm, ctx)}){target}";
        }
    }
}
