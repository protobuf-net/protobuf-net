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
        internal List<Error> Errors { get; } = new List<Error>();
        public void Add(string name, System.IO.TextReader source = null)
        {
            if (!TryResolve(name, out var descriptor))
            {
                using (var reader = source ?? Open(name))
                {
                    descriptor = new FileDescriptorProto { Name = name };
                    Files.Add(descriptor);

                    descriptor.Parse(reader, Errors);
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
    partial class DescriptorProto : ISchemaObject
    {
        internal DescriptorProto Parent { get; set; }
        internal string FullyQualifiedName { get; set; }

        internal static bool TryParse(ParserContext ctx, out DescriptorProto obj)
        {
            var name = ctx.Tokens.Consume(TokenType.AlphaNumeric);
            if (ctx.TryReadObject(out obj))
            {
                obj.Name = name;
                return true;
            }
            return false;
        }
        void ISchemaObject.ReadOne(ParserContext ctx)
        {
            var tokens = ctx.Tokens;
            if (tokens.ConsumeIf(TokenType.AlphaNumeric, "message"))
            {
                if (DescriptorProto.TryParse(ctx, out var obj))
                    NestedTypes.Add(obj);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "enum"))
            {
                if (EnumDescriptorProto.TryParse(ctx, out var obj))
                    EnumTypes.Add(obj);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "option"))
            {
                Options = ctx.ParseOptionStatement(Options);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "reserved"))
            {
                ParseReservedRanges(ctx);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "extensions"))
            {
                ParseExtensionRange(ctx);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "extend"))
            {
                FieldDescriptorProto.ParseExtensions(ctx, Extensions);
            }
            else
            {
                if (FieldDescriptorProto.TryParse(ctx, this, out var obj))
                    Fields.Add(obj);
            }
        }
        private void ParseExtensionRange(ParserContext ctx)
        {
            ctx.AbortState = AbortState.Statement;
            var tokens = ctx.Tokens;
            tokens.Previous.RequireProto2(ctx);
            const int MAX = 536870911;
            while (true)
            {
                int from = tokens.ConsumeInt32(MAX), to = from;
                if (tokens.Read().Is(TokenType.AlphaNumeric, "to"))
                {
                    tokens.Consume();
                    to = tokens.ConsumeInt32(MAX);
                }
                ExtensionRanges.Add(new ExtensionRange { Start = from, End = to + 1 });

                if (tokens.ConsumeIf(TokenType.Symbol, ","))
                {
                    tokens.Consume();
                }
                else if (tokens.ConsumeIf(TokenType.Symbol, ";"))
                {
                    break;
                }
                else
                {
                    tokens.Read().Throw("unable to parse extension range");
                }
            }
            ctx.AbortState = AbortState.None;
        }



        private void ParseReservedRanges(ParserContext ctx)
        {
            ctx.AbortState = AbortState.Statement;
            var tokens = ctx.Tokens;
            var token = tokens.Read(); // test the first one to determine what we're doing
            switch (token.Type)
            {
                case TokenType.StringLiteral:
                    while (true)
                    {
                        ReservedNames.Add(tokens.Consume(TokenType.StringLiteral));
                        if (tokens.ConsumeIf(TokenType.Symbol, ","))
                        {
                        }
                        else if (tokens.ConsumeIf(TokenType.Symbol, ";"))
                        {
                            break;
                        }
                        else
                        {
                            tokens.Read().Throw("unable to parse reserved range");
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
                        ReservedRanges.Add(new ReservedRange { Start = from, End = to + 1 });
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
                            token.Throw();
                        }
                    }
                    break;
                default:
                    throw token.Throw();
            }
            ctx.AbortState = AbortState.None;
        }

    }

    partial class FileDescriptorProto : ISchemaObject
    {
        internal const string SyntaxProto2 = "proto2", SyntaxProto3 = "proto3";

        void ISchemaObject.ReadOne(ParserContext ctx)
        {
            var tokens = ctx.Tokens;
            if (tokens.ConsumeIf(TokenType.AlphaNumeric, "message"))
            {
                if (DescriptorProto.TryParse(ctx, out var obj))
                    MessageTypes.Add(obj);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "enum"))
            {
                if (EnumDescriptorProto.TryParse(ctx, out var obj))
                    EnumTypes.Add(obj);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "extend"))
            {
                FieldDescriptorProto.ParseExtensions(ctx, Extensions);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "service"))
            {
                if (ServiceDescriptorProto.TryParse(ctx, out var obj))
                    Services.Add(obj);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "syntax"))
            {
                ctx.AbortState = AbortState.Statement;
                if (MessageTypes.Any() || EnumTypes.Any() || Extensions.Any())
                {
                    ctx.Errors.Add(new Error(tokens.Previous, "syntax must be set before types are defined", true));
                }
                tokens.Consume(TokenType.Symbol, "=");
                Syntax = tokens.Consume(TokenType.StringLiteral);
                switch (Syntax)
                {
                    case SyntaxProto2:
                    case SyntaxProto3:
                        break;
                    default:
                        ctx.Errors.Add(new Error(tokens.Previous, $"unknown syntax '{Syntax}'", true));
                        break;
                }
                tokens.Consume(TokenType.Symbol, ";");
                ctx.AbortState = AbortState.None;
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "package"))
            {
                ctx.AbortState = AbortState.Statement;
                Package = tokens.Consume(TokenType.AlphaNumeric);
                ctx.AbortState = AbortState.None;
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "option"))
            {
                Options = ctx.ParseOptionStatement(Options);
            }
            else if(tokens.Peek(out var token))
            {
                token.Throw();
            } // else EOF
        }



        public void Parse(System.IO.TextReader schema, List<Error> errors)
        {
            Syntax = "";
            using (var ctx = new ParserContext(this, new Peekable<Token>(schema.Tokenize().RemoveCommentsAndWhitespace()), errors))
            {
                var tokens = ctx.Tokens;
                tokens.Peek(out Token startOfFile); // want this for "stuff you didn't do" warnings

                // read the file into the object
                ctx.Fill(this);

                // finish up
                FixupTypes(ctx);
                if (string.IsNullOrWhiteSpace(Syntax))
                {
                    ctx.Errors.Add(startOfFile.Error("no syntax specified; it is strongly recommended to specify 'syntax=\"proto2\";' or 'syntax=\"proto3\";'", false));
                }
                if (Syntax == SyntaxProto2)
                {
                    Syntax = ""; // for output compatibility; is blank even if set to proto2 explicitly
                }
            }
        }


        internal bool TryResolveEnum(string type, DescriptorProto parent, out EnumDescriptorProto @enum, out string fqn)
        {
            while (parent != null)
            {
                @enum = parent.EnumTypes.FirstOrDefault(x => x.Name == type);
                if (@enum != null)
                {
                    fqn = @enum.FullyQualifiedName;
                    return true;
                }
                parent = parent.Parent;
            }
            @enum = EnumTypes.FirstOrDefault(x => x.Name == type);
            if (@enum != null)
            {
                fqn = @enum.FullyQualifiedName;
                return true;
            }
            fqn = null;
            return false;
        }

        internal bool TryResolveMessage(string typeName, DescriptorProto parent, out DescriptorProto message, out string fqn)
        {
            int split = typeName.IndexOf('.');
            if (split > 0)
            {
                // compound name; try to resolve the first part
                var left = typeName.Substring(0, split).Trim();

                if (TryResolveMessage(left, parent, out message, out fqn))
                {
                    parent = message;
                    var right = typeName.Substring(split + 1).Trim();
                    return TryResolveMessage(right, parent, out message, out fqn);
                }
                return false;
            }

            // simple name
            while (parent != null)
            {
                message = parent.NestedTypes.FirstOrDefault(x => x.Name == typeName);
                if (message != null)
                {
                    fqn = message.FullyQualifiedName;
                    return true;
                }
                parent = parent.Parent;
            }
            message = MessageTypes.FirstOrDefault(x => x.Name == typeName);
            if (message != null)
            {
                fqn = message.FullyQualifiedName;
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
        internal void FixupTypes(ParserContext ctx)
        {
            // build the tree starting at the root
            var prefix = string.IsNullOrWhiteSpace(Package) ? "" : ("." + Package);
            foreach (var type in EnumTypes)
            {
                SetParents(prefix, type);
            }
            foreach (var type in MessageTypes)
            {
                SetParents(prefix, type);
            }

            // now resolve everything
            foreach (var type in MessageTypes)
            {
                ResolveFieldTypes(ctx, type);
            }
            foreach (var service in Services)
            {
                ResolveFieldTypes(ctx, service);
            }
            ResolveFieldTypes(ctx, Extensions, null);
        }

        static bool ShouldResolveType(FieldDescriptorProto.Type type)
        {
            switch(type)
            {
                case 0:
                case FieldDescriptorProto.Type.TypeMessage:
                case FieldDescriptorProto.Type.TypeEnum:
                case FieldDescriptorProto.Type.TypeGroup:
                    return true;
                default:
                    return false;
            }
        }
        private void ResolveFieldTypes(ParserContext ctx, List<FieldDescriptorProto> extensions, DescriptorProto parent)
        {
            foreach (var field in extensions)
            {
                if (!string.IsNullOrEmpty(field.TypeName) && ShouldResolveType(field.type))
                {
                    string fqn;
                    if (TryResolveMessage(field.TypeName, parent, out var msg, out fqn))
                    {
                        if (field.type != FieldDescriptorProto.Type.TypeGroup)
                        {
                            field.type = FieldDescriptorProto.Type.TypeMessage;
                        }
                    }
                    else if (TryResolveEnum(field.TypeName, parent, out var @enum, out fqn))
                    {
                        field.type = FieldDescriptorProto.Type.TypeEnum;
                    }
                    else
                    {
                        ctx.Errors.Add(field.TypeToken.TypeNotFound(field.TypeName));
                        fqn = field.TypeName;
                    }
                    field.TypeName = fqn;
                }

                if (!string.IsNullOrEmpty(field.Extendee))
                {
                    string fqn;
                    if (!TryResolveMessage(field.Extendee, parent, out var msg, out fqn))
                    {
                        ctx.Errors.Add(field.TypeToken.TypeNotFound(field.Extendee));
                        fqn = field.Extendee;
                    }
                    field.Extendee = fqn;
                }
            }
        }

        private void ResolveFieldTypes(ParserContext ctx, ServiceDescriptorProto service)
        {
            foreach (var method in service.Methods)
            {
                if (!TryResolveMessage(method.InputType, null, out var msg, out string fqn))
                {
                    ctx.Errors.Add(method.InputTypeToken.TypeNotFound(method.InputType));
                }
                method.InputType = fqn;
                if (!TryResolveMessage(method.OutputType, null, out msg, out fqn))
                {
                    ctx.Errors.Add(method.OutputTypeToken.TypeNotFound(method.OutputType));
                }
                method.OutputType = fqn;
            }
        }

        private void ResolveFieldTypes(ParserContext ctx, DescriptorProto type)
        {
            ResolveFieldTypes(ctx, type.Fields, type);
            ResolveFieldTypes(ctx, type.Extensions, type);
        }

    }
    partial class EnumDescriptorProto : ISchemaObject
    {
        internal DescriptorProto Parent { get; set; }
        internal string FullyQualifiedName { get; set; }

        internal static bool TryParse(ParserContext ctx, out EnumDescriptorProto obj)
        {
            var name = ctx.Tokens.Consume(TokenType.AlphaNumeric);
            if (ctx.TryReadObject(out obj))
            {
                obj.Name = name;
                return true;
            }
            return false;
        }

        void ISchemaObject.ReadOne(ParserContext ctx)
        {
            ctx.AbortState = AbortState.Statement;
            var tokens = ctx.Tokens;
            if (tokens.ConsumeIf(TokenType.AlphaNumeric, "option"))
            {
                Options = ctx.ParseOptionStatement(Options);
            }
            else
            {
                Values.Add(EnumValueDescriptorProto.Parse(ctx));
            }
            ctx.AbortState = AbortState.None;
        }

    }
    partial class FieldDescriptorProto
    {
        internal DescriptorProto Parent { get; set; }
        internal Token TypeToken { get; set; }

        internal static bool TryParse(ParserContext ctx, DescriptorProto parent, out FieldDescriptorProto field)
        {
            var tokens = ctx.Tokens;
            ctx.AbortState = AbortState.Statement;
            Label label = Label.LabelOptional; // default

            if (tokens.ConsumeIf(TokenType.AlphaNumeric, "repeated"))
            {
                label = Label.LabelRepeated;
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "required"))
            {
                tokens.Previous.RequireProto2(ctx);
                label = Label.LabelRequired;
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "optional"))
            {
                tokens.Previous.RequireProto2(ctx);
                label = Label.LabelOptional;
            }
            else if(ctx.Syntax == FileDescriptorProto.SyntaxProto2)
            {
                // required in proto2
                throw tokens.Read().Throw("expected 'repeated' / 'required' / 'optional'");
            }



            var typeToken = tokens.Read();
            string typeName = tokens.Consume(TokenType.AlphaNumeric);

            var isGroup = typeName == "group";
            if (isGroup)
            {
                ctx.AbortState = AbortState.Object;
            }

            string name = tokens.Consume(TokenType.AlphaNumeric);
            var nameToken = tokens.Previous;
            tokens.Consume(TokenType.Symbol, "=");
            var number = tokens.ConsumeInt32();

            Type type;
            if (isGroup)
            {
                type = Type.TypeGroup;
                typeName = name;

                typeToken.RequireProto2(ctx);
                
                var firstChar = typeName[0].ToString();
                if(firstChar.ToLowerInvariant() == firstChar)
                {
                    ctx.Errors.Add(new Error(nameToken, "group names must start with an upper-case letter", true));
                }
                name = typeName.ToLowerInvariant();
                if(ctx.TryReadObject<DescriptorProto>(out var grpType))
                {
                    grpType.Name = typeName;
                    parent.NestedTypes.Add(grpType);
                }
            }
            else if (TryIdentifyType(typeName, out type))
            {
                typeName = "";
            }

            field = new FieldDescriptorProto
            {
                type = type,
                TypeName = typeName,
                Name = name,
                JsonName = GetJsonName(name),
                Number = number,
                label = label,
                TypeToken = typeToken // internal property that helps give useful error messages
            };

            if(!isGroup)
            {
                if (ctx.Syntax != FileDescriptorProto.SyntaxProto2)
                {
                    if (CanPack(type)) // packed by default
                    {
                        var opt = field.Options ?? (field.Options = new FieldOptions());
                        opt.Packed = true;
                    }
                }
                if (tokens.ConsumeIf(TokenType.Symbol, "["))
                {
                    field.Options = ctx.ParseOptionBlock(field.Options, field);

                    if ((field.Options?.Packed ?? false) && !CanPack(type))
                    {
                        ctx.Errors.Add(new Error(typeToken, $"field of type {field.type} cannot be packed", true));
                        field.Options.Packed = false;
                    }
                }

                tokens.Consume(TokenType.Symbol, ";");
            }
            ctx.AbortState = AbortState.None;
            return true;
        }

        private static string GetJsonName(string name)
            => Regex.Replace(name, "_([a-zA-Z])", match => match.Groups[1].Value.ToUpperInvariant());


        internal static bool CanPack(Type type)
        {
            switch (type)
            {
                case Type.TypeBool:
                case Type.TypeDouble:
                case Type.TypeEnum:
                case Type.TypeFixed32:
                case Type.TypeFixed64:
                case Type.TypeFloat:
                case Type.TypeInt32:
                case Type.TypeInt64:
                case Type.TypeSfixed32:
                case Type.TypeSfixed64:
                case Type.TypeSint32:
                case Type.TypeSint64:
                case Type.TypeUint32:
                case Type.TypeUint64:
                    return true;
                default:
                    return false;
            }
        }
        private static bool TryIdentifyType(string typeName, out Type type)
        {
            bool Assign(Type @in, out Type @out)
            {
                @out = @in;
                return true;
            }
            switch (typeName)
            {
                case "bool": return Assign(Type.TypeBool, out @type);
                case "bytes": return Assign(Type.TypeBytes, out @type);
                case "double": return Assign(Type.TypeDouble, out @type);
                case "fixed32": return Assign(Type.TypeFixed32, out @type);
                case "fixed64": return Assign(Type.TypeFixed64, out @type);
                case "float": return Assign(Type.TypeFloat, out @type);
                case "int32": return Assign(Type.TypeInt32, out @type);
                case "int64": return Assign(Type.TypeInt64, out @type);
                case "sfixed32": return Assign(Type.TypeSfixed32, out @type);
                case "sfixed64": return Assign(Type.TypeSfixed64, out @type);
                case "sint32": return Assign(Type.TypeSint32, out @type);
                case "sint64": return Assign(Type.TypeSint64, out @type);
                case "string": return Assign(Type.TypeString, out @type);
                case "uint32": return Assign(Type.TypeUint32, out @type);
                case "uint64": return Assign(Type.TypeUint64, out @type);
                default:
                    type = default(FieldDescriptorProto.Type);
                    return false;
            }
        }

        internal static void ParseExtensions(ParserContext ctx, List<FieldDescriptorProto> extensions)
        {
            // lazy; should improve this!
            if(DescriptorProto.TryParse(ctx, out var obj))
            {
                foreach(var field in obj.Fields)
                {
                    field.Extendee = obj.Name;
                }
                extensions.AddRange(obj.Fields);
            }
        }
    }

    partial class ServiceDescriptorProto : ISchemaObject
    {
        internal static bool TryParse(ParserContext ctx, out ServiceDescriptorProto obj)
        {
            var name = ctx.Tokens.Consume(TokenType.AlphaNumeric);
            if (ctx.TryReadObject(out obj))
            {
                obj.Name = name;
                return true;
            }
            return false;
        }
        void ISchemaObject.ReadOne(ParserContext ctx)
        {
            ctx.AbortState = AbortState.Statement;
            var tokens = ctx.Tokens;

            if (tokens.ConsumeIf(TokenType.AlphaNumeric, "option"))
            {
                Options = ctx.ParseOptionStatement(Options);
            }
            else
            {
                // is a method
                Methods.Add(MethodDescriptorProto.Parse(ctx));
            }
            ctx.AbortState = AbortState.None;
        }
    }

    partial class MethodDescriptorProto : ISchemaObject
    {
        internal Token InputTypeToken { get; set; }
        internal Token OutputTypeToken { get; set; }

        internal static MethodDescriptorProto Parse(ParserContext ctx)
        {
            var tokens = ctx.Tokens;
            tokens.Consume(TokenType.AlphaNumeric, "rpc");
            var name = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "(");
            var inputTypeToken = tokens.Read();
            var inputType = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, ")");
            tokens.Consume(TokenType.AlphaNumeric, "returns");
            tokens.Consume(TokenType.Symbol, "(");
            var outputTypeToken = tokens.Read();
            var outputType = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, ")");

            var method = new MethodDescriptorProto
            {
                Name = name,
                InputType = inputType,
                OutputType = outputType,
                InputTypeToken = inputTypeToken,
                OutputTypeToken = outputTypeToken
            };

            if (tokens.Peek(out var token) && token.Is(TokenType.Symbol, "{"))
            {
                ctx.AbortState = AbortState.Object;
                ctx.TryReadObjectImpl(ref method);
            }
            else
            {
                tokens.Consume(TokenType.Symbol, ";");
            }
            return method;
        }

        void ISchemaObject.ReadOne(ParserContext ctx)
        {
            ctx.Tokens.Consume(TokenType.AlphaNumeric, "option");
            Options = ctx.ParseOptionStatement(Options);
        }
    }

    partial class EnumValueDescriptorProto
    {
        internal static EnumValueDescriptorProto Parse(ParserContext ctx)
        {
            var tokens = ctx.Tokens;
            string name = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");
            var value = tokens.ConsumeInt32();

            var obj = new EnumValueDescriptorProto { Name = name, Number = value };
            if (tokens.ConsumeIf(TokenType.Symbol, "["))
            {
                obj.Options = ctx.ParseOptionBlock(obj.Options);
            }
            tokens.Consume(TokenType.Symbol, ";");
            return obj;
        }
        internal EnumDescriptorProto Parent { get; set; }
        // technically optional, but need value even for zero
        public bool ShouldSerializeNumber() => true;
    }
    partial class MessageOptions : ISchemaOptions
    {
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key)
        {
            switch (key)
            {
                case "map_entry": MapEntry = ctx.Tokens.ConsumeBoolean(); return true;
                case "message_set_wire_format": MessageSetWireFormat = ctx.Tokens.ConsumeBoolean(); return true;
                case "no_standard_descriptor_accessor": NoStandardDescriptorAccessor = ctx.Tokens.ConsumeBoolean(); return true;
                default: return false;
            }
        }
    }
    partial class MethodOptions : ISchemaOptions
    {
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key)
        {
            switch (key)
            {
                case "idempotency_level": idempotency_level = ctx.Tokens.ConsumeEnum<IdempotencyLevel>(); return true;
                default: return false;
            }
        }
    }
    partial class ServiceOptions : ISchemaOptions
    {
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key) => false;
    }
    partial class EnumOptions : ISchemaOptions
    {
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key)
        {
            switch (key)
            {
                case "allow_alias": AllowAlias = ctx.Tokens.ConsumeBoolean(); return true;
                default: return false;
            }
        }
    }
    partial class EnumValueOptions : ISchemaOptions
    {
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key) => false;

    }
    partial class FieldOptions : ISchemaOptions
    {
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key)
        {
            switch (key)
            {
                case "jstype": Jstype = ctx.Tokens.ConsumeEnum<JSType>(); return true;
                case "ctype": Ctype = ctx.Tokens.ConsumeEnum<CType>(); return true;
                case "lazy": Lazy = ctx.Tokens.ConsumeBoolean(); return true;
                case "packed": Packed = ctx.Tokens.ConsumeBoolean(); return true;
                case "weak": Weak = ctx.Tokens.ConsumeBoolean(); return true;
                default: return false;
            }
        }
    }
    partial class FileOptions : ISchemaOptions
    {
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key)
        {
            switch (key)
            {
                case "optimize_for": OptimizeFor = ctx.Tokens.ConsumeEnum<OptimizeMode>(); return true;
                case "cc_enable_arenas": CcEnableArenas = ctx.Tokens.ConsumeBoolean(); return true;
                case "cc_generic_services": CcGenericServices = ctx.Tokens.ConsumeBoolean(); return true;
#pragma warning disable 0612
                case "java_generate_equals_and_hash": JavaGenerateEqualsAndHash = ctx.Tokens.ConsumeBoolean(); return true;
#pragma warning restore 0612
                case "java_generic_services": JavaGenericServices = ctx.Tokens.ConsumeBoolean(); return true;
                case "java_multiple_files": JavaMultipleFiles = ctx.Tokens.ConsumeBoolean(); return true;
                case "java_string_check_utf8": JavaStringCheckUtf8 = ctx.Tokens.ConsumeBoolean(); return true;
                case "py_generic_services": PyGenericServices = ctx.Tokens.ConsumeBoolean(); return true;

                case "csharp_namespace": CsharpNamespace = ctx.Tokens.ConsumeString(); return true;
                case "go_package": GoPackage = ctx.Tokens.ConsumeString(); return true;
                case "java_outer_classname": JavaOuterClassname = ctx.Tokens.ConsumeString(); return true;
                case "java_package": JavaPackage = ctx.Tokens.ConsumeString(); return true;
                case "objc_class_prefix": ObjcClassPrefix = ctx.Tokens.ConsumeString(); return true;
                case "php_class_prefix": PhpClassPrefix = ctx.Tokens.ConsumeString(); return true;
                case "swift_prefix": SwiftPrefix = ctx.Tokens.ConsumeString(); return true;

                default: return false;
            }
        }
    }
    partial class OneofOptions
    {

    }

