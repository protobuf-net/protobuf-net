using Google.Protobuf.Reflection;
using ProtoBuf;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.IO;

namespace Google.Protobuf.Reflection
{
#pragma warning disable CS1591

    interface IType
    {
        IType Parent { get; }
        string FullyQualifiedName { get; }

        IType Find(string name);
    }
    partial class FileDescriptorSet
    {
        public Func<string, bool> ImportValidator { get; set; }

        internal List<string> importPaths = new List<string>();
        public void AddImportPath(string path)
        {
            importPaths.Add(path);
        }
        public Error[] GetErrors() => Error.GetArray(Errors);
        internal List<Error> Errors { get; } = new List<Error>();

#if !NO_IO
        public bool Add(string name, bool includeInOutput, System.IO.TextReader source = null)
            => Add(name, source == null ? null : new TextReaderLineReader(source), includeInOutput);
#endif
        internal bool Add(string name, LineReader source, bool includeInOutput)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            if (Path.IsPathRooted(name) || name.Contains(".."))
                throw new ArgumentException("Paths should be relative to the import paths, not rooted", nameof(name));
            if (TryResolve(name, out var descriptor))
            {
                if (includeInOutput) descriptor.IncludeInOutput = true;
                return true; // already exists, that counts as success
            }

            using (var reader = source ?? Open(name))
            {
                if (reader == null) return false; // not found

                descriptor = new FileDescriptorProto
                {
                    Name = name,
                    IncludeInOutput = includeInOutput
                };
                Files.Add(descriptor);

                descriptor.Parse(reader, Errors, name);
                return true;
            }
        }
#if NO_IO
        private LineReader Open(string name) => null;
#else
        private LineReader Open(string name)
        {
            var found = FindFile(name);
            if (found == null) return null;
            return new TextReaderLineReader(new StreamReader(File.OpenRead(found)));
        }
        string FindFile(string file)
        {
            foreach (var path in importPaths)
            {
                var rel = Path.Combine(path, file);
                if (File.Exists(rel)) return rel;
            }
            return null;
        }

#endif

        bool TryResolve(string name, out FileDescriptorProto descriptor)
        {
            descriptor = Files.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return descriptor != null;
        }

        private void ApplyImports()
        {
            bool didSomething;
            do
            {
                didSomething = false;
                var file = Files.FirstOrDefault(x => x.HasPendingImports);
                if (file != null)
                {
                    // note that GetImports clears the flag
                    foreach (var import in file.GetImports())
                    {
                        if (!(ImportValidator?.Invoke(import.Path) ?? true))
                        {
                            Errors.Error(import.Token, $"import of {import.Path} is disallowed");
                        }
                        else if (Add(import.Path, false))
                        {
                            didSomething = true;
                        }
                        else
                        {
                            Errors.Error(import.Token, $"unable to find: '{import.Path}'");
                        }
                    }
                }
            } while (didSomething);
        }

        public void Process()
        {
            ApplyImports();
            foreach (var file in Files)
            {
                using (var ctx = new ParserContext(file, null, Errors))
                {
                    file.BuildTypeHierarchy(this, ctx);
                }
            }
            foreach (var file in Files)
            {
                using (var ctx = new ParserContext(file, null, Errors))
                {
                    file.ResolveTypes(ctx);
                }
            }

            Files.RemoveAll(x => !x.IncludeInOutput);
        }

