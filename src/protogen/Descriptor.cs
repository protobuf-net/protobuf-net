#pragma warning disable CS1591
namespace Google.Protobuf.Reflection
{
    [global::ProtoBuf.ProtoContract(Name = @"FileDescriptorSet")]
    public partial class FileDescriptorSet
    {
        [global::ProtoBuf.ProtoMember(1, Name = @"file")]
        public global::System.Collections.Generic.List<FileDescriptorProto> Files { get; } = new global::System.Collections.Generic.List<FileDescriptorProto>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"FileDescriptorProto")]
    public partial class FileDescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1, Name = @"name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2, Name = @"package")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Package { get; set; } = "";
        [global::ProtoBuf.ProtoMember(3, Name = @"dependency")]
        public global::System.Collections.Generic.List<string> Dependencies { get; } = new global::System.Collections.Generic.List<string>();
        [global::ProtoBuf.ProtoMember(10, Name = @"public_dependency")]
        public int[] PublicDependencies { get; set; }
        [global::ProtoBuf.ProtoMember(11, Name = @"weak_dependency")]
        public int[] WeakDependencies { get; set; }
        [global::ProtoBuf.ProtoMember(4, Name = @"message_type")]
        public global::System.Collections.Generic.List<DescriptorProto> MessageTypes { get; } = new global::System.Collections.Generic.List<DescriptorProto>();
        [global::ProtoBuf.ProtoMember(5, Name = @"enum_type")]
        public global::System.Collections.Generic.List<EnumDescriptorProto> EnumTypes { get; } = new global::System.Collections.Generic.List<EnumDescriptorProto>();
        [global::ProtoBuf.ProtoMember(6, Name = @"service")]
        public global::System.Collections.Generic.List<ServiceDescriptorProto> Services { get; } = new global::System.Collections.Generic.List<ServiceDescriptorProto>();
        [global::ProtoBuf.ProtoMember(7, Name = @"extension")]
        public global::System.Collections.Generic.List<FieldDescriptorProto> Extensions { get; } = new global::System.Collections.Generic.List<FieldDescriptorProto>();
        [global::ProtoBuf.ProtoMember(8, Name = @"options")]
        public FileOptions Options { get; set; }
        [global::ProtoBuf.ProtoMember(9, Name = @"source_code_info")]
        public SourceCodeInfo SourceCodeInfo { get; set; }
        [global::ProtoBuf.ProtoMember(12, Name = @"syntax")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Syntax { get; set; } = "";
    }
    [global::ProtoBuf.ProtoContract(Name = @"DescriptorProto")]
    public partial class DescriptorProto
    {
        [global::ProtoBuf.ProtoContract(Name = @"ExtensionRange")]
        public partial class ExtensionRange
        {
            [global::ProtoBuf.ProtoMember(1, Name = @"start")]
            public int Start { get; set; }
            [global::ProtoBuf.ProtoMember(2, Name = @"end")]
            public int End { get; set; }
        }
        [global::ProtoBuf.ProtoContract(Name = @"ReservedRange")]
        public partial class ReservedRange
        {
            [global::ProtoBuf.ProtoMember(1, Name = @"start")]
            public int Start { get; set; }
            [global::ProtoBuf.ProtoMember(2, Name = @"end")]
            public int End { get; set; }
        }
        [global::ProtoBuf.ProtoMember(1, Name = @"name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2, Name = @"field")]
        public global::System.Collections.Generic.List<FieldDescriptorProto> Fields { get; } = new global::System.Collections.Generic.List<FieldDescriptorProto>();
        [global::ProtoBuf.ProtoMember(6, Name = @"extension")]
        public global::System.Collections.Generic.List<FieldDescriptorProto> Extensions { get; } = new global::System.Collections.Generic.List<FieldDescriptorProto>();
        [global::ProtoBuf.ProtoMember(3, Name = @"nested_type")]
        public global::System.Collections.Generic.List<DescriptorProto> NestedTypes { get; } = new global::System.Collections.Generic.List<DescriptorProto>();
        [global::ProtoBuf.ProtoMember(4, Name = @"enum_type")]
        public global::System.Collections.Generic.List<EnumDescriptorProto> EnumTypes { get; } = new global::System.Collections.Generic.List<EnumDescriptorProto>();
        [global::ProtoBuf.ProtoMember(5, Name = @"extension_range")]
        public global::System.Collections.Generic.List<ExtensionRange> ExtensionRanges { get; } = new global::System.Collections.Generic.List<ExtensionRange>();
        [global::ProtoBuf.ProtoMember(8, Name = @"oneof_decl")]
        public global::System.Collections.Generic.List<OneofDescriptorProto> OneofDecls { get; } = new global::System.Collections.Generic.List<OneofDescriptorProto>();
        [global::ProtoBuf.ProtoMember(7, Name = @"options")]
        public MessageOptions Options { get; set; }
        [global::ProtoBuf.ProtoMember(9, Name = @"reserved_range")]
        public global::System.Collections.Generic.List<ReservedRange> ReservedRanges { get; } = new global::System.Collections.Generic.List<ReservedRange>();
        [global::ProtoBuf.ProtoMember(10, Name = @"reserved_name")]
        public global::System.Collections.Generic.List<string> ReservedNames { get; } = new global::System.Collections.Generic.List<string>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"FieldDescriptorProto")]
    public partial class FieldDescriptorProto
    {
        [global::ProtoBuf.ProtoContract(Name = @"Type")]
        public enum Type
        {
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_DOUBLE", Value = 1)]
            TypeDouble = 1,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_FLOAT", Value = 2)]
            TypeFloat = 2,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_INT64", Value = 3)]
            TypeInt64 = 3,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_UINT64", Value = 4)]
            TypeUint64 = 4,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_INT32", Value = 5)]
            TypeInt32 = 5,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_FIXED64", Value = 6)]
            TypeFixed64 = 6,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_FIXED32", Value = 7)]
            TypeFixed32 = 7,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_BOOL", Value = 8)]
            TypeBool = 8,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_STRING", Value = 9)]
            TypeString = 9,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_GROUP", Value = 10)]
            TypeGroup = 10,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_MESSAGE", Value = 11)]
            TypeMessage = 11,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_BYTES", Value = 12)]
            TypeBytes = 12,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_UINT32", Value = 13)]
            TypeUint32 = 13,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_ENUM", Value = 14)]
            TypeEnum = 14,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_SFIXED32", Value = 15)]
            TypeSfixed32 = 15,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_SFIXED64", Value = 16)]
            TypeSfixed64 = 16,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_SINT32", Value = 17)]
            TypeSint32 = 17,
            [global::ProtoBuf.ProtoEnum(Name = @"TYPE_SINT64", Value = 18)]
            TypeSint64 = 18,
        }
        [global::ProtoBuf.ProtoContract(Name = @"Label")]
        public enum Label
        {
            [global::ProtoBuf.ProtoEnum(Name = @"LABEL_OPTIONAL", Value = 1)]
            LabelOptional = 1,
            [global::ProtoBuf.ProtoEnum(Name = @"LABEL_REQUIRED", Value = 2)]
            LabelRequired = 2,
            [global::ProtoBuf.ProtoEnum(Name = @"LABEL_REPEATED", Value = 3)]
            LabelRepeated = 3,
        }
        [global::ProtoBuf.ProtoMember(1, Name = @"name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(3, Name = @"number")]
        public int Number { get; set; }
        [global::ProtoBuf.ProtoMember(4, Name = @"label")]
        public Label label { get; set; }
        [global::ProtoBuf.ProtoMember(5, Name = @"type")]
        public Type type { get; set; }
        [global::ProtoBuf.ProtoMember(6, Name = @"type_name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string TypeName { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2, Name = @"extendee")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Extendee { get; set; } = "";
        [global::ProtoBuf.ProtoMember(7, Name = @"default_value")]
        [global::System.ComponentModel.DefaultValue("")]
        public string DefaultValue { get; set; } = "";
        [global::ProtoBuf.ProtoMember(9, Name = @"oneof_index")]
        public int OneofIndex { get; set; }
        [global::ProtoBuf.ProtoMember(10, Name = @"json_name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string JsonName { get; set; } = "";
        [global::ProtoBuf.ProtoMember(8, Name = @"options")]
        public FieldOptions Options { get; set; }
    }
    [global::ProtoBuf.ProtoContract(Name = @"OneofDescriptorProto")]
    public partial class OneofDescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1, Name = @"name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2, Name = @"options")]
        public OneofOptions Options { get; set; }
    }
    [global::ProtoBuf.ProtoContract(Name = @"EnumDescriptorProto")]
    public partial class EnumDescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1, Name = @"name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2, Name = @"value")]
        public global::System.Collections.Generic.List<EnumValueDescriptorProto> Values { get; } = new global::System.Collections.Generic.List<EnumValueDescriptorProto>();
        [global::ProtoBuf.ProtoMember(3, Name = @"options")]
        public EnumOptions Options { get; set; }
    }
    [global::ProtoBuf.ProtoContract(Name = @"EnumValueDescriptorProto")]
    public partial class EnumValueDescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1, Name = @"name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2, Name = @"number")]
        public int Number { get; set; }
        [global::ProtoBuf.ProtoMember(3, Name = @"options")]
        public EnumValueOptions Options { get; set; }
    }
    [global::ProtoBuf.ProtoContract(Name = @"ServiceDescriptorProto")]
    public partial class ServiceDescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1, Name = @"name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2, Name = @"method")]
        public global::System.Collections.Generic.List<MethodDescriptorProto> Methods { get; } = new global::System.Collections.Generic.List<MethodDescriptorProto>();
        [global::ProtoBuf.ProtoMember(3, Name = @"options")]
        public ServiceOptions Options { get; set; }
    }
    [global::ProtoBuf.ProtoContract(Name = @"MethodDescriptorProto")]
    public partial class MethodDescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1, Name = @"name")]
        [global::System.ComponentModel.DefaultValue("")]
        public string Name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2, Name = @"input_type")]
        [global::System.ComponentModel.DefaultValue("")]
        public string InputType { get; set; } = "";
        [global::ProtoBuf.ProtoMember(3, Name = @"output_type")]
        [global::System.ComponentModel.DefaultValue("")]
        public string OutputType { get; set; } = "";
        [global::ProtoBuf.ProtoMember(4, Name = @"options")]
        public MethodOptions Options { get; set; }
        [global::ProtoBuf.ProtoMember(5, Name = @"client_streaming")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool ClientStreaming { get; set; } = false;
        [global::ProtoBuf.ProtoMember(6, Name = @"server_streaming")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool ServerStreaming { get; set; } = false;
    }
    [global::ProtoBuf.ProtoContract(Name = @"FileOptions")]
    public partial class FileOptions
    {
        [global::ProtoBuf.ProtoContract(Name = @"OptimizeMode")]
        public enum OptimizeMode
        {
            [global::ProtoBuf.ProtoEnum(Name = @"SPEED", Value = 1)]
            Speed = 1,
            [global::ProtoBuf.ProtoEnum(Name = @"CODE_SIZE", Value = 2)]
            CodeSize = 2,
            [global::ProtoBuf.ProtoEnum(Name = @"LITE_RUNTIME", Value = 3)]
            LiteRuntime = 3,
        }
        [global::ProtoBuf.ProtoMember(1, Name = @"java_package")]
        [global::System.ComponentModel.DefaultValue("")]
        public string JavaPackage { get; set; } = "";
        [global::ProtoBuf.ProtoMember(8, Name = @"java_outer_classname")]
        [global::System.ComponentModel.DefaultValue("")]
        public string JavaOuterClassname { get; set; } = "";
        [global::ProtoBuf.ProtoMember(10, Name = @"java_multiple_files")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool JavaMultipleFiles { get; set; } = false;
        [global::ProtoBuf.ProtoMember(20, Name = @"java_generate_equals_and_hash")]
        [global::System.Obsolete]
        public bool JavaGenerateEqualsAndHash { get; set; }
        [global::ProtoBuf.ProtoMember(27, Name = @"java_string_check_utf8")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool JavaStringCheckUtf8 { get; set; } = false;
        [global::ProtoBuf.ProtoMember(9, Name = @"optimize_for")]
        [global::System.ComponentModel.DefaultValue(OptimizeMode.Speed)]
        public OptimizeMode OptimizeFor { get; set; } = OptimizeMode.Speed;
        [global::ProtoBuf.ProtoMember(11, Name = @"go_package")]
        [global::System.ComponentModel.DefaultValue("")]
        public string GoPackage { get; set; } = "";
        [global::ProtoBuf.ProtoMember(16, Name = @"cc_generic_services")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool CcGenericServices { get; set; } = false;
        [global::ProtoBuf.ProtoMember(17, Name = @"java_generic_services")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool JavaGenericServices { get; set; } = false;
        [global::ProtoBuf.ProtoMember(18, Name = @"py_generic_services")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool PyGenericServices { get; set; } = false;
        [global::ProtoBuf.ProtoMember(23, Name = @"deprecated")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool Deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(31, Name = @"cc_enable_arenas")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool CcEnableArenas { get; set; } = false;
        [global::ProtoBuf.ProtoMember(36, Name = @"objc_class_prefix")]
        [global::System.ComponentModel.DefaultValue("")]
        public string ObjcClassPrefix { get; set; } = "";
        [global::ProtoBuf.ProtoMember(37, Name = @"csharp_namespace")]
        [global::System.ComponentModel.DefaultValue("")]
        public string CsharpNamespace { get; set; } = "";
        [global::ProtoBuf.ProtoMember(39, Name = @"swift_prefix")]
        [global::System.ComponentModel.DefaultValue("")]
        public string SwiftPrefix { get; set; } = "";
        [global::ProtoBuf.ProtoMember(40, Name = @"php_class_prefix")]
        [global::System.ComponentModel.DefaultValue("")]
        public string PhpClassPrefix { get; set; } = "";
        [global::ProtoBuf.ProtoMember(999, Name = @"uninterpreted_option")]
        public global::System.Collections.Generic.List<UninterpretedOption> UninterpretedOptions { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"MessageOptions")]
    public partial class MessageOptions
    {
        [global::ProtoBuf.ProtoMember(1, Name = @"message_set_wire_format")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool MessageSetWireFormat { get; set; } = false;
        [global::ProtoBuf.ProtoMember(2, Name = @"no_standard_descriptor_accessor")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool NoStandardDescriptorAccessor { get; set; } = false;
        [global::ProtoBuf.ProtoMember(3, Name = @"deprecated")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool Deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(7, Name = @"map_entry")]
        public bool MapEntry { get; set; }
        [global::ProtoBuf.ProtoMember(999, Name = @"uninterpreted_option")]
        public global::System.Collections.Generic.List<UninterpretedOption> UninterpretedOptions { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"FieldOptions")]
    public partial class FieldOptions
    {
        [global::ProtoBuf.ProtoContract(Name = @"CType")]
        public enum CType
        {
            [global::ProtoBuf.ProtoEnum(Name = @"STRING", Value = 0)]
            String = 0,
            [global::ProtoBuf.ProtoEnum(Name = @"CORD", Value = 1)]
            Cord = 1,
            [global::ProtoBuf.ProtoEnum(Name = @"STRING_PIECE", Value = 2)]
            StringPiece = 2,
        }
        [global::ProtoBuf.ProtoContract(Name = @"JSType")]
        public enum JSType
        {
            [global::ProtoBuf.ProtoEnum(Name = @"JS_NORMAL", Value = 0)]
            JsNormal = 0,
            [global::ProtoBuf.ProtoEnum(Name = @"JS_STRING", Value = 1)]
            JsString = 1,
            [global::ProtoBuf.ProtoEnum(Name = @"JS_NUMBER", Value = 2)]
            JsNumber = 2,
        }
        [global::ProtoBuf.ProtoMember(1, Name = @"ctype")]
        [global::System.ComponentModel.DefaultValue(CType.String)]
        public CType Ctype { get; set; } = CType.String;
        [global::ProtoBuf.ProtoMember(2, Name = @"packed")]
        public bool Packed { get; set; }
        [global::ProtoBuf.ProtoMember(6, Name = @"jstype")]
        [global::System.ComponentModel.DefaultValue(JSType.JsNormal)]
        public JSType Jstype { get; set; } = JSType.JsNormal;
        [global::ProtoBuf.ProtoMember(5, Name = @"lazy")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool Lazy { get; set; } = false;
        [global::ProtoBuf.ProtoMember(3, Name = @"deprecated")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool Deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(10, Name = @"weak")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool Weak { get; set; } = false;
        [global::ProtoBuf.ProtoMember(999, Name = @"uninterpreted_option")]
        public global::System.Collections.Generic.List<UninterpretedOption> UninterpretedOptions { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"OneofOptions")]
    public partial class OneofOptions
    {
        [global::ProtoBuf.ProtoMember(999, Name = @"uninterpreted_option")]
        public global::System.Collections.Generic.List<UninterpretedOption> UninterpretedOptions { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"EnumOptions")]
    public partial class EnumOptions
    {
        [global::ProtoBuf.ProtoMember(2, Name = @"allow_alias")]
        public bool AllowAlias { get; set; }
        [global::ProtoBuf.ProtoMember(3, Name = @"deprecated")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool Deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(999, Name = @"uninterpreted_option")]
        public global::System.Collections.Generic.List<UninterpretedOption> UninterpretedOptions { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"EnumValueOptions")]
    public partial class EnumValueOptions
    {
        [global::ProtoBuf.ProtoMember(1, Name = @"deprecated")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool Deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(999, Name = @"uninterpreted_option")]
        public global::System.Collections.Generic.List<UninterpretedOption> UninterpretedOptions { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"ServiceOptions")]
    public partial class ServiceOptions
    {
        [global::ProtoBuf.ProtoMember(33, Name = @"deprecated")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool Deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(999, Name = @"uninterpreted_option")]
        public global::System.Collections.Generic.List<UninterpretedOption> UninterpretedOptions { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"MethodOptions")]
    public partial class MethodOptions
    {
        [global::ProtoBuf.ProtoContract(Name = @"IdempotencyLevel")]
        public enum IdempotencyLevel
        {
            [global::ProtoBuf.ProtoEnum(Name = @"IDEMPOTENCY_UNKNOWN", Value = 0)]
            IdempotencyUnknown = 0,
            [global::ProtoBuf.ProtoEnum(Name = @"NO_SIDE_EFFECTS", Value = 1)]
            NoSideEffects = 1,
            [global::ProtoBuf.ProtoEnum(Name = @"IDEMPOTENT", Value = 2)]
            Idempotent = 2,
        }
        [global::ProtoBuf.ProtoMember(33, Name = @"deprecated")]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool Deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(34, Name = @"idempotency_level")]
        [global::System.ComponentModel.DefaultValue(IdempotencyLevel.IdempotencyUnknown)]
        public IdempotencyLevel idempotency_level { get; set; } = IdempotencyLevel.IdempotencyUnknown;
        [global::ProtoBuf.ProtoMember(999, Name = @"uninterpreted_option")]
        public global::System.Collections.Generic.List<UninterpretedOption> UninterpretedOptions { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"UninterpretedOption")]
    public partial class UninterpretedOption
    {
        [global::ProtoBuf.ProtoContract(Name = @"NamePart")]
        public partial class NamePart
        {
            [global::ProtoBuf.ProtoMember(1, Name = @"name_part", IsRequired = true)]
            public string name_part { get; set; }
            [global::ProtoBuf.ProtoMember(2, Name = @"is_extension", IsRequired = true)]
            public bool IsExtension { get; set; }
        }
        [global::ProtoBuf.ProtoMember(2, Name = @"name")]
        public global::System.Collections.Generic.List<NamePart> Names { get; } = new global::System.Collections.Generic.List<NamePart>();
        [global::ProtoBuf.ProtoMember(3, Name = @"identifier_value")]
        [global::System.ComponentModel.DefaultValue("")]
        public string IdentifierValue { get; set; } = "";
        [global::ProtoBuf.ProtoMember(4, Name = @"positive_int_value")]
        public ulong PositiveIntValue { get; set; }
        [global::ProtoBuf.ProtoMember(5, Name = @"negative_int_value")]
        public long NegativeIntValue { get; set; }
        [global::ProtoBuf.ProtoMember(6, Name = @"double_value")]
        public double DoubleValue { get; set; }
        [global::ProtoBuf.ProtoMember(7, Name = @"string_value")]
        public byte[] StringValue { get; set; }
        [global::ProtoBuf.ProtoMember(8, Name = @"aggregate_value")]
        [global::System.ComponentModel.DefaultValue("")]
        public string AggregateValue { get; set; } = "";
    }
    [global::ProtoBuf.ProtoContract(Name = @"SourceCodeInfo")]
    public partial class SourceCodeInfo
    {
        [global::ProtoBuf.ProtoContract(Name = @"Location")]
        public partial class Location
        {
            [global::ProtoBuf.ProtoMember(1, Name = @"path", IsPacked = true)]
            public int[] Paths { get; set; }
            [global::ProtoBuf.ProtoMember(2, Name = @"span", IsPacked = true)]
            public int[] Spans { get; set; }
            [global::ProtoBuf.ProtoMember(3, Name = @"leading_comments")]
            [global::System.ComponentModel.DefaultValue("")]
            public string LeadingComments { get; set; } = "";
            [global::ProtoBuf.ProtoMember(4, Name = @"trailing_comments")]
            [global::System.ComponentModel.DefaultValue("")]
            public string TrailingComments { get; set; } = "";
            [global::ProtoBuf.ProtoMember(6, Name = @"leading_detached_comments")]
            public global::System.Collections.Generic.List<string> LeadingDetachedComments { get; } = new global::System.Collections.Generic.List<string>();
        }
        [global::ProtoBuf.ProtoMember(1, Name = @"location")]
        public global::System.Collections.Generic.List<Location> Locations { get; } = new global::System.Collections.Generic.List<Location>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"GeneratedCodeInfo")]
    public partial class GeneratedCodeInfo
    {
        [global::ProtoBuf.ProtoContract(Name = @"Annotation")]
        public partial class Annotation
        {
            [global::ProtoBuf.ProtoMember(1, Name = @"path", IsPacked = true)]
            public int[] Paths { get; set; }
            [global::ProtoBuf.ProtoMember(2, Name = @"source_file")]
            [global::System.ComponentModel.DefaultValue("")]
            public string SourceFile { get; set; } = "";
            [global::ProtoBuf.ProtoMember(3, Name = @"begin")]
            public int Begin { get; set; }
            [global::ProtoBuf.ProtoMember(4, Name = @"end")]
            public int End { get; set; }
        }
        [global::ProtoBuf.ProtoMember(1, Name = @"annotation")]
        public global::System.Collections.Generic.List<Annotation> Annotations { get; } = new global::System.Collections.Generic.List<Annotation>();
    }
}
#pragma warning restore CS1591
