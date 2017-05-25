using Google.Protobuf.Reflection;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Google.Protobuf.Reflection
{
#pragma warning disable CS1591
    partial class FileDescriptorProto
    {
        public void GenerateCSharp(TextWriter target, NameNormalizer normalizer = null, IList<Error> errors = null)
            => Generators.GenerateCSharp(target, this, normalizer, errors);

        public string GenerateCSharp(NameNormalizer normalizer = null, IList<Error> errors = null)
        {
            using (var sw = new StringWriter())
            {
                GenerateCSharp(sw, normalizer, errors);
                return sw.ToString();
            }
        }
    }

#pragma warning restore CS1591
}

namespace ProtoBuf
{

    internal class ParserException : Exception
    {
        public int ColumnNumber { get; }
        public int LineNumber { get; }
        public string Text { get; }
        public string LineContents { get; }
        public bool IsError { get; }
        internal ParserException(Token token, string message, bool isError)
            : base(message ?? "error")
        {
            ColumnNumber = token.ColumnNumber;
            LineNumber = token.LineNumber;
            LineContents = token.LineContents;
            Text = token.Value ?? "";
            IsError = isError;
        }
    }

    public abstract class NameNormalizer
    {
        private class NullNormalizer : NameNormalizer
        {
            protected override string GetName(string identifier) => identifier;
            public override string Pluralize(string identifier) => identifier;
        }
        private class DefaultNormalizer : NameNormalizer
        {
            protected override string GetName(string identifier) => AutoCapitalize(identifier);
            public override string Pluralize(string identifier) => AutoPluralize(identifier);
        }
        protected static string AutoCapitalize(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return identifier;
            // if all upper-case, make proper-case
            if (Regex.IsMatch(identifier, @"^[_A-Z0-9]*$"))
            {
                return Regex.Replace(identifier, @"(^|_)([A-Z0-9])([A-Z0-9]*)",
                    match => match.Groups[2].Value.ToUpperInvariant() + match.Groups[3].Value.ToLowerInvariant());
            }
            // if all lower-case, make proper case
            if (Regex.IsMatch(identifier, @"^[_a-z0-9]*$"))
            {
                return Regex.Replace(identifier, @"(^|_)([a-z0-9])([a-z0-9]*)",
                    match => match.Groups[2].Value.ToUpperInvariant() + match.Groups[3].Value.ToLowerInvariant());
            }
            // just remove underscores - leave their chosen casing alone
            return identifier.Replace("_", "");
        }
        protected static string AutoPluralize(string identifier)
        {
            // horribly Anglo-centric and only covers common cases; but: is swappable

            if (string.IsNullOrEmpty(identifier) || identifier.Length == 1) return identifier;

            if (identifier.EndsWith("ss") || identifier.EndsWith("o")) return identifier + "es";
            if (identifier.EndsWith("is") && identifier.Length > 2) return identifier.Substring(0, identifier.Length - 2) + "es";

            if (identifier.EndsWith("s")) return identifier; // misses some things (bus => buses), but: might already be pluralized

            if (identifier.EndsWith("y") && identifier.Length > 2)
            {   // identity => identities etc
                switch (identifier[identifier.Length - 2])
                {
                    case 'a':
                    case 'e':
                    case 'i':
                    case 'o':
                    case 'u':
                        break; // only for consonant prefix
                    default:
                        return identifier.Substring(0, identifier.Length - 1) + "ies";
                }
            }
            return identifier + "s";
        }
        public static NameNormalizer Default { get; } = new DefaultNormalizer();
        public static NameNormalizer Null { get; } = new NullNormalizer();
        protected abstract string GetName(string identifier);
        public abstract string Pluralize(string identifier);
        public virtual string GetName(DescriptorProto definition)
            => GetName(definition.Parent, GetName(definition.Name), definition.Name, false);
        public virtual string GetName(EnumDescriptorProto definition)
            => GetName(definition.Parent, GetName(definition.Name), definition.Name, false);
        public virtual string GetName(EnumValueDescriptorProto definition) => AutoCapitalize(definition.Name);
        public virtual string GetName(FieldDescriptorProto definition)
        {
            var preferred = GetName(definition.Name);
            if (definition.label == FieldDescriptorProto.Label.LabelRepeated)
            {
                preferred = Pluralize(preferred);
            }
            return GetName(definition.Parent, preferred, definition.Name, true);
        }

