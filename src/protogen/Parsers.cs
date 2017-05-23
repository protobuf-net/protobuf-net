using Google.Protobuf.Reflection;
using ProtoBuf;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Google.Protobuf.Reflection
{
#pragma warning disable CS1591

    partial class FileDescriptorSet
    {
        public Error[] GetErrors() => Error.GetArray(Errors);
        internal List<ParserException> Errors { get; } = new List<ParserException>();
        public void Add(string name, System.IO.TextReader source = null)
        {
            if (!TryResolve(name, out var descriptor))
            {
                using (var reader = source ?? Open(name))
                {
                    descriptor = new FileDescriptorProto { Name = name };
                    Files.Add(descriptor);

                    Parsers.Parse(reader, descriptor, Errors);
                }
            }
        }

        private System.IO.TextReader Open(string name)
            => new System.IO.StreamReader(System.IO.File.OpenRead(name));

        bool TryResolve(string name, out FileDescriptorProto descriptor)
        {
            descriptor = Files.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return descriptor != null;
        }
    }
    partial class FileDescriptorProto
    {
        internal bool TryResolveEnum(string type, DescriptorProto parent, out EnumDescriptorProto @enum, out string fqn)
        {
            while (parent != null)
            {
                @enum = parent.EnumTypes.FirstOrDefault(x => x.Name == type);
                if (@enum != null)
                {
                    fqn = GetFQN(@enum.Parent, @enum.Name);
                    return true;
                }
                parent = parent.Parent;
            }
            @enum = EnumTypes.FirstOrDefault(x => x.Name == type);
            if (@enum != null)
            {
                fqn = GetFQN(@enum.Parent, @enum.Name);
                return true;
            }
            fqn = null;
            return false;
        }
        private string GetFQN(DescriptorProto parent, string name)
        {
            // special case a few obvious scenarios
            if (parent == null) return "." + Package + "." + name;
            if (parent.Parent == null) return "." + Package + "." + parent.Name + "." + name;

            // need to get ancestors first
            var sb = new StringBuilder(".").Append(Package);
            var stack = new Stack<DescriptorProto>();
            while (parent != null)
            {
                stack.Push(parent);
                parent = parent.Parent;
            }
            while (stack.Count != 0)
            {
                parent = stack.Pop();
                sb.Append(".").Append(parent.Name);
            }
            return sb.Append(".").Append(name).ToString();

        }
        internal bool TryResolveMessage(string typeName, DescriptorProto parent, out DescriptorProto message, out string fqn)
        {
            while (parent != null)
            {
                message = parent.NestedTypes.FirstOrDefault(x => x.Name == typeName);
                if (message != null)
                {
                    fqn = GetFQN(message.Parent, message.Name);
                    return true;
                }
                parent = parent.Parent;
            }
            message = MessageTypes.FirstOrDefault(x => x.Name == typeName);
            if (message != null)
            {
                fqn = GetFQN(message.Parent, message.Name);
                return true;
            }
            fqn = null;
            return false;
        }

        static void SetParents(string prefix, EnumDescriptorProto parent)
        {
            parent.FullyQualifiedName = prefix + "." + parent.Name;
            foreach (var val in parent.Values)
            {
                val.Parent = parent;
            }
        }
        static void SetParents(string prefix, DescriptorProto parent)
        {
            var fqn = parent.FullyQualifiedName = prefix + "." + parent.Name;
            foreach (var field in parent.Fields)
            {
                field.Parent = parent;
            }
            foreach (var @enum in parent.EnumTypes)
            {
                @enum.Parent = parent;
                SetParents(fqn, @enum);
            }
            foreach (var child in parent.NestedTypes)
            {
                child.Parent = parent;
                SetParents(fqn, child);
            }
        }
        internal void FixupTypes()
        {
            var prefix = "." + Package;
            foreach (var type in EnumTypes)
            {
                SetParents(prefix, type);
            }
            foreach (var type in MessageTypes)
            {
                SetParents(prefix, type);
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
                    string fqn;
                    if (TryResolveMessage(field.TypeName, type, out var msg, out fqn))
                    {
                        // TODO: how to identify groups? FieldDescriptorProto.Type.TypeGroup
                        // do I need to track that bespokely? or is there something on the type?
                        field.type = FieldDescriptorProto.Type.TypeMessage;
                    }
                    else if (TryResolveEnum(field.TypeName, type, out var @enum, out fqn))
                    {
                        field.type = FieldDescriptorProto.Type.TypeEnum;
                    }
                    else
                    {
                        fqn = field.TypeName;
                    }
                    field.TypeName = fqn;
                }
            }
        }
    }
    partial class EnumDescriptorProto
    {
        internal DescriptorProto Parent { get; set; }
        internal string FullyQualifiedName { get; set; }
    }
    partial class FieldDescriptorProto
    {
        internal DescriptorProto Parent { get; set; }
        internal Token TypeToken { get; set; }
    }
    partial class DescriptorProto
    {
        internal DescriptorProto Parent { get; set; }
        internal string FullyQualifiedName { get; set; }
    }

    partial class EnumValueDescriptorProto
    {
        internal EnumDescriptorProto Parent { get; set; }
    }
