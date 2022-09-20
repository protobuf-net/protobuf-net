using System;

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal partial class CodeGenCSharpCodeGenerator
{
    protected override void WriteMessageSerializer(CodeGenGeneratorContext ctx, CodeGenMessage message, ref object state)
    {
        // very much work-in-progress; not much of this is expected to work at all!
        const string NanoNS = "global::ProtoBuf.Nano.";

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

        ctx.WriteLine($"internal static void Serialize({Escape(message.Name)} value, ref {NanoNS}Writer writer)").WriteLine("{").Indent();

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
                        .WriteLine($"writer.WriteTag({GetTag(field, WireType.String)}); // field {field.FieldNumber}, string")
                        .WriteLine($"writer.WriteVarintUInt64({Type(field.Type)}.Measure(obj{field.FieldNumber});")
                        .WriteLine($"{Type(field.Type)}.Write(obj{field.FieldNumber}, ref writer);")
                        .Outdent().WriteLine("}");
                    break;
                case CodeGenWellKnownType.String:
                    ctx.WriteLine($"if (value.{field.BackingName} is string s && s.Length > 0)").WriteLine("{").Indent()
                        .WriteLine($"writer.WriteTag({GetTag(field, WireType.String)}); // field {field.FieldNumber}, string")
                        .WriteLine($"writer.WriteWithLengthPrefix(s);")
                        .Outdent().WriteLine("}");
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
                    ctx.WriteLine($"if (value.{field.BackingName} is string s && s.Length > 0)").WriteLine("{").Indent()
                        .WriteLine($"len += {Measure(field, WireType.String)} + {NanoNS}Writer.MeasureWithLengthPrefix(s);")
                        .Outdent().WriteLine("}");
                    break;
                default:
                    throw new NotImplementedException($"type not yet supported in {nameof(WriteMessageSerializer)}: {knownType}");
            }
        }
        ctx.WriteLine("return len;").Outdent().WriteLine("}").WriteLine();

        ctx.WriteLine($"internal static {Escape(message.Name)} Merge({Escape(message.Name)} value, ref {NanoNS}Reader reader)").WriteLine("{").Indent()
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
                case CodeGenWellKnownType.None:
                    ctx.WriteLine($"case {GetTag(field, WireType.String)}: // field {field.FieldNumber}, string").Indent()
                        .WriteLine($"oldEnd = reader.ConstrainByLengthPrefix();")
                        .WriteLine($"value.{field.BackingName} = {Type(field.Type)}.Merge(value.{field.BackingName}, ref reader);")
                        .WriteLine("reader.Unconstrain(oldEnd);").WriteLine("break;")
                        .Outdent();
                    // we always write both string and group decoder, because protobuf-net has always been forgiving
                    ctx.WriteLine($"case {GetTag(field, WireType.StartGroup)}: // field {field.FieldNumber}, group").Indent()
                        .WriteLine($"oldEnd = reader.ConstrainByGroup(tag);")
                        .WriteLine($"value.{field.BackingName} = {Type(field.Type)}.Merge(value.{field.BackingName}, ref reader);")
                        .WriteLine("reader.Unconstrain(oldEnd);").WriteLine("break;")
                        .Outdent();
                    break;
                default:
                    throw new NotImplementedException($"type not yet supported in {nameof(WriteMessageSerializer)}: {knownType}");
            }
        }
        ctx.WriteLine("default:").Indent().WriteLine("reader.Skip(tag);").WriteLine("break;").Outdent();
        ctx.Outdent().WriteLine("}").Outdent().WriteLine("}");


       

        ctx.WriteLine("return value;").Outdent().WriteLine("}").WriteLine();
    }



    string Type(CodeGenType type) => "global::" + type.ToString().Replace('+', '.'); // lazy
}