        protected HashSet<string> BuildConflicts(DescriptorProto parent, bool includeDescendents)
        {
            var conflicts = new HashSet<string>();
            if (parent != null)
            {
                conflicts.Add(GetName(parent));
                if (includeDescendents)
                {
                    foreach (var type in parent.NestedTypes)
                    {
                        conflicts.Add(GetName(type));
                    }
                    foreach (var type in parent.EnumTypes)
                    {
                        conflicts.Add(GetName(type));
                    }
                }
            }
            return conflicts;
        }
        protected virtual string GetName(DescriptorProto parent, string preferred, string fallback, bool includeDescendents)
        {
            var conflicts = BuildConflicts(parent, includeDescendents);

            if (!conflicts.Contains(preferred)) return preferred;
            if (!conflicts.Contains(fallback)) return fallback;

            var attempt = preferred + "Value";
            if (!conflicts.Contains(attempt)) return attempt;

            attempt = fallback + "Value";
            if (!conflicts.Contains(attempt)) return attempt;

            int i = 1;
            while (true)
            {
                attempt = preferred + i.ToString();
                if (!conflicts.Contains(attempt)) return attempt;
            }
        }
    }
    internal static class Generators
    {
        private static string Escape(string identifier)
        {
            switch (identifier)
            {
                case "abstract":
                case "event":
                case "new":
                case "struct":
                case "as":
                case "explicit":
                case "null":
                case "switch":
                case "base":
                case "extern":
                case "object":
                case "this":
                case "bool":
                case "false":
                case "operator":
                case "throw":
                case "break":
                case "finally":
                case "out":
                case "true":
                case "byte":
                case "fixed":
                case "override":
                case "try":
                case "case":
                case "float":
                case "params":
                case "typeof":
                case "catch":
                case "for":
                case "private":
                case "uint":
                case "char":
                case "foreach":
                case "protected":
                case "ulong":
                case "checked":
                case "goto":
                case "public":
                case "unchecked":
                case "class":
                case "if":
                case "readonly":
                case "unsafe":
                case "const":
                case "implicit":
                case "ref":
                case "ushort":
                case "continue":
                case "in":
                case "return":
                case "using":
                case "decimal":
                case "int":
                case "sbyte":
                case "virtual":
                case "default":
                case "interface":
                case "sealed":
                case "volatile":
                case "delegate":
                case "internal":
                case "short":
                case "void":
                case "do":
                case "is":
                case "sizeof":
                case "while":
                case "double":
                case "lock":
                case "stackalloc":
                case "else":
                case "long":
                case "static":
                case "enum":
                case "namespace":
                case "string":
                    return "@" + identifier;
                default:
                    return identifier;
            }
        }
        private static void Write(GeneratorContext context, EnumDescriptorProto @enum)
        {
            var name = context.Normalizer.GetName(@enum);
            context.WriteLine($@"[global::ProtoBuf.ProtoContract(Name = @""{@enum.Name}"")]");
            WriteOptions(context, @enum.Options);
            context.WriteLine($"public enum {Escape(name)}").WriteLine("{").Indent();
            foreach (var val in @enum.Values)
            {
                name = context.Normalizer.GetName(val);
                context.WriteLine($@"[global::ProtoBuf.ProtoEnum(Name = @""{val.Name}"", Value = {val.Number})]");
                WriteOptions(context, val.Options);
                context.WriteLine($"{Escape(name)} = {val.Number},");
            }
            context.Outdent().WriteLine("}").WriteLine();
        }
        internal class GeneratorContext
        {
            public string Syntax => string.IsNullOrWhiteSpace(fileDescriptor.Syntax)
                ? FileDescriptorProto.SyntaxProto2 : fileDescriptor.Syntax;