#pragma warning restore CS1591
}
namespace ProtoBuf
{
    public class Error
    {
        public override string ToString() =>
            Text.Length == 0
                ? $"({LineNumber},{ColumnNumber}): {(IsError ? "error" : "warning")}: {Message}"
                : $"({LineNumber},{ColumnNumber},{LineNumber},{ColumnNumber + Text.Length}): {(IsError ? "error" : "warning")}: {Message}";

        internal static Error[] GetArray(List<ParserException> errors)
        {
            if (errors.Count == 0) return noErrors;
            var arr = new Error[errors.Count];
            int index = 0;
            foreach (var err in errors)
            {
                arr[index++] = new Error(err);
            }
            return arr;
        }

        private static readonly Error[] noErrors = new Error[0];

        internal Error(ParserException ex)
        {
            ColumnNumber = ex.ColumnNumber;
            LineNumber = ex.LineNumber;
            LineContents = ex.LineContents;
            Message = ex.Message;
            IsError = ex.IsError;
            Text = ex.Text;
        }
        public bool IsWarning => !IsError;

        public bool IsError { get; }
        public string Text { get; }
        public string Message { get; }
        public string LineContents { get; }
        public int LineNumber { get; }
        public int ColumnNumber { get; }
    }
    internal class ParserContext : IDisposable
    {
        public ParserContext(FileDescriptorProto file, Peekable<Token> tokens, List<ParserException> errors)
        {
            Tokens = tokens;
            Errors = errors;
            _file = file;
        }

        public string Syntax => _file.Syntax;

        private readonly FileDescriptorProto _file;
        public Peekable<Token> Tokens { get; }
        public List<ParserException> Errors { get; }