#pragma warning restore CS1591
}
namespace ProtoBuf
{
    public class Error
    {
        internal string ToString(bool includeType) => Text.Length == 0
                ? $"({LineNumber},{ColumnNumber}): {(includeType ? (IsError ? "error: " : "warning: ") : "")}{Message}"
                : $"({LineNumber},{ColumnNumber},{LineNumber},{ColumnNumber + Text.Length}): {(includeType ? (IsError ? "error: " : "warning: ") : "")}{Message}";
        public override string ToString() => ToString(true);
            
        internal static Error[] GetArray(List<Error> errors)
            => errors.Count == 0 ? noErrors : errors.ToArray();

        private static readonly Error[] noErrors = new Error[0];

        internal Error(Token token, string message, bool isError)
        {
            ColumnNumber = token.ColumnNumber;
            LineNumber = token.LineNumber;
            LineContents = token.LineContents;
            Message = message;
            IsError = isError;
            Text = token.Value;
        }
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
    enum AbortState
    {
        None, Statement, Object
    }
    interface ISchemaOptions
    {
        List<UninterpretedOption> UninterpretedOptions { get; }
        bool Deprecated { get; set; }
        bool ReadOne(ParserContext ctx, string key);
    }
    interface ISchemaObject
    {
        void ReadOne(ParserContext ctx);
    }
    internal class ParserContext : IDisposable
    {
        public AbortState AbortState { get; set; }
        private void ReadOne<T>(T obj) where T : class, ISchemaObject
        {
            AbortState oldState = AbortState;
            AbortState = AbortState.None;
            if (!Tokens.Peek(out var stateBefore)) return;
            
            try
            {
                obj.ReadOne(this);
            }
            catch (ParserException ex)
            {
                Errors.Add(new Error(ex));
            }
            finally
            {
                var state = AbortState;
                if(Tokens.Peek(out var stateAfter) && stateBefore == stateAfter)
                {
                    // we didn't move! avoid looping forever failing to do the same thing
                    Errors.Add(stateAfter.Error());
                    state = stateAfter.Is(TokenType.Symbol, "}")
                        ? AbortState.Object : AbortState.Statement;
                }
                AbortState = oldState;
                switch (state)
                {
                    case AbortState.Object:
                        Tokens.SkipToEndObject();
                        break;
                    case AbortState.Statement:
                        Tokens.SkipToEndStatement();
                        break;
                }
            }
        }
        public void Fill<T>(T obj) where T : class, ISchemaObject
        {
            var tokens = Tokens;
            while (tokens.Peek(out var token))
            {
                if (tokens.ConsumeIf(TokenType.Symbol, ";"))
                { }
                else
                {
                    ReadOne(obj);
                }
            }
        }
        private void ReadOption<T>(ref T obj, object parent) where T : class, ISchemaOptions, new()
        {
            var tokens = Tokens;
            string key = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, "=");

