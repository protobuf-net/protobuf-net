namespace Google.Protobuf.Reflection
{
    [global::ProtoBuf.ProtoContract(Name = @"FileDescriptorSet")]
    public partial class FileDescriptorSet
    {
        [global::ProtoBuf.ProtoMember(1)]
        public global::System.Collections.Generic.List<FileDescriptorProto> file { get; } = new global::System.Collections.Generic.List<FileDescriptorProto>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"FileDescriptorProto")]
    public partial class FileDescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue("")]
        public string name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2)]
        [global::System.ComponentModel.DefaultValue("")]
        public string package { get; set; } = "";
        [global::ProtoBuf.ProtoMember(3)]
        public string[] dependency { get; set; }
        [global::ProtoBuf.ProtoMember(10)]
        public int[] public_dependency { get; set; }
        [global::ProtoBuf.ProtoMember(11)]
        public int[] weak_dependency { get; set; }
        [global::ProtoBuf.ProtoMember(4)]
        public global::System.Collections.Generic.List<DescriptorProto> message_type { get; } = new global::System.Collections.Generic.List<DescriptorProto>();
        [global::ProtoBuf.ProtoMember(5)]
        public global::System.Collections.Generic.List<EnumDescriptorProto> enum_type { get; } = new global::System.Collections.Generic.List<EnumDescriptorProto>();
        [global::ProtoBuf.ProtoMember(6)]
        public global::System.Collections.Generic.List<ServiceDescriptorProto> service { get; } = new global::System.Collections.Generic.List<ServiceDescriptorProto>();
        [global::ProtoBuf.ProtoMember(7)]
        public global::System.Collections.Generic.List<FieldDescriptorProto> extension { get; } = new global::System.Collections.Generic.List<FieldDescriptorProto>();
        [global::ProtoBuf.ProtoMember(8)]
        public FileOptions options { get; set; }
        [global::ProtoBuf.ProtoMember(9)]
        public SourceCodeInfo source_code_info { get; set; }
        [global::ProtoBuf.ProtoMember(12)]
        [global::System.ComponentModel.DefaultValue("")]
        public string syntax { get; set; } = "";
    }
    [global::ProtoBuf.ProtoContract(Name = @"DescriptorProto")]
    public partial class DescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue("")]
        public string name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2)]
        public global::System.Collections.Generic.List<FieldDescriptorProto> field { get; } = new global::System.Collections.Generic.List<FieldDescriptorProto>();
        [global::ProtoBuf.ProtoMember(6)]
        public global::System.Collections.Generic.List<FieldDescriptorProto> extension { get; } = new global::System.Collections.Generic.List<FieldDescriptorProto>();
        [global::ProtoBuf.ProtoMember(3)]
        public global::System.Collections.Generic.List<DescriptorProto> nested_type { get; } = new global::System.Collections.Generic.List<DescriptorProto>();
        [global::ProtoBuf.ProtoMember(4)]
        public global::System.Collections.Generic.List<EnumDescriptorProto> enum_type { get; } = new global::System.Collections.Generic.List<EnumDescriptorProto>();
        [global::ProtoBuf.ProtoContract(Name = @"ExtensionRange")]
        public partial class ExtensionRange
        {
            [global::ProtoBuf.ProtoMember(1)]
            public int start { get; set; }
            [global::ProtoBuf.ProtoMember(2)]
            public int end { get; set; }
        }
        [global::ProtoBuf.ProtoMember(5)]
        public global::System.Collections.Generic.List<ExtensionRange> extension_range { get; } = new global::System.Collections.Generic.List<ExtensionRange>();
        [global::ProtoBuf.ProtoMember(8)]
        public global::System.Collections.Generic.List<OneofDescriptorProto> oneof_decl { get; } = new global::System.Collections.Generic.List<OneofDescriptorProto>();
        [global::ProtoBuf.ProtoMember(7)]
        public MessageOptions options { get; set; }
        [global::ProtoBuf.ProtoContract(Name = @"ReservedRange")]
        public partial class ReservedRange
        {
            [global::ProtoBuf.ProtoMember(1)]
            public int start { get; set; }
            [global::ProtoBuf.ProtoMember(2)]
            public int end { get; set; }
        }
        [global::ProtoBuf.ProtoMember(9)]
        public global::System.Collections.Generic.List<ReservedRange> reserved_range { get; } = new global::System.Collections.Generic.List<ReservedRange>();
        [global::ProtoBuf.ProtoMember(10)]
        public string[] reserved_name { get; set; }
    }
    [global::ProtoBuf.ProtoContract(Name = @"FieldDescriptorProto")]
    public partial class FieldDescriptorProto
    {
        public enum Type
        {
            TYPE_DOUBLE = 1,
            TYPE_FLOAT = 2,
            TYPE_INT64 = 3,
            TYPE_UINT64 = 4,
            TYPE_INT32 = 5,
            TYPE_FIXED64 = 6,
            TYPE_FIXED32 = 7,
            TYPE_BOOL = 8,
            TYPE_STRING = 9,
            TYPE_GROUP = 10,
            TYPE_MESSAGE = 11,
            TYPE_BYTES = 12,
            TYPE_UINT32 = 13,
            TYPE_ENUM = 14,
            TYPE_SFIXED32 = 15,
            TYPE_SFIXED64 = 16,
            TYPE_SINT32 = 17,
            TYPE_SINT64 = 18,
        }
        public enum Label
        {
            LABEL_OPTIONAL = 1,
            LABEL_REQUIRED = 2,
            LABEL_REPEATED = 3,
        }
        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue("")]
        public string name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(3)]
        public int number { get; set; }
        [global::ProtoBuf.ProtoMember(4)]
        public Label label { get; set; }
        [global::ProtoBuf.ProtoMember(5)]
        public Type type { get; set; }
        [global::ProtoBuf.ProtoMember(6)]
        [global::System.ComponentModel.DefaultValue("")]
        public string type_name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2)]
        [global::System.ComponentModel.DefaultValue("")]
        public string extendee { get; set; } = "";
        [global::ProtoBuf.ProtoMember(7)]
        [global::System.ComponentModel.DefaultValue("")]
        public string default_value { get; set; } = "";
        [global::ProtoBuf.ProtoMember(9)]
        public int oneof_index { get; set; }
        [global::ProtoBuf.ProtoMember(10)]
        [global::System.ComponentModel.DefaultValue("")]
        public string json_name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(8)]
        public FieldOptions options { get; set; }
    }
    [global::ProtoBuf.ProtoContract(Name = @"OneofDescriptorProto")]
    public partial class OneofDescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue("")]
        public string name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2)]
        public OneofOptions options { get; set; }
    }
    [global::ProtoBuf.ProtoContract(Name = @"EnumDescriptorProto")]
    public partial class EnumDescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue("")]
        public string name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2)]
        public global::System.Collections.Generic.List<EnumValueDescriptorProto> value { get; } = new global::System.Collections.Generic.List<EnumValueDescriptorProto>();
        [global::ProtoBuf.ProtoMember(3)]
        public EnumOptions options { get; set; }
    }
    [global::ProtoBuf.ProtoContract(Name = @"EnumValueDescriptorProto")]
    public partial class EnumValueDescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue("")]
        public string name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2)]
        public int number { get; set; }
        [global::ProtoBuf.ProtoMember(3)]
        public EnumValueOptions options { get; set; }
    }
    [global::ProtoBuf.ProtoContract(Name = @"ServiceDescriptorProto")]
    public partial class ServiceDescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue("")]
        public string name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2)]
        public global::System.Collections.Generic.List<MethodDescriptorProto> method { get; } = new global::System.Collections.Generic.List<MethodDescriptorProto>();
        [global::ProtoBuf.ProtoMember(3)]
        public ServiceOptions options { get; set; }
    }
    [global::ProtoBuf.ProtoContract(Name = @"MethodDescriptorProto")]
    public partial class MethodDescriptorProto
    {
        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue("")]
        public string name { get; set; } = "";
        [global::ProtoBuf.ProtoMember(2)]
        [global::System.ComponentModel.DefaultValue("")]
        public string input_type { get; set; } = "";
        [global::ProtoBuf.ProtoMember(3)]
        [global::System.ComponentModel.DefaultValue("")]
        public string output_type { get; set; } = "";
        [global::ProtoBuf.ProtoMember(4)]
        public MethodOptions options { get; set; }
        [global::ProtoBuf.ProtoMember(5)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool client_streaming { get; set; } = false;
        [global::ProtoBuf.ProtoMember(6)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool server_streaming { get; set; } = false;
    }
    [global::ProtoBuf.ProtoContract(Name = @"FileOptions")]
    public partial class FileOptions
    {
        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue("")]
        public string java_package { get; set; } = "";
        [global::ProtoBuf.ProtoMember(8)]
        [global::System.ComponentModel.DefaultValue("")]
        public string java_outer_classname { get; set; } = "";
        [global::ProtoBuf.ProtoMember(10)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool java_multiple_files { get; set; } = false;
        [global::ProtoBuf.ProtoMember(20)]
        [global::System.Obsolete]
        public bool java_generate_equals_and_hash { get; set; }
        [global::ProtoBuf.ProtoMember(27)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool java_string_check_utf8 { get; set; } = false;
        public enum OptimizeMode
        {
            SPEED = 1,
            CODE_SIZE = 2,
            LITE_RUNTIME = 3,
        }
        [global::ProtoBuf.ProtoMember(9)]
        [global::System.ComponentModel.DefaultValue(OptimizeMode.SPEED)]
        public OptimizeMode optimize_for { get; set; } = OptimizeMode.SPEED;
        [global::ProtoBuf.ProtoMember(11)]
        [global::System.ComponentModel.DefaultValue("")]
        public string go_package { get; set; } = "";
        [global::ProtoBuf.ProtoMember(16)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool cc_generic_services { get; set; } = false;
        [global::ProtoBuf.ProtoMember(17)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool java_generic_services { get; set; } = false;
        [global::ProtoBuf.ProtoMember(18)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool py_generic_services { get; set; } = false;
        [global::ProtoBuf.ProtoMember(23)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(31)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool cc_enable_arenas { get; set; } = false;
        [global::ProtoBuf.ProtoMember(36)]
        [global::System.ComponentModel.DefaultValue("")]
        public string objc_class_prefix { get; set; } = "";
        [global::ProtoBuf.ProtoMember(37)]
        [global::System.ComponentModel.DefaultValue("")]
        public string csharp_namespace { get; set; } = "";
        [global::ProtoBuf.ProtoMember(39)]
        [global::System.ComponentModel.DefaultValue("")]
        public string swift_prefix { get; set; } = "";
        [global::ProtoBuf.ProtoMember(40)]
        [global::System.ComponentModel.DefaultValue("")]
        public string php_class_prefix { get; set; } = "";
        [global::ProtoBuf.ProtoMember(999)]
        public global::System.Collections.Generic.List<UninterpretedOption> uninterpreted_option { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"MessageOptions")]
    public partial class MessageOptions
    {
        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool message_set_wire_format { get; set; } = false;
        [global::ProtoBuf.ProtoMember(2)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool no_standard_descriptor_accessor { get; set; } = false;
        [global::ProtoBuf.ProtoMember(3)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(7)]
        public bool map_entry { get; set; }
        [global::ProtoBuf.ProtoMember(999)]
        public global::System.Collections.Generic.List<UninterpretedOption> uninterpreted_option { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"FieldOptions")]
    public partial class FieldOptions
    {
        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue(CType.STRING)]
        public CType ctype { get; set; } = CType.STRING;
        public enum CType
        {
            STRING = 0,
            CORD = 1,
            STRING_PIECE = 2,
        }
        [global::ProtoBuf.ProtoMember(2)]
        public bool packed { get; set; }
        [global::ProtoBuf.ProtoMember(6)]
        [global::System.ComponentModel.DefaultValue(JSType.JS_NORMAL)]
        public JSType jstype { get; set; } = JSType.JS_NORMAL;
        public enum JSType
        {
            JS_NORMAL = 0,
            JS_STRING = 1,
            JS_NUMBER = 2,
        }
        [global::ProtoBuf.ProtoMember(5)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool lazy { get; set; } = false;
        [global::ProtoBuf.ProtoMember(3)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(10)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool weak { get; set; } = false;
        [global::ProtoBuf.ProtoMember(999)]
        public global::System.Collections.Generic.List<UninterpretedOption> uninterpreted_option { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"OneofOptions")]
    public partial class OneofOptions
    {
        [global::ProtoBuf.ProtoMember(999)]
        public global::System.Collections.Generic.List<UninterpretedOption> uninterpreted_option { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"EnumOptions")]
    public partial class EnumOptions
    {
        [global::ProtoBuf.ProtoMember(2)]
        public bool allow_alias { get; set; }
        [global::ProtoBuf.ProtoMember(3)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(999)]
        public global::System.Collections.Generic.List<UninterpretedOption> uninterpreted_option { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"EnumValueOptions")]
    public partial class EnumValueOptions
    {
        [global::ProtoBuf.ProtoMember(1)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(999)]
        public global::System.Collections.Generic.List<UninterpretedOption> uninterpreted_option { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"ServiceOptions")]
    public partial class ServiceOptions
    {
        [global::ProtoBuf.ProtoMember(33)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool deprecated { get; set; } = false;
        [global::ProtoBuf.ProtoMember(999)]
        public global::System.Collections.Generic.List<UninterpretedOption> uninterpreted_option { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"MethodOptions")]
    public partial class MethodOptions
    {
        [global::ProtoBuf.ProtoMember(33)]
        [global::System.ComponentModel.DefaultValue(false)]
        public bool deprecated { get; set; } = false;
        public enum IdempotencyLevel
        {
            IDEMPOTENCY_UNKNOWN = 0,
            NO_SIDE_EFFECTS = 1,
            IDEMPOTENT = 2,
        }
        [global::ProtoBuf.ProtoMember(34)]
        [global::System.ComponentModel.DefaultValue(IdempotencyLevel.IDEMPOTENCY_UNKNOWN)]
        public IdempotencyLevel idempotency_level { get; set; } = IdempotencyLevel.IDEMPOTENCY_UNKNOWN;
        [global::ProtoBuf.ProtoMember(999)]
        public global::System.Collections.Generic.List<UninterpretedOption> uninterpreted_option { get; } = new global::System.Collections.Generic.List<UninterpretedOption>();
    }
    [global::ProtoBuf.ProtoContract(Name = @"UninterpretedOption")]
    public partial class UninterpretedOption
    {
        [global::ProtoBuf.ProtoContract(Name = @"NamePart")]
        public partial class NamePart
        {
            [global::ProtoBuf.ProtoMember(1, IsRequired = true)]
            public string name_part { get; set; }
            [global::ProtoBuf.ProtoMember(2, IsRequired = true)]
            public bool is_extension { get; set; }
        }
        [global::ProtoBuf.ProtoMember(2)]
        public global::System.Collections.Generic.List<NamePart> name { get; } = new global::System.Collections.Generic.List<NamePart>();
        [global::ProtoBuf.ProtoMember(3)]
        [global::System.ComponentModel.DefaultValue("")]
        public string identifier_value { get; set; } = "";
        [global::ProtoBuf.ProtoMember(4)]
        public ulong positive_int_value { get; set; }
        [global::ProtoBuf.ProtoMember(5)]
        public long negative_int_value { get; set; }
        [global::ProtoBuf.ProtoMember(6)]
        public double double_value { get; set; }
        [global::ProtoBuf.ProtoMember(7)]
        public byte[] string_value { get; set; }
        [global::ProtoBuf.ProtoMember(8)]
        [global::System.ComponentModel.DefaultValue("")]
        public string aggregate_value { get; set; } = "";
    }
    [global::ProtoBuf.ProtoContract(Name = @"SourceCodeInfo")]
    public partial class SourceCodeInfo
    {
        [global::ProtoBuf.ProtoMember(1)]
        public global::System.Collections.Generic.List<Location> location { get; } = new global::System.Collections.Generic.List<Location>();
        [global::ProtoBuf.ProtoContract(Name = @"Location")]
        public partial class Location
        {
            [global::ProtoBuf.ProtoMember(1, IsPacked = true)]
            public int[] path { get; set; }
            [global::ProtoBuf.ProtoMember(2, IsPacked = true)]
            public int[] span { get; set; }
            [global::ProtoBuf.ProtoMember(3)]
            [global::System.ComponentModel.DefaultValue("")]
            public string leading_comments { get; set; } = "";
            [global::ProtoBuf.ProtoMember(4)]
            [global::System.ComponentModel.DefaultValue("")]
            public string trailing_comments { get; set; } = "";
            [global::ProtoBuf.ProtoMember(6)]
            public string[] leading_detached_comments { get; set; }
        }
    }
    [global::ProtoBuf.ProtoContract(Name = @"GeneratedCodeInfo")]
    public partial class GeneratedCodeInfo
    {
        [global::ProtoBuf.ProtoMember(1)]
        public global::System.Collections.Generic.List<Annotation> annotation { get; } = new global::System.Collections.Generic.List<Annotation>();
        [global::ProtoBuf.ProtoContract(Name = @"Annotation")]
        public partial class Annotation
        {
            [global::ProtoBuf.ProtoMember(1, IsPacked = true)]
            public int[] path { get; set; }
            [global::ProtoBuf.ProtoMember(2)]
            [global::System.ComponentModel.DefaultValue("")]
            public string source_file { get; set; } = "";
            [global::ProtoBuf.ProtoMember(3)]
            public int begin { get; set; }
            [global::ProtoBuf.ProtoMember(4)]
            public int end { get; set; }
        }
    }
}
