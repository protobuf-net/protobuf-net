//using Google.Protobuf.Reflection;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace ProtoBuf
//{
//    internal static class Generators
//    {
//        private static void Write(StringBuilder sb, int indent, EnumDescriptorProto @enum)
//        {
//            sb.AppendLine(indent, $"public enum {@enum.name}");
//            sb.AppendLine(indent++, "{");
//            foreach (var val in @enum.value)
//            {
//                sb.AppendLine(indent, $"{val.name} = {val.number},");
//            }
//            sb.AppendLine(--indent, "}");
//        }
//        public static string GenerateCSharp()
//        {
//            var sb = new StringBuilder();

//            int indent = 0;
//            var @namespace = Options["csharp_namespace"] ?? Package;
//            if (!string.IsNullOrWhiteSpace(@namespace))
//            {
//                sb.AppendLine(indent, $"namespace {@namespace}");
//                sb.AppendLine(indent++, "{");
//            }
//            List<Message> stack = new List<Message>();
//            foreach (var obj in Items)
//            {
//                if (obj is Enum)
//                {
//                    Write(sb, indent, (Enum)obj);
//                }
//                else if (obj is Message)
//                {
//                    Write(sb, indent, (Message)obj, stack);
//                }
//            }
//            if (!string.IsNullOrWhiteSpace(Package))
//            {
//                sb.AppendLine(--indent, "}");
//            }
//            return sb.ToString();
//        }

//        private static void Write(StringBuilder sb, int indent, DescriptorProto message, List<DescriptorProto> stack)
//        {
//            sb.AppendLine(indent, $@"[global::ProtoBuf.ProtoContract(Name = @""{message.name}"")]");
//            sb.AppendLine(indent, $"public partial class {message.name}");
//            sb.AppendLine(indent++, "{");
//            stack.Add(message);
//            foreach (var obj in message.enum_type)
//            {
//                Write(sb, indent, obj);
//            }
//            foreach (var obj in message.nested_type)
//            {
//                Write(sb, indent, obj, stack);
//            }
//            foreach (var obj in message.field)
//            {
//                Write(sb, indent, obj, stack);
//            }
//            stack.RemoveAt(stack.Count - 1);
//            sb.AppendLine(--indent, "}");
//        }
//        private static bool UseArray(FieldDescriptorProto field)
//        {
//            switch (field.type)
//            {
//                case FieldDescriptorProto.Type.TYPE_BOOL:
//                case FieldDescriptorProto.Type.TYPE_DOUBLE:
//                case FieldDescriptorProto.Type.TYPE_FIXED32:
//                case FieldDescriptorProto.Type.TYPE_FIXED64:
//                case FieldDescriptorProto.Type.TYPE_FLOAT:
//                case FieldDescriptorProto.Type.TYPE_INT32:
//                case FieldDescriptorProto.Type.TYPE_INT64:
//                case FieldDescriptorProto.Type.TYPE_SFIXED32:
//                case FieldDescriptorProto.Type.TYPE_SFIXED64:
//                case FieldDescriptorProto.Type.TYPE_SINT32:
//                case FieldDescriptorProto.Type.TYPE_SINT64:
//                case FieldDescriptorProto.Type.TYPE_UINT32:
//                case FieldDescriptorProto.Type.TYPE_UINT64:
//                    return true;
//                default:
//                    return false;
//            }
//        }
//        private static void Write(StringBuilder sb, int indent, FieldDescriptorProto field, List<DescriptorProto> stack)
//        {
//            sb.Append(indent, $"[global::ProtoBuf.ProtoMember({field.number}");
//            bool isOptional = field.label == FieldDescriptorProto.Label.LABEL_OPTIONAL;
//            bool isRepeated = field.label == FieldDescriptorProto.Label.LABEL_REPEATED;
//            string defaultValue = null;
//            if (isOptional)
//            {
//                defaultValue = field.default_value;

//                if (field.type == FieldDescriptorProto.Type.TYPE_STRING)
//                {
//                    defaultValue = string.IsNullOrEmpty(defaultValue) ? "\"\""
//                        : ("@\"" + (defaultValue ?? "").Replace("\"", "\"\"") + "\"");
//                }
//                else if (!string.IsNullOrWhiteSpace(defaultValue)
//                    && TryResolveEnum(field.type_name, stack, out EnumDescriptorProto @enum))
//                {
//                    defaultValue = @enum.name + "." + defaultValue;
//                }
//            }
//            var typeName = GetTypeName(field, out var dataFormat);
//            if (!string.IsNullOrWhiteSpace(dataFormat))
//            {
//                sb.Append($", DataFormat=DataFormat.{dataFormat}");
//            }
//            if (field.options?.packed ?? false)
//            {
//                sb.Append($", IsPacked = true");
//            }
//            if (field.label == FieldDescriptorProto.Label.LABEL_REQUIRED)
//            {
//                sb.Append($", IsRequired = true");
//            }
//            sb.AppendLine(")]");
//            if (!isRepeated && !string.IsNullOrWhiteSpace(defaultValue))
//            {
//                sb.AppendLine(indent, $"[global::System.ComponentModel.DefaultValue({defaultValue})]");
//            }
//            if (field.options?.deprecated ?? false)
//            {
//                sb.AppendLine(indent, $"[global::System.Obsolete]");
//            }
//            if (isRepeated)
//            {
//                if (UseArray(field))
//                {
//                    sb.AppendLine(indent, $"public {typeName}[] {field.name} {{ get; set; }}");
//                }
//                else
//                {
//                    sb.AppendLine(indent, $"public global::System.Collections.Generic.List<{typeName}> {field.Name} {{ get; }} = new global::System.Collections.Generic.List<{typeName}>();");
//                }
//            }
//            else
//            {
//                sb.Append(indent, $"public {typeName} {field.name} {{ get; set; }}");
//                if (!string.IsNullOrWhiteSpace(defaultValue)) sb.Append($" = {defaultValue};");
//                sb.AppendLine();
//            }
//        }

//        private static bool TryResolveEnum(string type, List<DescriptorProto> stack, out EnumDescriptorProto @enum)
//        {
//            for (int i = stack.Count - 1; i >= 0; i--)
//            {
//                @enum = stack[i].enum_type.FirstOrDefault(x => x.name == type);
//                if (@enum != null) return true;
//            }
//            @enum = null;
//            return false;
//        }

//        private static string GetTypeName(Field field, out string dataFormat)
//        {
//            const string SIGNED = "ZigZag", FIXED = "FixedSize";
//            dataFormat = "";
//            switch (field.Type)
//            {
//                case "double":
//                case "float":
//                case "bool":
//                case "string":
//                    return field.Type;
//                case "sint32":
//                    dataFormat = SIGNED;
//                    return "int";
//                case "int32":
//                    return "int";
//                case "sfixed32":
//                    dataFormat = FIXED;
//                    return "int";
//                case "sint64":
//                    dataFormat = SIGNED;
//                    return "long";
//                case "int64":
//                    return "long";
//                case "sfixed64":
//                    dataFormat = FIXED;
//                    return "long";
//                case "fixed32":
//                    dataFormat = FIXED;
//                    return "uint";
//                case "uint32":
//                    return "uint";
//                case "fixed64":
//                    dataFormat = FIXED;
//                    return "ulong";
//                case "uint64":
//                    return "ulong";
//                case "bytes":
//                    return "byte[]";
//                default:
//                    return field.Type;
//            }
//        }
//    }
//}