            public string GetTypeName(FieldDescriptorProto field, out string dataFormat, out bool isMap)
            {
                dataFormat = "";
                isMap = false;
                switch (field.type)
                {
                    case FieldDescriptorProto.Type.TypeDouble:
                        return "double";
                    case FieldDescriptorProto.Type.TypeFloat:
                        return "float";
                    case FieldDescriptorProto.Type.TypeBool:
                        return "bool";
                    case FieldDescriptorProto.Type.TypeString:
                        return "string";
                    case FieldDescriptorProto.Type.TypeSint32:
                        dataFormat = nameof(DataFormat.ZigZag);
                        return "int";
                    case FieldDescriptorProto.Type.TypeInt32:
                        return "int";
                    case FieldDescriptorProto.Type.TypeSfixed32:
                        dataFormat = nameof(DataFormat.FixedSize);
                        return "int";
                    case FieldDescriptorProto.Type.TypeSint64:
                        dataFormat = nameof(DataFormat.ZigZag);
                        return "long";
                    case FieldDescriptorProto.Type.TypeInt64:
                        return "long";
                    case FieldDescriptorProto.Type.TypeSfixed64:
                        dataFormat = nameof(DataFormat.FixedSize);
                        return "long";
                    case FieldDescriptorProto.Type.TypeFixed32:
                        dataFormat = nameof(DataFormat.FixedSize);
                        return "uint";
                    case FieldDescriptorProto.Type.TypeUint32:
                        return "uint";
                    case FieldDescriptorProto.Type.TypeFixed64:
                        dataFormat = nameof(DataFormat.FixedSize);
                        return "ulong";
                    case FieldDescriptorProto.Type.TypeUint64:
                        return "ulong";
                    case FieldDescriptorProto.Type.TypeBytes:
                        return "byte[]";
                    case FieldDescriptorProto.Type.TypeEnum:
                        var enumType = Find<EnumDescriptorProto>(field.TypeName);
                        return Normalizer.GetName(enumType);
                    case FieldDescriptorProto.Type.TypeGroup:
                    case FieldDescriptorProto.Type.TypeMessage:
                        var msgType = Find<DescriptorProto>(field.TypeName);
                        if (field.type == FieldDescriptorProto.Type.TypeGroup)
                        {
                            dataFormat = nameof(DataFormat.Group);
                        }
                        isMap = msgType.Options?.MapEntry ?? false;
                        return Normalizer.GetName(msgType);
                    default:
                        return field.TypeName;
                }
            }
            public GeneratorContext Indent()
            {
                _indent++;
                return this;
            }
            public GeneratorContext Outdent()
            {
                _indent--;
                return this;
            }
            private int _indent;
            public GeneratorContext(FileDescriptorProto schema, NameNormalizer normalizer, TextWriter output)
            {
                this.fileDescriptor = schema;
                Normalizer = normalizer;
                Output = output;
            }
            public TextWriter Write(string value)
            {
                var indent = _indent;
                var target = Output;
                while (indent-- > 0)
                {
                    target.Write(Tab);
                }
                target.Write(value);
                return target;
            }
            public string Tab { get; set; } = "    ";
            public GeneratorContext WriteLine()
            {
                Output.WriteLine();
                return this;
            }
            public GeneratorContext WriteLine(string line)
            {
                var indent = _indent;
                var target = Output;
                while (indent-- > 0)
                {
                    target.Write(Tab);
                }
                target.WriteLine(line);
                return this;
            }
            public TextWriter Output { get; }
            public NameNormalizer Normalizer { get; }

            private Dictionary<string, object> _knownTypes = new Dictionary<string, object>();
            private readonly FileDescriptorProto fileDescriptor;

