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
        public void GenerateCSharp(TextWriter target, NameNormalizer normalizer = null)
            => Generators.GenerateCSharp(target, this, normalizer);

        public string GenerateCSharp(NameNormalizer normalizer = null)
        {
            using (var sw = new StringWriter())
            {
                GenerateCSharp(sw, normalizer);
                return sw.ToString();
            }
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
            if(Regex.IsMatch(identifier, @"^[_A-Z0-9]*$"))
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
                switch(identifier[identifier.Length - 2])
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
            if(definition.label == FieldDescriptorProto.Label.LabelRepeated)
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
#pragma warning restore CS1591
}

namespace ProtoBuf
{
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
        private static void Write(TextWriter target, int indent, EnumDescriptorProto @enum, GeneratorContext context)
        {
            var name = context.Normalizer.GetName(@enum);
            target.WriteLine(indent, $@"[global::ProtoBuf.ProtoContract(Name = @""{@enum.Name}"")]");
            target.WriteLine(indent, $"public enum {Escape(name)}");
            target.WriteLine(indent++, "{");
            foreach (var val in @enum.Values)
            {
                name = context.Normalizer.GetName(val);
                target.WriteLine(indent, $@"[global::ProtoBuf.ProtoEnum(Name = @""{val.Name}"", Value = {val.Number})]");
                target.WriteLine(indent, $"{Escape(name)} = {val.Number},");
            }
            target.WriteLine(--indent, "}");
        }
        private class GeneratorContext
        {
            private readonly FileDescriptorProto schema;
            private readonly NameNormalizer normalizer;

            public string GetTypeName(FieldDescriptorProto field, out string dataFormat)
            {
                const string SIGNED = "ZigZag", FIXED = "FixedSize";
                dataFormat = "";
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
                        dataFormat = SIGNED;
                        return "int";
                    case FieldDescriptorProto.Type.TypeInt32:
                        return "int";
                    case FieldDescriptorProto.Type.TypeSfixed32:
                        dataFormat = FIXED;
                        return "int";
                    case FieldDescriptorProto.Type.TypeSint64:
                        dataFormat = SIGNED;
                        return "long";
                    case FieldDescriptorProto.Type.TypeInt64:
                        return "long";
                    case FieldDescriptorProto.Type.TypeSfixed64:
                        dataFormat = FIXED;
                        return "long";
                    case FieldDescriptorProto.Type.TypeFixed32:
                        dataFormat = FIXED;
                        return "uint";
                    case FieldDescriptorProto.Type.TypeUint32:
                        return "uint";
                    case FieldDescriptorProto.Type.TypeFixed64:
                        dataFormat = FIXED;
                        return "ulong";
                    case FieldDescriptorProto.Type.TypeUint64:
                        return "ulong";
                    case FieldDescriptorProto.Type.TypeBytes:
                        return "byte[]";
                    case FieldDescriptorProto.Type.TypeEnum:
                        var enumType = FindEnum(field.TypeName);
                        return enumType == null ? field.TypeName : Normalizer.GetName(enumType);
                    case FieldDescriptorProto.Type.TypeMessage:
                    case FieldDescriptorProto.Type.TypeGroup:
                        var msgType = FindMessage(field.TypeName);
                        return msgType == null ? field.TypeName : Normalizer.GetName(msgType);
                    default:
                        if (field.type == 0)
                        {
                            throw new InvalidOperationException($"Unknown type: {field.TypeName}");
                        }
                        else
                        {
                            throw new InvalidOperationException($"Unknown type: {field.type} ({field.TypeName})");
                        }
                }
            }

            public GeneratorContext(FileDescriptorProto schema, NameNormalizer normalizer)
            {
                this.schema = schema;
                this.normalizer = normalizer;
            }
            public NameNormalizer Normalizer => normalizer;

            public DescriptorProto FindMessage(string name)
            {
                DescriptorProto FindMessage(DescriptorProto message, string type)
                {
                    foreach (var inner in message.NestedTypes)
                    {
                        if (inner.Name == name) return inner;

                        var found = FindMessage(inner, type);
                        if (found != null) return found;
                    }
                    return null;
                }
                // this will all be replaced when we have the full names thing fixed
                foreach (var type in schema.MessageTypes)
                {
                    if (type.Name == name) return type;
                    var found = FindMessage(type, name);
                    if (found != null) return found;
                }
                return null;
            }