        internal FileDescriptorProto GetFile(string path)
            // try full match first, then name-only match
            => Files.FirstOrDefault(x => string.Equals(x.Name, path, StringComparison.OrdinalIgnoreCase));
    }
    partial class DescriptorProto : ISchemaObject, IHazNames, IType
    {
        public override string ToString() => Name;
        internal IType Parent { get; set; }
        IType IType.Parent => Parent;
        string IType.FullyQualifiedName => FullyQualifiedName;
        IType IType.Find(string name)
        {
            return (IType)NestedTypes.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? (IType)EnumTypes.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        internal string FullyQualifiedName { get; set; }

        internal static bool TryParse(ParserContext ctx, IHazNames parent, out DescriptorProto obj)
        {
            var name = ctx.Tokens.Consume(TokenType.AlphaNumeric);
            ctx.CheckNames(parent, name, ctx.Tokens.Previous);
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
                if (DescriptorProto.TryParse(ctx, this, out var obj))
                {
                    NestedTypes.Add(obj);
                }
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "enum"))
            {
                if (EnumDescriptorProto.TryParse(ctx, this, out var obj))
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
                FieldDescriptorProto.ParseExtensions(ctx, this, Extensions);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "oneof"))
            {
                OneofDescriptorProto.Parse(ctx, this);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "map"))
            {
                ParseMap(ctx);
            }
            else
            {
                if (FieldDescriptorProto.TryParse(ctx, this, false, out var obj))
                    Fields.Add(obj);
            }
        }

        private void ParseMap(ParserContext ctx)
        {
            ctx.AbortState = AbortState.Statement;
            var tokens = ctx.Tokens;
            tokens.Consume(TokenType.Symbol, "<");
            var keyName = tokens.Consume(TokenType.AlphaNumeric);
            var keyToken = tokens.Previous;
            if (FieldDescriptorProto.TryIdentifyType(keyName, out var keyType))
            {
                keyName = null;
            }
            switch (keyType)
            {
                case 0:
                case FieldDescriptorProto.Type.TypeBytes:
                case FieldDescriptorProto.Type.TypeMessage:
                case FieldDescriptorProto.Type.TypeGroup:
                case FieldDescriptorProto.Type.TypeFloat:
                case FieldDescriptorProto.Type.TypeDouble:
                    ctx.Errors.Error(tokens.Previous, "invalid map key type (only integral and string types are allowed)");
                    break;
            }
            tokens.Consume(TokenType.Symbol, ",");
            var valueName = tokens.Consume(TokenType.AlphaNumeric);
            var valueToken = tokens.Previous;
            if (FieldDescriptorProto.TryIdentifyType(valueName, out var valueType))
            {
                valueName = null;
            }
            tokens.Consume(TokenType.Symbol, ">");

            var name = tokens.Consume(TokenType.AlphaNumeric);
            var nameToken = tokens.Previous;
            ctx.CheckNames(this, name, nameToken);

            tokens.Consume(TokenType.Symbol, "=");
            int number = tokens.ConsumeInt32();

            var jsonName = FieldDescriptorProto.GetJsonName(name);
            var typeName = jsonName.Substring(0, 1).ToUpperInvariant() + jsonName.Substring(1) + "Entry";
            ctx.CheckNames(this, typeName, nameToken);

            var field = new FieldDescriptorProto
            {
                type = FieldDescriptorProto.Type.TypeMessage,
                TypeName = typeName,
                Name = name,
                JsonName = jsonName,
                Number = number,
                label = FieldDescriptorProto.Label.LabelRepeated,
                TypeToken = nameToken
            };

            if (tokens.ConsumeIf(TokenType.Symbol, "["))
            {
                field.Options = ctx.ParseOptionBlock(field.Options, field);
            }
            Fields.Add(field);

            var msgType = new DescriptorProto
            {
                Name = typeName,
                Fields =
                {
                    new FieldDescriptorProto
                    {
                        label = FieldDescriptorProto.Label.LabelOptional,
                        Name = "key",
                        JsonName = "key",
                        Number = 1,
                        type = keyType,
                        TypeName = keyName,
                        TypeToken = keyToken,
                    },
                    new FieldDescriptorProto
                    {
                        label = FieldDescriptorProto.Label.LabelOptional,
                        Name = "value",
                        JsonName = "value",
                        Number = 2,
                        type = valueType,
                        TypeName = valueName,
                        TypeToken = valueToken,
                    }
                }
            };
            if (msgType.Options == null) msgType.Options = new MessageOptions();
            msgType.Options.MapEntry = true;
            NestedTypes.Add(msgType);

            ctx.AbortState = AbortState.None;
        }

        private void ParseExtensionRange(ParserContext ctx)
        {
            ctx.AbortState = AbortState.Statement;
            var tokens = ctx.Tokens;
            tokens.Previous.RequireProto2(ctx);

            while (true)
            {
                int from = tokens.ConsumeInt32(FieldDescriptorProto.MaxField), to = from;
                if (tokens.Read().Is(TokenType.AlphaNumeric, "to"))
                {
                    tokens.Consume();
                    to = tokens.ConsumeInt32(FieldDescriptorProto.MaxField);
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
                        var name = tokens.Consume(TokenType.StringLiteral);
                        var conflict = Fields.FirstOrDefault(x => x.Name == name);
                        if (conflict != null)
                        {
                            ctx.Errors.Error(tokens.Previous, $"'{conflict.Name}' is already in use by field {conflict.Number}");
                        }
                        ReservedNames.Add(name);

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
                        var conflict = Fields.FirstOrDefault(x => x.Number >= from && x.Number <= to);
                        if (conflict != null)
                        {
                            ctx.Errors.Error(tokens.Previous, $"field {conflict.Number} is already in use by '{conflict.Name}'");
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

        IEnumerable<string> IHazNames.GetNames()
        {
            foreach (var field in Fields) yield return field.Name;
            foreach (var type in NestedTypes) yield return type.Name;
            foreach (var type in EnumTypes) yield return type.Name;
            foreach (var name in ReservedNames) yield return name;
        }
    }

    partial class OneofDescriptorProto : ISchemaObject
    {
        internal DescriptorProto Parent { get; set; }
        internal static void Parse(ParserContext ctx, DescriptorProto parent)
        {
            ctx.AbortState = AbortState.Object;
            var oneOf = new OneofDescriptorProto
            {
                Name = ctx.Tokens.Consume(TokenType.AlphaNumeric)
            };
            parent.OneofDecls.Add(oneOf);
            oneOf.Parent = parent;

            if (ctx.TryReadObjectImpl(oneOf))
            {
                ctx.AbortState = AbortState.None;
            }
        }
        void ISchemaObject.ReadOne(ParserContext ctx)
        {
            var tokens = ctx.Tokens;
            if (tokens.ConsumeIf(TokenType.AlphaNumeric, "option"))
            {
                Options = ctx.ParseOptionStatement(Options);
            }
            else
            {
                if (FieldDescriptorProto.TryParse(ctx, Parent, true, out var field))
                {
                    field.OneofIndex = Parent.OneofDecls.Count() - 1;
                    Parent.Fields.Add(field);
                }
            }
        }
    }
    partial class OneofOptions : ISchemaOptions
    {
        bool ISchemaOptions.Deprecated { get { return false; } set { } }
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key) => false;
    }
    partial class FileDescriptorProto : ISchemaObject, IHazNames, IType
    {
        public override string ToString() => Name;

        string IType.FullyQualifiedName => null;
        IType IType.Parent => null;
        IType IType.Find(string name)
        {
            return (IType)MessageTypes.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? (IType)EnumTypes.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        internal bool HasPendingImports { get; private set; }
        internal FileDescriptorSet Parent { get; private set; }

        internal bool IncludeInOutput { get; set; }

        public bool HasImports() => _imports.Count != 0;
        internal IEnumerable<Import> GetImports()
        {
            HasPendingImports = false;
            return _imports;
        }
        readonly List<Import> _imports = new List<Import>();
        internal bool AddImport(string path, bool isPublic, Token token)
        {
            var existing = _imports.FirstOrDefault(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                // we'll allow this to upgrade
                if (isPublic) existing.IsPublic = true;
                return false;
            }
            HasPendingImports = true;
            _imports.Add(new Import { Path = path, IsPublic = isPublic, Token = token });
            return true;
        }

        internal const string SyntaxProto2 = "proto2", SyntaxProto3 = "proto3";

        void ISchemaObject.ReadOne(ParserContext ctx)
        {
            var tokens = ctx.Tokens;
            if (tokens.ConsumeIf(TokenType.AlphaNumeric, "message"))
            {
                if (DescriptorProto.TryParse(ctx, this, out var obj))
                    MessageTypes.Add(obj);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "enum"))
            {
                if (EnumDescriptorProto.TryParse(ctx, this, out var obj))
                    EnumTypes.Add(obj);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "extend"))
            {
                FieldDescriptorProto.ParseExtensions(ctx, this, Extensions);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "service"))
            {
                if (ServiceDescriptorProto.TryParse(ctx, out var obj))
                    Services.Add(obj);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "import"))
            {
                ctx.AbortState = AbortState.Statement;
                bool isPublic = tokens.ConsumeIf(TokenType.AlphaNumeric, "public");
                string path = tokens.Consume(TokenType.StringLiteral);

                if (!AddImport(path, isPublic, tokens.Previous))
                {
                    ctx.Errors.Warn(tokens.Previous, $"duplicate import: '{path}'");
                }
                tokens.Consume(TokenType.Symbol, ";");
                ctx.AbortState = AbortState.None;


            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "syntax"))
            {
                ctx.AbortState = AbortState.Statement;
                if (MessageTypes.Any() || EnumTypes.Any() || Extensions.Any())
                {
                    ctx.Errors.Error(tokens.Previous, "syntax must be set before types are defined");
                }
                tokens.Consume(TokenType.Symbol, "=");
                Syntax = tokens.Consume(TokenType.StringLiteral);
                switch (Syntax)
                {
                    case SyntaxProto2:
                    case SyntaxProto3:
                        break;
                    default:
                        ctx.Errors.Error(tokens.Previous, $"unknown syntax '{Syntax}'");
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
            else if (tokens.Peek(out var token))
            {
                token.Throw();
            } // else EOF
        }
#if !NO_IO
        public void Parse(TextReader schema, List<Error> errors, string file)
            => Parse(new TextReaderLineReader(schema), errors, file);
#endif

        internal void Parse(LineReader schema, List<Error> errors, string file)
        {
            Syntax = "";
            using (var ctx = new ParserContext(this, new Peekable<Token>(schema.Tokenize(file).RemoveCommentsAndWhitespace()), errors))
            {
                var tokens = ctx.Tokens;
                tokens.Peek(out Token startOfFile); // want this for "stuff you didn't do" warnings

                // read the file into the object
                ctx.Fill(this);

                // finish up
                if (string.IsNullOrWhiteSpace(Syntax))
                {
                    ctx.Errors.Warn(startOfFile, "no syntax specified; it is strongly recommended to specify 'syntax=\"proto2\";' or 'syntax=\"proto3\";'");
                }
                if (Syntax == "" || Syntax == SyntaxProto2)
                {
                    Syntax = null; // for output compatibility; is blank even if set to proto2 explicitly
                }
            }
        }


        internal bool TryResolveEnum(string typeName, IType parent, out EnumDescriptorProto @enum, bool allowImports)
        {
            if (TryResolveType(typeName, parent, out var type, allowImports))
            {
                @enum = type as EnumDescriptorProto;
                return @enum != null;
            }
            @enum = null;
            return false;
        }
        internal bool TryResolveMessage(string typeName, IType parent, out DescriptorProto message, bool allowImports)
        {
            if (TryResolveType(typeName, parent, out var type, allowImports))
            {
                message = type as DescriptorProto;
                return message != null;
            }
            message = null;
            return false;
        }
        internal static bool TrySplit(string input, out string left, out string right)
        {
            var split = input.IndexOf('.');
            if (split < 0)
            {
                left = right = null;
                return false;
            }
            left = input.Substring(0, split).Trim();
            right = input.Substring(split + 1).Trim();
            return true;
        }
        internal bool TryResolveType(string typeName, IType parent, out IType type, bool allowImports, bool checkOwnPackage = true)
        {
            bool TryResolveFromFile(FileDescriptorProto file, string tn, bool ai, out IType tp)
            {
                tp = null;
                if (file == null) return false;

                var pkg = file.Package;
                if(!string.IsNullOrEmpty(pkg))
                {
                    if (!tn.StartsWith(pkg + ".")) return false; // wrong file

                    tn = tn.Substring(pkg.Length + 1);
                }

                return TryResolveType(tn, file, out tp, ai, false);

            }
            if (TrySplit(typeName, out var left, out var right))
            {
                // compound name; try to resolve the first part
                if (parent is FileDescriptorProto)
                {
                    var fdp = (FileDescriptorProto)parent;
                    if (!string.IsNullOrEmpty(fdp.Package))
                    {
                        // has a package name
                        if (fdp.Package != left)
                        {    // not the right package, or nothing more to the right
                            type = null;
                            return false;
                        }
                        var oldRight = right;
                        if (!TrySplit(right, out left, out right))
                        {
                            // simple name
                            type = parent.Find(oldRight);
                            if (type != null) return true;
                        }
                    }
                }
                
                while (parent != null)
                {
                    var next = parent?.Find(left);
                    if (next != null && TryResolveType(right, next, out type, false)) return true;

                    parent = parent.Parent;
                }

                if (checkOwnPackage && TryResolveFromFile(this, typeName, false, out type)) return true;

                // look at imports
                if (allowImports)
                {
                    foreach (var import in _imports)
                    {
                        var file = Parent?.GetFile(import.Path);
                        if (TryResolveFromFile(file, typeName, import.IsPublic, out type))
                        {
                            import.Used = true;
                            return true;
                        }
                    }
                }

                type = null;
                return false;
            }
            // simple name
            while (parent != null)
            {
                type = parent.Find(typeName);
                if (type != null)
                {
                    return true;
                }
                parent = parent.Parent;
            }

            // look at imports in the root namespace (so: immediate children)
            if (allowImports)
            {
                foreach (var import in _imports)
                {
                    var file = Parent?.GetFile(import.Path);
                    if (file == null) continue;

                    if (string.IsNullOrEmpty(file.Package))
                    {
                        type = ((IType)file).Find(typeName);
                        if (type != null)
                        {
                            import.Used = true;
                            return true;
                        }
                    }
                }
            }
            type = null;
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
        internal void BuildTypeHierarchy(FileDescriptorSet set, ParserContext ctx)
        {
            // build the tree starting at the root
            Parent = set;
            var prefix = string.IsNullOrWhiteSpace(Package) ? "" : ("." + Package);
            foreach (var type in EnumTypes)
            {
                type.Parent = this;
                SetParents(prefix, type);
            }
            foreach (var type in MessageTypes)
            {
                type.Parent = this;
                SetParents(prefix, type);
            }
        }

        static bool ShouldResolveType(FieldDescriptorProto.Type type)
        {
            switch (type)
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
        private void ResolveFieldTypes(ParserContext ctx, List<FieldDescriptorProto> extensions, IType parent)
        {
            foreach (var field in extensions)
            {
                if (!string.IsNullOrEmpty(field.TypeName) && ShouldResolveType(field.type))
                {
                    // TODO: use TryResolveType once rather than twice
                    string fqn;
                    if (TryResolveMessage(field.TypeName, parent, out var msg, true))
                    {
                        if (field.type != FieldDescriptorProto.Type.TypeGroup)
                        {
                            field.type = FieldDescriptorProto.Type.TypeMessage;
                        }
                        fqn = msg?.FullyQualifiedName;
                    }
                    else if (TryResolveEnum(field.TypeName, parent, out var @enum, true))
                    {
                        field.type = FieldDescriptorProto.Type.TypeEnum;
                        if (!string.IsNullOrWhiteSpace(field.DefaultValue)
                            & !@enum.Values.Any(x => x.Name == field.DefaultValue))
                        {
                            ctx.Errors.Error(field.TypeToken, $"enum {@enum.Name} does not contain value '{field.DefaultValue}'");
                        }
                        fqn = @enum?.FullyQualifiedName;
                    }
                    else
                    {
                        ctx.Errors.Add(field.TypeToken.TypeNotFound(field.TypeName));
                        fqn = field.TypeName;
                        field.type = FieldDescriptorProto.Type.TypeMessage; // just an assumption
                    }
                    field.TypeName = fqn;
                }

                if (!string.IsNullOrEmpty(field.Extendee))
                {
                    string fqn;
                    if (TryResolveMessage(field.Extendee, parent, out var msg, true))
                    {
                        fqn = msg?.FullyQualifiedName;
                    }
                    else
                    {
                        ctx.Errors.Add(field.TypeToken.TypeNotFound(field.Extendee));
                        fqn = field.Extendee;
                    }
                    field.Extendee = fqn;
                }

                bool canPack = FieldDescriptorProto.CanPack(field.type);
                if (ctx.Syntax != FileDescriptorProto.SyntaxProto2 && canPack
                    && field.label == FieldDescriptorProto.Label.LabelRepeated)
                {
                    // packed by default *if not explicitly specified*
                    var opt = field.Options ?? (field.Options = new FieldOptions());
                    if (!opt.ShouldSerializePacked())
                    {
                        opt.Packed = true;
                    }
                }

                if (field.Options?.Packed ?? false)
                {
                    if (!canPack)
                    {
                        ctx.Errors.Error(field.TypeToken, $"field of type {field.type} cannot be packed");
                        field.Options.Packed = false;
                    }
                }
            }
        }

        private void ResolveFieldTypes(ParserContext ctx, ServiceDescriptorProto service)
        {
            foreach (var method in service.Methods)
            {
                if (!TryResolveMessage(method.InputType, this, out var msg, true))
                {
                    ctx.Errors.Add(method.InputTypeToken.TypeNotFound(method.InputType));
                }
                method.InputType = msg?.FullyQualifiedName;
                if (!TryResolveMessage(method.OutputType, this, out msg, true))
                {
                    ctx.Errors.Add(method.OutputTypeToken.TypeNotFound(method.OutputType));
                }
                method.OutputType = msg?.FullyQualifiedName;
            }
        }

        private void ResolveFieldTypes(ParserContext ctx, DescriptorProto type)
        {
            ResolveFieldTypes(ctx, type.Fields, type);
            ResolveFieldTypes(ctx, type.Extensions, type);
            foreach (var nested in type.NestedTypes)
            {
                ResolveFieldTypes(ctx, nested);
            }
        }


        IEnumerable<string> IHazNames.GetNames()
        {
            foreach (var type in MessageTypes) yield return type.Name;
            foreach (var type in EnumTypes) yield return type.Name;
        }

        internal void ResolveTypes(ParserContext ctx)
        {
            foreach (var type in MessageTypes)
            {
                ResolveFieldTypes(ctx, type);
            }
            foreach (var service in Services)
            {
                ResolveFieldTypes(ctx, service);
            }
            ResolveFieldTypes(ctx, Extensions, this);

            foreach (var import in _imports)
            {
                if (import.Used)
                {
                    Dependencies.Add(import.Path);
                }
            }
        }
    }
    partial class EnumDescriptorProto : ISchemaObject, IType
    {
        public override string ToString() => Name;
        internal IType Parent { get; set; }
        string IType.FullyQualifiedName => FullyQualifiedName;
        IType IType.Parent => Parent;
        IType IType.Find(string name) => null;
        internal string FullyQualifiedName { get; set; }

        internal static bool TryParse(ParserContext ctx, IHazNames parent, out EnumDescriptorProto obj)
        {
            var name = ctx.Tokens.Consume(TokenType.AlphaNumeric);
            ctx.CheckNames(parent, name, ctx.Tokens.Previous);
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
        public override string ToString() => Name;
        internal const int MaxField = 536870911;
        internal const int FirstReservedField = 19000;
        internal const int LastReservedField = 19999;

        internal DescriptorProto Parent { get; set; }
        internal Token TypeToken { get; set; }

        internal static bool TryParse(ParserContext ctx, IHazNames parent, bool isOneOf, out FieldDescriptorProto field)
        {
            void NotAllowedOneOf(ParserContext context)
            {
                var token = ctx.Tokens.Previous;
                context.Errors.Error(token, $"'{token.Value}' not allowed with 'oneof'");
            }
            var tokens = ctx.Tokens;
            ctx.AbortState = AbortState.Statement;
            Label label = Label.LabelOptional; // default

            if (tokens.ConsumeIf(TokenType.AlphaNumeric, "repeated"))
            {
                if (isOneOf) NotAllowedOneOf(ctx);
                label = Label.LabelRepeated;
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "required"))
            {
                if (isOneOf) NotAllowedOneOf(ctx);
                else tokens.Previous.RequireProto2(ctx);
                label = Label.LabelRequired;
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "optional"))
            {
                if (isOneOf) NotAllowedOneOf(ctx);
                else tokens.Previous.RequireProto2(ctx);
                label = Label.LabelOptional;
            }
            else if (ctx.Syntax == FileDescriptorProto.SyntaxProto2 && !isOneOf)
            {
                // required in proto2
                throw tokens.Read().Throw("expected 'repeated' / 'required' / 'optional'");
            }

            var typeToken = tokens.Read();
            if (typeToken.Is(TokenType.AlphaNumeric, "map"))
            {
                tokens.Previous.Throw($"'{tokens.Previous.Value}' can not be used with 'map'");
            }
            string typeName = tokens.Consume(TokenType.AlphaNumeric);

            var parentTyped = parent as DescriptorProto;
            var isGroup = typeName == "group";
            if (isGroup)
            {
                if (isOneOf) NotAllowedOneOf(ctx);
                else if (parentTyped == null)
                {
                    ctx.Errors.Error(tokens.Previous, "group not allowed in this context");
                }
                ctx.AbortState = AbortState.Object;
            }

            string name = tokens.Consume(TokenType.AlphaNumeric);
            var nameToken = tokens.Previous;
            tokens.Consume(TokenType.Symbol, "=");
            var number = tokens.ConsumeInt32();
            var numberToken = tokens.Previous;

            if (number < 1 || number > MaxField)
            {
                ctx.Errors.Error(numberToken, $"field numbers must be in the range 1-{MaxField}");
            }
            else if (number >= FirstReservedField && number <= LastReservedField)
            {
                ctx.Errors.Warn(numberToken, $"field numbers in the range {FirstReservedField}-{LastReservedField} are reserved; this may cause problems on many implementations");
            }
            ctx.CheckNames(parent, name, nameToken);
            if (parentTyped != null)
            {

                var conflict = parentTyped.Fields.FirstOrDefault(x => x.Number == number);
                if (conflict != null)
                {
                    ctx.Errors.Error(numberToken, $"field {number} is already in use by '{conflict.Name}'");
                }
                if (parentTyped.ReservedNames.Contains(name))
                {
                    ctx.Errors.Error(nameToken, $"field '{name}' is reserved");
                }
                if (parentTyped.ReservedRanges.Any(x => x.Start <= number && x.End > number))
                {
                    ctx.Errors.Error(numberToken, $"field {number} is reserved");
                }
            }

            Type type;
            if (isGroup)
            {
                type = Type.TypeGroup;
                typeName = name;

                typeToken.RequireProto2(ctx);

                var firstChar = typeName[0].ToString();
                if (firstChar.ToLowerInvariant() == firstChar)
                {
                    ctx.Errors.Error(nameToken, "group names must start with an upper-case letter");
                }
                name = typeName.ToLowerInvariant();
                if (ctx.TryReadObject<DescriptorProto>(out var grpType))
                {
                    grpType.Name = typeName;
                    ctx.CheckNames(parent, typeName, nameToken);
                    parentTyped?.NestedTypes.Add(grpType);
                }
            }
            else if (TryIdentifyType(typeName, out type))
            {
                typeName = null;
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

            if (!isGroup)
            {
                if (tokens.ConsumeIf(TokenType.Symbol, "["))
                {
                    field.Options = ctx.ParseOptionBlock(field.Options, field);
                }

                tokens.Consume(TokenType.Symbol, ";");
            }
            ctx.AbortState = AbortState.None;
            return true;
        }

        internal static string GetJsonName(string name)
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
        internal static bool TryIdentifyType(string typeName, out Type type)
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
                    type = default(Type);
                    return false;
            }
        }

        internal static void ParseExtensions(ParserContext ctx, IHazNames parent, List<FieldDescriptorProto> extensions)
        {
            var extendee = ctx.Tokens.Consume(TokenType.AlphaNumeric);
            var dummy = new DummyExtensions(extendee, extensions);
            ctx.TryReadObjectImpl(dummy);
        }

        class DummyExtensions : ISchemaObject, IHazNames
        {
            IEnumerable<string> IHazNames.GetNames()
            {
                foreach (var field in extensions) yield return field.Name;
            }

            void ISchemaObject.ReadOne(ParserContext ctx)
            {
                ctx.AbortState = AbortState.Statement;
                if (TryParse(ctx, this, false, out var field))
                {
                    field.Extendee = extendee;
                    extensions.Add(field);
                }
                ctx.AbortState = AbortState.None;
            }

            private List<FieldDescriptorProto> extensions;
            private string extendee;

            public DummyExtensions(string extendee, List<FieldDescriptorProto> extensions)
            {
                this.extendee = extendee;
                this.extensions = extensions;
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
            bool isInputStream = tokens.ConsumeIf(TokenType.AlphaNumeric, "stream");
            var inputTypeToken = tokens.Read();
            var inputType = tokens.Consume(TokenType.AlphaNumeric);
            tokens.Consume(TokenType.Symbol, ")");
            tokens.Consume(TokenType.AlphaNumeric, "returns");
            tokens.Consume(TokenType.Symbol, "(");
            bool isOutputStream = tokens.ConsumeIf(TokenType.AlphaNumeric, "stream");
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
            if (isInputStream) method.ClientStreaming = true;
            if (isOutputStream) method.ServerStreaming = true;

            if (tokens.Peek(out var token) && token.Is(TokenType.Symbol, "{"))
            {
                ctx.AbortState = AbortState.Object;
                ctx.TryReadObjectImpl(method);
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

    }
    partial class MessageOptions : ISchemaOptions
    {
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key)
        {
            switch (key)
            {
                case "map_entry":
                    MapEntry = ctx.Tokens.ConsumeBoolean();
                    ctx.Errors.Error(ctx.Tokens.Previous, "'map_entry' should not be set explicitly; use 'map<TKey,TValue>' instead");
                    return true;
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
    internal static class ErrorExtensions
    {
        public static void Warn(this List<Error> errors, Token token, string message)
            => errors.Add(new Error(token, message, false));
        public static void Error(this List<Error> errors, Token token, string message)
            => errors.Add(new Error(token, message, true));
        public static void Error(this List<Error> errors, ParserException ex)
            => errors.Add(new Error(ex));
    }

    public class CodeFile
    {
        public override string ToString() => Name;
        public CodeFile(string name, string text)
        {
            Name = name;
            Text = text;
        }
        public string Name { get; }
        public string Text { get; }
    }

    internal abstract class LineWriter : IDisposable
    {
        public abstract void Write(string value);
        public virtual void WriteLine() => Write("\r\n");
        public virtual void WriteLine(string value)
        {
            Write(value);
            WriteLine();
        }
        public virtual void Dispose() { }
    }
#if !NO_IO
    internal sealed class TextWriterLineWriter : LineWriter
    {
        public TextWriterLineWriter(TextWriter buffer)
        {
            this.buffer = buffer;
        }
        private TextWriter buffer;
        public override void Write(string value) => buffer.Write(value);
        public override void WriteLine() => buffer.WriteLine();
        public override void WriteLine(string value) => buffer.WriteLine(value);
        public override void Dispose() { buffer?.Dispose(); buffer = null; }
        public override string ToString() => buffer?.ToString();
    }
#endif
    internal sealed class StringLineWriter : LineWriter
    {
        private StringBuilder buffer = new StringBuilder();
        public override void Write(string value) => buffer.Append(value);
        public override void WriteLine() => buffer.AppendLine();
        public override void WriteLine(string value) => buffer.AppendLine(value);
        public override void Dispose() { buffer = null; }
        public override string ToString() => buffer?.ToString();
    }
    internal abstract class LineReader : IDisposable
    {
        public abstract string ReadLine();
        public virtual void Dispose() { }
    }
    sealed class StringLineReader : LineReader
    {
        string[] lines;
        static readonly string[] splits = { "\r\n", "\r", "\n" };
        public StringLineReader(string text)
        {
            lines = text.Split(lines, StringSplitOptions.None);
        }
        int index;
        public override string ReadLine() =>
            index < lines.Length ? lines[index++] : null;
    }
#if !NO_IO
    sealed class TextReaderLineReader : LineReader
    {
        TextReader reader;
        public TextReaderLineReader(TextReader reader)
        {
            this.reader = reader;
        }
        public override void Dispose() => reader?.Dispose();
        public override string ReadLine() => reader?.ReadLine();
    }
#endif
    public static class CSharpCompiler
    {
        public static CompilerResult Compile(CodeFile file)
            => Compile(new[] { file });
        public static CompilerResult Compile(params CodeFile[] files)
        {
            var set = new FileDescriptorSet();
            foreach (var file in files)
            {
                using (var reader = new StringLineReader(file.Text))
                {
                    Console.WriteLine($"Parsing {file.Name}...");
                    set.Add(file.Name, reader, true);
                }
            }
            set.Process();
            var results = new List<CodeFile>();
            var newErrors = new List<Error>();

            try
            {
                results.AddRange(CSharpCodeGenerator.Default.Generate(set));
            }
            catch (Exception ex)
            {
                set.Errors.Add(new Error(default(Token), ex.Message, true));
            }
            var errors = set.GetErrors();

            return new CompilerResult(errors, results.ToArray());
        }
    }
    public class CompilerResult
    {
        internal CompilerResult(Error[] errors, CodeFile[] files)
        {
            Errors = errors;
            Files = files;
        }
        public Error[] Errors { get; }
        public CodeFile[] Files { get; }
    }

    internal class Import
    {
        public string Path { get; set; }
        public bool IsPublic { get; set; }
        public Token Token { get; set; }
        public bool Used { get; set; }
    }
    public class Error
    {
        public static Error[] Parse(string stdout, string stderr)
        {
            if (string.IsNullOrWhiteSpace(stdout) && string.IsNullOrWhiteSpace(stderr))
                return noErrors;

            List<Error> errors = new List<Error>();
            using(var reader = new StringReader(stdout))
            {
                Add(reader, errors);
            }
            using (var reader = new StringReader(stderr))
            {
                Add(reader, errors);
            }
            return errors.ToArray();
        }
        static void Add(TextReader lines, List<Error> errors)
        {
            string line;
            while((line = lines.ReadLine()) != null)
            {
                var s = line;
                bool isError = true;
                int lineNumber = 1, columnNumber = 1;
                if(s[0] == '[')
                {
                    int i = s.IndexOf(']');
                    if (i > 0)
                    {
                        var prefix = line.Substring(1, i).Trim();
                        s = line.Substring(i + 1).Trim();
                        if(prefix.IndexOf("WARNING", StringComparison.OrdinalIgnoreCase) >= 0
                            && prefix.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            isError = false;
                        }
                    }
                }
                var match = Regex.Match(s, @"^([^:]+):([0-9]+):([0-9]+):\s+");
                string file = "";
                if(match.Success)
                {
                    file = match.Groups[1].Value;
                    if (!int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out lineNumber))
                        lineNumber = 1;
                    if (!int.TryParse(match.Groups[3].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out columnNumber))
                        columnNumber = 1;
                    s = s.Substring(match.Length).Trim();
                }
                errors.Add(new Error(new Token(" ", lineNumber, columnNumber, TokenType.None, "", 0, file), s, isError));
            }
        }
        internal string ToString(bool includeType) => Text.Length == 0
                ? $"{File}({LineNumber},{ColumnNumber}): {(includeType ? (IsError ? "error: " : "warning: ") : "")}{Message}"
                : $"{File}({LineNumber},{ColumnNumber},{LineNumber},{ColumnNumber + Text.Length}): {(includeType ? (IsError ? "error: " : "warning: ") : "")}{Message}";
        public override string ToString() => ToString(true);

        internal static Error[] GetArray(List<Error> errors)
            => errors.Count == 0 ? noErrors : errors.ToArray();

        private static readonly Error[] noErrors = new Error[0];

        internal Error(Token token, string message, bool isError)
        {
            ColumnNumber = token.ColumnNumber;
            LineNumber = token.LineNumber;
            File = token.File;
            LineContents = token.LineContents;
            Message = message;
            IsError = isError;
            Text = token.Value;
        }
        internal Error(ParserException ex)
        {
            ColumnNumber = ex.ColumnNumber;
            LineNumber = ex.LineNumber;
            File = ex.File;
            LineContents = ex.LineContents;
            Message = ex.Message;
            IsError = ex.IsError;
            Text = ex.Text ?? "";
        }
        public bool IsWarning => !IsError;

        public bool IsError { get; }
        public string File { get; }
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

    interface IHazNames
    {
        IEnumerable<string> GetNames();
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
                Errors.Error(ex);
            }
            finally
            {
                var state = AbortState;
                if (Tokens.Peek(out var stateAfter) && stateBefore == stateAfter)
                {
                    // we didn't move! avoid looping forever failing to do the same thing
                    Errors.Error(stateAfter, "unknown error");
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
            var keyToken = tokens.Previous;
            tokens.Consume(TokenType.Symbol, "=");

            var field = parent as FieldDescriptorProto;
            bool isField = typeof(T) == typeof(FieldOptions) && field != null;
            if (key == "default" && isField)
            {
                string defaultValue = tokens.ConsumeString();
                keyToken.RequireProto2(this);
                ParseDefault(tokens.Previous, field.type, ref defaultValue);
                if (defaultValue != null)
                {
                    field.DefaultValue = defaultValue;
                }
            }
            else if (key == "json_name" && isField)
            {
                string jsonName = tokens.ConsumeString();
                if (string.Equals(jsonName, "none", StringComparison.OrdinalIgnoreCase) && tokens.Previous.Is(TokenType.StringLiteral))
                {
                    field.JsonName = jsonName;
                }
                else
                {
                    field.ResetJsonName();
                }
            }
            else
            {
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

        private void ParseDefault(Token token, FieldDescriptorProto.Type type, ref string defaultValue)
        {
            switch (type)
            {
                case FieldDescriptorProto.Type.TypeBool:
                    switch (defaultValue)
                    {
                        case "true":
                        case "false":
                            break;
                        default:
                            Errors.Error(token, "expected 'true' or 'false'");
                            break;
                    }
                    break;
                case FieldDescriptorProto.Type.TypeDouble:
                    switch (defaultValue)
                    {
                        case "inf":
                        case "-inf":
                        case "nan":
                            break;
                        default:
                            if (double.TryParse(defaultValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                            {
                                defaultValue = val.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                Errors.Error(token, "invalid floating-point number");
                            }
                            break;
                    }
                    break;
                case FieldDescriptorProto.Type.TypeFloat:
                    switch (defaultValue)
                    {
                        case "inf":
                        case "-inf":
                        case "nan":
                            break;
                        default:
                            if (float.TryParse(defaultValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                            {
                                defaultValue = val.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                Errors.Error(token, "invalid floating-point number");
                            }
                            break;
                    }
                    break;
                case FieldDescriptorProto.Type.TypeFixed32:
                case FieldDescriptorProto.Type.TypeInt32:
                case FieldDescriptorProto.Type.TypeSint32:
                    {
                        if (int.TryParse(defaultValue, NumberStyles.Number & NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var val))
                        {
                            defaultValue = val.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            Errors.Error(token, "invalid integer");
                        }
                    }
                    break;
                case FieldDescriptorProto.Type.TypeUint32:
                    {
                        if (uint.TryParse(defaultValue, NumberStyles.Number & ~NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var val))
                        {
                            defaultValue = val.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            Errors.Error(token, "invalid unsigned integer");
                        }
                    }
                    break;
                case FieldDescriptorProto.Type.TypeFixed64:
                case FieldDescriptorProto.Type.TypeInt64:
                case FieldDescriptorProto.Type.TypeSint64:
                    {
                        if (long.TryParse(defaultValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var val))
                        {
                            defaultValue = val.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            Errors.Error(token, "invalid integer");
                        }
                    }
                    break;
                case FieldDescriptorProto.Type.TypeUint64:
                    {
                        if (ulong.TryParse(defaultValue, NumberStyles.Number & ~NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var val))
                        {
                            defaultValue = val.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            Errors.Error(token, "invalid unsigned integer");
                        }
                    }
                    break;

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
                Errors.Error(ex);
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
                Errors.Error(ex);
                tokens.SkipToEndStatement();
            }
            return obj;
        }
        public bool TryReadObject<T>(out T obj) where T : class, ISchemaObject, new()
        {
            obj = new T();
            return TryReadObjectImpl(obj);
        }
        internal bool TryReadObjectImpl<T>(T obj) where T : class, ISchemaObject
        {
            var tokens = Tokens;

            try
            {
                tokens.Consume(TokenType.Symbol, "{");
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
                Errors.Error(ex);
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

        public void Dispose() { Tokens?.Dispose(); }

        internal void CheckNames(IHazNames parent, string name, Token token
#if DEBUG && NETSTANDARD1_3
            , [System.Runtime.CompilerServices.CallerMemberName] string caller = null
#endif
            )
        {
            if (parent != null && parent.GetNames().Contains(name))
            {
                Errors.Error(token, $"name '{name}' is already in use"
#if DEBUG && NETSTANDARD1_3
             + $" ({caller})"
#endif
                    );
            }
        }
    }
}
