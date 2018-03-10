using Google.Protobuf.Reflection;
using ProtoBuf.Reflection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ProtoBuf
{
    /// <summary>
    /// A code generator that writes VB
    /// </summary>
    [Obsolete("Experimental; this is not stable", false)]
    public class VBCodeGenerator : CommonCodeGenerator
    {
        /// <summary>
        /// Reusable code-generator instance
        /// </summary>
        public static VBCodeGenerator Default { get; } = new VBCodeGenerator();

        /// <summary>
        /// Should case-sensitivity be used when computing conflicts?
        /// </summary>
        protected internal override bool IsCaseSensitive => true;

        /// <summary>
        /// Create a new VBCodeGenerator instance
        /// </summary>
        protected VBCodeGenerator() { }

        /// <summary>
        /// Returns the language name
        /// </summary>
        public override string Name => "VB.NET";

        /// <summary>
        /// Returns the default file extension
        /// </summary>
        protected override string DefaultFileExtension => "vb";
        /// <summary>
        /// Escapes language keywords
        /// </summary>
        protected override string Escape(string identifier)
        {
            switch (identifier)
            {
                case "AddHandler":
                case "AddressOf":
                case "Alias":
                case "And":
                case "AndAlso":
                case "As":
                case "Boolean":
                case "ByRef":
                case "Byte":
                case "ByVal":
                case "Call":
                case "Case":
                case "Catch":
                case "CBool":
                case "CByte":
                case "CChar":
                case "CDate":
                case "CDbl":
                case "CDec":
                case "Char":
                case "CInt":
                case "Class":
                case "Constraint":
                case "CLng":
                case "CObj":
                case "Const":
                case "Continue":
                case "CSByte":
                case "CShort":
                case "CSng":
                case "CStr":
                case "CType":
                case "CUInt":
                case "CULng":
                case "CUShort":
                case "Date":
                case "Decimal":
                case "Declare":
                case "Default":
                case "Delegate":
                case "Dim":
                case "DirectCast":
                case "Do":
                case "Double":
                case "Each":
                case "Else":
                case "ElseIf":
                case "End":
                case "EndIf":
                case "Enum":
                case "Erase":
                case "Error":
                case "Event":
                case "Exit":
                case "False":
                case "Finally":
                case "For":
                case "Friend":
                case "Function":
                case "Get":
                case "GetType":
                case "GetXMLNamespace":
                case "Global":
                case "GoSub":
                case "GoTo":
                case "Handles":
                case "If":
                case "Implements":
                case "Imports":
                case "In":
                case "Inherits":
                case "Integer":
                case "Interface":
                case "Is":
                case "IsNot":
                case "Let":
                case "Lib":
                case "Like":
                case "Long":
                case "Loop":
                case "Me":
                case "Mod":
                case "Module":
                case "MustInherit":
                case "MustOverride":
                case "MyBase":
                case "MyClass":
                case "Namespace":
                case "Narrowing":
                case "New":
                case "Next":
                case "Not":
                case "Nothing":
                case "NotInheritable":
                case "NotOverridable":
                case "Object":
                case "Of":
                case "On":
                case "Operator":
                case "Option":
                case "Optional":
                case "Or":
                case "OrElse":
                case "Out":
                case "Overloads":
                case "Overridable":
                case "Overrides":
                case "ParamArray":
                case "Partial":
                case "Private":
                case "Property":
                case "Protected":
                case "Public":
                case "RaiseEvent":
                case "ReadOnly":
                case "ReDim":
                case "REM":
                case "RemoveHandler":
                case "Resume":
                case "Return":
                case "SByte":
                case "Select":
                case "Set":
                case "Shadows":
                case "Shared":
                case "Short":
                case "Single":
                case "Static":
                case "Step":
                case "Stop":
                case "String":
                case "Structure":
                case "Sub":
                case "SyncLock":
                case "Then":
                case "Throw":
                case "To":
                case "True":
                case "Try":
                case "TryCast":
                case "TypeOf":
                case "UInteger":
                case "ULong":
                case "UShort":
                case "Using":
                case "Variant":
                case "Wend":
                case "When":
                case "While":
                case "Widening":
                case "With":
                case "WithEvents":
                case "WriteOnly":
                case "Xor":
                    return "[" + identifier + "]";
                default:
                    return identifier;
            }
        }

        /// <summary>
        /// Start a file
        /// </summary>
        protected override void WriteFileHeader(GeneratorContext ctx, FileDescriptorProto file, ref object state)
        {
            //var prefix = ctx.Supports(CSharp6) ? "CS" : "";
            ctx.WriteLine("' This file was generated by a tool; you should avoid making direct changes.")
               .WriteLine("' Consider using 'partial classes' to extend these types")
               .WriteLine($"' Input: {Path.GetFileName(ctx.File.Name)}").WriteLine()
               .WriteLine($"#Disable Warning BC40008").WriteLine();


            var @namespace = ctx.NameNormalizer.GetName(file);

            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                state = @namespace;
                ctx.WriteLine($"Namespace {@namespace}").Indent();
            }

        }
        /// <summary>
        /// End a file
        /// </summary>
        protected override void WriteFileFooter(GeneratorContext ctx, FileDescriptorProto file, ref object state)
        {
            var @namespace = (string)state;
            //var prefix = ctx.Supports(CSharp6) ? "CS" : "";
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                ctx.Outdent().WriteLine("End Namespace").WriteLine();
            }
            ctx.WriteLine($"#Enable Warning BC40008").WriteLine();
        }
        /// <summary>
        /// Start an enum
        /// </summary>
        protected override void WriteEnumHeader(GeneratorContext ctx, EnumDescriptorProto obj, ref object state)
        {
            var name = ctx.NameNormalizer.GetName(obj);
            var tw = ctx.Write($@"<Global.ProtoBuf.ProtoContract(");
            if (name != obj.Name) tw.Write($@"Name := ""{obj.Name}""");
            tw.WriteLine(")> _");
            WriteOptions(ctx, obj.Options);
            ctx.WriteLine($"{GetAccess(GetAccess(obj))} Enum {Escape(name)}").Indent();
        }
        /// <summary>
        /// End an enum
        /// </summary>

        protected override void WriteEnumFooter(GeneratorContext ctx, EnumDescriptorProto obj, ref object state)
        {
            ctx.Outdent().WriteLine("End Enum").WriteLine();
        }
        /// <summary>
        /// Write an enum value
        /// </summary>
        protected override void WriteEnumValue(GeneratorContext ctx, EnumValueDescriptorProto obj, ref object state)
        {
            var name = ctx.NameNormalizer.GetName(obj);
            if (name != obj.Name)
            {
                var tw = ctx.Write($@"<Global.ProtoBuf.ProtoEnum(");
                tw.Write($@"Name := ""{obj.Name}""");
                tw.WriteLine(")> _");
            }

            WriteOptions(ctx, obj.Options);
            ctx.WriteLine($"{Escape(name)} = {obj.Number}");
        }

        /// <summary>
        /// End a message
        /// </summary>
        protected override void WriteMessageFooter(GeneratorContext ctx, DescriptorProto obj, ref object state)
        {
            ctx.Outdent().WriteLine("End Class").WriteLine();
        }
        /// <summary>
        /// Start a message
        /// </summary>
        protected override void WriteMessageHeader(GeneratorContext ctx, DescriptorProto obj, ref object state)
        {
            var name = ctx.NameNormalizer.GetName(obj);
            var tw = ctx.Write($@"<Global.ProtoBuf.ProtoContract(");
            if (name != obj.Name) tw.Write($@"Name := ""{obj.Name}""");
            tw.WriteLine(")> _");
            WriteOptions(ctx, obj.Options);
            ctx.WriteLine($"Partial {GetAccess(GetAccess(obj))} Class {Escape(name)}");
            ctx.Indent().WriteLine("Implements Global.ProtoBuf.IExtensible").Outdent();

            ctx.Indent();
            if (obj.Options?.MessageSetWireFormat == true)
            {
                ctx.WriteLine("REM #error message_set_wire_format is not currently implemented").WriteLine();
            }

            ctx.WriteLine($"Private {FieldPrefix}extensionData As Global.ProtoBuf.IExtension")
                .WriteLine($"Private Function GetExtensionObject(ByVal createIfMissing As Boolean) As IExtension Implements IExtensible.GetExtensionObject")
                .Indent().WriteLine($"Return Extensible.GetExtensionObject({FieldPrefix}extensionData, createIfMissing)")
                .Outdent().WriteLine("End Function");
        }

        private static void WriteOptions<T>(GeneratorContext ctx, T obj) where T : class, ISchemaOptions
        {
            if (obj == null) return;
            if (obj.Deprecated)
            {
                ctx.WriteLine($"<Global.System.Obsolete> _");
            }
        }

        const string FieldPrefix = "__pbn__";

        /// <summary>
        /// Get the language specific keyword representing an access level
        /// </summary>
        public override string GetAccess(Access access)
        {
            switch (access)
            {
                case Access.Internal: return "Friend";
                case Access.Public: return "Public";
                case Access.Private: return "Private";
                default: return base.GetAccess(access);
            }
        }

        private string GetDefaultValue(GeneratorContext ctx, FieldDescriptorProto obj, string typeName)
        {
            string defaultValue = null;
            bool isOptional = obj.label == FieldDescriptorProto.Label.LabelOptional;

            if (isOptional || ctx.EmitRequiredDefaults || obj.type == FieldDescriptorProto.Type.TypeEnum)
            {
                defaultValue = obj.DefaultValue;

                if (obj.type == FieldDescriptorProto.Type.TypeString)
                {
                    defaultValue = string.IsNullOrEmpty(defaultValue) ? "\"\""
                        : ("\"" + (defaultValue ?? "").Replace("\"", "\"\"") + "\"");
                }
                else if (obj.type == FieldDescriptorProto.Type.TypeDouble)
                {
                    switch (defaultValue)
                    {
                        case "inf": defaultValue = "Double.PositiveInfinity"; break;
                        case "-inf": defaultValue = "Double.NegativeInfinity"; break;
                        case "nan": defaultValue = "Double.NaN"; break;
                    }
                }
                else if (obj.type == FieldDescriptorProto.Type.TypeFloat)
                {
                    switch (defaultValue)
                    {
                        case "inf": defaultValue = "Single.PositiveInfinity"; break;
                        case "-inf": defaultValue = "Single.NegativeInfinity"; break;
                        case "nan": defaultValue = "Single.NaN"; break;
                    }
                }
                else if (obj.type == FieldDescriptorProto.Type.TypeEnum)
                {
                    var enumType = ctx.TryFind<EnumDescriptorProto>(obj.TypeName);
                    if (enumType != null)
                    {
                        EnumValueDescriptorProto found = null;
                        if (!string.IsNullOrEmpty(defaultValue))
                        {
                            found = enumType.Values.FirstOrDefault(x => x.Name == defaultValue);
                        }
                        else if (ctx.Syntax == FileDescriptorProto.SyntaxProto2)
                        {
                            // find the first one; if that is a zero, we don't need it after all
                            found = enumType.Values.FirstOrDefault();
                            if (found != null && found.Number == 0)
                            {
                                if (!isOptional) found = null; // we don't need it after all
                            }
                        }
                        // for proto3 the default is 0, so no need to do anything - GetValueOrDefault() will do it all

                        if (found != null)
                        {
                            defaultValue = ctx.NameNormalizer.GetName(found);
                        }
                        if (!string.IsNullOrWhiteSpace(defaultValue))
                        {
                            defaultValue = typeName + "." + defaultValue;
                        }
                    }
                }
            }

            return defaultValue;
        }
        /// <summary>
        /// Write a field
        /// </summary>
        protected override void WriteField(GeneratorContext ctx, FieldDescriptorProto obj, ref object state, OneOfStub[] oneOfs)
        {
            var name = ctx.NameNormalizer.GetName(obj);
            if (name == "StringValue") System.Diagnostics.Debugger.Break();
            var tw = ctx.Write($@"<Global.ProtoBuf.ProtoMember({obj.Number}");
            if (name != obj.Name)
            {
                tw.Write($@", Name := ""{obj.Name}""");
            }
            var options = obj.Options?.GetOptions();
            if (options?.AsReference == true)
            {
                tw.Write($@", AsReference := True");
            }
            if (options?.DynamicType == true)
            {
                tw.Write($@", DynamicType := True");
            }

            bool isOptional = obj.label == FieldDescriptorProto.Label.LabelOptional;
            bool isRepeated = obj.label == FieldDescriptorProto.Label.LabelRepeated;

            OneOfStub oneOf = obj.ShouldSerializeOneofIndex() ? oneOfs?[obj.OneofIndex] : null;
            if (oneOf != null && oneOf.CountTotal == 1)
            {
                oneOf = null; // not really a one-of, then!
            }
            bool explicitValues = isOptional && oneOf == null && ctx.Syntax == FileDescriptorProto.SyntaxProto2
                && obj.type != FieldDescriptorProto.Type.TypeMessage
                && obj.type != FieldDescriptorProto.Type.TypeGroup;

            bool suppressDefaultAttribute = !isOptional;
            var typeName = GetTypeName(ctx, obj, out var dataFormat, out var isMap);
            string defaultValue = GetDefaultValue(ctx, obj, typeName);


            if (!string.IsNullOrWhiteSpace(dataFormat))
            {
                tw.Write($", DataFormat := Global.ProtoBuf.DataFormat.{dataFormat}");
            }
            if (obj.IsPacked(ctx.Syntax))
            {
                tw.Write($", IsPacked := True");
            }
            if (obj.label == FieldDescriptorProto.Label.LabelRequired)
            {
                tw.Write($", IsRequired := True");
            }
            tw.WriteLine(")> _");
            if (!isRepeated && !string.IsNullOrWhiteSpace(defaultValue) && !suppressDefaultAttribute)
            {
                ctx.WriteLine($"<Global.System.ComponentModel.DefaultValue({defaultValue})> _");
            }
            WriteOptions(ctx, obj.Options);
            if (isRepeated)
            {
                var mapMsgType = isMap ? ctx.TryFind<DescriptorProto>(obj.TypeName) : null;
                if (mapMsgType != null)
                {
                    var keyTypeName = GetTypeName(ctx, mapMsgType.Fields.Single(x => x.Number == 1),
                        out var keyDataFormat, out var _);
                    var valueTypeName = GetTypeName(ctx, mapMsgType.Fields.Single(x => x.Number == 2),
                        out var valueDataFormat, out var _);

                    bool first = true;
                    tw = ctx.Write($"<Global.ProtoBuf.ProtoMap");
                    if (!string.IsNullOrWhiteSpace(keyDataFormat))
                    {
                        tw.Write($"{(first ? "(" : ", ")}KeyFormat := Global.ProtoBuf.DataFormat.{keyDataFormat}");
                        first = false;
                    }
                    if (!string.IsNullOrWhiteSpace(valueDataFormat))
                    {
                        tw.Write($"{(first ? "(" : ", ")}ValueFormat := Global.ProtoBuf.DataFormat.{valueDataFormat}");
                        first = false;
                    }
                    tw.WriteLine(first ? "> _" : ")> _");

                    ctx.WriteLine($"{GetAccess(GetAccess(obj))} Readonly Property {Escape(name)} As New Global.System.Collections.Generic.Dictionary(Of {keyTypeName}, {valueTypeName})");
                }
                else if (UseArray(obj))
                {
                    ctx.WriteLine($"{GetAccess(GetAccess(obj))} Property {Escape(name)} As {typeName}()");
                }
                else
                {
                    ctx.WriteLine($"{GetAccess(GetAccess(obj))} Readonly Property {Escape(name)} As New Global.System.Collections.Generic.List(Of {typeName})");
                }
            }
            else if (oneOf != null)
            {
                var defValue = string.IsNullOrWhiteSpace(defaultValue) ? $"CType(Nothing, {typeName})" : defaultValue;
                var fieldName = FieldPrefix + oneOf.OneOf.Name;
                var storage = oneOf.GetStorage(obj.type, obj.TypeName);
                ctx.WriteLine($"{GetAccess(GetAccess(obj))} Property {Escape(name)} As {typeName}").WriteLine().Indent();

                ctx.WriteLine("Get").Indent();
                switch (obj.type)
                {
                    case FieldDescriptorProto.Type.TypeMessage:
                    case FieldDescriptorProto.Type.TypeGroup:
                    case FieldDescriptorProto.Type.TypeEnum:
                    case FieldDescriptorProto.Type.TypeBytes:
                    case FieldDescriptorProto.Type.TypeString:
                        ctx.WriteLine($"Return If({fieldName}.Is({obj.Number}), CType({fieldName}.{storage}, {typeName}), {defValue})");
                        break;
                    default:
                        ctx.WriteLine($"Return If({fieldName}.Is({obj.Number}), {fieldName}.{storage}, {defValue})");
                        break;
                }
                ctx.Outdent().WriteLine("End Get");

                var unionType = oneOf.GetUnionType();

                ctx.WriteLine($"Set(ByVal value As {typeName})").Indent()
                    .WriteLine($"{fieldName} = New Global.ProtoBuf.{unionType}({obj.Number}, value)").Outdent().WriteLine("End Set");

                ctx.Outdent().WriteLine("End Property");

                ctx.WriteLine($"{GetAccess(GetAccess(obj))} Function ShouldSerialize{name}() As Boolean").Indent()
                    .WriteLine($"Return {fieldName}.Is({obj.Number})").Outdent()
                    .WriteLine("End Function")
                    .WriteLine($"{GetAccess(GetAccess(obj))} Sub Reset{name}()").Indent()
                    .WriteLine($"Global.ProtoBuf.{unionType}.Reset({fieldName}, {obj.Number})")
                    .Outdent().WriteLine("End Sub");

                if (oneOf.IsFirst())
                {
                    ctx.WriteLine().WriteLine($"Private {fieldName} As Global.ProtoBuf.{unionType}");
                }
            }
            else if (explicitValues)
            {
                string fieldName = FieldPrefix + name, fieldType;
                bool isRef = false;
                switch (obj.type)
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

                ctx.WriteLine($"{GetAccess(GetAccess(obj))} Property {Escape(name)} As {typeName}").Indent()
                    .WriteLine("Get").Indent();

                if (!string.IsNullOrWhiteSpace(defaultValue))
                {
                    ctx.WriteLine($"Return If({fieldName}, {defaultValue})");
                }
                else if (!isRef)
                {
                    ctx.WriteLine($"Return {fieldName}.GetValueOrDefault()");
                }
                else
                {
                    ctx.WriteLine($"Return {fieldName}");
                }

                ctx.Outdent().WriteLine("End Get").WriteLine($"Set(ByVal value As {typeName})").Indent()
                    .WriteLine($"{fieldName} = value").Outdent().WriteLine("End Set").
                    Outdent().WriteLine("End Property");

                ctx.WriteLine($"{GetAccess(GetAccess(obj))} Function ShouldSerialize{name}() As Boolean").Indent()
                    .WriteLine($"Return Not ({fieldName} Is Nothing)").Outdent()
                    .WriteLine("End Function")
                    .WriteLine($"{GetAccess(GetAccess(obj))} Sub Reset{name}()").Indent()
                    .WriteLine($"{fieldName} = Nothing").Outdent().WriteLine("End Sub");

                ctx.WriteLine($"Private {fieldName} As {fieldType}");
            }
            else
            {
                tw = ctx.Write($"{GetAccess(GetAccess(obj))} Property {Escape(name)} As {typeName}");
                if (!string.IsNullOrWhiteSpace(defaultValue)) tw.Write($" = {defaultValue}");
                tw.WriteLine();
            }
            ctx.WriteLine();
        }

        /// <summary>
        /// Starts an extgensions block
        /// </summary>
        protected override void WriteExtensionsHeader(GeneratorContext ctx, FileDescriptorProto obj, ref object state)
        {
            var name = obj?.Options?.GetOptions()?.ExtensionTypeName;
            if (string.IsNullOrWhiteSpace(name)) name = "Extensions";
            // ctx.WriteLine($"{GetAccess(GetAccess(obj))} Module {Escape(name)}").Indent();
        }
        /// <summary>
        /// Ends an extgensions block
        /// </summary>
        protected override void WriteExtensionsFooter(GeneratorContext ctx, FileDescriptorProto obj, ref object state)
        {
            // ctx.Outdent().WriteLine("End Module");
        }
        /// <summary>
        /// Starts an extensions block
        /// </summary>
        protected override void WriteExtensionsHeader(GeneratorContext ctx, DescriptorProto obj, ref object state)
        {
            var name = obj?.Options?.GetOptions()?.ExtensionTypeName;
            if (string.IsNullOrWhiteSpace(name)) name = "Extensions";
            //ctx.WriteLine($"{GetAccess(GetAccess(obj))} Module {Escape(name)}").Indent();
        }
        /// <summary>
        /// Ends an extensions block
        /// </summary>
        protected override void WriteExtensionsFooter(GeneratorContext ctx, DescriptorProto obj, ref object state)
        {
            // ctx.Outdent().WriteLine("End Module");
        }
        /// <summary>
        /// Write an extension
        /// </summary>
        protected override void WriteExtension(GeneratorContext ctx, FieldDescriptorProto field)
        {
            var type = GetTypeName(ctx, field, out string dataFormat, out bool isMap);

            if (isMap)
            {
                ctx.WriteLine("REM #error map extensions not yet implemented");
            }
            else if (field.label == FieldDescriptorProto.Label.LabelRepeated)
            {
                ctx.WriteLine("REM #error repeated extensions not yet implemented");
            }
            else
            {
                ctx.WriteLine("REM #error extensions not yet implemented");
                //var msg = ctx.TryFind<DescriptorProto>(field.Extendee);
                //var extendee = MakeRelativeName(field, msg, ctx.NameNormalizer);

                //var @this = field.Parent is FileDescriptorProto ? "this " : "";
                //string name = ctx.NameNormalizer.GetName(field);
                //ctx.WriteLine($"{GetAccess(GetAccess(field))} static {type} Get{name}({@this}{extendee} obj)");

                //TextWriter tw;

                //    ctx.WriteLine("{").Indent();
                //    tw = ctx.Write("return ");

                //tw.Write($"obj == null ? default({type}) : global::ProtoBuf.Extensible.GetValue<{type}>(obj, {field.Number}");
                //if (!string.IsNullOrEmpty(dataFormat))
                //{
                //    tw.Write($", global::ProtoBuf.DataFormat.{dataFormat}");
                //}
                //tw.WriteLine(");");
                //if (ctx.Supports(CSharp6))
                //{
                //    ctx.Outdent().WriteLine();
                //}
                //else
                //{
                //    ctx.Outdent().WriteLine("}").WriteLine();
                //}

                //  GetValue<TValue>(IExtensible instance, int tag, DataFormat format)
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

        private string GetTypeName(GeneratorContext ctx, FieldDescriptorProto field, out string dataFormat, out bool isMap)
        {
            dataFormat = "";
            isMap = false;
            switch (field.type)
            {
                case FieldDescriptorProto.Type.TypeDouble:
                    return "Double";
                case FieldDescriptorProto.Type.TypeFloat:
                    return "Single";
                case FieldDescriptorProto.Type.TypeBool:
                    return "Boolean";
                case FieldDescriptorProto.Type.TypeString:
                    return "String";
                case FieldDescriptorProto.Type.TypeSint32:
                    dataFormat = nameof(DataFormat.ZigZag);
                    return "Integer";
                case FieldDescriptorProto.Type.TypeInt32:
                    return "Integer";
                case FieldDescriptorProto.Type.TypeSfixed32:
                    dataFormat = nameof(DataFormat.FixedSize);
                    return "Integer";
                case FieldDescriptorProto.Type.TypeSint64:
                    dataFormat = nameof(DataFormat.ZigZag);
                    return "Long";
                case FieldDescriptorProto.Type.TypeInt64:
                    return "Long";
                case FieldDescriptorProto.Type.TypeSfixed64:
                    dataFormat = nameof(DataFormat.FixedSize);
                    return "Long";
                case FieldDescriptorProto.Type.TypeFixed32:
                    dataFormat = nameof(DataFormat.FixedSize);
                    return "UInteger";
                case FieldDescriptorProto.Type.TypeUint32:
                    return "UInteger";
                case FieldDescriptorProto.Type.TypeFixed64:
                    dataFormat = nameof(DataFormat.FixedSize);
                    return "ULong";
                case FieldDescriptorProto.Type.TypeUint64:
                    return "ULong";
                case FieldDescriptorProto.Type.TypeBytes:
                    return "Byte()";
                case FieldDescriptorProto.Type.TypeEnum:
                    switch (field.TypeName)
                    {
                        case ".bcl.DateTime.DateTimeKind":
                            return "Global.System.DateTimeKind";
                    }
                    var enumType = ctx.TryFind<EnumDescriptorProto>(field.TypeName);
                    return MakeRelativeName(field, enumType, ctx.NameNormalizer);
                case FieldDescriptorProto.Type.TypeGroup:
                case FieldDescriptorProto.Type.TypeMessage:
                    switch (field.TypeName)
                    {
                        case WellKnownTypeTimestamp:
                            dataFormat = "WellKnown";
                            return "Date?";
                        case WellKnownTypeDuration:
                            dataFormat = "WellKnown";
                            return "Global.System.TimeSpan?";
                        case ".bcl.NetObjectProxy":
                            return "Object";
                        case ".bcl.DateTime":
                            return "Date?";
                        case ".bcl.TimeSpan":
                            return "Global.System.TimeSpan?";
                        case ".bcl.Decimal":
                            return "Decimal?";
                        case ".bcl.Guid":
                            return "Global.System.Guid?";
                    }
                    var msgType = ctx.TryFind<DescriptorProto>(field.TypeName);
                    if (field.type == FieldDescriptorProto.Type.TypeGroup)
                    {
                        dataFormat = nameof(DataFormat.Group);
                    }
                    isMap = msgType?.Options?.MapEntry ?? false;
                    return MakeRelativeName(field, msgType, ctx.NameNormalizer);
                default:
                    return field.TypeName;
            }
        }

        private string MakeRelativeName(FieldDescriptorProto field, IType target, NameNormalizer normalizer)
        {
            if (target == null) return Escape(field.TypeName); // the only thing we know

            var declaringType = field.Parent;

            if (declaringType is IType)
            {
                var name = FindNameFromCommonAncestor((IType)declaringType, target, normalizer);
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
            return Escape(field.TypeName); // give up!
        }

        // k, what we do is; we have two types; each knows the parent, but nothing else, so:
        // for each, use a stack to build the ancestry tree - the "top" of the stack will be the
        // package, the bottom of the stack will be the type itself. They will often be stacks
        // of different heights.
        //
        // Find how many is in the smallest stack; now take that many items, in turn, until we
        // get something that is different (at which point, put that one back on the stack), or 
        // we run out of items in one of the stacks.
        //
        // There are now two options:
        // - we ran out of things in the "target" stack - in which case, they are common enough to not
        //   need any resolution - just give back the fixed name
        // - we have things left in the "target" stack - in which case we have found a common ancestor,
        //   or the target is a descendent; either way, just concat what is left (including the package
        //   if the package itself was different)

        private string FindNameFromCommonAncestor(IType declaring, IType target, NameNormalizer normalizer)
        {
            // trivial case; asking for self, or asking for immediate child
            if (ReferenceEquals(declaring, target) || ReferenceEquals(declaring, target.Parent))
            {
                if (target is DescriptorProto) return Escape(normalizer.GetName((DescriptorProto)target));
                if (target is EnumDescriptorProto) return Escape(normalizer.GetName((EnumDescriptorProto)target));
                return null;
            }

            var origTarget = target;
            var xStack = new Stack<IType>();

            while (declaring != null)
            {
                xStack.Push(declaring);
                declaring = declaring.Parent;
            }
            var yStack = new Stack<IType>();

            while (target != null)
            {
                yStack.Push(target);
                target = target.Parent;
            }
            int lim = Math.Min(xStack.Count, yStack.Count);
            for (int i = 0; i < lim; i++)
            {
                declaring = xStack.Peek();
                target = yStack.Pop();
                if (!ReferenceEquals(target, declaring))
                {
                    // special-case: if both are the package (file), and they have the same namespace: we're OK
                    if (target is FileDescriptorProto && declaring is FileDescriptorProto &&
                        normalizer.GetName((FileDescriptorProto)declaring) == normalizer.GetName((FileDescriptorProto)target))
                    {
                        // that's fine, keep going
                    }
                    else
                    {
                        // put it back
                        yStack.Push(target);
                        break;
                    }
                }
            }
            // if we used everything, then the target is an ancestor-or-self
            if (yStack.Count == 0)
            {
                target = origTarget;
                if (target is DescriptorProto) return Escape(normalizer.GetName((DescriptorProto)target));
                if (target is EnumDescriptorProto) return Escape(normalizer.GetName((EnumDescriptorProto)target));
                return null;
            }

            var sb = new StringBuilder();
            while (yStack.Count != 0)
            {
                target = yStack.Pop();

                string nextName;
                if (target is FileDescriptorProto) nextName = normalizer.GetName((FileDescriptorProto)target);
                else if (target is DescriptorProto) nextName = normalizer.GetName((DescriptorProto)target);
                else if (target is EnumDescriptorProto) nextName = normalizer.GetName((EnumDescriptorProto)target);
                else return null;

                if (!string.IsNullOrWhiteSpace(nextName))
                {
                    if (sb.Length == 0 && target is FileDescriptorProto) sb.Append("Global.");
                    else if (sb.Length != 0) sb.Append('.');
                    sb.Append(Escape(nextName));
                }
            }
            return sb.ToString();
        }

        static bool IsAncestorOrSelf(IType parent, IType child)
        {
            while (parent != null)
            {
                if (ReferenceEquals(parent, child)) return true;
                parent = parent.Parent;
            }
            return false;
        }
        const string WellKnownTypeTimestamp = ".google.protobuf.Timestamp",
                     WellKnownTypeDuration = ".google.protobuf.Duration";
    }
}
