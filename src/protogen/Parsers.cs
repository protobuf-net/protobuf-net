using Google.Protobuf.Reflection;
using ProtoBuf;
using System.Collections.Generic;
using System.Linq;

namespace Google.Protobuf.Reflection
{
#pragma warning disable CS1591
    partial class FileDescriptorProto
    {
        public static FileDescriptorProto Parse(System.IO.TextReader schema)
            => Parsers.Parse(schema);
    }
#pragma warning restore CS1591
}
namespace ProtoBuf
{
    internal static class Parsers
    {
        public static FileDescriptorProto Parse(System.IO.TextReader schema)
        {
            var parsed = new FileDescriptorProto();
            parsed.name = ((schema as System.IO.StreamReader)?.BaseStream as System.IO.FileStream)?.Name ?? "";

            using (var tokens = new Peekable<Token>(schema.Tokenize().RemoveCommentsAndWhitespace()))
            {
                while (tokens.Peek(out Token token))
                {
                    if (TryParseFileDescriptorProtoChildren(tokens, parsed.syntax, parsed))
                    {
                        // handled
                    }
                    else if (token.Is(TokenType.AlphaNumeric))
                    {
                        switch (token.Value)
                        {
                            case "syntax":
                                if (parsed.message_type.Any())
                                {
                                    token.SyntaxError("syntax must be set before messages are included");
                                }
                                tokens.Consume();
                                tokens.Consume(TokenType.Symbol, "=");
                                parsed.syntax = tokens.Consume(TokenType.StringLiteral);
                                tokens.Consume(TokenType.Symbol, ";");
                                break;
                            case "package":
                                tokens.Consume();
                                parsed.package = tokens.Consume(TokenType.AlphaNumeric);
                                tokens.Consume(TokenType.Symbol, ";");
                                break;
                            case "option":
                                parsed.options = ParseFileOptions(tokens, parsed.options);
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
        internal static bool TryParseFileDescriptorProtoChildren(Peekable<Token> tokens, string syntax, FileDescriptorProto schema)
        {
            if (tokens.Peek(out var token) && token.Is(TokenType.AlphaNumeric))
            {
                switch (token.Value)
                {
                    case "message":
                        schema.message_type.Add(ParseDescriptorProto(tokens, syntax));
                        return true;
                    case "enum":
                        schema.enum_type.Add(ParseEnumDescriptorProto(tokens, syntax));
                        return true;
                }
            }
            return false;
        }
        internal static bool TryParseDescriptorProtoChildren(Peekable<Token> tokens, string syntax, DescriptorProto message)
        {
            if (tokens.Peek(out var token) && token.Is(TokenType.AlphaNumeric))
            {
                switch (token.Value)
                {
                    case "message":
                        message.nested_type.Add(ParseDescriptorProto(tokens, syntax));
                        return true;
                    case "enum":
                        message.enum_type.Add(ParseEnumDescriptorProto(tokens, syntax));
                        return true;
                    case "reserved":
                        ParseReservedRanges(message.reserved_name, message.reserved_range, tokens, syntax);
                        return true;
                    case "extensions":
                        ParseExtensionRange(message.extension_range, tokens, syntax);
                        return true;
                }
            }
            return false;
        }

        private static void ParseReservedRanges(List<string> names, List<DescriptorProto.ReservedRange> ranges, Peekable<Token> tokens, string syntax)
        {
            tokens.Consume(TokenType.AlphaNumeric, "reserved");
            var token = tokens.Read(); // test the first one to determine what we're doing
            switch (token.Type)
            {
                case TokenType.StringLiteral:
                    while (true)
                    {
                        names.Add(tokens.Consume(TokenType.StringLiteral));
                        token = tokens.Read();
                        if (token.Is(TokenType.Symbol, ","))
                        {
                            tokens.Consume();
                        }
                        else if (token.Is(TokenType.Symbol, ";"))
                        {
                            tokens.Consume();
                            break;
                        }
                        else
                        {
                            token.SyntaxError();
                        }
                    }
                    break;
                case TokenType.AlphaNumeric:
                    while (true)
                    {
                        int from = tokens.ConsumeInt32(), to = from;
                        if (tokens.Read().Is(TokenType.AlphaNumeric, "to"))
                        {
                            tokens.Consume();
                            to = tokens.ConsumeInt32();
                        }
                        ranges.Add(new DescriptorProto.ReservedRange { start = from, end = to });
                        token = tokens.Read();
                        if (token.Is(TokenType.Symbol, ","))
                        {
                            tokens.Consume();
                        }
                        else if (token.Is(TokenType.Symbol, ";"))
                        {
                            tokens.Consume();
                            break;
                        }
                        else
                        {
                            token.SyntaxError();
                        }
                    }
                    break;
                default:
                    throw token.SyntaxError();
            }
        }

        private static void ParseExtensionRange(List<DescriptorProto.ExtensionRange> ranges, Peekable<Token> tokens, string syntax)
        {
            tokens.Consume(TokenType.AlphaNumeric, "extensions");

            const int MAX = 536870911;
            while (true)
            {
                int from = tokens.ConsumeInt32(MAX), to = from;
                if (tokens.Read().Is(TokenType.AlphaNumeric, "to"))
                {
                    tokens.Consume();
                    to = tokens.ConsumeInt32(MAX);
                }
                ranges.Add(new DescriptorProto.ExtensionRange { start = from, end = to });

                var token = tokens.Read();
                if (token.Is(TokenType.Symbol, ","))
                {
                    tokens.Consume();
                }
                else if (token.Is(TokenType.Symbol, ";"))
                {
                    tokens.Consume();
                    break;
                }
                else
                {
                    throw token.SyntaxError();
                }
            }
        }

        public static DescriptorProto ParseDescriptorProto(Peekable<Token> tokens, string syntax)
        {
            tokens.Consume(TokenType.AlphaNumeric, "message");

            string msgName = tokens.Consume(TokenType.AlphaNumeric);
            var message = new DescriptorProto { name = msgName };
            tokens.Consume(TokenType.Symbol, "{");
            while (tokens.Peek(out Token token) && !token.Is(TokenType.Symbol, "}"))
            {
                if (TryParseDescriptorProtoChildren(tokens, syntax, message))
                {
                    // handled
                }
                else
                {   // assume anything else is a field
                    message.field.Add(ParseFieldDescriptorProto(tokens, syntax));
                }
            }
            tokens.Consume(TokenType.Symbol, "}");
            return message;

        }
        public static FieldDescriptorProto ParseFieldDescriptorProto(Peekable<Token> tokens, string syntax)
        {
            FieldDescriptorProto.Label label = default(FieldDescriptorProto.Label);

            var token = tokens.Read();
            if (token.Is(TokenType.AlphaNumeric, "repeated"))
            {
                label = FieldDescriptorProto.Label.LABEL_REPEATED;
                tokens.Consume();
            }
            else if (token.Is(TokenType.AlphaNumeric, "required"))
            {
                label = FieldDescriptorProto.Label.LABEL_REQUIRED;
                tokens.Consume();
            }
            else if (token.Is(TokenType.AlphaNumeric, "optional"))
            {
                label = FieldDescriptorProto.Label.LABEL_OPTIONAL;
                tokens.Consume();
            }

            string type = tokens.Consume(TokenType.AlphaNumeric);
            string name = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");
            var number = tokens.ConsumeInt32();

            var field = new FieldDescriptorProto
            {
                type_name = type,
                name = name,
                number = number,
                label = label
            };
            bool haveEndedSemicolon = false;
            if (tokens.Read().Is(TokenType.Symbol, "["))
            {
                string defaultValue = field.default_value;
                field.options = ParseFieldOptions(tokens, field.options, out haveEndedSemicolon, ref defaultValue);
                field.default_value = defaultValue;
            }

            if (!haveEndedSemicolon)
            {
                tokens.Consume(TokenType.Symbol, ";");
            }
            return field;
        }
        private static FileOptions ParseFileOptions(Peekable<Token> tokens, FileOptions options)
        {
            tokens.Consume(TokenType.AlphaNumeric, "option");
            var key = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");
            switch (key)
            {
                case nameof(options.deprecated):
                    if (options == null) options = new FileOptions();
                    options.deprecated = tokens.ConsumeBoolean();
                    break;
                case nameof(options.csharp_namespace):
                    if (options == null) options = new FileOptions();
                    options.csharp_namespace = tokens.ConsumeString();
                    break;
                case nameof(options.optimize_for):
                    if (options == null) options = new FileOptions();
                    options.optimize_for = tokens.ConsumeEnum<FileOptions.OptimizeMode>(TokenType.AlphaNumeric);
                    break;
                default:
                    // drop it on the floor
                    tokens.ConsumeString();
                    break;

            }
            tokens.Consume(TokenType.Symbol, ";");
            return options;
        }
        private static FieldOptions ParseFieldOptions(Peekable<Token> tokens, FieldOptions options, out bool consumedSemicolon, ref string defaultValue)
        {
            tokens.Consume(TokenType.Symbol, "[");
            while (true)
            {
                var token = tokens.Read();
                if (token.Is(TokenType.Symbol, "]"))
                {
                    tokens.Consume();
                    consumedSemicolon = false;
                    break;
                }
                else if (token.Is(TokenType.Symbol, "];"))
                {
                    tokens.Consume();
                    consumedSemicolon = true;
                    break;
                }
                else if (token.Is(TokenType.Symbol, ","))
                {
                    tokens.Consume();
                }
                else
                {
                    var key = tokens.Consume(TokenType.AlphaNumeric);
                    tokens.Consume(TokenType.Symbol, "=");
                    switch(key)
                    {
                        case nameof(options.deprecated):
                            if (options == null) options = new FieldOptions();
                            options.deprecated = tokens.ConsumeBoolean();
                            break;
                        case nameof(options.packed):
                            if (options == null) options = new FieldOptions();
                            options.packed = tokens.ConsumeBoolean();
                            break;
                        case "default":
                            defaultValue = tokens.ConsumeString();
                            break;
                        default:
                            // drop it on the floor
                            tokens.ConsumeString();
                            break;
                    }
                }
            }
            return options;
        }

        public static EnumDescriptorProto ParseEnumDescriptorProto(Peekable<Token> tokens, string syntax)
        {
            tokens.Consume(TokenType.AlphaNumeric, "enum");
            var obj = new EnumDescriptorProto { name = tokens.Consume(TokenType.AlphaNumeric) };
            tokens.Consume(TokenType.Symbol, "{");

            bool cont = true;
            while (cont)
            {
                var token = tokens.Read();
                if (token.Is(TokenType.Symbol, "};"))
                {
                    tokens.Consume();
                    cont = false;
                }
                else if (token.Is(TokenType.Symbol, "}"))
                {
                    tokens.Consume();
                    // trailing semi-colon is optional and used inconsistently
                    if (tokens.Peek(out token) & token.Is(TokenType.Symbol, ";"))
                    {
                        tokens.Consume();
                    }
                    cont = false;
                }
                else
                {
                    obj.value.Add(ParseEnumValueDescriptorProto(tokens, syntax));
                }
            }
            return obj;
        }

        public static EnumValueDescriptorProto ParseEnumValueDescriptorProto(Peekable<Token> tokens, string syntax)
        {
            string name = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");
            var value = tokens.ConsumeInt32();
            tokens.Consume(TokenType.Symbol, ";");
            return new EnumValueDescriptorProto { name = name, number = value };
        }
    }
}
