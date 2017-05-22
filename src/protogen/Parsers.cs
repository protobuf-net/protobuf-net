using Google.Protobuf.Reflection;
using ProtoBuf;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Google.Protobuf.Reflection
{
#pragma warning disable CS1591
    partial class FileDescriptorProto
    {
        public static FileDescriptorProto Parse(System.IO.TextReader schema)
            => Parsers.Parse(schema);

        internal bool TryResolveEnum(string type, DescriptorProto parent, out EnumDescriptorProto @enum)
        {
            while (parent != null)
            {
                @enum = parent.EnumTypes.FirstOrDefault(x => x.Name == type);
                if (@enum != null) return true;
                parent = parent.Parent;
            }
            @enum = EnumTypes.FirstOrDefault(x => x.Name == type);
            return @enum != null;
        }

        internal bool TryResolveMessage(string type, DescriptorProto parent, out DescriptorProto message)
        {
            while (parent != null)
            {
                message = parent.NestedTypes.FirstOrDefault(x => x.Name == type);
                if (message != null) return true;
                parent = parent.Parent;
            }
            message = MessageTypes.FirstOrDefault(x => x.Name == type);
            return message != null;
        }

        static void SetParents(EnumDescriptorProto parent)
        {
            foreach (var val in parent.Values)
            {
                val.Parent = parent;
            }
        }
        static void SetParents(DescriptorProto parent)
        {
            foreach (var field in parent.Fields)
            {
                field.Parent = parent;
            }
            foreach (var @enum in parent.EnumTypes)
            {
                @enum.Parent = parent;
                SetParents(@enum);
            }
            foreach (var child in parent.NestedTypes)
            {
                child.Parent = parent;
                SetParents(child);
            }
        }
        internal void FixupTypes()
        {
            foreach (var type in EnumTypes)
            {
                SetParents(type);
            }
            foreach (var type in MessageTypes)
            {
                SetParents(type);
            }
            foreach (var type in MessageTypes)
            {
                ResolveFieldTypes(type);
            }
        }

        private void ResolveFieldTypes(DescriptorProto type)
        {
            foreach (var field in type.Fields)
            {
                if (field.TypeName != null && field.type == default(FieldDescriptorProto.Type))
                {
                    if (TryResolveMessage(field.TypeName, type, out var msg))
                    {
                        // TODO: how to identify groups? FieldDescriptorProto.Type.TypeGroup
                        // do I need to track that bespokely? or is there something on the type?
                        field.type = FieldDescriptorProto.Type.TypeMessage;
                    }
                    else if (TryResolveEnum(field.TypeName, type, out var @enum))
                    {
                        field.type = FieldDescriptorProto.Type.TypeEnum;
                    }
                }
            }
        }
    }
    partial class EnumDescriptorProto
    {
        internal DescriptorProto Parent { get; set; }
    }
    partial class FieldDescriptorProto
    {
        internal DescriptorProto Parent { get; set; }
    }
    partial class DescriptorProto
    {
        internal DescriptorProto Parent { get; set; }
    }

    partial class EnumValueDescriptorProto
    {
        internal EnumDescriptorProto Parent { get; set; }
    }
#pragma warning restore CS1591
}
namespace ProtoBuf
{
    internal static class Parsers
    {
        internal const string SyntaxProto2 = "proto2", SyntaxProto3 = "proto3";
        public static FileDescriptorProto Parse(System.IO.TextReader schema)
        {
            var parsed = new FileDescriptorProto();
            parsed.Syntax = SyntaxProto2;
            parsed.Name = ((schema as System.IO.StreamReader)?.BaseStream as System.IO.FileStream)?.Name ?? "";

            using (var tokens = new Peekable<Token>(schema.Tokenize().RemoveCommentsAndWhitespace()))
            {
                while (tokens.Peek(out Token token))
                {
                    if (TryParseFileDescriptorProtoChildren(tokens, parsed.Syntax, parsed))
                    {
                        // handled
                    }
                    else if (token.Is(TokenType.AlphaNumeric))
                    {
                        switch (token.Value)
                        {
                            case "syntax":
                                if (parsed.MessageTypes.Any())
                                {
                                    token.SyntaxError("syntax must be set before messages are included");
                                }
                                tokens.Consume();
                                tokens.Consume(TokenType.Symbol, "=");
                                parsed.Syntax = tokens.Consume(TokenType.StringLiteral);
                                tokens.Consume(TokenType.Symbol, ";");
                                break;
                            case "package":
                                tokens.Consume();
                                parsed.Package = tokens.Consume(TokenType.AlphaNumeric);
                                tokens.Consume(TokenType.Symbol, ";");
                                break;
                            case "option":
                                parsed.Options = ParseFileOptions(tokens, parsed.Options);
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
            parsed.FixupTypes();
            return parsed;
        }

        internal static bool TryParseFileDescriptorProtoChildren(Peekable<Token> tokens, string syntax, FileDescriptorProto schema)
        {
            if (tokens.Peek(out var token))
            {
                if (token.Is(TokenType.AlphaNumeric))
                {
                    switch (token.Value)
                    {
                        case "message":
                            schema.MessageTypes.Add(ParseDescriptorProto(tokens, syntax));
                            return true;
                        case "enum":
                            schema.EnumTypes.Add(ParseEnumDescriptorProto(tokens, syntax));
                            return true;
                    }
                }
                else if (token.Is(TokenType.Symbol, ";"))
                {
                    tokens.Consume();
                    return true;
                }
            }
            return false;
        }
        internal static bool TryParseDescriptorProtoChildren(Peekable<Token> tokens, string syntax, DescriptorProto message)
        {
            if (tokens.Peek(out var token))
            {
                if (token.Is(TokenType.AlphaNumeric))
                {
                    switch (token.Value)
                    {
                        case "message":
                            message.NestedTypes.Add(ParseDescriptorProto(tokens, syntax));
                            return true;
                        case "enum":
                            message.EnumTypes.Add(ParseEnumDescriptorProto(tokens, syntax));
                            return true;
                        case "reserved":
                            ParseReservedRanges(message.ReservedNames, message.ReservedRanges, tokens, syntax);
                            return true;
                        case "extensions":
                            token.RequireProto2(syntax);
                            ParseExtensionRange(message.ExtensionRanges, tokens, syntax);
                            return true;
                    }
                }
                else if (token.Is(TokenType.Symbol, ";"))
                {
                    tokens.Consume();
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
                        ranges.Add(new DescriptorProto.ReservedRange { Start = from, End = to });
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
                ranges.Add(new DescriptorProto.ExtensionRange { Start = from, End = to });

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
            var message = new DescriptorProto { Name = msgName };
            tokens.Consume(TokenType.Symbol, "{");
            while (tokens.Peek(out Token token) && !token.Is(TokenType.Symbol, "}"))
            {
                if (TryParseDescriptorProtoChildren(tokens, syntax, message))
                {
                    // handled
                }
                else
                {   // assume anything else is a field
                    message.Fields.Add(ParseFieldDescriptorProto(tokens, syntax));
                }
            }
            tokens.Consume(TokenType.Symbol, "}");
            return message;

        }
        public static FieldDescriptorProto ParseFieldDescriptorProto(Peekable<Token> tokens, string syntax)
        {
            FieldDescriptorProto.Label label;

            var token = tokens.Read();
            if (syntax != Parsers.SyntaxProto2)
            {
                label = FieldDescriptorProto.Label.LabelOptional;
            }
            else if (token.Is(TokenType.AlphaNumeric, "repeated"))
            {
                label = FieldDescriptorProto.Label.LabelRepeated;
                tokens.Consume();
            }
            else if (token.Is(TokenType.AlphaNumeric, "required"))
            {
                token.RequireProto2(syntax);
                label = FieldDescriptorProto.Label.LabelRequired;
                tokens.Consume();
            }
            else if (token.Is(TokenType.AlphaNumeric, "optional"))
            {
                token.RequireProto2(syntax);
                label = FieldDescriptorProto.Label.LabelOptional;
                tokens.Consume();
            }
            else
            {
                throw token.SyntaxError("Expected 'repeated' / 'required' / 'optional'");
            }

            string typeName = tokens.Consume(TokenType.AlphaNumeric);
            string name = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");
            var number = tokens.ConsumeInt32();
            if (TryIdentifyType(typeName, out var type))
            {
                typeName = null;
            }


            var field = new FieldDescriptorProto
            {
                type = type,
                TypeName = typeName,
                Name = name,
                Number = number,
                label = label
            };

            if (syntax != Parsers.SyntaxProto2)
            {
                if (CanPack(type)) // packed by default
                {
                    var opt = field.Options ?? (field.Options = new FieldOptions());
                    opt.Packed = true;
                }
            }
            bool haveEndedSemicolon = false;
            if (tokens.Read().Is(TokenType.Symbol, "["))
            {
                ParseFieldOptions(tokens, field, syntax, out haveEndedSemicolon);
            }

            if (!haveEndedSemicolon)
            {
                tokens.Consume(TokenType.Symbol, ";");
            }
            return field;
        }

        private static bool CanPack(FieldDescriptorProto.Type type)
        {
            switch (type)
            {
                case FieldDescriptorProto.Type.TypeBool:
                case FieldDescriptorProto.Type.TypeDouble:
                case FieldDescriptorProto.Type.TypeEnum:
                case FieldDescriptorProto.Type.TypeFixed32:
                case FieldDescriptorProto.Type.TypeFixed64:
                case FieldDescriptorProto.Type.TypeFloat:
                case FieldDescriptorProto.Type.TypeInt32:
                case FieldDescriptorProto.Type.TypeInt64:
                case FieldDescriptorProto.Type.TypeSfixed32:
                case FieldDescriptorProto.Type.TypeSfixed64:
                case FieldDescriptorProto.Type.TypeSint32:
                case FieldDescriptorProto.Type.TypeSint64:
                case FieldDescriptorProto.Type.TypeUint32:
                case FieldDescriptorProto.Type.TypeUint64:
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryIdentifyType(string typeName, out FieldDescriptorProto.Type type)
        {
            bool Assign(FieldDescriptorProto.Type @in, out FieldDescriptorProto.Type @out)
            {
                @out = @in;
                return true;
            }
            switch (typeName)
            {
                case "bool": return Assign(FieldDescriptorProto.Type.TypeBool, out @type);
                case "bytes": return Assign(FieldDescriptorProto.Type.TypeBytes, out @type);
                case "double": return Assign(FieldDescriptorProto.Type.TypeDouble, out @type);
                case "fixed32": return Assign(FieldDescriptorProto.Type.TypeFixed32, out @type);
                case "fixed64": return Assign(FieldDescriptorProto.Type.TypeFixed64, out @type);
                case "float": return Assign(FieldDescriptorProto.Type.TypeFloat, out @type);
                case "int32": return Assign(FieldDescriptorProto.Type.TypeInt32, out @type);
                case "int64": return Assign(FieldDescriptorProto.Type.TypeInt64, out @type);
                case "sfixed32": return Assign(FieldDescriptorProto.Type.TypeSfixed32, out @type);
                case "sfixed64": return Assign(FieldDescriptorProto.Type.TypeSfixed64, out @type);
                case "sint32": return Assign(FieldDescriptorProto.Type.TypeSint32, out @type);
                case "sint64": return Assign(FieldDescriptorProto.Type.TypeSint64, out @type);
                case "string": return Assign(FieldDescriptorProto.Type.TypeString, out @type);
                case "uint32": return Assign(FieldDescriptorProto.Type.TypeUint32, out @type);
                case "uint64": return Assign(FieldDescriptorProto.Type.TypeUint64, out @type);
                default:
                    type = default(FieldDescriptorProto.Type);
                    return false;
            }
        }

        private static FileOptions ParseFileOptions(Peekable<Token> tokens, FileOptions options)
        {
            tokens.Consume(TokenType.AlphaNumeric, "option");
            var key = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");
            switch (key)
            {
                case "deprecated":
                    if (options == null) options = new FileOptions();
                    options.Deprecated = tokens.ConsumeBoolean();
                    break;
                case "csharp_namespace":
                    if (options == null) options = new FileOptions();
                    options.CsharpNamespace = tokens.ConsumeString();
                    break;
                case "optimize_for":
                    if (options == null) options = new FileOptions();
                    options.OptimizeFor = tokens.ConsumeEnum<FileOptions.OptimizeMode>(TokenType.AlphaNumeric);
                    break;
                default:
                    // drop it on the floor
                    tokens.ConsumeString();
                    break;

            }
            tokens.Consume(TokenType.Symbol, ";");
            return options;
        }
        private static void ParseFieldOptions(Peekable<Token> tokens, FieldDescriptorProto field, string syntax, out bool consumedSemicolon)
        {
            tokens.Consume(TokenType.Symbol, "[");
            var options = field.Options;
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
                    token = tokens.Read();
                    var key = tokens.Consume(TokenType.AlphaNumeric);
                    tokens.Consume(TokenType.Symbol, "=");
                    switch (key)
                    {
                        case "deprecated":
                            if (options == null) options = new FieldOptions();
                            options.Deprecated = tokens.ConsumeBoolean();
                            break;
                        case "packed":
                            if (!CanPack(field.type)) token.SyntaxError($"Field of type {field.type} cannot be packed");
                            if (options == null) options = new FieldOptions();
                            options.Packed = tokens.ConsumeBoolean();
                            break;
                        case "default":
                            token.RequireProto2(syntax);
                            field.DefaultValue = tokens.ConsumeString();
                            break;
                        default:
                            // drop it on the floor
                            tokens.ConsumeString();
                            break;
                    }
                }
            }
            field.Options = options;
        }

        public static EnumDescriptorProto ParseEnumDescriptorProto(Peekable<Token> tokens, string syntax)
        {
            tokens.Consume(TokenType.AlphaNumeric, "enum");
            var obj = new EnumDescriptorProto { Name = tokens.Consume(TokenType.AlphaNumeric) };
            tokens.Consume(TokenType.Symbol, "{");

            bool cont = true;
            while (cont)
            {
                var token = tokens.Read();
                if (token.Is(TokenType.Symbol, ";"))
                {
                    tokens.Consume();
                }
                else if (token.Is(TokenType.Symbol, "}"))
                {
                    tokens.Consume();
                    cont = false;
                }
                else
                {
                    obj.Values.Add(ParseEnumValueDescriptorProto(tokens, syntax));
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
            return new EnumValueDescriptorProto { Name = name, Number = value };
        }
    }
}
