using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProtoBuf
{
    public sealed class Schema
    {
        public Options Options { get; } = new Options();
        private Schema() { }
        public static Schema Parse(TextReader schema)
        {
            var parsed = new Schema();
            using (var tokens = new Peekable<Token>(schema.Tokenize().RemoveCommentsAndWhitespace()))
            {
                while (tokens.Peek(out Token token))
                {
                    if (ProtoBase.TryParse(tokens, parsed.Syntax, out var item))
                    {
                        parsed.Items.Add(item);
                    }
                    else if (token.Is(TokenType.AlphaNumeric))
                    {
                        switch (token.Value)
                        {
                            case "syntax":
                                if (parsed.Items.Any())
                                {
                                    token.SyntaxError("must preceed all other instructions");
                                }
                                tokens.Consume();
                                tokens.Consume(TokenType.Symbol, "=");
                                parsed.Syntax = tokens.ConsumeEnum<ProtoSyntax>(TokenType.StringLiteral);
                                tokens.Consume(TokenType.Symbol, ";");
                                break;
                            case "package":
                                tokens.Consume();
                                parsed.Package = tokens.Consume(TokenType.AlphaNumeric);
                                tokens.Consume(TokenType.Symbol, ";");
                                break;
                            case "option":
                                parsed.Options.ParseForSchema(tokens);
                                break;
                            default:
                                token.SyntaxError();
                                break;
                        }
                    }
                    else
                    {
                        token.SyntaxError();
                    }
                }
            }
            return parsed;
        }

        public string GenerateCSharp()
        {
            var sb = new StringBuilder();

            int indent = 0;
            var @namespace = Options["csharp_namespace"] ?? Package;
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                sb.AppendLine(indent, $"namespace {@namespace}");
                sb.AppendLine(indent++, "{");
            }
            List<Message> stack = new List<Message>();
            foreach (var obj in Items)
            {
                if (obj is Enum)
                {
                    Write(sb, indent, (Enum)obj);
                }
                else if (obj is Message)
                {
                    Write(sb, indent, (Message)obj, stack);
                }
            }
            if (!string.IsNullOrWhiteSpace(Package))
            {
                sb.AppendLine(--indent, "}");
            }
            return sb.ToString();
        }

        private void Write(StringBuilder sb, int indent, Message message, List<Message> stack)
        {
            sb.AppendLine(indent, $@"[global::ProtoBuf.ProtoContract(Name = @""{message.Name}"")]");
            sb.AppendLine(indent, $"public partial class {message.Name}");
            sb.AppendLine(indent++, "{");
            stack.Add(message);
            foreach (var obj in message.Items)
            {
                if (obj is Message)
                {
                    Write(sb, indent, (Message)obj, stack);
                }
                else if (obj is Enum)
                {
                    Write(sb, indent, (Enum)obj);
                }
                else if (obj is Field)
                {
                    Write(sb, indent, (Field)obj, stack);
                }
            }
            stack.RemoveAt(stack.Count - 1);
            sb.AppendLine(--indent, "}");
        }
        private bool UseArray(Field field)
        {
            switch (field.Type)
            {
                case "double":
                case "float":
                case "bool":
                case "string":
                case "sint32":
                case "int32":
                case "sfixed32":
                case "sint64":
                case "int64":
                case "sfixed64":
                case "fixed32":
                case "uint32":
                case "fixed64":
                case "uint64":
                    return true;
                default:
                    return false;
            }
        }
        private void Write(StringBuilder sb, int indent, Field field, List<Message> stack)
        {
            sb.Append(indent, $"[global::ProtoBuf.ProtoMember({field.Number}");
            bool isOptional = (field.Modifiers & Field.FieldModifiers.Optional) != 0;
            bool isRepeated = (field.Modifiers & Field.FieldModifiers.Repeated) != 0;
            string defaultValue = null;
            if (isOptional)
            {
                defaultValue = field.Options["default"];

                if (field.Type == "string")
                {
                    defaultValue = string.IsNullOrEmpty(defaultValue) ? "\"\""
                        : ("@\"" + (defaultValue ?? "").Replace("\"", "\"\"") + "\"");
                }
                else if (!string.IsNullOrWhiteSpace(defaultValue) && TryResolveEnum(field.Type, stack, out Enum @enum))
                {
                    defaultValue = @enum.Name + "." + defaultValue;
                }
            }
            var typeName = GetTypeName(field, out var dataFormat);
            if (!string.IsNullOrWhiteSpace(dataFormat))
            {
                sb.Append($", DataFormat=DataFormat.{dataFormat}");
            }
            if (string.Equals(field.Options["packed"] ?? "", "true", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append($", IsPacked = true");
            }
            if ((field.Modifiers & Field.FieldModifiers.Required) != 0)
            {
                sb.Append($", IsRequired = true");
            }
            sb.AppendLine(")]");
            if (!isRepeated && !string.IsNullOrWhiteSpace(defaultValue))
            {
                sb.AppendLine(indent, $"[global::System.ComponentModel.DefaultValue({defaultValue})]");
            }
            if (string.Equals(field.Options["deprecated"] ?? "", "true", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine(indent, $"[global::System.Obsolete]");
            }
            if (isRepeated)
            {
                if(UseArray(field))
                {
                    sb.AppendLine(indent, $"public {typeName}[] {field.Name} {{ get; set; }}");
                }
                else
                {
                    sb.AppendLine(indent, $"public global::System.Collections.Generic.List<{typeName}> {field.Name} {{ get; }} = new global::System.Collections.Generic.List<{typeName}>();");
                }
            }
            else
            {
                sb.Append(indent, $"public {typeName} {field.Name} {{ get; set; }}");
                if (!string.IsNullOrWhiteSpace(defaultValue)) sb.Append($" = {defaultValue};");
                sb.AppendLine();
            }
        }

        private bool TryResolveEnum(string type, List<Message> stack, out Enum @enum)
        {
            for(int i = stack.Count - 1; i >= 0; i--)
            {
                @enum = stack[i].Enums.FirstOrDefault(x => x.Name == type);
                if (@enum != null) return true;
            }
            @enum = null;
            return false;
        }

        private string GetTypeName(Field field, out string dataFormat)
        {
            const string SIGNED = "ZigZag", FIXED = "FixedSize";
            dataFormat = "";
            switch (field.Type)
            {
                case "double":
                case "float":
                case "bool":
                case "string":
                    return field.Type;
                case "sint32":
                    dataFormat = SIGNED;
                    return "int";
                case "int32":
                    return "int";
                case "sfixed32":
                    dataFormat = FIXED;
                    return "int";
                case "sint64":
                    dataFormat = SIGNED;
                    return "long";
                case "int64":
                    return "long";
                case "sfixed64":
                    dataFormat = FIXED;
                    return "long";
                case "fixed32":
                    dataFormat = FIXED;
                    return "uint";
                case "uint32":
                    return "uint";
                case "fixed64":
                    dataFormat = FIXED;
                    return "ulong";
                case "uint64":
                    return "ulong";
                case "bytes":
                    return "byte[]";
                default:
                    return field.Type;
            }
        }

        private void Write(StringBuilder sb, int indent, Enum @enum)
        {
            sb.AppendLine(indent, $"public enum {@enum.Name}");
            sb.AppendLine(indent++, "{");
            foreach (var val in @enum.Values)
            {
                sb.AppendLine(indent, $"{val.Name} = {val.Value},");
            }
            sb.AppendLine(--indent, "}");
        }

        public IEnumerable<Message> Messages => Items.OfType<Message>();
        public List<ProtoBase> Items { get; } = new List<ProtoBase>();
        public ProtoSyntax Syntax { get; private set; } = ProtoSyntax.proto2;
        public string Package { get; private set; } = "";
    }


}