            if (key == "default" && typeof(T) == typeof(FieldOptions)
                && parent is FieldDescriptorProto)
            {
                // special-case this; for compatibility with protoc,
                // we need obj to stay null if *just* default set
                ((FieldDescriptorProto)parent).DefaultValue = tokens.ConsumeString();
            }
            else {
                if (obj == null) obj = new T();
                if (key == "deprecated")
                {
                    obj.Deprecated = tokens.ConsumeBoolean();
                }
                else if (!obj.ReadOne(this, key))
                {
                    //TODO: something with uninterpreted options
                    tokens.Consume(); // drop it on the floor
                }
            }
        }
        public T ParseOptionBlock<T>(T obj, object parent = null) where T : class, ISchemaOptions, new()
        {
            var tokens = Tokens;
            try
            {
                while (true)
                {
                    if (tokens.ConsumeIf(TokenType.Symbol, "]"))
                    {
                        break;
                    }
                    else if (tokens.ConsumeIf(TokenType.Symbol, ","))
                    {
                    }
                    else
                    {
                        ReadOption(ref obj, parent);
                    }
                }
            }
            catch (ParserException ex)
            {
                Errors.Add(new Error(ex));
                tokens.SkipToEndOptions();
            }
            return obj;
        }
        public T ParseOptionStatement<T>(T obj, object parent = null) where T : class, ISchemaOptions, new()
        {
            var tokens = Tokens;
            try
            {
                ReadOption(ref obj, parent);
                tokens.Consume(TokenType.Symbol, ";");
            }
            catch (ParserException ex)
            {
                Errors.Add(new Error(ex));
                tokens.SkipToEndStatement();
            }
            return obj;
        }
        public bool TryReadObject<T>(out T obj) where T : class, ISchemaObject, new()
        {
            obj = null;
            return TryReadObjectImpl(ref obj);
        }
        internal bool TryReadObjectImpl<T>(ref T obj) where T : class, ISchemaObject, new()
        {
            var tokens = Tokens;

            try
            {
                tokens.Consume(TokenType.Symbol, "{");
                if(obj == null) obj = new T();
                while (tokens.Peek(out var token) && !token.Is(TokenType.Symbol, "}"))
                {
                    if (tokens.ConsumeIf(TokenType.Symbol, ";"))
                    { }
                    else
                    {
                        ReadOne(obj);
                    }
                }
                tokens.Consume(TokenType.Symbol, "}");
                return true;
            }
            catch (ParserException ex)
            {
                Errors.Add(new Error(ex));
                tokens.SkipToEndObject();
            }
            obj = null;
            return false;
        }
        public ParserContext(FileDescriptorProto file, Peekable<Token> tokens, List<Error> errors)
        {
            Tokens = tokens;
            Errors = errors;
            _file = file;
        }

        public string Syntax
        {
            get
            {
                var syntax = _file.Syntax;
                return string.IsNullOrEmpty(syntax) ? FileDescriptorProto.SyntaxProto2 : syntax;
            }
        }

        private readonly FileDescriptorProto _file;
        public Peekable<Token> Tokens { get; }
        public List<Error> Errors { get; }

        public void Dispose() { Tokens.Dispose(); }
    }
}
