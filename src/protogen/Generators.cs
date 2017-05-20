using Google.Protobuf.Reflection;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Google.Protobuf.Reflection
{
#pragma warning disable CS1591
    partial class FileDescriptorProto
    {
        public void GenerateCSharp(TextWriter target)
            => Generators.GenerateCSharp(target, this);

        public string GenerateCSharp()
        {
            using (var sw = new StringWriter())
            {
                GenerateCSharp(sw);
                return sw.ToString();
            }
        }
    }
#pragma warning restore CS1591
}

namespace ProtoBuf
{
    internal static class Generators
    {
        private static void Write(TextWriter target, int indent, EnumDescriptorProto @enum)
        {
            target.WriteLine(indent, $"public enum {@enum.name}");
            target.WriteLine(indent++, "{");
            foreach (var val in @enum.value)
            {
                target.WriteLine(indent, $"{val.name} = {val.number},");
            }
            target.WriteLine(--indent, "}");
        }
        public static void GenerateCSharp(TextWriter target, FileDescriptorProto schema)
        {
            int indent = 0;
            var @namespace = schema.options ?.csharp_namespace ?? schema.package;
            target.WriteLine(indent, "#pragma warning disable CS1591");
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                target.WriteLine(indent, $"namespace {@namespace}");
                target.WriteLine(indent++, "{");
            }
            foreach (var obj in schema.enum_type)
            {
                Write(target, indent, obj);
            }
            foreach (var obj in schema.message_type)
            {
                Write(target, indent, obj);
            }
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                target.WriteLine(--indent, "}");
            }
            target.WriteLine(indent, "#pragma warning restore CS1591");
        }

        private static void Write(TextWriter target, int indent, DescriptorProto message)
        {
            target.WriteLine(indent, $@"[global::ProtoBuf.ProtoContract(Name = @""{message.name}"")]");
            target.WriteLine(indent, $"public partial class {message.name}");
            target.WriteLine(indent++, "{");
            foreach (var obj in message.enum_type)
            {
                Write(target, indent, obj);
            }
            foreach (var obj in message.nested_type)
            {
                Write(target, indent, obj);
            }
            foreach (var obj in message.field)
            {
                Write(target, indent, obj);
            }
            target.WriteLine(--indent, "}");
        }
        private static bool UseArray(FieldDescriptorProto field)
        {
            switch (field.type)
            {
                case FieldDescriptorProto.Type.TYPE_BOOL:
                case FieldDescriptorProto.Type.TYPE_DOUBLE:
                case FieldDescriptorProto.Type.TYPE_FIXED32:
                case FieldDescriptorProto.Type.TYPE_FIXED64:
                case FieldDescriptorProto.Type.TYPE_FLOAT:
                case FieldDescriptorProto.Type.TYPE_INT32:
                case FieldDescriptorProto.Type.TYPE_INT64:
                case FieldDescriptorProto.Type.TYPE_SFIXED32:
                case FieldDescriptorProto.Type.TYPE_SFIXED64:
                case FieldDescriptorProto.Type.TYPE_SINT32:
                case FieldDescriptorProto.Type.TYPE_SINT64:
                case FieldDescriptorProto.Type.TYPE_UINT32:
                case FieldDescriptorProto.Type.TYPE_UINT64:
                    return true;
                default:
                    return false;
            }
        }
        private static void Write(TextWriter target, int indent, FieldDescriptorProto field)
        {
            target.Write(indent, $"[global::ProtoBuf.ProtoMember({field.number}");
            bool isOptional = field.label == FieldDescriptorProto.Label.LABEL_OPTIONAL;
            bool isRepeated = field.label == FieldDescriptorProto.Label.LABEL_REPEATED;
            string defaultValue = null;
            if (isOptional)
            {
                defaultValue = field.default_value;

                if (field.type == FieldDescriptorProto.Type.TYPE_STRING)
                {
                    defaultValue = string.IsNullOrEmpty(defaultValue) ? "\"\""
                        : ("@\"" + (defaultValue ?? "").Replace("\"", "\"\"") + "\"");
                }
                else if (!string.IsNullOrWhiteSpace(defaultValue) && field.type == FieldDescriptorProto.Type.TYPE_ENUM)
                {
                    defaultValue = field.type_name + "." + defaultValue;
                }
            }
            var typeName = GetTypeName(field, out var dataFormat);
            if (!string.IsNullOrWhiteSpace(dataFormat))
            {
                target.Write($", DataFormat=DataFormat.{dataFormat}");
            }
            if (field.options?.packed ?? false)
            {
                target.Write($", IsPacked = true");
            }
            if (field.label == FieldDescriptorProto.Label.LABEL_REQUIRED)
            {
                target.Write($", IsRequired = true");
            }
            target.WriteLine(")]");
            if (!isRepeated && !string.IsNullOrWhiteSpace(defaultValue))
            {
                target.WriteLine(indent, $"[global::System.ComponentModel.DefaultValue({defaultValue})]");
            }
            if (field.options?.deprecated ?? false)
            {
                target.WriteLine(indent, $"[global::System.Obsolete]");
            }
            if (isRepeated)
            {
                if (UseArray(field))
                {
                    target.WriteLine(indent, $"public {typeName}[] {field.name} {{ get; set; }}");
                }
                else
                {
                    target.WriteLine(indent, $"public global::System.Collections.Generic.List<{typeName}> {field.name} {{ get; }} = new global::System.Collections.Generic.List<{typeName}>();");
                }
            }
            else
            {
                target.Write(indent, $"public {typeName} {field.name} {{ get; set; }}");
                if (!string.IsNullOrWhiteSpace(defaultValue)) target.Write($" = {defaultValue};");
                target.WriteLine();
            }
        }

        private static string GetTypeName(FieldDescriptorProto field, out string dataFormat)
        {
            const string SIGNED = "ZigZag", FIXED = "FixedSize";
            dataFormat = "";
            switch (field.type)
            {
                case FieldDescriptorProto.Type.TYPE_DOUBLE:
                    return "double";
                case FieldDescriptorProto.Type.TYPE_FLOAT:
                    return "float";
                case FieldDescriptorProto.Type.TYPE_BOOL:
                    return "bool";
                case FieldDescriptorProto.Type.TYPE_STRING:
                    return "string";
                case FieldDescriptorProto.Type.TYPE_SINT32:
                    dataFormat = SIGNED;
                    return "int";
                case FieldDescriptorProto.Type.TYPE_INT32:
                    return "int";
                case FieldDescriptorProto.Type.TYPE_SFIXED32:
                    dataFormat = FIXED;
                    return "int";
                case FieldDescriptorProto.Type.TYPE_SINT64:
                    dataFormat = SIGNED;
                    return "long";
                case FieldDescriptorProto.Type.TYPE_INT64:
                    return "long";
                case FieldDescriptorProto.Type.TYPE_SFIXED64:
                    dataFormat = FIXED;
                    return "long";
                case FieldDescriptorProto.Type.TYPE_FIXED32:
                    dataFormat = FIXED;
                    return "uint";
                case FieldDescriptorProto.Type.TYPE_UINT32:
                    return "uint";
                case FieldDescriptorProto.Type.TYPE_FIXED64:
                    dataFormat = FIXED;
                    return "ulong";
                case FieldDescriptorProto.Type.TYPE_UINT64:
                    return "ulong";
                case FieldDescriptorProto.Type.TYPE_BYTES:
                    return "byte[]";
                case FieldDescriptorProto.Type.TYPE_ENUM:
                case FieldDescriptorProto.Type.TYPE_MESSAGE:
                    return field.type_name;
                default:
                    throw new InvalidOperationException($"Unknown type: {field.type} ({field.type_name})");
            }
        }
    }
}