            public EnumDescriptorProto FindEnum(string name)
            {
                EnumDescriptorProto FindEnum(DescriptorProto message, string type)
                {
                    foreach (var @enum in message.EnumTypes)
                    {
                        if (@enum.Name == name) return @enum;
                    }
                    foreach (var inner in message.NestedTypes)
                    {
                        var found = FindEnum(inner, type);
                        if (found != null) return found;
                    }
                    return null;
                }
                // this will all be replaced when we have the full names thing fixed
                foreach (var @enum in schema.EnumTypes)
                {
                    if (@enum.Name == name) return @enum;
                }
                foreach (var type in schema.MessageTypes)
                {
                    var found = FindEnum(type, name);
                    if (found != null) return found;
                }
                return null;
            }
        }

        public static void GenerateCSharp(TextWriter target, FileDescriptorProto schema, NameNormalizer normalizer = null)
        {
            var ctx = new GeneratorContext(schema, normalizer ?? NameNormalizer.Default);

            int indent = 0;
            var @namespace = schema.Options?.CsharpNamespace ?? schema.Package;
            target.WriteLine(indent, "#pragma warning disable CS1591");
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                target.WriteLine(indent, $"namespace {@namespace}");
                target.WriteLine(indent++, "{");
            }
            foreach (var obj in schema.EnumTypes)
            {
                Write(target, indent, obj, ctx);
            }
            foreach (var obj in schema.MessageTypes)
            {
                Write(target, indent, obj, ctx);
            }
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                target.WriteLine(--indent, "}");
            }
            target.WriteLine(indent, "#pragma warning restore CS1591");
        }

        private static void Write(TextWriter target, int indent, DescriptorProto message, GeneratorContext context)
        {
            var name = context.Normalizer.GetName(message);
            target.WriteLine(indent, $@"[global::ProtoBuf.ProtoContract(Name = @""{message.Name}"")]");
            target.WriteLine(indent, $"public partial class {Escape(name)}");
            target.WriteLine(indent++, "{");
            foreach (var obj in message.EnumTypes)
            {
                Write(target, indent, obj, context);
            }
            foreach (var obj in message.NestedTypes)
            {
                Write(target, indent, obj, context);
            }
            foreach (var obj in message.Fields)
            {
                Write(target, indent, obj, context);
            }
            target.WriteLine(--indent, "}");
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

        private static void Write(TextWriter target, int indent, FieldDescriptorProto field, GeneratorContext context)
        {
            var name = context.Normalizer.GetName(field);
            target.Write(indent, $@"[global::ProtoBuf.ProtoMember({field.Number}, Name = @""{field.Name}""");
            bool isOptional = field.label == FieldDescriptorProto.Label.LabelOptional;
            bool isRepeated = field.label == FieldDescriptorProto.Label.LabelRepeated;
            string defaultValue = null;
            if (isOptional)
            {
                defaultValue = field.DefaultValue;

                if (field.type == FieldDescriptorProto.Type.TypeString)
                {
                    defaultValue = string.IsNullOrEmpty(defaultValue) ? "\"\""
                        : ("@\"" + (defaultValue ?? "").Replace("\"", "\"\"") + "\"");
                }
                else if (!string.IsNullOrWhiteSpace(defaultValue) && field.type == FieldDescriptorProto.Type.TypeEnum)
                {
                    var enumType = context.FindEnum(field.TypeName);
                    if (enumType != null)
                    {
                        var found = enumType.Values.FirstOrDefault(x => x.Name == defaultValue);
                        if (found != null) defaultValue = context.Normalizer.GetName(found);
                        defaultValue = context.Normalizer.GetName(enumType) + "." + defaultValue;
                    }
                    else
                    {
                        defaultValue = field.TypeName + "." + defaultValue;
                    }
                }
            }
            var typeName = context.GetTypeName(field, out var dataFormat);
            if (!string.IsNullOrWhiteSpace(dataFormat))
            {
                target.Write($", DataFormat=DataFormat.{dataFormat}");
            }
            if (field.Options?.Packed ?? false)
            {
                target.Write($", IsPacked = true");
            }
            if (field.label == FieldDescriptorProto.Label.LabelRequired)
            {
                target.Write($", IsRequired = true");
            }
            target.WriteLine(")]");
            if (!isRepeated && !string.IsNullOrWhiteSpace(defaultValue))
            {
                target.WriteLine(indent, $"[global::System.ComponentModel.DefaultValue({defaultValue})]");
            }
            if (field.Options?.Deprecated ?? false)
            {
                target.WriteLine(indent, $"[global::System.Obsolete]");
            }
            if (isRepeated)
            {
                if (UseArray(field))
                {
                    target.WriteLine(indent, $"public {typeName}[] {Escape(name)} {{ get; set; }}");
                }
                else
                {
                    target.WriteLine(indent, $"public global::System.Collections.Generic.List<{typeName}> {Escape(name)} {{ get; }} = new global::System.Collections.Generic.List<{typeName}>();");
                }
            }
            else
            {
                target.Write(indent, $"public {typeName} {Escape(name)} {{ get; set; }}");
                if (!string.IsNullOrWhiteSpace(defaultValue)) target.Write($" = {defaultValue};");
                target.WriteLine();
            }
        }


    }
}
