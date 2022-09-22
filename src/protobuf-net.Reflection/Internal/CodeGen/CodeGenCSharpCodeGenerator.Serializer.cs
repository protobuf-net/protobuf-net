using System;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal partial class CodeGenCSharpCodeGenerator
{
    protected override void WriteMessageSerializer(CodeGenGeneratorContext ctx, CodeGenMessage message, ref object state)
    {
        // very much work-in-progress; not much of this is expected to work at all!
        const string NanoNS = "global::ProtoBuf.Nano";

        static ulong GetTag(CodeGenField field, WireType wireType)
            => ((ulong)field.FieldNumber << 3) | (ulong)wireType;

        static long Measure(CodeGenField field, WireType wireType)
        {
            var value = GetTag(field, wireType);
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

        ctx.WriteLine($"internal static void Serialize({Escape(message.Name)} value, ref {NanoNS}.Writer writer)").WriteLine("{").Indent();

        foreach (var field in message.Fields)
        {
            field.Type.IsWellKnownType(out var knownType);

            // not yet implemented:
            // repeated
            // optional/conditional/defaults
            // inheritance
            // groups
            // extension data
            // lots of well-known types
            // constructor usage
            // down-level langver
            // consideration of value-swap for sub-messages
            switch (knownType)
            {
                case CodeGenWellKnownType.None:
                    ctx.WriteLine($"if (value.{field.BackingName} is {Type(field.Type)} obj{field.FieldNumber})").WriteLine("{").Indent()
                        .WriteLine($"writer.WriteVarint({GetTag(field, WireType.String)}); // field {field.FieldNumber}, string")
                        .WriteLine($"writer.WriteVarintUInt64({Type(field.Type)}.Measure(obj{field.FieldNumber});")
                        .WriteLine($"{Type(field.Type)}.Write(obj{field.FieldNumber}, ref writer);")
                        .Outdent().WriteLine("}");
                    break;
                case CodeGenWellKnownType.String:
                case CodeGenWellKnownType.Bytes:
                    ctx.WriteLine($"if (value.{field.BackingName} is {{ Length: > 0}} s)").WriteLine("{").Indent()
                        .WriteLine($"writer.WriteVarint({GetTag(field, WireType.String)}); // field {field.FieldNumber}, string")
                        .WriteLine($"writer.WriteWithLengthPrefix(s);")
                        .Outdent().WriteLine("}");
                    break;
                case CodeGenWellKnownType.UInt32:
                case CodeGenWellKnownType.UInt64:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Varint)}); // field {field.FieldNumber}, varint")
                        .WriteLine($"writer.WriteVarint(value.{field.BackingName});");
                    break;
                case CodeGenWellKnownType.Int32:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Varint)}); // field {field.FieldNumber}, varint")
                        .WriteLine($"writer.WriteVarint(unchecked((uint)value.{field.BackingName}));");
                    break;
                case CodeGenWellKnownType.Int64:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Varint)}); // field {field.FieldNumber}, varint")
                        .WriteLine($"writer.WriteVarint(unchecked((ulong)value.{field.BackingName}));");
                    break;
                case CodeGenWellKnownType.SInt32:
                case CodeGenWellKnownType.SInt64:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Varint)}); // field {field.FieldNumber}, varint")
                        .WriteLine($"writer.WriteVarint({NanoNS}.Writer.Zig(value.{field.BackingName}));");
                    break;
                case CodeGenWellKnownType.Boolean:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Varint)}); // field {field.FieldNumber}, varint")
                        .WriteLine($"writer.WriteVarint(value.{field.BackingName});");
                    break;
                case CodeGenWellKnownType.Float:
                case CodeGenWellKnownType.Fixed32:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Fixed32)}); // field {field.FieldNumber}, fixed32")
                        .WriteLine($"writer.WriteFixed(value.{field.BackingName});");
                    break;
                case CodeGenWellKnownType.Double:
                case CodeGenWellKnownType.Fixed64:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Fixed64)}); // field {field.FieldNumber}, fixed64")
                        .WriteLine($"writer.WriteFixed(value.{field.BackingName});");
                    break;
                case CodeGenWellKnownType.SFixed32:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Fixed32)}); // field {field.FieldNumber}, fixed32")
                        .WriteLine($"writer.WriteFixed(unchecked((uint)value.{field.BackingName}));");
                    break;
                case CodeGenWellKnownType.SFixed64:
                    ctx.WriteLine($"writer.WriteVarint({GetTag(field, WireType.Fixed64)}); // field {field.FieldNumber}, fixed64")
                        .WriteLine($"writer.WriteFixed(unchecked((ulong)value.{field.BackingName}));");
                    break;
                case CodeGenWellKnownType.NetObjectProxy:
                    ctx.WriteLine($"if (value.{field.BackingName} is not null)").WriteLine("{").Indent()
                        .WriteLine(@"throw new global::System.NotSupportedException(""dynamic types/reference-tracking is not supported"");").Outdent().WriteLine("}");
                    break;
                default:
                    throw new NotImplementedException($"type not yet supported in {nameof(WriteMessageSerializer)}: {knownType}");
            }
        }
        ctx.Outdent().WriteLine("}").WriteLine();

        ctx.WriteLine($"internal static ulong Measure({Escape(message.Name)} value)").WriteLine("{").Indent().WriteLine("ulong len = 0;");

        foreach (var field in message.Fields)
        {
            field.Type.IsWellKnownType(out var knownType);
            switch (knownType)
            {
                case CodeGenWellKnownType.None:
                    ctx.WriteLine($"if (value.{field.BackingName} is {Type(field.Type)} obj{field.FieldNumber})").WriteLine("{").Indent()
                        .WriteLine($"len += {Measure(field, WireType.String)} + {Type(field.Type)}.Measure(obj{field.FieldNumber});")
                        .Outdent().WriteLine("}");
                    break;
                case CodeGenWellKnownType.String:
                case CodeGenWellKnownType.Bytes:
                    ctx.WriteLine($"if (value.{field.BackingName} is {{ Length: > 0}} s)").WriteLine("{").Indent()
                        .WriteLine($"len += {Measure(field, WireType.String)} + {NanoNS}.Writer.MeasureWithLengthPrefix(s);")
                        .Outdent().WriteLine("}");
                    break;
                case CodeGenWellKnownType.UInt32:
                case CodeGenWellKnownType.UInt64:
                    ctx.WriteLine($"len += {Measure(field, WireType.Varint)} + {NanoNS}.Writer.MeasureVarint(value.{field.BackingName});");
                    break;
                case CodeGenWellKnownType.Int32:
                    ctx.WriteLine($"len += {Measure(field, WireType.Varint)} + {NanoNS}.Writer.MeasureVarint(unchecked((uint)value.{field.BackingName}));");
                    break;
                case CodeGenWellKnownType.Int64:
                    ctx.WriteLine($"len += {Measure(field, WireType.Varint)} + {NanoNS}.Writer.MeasureVarint(unchecked((ulong)value.{field.BackingName}));");
                    break;
                case CodeGenWellKnownType.SInt32:
                case CodeGenWellKnownType.SInt64:
                    ctx.WriteLine($"len += {Measure(field, WireType.Varint)} + {NanoNS}.Writer.MeasureVarint({NanoNS}.Writer.Zig(value.{field.BackingName}));");
                    break;
                case CodeGenWellKnownType.Boolean:
                    ctx.WriteLine($"len += {Measure(field, WireType.Varint) + 1};");
                    break;
                case CodeGenWellKnownType.Float:
                case CodeGenWellKnownType.Fixed32:
                case CodeGenWellKnownType.SFixed32:
                    ctx.WriteLine($"len += {Measure(field, WireType.Varint) + 4};");
                    break;
                case CodeGenWellKnownType.Double:
                case CodeGenWellKnownType.Fixed64:
                case CodeGenWellKnownType.SFixed64:
                    ctx.WriteLine($"len += {Measure(field, WireType.Varint) + 8};");
                    break;
                case CodeGenWellKnownType.NetObjectProxy:
                    ctx.WriteLine($"if (value.{field.BackingName} is not null)").WriteLine("{").Indent()
                        .WriteLine(@"throw new global::System.NotSupportedException(""dynamic types/reference-tracking is not supported"");").Outdent().WriteLine("}");
                    break;
                default:
                    throw new NotImplementedException($"type not yet supported in {nameof(WriteMessageSerializer)}: {knownType}");
            }
        }
        ctx.WriteLine("return len;").Outdent().WriteLine("}").WriteLine();

        ctx.WriteLine($"internal static {Escape(message.Name)} Merge({Escape(message.Name)} value, ref {NanoNS}.Reader reader)").WriteLine("{").Indent()
            .WriteLine("ulong oldEnd;");
        ctx.WriteLine($"if (value is null) value = new();").WriteLine("uint tag;").WriteLine("while ((tag = reader.ReadTag()) != 0)").WriteLine("{").Indent().WriteLine("switch (tag)").WriteLine("{").Indent();

        foreach (var field in message.Fields)
        {
            field.Type.IsWellKnownType(out var knownType);
            switch (knownType)
            {
                case CodeGenWellKnownType.String:
                    ctx.WriteLine($"case {GetTag(field, WireType.String)}: // field {field.FieldNumber}, string").Indent()
                        .WriteLine($"value.{field.BackingName} = reader.ReadString();").WriteLine("break;")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.Bytes:
                    ctx.WriteLine($"case {GetTag(field, WireType.String)}: // field {field.FieldNumber}, string").Indent()
                        .WriteLine($"value.{field.BackingName} = reader.ReadBytes();").WriteLine("break;")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.None:
                    ctx.WriteLine($"case {GetTag(field, WireType.String)}: // field {field.FieldNumber}, string").Indent()
                        .WriteLine($"oldEnd = reader.ConstrainByLengthPrefix();")
                        .WriteLine($"value.{field.BackingName} = {Type(field.Type)}.Merge(value.{field.BackingName}, ref reader);")
                        .WriteLine("reader.Unconstrain(oldEnd);").WriteLine("break;")
                        .Outdent();
                    // we always write both string and group decoder, because protobuf-net has always been forgiving
                    ctx.WriteLine($"case {GetTag(field, WireType.StartGroup)}: // field {field.FieldNumber}, group").Indent()
                        .WriteLine($"value.{field.BackingName} = {Type(field.Type)}.Merge(value.{field.BackingName}, ref reader);")
                        .WriteLine($"reader.PopGroup({field.FieldNumber});").WriteLine("break;")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.UInt32:
                    ctx.WriteLine($"case {GetTag(field, WireType.Varint)}: // field {field.FieldNumber}, varint").Indent()
                        .WriteLine($"value.{field.BackingName} = reader.ReadVarint32();")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.UInt64:
                    ctx.WriteLine($"case {GetTag(field, WireType.Varint)}: // field {field.FieldNumber}, varint").Indent()
                        .WriteLine($"value.{field.BackingName} = reader.ReadVarint64();")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.Int32:
                    ctx.WriteLine($"case {GetTag(field, WireType.Varint)}: // field {field.FieldNumber}, varint").Indent()
                        .WriteLine($"value.{field.BackingName} = unchecked((int)reader.ReadVarint32());")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.Int64:
                    ctx.WriteLine($"case {GetTag(field, WireType.Varint)}: // field {field.FieldNumber}, varint").Indent()
                        .WriteLine($"value.{field.BackingName} = unchecked((int)reader.ReadVarint64());")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.SInt32:
                    ctx.WriteLine($"case {GetTag(field, WireType.Varint)}: // field {field.FieldNumber}, varint").Indent()
                        .WriteLine($"value.{field.BackingName} = {NanoNS}.Reader.Zag(reader.ReadVarint32());")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.SInt64:
                    ctx.WriteLine($"case {GetTag(field, WireType.Varint)}: // field {field.FieldNumber}, varint").Indent()
                        .WriteLine($"value.{field.BackingName} = {NanoNS}.Reader.Zag(reader.ReadVarint64());")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.Float:
                    ctx.WriteLine($"case {GetTag(field, WireType.Fixed32)}: // field {field.FieldNumber}, fixed32").Indent()
                        .WriteLine($"value.{field.BackingName} = reader.ReadFixedSingle();")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.Double:
                    ctx.WriteLine($"case {GetTag(field, WireType.Fixed64)}: // field {field.FieldNumber}, fixed64").Indent()
                        .WriteLine($"value.{field.BackingName} = reader.ReadFixedDouble();")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.Boolean:
                    ctx.WriteLine($"value.{field.BackingName} = reader.ReadVarint32() != 0;");
                    break;
                case CodeGenWellKnownType.Fixed32:
                    ctx.WriteLine($"case {GetTag(field, WireType.Fixed32)}: // field {field.FieldNumber}, fixed32").Indent()
                        .WriteLine($"value.{field.BackingName} = reader.ReadFixed32();")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.Fixed64:
                    ctx.WriteLine($"case {GetTag(field, WireType.Fixed64)}: // field {field.FieldNumber}, fixed64").Indent()
                        .WriteLine($"value.{field.BackingName} = reader.ReadFixed64();")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.SFixed32:
                    ctx.WriteLine($"case {GetTag(field, WireType.Fixed32)}: // field {field.FieldNumber}, fixed32").Indent()
                        .WriteLine($"value.{field.BackingName} = unchecked((int)reader.ReadFixed32());")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.SFixed64:
                    ctx.WriteLine($"case {GetTag(field, WireType.Fixed64)}: // field {field.FieldNumber}, fixed64").Indent()
                        .WriteLine($"value.{field.BackingName} = unchecked((long)reader.ReadFixed64());")
                        .Outdent();
                    break;
                case CodeGenWellKnownType.NetObjectProxy:
                    // we always write both string and group decoder, because protobuf-net has always been forgiving
                    ctx.WriteLine($"case {GetTag(field, WireType.String)}: // field {field.FieldNumber}, string")
                       .WriteLine($"case {GetTag(field, WireType.StartGroup)}: // field {field.FieldNumber}, group").Indent()
                       .WriteLine(@"throw new global::System.NotSupportedException(""dynamic types/reference-tracking is not supported"");")
                       .WriteLine("break;").Outdent();
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
            ctx.Indent().WriteLine($"reader.UnhandledTag(tag); // throws").WriteLine("break;").Outdent().Outdent().WriteLine("}");
        }
        ctx.WriteLine("reader.Skip(tag);").WriteLine("break;").Outdent();
        ctx.Outdent().WriteLine("}").Outdent().WriteLine("}");

        ctx.Outdent().WriteLine("ExitLoop:").Indent()
            .WriteLine("return value;").Outdent().WriteLine("}").WriteLine();
    }



    string Type(CodeGenType type) => "global::" + type.ToString().Replace('+', '.'); // lazy
}
