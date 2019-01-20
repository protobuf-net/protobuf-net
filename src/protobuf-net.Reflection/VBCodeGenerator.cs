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
    public class VBCodeGenerator : CommonCodeGenerator
    {
        private static readonly Version VB14 = new Version(14, 0), VB11 = new Version(11, 0);

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
        /// Get the language version for this language from a schema
        /// </summary>
        protected override string GetLanguageVersion(FileDescriptorProto obj)
            => obj?.Options?.GetOptions()?.VisualBasicLanguageVersion;

        /// <summary>
        /// Returns the default file extension
        /// </summary>
        protected override string DefaultFileExtension => "vb";

        private static readonly HashSet<string> keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "AddHandler",
            "AddressOf",
            "Alias",
            "And",
            "AndAlso",
            "As",
            "Boolean",
            "ByRef",
            "Byte",
            "ByVal",
            "Call",
            "Case",
            "Catch",
            "CBool",
            "CByte",
            "CChar",
            "CDate",
            "CDbl",
            "CDec",
            "Char",
            "CInt",
            "Class",
            "Constraint",
            "CLng",
            "CObj",
            "Const",
            "Continue",
            "CSByte",
            "CShort",
            "CSng",
            "CStr",
            "CType",
            "CUInt",
            "CULng",
            "CUShort",
            "Date",
            "Decimal",
            "Declare",
            "Default",
            "Delegate",
            "Dim",
            "DirectCast",
            "Do",
            "Double",
            "Each",
            "Else",
            "ElseIf",
            "End",
            "EndIf",
            "Enum",
            "Erase",
            "Error",
            "Event",
            "Exit",
            "False",
            "Finally",
            "For",
            "Friend",
            "Function",
            "Get",
            "GetType",
            "GetXMLNamespace",
            "Global",
            "GoSub",
            "GoTo",
            "Handles",
            "If",
            "Implements",
            "Imports",
            "In",
            "Inherits",
            "Integer",
            "Interface",
            "Is",
            "IsNot",
            "Let",
            "Lib",
            "Like",
            "Long",
            "Loop",
            "Me",
            "Mod",
            "Module",
            "MustInherit",
            "MustOverride",
            "MyBase",
            "MyClass",
            "Namespace",
            "Narrowing",
            "New",
            "Next",
            "Not",
            "Nothing",
            "NotInheritable",
            "NotOverridable",
            "Object",
            "Of",
            "On",
            "Operator",
            "Option",
            "Optional",
            "Or",
            "OrElse",
            "Out",
            "Overloads",
            "Overridable",
            "Overrides",
            "ParamArray",
            "Partial",
            "Private",
            "Property",
            "Protected",
            "Public",
            "RaiseEvent",
            "ReadOnly",
            "ReDim",
            "REM",
            "RemoveHandler",
            "Resume",
            "Return",
            "SByte",
            "Select",
            "Set",
            "Shadows",
            "Shared",
            "Short",
            "Single",
            "Static",
            "Step",
            "Stop",
            "String",
            "Structure",
            "Sub",
            "SyncLock",
            "Then",
            "Throw",
            "To",
            "True",
            "Try",
            "TryCast",
            "TypeOf",
            "UInteger",
            "ULong",
            "UShort",
            "Using",
            "Variant",
            "Wend",
            "When",
            "While",
            "Widening",
            "With",
            "WithEvents",
            "WriteOnly",
            "Xor",
        };
        /// <summary>
        /// Escapes language keywords
        /// </summary>
        protected override string Escape(string identifier)
        {
            if (keywords.Contains(identifier))
                return "[" + identifier + "]";
            return identifier;
        }

        /// <summary>
        /// Start a file
        /// </summary>
        protected override void WriteFileHeader(GeneratorContext ctx, FileDescriptorProto file, ref object state)
        {
            //var prefix = ctx.Supports(CSharp6) ? "CS" : "";
            ctx.WriteLine("' <auto-generated>")
               .WriteLine("'   This file was generated by a tool; you should avoid making direct changes.")
               .WriteLine("'   Consider using 'partial classes' to extend these types")
               .WriteLine($"'   Input: {Path.GetFileName(ctx.File.Name)}")
               .WriteLine("' </auto-generated>")
               .WriteLine();

            if (ctx.Supports(VB14))
            {
                ctx.WriteLine($"#Disable Warning BC40008, BC40055, IDE1006").WriteLine();
            }

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
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                ctx.Outdent().WriteLine("End Namespace").WriteLine();
            }
            if (ctx.Supports(VB14))
            {
                ctx.WriteLine($"#Enable Warning BC40008, BC40055, IDE1006").WriteLine();
            }
        }

        /// <summary>
        /// Start an enum
        /// </summary>
        protected override void WriteEnumHeader(GeneratorContext ctx, EnumDescriptorProto @enum, ref object state)
        {
            var name = ctx.NameNormalizer.GetName(@enum);
            var tw = ctx.Write("<Global.ProtoBuf.ProtoContract(");
            if (name != @enum.Name) tw.Write($@"Name := ""{@enum.Name}""");
            tw.WriteLine(")> _");
            WriteOptions(ctx, @enum.Options);
            ctx.WriteLine($"{GetAccess(GetAccess(@enum))} Enum {Escape(name)}").Indent();
        }
        /// <summary>
        /// End an enum
        /// </summary>

        protected override void WriteEnumFooter(GeneratorContext ctx, EnumDescriptorProto @enum, ref object state)
        {
            ctx.Outdent().WriteLine("End Enum").WriteLine();
        }

        /// <summary>
        /// Write an enum value
        /// </summary>
        protected override void WriteEnumValue(GeneratorContext ctx, EnumValueDescriptorProto @enum, ref object state)
        {
            var name = ctx.NameNormalizer.GetName(@enum);
            if (name != @enum.Name)
            {
                var tw = ctx.Write("<Global.ProtoBuf.ProtoEnum(");
                tw.Write($@"Name := ""{@enum.Name}""");
                tw.WriteLine(")> _");
            }

            WriteOptions(ctx, @enum.Options);
            ctx.WriteLine($"{Escape(name)} = {@enum.Number}");
        }
        private static string GetOneOfFieldName(OneofDescriptorProto obj) => FieldPrefix + obj.Name;

        /// <summary>
        /// Emit  the discriminator accessor for 'oneof' groups
        /// </summary>
        protected override void WriteOneOfDiscriminator(GeneratorContext ctx, OneofDescriptorProto oneof, ref object state)
        {
            var name = ctx.NameNormalizer.GetName(oneof);
            var fieldName = GetOneOfFieldName(oneof);
            ctx.WriteLine($"Public ReadOnly Property {name}{OneOfEnumSuffixDiscriminator} As {name}{OneOfEnumSuffixEnum}").Indent().WriteLine("Get").Indent()
                .WriteLine($"Return {fieldName}.Discriminator")
                .Outdent().WriteLine("End Get").Outdent().WriteLine("End Property").WriteLine();
        }

        /// <summary>
        /// Emit the end of an enum declaration for 'oneof' groups
        /// </summary>
        protected override void WriteOneOfEnumFooter(GeneratorContext ctx, OneofDescriptorProto oneof, ref object state)
        {
            ctx.Outdent().WriteLine("End Enum").WriteLine();
        }

        /// <summary>
        /// Emit the start of an enum declaration for 'oneof' groups, including the 0/None element
        /// </summary>
        protected override void WriteOneOfEnumHeader(GeneratorContext ctx, OneofDescriptorProto oneof, ref object state)
        {
            ctx.WriteLine($"Public Enum {Escape(ctx.NameNormalizer.GetName(oneof))}{OneOfEnumSuffixEnum}").Indent().WriteLine("None = 0");
        }

        /// <summary>
        /// Emit a field-based entry for a 'oneof' groups's enum
        /// </summary>
        protected override void WriteOneOfEnumValue(GeneratorContext ctx, FieldDescriptorProto field, ref object state)
        {
            var name = ctx.NameNormalizer.GetName(field);
            ctx.WriteLine($"{Escape(name)} = {field.Number}");
        }

        /// <summary>
        /// End a message
        /// </summary>
        protected override void WriteMessageFooter(GeneratorContext ctx, DescriptorProto message, ref object state)
        {
            ctx.Outdent().WriteLine("End Class").WriteLine();
        }
        /// <summary>
        /// Start a message
        /// </summary>
        protected override void WriteMessageHeader(GeneratorContext ctx, DescriptorProto message, ref object state)
        {
            var name = ctx.NameNormalizer.GetName(message);
            var tw = ctx.Write("<Global.ProtoBuf.ProtoContract(");
            if (name != message.Name) tw.Write($@"Name := ""{message.Name}""");
            tw.WriteLine(")> _");
            WriteOptions(ctx, message.Options);
            ctx.WriteLine($"Partial {GetAccess(GetAccess(message))} Class {Escape(name)}");
            ctx.Indent().WriteLine("Implements Global.ProtoBuf.IExtensible").WriteLine();

            if (message.Options?.MessageSetWireFormat == true)
            {
                ctx.WriteLine("REM #error message_set_wire_format is not currently implemented").WriteLine();
            }

            ctx.WriteLine($"Private {FieldPrefix}extensionData As Global.ProtoBuf.IExtension").WriteLine()
                .WriteLine($"Private Function GetExtensionObject(ByVal createIfMissing As Boolean) As Global.ProtoBuf.IExtension Implements Global.ProtoBuf.IExtensible.GetExtensionObject")
                .Indent().WriteLine($"Return Global.ProtoBuf.Extensible.GetExtensionObject({FieldPrefix}extensionData, createIfMissing)")
                .Outdent().WriteLine("End Function").WriteLine();
        }

        private static void WriteOptions<T>(GeneratorContext ctx, T obj) where T : class, ISchemaOptions
        {
            if (obj == null) return;
            if (obj.Deprecated)
            {
                ctx.WriteLine($"<Global.System.Obsolete> _");
            }
        }

        private const string FieldPrefix = "__pbn__";

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
                            found = enumType.Values.Find(x => x.Name == defaultValue);
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
                else if (!string.IsNullOrWhiteSpace(defaultValue))
                {
                    switch (typeName)
                    {
                        case "UInteger": defaultValue += "UI"; break;
                        case "ULong": defaultValue += "UL"; break;
                    }
                }
            }

            return defaultValue;
        }
        /// <summary>
        /// Write a field
        /// </summary>
        protected override void WriteField(GeneratorContext ctx, FieldDescriptorProto field, ref object state, OneOfStub[] oneOfs)
        {
            var name = ctx.NameNormalizer.GetName(field);

            var tw = ctx.Write($"<Global.ProtoBuf.ProtoMember({field.Number}");
            if (name != field.Name)
            {
                tw.Write($@", Name := ""{field.Name}""");
            }
            var options = field.Options?.GetOptions();
            if (options?.AsReference == true)
            {
                tw.Write(", AsReference := True");
            }
            if (options?.DynamicType == true)
            {
                tw.Write(", DynamicType := True");
            }

            bool isOptional = field.label == FieldDescriptorProto.Label.LabelOptional;
            bool isRepeated = field.label == FieldDescriptorProto.Label.LabelRepeated;

            OneOfStub oneOf = field.ShouldSerializeOneofIndex() ? oneOfs?[field.OneofIndex] : null;
            if (oneOf != null && !ctx.OneOfEnums && oneOf.CountTotal == 1)
            {
                oneOf = null; // not really a one-of, then!
            }
            bool explicitValues = isOptional && oneOf == null && ctx.Syntax == FileDescriptorProto.SyntaxProto2
                && field.type != FieldDescriptorProto.Type.TypeMessage
                && field.type != FieldDescriptorProto.Type.TypeGroup;

            bool suppressDefaultAttribute = !isOptional;
            var typeName = GetTypeName(ctx, field, out var dataFormat, out var isMap);

            string defaultValue = GetDefaultValue(ctx, field, typeName);

            if (!string.IsNullOrWhiteSpace(dataFormat))
            {
                tw.Write($", DataFormat := Global.ProtoBuf.DataFormat.{dataFormat}");
            }
            if (field.IsPacked(ctx.Syntax))
            {
                tw.Write($", IsPacked := True");
            }
            if (field.label == FieldDescriptorProto.Label.LabelRequired)
            {
                tw.Write($", IsRequired := True");
            }
            tw.WriteLine(")> _");
            if (!isRepeated && !string.IsNullOrWhiteSpace(defaultValue) && !suppressDefaultAttribute)
            {
                ctx.WriteLine($"<Global.System.ComponentModel.DefaultValue({defaultValue})> _");
            }
            WriteOptions(ctx, field.Options);
            if (isRepeated)
            {
                var mapMsgType = isMap ? ctx.TryFind<DescriptorProto>(field.TypeName) : null;
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

                    if (ctx.Supports(VB14))
                    {
                        ctx.WriteLine($"{GetAccess(GetAccess(field))} Readonly Property {Escape(name)} As New Global.System.Collections.Generic.Dictionary(Of {keyTypeName}, {valueTypeName})");
                    }
                    else
                    {
                        var fieldName = FieldPrefix + name;
                        ctx.WriteLine($"{GetAccess(GetAccess(field))} Readonly Property {Escape(name)} As Global.System.Collections.Generic.Dictionary(Of {keyTypeName}, {valueTypeName})").Indent()
                            .WriteLine("Get").Indent()
                            .WriteLine($"Return {fieldName}")
                            .Outdent().WriteLine("End Get").Outdent().WriteLine("End Property").WriteLine()
                            .WriteLine($"Private ReadOnly {fieldName} As New Global.System.Collections.Generic.Dictionary(Of {keyTypeName}, {valueTypeName})").WriteLine();
                    }
                }
                else if (UseArray(field))
                {
                    if (ctx.Supports(VB11))
                    {
                        ctx.WriteLine($"{GetAccess(GetAccess(field))} Property {Escape(name)} As {typeName}()");
                    }
                    else
                    {
                        var fieldName = FieldPrefix + name;
                        ctx.WriteLine($"{GetAccess(GetAccess(field))} Property {Escape(name)} As {typeName}()")
                            .Indent().WriteLine("Get").Indent().WriteLine($"Return {fieldName}").Outdent().WriteLine("End Get").Outdent()
                            .Indent().WriteLine($"Set(ByVal value as {typeName}())").Indent().WriteLine($"{fieldName} = value").Outdent().WriteLine("End Set").Outdent()
                            .WriteLine("End Property").WriteLine()
                            .WriteLine($"Private {fieldName} As {typeName}()").WriteLine();
                    }
                }
                else
                {
                    if (ctx.Supports(VB14))
                    {
                        ctx.WriteLine($"{GetAccess(GetAccess(field))} Readonly Property {Escape(name)} As New Global.System.Collections.Generic.List(Of {typeName})");
                    }
                    else
                    {
                        var fieldName = FieldPrefix + name;
                        ctx.WriteLine($"{GetAccess(GetAccess(field))} Readonly Property {Escape(name)} As Global.System.Collections.Generic.List(Of {typeName})").Indent()
                            .WriteLine("Get").Indent()
                            .WriteLine($"Return {fieldName}")
                            .Outdent().WriteLine("End Get").Outdent().WriteLine("End Property").WriteLine()
                            .WriteLine($"Private ReadOnly {fieldName} As New Global.System.Collections.Generic.List(Of {typeName})").WriteLine();
                    }
                }
            }
            else if (oneOf != null)
            {
                var defValue = string.IsNullOrWhiteSpace(defaultValue) ? $"CType(Nothing, {typeName})" : defaultValue;
                var fieldName = GetOneOfFieldName(oneOf.OneOf);
                var storage = oneOf.GetStorage(field.type, field.TypeName);
                ctx.WriteLine($"{GetAccess(GetAccess(field))} Property {Escape(name)} As {typeName}").Indent().WriteLine("Get").Indent();
                switch (field.type)
                {
                    case FieldDescriptorProto.Type.TypeMessage:
                    case FieldDescriptorProto.Type.TypeGroup:
                    case FieldDescriptorProto.Type.TypeEnum:
                    case FieldDescriptorProto.Type.TypeBytes:
                    case FieldDescriptorProto.Type.TypeString:
                        ctx.WriteLine($"Return If({fieldName}.Is({field.Number}), CType({fieldName}.{storage}, {typeName}), {defValue})");
                        break;
                    default:
                        ctx.WriteLine($"Return If({fieldName}.Is({field.Number}), {fieldName}.{storage}, {defValue})");
                        break;
                }
                ctx.Outdent().WriteLine("End Get");

                var unionType = oneOf.GetUnionType();

                ctx.WriteLine($"Set(ByVal value As {typeName})").Indent()
                    .WriteLine($"{fieldName} = New Global.ProtoBuf.{unionType}({field.Number}, value)").Outdent().WriteLine("End Set");

                ctx.Outdent().WriteLine("End Property").WriteLine();

                ctx.WriteLine($"{GetAccess(GetAccess(field))} Function ShouldSerialize{name}() As Boolean").Indent()
                    .WriteLine($"Return {fieldName}.Is({field.Number})").Outdent()
                    .WriteLine("End Function").WriteLine()
                    .WriteLine($"{GetAccess(GetAccess(field))} Sub Reset{name}()").Indent()
                    .WriteLine($"Global.ProtoBuf.{unionType}.Reset({fieldName}, {field.Number})")
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

                ctx.WriteLine($"{GetAccess(GetAccess(field))} Property {Escape(name)} As {typeName}").Indent()
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

                ctx.WriteLine($"{GetAccess(GetAccess(field))} Function ShouldSerialize{name}() As Boolean").Indent()
                    .WriteLine($"Return Not ({fieldName} Is Nothing)").Outdent()
                    .WriteLine("End Function")
                    .WriteLine($"{GetAccess(GetAccess(field))} Sub Reset{name}()").Indent()
                    .WriteLine($"{fieldName} = Nothing").Outdent().WriteLine("End Sub");

                ctx.WriteLine($"Private {fieldName} As {fieldType}");
            }
            else
            {
                if (ctx.Supports(VB11))
                {
                    tw = ctx.Write($"{GetAccess(GetAccess(field))} Property {Escape(name)} As {typeName}");
                    if (!string.IsNullOrWhiteSpace(defaultValue)) tw.Write($" = {defaultValue}");
                    tw.WriteLine();
                }
                else
                {
                    var fieldName = FieldPrefix + name;
                    tw = ctx.WriteLine($"{GetAccess(GetAccess(field))} Property {Escape(name)} As {typeName}")
                        .Indent().WriteLine("Get").Indent().WriteLine($"Return {fieldName}").Outdent().WriteLine("End Get").Outdent()
                        .Indent().WriteLine($"Set(ByVal value as {typeName})").Indent().WriteLine($"{fieldName} = value").Outdent().WriteLine("End Set").Outdent()
                        .WriteLine("End Property").WriteLine()
                        .Write($"Private {fieldName} As {typeName}");
                    if (!string.IsNullOrWhiteSpace(defaultValue)) tw.Write($" = {defaultValue}");
                    tw.WriteLine();
                }
            }
            ctx.WriteLine();
        }

        /// <summary>
        /// Starts an extgensions block
        /// </summary>
        protected override void WriteExtensionsHeader(GeneratorContext ctx, FileDescriptorProto file, ref object state)
        {
            var name = file?.Options?.GetOptions()?.ExtensionTypeName;
            if (string.IsNullOrWhiteSpace(name)) name = "Extensions";
            ctx.WriteLine("<Global.System.Runtime.CompilerServices.Extension> _")
               .WriteLine($"{GetAccess(GetAccess(file))} Module {Escape(name)}").Indent();
        }
        /// <summary>
        /// Ends an extgensions block
        /// </summary>
        protected override void WriteExtensionsFooter(GeneratorContext ctx, FileDescriptorProto file, ref object state)
        {
            ctx.Outdent().WriteLine("End Module");
        }
        /// <summary>
        /// Starts an extensions block
        /// </summary>
        protected override void WriteExtensionsHeader(GeneratorContext ctx, DescriptorProto message, ref object state)
        {
            var name = message?.Options?.GetOptions()?.ExtensionTypeName;
            if (string.IsNullOrWhiteSpace(name)) name = "Extensions";
            //ctx.WriteLine($"{GetAccess(GetAccess(obj))} Module {Escape(name)}").Indent();
        }
        /// <summary>
        /// Ends an extensions block
        /// </summary>
        protected override void WriteExtensionsFooter(GeneratorContext ctx, DescriptorProto message, ref object state)
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
                ctx.WriteLine("REM #error map extensions not yet implemented; please file an issue");
            }
            else if (field.label == FieldDescriptorProto.Label.LabelRepeated)
            {
                ctx.WriteLine("REM #error repeated extensions not yet implemented; please file an issue");
            }
            else
            {
                var msg = ctx.TryFind<DescriptorProto>(field.Extendee);
                var extendee = MakeRelativeName(field, msg, ctx.NameNormalizer);

                string name = ctx.NameNormalizer.GetName(field);
                string shared = "";
                if (field.Parent is FileDescriptorProto)
                {
                    ctx.WriteLine("<Global.System.Runtime.CompilerServices.Extension> _");
                }
                else
                {
                    shared = "Shared ";
                }
                ctx.WriteLine($"{GetAccess(GetAccess(field))} {shared}Function Get{name}(ByVal obj As {extendee}) As {type}");

                var tw = ctx.Indent().Write($"Return If(obj Is Nothing, CType(Nothing, {type}), Global.ProtoBuf.Extensible.GetValue(Of {type})(obj, {field.Number}");
                if (!string.IsNullOrEmpty(dataFormat))
                {
                    tw.Write($", Global.ProtoBuf.DataFormat.{dataFormat}");
                }
                tw.WriteLine("))");
                ctx.Outdent().WriteLine("End Function");
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

            if (declaringType is IType type)
            {
                var name = FindNameFromCommonAncestor(type, target, normalizer);
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
                if (target is DescriptorProto message) return Escape(normalizer.GetName(message));
                if (target is EnumDescriptorProto @enum) return Escape(normalizer.GetName(@enum));
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
                    if (target is FileDescriptorProto && declaring is FileDescriptorProto
                        && normalizer.GetName((FileDescriptorProto)declaring) == normalizer.GetName((FileDescriptorProto)target))
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
                if (target is DescriptorProto message) return Escape(normalizer.GetName(message));
                if (target is EnumDescriptorProto @enum) return Escape(normalizer.GetName(@enum));
                return null;
            }

            var sb = new StringBuilder();
            while (yStack.Count != 0)
            {
                target = yStack.Pop();

                string nextName;
                if (target is FileDescriptorProto file) nextName = normalizer.GetName(file);
                else if (target is DescriptorProto message) nextName = normalizer.GetName(message);
                else if (target is EnumDescriptorProto @enum) nextName = normalizer.GetName(@enum);
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

        private const string WellKnownTypeTimestamp = ".google.protobuf.Timestamp",
                     WellKnownTypeDuration = ".google.protobuf.Duration";
    }
}