        public void Dispose() { Tokens.Dispose(); }
    }
    internal static class Parsers
    {
        internal const string SyntaxProto2 = "proto2", SyntaxProto3 = "proto3";
        public static FileDescriptorProto Parse(System.IO.TextReader schema, FileDescriptorProto descriptor, List<ParserException> errors)
        {
            descriptor.Syntax = Parsers.SyntaxProto2;
            using (var ctx = new ParserContext(descriptor, new Peekable<Token>(schema.Tokenize().RemoveCommentsAndWhitespace()), errors))
            {
                var tokens = ctx.Tokens;
                while (tokens.Peek(out Token token))
                {
                    try
                    {
                        if (TryParseFileDescriptorProtoChildren(ctx, descriptor))
                        {
                            // handled
                        }
                        else if (token.Is(TokenType.AlphaNumeric))
                        {
                            switch (token.Value)
                            {
                                case "syntax":
                                    if (descriptor.MessageTypes.Any() || descriptor.EnumTypes.Any())
                                    {
                                        token.SyntaxError("syntax must be set types are included");
                                    }
                                    tokens.Consume();
                                    tokens.Consume(TokenType.Symbol, "=");
                                    descriptor.Syntax = tokens.Consume(TokenType.StringLiteral);
                                    tokens.Consume(TokenType.Symbol, ";");
                                    break;
                                case "package":
                                    tokens.Consume();
                                    descriptor.Package = tokens.Consume(TokenType.AlphaNumeric);
                                    tokens.Consume(TokenType.Symbol, ";");
                                    break;
                                case "option":
                                    descriptor.Options = ParseFileOptions(ctx, descriptor.Options);
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
                    catch (ParserException ex)
                    {
                        ctx.Errors.Add(ex);
                        tokens.SkipToEndStatementOrObject();
                    }
                }
            }
            descriptor.FixupTypes();
            if(descriptor.Syntax == Parsers.SyntaxProto2)
            {
                descriptor.Syntax = ""; // for output compatibility; is blank even if set to proto2 explicitly
            }
            return descriptor;
        }

        internal static bool TryParseFileDescriptorProtoChildren(ParserContext ctx, FileDescriptorProto schema)
        {
            var tokens = ctx.Tokens;
            if (tokens.Peek(out var token))
            {
                if (token.Is(TokenType.AlphaNumeric))
                {
                    switch (token.Value)
                    {
                        case "message":
                            try
                            {
                                schema.MessageTypes.Add(ParseDescriptorProto(ctx));
                            }
                            catch (ParserException ex)
                            {
                                ctx.Errors.Add(ex);
                                tokens.SkipToEndObject();
                            }
                            return true;
                        case "enum":
                            try
                            {
                                schema.EnumTypes.Add(ParseEnumDescriptorProto(ctx));
                            }
                            catch (ParserException ex)
                            {
                                ctx.Errors.Add(ex);
                                tokens.SkipToEndObject();
                            }
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
        internal static bool TryParseDescriptorProtoChildren(ParserContext ctx, DescriptorProto message)
        {
            var tokens = ctx.Tokens;
            if (tokens.Peek(out var token))
            {
                if (token.Is(TokenType.AlphaNumeric))
                {
                    switch (token.Value)
                    {
                        case "message":
                            try
                            {
                                message.NestedTypes.Add(ParseDescriptorProto(ctx));
                            }
                            catch (ParserException ex)
                            {
                                ctx.Errors.Add(ex);
                                tokens.SkipToEndObject();
                            }
                            return true;
                        case "enum":
                            try
                            {
                                message.EnumTypes.Add(ParseEnumDescriptorProto(ctx));
                            }
                            catch (ParserException ex)
                            {
                                ctx.Errors.Add(ex);
                                tokens.SkipToEndObject();
                            }
                            return true;
                        case "reserved":
                            try
                            {
                                ParseReservedRanges(message.ReservedNames, message.ReservedRanges, ctx);
                            }
                            catch (ParserException ex)
                            {
                                ctx.Errors.Add(ex);
                                tokens.SkipToEndStatement();
                            }
                            return true;
                        case "extensions":
                            try
                            {
                                token.RequireProto2(ctx.Syntax);
                                ParseExtensionRange(message.ExtensionRanges, ctx);
                            }
                            catch (ParserException ex)
                            {
                                ctx.Errors.Add(ex);
                                tokens.SkipToEndStatement();
                            }
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

        private static void ParseReservedRanges(List<string> names, List<DescriptorProto.ReservedRange> ranges, ParserContext ctx)
        {
            var tokens = ctx.Tokens;
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
                        ranges.Add(new DescriptorProto.ReservedRange { Start = from, End = to + 1 });
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

        private static void ParseExtensionRange(List<DescriptorProto.ExtensionRange> ranges, ParserContext ctx)
        {
            var tokens = ctx.Tokens;
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
                ranges.Add(new DescriptorProto.ExtensionRange { Start = from, End = to + 1 });

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

        public static DescriptorProto ParseDescriptorProto(ParserContext ctx)
        {
            var tokens = ctx.Tokens;
            tokens.Consume(TokenType.AlphaNumeric, "message");

            string msgName = tokens.Consume(TokenType.AlphaNumeric);
            var message = new DescriptorProto { Name = msgName };
            tokens.Consume(TokenType.Symbol, "{");
            while (tokens.Peek(out Token token) && !token.Is(TokenType.Symbol, "}"))
            {
                if (TryParseDescriptorProtoChildren(ctx, message))
                {
                    // handled
                }
                else
                {   // assume anything else is a field
                    try
                    {
                        message.Fields.Add(ParseFieldDescriptorProto(ctx));
                    }
                    catch (ParserException ex)
                    {
                        ctx.Errors.Add(ex);
                        tokens.SkipToEndStatement();
                    }
                }
            }
            tokens.Consume(TokenType.Symbol, "}");
            return message;

        }
        public static FieldDescriptorProto ParseFieldDescriptorProto(ParserContext ctx)
        {
            var tokens = ctx.Tokens;
            FieldDescriptorProto.Label label;

            var token = tokens.Read();
            if (ctx.Syntax != Parsers.SyntaxProto2)
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
                token.RequireProto2(ctx.Syntax);
                label = FieldDescriptorProto.Label.LabelRequired;
                tokens.Consume();
            }
            else if (token.Is(TokenType.AlphaNumeric, "optional"))
            {
                token.RequireProto2(ctx.Syntax);
                label = FieldDescriptorProto.Label.LabelOptional;
                tokens.Consume();
            }
            else
            {
                throw token.SyntaxError("Expected 'repeated' / 'required' / 'optional'");
            }

            var typeToken = tokens.Read();
            string typeName = tokens.Consume(TokenType.AlphaNumeric);
            string name = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");
            var number = tokens.ConsumeInt32();
            if (TryIdentifyType(typeName, out var type))
            {
                typeName = "";
            }


            var field = new FieldDescriptorProto
            {
                type = type,
                TypeName = typeName,
                Name = name,
                JsonName = GetJsonName(name),
                Number = number,
                label = label,
                TypeToken = typeToken // internal property that helps give useful error messages
            };

            if (ctx.Syntax != Parsers.SyntaxProto2)
            {
                if (CanPack(type)) // packed by default
                {
                    var opt = field.Options ?? (field.Options = new FieldOptions());
                    opt.Packed = true;
                }
            }
            if (tokens.Read().Is(TokenType.Symbol, "["))
            {
                try
                {
                    ParseFieldOptions(ctx, field);
                }
                catch (ParserException ex)
                {
                    ctx.Errors.Add(ex);
                    tokens.SkipToEndOptions();
                }
            }

            tokens.Consume(TokenType.Symbol, ";");
            return field;
        }

        private static string GetJsonName(string name)
            => Regex.Replace(name, "_([a-zA-Z])", match => match.Groups[1].Value.ToUpperInvariant());


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
#pragma warning disable 0618, 0612
        private static readonly Dictionary<string, Action<FileOptions, bool>> fileBoolActions = new Dictionary<string, Action<FileOptions, bool>>
        {
            { "cc_enable_arenas", (options, value) => options.CcEnableArenas = value },
            { "cc_generic_services", (options, value) => options.CcGenericServices = value },
            { "deprecated", (options, value) => options.Deprecated = value },
            { "java_generate_equals_and_hash", (options, value) => options.JavaGenerateEqualsAndHash = value },
            { "java_generic_services", (options, value) => options.JavaGenericServices  = value },
            { "java_multiple_files", (options, value) => options.JavaMultipleFiles = value },
            { "java_string_check_utf8", (options, value) => options.JavaStringCheckUtf8 = value },
            { "py_generic_services", (options, value) => options.PyGenericServices = value },
        };

        private static readonly Dictionary<string, Action<FileOptions, string>> fileStringActions = new Dictionary<string, Action<FileOptions, string>>
        {
            { "csharp_namespace", (options, value) => options.CsharpNamespace = value },
            { "go_package", (options, value) => options.GoPackage = value },
            { "java_outer_classname", (options, value) => options.JavaOuterClassname = value },
            { "java_package", (options, value) => options.JavaPackage = value },
            { "objc_class_prefix", (options, value) => options.ObjcClassPrefix  = value },
            { "php_class_prefix", (options, value) => options.PhpClassPrefix = value },
            { "swift_prefix", (options, value) => options.SwiftPrefix = value },
        };
        private static readonly Dictionary<string, Action<MessageOptions, bool>> messageBoolActions = new Dictionary<string, Action<MessageOptions, bool>>
        {
            { "deprecated", (options, value) => options.Deprecated = value },
            { "map_entry", (options, value) => options.MapEntry = value },
            { "message_set_wire_format", (options, value) => options.MessageSetWireFormat = value },
            { "no_standard_descriptor_accessor", (options, value) => options.NoStandardDescriptorAccessor = value },
        };

        private static readonly Dictionary<string, Action<FieldOptions, bool>> fieldBoolActions = new Dictionary<string, Action<FieldOptions, bool>>
        {
            { "deprecated", (options, value) => options.Deprecated = value },
            { "lazy", (options, value) => options.Lazy = value },
            { "packed", (options, value) => options.Packed = value },
            { "weak", (options, value) => options.Weak = value },
        };

#pragma warning restore 0618, 0612
        private static FileOptions ParseFileOptions(ParserContext ctx, FileOptions options)
        {
            var tokens = ctx.Tokens;
            tokens.Consume(TokenType.AlphaNumeric, "option");
            var key = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");

            if (options == null) options = new FileOptions();
            if (fileBoolActions.TryGetValue(key, out var boolAction))
            {
                boolAction(options, tokens.ConsumeBoolean());
            }
            else if (fileStringActions.TryGetValue(key, out var stringAction))
            {
                stringAction(options, tokens.ConsumeString());
            }
            else if (key == "optimize_for")
            {
                options.OptimizeFor = tokens.ConsumeEnum<FileOptions.OptimizeMode>();
            }
            else
            {
                // drop it on the floor
                tokens.ConsumeString();
            }
            tokens.Consume(TokenType.Symbol, ";");
            return options;
        }
        private static void ParseFieldOptions(ParserContext ctx, FieldDescriptorProto field)
        {
            var tokens = ctx.Tokens;
            tokens.Consume(TokenType.Symbol, "[");
            var options = field.Options;
            while (true)
            {
                var token = tokens.Read();
                if (token.Is(TokenType.Symbol, "]"))
                {
                    tokens.Consume();
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

                    if (key == "default")
                    {
                        field.DefaultValue = tokens.ConsumeString();
                    }
                    else
                    {
                        if (options == null) options = new FieldOptions();
                        if (fieldBoolActions.TryGetValue(key, out var action))
                        {
                            action(options, tokens.ConsumeBoolean());
                        }
                        else if (key == "ctype")
                        {
                            options.Ctype = tokens.ConsumeEnum<FieldOptions.CType>();
                        }
                        else if (key == "jstype")
                        {
                            options.Jstype = tokens.ConsumeEnum<FieldOptions.JSType>();
                        }
                        else
                        {
                            // drop it on the floor
                            tokens.ConsumeString();
                        }

                        if (key == "packed" && options.Packed && !CanPack(field.type))
                        {
                            token.SyntaxError($"Field of type {field.type} cannot be packed");
                        }
                    }
                }
            }
            field.Options = options;
        }

        public static EnumDescriptorProto ParseEnumDescriptorProto(ParserContext ctx)
        {
            var tokens = ctx.Tokens;
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
                    try
                    {
                        obj.Values.Add(ParseEnumValueDescriptorProto(ctx));
                    }
                    catch (ParserException ex)
                    {
                        ctx.Errors.Add(ex);
                        tokens.SkipToEndStatement();
                    }
                }
            }
            return obj;
        }

        public static EnumValueDescriptorProto ParseEnumValueDescriptorProto(ParserContext ctx)
        {
            var tokens = ctx.Tokens;
            string name = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");
            var value = tokens.ConsumeInt32();
            tokens.Consume(TokenType.Symbol, ";");
            return new EnumValueDescriptorProto { Name = name, Number = value };
        }
    }
}