            internal void BuildTypeIndex()
            {
                void AddMessage(DescriptorProto message)
                {
                    _knownTypes[message.FullyQualifiedName] = message;
                    foreach (var @enum in message.EnumTypes)
                    {
                        _knownTypes[@enum.FullyQualifiedName] = @enum;
                    }
                    foreach (var msg in message.NestedTypes)
                    {
                        AddMessage(msg);
                    }
                }
                {
                    _knownTypes.Clear();
                    foreach (var @enum in fileDescriptor.EnumTypes)
                    {
                        _knownTypes[@enum.FullyQualifiedName] = @enum;
                    }
                    foreach (var msg in fileDescriptor.MessageTypes)
                    {
                        AddMessage(msg);
                    }
                }
            }
            public T Find<T>(string typeName) where T : class
            {
                if (!_knownTypes.TryGetValue(typeName, out var obj) || obj == null)
                {
                    throw new InvalidOperationException($"Type not found: {typeName}");
                }
                if (obj is T) return (T)obj;

                throw new InvalidOperationException($"Type of {typeName} is not suitable; expected {typeof(T).Name}, got {obj.GetType().Name}");
            }
        }

        public static void GenerateCSharp(TextWriter target, FileDescriptorProto schema, NameNormalizer normalizer = null, IList<Error> errors = null)
        {
            var ctx = new GeneratorContext(schema, normalizer ?? NameNormalizer.Default, target);
            ctx.BuildTypeIndex();

            ctx.WriteLine("// This file is generated by a tool; you should avoid making direct changes.")
                .WriteLine("// Consider using 'partial classes' to extend these types")
                .WriteLine($"// Input: {Path.GetFileName(schema.Name)}").WriteLine();

            var @namespace = schema.Options?.CsharpNamespace ?? schema.Package;

            if (errors != null)
            {
                bool isFirst = true;
                foreach (var error in errors.Where(x => x.IsError))
                {
                    if (isFirst)
                    {
                        ctx.WriteLine("// errors in " + schema.Name);
                        isFirst = false;
                    }
                    ctx.WriteLine("#error " + error.ToString(false));
                }
                if (!isFirst) ctx.WriteLine();
            }
            ctx.WriteLine("#pragma warning disable CS1591, CS0612").WriteLine();
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                ctx.WriteLine($"namespace {@namespace}");
                ctx.WriteLine("{").Indent().WriteLine();
            }
            foreach (var @enum in schema.EnumTypes)
            {
                Write(ctx, @enum);
            }
            foreach (var msgType in schema.MessageTypes)
            {
                Write(ctx, msgType);
            }
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                ctx.Outdent().WriteLine("}").WriteLine();
            }
            ctx.WriteLine("#pragma warning restore CS1591, CS0612");
        }

        private static void Write(GeneratorContext context, DescriptorProto message)
        {
            if(message.Options?.MapEntry??false)
            {
                return; // don't write this type - use a dictionary instead
            }

            var name = context.Normalizer.GetName(message);
            context.WriteLine($@"[global::ProtoBuf.ProtoContract(Name = @""{message.Name}"")]");
            WriteOptions(context, message.Options);
            context.WriteLine($"public partial class {Escape(name)}");
            context.WriteLine("{").Indent();
            foreach (var obj in message.EnumTypes)
            {
                Write(context, obj);
            }
            foreach (var obj in message.NestedTypes)
            {
                Write(context, obj);
            }
            var oneOfs = OneOfStub.Build(context, message);
            foreach (var obj in message.Fields)
            {
                Write(context, obj, oneOfs);
            }
            context.Outdent().WriteLine("}").WriteLine();
        }
        internal class OneOfStub
        {
            public OneofDescriptorProto OneOf { get; }

            internal OneOfStub(GeneratorContext context, OneofDescriptorProto decl)
            {
                OneOf = decl;
                //context.
            }
            public int Count32 { get; private set; }
            public int Count64 { get; private set; }
            public int CountRef { get; private set; }
            public int CountTotal => CountRef + Count32 + Count64;

            void AccountFor(FieldDescriptorProto.Type type)
            {
                switch (type)
                {
                    case FieldDescriptorProto.Type.TypeBool:
                    case FieldDescriptorProto.Type.TypeEnum:
                    case FieldDescriptorProto.Type.TypeFixed32:
                    case FieldDescriptorProto.Type.TypeFloat:
                    case FieldDescriptorProto.Type.TypeInt32:
                    case FieldDescriptorProto.Type.TypeSfixed32:
                    case FieldDescriptorProto.Type.TypeSint32:
                    case FieldDescriptorProto.Type.TypeUint32:
                        Count32++;
                        break;
                    case FieldDescriptorProto.Type.TypeDouble:
                    case FieldDescriptorProto.Type.TypeFixed64:
                    case FieldDescriptorProto.Type.TypeInt64:
                    case FieldDescriptorProto.Type.TypeSfixed64:
                    case FieldDescriptorProto.Type.TypeSint64:
                    case FieldDescriptorProto.Type.TypeUint64:
                        Count32++;
                        Count64++;
                        break;
                    default:
                        CountRef++;
                        break;
                }
            }
            internal string GetStorage(FieldDescriptorProto.Type type)
            {
                switch (type)
                {
                    case FieldDescriptorProto.Type.TypeBool:
                        return nameof(DiscriminatedUnion64Object.Boolean);
                    case FieldDescriptorProto.Type.TypeInt32:
                    case FieldDescriptorProto.Type.TypeSfixed32:
                    case FieldDescriptorProto.Type.TypeSint32:
                    case FieldDescriptorProto.Type.TypeFixed32:
                    case FieldDescriptorProto.Type.TypeEnum:
                        return nameof(DiscriminatedUnion64Object.Int32);
                    case FieldDescriptorProto.Type.TypeFloat:
                        return nameof(DiscriminatedUnion64Object.Single);
                    case FieldDescriptorProto.Type.TypeUint32:
                        return nameof(DiscriminatedUnion64Object.UInt32);
                    case FieldDescriptorProto.Type.TypeDouble:
                        return nameof(DiscriminatedUnion64Object.Double);
                    case FieldDescriptorProto.Type.TypeFixed64:
                    case FieldDescriptorProto.Type.TypeInt64:
                    case FieldDescriptorProto.Type.TypeSfixed64:
                    case FieldDescriptorProto.Type.TypeSint64:
                        return nameof(DiscriminatedUnion64Object.Int64);
                    case FieldDescriptorProto.Type.TypeUint64:
                        return nameof(DiscriminatedUnion64Object.UInt64);
                    default:
                        return nameof(DiscriminatedUnion64Object.Object);
                }
            }
            internal static OneOfStub[] Build(GeneratorContext context, DescriptorProto message)
            {
                if (message.OneofDecls.Count == 0) return null;
                var stubs = new OneOfStub[message.OneofDecls.Count];
                int index = 0;
                foreach (var decl in message.OneofDecls)
                {
                    stubs[index++] = new OneOfStub(context, decl);
                }
                foreach (var field in message.Fields)
                {
                    if (field.ShouldSerializeOneofIndex())
                    {
                        stubs[field.OneofIndex].AccountFor(field.type);
                    }
                }
                return stubs;
            }
            private bool isFirst = true;
            internal bool IsFirst()
            {
                if (isFirst)
                {
                    isFirst = false;
                    return true;
                }
                return false;
            }

            internal string GetUnionType()
            {
                if (Count64 != 0)
                {
                    return CountRef == 0 ? nameof(DiscriminatedUnion64) : nameof(DiscriminatedUnion64Object);
                }
                if (Count32 != 0)
                {
                    return CountRef == 0 ? nameof(DiscriminatedUnion32) : nameof(DiscriminatedUnion32Object);
                }
                return nameof(DiscriminatedUnionObject);
            }
        }
        private static bool UseArray(FieldDescriptorProto field)
        {
            switch (field.type)
            {
                case FieldDescriptorProto.Type.TypeBool:
                case FieldDescriptorProto.Type.TypeDouble:
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
        private static void WriteOptions<T>(GeneratorContext context, T obj) where T : class, ISchemaOptions
        {
            if (obj == null) return;
            if (obj.Deprecated)
            {
                context.WriteLine($"[global::System.Obsolete]");
            }
        }
        const string FieldPrefix = "__pbn__";
        private static void Write(GeneratorContext context, FieldDescriptorProto field, OneOfStub[] oneOfs)
        {
            var name = context.Normalizer.GetName(field);
            var tw = context.Write($@"[global::ProtoBuf.ProtoMember({field.Number}, Name = @""{field.Name}""");
            bool isOptional = field.label == FieldDescriptorProto.Label.LabelOptional;
            bool isRepeated = field.label == FieldDescriptorProto.Label.LabelRepeated;

            OneOfStub oneOf = field.ShouldSerializeOneofIndex() ? oneOfs?[field.OneofIndex] : null;
            if (oneOf != null && oneOf.CountTotal == 1)
            {
                oneOf = null; // not really a one-of, then!
            }
            bool explicitValues = isOptional && oneOf == null && context.Syntax == FileDescriptorProto.SyntaxProto2
                && field.type != FieldDescriptorProto.Type.TypeMessage
                && field.type != FieldDescriptorProto.Type.TypeGroup;


            string defaultValue = null;
            if (isOptional)
            {
                defaultValue = field.DefaultValue;

                if (field.type == FieldDescriptorProto.Type.TypeString)
                {
                    defaultValue = string.IsNullOrEmpty(defaultValue) ? "\"\""
                        : ("@\"" + (defaultValue ?? "").Replace("\"", "\"\"") + "\"");
                }
                else if (field.type == FieldDescriptorProto.Type.TypeDouble)
                {
                    switch(defaultValue)
                    {
                        case "inf": defaultValue = "double.PositiveInfinity"; break;
                        case "-inf": defaultValue = "double.NegativeInfinity"; break;
                        case "nan": defaultValue = "double.NaN"; break;
                    }
                }
                else if (field.type == FieldDescriptorProto.Type.TypeFloat)
                {
                    switch (defaultValue)
                    {
                        case "inf": defaultValue = "float.PositiveInfinity"; break;
                        case "-inf": defaultValue = "float.NegativeInfinity"; break;
                        case "nan": defaultValue = "float.NaN"; break;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(defaultValue) && field.type == FieldDescriptorProto.Type.TypeEnum)
                {
                    var enumType = context.Find<EnumDescriptorProto>(field.TypeName);

                    var found = enumType.Values.FirstOrDefault(x => x.Name == defaultValue);
                    if (found != null) defaultValue = context.Normalizer.GetName(found);
                    defaultValue = context.Normalizer.GetName(enumType) + "." + defaultValue;
                }
            }
            var typeName = context.GetTypeName(field, out var dataFormat, out var isMap);
            if (!string.IsNullOrWhiteSpace(dataFormat))
            {
                tw.Write($", DataFormat = global::ProtoBuf.DataFormat.{dataFormat}");
            }
            if (field.Options?.Packed ?? false)
            {
                tw.Write($", IsPacked = true");
            }
            if (field.label == FieldDescriptorProto.Label.LabelRequired)
            {
                tw.Write($", IsRequired = true");
            }
            tw.WriteLine(")]");
            if (!isRepeated && !string.IsNullOrWhiteSpace(defaultValue))
            {
                context.WriteLine($"[global::System.ComponentModel.DefaultValue({defaultValue})]");
            }
            WriteOptions(context, field.Options);
            if (isRepeated)
            {

                if(isMap)
                {
                    var msgType = context.Find<DescriptorProto>(field.TypeName);
                    
                    var keyTypeName = context.GetTypeName(msgType.Fields.Single(x => x.Number == 1),
                        out var keyDataFormat, out var _);
                    var valueTypeName = context.GetTypeName(msgType.Fields.Single(x => x.Number == 2),
                        out var valueDataFormat, out var _);

                    bool first = true;
                    tw = context.Write($"[global::ProtoBuf.Map");
                    if (!string.IsNullOrWhiteSpace(keyDataFormat))
                    {
                        tw.Write($"{(first ? "(" : ", ")}KeyFormat = global::ProtoBuf.DataFormat.{keyDataFormat}");
                        first = false;
                    }
                    if (!string.IsNullOrWhiteSpace(valueDataFormat))
                    {
                        tw.Write($"{(first ? "(" : ", ")}ValueFormat = global::ProtoBuf.DataFormat.{valueDataFormat}");
                        first = false;
                    }
                    tw.WriteLine(first ? "]" : ")]");
                    context.WriteLine($"public global::System.Collections.Generic.Dictionary<{keyTypeName}, {valueTypeName}> {Escape(name)} {{ get; }} = new global::System.Collections.Generic.Dictionary<{keyTypeName}, {valueTypeName}>();");
                }
                else if (UseArray(field))
                {
                    context.WriteLine($"public {typeName}[] {Escape(name)} {{ get; set; }}");
                }
                else
                {
                    context.WriteLine($"public global::System.Collections.Generic.List<{typeName}> {Escape(name)} {{ get; }} = new global::System.Collections.Generic.List<{typeName}>();");
                }
            }
            else if (oneOf != null)
            {
                var defValue = string.IsNullOrWhiteSpace(defaultValue) ? $"default({typeName})" : defaultValue;
                var fieldName = FieldPrefix + oneOf.OneOf.Name;
                var storage = oneOf.GetStorage(field.type);
                context.WriteLine($"public {typeName} {Escape(name)}").WriteLine("{").Indent();

                switch (field.type)
                {
                    case FieldDescriptorProto.Type.TypeMessage:
                    case FieldDescriptorProto.Type.TypeGroup:
                    case FieldDescriptorProto.Type.TypeEnum:
                    case FieldDescriptorProto.Type.TypeBytes:
                    case FieldDescriptorProto.Type.TypeString:
                        context.WriteLine($"get {{ return {fieldName}.Is({field.Number}) ? (({typeName}){fieldName}.{storage}) : {defValue}; }}");
                        break;
                    default:
                        context.WriteLine($"get {{ return {fieldName}.Is({field.Number}) ? {fieldName}.{storage} : {defValue}; }}");
                        break;
                }
                var unionType = oneOf.GetUnionType();
                context.WriteLine($"set {{ {fieldName} = new global::ProtoBuf.{unionType}({field.Number}, value); }}")
                    .Outdent().WriteLine("}")
                    .WriteLine($"public bool ShouldSerialize{name}() => {fieldName}.Is({field.Number});")
                    .WriteLine($"public void Reset{name}() => global::ProtoBuf.{unionType}.Reset(ref {fieldName}, {field.Number});");

                if (oneOf.IsFirst())
                {
                    context.WriteLine().WriteLine($"private global::ProtoBuf.{unionType} {fieldName};");
                }
            }
            else if (explicitValues)
            {
                string fieldName = FieldPrefix + name, fieldType;
                bool isRef = false;
                switch (field.type)
                {
                    case FieldDescriptorProto.Type.TypeString:
                    case FieldDescriptorProto.Type.TypeBytes:
                        fieldType = typeName;
                        isRef = true;
                        break;
                    default:
                        fieldType = typeName + "?";
                        break;
                }
                context.WriteLine($"public {typeName} {Escape(name)}").WriteLine("{").Indent();
                tw = context.Write($"get {{ return {fieldName}");
                if (!string.IsNullOrWhiteSpace(defaultValue))
                {
                    tw.Write(" ?? ");
                    tw.Write(defaultValue);
                }
                else if (!isRef)
                {
                    tw.Write(".GetValueOrDefault()");
                }
                tw.WriteLine("; }");
                context.WriteLine($"set {{ {fieldName} = value; }}")
                    .Outdent().WriteLine("}")
                    .WriteLine($"public bool ShouldSerialize{name}() => {fieldName} != null;")
                    .WriteLine($"public void Reset{name}() => {fieldName} = null;")
                    .WriteLine($"private {fieldType} {fieldName};");
            }
            else
            {
                tw = context.Write($"public {typeName} {Escape(name)} {{ get; set; }}");
                if (!string.IsNullOrWhiteSpace(defaultValue)) tw.Write($" = {defaultValue};");
                tw.WriteLine();
            }
            context.WriteLine();
        }


    }
}
