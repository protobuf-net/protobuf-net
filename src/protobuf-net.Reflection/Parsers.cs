﻿using Google.Protobuf.Reflection;
using ProtoBuf;
using ProtoBuf.Meta;
using ProtoBuf.Reflection;
using ProtoBuf.Reflection.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Google.Protobuf.Reflection
{
    internal interface IType
    {
        IType Parent { get; }
        string FullyQualifiedName { get; }

        IType Find(string name);
    }

    internal interface IReserved<TRange, TField>
        where TRange : IReservedRange
        where TField : IField
    {
        List<string> ReservedNames { get; }
        List<TRange> ReservedRanges { get; }
        List<TField> Fields { get; }
    }

    internal interface IReservedRange
    {
        int Start { get; set; }
        int End { get; set; }
    }

    internal interface IField
    {
        int Number { get; }
        string Name { get; }
    }

    /// <summary>
    /// Represents a virtual file system
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Indicates whether a specified file exists
        /// </summary>
        bool Exists(string path);
        /// <summary>
        /// Opens the specified file for text parsing
        /// </summary>
        TextReader OpenText(string path);
    }

    internal class DefaultFileSystem : IFileSystem
    {
        private DefaultFileSystem() { }
        public static IFileSystem Instance { get; } = new DefaultFileSystem();
        bool IFileSystem.Exists(string path) => File.Exists(path);

        TextReader IFileSystem.OpenText(string path) => File.OpenText(path);
    }

    /// <summary>
    /// The protocol compiler can output a FileDescriptorSet containing the .proto
    /// files it parses.
    /// </summary>
    public partial class FileDescriptorSet
    {
        internal const string NotIntendedForPublicUse = "This method was not intended for public use, and it may be removed in a later version";

        internal const string Namespace = ".google.protobuf.";

        /// <summary>
        /// Provides a callback to allow/deny individual imports.
        /// </summary>
        public Func<string, bool> ImportValidator { get; set; }

        /// <summary>
        /// Provides a virtual file system (otherwise OS defaults are assumed)
        /// </summary>
        public IFileSystem FileSystem { get; set; }

        internal IFileSystem EffectiveFileSystem => FileSystem ?? DefaultFileSystem.Instance;

        internal List<string> importPaths = new List<string>();

        /// <summary>
        /// Adds a file system location for resolving imports
        /// </summary>
        public void AddImportPath(string path)
        {
            importPaths.Add(path);
        }

        /// <summary>
        /// Gets the errors encountered when calling <see cref="Process"/>
        /// </summary>
        public Error[] GetErrors() => Error.GetArray(Errors);
        internal List<Error> Errors { get; } = new List<Error>();

        /// <summary>
        /// When processing Foo/Bar/Some.proto, should Foo/Bar be treated as an implicit import location?
        /// </summary>
        public bool AllowNameOnlyImport { get; set; }

        /// <summary>
        /// Adds an input file to the set for processing
        /// </summary>
        public bool Add(string name, bool includeInOutput = true, TextReader source = null)
            => Add(name, includeInOutput, source, null);
        internal bool Add(string name, bool includeInOutput, TextReader source, FileDescriptorProto fromFile)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException(nameof(name));
            if (Path.IsPathRooted(name) || name.Contains(".."))
                throw new ArgumentException("Paths should be relative to the import paths, not rooted", nameof(name));

            if (TryResolve(name, fromFile, out var descriptor))
            {
                if (includeInOutput) descriptor.IncludeInOutput = true;
                return true; // already exists, that counts as success
            }
            string path = null;
            using var reader = source ?? Open(name, out path);
            if (reader == null) return false; // not found

            descriptor = new FileDescriptorProto
            {
                Name = name,
                IncludeInOutput = includeInOutput,
                DefaultPackage = GetDefaultPackageName(path),
            };
            Files.Add(descriptor);

            descriptor.ParseSchema(reader, Errors, name);
            return true;
        }
        /// <summary>
        /// Default package to use when none is specified; can use #FILE# and #DIR# tokens
        /// </summary>
        public string DefaultPackage { get; set; }
        private string GetDefaultPackageName(string path)
        {
            if (string.IsNullOrWhiteSpace(DefaultPackage)) return null;

            if (DefaultPackage.IndexOf('#') < 0) return DefaultPackage;

            var file = Path.GetFileNameWithoutExtension(path)?.Replace(' ', '_');
            var dir = Path.GetFileName(Path.GetDirectoryName(path))?.Replace(' ', '_');

            return DefaultPackage.Replace("#FILE#", file).Replace("#DIR#", dir);
        }

        private TextReader Open(string name, out string found)
        {
            found = FindFile(name);
            if (found == null)
            {
                var embedded = TryGetEmbedded(name);
                if (embedded != null)
                    return new StreamReader(embedded);
                return null;
            }
            return EffectiveFileSystem.OpenText(found);
        }



        private static Stream TryGetEmbedded(string name)
        {
            if (string.IsNullOrWhiteSpace(name) || !name.EndsWith(".proto")) return null;

            if (name.StartsWith("google/")
                || name.StartsWith("protobuf-net/"))
            {
                var resourceName = "ProtoBuf." + name.Replace('/', '.').Replace('-', '_');
                try
                {
                    return typeof(FileDescriptorSet)
                    .Assembly.GetManifestResourceStream(resourceName);
                } catch { }
            }
            return null;
        }
        private string FindFile(string file)
        {
            string rel;
            var fileSystem = EffectiveFileSystem;
            foreach (var path in importPaths)
            {
                rel = Path.Combine(path, file);
                if (fileSystem.Exists(rel)) return rel;
            }
            return null;
        }

        private FileDescriptorProto TryFindFileByName(string filename)
        {
            foreach (var file in Files)
            {
                if (string.Equals(file.Name, filename, StringComparison.OrdinalIgnoreCase))
                    return file;
            }
            return null;
        }
        private bool TryResolve(string name, FileDescriptorProto from, out FileDescriptorProto descriptor)
        {
            descriptor = TryFindFileByName(name);

            if (descriptor == null && from != null && AllowNameOnlyImport && Path.GetFileName(name) == name) // only if no folder specified
            {
                try
                {
                    var inSameFolder = Path.Combine(Path.GetDirectoryName(from.Name), name);
                    descriptor = TryFindFileByName(inSameFolder);
                }
                catch { } // ignore
            }
            return descriptor != null;
        }

        private void ApplyImports()
        {
            bool didSomething;
            do
            {
                didSomething = false;
                var file = Files.Find(static x => x.HasPendingImports);
                if (file != null)
                {
                    // note that GetImports clears the flag
                    foreach (var import in file.GetImports(true))
                    {
                        if (!(ImportValidator?.Invoke(import.Path) ?? true))
                        {
                            Errors.Error(import.Token, $"import of {import.Path} is disallowed", ErrorCode.ImportNotPermitted);
                        }
                        else if (Add(import.Path, false, null, file))
                        {
                            didSomething = true;
                        }
                        else
                        {
                            Errors.Error(import.Token, $"unable to find: '{import.Path}'", ErrorCode.ImportNotFound);
                        }
                    }
                }
            } while (didSomething);
        }

        /// <summary>
        /// Process the added schemas; this resolves member types between schemas, and prepares the type hierarchy.
        /// </summary>
        public void Process()
        {
            ApplyImports();
            foreach (var file in Files)
            {
                using var ctx = new ParserContext(file, null, Errors);
                file.BuildTypeHierarchy(this);
            }
            foreach (var file in Files)
            {
                using var ctx = new ParserContext(file, null, Errors);
                file.ResolveTypes(ctx, false);
            }
            foreach (var file in Files)
            {
                using var ctx = new ParserContext(file, null, Errors);
                file.ResolveTypes(ctx, true);
            }
            foreach (var file in Files)
            {
                using var ctx = new ParserContext(file, null, Errors);
                foreach (var message in file.MessageTypes)
                {
                    PreProcessMessage(message, ctx);
                }
                static void PreProcessMessage(DescriptorProto message, ParserContext ctx)
                {
                    if (message == null) return;
                    var options = Extensions.GetOptions(message?.Options);
                    if (options is not null)
                    {
                        switch (options.MessageKind)
                        {
                            case MessageKind.None:
                                break; // nothing to do
                            case MessageKind.NullWrapper:
                                if (ctx.Syntax != FileDescriptorProto.SyntaxProto3)
                                {
                                    ctx.Errors.Warn(message.SourceLocation, $"Null wrapper message '{message.Name}' requires {FileDescriptorProto.SyntaxProto3} syntax", ErrorCode.MessageKindNullWrapperProto3);
                                }
                                else if (message.Fields.Count != 1)
                                {
                                    ctx.Errors.Warn(message.SourceLocation, $"Null wrapper message '{message.Name}' requires exactly one field (found: {message.Fields.Count})", ErrorCode.MessageKindNullWrapperSingleField);
                                }
                                else
                                {
                                    var field = message.Fields.Single();
                                    if (field.Number == 1)
                                    {
                                        message.ParsedSpecialKind = field.Proto3Optional || field.ShouldSerializeOneofIndex()
                                            ? DescriptorProto.SpecialKind.NullableWrapperFieldPresence
                                            : DescriptorProto.SpecialKind.NullableWrapperImplicitZero;
                                    }
                                    else
                                    {
                                        ctx.Errors.Warn(message.SourceLocation, $"Null wrapper message '{message.Name}' must use field 1 (found: {field.Number})", ErrorCode.MessageKindNullWrapperFieldOne);
                                    }
                                }
                                break;
                            default:
                                ctx.Errors.Warn(message.SourceLocation, $"Unexpected message kind on '{message.Name}': {options.MessageKind}", ErrorCode.MessageKindUnknownKind);
                                break;
                        }
                    }
                    foreach (var subMessage in message.NestedTypes)
                    {
                        PreProcessMessage(subMessage, ctx);
                    }
                }
            }
        }

        /// <summary>
        /// Reorders the <see cref="Files"/> so that they are in dependency order, i.e. dependencies come FIRST
        /// </summary>
        /// <remarks>This is mostly useful for compatibility with <c>protoc</c> with the <c>--descriptor_set_out --include_imports</c> switches</remarks>
        public void ApplyFileDependencyOrder()
        {
            var fileOrder = new Dictionary<FileDescriptorProto, int>(Files.Count);
            void Observe(FileDescriptorProto file)
            {
                if (file is null || fileOrder.ContainsKey(file))
                    return; // nothing to do

                foreach (var dep in file.Dependencies)
                {
                    Observe(TryFindFileByName(dep));
                }
                if (!fileOrder.TryGetValue(file, out _))
                    fileOrder.Add(file, fileOrder.Count);
            }

            int GetOrder(FileDescriptorProto file)
                => fileOrder.TryGetValue(file, out var order) ? order : fileOrder.Count;

            foreach (var file in Files)
            {
                Observe(file);
            }
            Files.Sort((x, y) => GetOrder(x).CompareTo(GetOrder(y)));
        }

        /// <summary>
        /// Serializes this instance using the provided serializer (which does not need to be protobuf)
        /// </summary>
        public T Serialize<T>(Func<FileDescriptorSet,object,T> customSerializer, bool includeImports, object state = null)
        {
            T result;
            if (includeImports || Files.All(static x => x.IncludeInOutput))
            {
                result = customSerializer(this, state);
            }
            else
            {
                var snapshort = Files.ToArray();
                Files.RemoveAll(static x => !x.IncludeInOutput);
                result = customSerializer(this, state);
                Files.Clear();
                Files.AddRange(snapshort);
            }
            return result;
        }

        /// <summary>
        /// Serializes this instance using the provided <see cref="TypeModel"/>
        /// </summary>
        public void Serialize(TypeModel model, Stream destination, bool includeImports)
        {
            Tuple<TypeModel, Stream> state = Tuple.Create(model, destination);
            Serialize(static (fds,o) => {

                var tuple = (Tuple<TypeModel, Stream>)o;
                tuple.Item1.Serialize(tuple.Item2, fds);
                return true; }, includeImports, state);
        }

        internal FileDescriptorProto GetFile(FileDescriptorProto from, string path)
            => TryResolve(path, from, out var descriptor) ? descriptor : null;
    }
    /// <summary>
    /// Describes a message type.
    /// </summary>
    public partial class DescriptorProto : ISchemaObject, IType, IMessage, IReserved<DescriptorProto.ReservedRange, FieldDescriptorProto>
    {
        /// <summary>
        /// Range of reserved tag numbers. Reserved tag numbers may not be used by
        /// fields or extension ranges in the same message. Reserved ranges may
        /// not overlap.
        /// </summary>
        public partial class ReservedRange : IReservedRange { }

        internal Token SourceLocation { get; set; }
        internal enum SpecialKind
        {
            None,
            NullableWrapperImplicitZero,
            NullableWrapperFieldPresence,
        }

        internal SpecialKind ParsedSpecialKind { get; set; }

        /// <summary>
        /// Gets the extension data associated with an object, as a byte-array
        /// </summary>
        /// <remarks>This is required for equivalence tests vs 'protoc'</remarks>
        [Obsolete(FileDescriptorSet.NotIntendedForPublicUse, false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static byte[] GetExtensionData(IExtensible obj)
            => GetRawExtensionData(obj);

        internal static byte[] GetRawExtensionData(IExtensible obj)
        {
            var ext = obj?.GetExtensionObject(false);
            int len;
            if (ext == null || (len = ext.GetLength()) == 0) return null;
            var s = ext.BeginQuery();
            try
            {
                if (s is MemoryStream ms) return (ms).ToArray();

                byte[] buffer = new byte[len];
                int offset = 0, read;
                while ((read = s.Read(buffer, offset, len)) > 0)
                {
                    offset += read;
                    len -= read;
                }
                if (len != 0) throw new EndOfStreamException();
                return buffer;
            }
            finally
            {
                ext.EndQuery(s);
            }
        }

        /// <summary>
        /// Sets the extension data associated with an object, as a byte-array
        /// </summary>
        /// <remarks>This is required for equivalence tests vs 'protoc'</remarks>
        [Obsolete(FileDescriptorSet.NotIntendedForPublicUse, false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetExtensionData(IExtensible obj, byte[] data)
            => SetRawExtensionData(obj, data);

        internal static void SetRawExtensionData(IExtensible obj, byte[] data)
        {
            if (obj == null || data == null || data.Length == 0) return;
            var ext = obj.GetExtensionObject(true);
            (ext as IExtensionResettable)?.Reset();
            var s = ext.BeginAppend();
            try
            {
                s.Write(data, 0, data.Length);
                ext.EndAppend(s, true);
            }
            catch
            {
                ext.EndAppend(s, false);
                throw;
            }
        }

        /// <inheritdoc/>
        public override string ToString() => Name;
        internal IType Parent { get; set; }
        IType IType.Parent => Parent;
        string IType.FullyQualifiedName => FullyQualifiedName;
        IType IType.Find(string name)
        {
            return (IType)NestedTypes.Find(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? (IType)EnumTypes.Find(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        internal string FullyQualifiedName { get; set; }

        List<DescriptorProto> IMessage.Types => NestedTypes;

        internal int MaxField => (Options?.MessageSetWireFormat == true) ? int.MaxValue : FieldDescriptorProto.DefaultMaxField;
        int IMessage.MaxField => MaxField;
        internal static bool TryParse(ParserContext ctx, IHazNames parent, out DescriptorProto obj)
        {
            var name = ctx.Tokens.Consume(TokenType.AlphaNumeric, out var token);
            ctx.CheckNames(parent, name, ctx.Tokens.Previous);
            if (ctx.TryReadObject(out obj))
            {
                obj.Name = name;
                obj.SourceLocation = token;
                GenerateSyntheticOneOfs(obj);
                return true;
            }
            return false;

            static void GenerateSyntheticOneOfs(DescriptorProto obj)
            {
                foreach(var field in obj.Fields)
                {
                    if (field.Proto3Optional && !field.ShouldSerializeOneofIndex())
                    {
                        field.OneofIndex = obj.OneofDecls.Count;
                        obj.OneofDecls.Add(new OneofDescriptorProto
                        {
                            Name = "_" + field.Name,
                            Parent = obj,
                        });
                    }
                }
            }
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
                Options = ctx.ParseOptionStatement(Options, this);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "reserved"))
            {
                ParseReservedRanges(ctx, this, "field", MaxField, true);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "extensions"))
            {
                ParseExtensionRange(ctx);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "extend"))
            {
                FieldDescriptorProto.ParseExtensions(ctx, this);
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
                    ctx.Errors.Error(tokens.Previous, "invalid map key type (only integral and string types are allowed)", ErrorCode.InvalidMapKeyType);
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
            msgType.Options ??= new MessageOptions();
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
                int from = tokens.ConsumeInt32(MaxField), to = from;
                if (tokens.Read().Is(TokenType.AlphaNumeric, "to"))
                {
                    tokens.Consume();
                    to = tokens.ConsumeInt32(MaxField);
                }
                // the end is off by one
                if (to != int.MaxValue) to++;
                ExtensionRanges.Add(new ExtensionRange { Start = from, End = to });

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
                    tokens.Read().Throw(ErrorCode.ParseFailExtensionRange, "unable to parse extension range");
                }
            }
            ctx.AbortState = AbortState.None;
        }

        internal static void ParseReservedRanges<TRange, TField>(ParserContext ctx, IReserved<TRange, TField> reserved, string label, int? max, bool extendRange)
            where TRange : class, IReservedRange, new()
            where TField : IField
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
                        var conflict = reserved.Fields.Find(x => x.Name == name);
                        if (conflict != null)
                        {
                            ctx.Errors.Error(tokens.Previous, $"'{conflict.Name}' is already in use by {label} {conflict.Number}", ErrorCode.FieldDuplicatedNumber);
                        }
                        reserved.ReservedNames.Add(name);

                        if (tokens.ConsumeIf(TokenType.Symbol, ","))
                        {
                        }
                        else if (tokens.ConsumeIf(TokenType.Symbol, ";"))
                        {
                            break;
                        }
                        else
                        {
                            tokens.Read().Throw(ErrorCode.ParseFailReservedRange, "unable to parse reserved range");
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
                            to = tokens.ConsumeInt32(max);
                        }
                        var conflict = reserved.Fields.Find(x => x.Number >= from && x.Number <= to);
                        if (conflict != null)
                        {
                            ctx.Errors.Error(tokens.Previous, $"{label} {conflict.Number} is already in use by '{conflict.Name}'", ErrorCode.FieldDuplicatedNumber);
                        }
                        if (extendRange) to++; // message are extended (i.e. range is whatever+1); enums are not; because reasons
                        reserved.ReservedRanges.Add(new TRange { Start = from, End = to });

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
                            token.Throw(ErrorCode.ParseFailReservedRange);
                        }
                    }
                    break;
                default:
                    throw token.Throw(ErrorCode.ParseFailReservedRange);
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

    /// <summary>
    /// Describes a oneof.
    /// </summary>
    public partial class OneofDescriptorProto : ISchemaObject
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
                Options = ctx.ParseOptionStatement(Options, this);
            }
            else
            {
                if (FieldDescriptorProto.TryParse(ctx, Parent, true, out var field))
                {
                    field.OneofIndex = Parent.OneofDecls.Count - 1;
                    Parent.Fields.Add(field);
                }
            }
        }
    }
    /// <summary>
    /// Options relating to a oneof
    /// </summary>
    public partial class OneofOptions : ISchemaOptions
    {
        string ISchemaOptions.Extendee => FileDescriptorSet.Namespace + nameof(OneofOptions);
        /// <summary>
        /// Gets or sets the extension data as a byte-array
        /// </summary>
        /// <remarks>This is required for equivalence tests vs 'protoc'</remarks>
        [Obsolete(FileDescriptorSet.NotIntendedForPublicUse, false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public byte[] ExtensionData
        {
            get { return DescriptorProto.GetRawExtensionData(this); }
            set { DescriptorProto.SetRawExtensionData(this, value); }
        }
        bool ISchemaOptions.Deprecated { get { return false; } set { } }
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key) => false;
    }
    /// <summary>
    /// The protocol compiler can output a FileDescriptorSet containing the .proto
    /// files it parses.
    /// </summary>
    public partial class FileDescriptorProto : ISchemaObject, IMessage, IType
    {
        internal static FileDescriptorProto GetFile(IType type)
        {
            while (type != null)
            {
                if (type is FileDescriptorProto file) return file;
                type = type.Parent;
            }
            return null;
        }
        int IMessage.MaxField => FieldDescriptorProto.DefaultMaxField;
        List<FieldDescriptorProto> IMessage.Fields => null;
        List<FieldDescriptorProto> IMessage.Extensions => Extensions;
        List<DescriptorProto> IMessage.Types => MessageTypes;

        /// <inheritdoc/>
        public override string ToString() => Name;

        string IType.FullyQualifiedName => null;
        IType IType.Parent => null;
        IType IType.Find(string name)
        {
            return (IType)MessageTypes.Find(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? (IType)EnumTypes.Find(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase))
                ?? (IType)Services.Find(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        internal bool HasPendingImports { get; private set; }
        internal FileDescriptorSet Parent { get; private set; }

        /// <summary>
        /// Indicates whether this file is intended as part of the output set of the parse operation.
        /// </summary>
        public bool IncludeInOutput { get; set; }

        /// <summary>
        /// Controls external serialization
        /// </summary>
        /// <remarks>This is required for equivalence tests vs 'protoc'</remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeIncludeInOutput() => false;

        internal string DefaultPackage { get; set; }

        /// <summary>
        /// Indicates whether this file has imports
        /// </summary>
        public bool HasImports() => _imports.Count != 0;
        internal IEnumerable<Import> GetImports(bool resetPendingFlag = false)
        {
            if (resetPendingFlag)
            {
                HasPendingImports = false;
            }
            return _imports;
        }
        private readonly List<Import> _imports = new List<Import>();
        internal bool AddImport(string path, bool isPublic, Token token)
        {
            var existing = _imports.Find(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase));
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
                FieldDescriptorProto.ParseExtensions(ctx, this);
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
                    ctx.Errors.Warn(tokens.Previous, $"duplicate import: '{path}'", ErrorCode.ImportDuplicated);
                }
                tokens.Consume(TokenType.Symbol, ";");
                ctx.AbortState = AbortState.None;
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "syntax"))
            {
                ctx.AbortState = AbortState.Statement;
                if (MessageTypes.Count > 0 || EnumTypes.Count > 0 || Extensions.Count > 0)
                {
                    ctx.Errors.Error(tokens.Previous, "syntax must be set before types are defined", ErrorCode.ProtoSyntaxPreceedTypes);
                }
                tokens.Consume(TokenType.Symbol, "=");
                Syntax = tokens.Consume(TokenType.StringLiteral);
                switch (Syntax)
                {
                    case SyntaxProto2:
                    case SyntaxProto3:
                        break;
                    default:
                        ctx.Errors.Error(tokens.Previous, $"unknown syntax '{Syntax}'", ErrorCode.ProtoSyntaxInvalid);
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
                Options = ctx.ParseOptionStatement(Options, this);
            }
            else if (tokens.Peek(out var token))
            {
                token.Throw(ErrorCode.SyntaxErrorUnknownEntity);
            } // else EOF
        }

        /// <summary>
        /// Processes an individual file contents, but does <em>not</em> perform type resolution.
        /// </summary>
        [Obsolete(FileDescriptorSet.NotIntendedForPublicUse, false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public void Parse(TextReader schema, List<Error> errors, string file)
            => ParseSchema(schema, errors, file);

        internal void ParseSchema(TextReader schema, List<Error> errors, string file)
        {
            Syntax = "";
            using var ctx = new ParserContext(this, new Peekable<Token>(schema.Tokenize(file).RemoveCommentsAndWhitespace(), errors), errors);
            var tokens = ctx.Tokens;
            tokens.Peek(out Token startOfFile); // want this for "stuff you didn't do" warnings

            // read the file into the object
            ctx.Fill(this);

            // finish up
            if (string.IsNullOrWhiteSpace(Syntax))
            {
                ctx.Errors.Warn(startOfFile, "no syntax specified; it is strongly recommended to specify 'syntax=\"proto2\";' or 'syntax=\"proto3\";'", ErrorCode.ProtoSyntaxNotSpecified);
            }

            if (Syntax == "" || Syntax == SyntaxProto2)
            {
                Syntax = null; // for output compatibility; is blank even if set to proto2 explicitly
            }
        }
        internal bool TryResolveEnum(string typeName, IType parent, out EnumDescriptorProto @enum, bool allowImports, bool treatAllAsPublic = false)
        {
            if (TryResolveType(typeName, parent, out var type, allowImports, true, treatAllAsPublic))
            {
                @enum = type as EnumDescriptorProto;
                return @enum != null;
            }
            @enum = null;
            return false;
        }
        internal bool TryResolveMessage(string typeName, IType parent, out DescriptorProto message, bool allowImports, bool treatAllAsPublic = false)
        {
            if (TryResolveType(typeName, parent, out var type, allowImports, true, treatAllAsPublic))
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
        internal static bool TrySplitLast(string input, out string left, out string right)
        {
            var split = input.LastIndexOf('.');
            if (split < 0)
            {
                left = right = null;
                return false;
            }
            left = input.Substring(0, split).Trim();
            right = input.Substring(split + 1).Trim();
            return true;
        }
        private bool TryResolveExtension(string extendee, string extension, out FieldDescriptorProto field, bool allowImports = true, bool checkOwnPackage = true)
        {
            static bool TryResolveFromFile(FileDescriptorProto file, string ee, string ion, out FieldDescriptorProto fld, bool withPackageName, bool ai)
            {
                fld = null;
                if (file == null) return false;

                if (withPackageName)
                {
                    var pkg = file.Package;
                    if (string.IsNullOrWhiteSpace(pkg)) return false; // we're only looking *with* packages right now

                    if (!ion.StartsWith(pkg + ".")) return false; // wrong file

                    ion = ion.Substring(pkg.Length + 1); // and fully qualified (non-qualified is a second pass)
                }

                return file.TryResolveExtension(ee, ion, out fld, ai, false);
            }

            bool isRooted = extension.StartsWith(".");
            if (isRooted)
            {
                // rooted
                extension = extension.Substring(1); // remove the root
            }
            if (TrySplitLast(extension, out var left, out var right))
            {
                if (TryResolveType(left, null, out var type, true, true))
                {
                    field = (type as DescriptorProto)?.Extensions?.FirstOrDefault(x => x.Extendee == extendee
                        && x.Name == right);
                    if (field != null) return true;
                }
            }
            else
            {
                field = Extensions?.FirstOrDefault(x => x.Extendee == extendee && x.Name == extension);
                if (field != null) return true;
            }

            if (checkOwnPackage)
            {
                if (TryResolveFromFile(this, extendee, extension, out field, true, false)) return true;
                if (TryResolveFromFile(this, extendee, extension, out field, false, false)) return true;
            }
            if (allowImports)
            {
                foreach (var import in _imports)
                {
                    var file = Parent?.GetFile(this, import.Path);
                    if (file != null)
                    {
                        if (TryResolveFromFile(file, extendee, extension, out field, true, import.IsPublic))
                        {
                            import.Used = true;
                            return true;
                        }
                    }
                }
                foreach (var import in _imports)
                {
                    var file = Parent?.GetFile(this, import.Path);
                    if (file != null)
                    {
                        if (TryResolveFromFile(file, extendee, extension, out field, false, import.IsPublic))
                        {
                            import.Used = true;
                            return true;
                        }
                    }
                }
            }
            field = null;
            return false;
        }

        bool TryResolveFromFile(FileDescriptorProto file, string tn, out IType tp, bool taap)
        {
            tp = null;
            if (file == null || string.IsNullOrEmpty(tn)) return false;

            if (tn[0] == '.')
            {
                // fully qualified
                return file.TryResolveType(tn, file, out tp, checkOwnPackage: false, allowImports: false, treatAllAsPublic: taap);
            }
            foreach (var prefixPart in file.GetDescendingPackagePrefixes())
            {
                if (file.TryResolveType(prefixPart + tn, file, out tp, checkOwnPackage: false, allowImports: false, treatAllAsPublic: taap))
                    return true;
            }
            tp = null;
            return false;
        }

        private string[] GetDescendingPackagePrefixes()
        // if the package is Foo.Bar.Blap, then this gives ".Foo.Bar.Blap.", ".Foo.Bar.", ".Foo.", "."
            => _packagePrefixes ??= CalculateDescendingPackagePrefixes(Package);

        private string[] _packagePrefixes;
        private static readonly string[] s_defaultPackagePrefixes = new[] { "." };
        private static readonly char[] s_packageDelimiters = new[] { '.' };

        static string[] CalculateDescendingPackagePrefixes(string package)
        {
            if (string.IsNullOrWhiteSpace(package)) return s_defaultPackagePrefixes;

            var pieces = package.Split(s_packageDelimiters);
            var result = new string[pieces.Length + 1];
            int target = pieces.Length;
            string s = result[target--] = ".";
            for (int i = 0; i < pieces.Length;i++)
            {
                s = result[target--] = s + pieces[i] + ".";
            }
            return result;
        }

        internal bool TryResolveType(string typeName, IType parent, out IType type, bool allowImports, bool checkOwnPackage = true, bool treatAllAsPublic = false)
        {
            var originalTypeName = typeName;
            bool checkLocal = true;
            if (typeName.StartsWith("."))
            {
                if (string.IsNullOrWhiteSpace(Package))
                { // could be anything...
                    typeName = typeName.Substring(1); // remove the root
                }
                else if(typeName.StartsWith("." + Package + "."))
                { // looks like a match
                    typeName = typeName.Substring(Package.Length + 2);
                }
                else
                {
                    checkLocal = false; // not us
                }
                // rooted
            }
            if (!checkLocal) { } // not this package
            else if (TrySplit(typeName, out var left, out var right))
            {
                while (parent != null)
                {
                    var next = parent?.Find(left);
                    if (next != null && TryResolveType(right, next, out type, false, treatAllAsPublic)) return true;

                    parent = parent.Parent;
                }
            }
            else
            {
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
            }

            if (checkOwnPackage && TryResolveFromFile(this, originalTypeName, out type, treatAllAsPublic)) return true;

            // look at imports
            foreach (var import in _imports)
            {
                if (allowImports || import.IsPublic || treatAllAsPublic)
                {
                    var file = Parent?.GetFile(this, import.Path);
                    if (TryResolveFromFile(file, originalTypeName, out type, treatAllAsPublic))
                    {
                        import.Used = true;
                        return true;
                    }
                }
            }

            type = null;
            return false;
        }

        private static void SetParents(string prefix, EnumDescriptorProto parent)
        {
            parent.FullyQualifiedName = prefix + "." + parent.Name;
            foreach (var val in parent.Values)
            {
                val.Parent = parent;
            }
        }
        private static void SetParents(string prefix, DescriptorProto parent)
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
            foreach (var ext in parent.Extensions)
            {
                ext.Parent = parent;
            }
        }
        private static void SetParents(string prefix, ServiceDescriptorProto parent)
        {
            var fqn = parent.FullyQualifiedName = prefix + "." + parent.Name;
            foreach (var method in parent.Methods)
            {
                method.Parent = parent;
                method.FullyQualifiedName = fqn + "." + method.Name;
            }
        }
        internal void BuildTypeHierarchy(FileDescriptorSet set)
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
            foreach (var type in Services)
            {
                type.Parent = this;
                SetParents(prefix, type);
            }
            foreach (var type in Extensions)
            {
                type.Parent = this;
            }
        }

        private static bool ShouldResolveType(FieldDescriptorProto.Type type) => type switch
        {
            0 or FieldDescriptorProto.Type.TypeMessage or FieldDescriptorProto.Type.TypeEnum or FieldDescriptorProto.Type.TypeGroup => true,
            _ => false,
        };
        private void ResolveTypes(ParserContext ctx, List<FieldDescriptorProto> fields, IType parent, bool options)
        {
            foreach (var field in fields)
            {
                if (options)
                {
                    ResolveOptions(ctx, field.Options);
                }
                else
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
                            field.ResolvedType = msg;
                        }
                        else if (TryResolveEnum(field.TypeName, parent, out var @enum, true))
                        {
                            field.type = FieldDescriptorProto.Type.TypeEnum;
                            if (!string.IsNullOrWhiteSpace(field.DefaultValue)
                                & !@enum.Values.Any(x => x.Name == field.DefaultValue))
                            {
                                ctx.Errors.Error(field.TypeToken, $"enum {@enum.Name} does not contain value '{field.DefaultValue}'", ErrorCode.EnumValueNotFound);
                            }
                            fqn = @enum?.FullyQualifiedName;
                            field.ResolvedType = @enum;
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

                    if (field.Options?.Packed ?? false)
                    {
                        bool canPack = FieldDescriptorProto.CanPack(field.type);
                        if (!canPack)
                        {
                            ctx.Errors.Error(field.TypeToken, $"field of type {field.type} cannot be packed", ErrorCode.PackedFieldInvalidType);
                            field.Options.Packed = false;
                        }
                    }
                }
            }
        }

        private void ResolveTypes(ParserContext ctx, ServiceDescriptorProto service, bool options)
        {
            if (options) ResolveOptions(ctx, service.Options);

            foreach (var method in service.Methods)
            {
                if (options)
                {
                    ResolveOptions(ctx, method.Options);
                }
                else
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
        }

        private void ResolveTypes(ParserContext ctx, DescriptorProto type, bool options)
        {
            if (options)
            {
                ResolveOptions(ctx, type.Options);
                foreach (var decl in type.OneofDecls)
                    ResolveOptions(ctx, decl.Options);
            }

            ResolveTypes(ctx, type.Fields, type, options);
            ResolveTypes(ctx, type.Extensions, type, options);
            foreach (var nested in type.NestedTypes)
            {
                ResolveTypes(ctx, nested, options);
            }
            foreach (var nested in type.EnumTypes)
            {
                ResolveTypes(ctx, nested, options);
            }
        }

        IEnumerable<string> IHazNames.GetNames()
        {
            foreach (var type in MessageTypes) yield return type.Name;
            foreach (var type in EnumTypes) yield return type.Name;
        }
        internal void ResolveTypes(ParserContext ctx, bool options)
        {
            if (options) ResolveOptions(ctx, Options);
            foreach (var type in MessageTypes)
            {
                ResolveTypes(ctx, type, options);
            }
            foreach (var type in EnumTypes)
            {
                ResolveTypes(ctx, type, options);
            }
            foreach (var service in Services)
            {
                ResolveTypes(ctx, service, options);
            }
            ResolveTypes(ctx, Extensions, this, options);

            if (options) // can only process deps on the second pass, once options have been resolved
            {
                HashSet<string> publicDependencies = null;
                foreach (var import in _imports)
                {
                    if (!Dependencies.Contains(import.Path))
                        Dependencies.Add(import.Path);
                    if (import.IsPublic)
                    {
                        (publicDependencies ??= new HashSet<string>()).Add(import.Path);
                    }
                    if (IncludeInOutput && !import.Used)
                    {
                        ctx.Errors.Warn(import.Token, $"import not used: '{import.Path}'", ErrorCode.ImportNotUsed);
                    }
                }
                // note that Dependencies should stay in declaration order to be consistent with protoc
                if (publicDependencies != null)
                {
                    var arr = publicDependencies.Select(path => Dependencies.IndexOf(path)).ToArray();
                    Array.Sort(arr);
                    PublicDependencies = arr;
                }
            }
        }

        private void ResolveTypes(ParserContext ctx, EnumDescriptorProto type, bool options)
        {
            if (options)
            {
                ResolveOptions(ctx, type.Options);
                foreach (var val in type.Values)
                {
                    ResolveOptions(ctx, val.Options);
                }
            }
        }

        private void ResolveOptions(ParserContext ctx, ISchemaOptions options)
        {
            if (options == null || options.UninterpretedOptions.Count == 0) return;

            var extension = ((IExtensible)options).GetExtensionObject(true);
            var target = extension.BeginAppend();
            try
            {
                var state = ProtoWriter.State.Create(target, null, null);
                try
                {
                    var hive = OptionHive.Build(options.UninterpretedOptions);

                    // first pass is used to sort the fields so we write them in the right order
                    AppendOptions(this, ref state, ctx, options.Extendee, hive.Children, true, 0, false);
                    // second pass applies the data
                    AppendOptions(this, ref state, ctx, options.Extendee, hive.Children, false, 0, false);
                    state.Close();
                }
                finally
                {
                    state.Dispose();
                }
                options.UninterpretedOptions.RemoveAll(x => x.Applied);
            }
            finally
            {
                extension.EndAppend(target, true);
            }
        }

        private class OptionHive
        {
            public OptionHive(string name, bool isExtension, Token token)
            {
                Name = name;
                IsExtension = isExtension;
                Token = token;
            }
            public override string ToString()
            {
                var sb = new StringBuilder();
                Concat(sb);
                return sb.ToString();
            }
            private void Concat(StringBuilder sb)
            {
                bool isFirst = true;
                foreach (var value in Options)
                {
                    if (!isFirst) sb.Append(", ");
                    isFirst = false;
                    sb.Append(value);
                }
                foreach (var child in Children)
                {
                    if (!isFirst) sb.Append(", ");
                    sb.Append(child.Name).Append("={");
                    child.Concat(sb);
                    sb.Append("}");
                }
            }
            public bool IsExtension { get; }
            public string Name { get; }
            public Token Token { get; }
            public List<UninterpretedOption> Options { get; } = new List<UninterpretedOption>();
            public List<OptionHive> Children { get; } = new List<OptionHive>();
            public FieldDescriptorProto Field { get; set; }

            public static OptionHive Build(List<UninterpretedOption> options)
            {
                if (options == null || options.Count == 0) return null;

                var root = new OptionHive(null, false, default);
                foreach (var option in options)
                {
                    var level = root;
                    OptionHive nextLevel = null;
                    foreach (var name in option.Names)
                    {
                        nextLevel = level.Children.Find(x => x.Name == name.name_part && x.IsExtension == name.IsExtension);
                        if (nextLevel == null)
                        {
                            nextLevel = new OptionHive(name.name_part, name.IsExtension, name.Token);
                            level.Children.Add(nextLevel);
                        }
                        level = nextLevel;
                    }
                    level.Options.Add(option);
                }
                return root;
            }
        }
        private static void AppendOptions(FileDescriptorProto file, ref ProtoWriter.State state, ParserContext ctx, string extendee, List<OptionHive> options, bool resolveOnly, int depth, bool messageSet)
        {
            foreach (var option in options)
                AppendOption(file, ref state, ctx, extendee, option, resolveOnly, depth, messageSet);

            if (resolveOnly && depth != 0) // fun fact: proto writes root fields in *file* order, but sub-fields in *field* order
            {
                // ascending field order
                options.Sort((x, y) => (x.Field?.Number ?? 0).CompareTo(y.Field?.Number ?? 0));
            }
        }
        private static void AppendOption(FileDescriptorProto file, ref ProtoWriter.State state, ParserContext ctx, string extendee, OptionHive option, bool resolveOnly, int depth, bool messageSet)
        {
            static bool ShouldWrite(FieldDescriptorProto f, string v, string d)
                => f.label != FieldDescriptorProto.Label.LabelOptional || v != (f.DefaultValue ?? d);

            // resolve the field for this level
            FieldDescriptorProto field = option.Field;
            if (field != null)
            {
                // already resolved
            }
            else if (option.IsExtension)
            {
                if (!file.TryResolveExtension(extendee, option.Name, out field)) field = null;
            }
            else if (file.TryResolveMessage(extendee, null, out var msg, true))
            {
                field = msg.Fields.Find(x => x.Name == option.Name);
            }
            else
            {
                field = null;
            }

            if (field == null)
            {
                if (!resolveOnly)
                {
                    ctx.Errors.Error(option.Token, $"unable to resolve custom option '{option.Name}' for '{extendee}'", ErrorCode.MissingCustomOption);
                }
                return;
            }
            option.Field = field;

            switch (field.type)
            {
                case FieldDescriptorProto.Type.TypeMessage:
                case FieldDescriptorProto.Type.TypeGroup:
                    var nextFile = GetFile(field.Parent as IType);
                    var nextMessageSet = !resolveOnly && nextFile.TryResolveMessage(field.TypeName, null, out var fieldType, true)
                        && (fieldType.Options?.MessageSetWireFormat ?? false);

                    if (option.Children.Count != 0)
                    {
#pragma warning disable CS0618 // legacy StartSubItem API
                        if (resolveOnly)
                        {
                            AppendOptions(nextFile, ref state, ctx, field.TypeName, option.Children, resolveOnly, depth + 1, nextMessageSet);
                        }
                        else if (messageSet)
                        {
                            state.WriteFieldHeader(1, WireType.StartGroup);
                            var grp = state.StartSubItem(null);

                            state.WriteFieldHeader(2, WireType.Varint);
                            state.WriteInt32(field.Number);

                            state.WriteFieldHeader(3, WireType.String);
                            var payload = state.StartSubItem(null);

                            AppendOptions(nextFile, ref state, ctx, field.TypeName, option.Children, resolveOnly, depth + 1, nextMessageSet);

                            state.EndSubItem(payload);
                            state.EndSubItem(grp);
                        }
                        else
                        {
                            state.WriteFieldHeader(field.Number,
                                field.type == FieldDescriptorProto.Type.TypeGroup ? WireType.StartGroup : WireType.String);
                            var tok = state.StartSubItem(null);

                            AppendOptions(nextFile, ref state, ctx, field.TypeName, option.Children, resolveOnly, depth + 1, nextMessageSet);

                            state.EndSubItem(tok);
                        }
                    }
                    if (resolveOnly) return; // nothing more to do

                    if (option.Options.Count == 1 && !option.Options.Single().ShouldSerializeAggregateValue())
                    {
                        // need to write an empty object to match protoc
                        if (messageSet)
                        {
                            state.WriteFieldHeader(1, WireType.StartGroup);
                            var grp = state.StartSubItem(null);

                            state.WriteFieldHeader(2, WireType.Varint);
                            state.WriteInt32(field.Number);

                            state.WriteFieldHeader(3, WireType.String);
                            var payload = state.StartSubItem(null);
                            state.EndSubItem(payload);
                            state.EndSubItem(grp);
                        }
                        else
                        {
                            state.WriteFieldHeader(field.Number,
                                   field.type == FieldDescriptorProto.Type.TypeGroup ? WireType.StartGroup : WireType.String);
                            var payload = state.StartSubItem(null);
                            state.EndSubItem(payload);
                        }
                        option.Options.Single().Applied = true;
#pragma warning restore CS0618 // legacy StartSubItem API
                    }
                    else
                    {
                        foreach (var values in option.Options)
                        {
                            ctx.Errors.Error(option.Token, $"unable to assign custom option '{option.Name}' for '{extendee}'", ErrorCode.MissingCustomOption);
                        }
                    }
                    break;
                default:
                    if (resolveOnly) return; // nothing more to do

                    foreach (var child in option.Children)
                    {
                        ctx.Errors.Error(option.Token, $"unable to assign custom option '{child.Name}' for '{extendee}'", ErrorCode.MissingCustomOption);
                    }
                    foreach (var value in option.Options)
                    {
                        int i32;
                        switch (field.type)
                        {
                            case FieldDescriptorProto.Type.TypeFloat:
                                if (!TokenExtensions.TryParseSingle(value.AggregateValue, out var f32))
                                {
                                    ctx.Errors.Error(option.Token, $"invalid value for floating point '{field.TypeName}': '{option.Name}' = '{value.AggregateValue}'", ErrorCode.InvalidFloatingPoint);
                                    continue;
                                }
                                if (ShouldWrite(field, value.AggregateValue, "0"))
                                {
                                    state.WriteFieldHeader(field.Number, WireType.Fixed32);
                                    state.WriteSingle(f32);
                                }
                                break;
                            case FieldDescriptorProto.Type.TypeDouble:
                                if (!TokenExtensions.TryParseDouble(value.AggregateValue, out var f64))
                                {
                                    ctx.Errors.Error(option.Token, $"invalid value for floating point '{field.TypeName}': '{option.Name}' = '{value.AggregateValue}'", ErrorCode.InvalidFloatingPoint);
                                    continue;
                                }
                                if (ShouldWrite(field, value.AggregateValue, "0"))
                                {
                                    state.WriteFieldHeader(field.Number, WireType.Fixed64);
                                    state.WriteDouble(f64);
                                }
                                break;
                            case FieldDescriptorProto.Type.TypeBool:
                                switch (value.AggregateValue)
                                {
                                    case "true":
                                        i32 = 1;
                                        break;
                                    case "false":
                                        i32 = 0;
                                        break;
                                    default:
                                        ctx.Errors.Error(option.Token, $"invalid value for boolean '{field.TypeName}': '{option.Name}' = '{value.AggregateValue}'", ErrorCode.InvalidBoolean);
                                        continue;
                                }
                                if (ShouldWrite(field, value.AggregateValue, "false"))
                                {
                                    state.WriteFieldHeader(field.Number, WireType.Varint);
                                    state.WriteInt32(i32);
                                }
                                break;
                            case FieldDescriptorProto.Type.TypeUint32:
                            case FieldDescriptorProto.Type.TypeFixed32:
                                {
                                    if (!TokenExtensions.TryParseUInt32(value.AggregateValue, out var ui32))
                                    {
                                        ctx.Errors.Error(option.Token, $"invalid value for unsigned integer '{field.TypeName}': '{option.Name}' = '{value.AggregateValue}'", ErrorCode.InvalidInteger);
                                        continue;
                                    }
                                    if (ShouldWrite(field, value.AggregateValue, "0"))
                                    {
                                        switch (field.type)
                                        {
                                            case FieldDescriptorProto.Type.TypeUint32:
                                                state.WriteFieldHeader(field.Number, WireType.Varint);
                                                break;
                                            case FieldDescriptorProto.Type.TypeFixed32:
                                                state.WriteFieldHeader(field.Number, WireType.Fixed32);
                                                break;
                                        }
                                        state.WriteUInt32(ui32);
                                    }
                                }
                                break;
                            case FieldDescriptorProto.Type.TypeUint64:
                            case FieldDescriptorProto.Type.TypeFixed64:
                                {
                                    if (!TokenExtensions.TryParseUInt64(value.AggregateValue, out var ui64))
                                    {
                                        ctx.Errors.Error(option.Token, $"invalid value for unsigned integer '{field.TypeName}': '{option.Name}' = '{value.AggregateValue}'", ErrorCode.InvalidInteger);
                                        continue;
                                    }
                                    if (ShouldWrite(field, value.AggregateValue, "0"))
                                    {
                                        switch (field.type)
                                        {
                                            case FieldDescriptorProto.Type.TypeUint64:
                                                state.WriteFieldHeader(field.Number, WireType.Varint);
                                                break;
                                            case FieldDescriptorProto.Type.TypeFixed64:
                                                state.WriteFieldHeader(field.Number, WireType.Fixed64);
                                                break;
                                        }
                                        state.WriteUInt64(ui64);
                                    }
                                }
                                break;
                            case FieldDescriptorProto.Type.TypeInt32:
                            case FieldDescriptorProto.Type.TypeSint32:
                            case FieldDescriptorProto.Type.TypeSfixed32:
                                if (!TokenExtensions.TryParseInt32(value.AggregateValue, out i32))
                                {
                                    ctx.Errors.Error(option.Token, $"invalid value for integer '{field.TypeName}': '{option.Name}' = '{value.AggregateValue}'", ErrorCode.InvalidInteger);
                                    continue;
                                }
                                if (ShouldWrite(field, value.AggregateValue, "0"))
                                {
                                    switch (field.type)
                                    {
                                        case FieldDescriptorProto.Type.TypeInt32:
                                            state.WriteFieldHeader(field.Number, WireType.Varint);
                                            break;
                                        case FieldDescriptorProto.Type.TypeSint32:
                                            state.WriteFieldHeader(field.Number, WireType.SignedVarint);
                                            break;
                                        case FieldDescriptorProto.Type.TypeSfixed32:
                                            state.WriteFieldHeader(field.Number, WireType.Fixed32);
                                            break;
                                    }
                                    state.WriteInt32(i32);
                                }
                                break;
                            case FieldDescriptorProto.Type.TypeInt64:
                            case FieldDescriptorProto.Type.TypeSint64:
                            case FieldDescriptorProto.Type.TypeSfixed64:
                                {
                                    if (!TokenExtensions.TryParseInt64(value.AggregateValue, out var i64))
                                    {
                                        ctx.Errors.Error(option.Token, $"invalid value for integer '{field.TypeName}': '{option.Name}' = '{value.AggregateValue}'", ErrorCode.InvalidInteger);
                                        continue;
                                    }
                                    if (ShouldWrite(field, value.AggregateValue, "0"))
                                    {
                                        switch (field.type)
                                        {
                                            case FieldDescriptorProto.Type.TypeInt64:
                                                state.WriteFieldHeader(field.Number, WireType.Varint);
                                                break;
                                            case FieldDescriptorProto.Type.TypeSint64:
                                                state.WriteFieldHeader(field.Number, WireType.SignedVarint);
                                                break;
                                            case FieldDescriptorProto.Type.TypeSfixed64:
                                                state.WriteFieldHeader(field.Number, WireType.Fixed64);
                                                break;
                                        }
                                        state.WriteInt64(i64);
                                    }
                                }
                                break;
                            case FieldDescriptorProto.Type.TypeEnum:
                                if (file.TryResolveEnum(field.TypeName, null, out var @enum, true, true))
                                {
                                    var found = @enum.Values.Find(x => x.Name == value.AggregateValue);
                                    if (found == null)
                                    {
                                        ctx.Errors.Error(option.Token, $"invalid value for enum '{field.TypeName}': '{option.Name}' = '{value.AggregateValue}'", ErrorCode.EnumValueNotFound);
                                        continue;
                                    }
                                    else
                                    {
                                        if (ShouldWrite(field, value.AggregateValue, @enum.Values.FirstOrDefault()?.Name))
                                        {
                                            state.WriteFieldHeader(field.Number, WireType.Varint);
                                            state.WriteInt32(found.Number);
                                        }
                                    }
                                }
                                else
                                {
                                    ctx.Errors.Error(option.Token, $"unable to resolve enum '{field.TypeName}': '{option.Name}' = '{value.AggregateValue}'", ErrorCode.EnumValueNotFound);
                                    continue;
                                }
                                break;
                            case FieldDescriptorProto.Type.TypeString:
                            case FieldDescriptorProto.Type.TypeBytes:
                                if (ShouldWrite(field, value.AggregateValue, ""))
                                {
                                    state.WriteFieldHeader(field.Number, WireType.String);
                                    if (value.AggregateValue == null || value.AggregateValue.IndexOf('\\') < 0)
                                    {
                                        state.WriteString(value.AggregateValue ?? "");
                                    }
                                    else
                                    {
                                        using var ms = new MemoryStream(value.AggregateValue.Length);
                                        if (!LoadBytes(ms, value.AggregateValue, out bool haveRawUnicode))
                                        {
                                            ctx.Errors.Error(option.Token, $"invalid escape sequence '{field.TypeName}': '{option.Name}' = '{value.AggregateValue}'", ErrorCode.InvalidEscapeSequence);
                                            state.WriteBytes(Array.Empty<byte>()); // just to silence the writer, so the real error is surfaced
                                            continue;
                                        }

                                        if (field.type == FieldDescriptorProto.Type.TypeString && haveRawUnicode)
                                        {
                                            TokenExtensions.ValidateUtf8(option.Token, ms, ctx.Errors); // still written, note
                                        }
                                        state.WriteBytes(new ReadOnlyMemory<byte>(ms.GetBuffer(), 0, (int)ms.Length));
                                    }
                                }
                                break;
                            default:
                                ctx.Errors.Error(option.Token, $"{field.type} options not yet implemented: '{option.Name}' = '{value.AggregateValue}'", ErrorCode.OptionsNotImplemented);
                                continue;
                        }
                        value.Applied = true;
                    }
                    break;
            }
        }

        private static unsafe bool LoadBytes(Stream ms, string value, out bool haveRawUnicode)
        {
            bool isEscaped = haveRawUnicode = false;
            byte* b = stackalloc byte[10];
            int octalCount = 0;
            uint pendingOctal = 0;
            foreach (char c in value)
            {
                if (isEscaped)
                {
                    // only a few things remain escaped after ConsumeString:
                    if (octalCount != 0 || c >= '0' && c <= '7')
                    {   // always written as 3 chars
                        if (!TokenExtensions.GetHexValue(c, out uint val, ref octalCount)) return false;
                        pendingOctal = (pendingOctal << 3) | val;
                        if (octalCount == 3)
                        {
                            ms.WriteByte(checked((byte)pendingOctal));
                            if (pendingOctal > 127) haveRawUnicode = true;
                            octalCount = 0;
                            pendingOctal = 0;
                            isEscaped = false;
                        }
                    }
                    else
                    {
                        switch (c)
                        {
                            case '\\': ms.WriteByte((byte)'\\'); break;
                            case '\'': ms.WriteByte((byte)'\''); break;
                            case '"': ms.WriteByte((byte)'"'); break;
                            case 'r': ms.WriteByte((byte)'\r'); break;
                            case 'n': ms.WriteByte((byte)'\n'); break;
                            case 't': ms.WriteByte((byte)'\t'); break;
                            default: return false;
                        }
                        isEscaped = false;
                    }
                }
                else if (c == '\\')
                {
                    isEscaped = true;
                }
                else
                {
                    var x = c; // can't take address of readonly local
                    int bytes = Encoding.UTF8.GetBytes(&x, 1, b, 10);
                    for (int i = 0; i < bytes; i++)
                    {
                        ms.WriteByte(b[i]);
                    }
                }
            }
            return !isEscaped;
        }
    }

    /// <summary>
    /// Describes an enum type.
    /// </summary>
    public partial class EnumDescriptorProto : ISchemaObject, IType, IReserved<EnumDescriptorProto.EnumReservedRange, EnumValueDescriptorProto>
    {
        /// <summary>
        /// Range of reserved numeric values. Reserved values may not be used by
        /// entries in the same enum. Reserved ranges may not overlap.
        ///
        /// Note that this is distinct from DescriptorProto.ReservedRange in that it
        /// is inclusive such that it can appropriately represent the entire int32
        /// domain.
        /// </summary>
        public partial class EnumReservedRange : IReservedRange { }

        /// <inheritdoc/>
        public override string ToString() => Name;
        internal IType Parent { get; set; }
        string IType.FullyQualifiedName => FullyQualifiedName;
        IType IType.Parent => Parent;
        IType IType.Find(string name) => null;
        internal string FullyQualifiedName { get; set; }

        List<EnumValueDescriptorProto> IReserved<EnumReservedRange, EnumValueDescriptorProto>.Fields => Values;

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
                Options = ctx.ParseOptionStatement(Options, this);
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "reserved"))
            {
                DescriptorProto.ParseReservedRanges(ctx, this, "enum", int.MaxValue, false);
            }
            else
            {
                Values.Add(EnumValueDescriptorProto.Parse(ctx));
            }
            ctx.AbortState = AbortState.None;
        }
    }

    /// <summary>
    /// Describes a field within a message.
    /// </summary>
    public partial class FieldDescriptorProto : ISchemaObject, IField
    {
        /// <summary>
        /// The resolved type if available.
        /// </summary>
        internal IType ResolvedType { get; set; }

        /// <summary>
        /// Indicates whether this field is considered "packed" in the given schema
        /// </summary>
        [Obsolete(FileDescriptorSet.NotIntendedForPublicUse, false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public bool IsPacked(string syntax)
            => IsPackedField(syntax);

        internal bool IsPackedField(string syntax)
        {
            if (label != Label.LabelRepeated) return false;

            var exp = Options?.Packed;
            if (exp.HasValue) return exp.GetValueOrDefault();

            return syntax != FileDescriptorProto.SyntaxProto2 && FieldDescriptorProto.CanPack(type);
        }

        /// <inheritdoc/>
        public override string ToString() => Name;
        internal const int DefaultMaxField = 536870911;
        internal const int FirstReservedField = 19000;
        internal const int LastReservedField = 19999;

        internal IMessage Parent { get; set; }
        internal Token TypeToken { get; set; }

        internal int MaxField => Parent?.MaxField ?? DefaultMaxField;

        internal static bool TryParse(ParserContext ctx, IMessage parent, bool isOneOf, out FieldDescriptorProto field)
        {
            void NotAllowedOneOf(ParserContext context, ErrorCode errorCode)
            {
                var token = ctx.Tokens.Previous;
                context.Errors.Error(token, $"'{token.Value}' not allowed with 'oneof'", errorCode);
            }
            var tokens = ctx.Tokens;
            ctx.AbortState = AbortState.Statement;
            Label label = Label.LabelOptional; // default
            bool explicitOptional = false;
            if (tokens.ConsumeIf(TokenType.AlphaNumeric, "repeated"))
            {
                if (isOneOf) NotAllowedOneOf(ctx, ErrorCode.OneOfRepeated);
                label = Label.LabelRepeated;
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "required"))
            {
                if (isOneOf) NotAllowedOneOf(ctx, ErrorCode.OneOfRequired);
                else tokens.Previous.RequireProto2(ctx);
                label = Label.LabelRequired;
            }
            else if (tokens.ConsumeIf(TokenType.AlphaNumeric, "optional"))
            {
                if (isOneOf) NotAllowedOneOf(ctx, ErrorCode.OneOfOptional);
                // proto3 now supports optional
                label = Label.LabelOptional;
                explicitOptional = true;
            }
            else if (ctx.Syntax == FileDescriptorProto.SyntaxProto2 && !isOneOf)
            {
                // required in proto2
                throw tokens.Read().Throw(ErrorCode.ExpectedArity, "expected 'repeated' / 'required' / 'optional'");
            }

            var typeToken = tokens.Read();
            if (typeToken.Is(TokenType.AlphaNumeric, "map"))
            {
                tokens.Previous.Throw(ErrorCode.InvalidMapUsage, $"'{tokens.Previous.Value}' can not be used with 'map'");
            }
            string typeName = tokens.Consume(TokenType.AlphaNumeric);

            var isGroup = typeName == "group";
            if (isGroup)
            {
                //if (isOneOf) NotAllowedOneOf(ctx);
                //else if (parentTyped == null)
                //{
                //    ctx.Errors.Error(tokens.Previous, "group not allowed in this context");
                //}
                ctx.AbortState = AbortState.Object;
            }

            string name = tokens.Consume(TokenType.AlphaNumeric);
            var nameToken = tokens.Previous;
            tokens.Consume(TokenType.Symbol, "=");
            var number = tokens.ConsumeInt32();
            var numberToken = tokens.Previous;

            if (number < 1 || number > parent.MaxField)
            {
                ctx.Errors.Error(numberToken, $"field numbers must be in the range 1-{parent.MaxField}", ErrorCode.FieldNumberInvalid);
            }
            else if (number >= FirstReservedField && number <= LastReservedField)
            {
                ctx.Errors.Warn(numberToken, $"field numbers in the range {FirstReservedField}-{LastReservedField} are reserved; this may cause problems on many implementations", ErrorCode.FieldNumberReserved);
            }
            ctx.CheckNames(parent, name, nameToken);
            if (parent is DescriptorProto parentTyped)
            {
                var conflict = parentTyped.Fields.Find(x => x.Number == number);
                if (conflict != null)
                {
                    ctx.Errors.Error(numberToken, $"field {number} is already in use by '{conflict.Name}'", ErrorCode.FieldDuplicatedNumber);
                }
                if (parentTyped.ReservedNames.Contains(name))
                {
                    ctx.Errors.Error(nameToken, $"field '{name}' is reserved", ErrorCode.FieldNameReserved);
                }
                if (parentTyped.ReservedRanges.Any(x => x.Start <= number && x.End > number))
                {
                    ctx.Errors.Error(numberToken, $"field {number} is reserved", ErrorCode.FieldNumberReserved);
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
                    ctx.Errors.Error(nameToken, "group names must start with an upper-case letter", ErrorCode.GroupNamesStartUpperCase);
                }
                name = typeName.ToLowerInvariant();
                if (ctx.TryReadObject<DescriptorProto>(out var grpType))
                {
                    grpType.Name = typeName;
                    ctx.CheckNames(parent, typeName, nameToken);
                    parent?.Types?.Add(grpType);
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
            if (field.label == Label.LabelOptional && explicitOptional && ctx.Syntax != FileDescriptorProto.SyntaxProto2)
            {
                field.Proto3Optional = true;
            }

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
        private static readonly char[] Underscores = { '_' };
        internal static string GetJsonName(string name)
            => Regex.Replace(name, "_+([0-9a-zA-Z])", match => match.Groups[1].Value.ToUpperInvariant()).TrimEnd(Underscores);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "Readability")]
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
            static bool Assign(Type @in, out Type @out)
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
                    type = default;
                    return false;
            }
        }

        internal static void ParseExtensions(ParserContext ctx, IMessage message)
        {
            var extendee = ctx.Tokens.Consume(TokenType.AlphaNumeric);
            var dummy = new DummyExtensions(extendee, message);
            ctx.TryReadObjectImpl(dummy);
        }

        void ISchemaObject.ReadOne(ParserContext ctx) => throw new InvalidOperationException();

        private class DummyExtensions : ISchemaObject, IHazNames, IMessage
        {
            int IMessage.MaxField => message.MaxField;
            List<DescriptorProto> IMessage.Types => message.Types;
            List<FieldDescriptorProto> IMessage.Extensions => message.Extensions;
            List<FieldDescriptorProto> IMessage.Fields => message.Fields;
            public byte[] ExtensionData
            {
                get { return null; }
                set { }
            }
            IEnumerable<string> IHazNames.GetNames()
            {
                var fields = message.Fields;
                if (fields != null)
                {
                    foreach (var field in fields) yield return field.Name;
                }
                foreach (var field in message.Extensions) yield return field.Name;
                foreach (var type in message.Types) yield return type.Name;
            }

            void ISchemaObject.ReadOne(ParserContext ctx)
            {
                ctx.AbortState = AbortState.Statement;
                if (TryParse(ctx, this, false, out var field))
                {
                    field.Extendee = extendee;
                    message.Extensions.Add(field);
                }
                ctx.AbortState = AbortState.None;
            }

            private readonly IMessage message;
            private readonly string extendee;

            public DummyExtensions(string extendee, IMessage message)
            {
                this.extendee = extendee;
                this.message = message;
            }
        }
    }

    internal interface IMessage : IHazNames
    {
        int MaxField { get; }
        List<DescriptorProto> Types { get; }
        List<FieldDescriptorProto> Extensions { get; }
        List<FieldDescriptorProto> Fields { get; }
    }

    /// <summary>
    /// Describes a service.
    /// </summary>
    public partial class ServiceDescriptorProto : ISchemaObject, IType
    {
        /// <inheritdoc/>
        public override string ToString() => Name;
        internal IType Parent { get; set; }
        string IType.FullyQualifiedName => FullyQualifiedName;
        IType IType.Parent => Parent;
        IType IType.Find(string name) => Methods.Find(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        internal string FullyQualifiedName { get; set; }

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
                Options = ctx.ParseOptionStatement(Options, this);
            }
            else
            {
                // is a method
                Methods.Add(MethodDescriptorProto.Parse(ctx));
            }
            ctx.AbortState = AbortState.None;
        }
    }

    /// <summary>
    /// Describes a method of a service.
    /// </summary>
    public partial class MethodDescriptorProto : ISchemaObject, IType
    {
        /// <inheritdoc/>
        public override string ToString() => Name;
        internal IType Parent { get; set; }
        string IType.FullyQualifiedName => FullyQualifiedName;
        IType IType.Parent => Parent;
        IType IType.Find(string name) => null;
        internal string FullyQualifiedName { get; set; }

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
                method.Options ??= new MethodOptions(); // protoc always initializes this, even if none found
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
            Options = ctx.ParseOptionStatement(Options, this);
        }
    }

    /// <summary>
    /// Describes a value within an enum.
    /// </summary>
    public partial class EnumValueDescriptorProto : IField
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

    /// <summary>
    /// Options relating to a message
    /// </summary>
    public partial class MessageOptions : ISchemaOptions
    {
        string ISchemaOptions.Extendee => FileDescriptorSet.Namespace + nameof(MessageOptions);
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key)
        {
            switch (key)
            {
                case "map_entry":
                    MapEntry = ctx.Tokens.ConsumeBoolean();
                    ctx.Errors.Error(ctx.Tokens.Previous, "'map_entry' should not be set explicitly; use 'map<TKey,TValue>' instead", ErrorCode.MapUseMapEntry);
                    return true;
                case "message_set_wire_format": MessageSetWireFormat = ctx.Tokens.ConsumeBoolean(); return true;
                case "no_standard_descriptor_accessor": NoStandardDescriptorAccessor = ctx.Tokens.ConsumeBoolean(); return true;
                default: return false;
            }
        }
        /// <summary>
        /// Gets or sets the extension data as a byte-array
        /// </summary>
        /// <remarks>This is required for equivalence tests vs 'protoc'</remarks>
        [Obsolete(FileDescriptorSet.NotIntendedForPublicUse, false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public byte[] ExtensionData
        {
            get { return DescriptorProto.GetRawExtensionData(this); }
            set { DescriptorProto.SetRawExtensionData(this, value); }
        }
    }

    /// <summary>
    /// Options relating to a method
    /// </summary>
    public partial class MethodOptions : ISchemaOptions
    {
        string ISchemaOptions.Extendee => FileDescriptorSet.Namespace + nameof(MethodOptions);
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key)
        {
            switch (key)
            {
                case "idempotency_level": idempotency_level = ctx.Tokens.ConsumeEnum<IdempotencyLevel>(); return true;
                default: return false;
            }
        }
        /// <summary>
        /// Gets or sets the extension data as a byte-array
        /// </summary>
        /// <remarks>This is required for equivalence tests vs 'protoc'</remarks>
        [Obsolete(FileDescriptorSet.NotIntendedForPublicUse, false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public byte[] ExtensionData
        {
            get { return DescriptorProto.GetRawExtensionData(this); }
            set { DescriptorProto.SetRawExtensionData(this, value); }
        }
    }

    /// <summary>
    /// Options relating to a service
    /// </summary>
    public partial class ServiceOptions : ISchemaOptions
    {
        string ISchemaOptions.Extendee => FileDescriptorSet.Namespace + nameof(ServiceOptions);
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key) => false;

        /// <summary>
        /// Gets or sets the extension data as a byte-array
        /// </summary>
        /// <remarks>This is required for equivalence tests vs 'protoc'</remarks>
        [Obsolete(FileDescriptorSet.NotIntendedForPublicUse, false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public byte[] ExtensionData
        {
            get { return DescriptorProto.GetRawExtensionData(this); }
            set { DescriptorProto.SetRawExtensionData(this, value); }
        }
    }

    /// <summary>
    /// A message representing a option the parser does not recognize. This only
    /// appears in options protos created by the compiler::Parser class.
    /// DescriptorPool resolves these when building Descriptor objects. Therefore,
    /// options protos in descriptor objects (e.g. returned by Descriptor::options(),
    /// or produced by Descriptor::CopyTo()) will never have UninterpretedOptions
    /// in them.
    /// </summary>
    public partial class UninterpretedOption
    {
        /// <summary>
        /// The name of the uninterpreted option.  Each string represents a segment in
        /// a dot-separated name.  is_extension is true iff a segment represents an
        /// extension (denoted with parentheses in options specs in .proto files).
        /// E.g.,{ ["foo", false], ["bar.baz", true], ["qux", false] } represents
        /// "foo.(bar.baz).qux".
        /// </summary>
        public partial class NamePart
        {
            /// <inheritdoc/>
            public override string ToString() => IsExtension ? ("(" + name_part + ")") : name_part;
            internal Token Token { get; set; }
        }
        internal bool Applied { get; set; }
        internal Token Token { get; set; }
    }

    /// <summary>
    /// Options relating to an enum.
    /// </summary>
    public partial class EnumOptions : ISchemaOptions
    {
        string ISchemaOptions.Extendee => FileDescriptorSet.Namespace + nameof(EnumOptions);
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key)
        {
            switch (key)
            {
                case "allow_alias": AllowAlias = ctx.Tokens.ConsumeBoolean(); return true;
                default: return false;
            }
        }
        /// <summary>
        /// Gets or sets the extension data as a byte-array
        /// </summary>
        /// <remarks>This is required for equivalence tests vs 'protoc'</remarks>
        [Obsolete(FileDescriptorSet.NotIntendedForPublicUse, false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public byte[] ExtensionData
        {
            get { return DescriptorProto.GetRawExtensionData(this); }
            set { DescriptorProto.SetRawExtensionData(this, value); }
        }
    }

    /// <summary>
    /// Options relating to an enum value
    /// </summary>
    public partial class EnumValueOptions : ISchemaOptions
    {
        string ISchemaOptions.Extendee => FileDescriptorSet.Namespace + nameof(EnumValueOptions);
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key) => false;

        /// <summary>
        /// Gets or sets the extension data as a byte-array
        /// </summary>
        /// <remarks>This is required for equivalence tests vs 'protoc'</remarks>
        [Obsolete(FileDescriptorSet.NotIntendedForPublicUse, false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public byte[] ExtensionData
        {
            get { return DescriptorProto.GetRawExtensionData(this); }
            set { DescriptorProto.SetRawExtensionData(this, value); }
        }
    }

    /// <summary>
    /// Options relating to a field
    /// </summary>
    public partial class FieldOptions : ISchemaOptions
    {
        string ISchemaOptions.Extendee => FileDescriptorSet.Namespace + nameof(FieldOptions);
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

        /// <summary>
        /// Gets or sets the extension data as a byte-array
        /// </summary>
        /// <remarks>This is required for equivalence tests vs 'protoc'</remarks>
        [Obsolete(FileDescriptorSet.NotIntendedForPublicUse, false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public byte[] ExtensionData
        {
            get { return DescriptorProto.GetRawExtensionData(this); }
            set { DescriptorProto.SetRawExtensionData(this, value); }
        }
    }

    /// <summary>
    /// Options relating to a file
    /// </summary>
    public partial class FileOptions : ISchemaOptions
    {
        string ISchemaOptions.Extendee => FileDescriptorSet.Namespace + nameof(FileOptions);
        bool ISchemaOptions.ReadOne(ParserContext ctx, string key)
        {
            switch (key)
            {
                // in field order
                case "java_package": JavaPackage = ctx.Tokens.ConsumeString(); return true;
                case "java_outer_classname": JavaOuterClassname = ctx.Tokens.ConsumeString(); return true;
                case "java_multiple_files": JavaMultipleFiles = ctx.Tokens.ConsumeBoolean(); return true;
#pragma warning disable 0612
                case "java_generate_equals_and_hash": JavaGenerateEqualsAndHash = ctx.Tokens.ConsumeBoolean(); return true;
#pragma warning restore 0612
                case "java_string_check_utf8": JavaStringCheckUtf8 = ctx.Tokens.ConsumeBoolean(); return true;
                case "optimize_for": OptimizeFor = ctx.Tokens.ConsumeEnum<OptimizeMode>(); return true;
                case "go_package": GoPackage = ctx.Tokens.ConsumeString(); return true;
                case "cc_generic_services": CcGenericServices = ctx.Tokens.ConsumeBoolean(); return true;
                case "java_generic_services": JavaGenericServices = ctx.Tokens.ConsumeBoolean(); return true;
                case "py_generic_services": PyGenericServices = ctx.Tokens.ConsumeBoolean(); return true;
                case "php_generic_services": PhpGenericServices = ctx.Tokens.ConsumeBoolean(); return true;
                case "deprecated": Deprecated = ctx.Tokens.ConsumeBoolean(); return true;
                case "cc_enable_arenas": CcEnableArenas = ctx.Tokens.ConsumeBoolean(); return true;
                case "objc_class_prefix": ObjcClassPrefix = ctx.Tokens.ConsumeString(); return true;
                case "csharp_namespace": CsharpNamespace = ctx.Tokens.ConsumeString(); return true;
                case "swift_prefix": SwiftPrefix = ctx.Tokens.ConsumeString(); return true;
                case "php_class_prefix": PhpClassPrefix = ctx.Tokens.ConsumeString(); return true;
                case "php_namespace":
                    PhpNamespace = ctx.Tokens.ConsumeString(); return true;

                case "php_metadata_namespace": PhpMetadataNamespace = ctx.Tokens.ConsumeString(); return true;
                case "ruby_package": RubyPackage = ctx.Tokens.ConsumeString(); return true;
                default: return false;
            }
        }

        /// <summary>
        /// Gets or sets the extension data as a byte-array
        /// </summary>
        /// <remarks>This is required for equivalence tests vs 'protoc'</remarks>
        [Obsolete(FileDescriptorSet.NotIntendedForPublicUse, false)]
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public byte[] ExtensionData
        {
            get { return DescriptorProto.GetRawExtensionData(this); }
            set { DescriptorProto.SetRawExtensionData(this, value); }
        }
    }
}
namespace ProtoBuf.Reflection
{
    /// <summary>
    /// Utility methods for descriptors
    /// </summary>
    public static class DescriptorExtensions
    {
        /// <summary>
        /// Gets the resolved enum type associated with a field
        /// </summary>
        public static EnumDescriptorProto GetEnumType(this FieldDescriptorProto field)
            => field?.ResolvedType as EnumDescriptorProto;

        /// <summary>
        /// Gets the resolved message type associated with a field
        /// </summary>
        public static DescriptorProto GetMessageType(this FieldDescriptorProto field)
            => field?.ResolvedType as DescriptorProto;

        /// <summary>
        /// Gets the fully qualified name of an enum
        /// </summary>
        public static string GetFullyQualifiedName(this EnumDescriptorProto @enum) 
            => @enum.FullyQualifiedName;

        /// <summary>
        /// Gets the fully qualified name of a message
        /// </summary>
        public static string GetFullyQualifiedName(this DescriptorProto message) 
            => message.FullyQualifiedName;
    }
    
    internal static class ErrorExtensions
    {
        public static void Warn(this List<Error> errors, Token token, string message, ErrorCode code)
            => errors.Add(new Error(token, message, false, code));
        public static void Error(this List<Error> errors, Token token, string message, ErrorCode code)
            => errors.Add(new Error(token, message, true, code));
        public static void Error(this List<Error> errors, ParserException ex)
            => errors.Add(new Error(ex));
    }

    /// <summary>
    /// Describes a generated file
    /// </summary>
    public class CodeFile
    {
        /// <summary>
        /// Get a string representation of this instance
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Name;
        /// <summary>
        /// Create a new CodeFile instance
        /// </summary>
        public CodeFile(string name, string text)
        {
            Name = name;
            Text = text;
        }
        /// <summary>
        /// The name (including path if necessary) of this file
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The contents of this file
        /// </summary>
        public string Text { get; }
    }

    /// <summary>
    /// Represents the overall result of a compilation process
    /// </summary>
    public class CompilerResult
    {
        internal CompilerResult(Error[] errors, CodeFile[] files)
        {
            Errors = errors;
            Files = files;
        }
        /// <summary>
        /// The errors from this execution
        /// </summary>
        public Error[] Errors { get; }
        /// <summary>
        /// The output files from this execution
        /// </summary>
        public CodeFile[] Files { get; }
    }

    internal class Import
    {
        public override string ToString() => Path;
        public string Path { get; set; }
        public bool IsPublic { get; set; }
        public Token Token { get; set; }
        public bool Used { get; set; }
    }
    /// <summary>
    /// Describes an error that occurred during processing
    /// </summary>
    public class Error
    {
        /// <summary>
        /// Parse an error from a PROTOC error message
        /// </summary>
        public static Error[] Parse(string stdout, string stderr)
        {
            if (string.IsNullOrWhiteSpace(stdout) && string.IsNullOrWhiteSpace(stderr))
                return noErrors;

            List<Error> errors = new List<Error>();
            using (var reader = new StringReader(stdout))
            {
                Add(reader, errors, ErrorCode.ProtocError);
            }
            using (var reader = new StringReader(stderr))
            {
                Add(reader, errors, ErrorCode.ProtocError);
            }
            return errors.ToArray();
        }
        private static void Add(TextReader lines, List<Error> errors, ErrorCode code)
        {
            string line;
            while ((line = lines.ReadLine()) != null)
            {
                var s = line;
                bool isError = true;
                int lineNumber = 1, columnNumber = 1;
                if (s[0] == '[')
                {
                    int i = s.IndexOf(']');
                    if (i > 0)
                    {
                        var prefix = line.Substring(1, i).Trim();
                        s = line.Substring(i + 1).Trim();
                        if (prefix.IndexOf("WARNING", StringComparison.OrdinalIgnoreCase) >= 0
                            && prefix.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            isError = false;
                        }
                    }
                }
                var match = Regex.Match(s, @"^([^:]+):([0-9]+):([0-9]+):\s+");
                string file = "";
                if (match.Success)
                {
                    file = match.Groups[1].Value;
                    if (!int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out lineNumber))
                        lineNumber = 1;
                    if (!int.TryParse(match.Groups[3].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out columnNumber))
                        columnNumber = 1;
                    s = s.Substring(match.Length).Trim();
                }
                errors.Add(new Error(new Token(" ", lineNumber, columnNumber, TokenType.None, "", 0, file), s, isError, code));
            }
        }
        internal string ToString(bool includeType) => Text.Length == 0
                ? $"{File}({LineNumber},{ColumnNumber}): {(includeType ? (IsError ? "error: " : "warning: ") : "")}{Message}"
                : $"{File}({LineNumber},{ColumnNumber},{LineNumber},{ColumnNumber + Text.Length}): {(includeType ? (IsError ? "error: " : "warning: ") : "")}{Message}";
        /// <summary>
        /// Get a text representation of this instance
        /// </summary>
        /// <returns></returns>
        public override string ToString() => ToString(true);

        internal static Error[] GetArray(List<Error> errors)
            => errors.Count == 0 ? noErrors : errors.ToArray();

        private static readonly Error[] noErrors = new Error[0];

        internal Error(Token token, string message, bool isError, ErrorCode code)
        {
            ColumnNumber = token.ColumnNumber;
            LineNumber = token.LineNumber;
            File = token.File;
            LineContents = token.LineContents;
            Message = message;
            IsError = isError;
            Text = token.Value;
            TypedCode = code;
        }

        private ErrorCode TypedCode { get; }
        /// <summary>
        /// The error code defined for this scenario
        /// </summary>
        public int ErrorNumber => (int)TypedCode;
        internal Error(ParserException ex)
        {
            ColumnNumber = ex.ColumnNumber;
            LineNumber = ex.LineNumber;
            File = ex.File;
            LineContents = ex.LineContents;
            Message = ex.Message;
            IsError = ex.IsError;
            Text = ex.Text ?? "";
            TypedCode = ex.ErrorCode;
        }
        /// <summary>
        /// True if this instance represents a non-fatal warning
        /// </summary>
        public bool IsWarning => !IsError;
        /// <summary>
        /// True if this instance represents a fatal error
        /// </summary>
        public bool IsError { get; }
        /// <summary>
        /// The file in which this error was identified
        /// </summary>
        public string File { get; }
        /// <summary>
        /// The source text relating to this error
        /// </summary>
        public string Text { get; }
        /// <summary>
        /// The error message
        /// </summary>
        public string Message { get; }
        /// <summary>
        /// The entire line contents in the source in which this error was located
        /// </summary>
        public string LineContents { get; }
        /// <summary>
        /// The line number in which this error was located
        /// </summary>
        public int LineNumber { get; }
        /// <summary>
        /// The column number in which this error was located
        /// </summary>
        public int ColumnNumber { get; }
    }
    internal enum AbortState
    {
        None, Statement, Object
    }
    internal interface ISchemaOptions
    {
        List<UninterpretedOption> UninterpretedOptions { get; }
        bool Deprecated { get; set; }
        bool ReadOne(ParserContext ctx, string key);
        byte[] ExtensionData { get; set; }
        string Extendee { get; }
    }

    internal interface IHazNames
    {
        IEnumerable<string> GetNames();
    }

    internal interface ISchemaObject
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
                    Errors.Error(stateAfter, "unknown error", ErrorCode.UnknownParseError);
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
            while (tokens.Peek(out var _))
            {
                if (!tokens.ConsumeIf(TokenType.Symbol, ";"))
                {
                    ReadOne(obj);
                }
            }
        }
        internal static readonly char[] Period = { '.' };
        private void ReadOption<T>(ref T obj, ISchemaObject parent, List<UninterpretedOption.NamePart> existingNameParts = null) where T : class, ISchemaOptions, new()
        {
            var tokens = Tokens;
            bool isBlock = existingNameParts != null;
            var nameParts = isBlock
                ? new List<UninterpretedOption.NamePart>(existingNameParts) // create a clone we can append to
                : new List<UninterpretedOption.NamePart>();

            do
            {
                if (nameParts.Count != 0) tokens.ConsumeIf(TokenType.AlphaNumeric, ".");

                bool isExtension = tokens.ConsumeIf(TokenType.Symbol, isBlock ? "[" : "(");
                string key = tokens.Consume(TokenType.AlphaNumeric);
                var keyToken = tokens.Previous;
                if (isExtension) tokens.Consume(TokenType.Symbol, isBlock ? "]" : ")");

                if (!isExtension && key.StartsWith("."))
                {
                    key = key.TrimStart(Period);
                }

                key = key.Trim();
                if (isExtension || nameParts.Count == 0 || key.IndexOf('.') < 0)
                {
                    var name = new UninterpretedOption.NamePart { IsExtension = isExtension, name_part = key, Token = keyToken };
                    nameParts.Add(name);
                }
                else
                {
                    foreach (var part in key.Split(Period, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var name = new UninterpretedOption.NamePart { IsExtension = false, name_part = part, Token = keyToken };
                        nameParts.Add(name);
                    }
                }
            } while (!(
            (isBlock && tokens.Is(TokenType.Symbol, "{"))
            || tokens.ConsumeIf(TokenType.Symbol, isBlock ? ":" : "=")));

            if (tokens.ConsumeIf(TokenType.Symbol, "{"))
            {
                obj ??= new T();
                bool any = false;
                while (!tokens.ConsumeIf(TokenType.Symbol, "}"))
                {
                    ReadOption(ref obj, parent, nameParts);
                    any = true;

                    // comma between elements is optional
                    var consumedComma = tokens.ConsumeIf(TokenType.Symbol, ",");

                    // alternatively, semicolons are also valid element separators
                    if (!consumedComma)
                    {
                        tokens.ConsumeIf(TokenType.Symbol, ";");
                    }
                }
                if (!any)
                {
                    var newOption = new UninterpretedOption();
                    newOption.Names.AddRange(nameParts);
                    obj.UninterpretedOptions.Add(newOption);
                }
            }
            else
            {
                var field = parent as FieldDescriptorProto;
                bool isField = typeof(T) == typeof(FieldOptions) && field != null;
                var singleKey = (nameParts.Count == 1 && !nameParts[0].IsExtension) ? nameParts[0].name_part : null;
                if (singleKey == "default" && isField)
                {
                    string defaultValue = tokens.ConsumeString(field.type == FieldDescriptorProto.Type.TypeBytes);
                    nameParts[0].Token.RequireProto2(this);
                    ParseDefault(tokens.Previous, field.type, ref defaultValue);
                    if (defaultValue != null)
                    {
                        field.DefaultValue = defaultValue;
                    }
                }
                else if (singleKey == "json_name" && isField)
                {
                    string jsonName = tokens.ConsumeString();
                    field.JsonName = jsonName;
                }
                else
                {
                    obj ??= new T();
                    if (singleKey == "deprecated")
                    {
                        obj.Deprecated = tokens.ConsumeBoolean();
                    }
                    else if (singleKey == null || !obj.ReadOne(this, singleKey))
                    {
                        var newOption = new UninterpretedOption
                        {
                            AggregateValue = tokens.ConsumeString(true),
                            Token = tokens.Previous
                        };
                        newOption.Names.AddRange(nameParts);
                        obj.UninterpretedOptions.Add(newOption);
                    }
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
                            Errors.Error(token, "expected 'true' or 'false'", ErrorCode.InvalidBoolean);
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
                            if (TokenExtensions.TryParseDouble(defaultValue, out var val))
                            {
                                defaultValue = Format(val);
                            }
                            else
                            {
                                Errors.Error(token, "invalid floating-point number", ErrorCode.InvalidFloatingPoint);
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
                            if (TokenExtensions.TryParseSingle(defaultValue, out var val))
                            {
                                defaultValue = Format(val);
                            }
                            else
                            {
                                Errors.Error(token, "invalid floating-point number", ErrorCode.InvalidFloatingPoint);
                            }
                            break;
                    }
                    break;
                case FieldDescriptorProto.Type.TypeSfixed32:
                case FieldDescriptorProto.Type.TypeInt32:
                case FieldDescriptorProto.Type.TypeSint32:
                    {
                        if (TokenExtensions.TryParseInt32(defaultValue, out var val))
                        {
                            defaultValue = val.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            Errors.Error(token, "invalid integer", ErrorCode.InvalidInteger);
                        }
                    }
                    break;
                case FieldDescriptorProto.Type.TypeFixed32:
                case FieldDescriptorProto.Type.TypeUint32:
                    {
                        if (TokenExtensions.TryParseUInt32(defaultValue, out var val))
                        {
                            defaultValue = val.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            Errors.Error(token, "invalid unsigned integer", ErrorCode.InvalidInteger);
                        }
                    }
                    break;
                case FieldDescriptorProto.Type.TypeSfixed64:
                case FieldDescriptorProto.Type.TypeInt64:
                case FieldDescriptorProto.Type.TypeSint64:
                    {
                        if (TokenExtensions.TryParseInt64(defaultValue, out var val))
                        {
                            defaultValue = val.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            Errors.Error(token, "invalid integer", ErrorCode.InvalidInteger);
                        }
                    }
                    break;
                case FieldDescriptorProto.Type.TypeFixed64:
                case FieldDescriptorProto.Type.TypeUint64:
                    {
                        if (TokenExtensions.TryParseUInt64(defaultValue, out var val))
                        {
                            defaultValue = val.ToString(CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            Errors.Error(token, "invalid unsigned integer", ErrorCode.InvalidInteger);
                        }
                    }
                    break;
                case 0:
                case FieldDescriptorProto.Type.TypeBytes:
                case FieldDescriptorProto.Type.TypeString:
                case FieldDescriptorProto.Type.TypeEnum:
                    break;
                default:
                    Errors.Error(token, $"default value not handled: {type}={defaultValue}", ErrorCode.DefaultValueNotHandled);
                    break;
            }
        }

        private static readonly char[] ExponentChars = { 'e', 'E' };
        private static string Format(float val)
        {
            string s = val.ToString(CultureInfo.InvariantCulture);
            return s.IndexOfAny(ExponentChars) < 0 ? s
                : val.ToString("0e+00", CultureInfo.InvariantCulture);
        }

        private static string Format(double val)
        {
            string s = val.ToString(CultureInfo.InvariantCulture).ToUpperInvariant();
            return s.IndexOfAny(ExponentChars) < 0 ? s
                :  val.ToString("0e+00", CultureInfo.InvariantCulture);
        }

        public T ParseOptionBlock<T>(T obj, ISchemaObject parent = null) where T : class, ISchemaOptions, new()
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
                    else if (!tokens.ConsumeIf(TokenType.Symbol, ","))
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
        public T ParseOptionStatement<T>(T obj, ISchemaObject parent) where T : class, ISchemaOptions, new()
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
                    if (!tokens.ConsumeIf(TokenType.Symbol, ";"))
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
            // obj = null;
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
#if DEBUG && !NETFRAMEWORK
            , [System.Runtime.CompilerServices.CallerMemberName] string caller = null
#endif
            )
        {
            if (parent != null && parent.GetNames().Contains(name))
            {
                Errors.Error(token, $"name '{name}' is already in use"
#if DEBUG && !NETFRAMEWORK
             + $" ({caller})"
#endif
                     , ErrorCode.FieldDuplicatedName);
            }
        }
    }
}
