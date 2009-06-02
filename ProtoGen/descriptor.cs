
// Generated from: descriptor.proto
namespace google.protobuf
{
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"FileDescriptorSet")]
    public partial class FileDescriptorSet : ProtoBuf.IExtensible
    {
      public FileDescriptorSet() {}
      
    private readonly System.Collections.Generic.List<google.protobuf.FileDescriptorProto> _file = new System.Collections.Generic.List<google.protobuf.FileDescriptorProto>();
    [ProtoBuf.ProtoMember(1, Name=@"file", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.FileDescriptorProto> file
    {
      get { return _file; }
    }
  
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"FileDescriptorProto")]
    public partial class FileDescriptorProto : ProtoBuf.IExtensible
    {
      public FileDescriptorProto() {}
      

    private string _name ="";
    [ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"name", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string name
    {
      get { return _name; }
      set { _name = value; }
    }

    private string _package ="";
    [ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"package", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string package
    {
      get { return _package; }
      set { _package = value; }
    }
    private readonly System.Collections.Generic.List<string> _dependency = new System.Collections.Generic.List<string>();
    [ProtoBuf.ProtoMember(3, Name=@"dependency", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<string> dependency
    {
      get { return _dependency; }
    }
  
    private readonly System.Collections.Generic.List<google.protobuf.DescriptorProto> _message_type = new System.Collections.Generic.List<google.protobuf.DescriptorProto>();
    [ProtoBuf.ProtoMember(4, Name=@"message_type", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.DescriptorProto> message_type
    {
      get { return _message_type; }
    }
  
    private readonly System.Collections.Generic.List<google.protobuf.EnumDescriptorProto> _enum_type = new System.Collections.Generic.List<google.protobuf.EnumDescriptorProto>();
    [ProtoBuf.ProtoMember(5, Name=@"enum_type", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.EnumDescriptorProto> enum_type
    {
      get { return _enum_type; }
    }
  
    private readonly System.Collections.Generic.List<google.protobuf.ServiceDescriptorProto> _service = new System.Collections.Generic.List<google.protobuf.ServiceDescriptorProto>();
    [ProtoBuf.ProtoMember(6, Name=@"service", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.ServiceDescriptorProto> service
    {
      get { return _service; }
    }
  
    private readonly System.Collections.Generic.List<google.protobuf.FieldDescriptorProto> _extension = new System.Collections.Generic.List<google.protobuf.FieldDescriptorProto>();
    [ProtoBuf.ProtoMember(7, Name=@"extension", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.FieldDescriptorProto> extension
    {
      get { return _extension; }
    }
  

    private google.protobuf.FileOptions _options =null;
    [ProtoBuf.ProtoMember(8, IsRequired = false, Name=@"options", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue(null)]
    public google.protobuf.FileOptions options
    {
      get { return _options; }
      set { _options = value; }
    }
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"DescriptorProto")]
    public partial class DescriptorProto : ProtoBuf.IExtensible
    {
      public DescriptorProto() {}
      

    private string _name ="";
    [ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"name", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string name
    {
      get { return _name; }
      set { _name = value; }
    }
    private readonly System.Collections.Generic.List<google.protobuf.FieldDescriptorProto> _field = new System.Collections.Generic.List<google.protobuf.FieldDescriptorProto>();
    [ProtoBuf.ProtoMember(2, Name=@"field", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.FieldDescriptorProto> field
    {
      get { return _field; }
    }
  
    private readonly System.Collections.Generic.List<google.protobuf.FieldDescriptorProto> _extension = new System.Collections.Generic.List<google.protobuf.FieldDescriptorProto>();
    [ProtoBuf.ProtoMember(6, Name=@"extension", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.FieldDescriptorProto> extension
    {
      get { return _extension; }
    }
  
    private readonly System.Collections.Generic.List<google.protobuf.DescriptorProto> _nested_type = new System.Collections.Generic.List<google.protobuf.DescriptorProto>();
    [ProtoBuf.ProtoMember(3, Name=@"nested_type", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.DescriptorProto> nested_type
    {
      get { return _nested_type; }
    }
  
    private readonly System.Collections.Generic.List<google.protobuf.EnumDescriptorProto> _enum_type = new System.Collections.Generic.List<google.protobuf.EnumDescriptorProto>();
    [ProtoBuf.ProtoMember(4, Name=@"enum_type", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.EnumDescriptorProto> enum_type
    {
      get { return _enum_type; }
    }
  
    private readonly System.Collections.Generic.List<google.protobuf.DescriptorProto.ExtensionRange> _extension_range = new System.Collections.Generic.List<google.protobuf.DescriptorProto.ExtensionRange>();
    [ProtoBuf.ProtoMember(5, Name=@"extension_range", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.DescriptorProto.ExtensionRange> extension_range
    {
      get { return _extension_range; }
    }
  

    private google.protobuf.MessageOptions _options =null;
    [ProtoBuf.ProtoMember(7, IsRequired = false, Name=@"options", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue(null)]
    public google.protobuf.MessageOptions options
    {
      get { return _options; }
      set { _options = value; }
    }
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"ExtensionRange")]
    public partial class ExtensionRange : ProtoBuf.IExtensible
    {
      public ExtensionRange() {}
      

    private int _start =default(int);
    [ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"start", DataFormat = ProtoBuf.DataFormat.TwosComplement)][System.ComponentModel.DefaultValue(default(int))]
    public int start
    {
      get { return _start; }
      set { _start = value; }
    }

    private int _end =default(int);
    [ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"end", DataFormat = ProtoBuf.DataFormat.TwosComplement)][System.ComponentModel.DefaultValue(default(int))]
    public int end
    {
      get { return _end; }
      set { _end = value; }
    }
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"FieldDescriptorProto")]
    public partial class FieldDescriptorProto : ProtoBuf.IExtensible
    {
      public FieldDescriptorProto() {}
      

    private string _name ="";
    [ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"name", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string name
    {
      get { return _name; }
      set { _name = value; }
    }

    private int _number =default(int);
    [ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"number", DataFormat = ProtoBuf.DataFormat.TwosComplement)][System.ComponentModel.DefaultValue(default(int))]
    public int number
    {
      get { return _number; }
      set { _number = value; }
    }

    private google.protobuf.FieldDescriptorProto.Label _label =google.protobuf.FieldDescriptorProto.Label.LABEL_OPTIONAL;
    [ProtoBuf.ProtoMember(4, IsRequired = false, Name=@"label", DataFormat = ProtoBuf.DataFormat.TwosComplement)][System.ComponentModel.DefaultValue(google.protobuf.FieldDescriptorProto.Label.LABEL_OPTIONAL)]
    public google.protobuf.FieldDescriptorProto.Label label
    {
      get { return _label; }
      set { _label = value; }
    }

    private google.protobuf.FieldDescriptorProto.Type _type =google.protobuf.FieldDescriptorProto.Type.TYPE_DOUBLE;
    [ProtoBuf.ProtoMember(5, IsRequired = false, Name=@"type", DataFormat = ProtoBuf.DataFormat.TwosComplement)][System.ComponentModel.DefaultValue(google.protobuf.FieldDescriptorProto.Type.TYPE_DOUBLE)]
    public google.protobuf.FieldDescriptorProto.Type type
    {
      get { return _type; }
      set { _type = value; }
    }

    private string _type_name ="";
    [ProtoBuf.ProtoMember(6, IsRequired = false, Name=@"type_name", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string type_name
    {
      get { return _type_name; }
      set { _type_name = value; }
    }

    private string _extendee ="";
    [ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"extendee", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string extendee
    {
      get { return _extendee; }
      set { _extendee = value; }
    }

    private string _default_value ="";
    [ProtoBuf.ProtoMember(7, IsRequired = false, Name=@"default_value", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string default_value
    {
      get { return _default_value; }
      set { _default_value = value; }
    }

    private google.protobuf.FieldOptions _options =null;
    [ProtoBuf.ProtoMember(8, IsRequired = false, Name=@"options", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue(null)]
    public google.protobuf.FieldOptions options
    {
      get { return _options; }
      set { _options = value; }
    }
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
    TYPE_SINT64 = 18
    }
  
    public enum Label
    {
      LABEL_OPTIONAL = 1,
    LABEL_REQUIRED = 2,
    LABEL_REPEATED = 3
    }
  
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"EnumDescriptorProto")]
    public partial class EnumDescriptorProto : ProtoBuf.IExtensible
    {
      public EnumDescriptorProto() {}
      

    private string _name ="";
    [ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"name", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string name
    {
      get { return _name; }
      set { _name = value; }
    }
    private readonly System.Collections.Generic.List<google.protobuf.EnumValueDescriptorProto> _value = new System.Collections.Generic.List<google.protobuf.EnumValueDescriptorProto>();
    [ProtoBuf.ProtoMember(2, Name=@"value", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.EnumValueDescriptorProto> value
    {
      get { return _value; }
    }
  

    private google.protobuf.EnumOptions _options =null;
    [ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"options", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue(null)]
    public google.protobuf.EnumOptions options
    {
      get { return _options; }
      set { _options = value; }
    }
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"EnumValueDescriptorProto")]
    public partial class EnumValueDescriptorProto : ProtoBuf.IExtensible
    {
      public EnumValueDescriptorProto() {}
      

    private string _name ="";
    [ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"name", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string name
    {
      get { return _name; }
      set { _name = value; }
    }

    private int _number =default(int);
    [ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"number", DataFormat = ProtoBuf.DataFormat.TwosComplement)][System.ComponentModel.DefaultValue(default(int))]
    public int number
    {
      get { return _number; }
      set { _number = value; }
    }

    private google.protobuf.EnumValueOptions _options =null;
    [ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"options", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue(null)]
    public google.protobuf.EnumValueOptions options
    {
      get { return _options; }
      set { _options = value; }
    }
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"ServiceDescriptorProto")]
    public partial class ServiceDescriptorProto : ProtoBuf.IExtensible
    {
      public ServiceDescriptorProto() {}
      

    private string _name ="";
    [ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"name", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string name
    {
      get { return _name; }
      set { _name = value; }
    }
    private readonly System.Collections.Generic.List<google.protobuf.MethodDescriptorProto> _method = new System.Collections.Generic.List<google.protobuf.MethodDescriptorProto>();
    [ProtoBuf.ProtoMember(2, Name=@"method", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.MethodDescriptorProto> method
    {
      get { return _method; }
    }
  

    private google.protobuf.ServiceOptions _options =null;
    [ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"options", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue(null)]
    public google.protobuf.ServiceOptions options
    {
      get { return _options; }
      set { _options = value; }
    }
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"MethodDescriptorProto")]
    public partial class MethodDescriptorProto : ProtoBuf.IExtensible
    {
      public MethodDescriptorProto() {}
      

    private string _name ="";
    [ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"name", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string name
    {
      get { return _name; }
      set { _name = value; }
    }

    private string _input_type ="";
    [ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"input_type", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string input_type
    {
      get { return _input_type; }
      set { _input_type = value; }
    }

    private string _output_type ="";
    [ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"output_type", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string output_type
    {
      get { return _output_type; }
      set { _output_type = value; }
    }

    private google.protobuf.MethodOptions _options =null;
    [ProtoBuf.ProtoMember(4, IsRequired = false, Name=@"options", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue(null)]
    public google.protobuf.MethodOptions options
    {
      get { return _options; }
      set { _options = value; }
    }
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"FileOptions")]
    public partial class FileOptions : ProtoBuf.IExtensible
    {
      public FileOptions() {}
      

    private string _java_package ="";
    [ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"java_package", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string java_package
    {
      get { return _java_package; }
      set { _java_package = value; }
    }

    private string _java_outer_classname ="";
    [ProtoBuf.ProtoMember(8, IsRequired = false, Name=@"java_outer_classname", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string java_outer_classname
    {
      get { return _java_outer_classname; }
      set { _java_outer_classname = value; }
    }

    private bool _java_multiple_files =(bool)false;
    [ProtoBuf.ProtoMember(10, IsRequired = false, Name=@"java_multiple_files", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue((bool)false)]
    public bool java_multiple_files
    {
      get { return _java_multiple_files; }
      set { _java_multiple_files = value; }
    }

    private google.protobuf.FileOptions.OptimizeMode _optimize_for =google.protobuf.FileOptions.OptimizeMode.SPEED;
    [ProtoBuf.ProtoMember(9, IsRequired = false, Name=@"optimize_for", DataFormat = ProtoBuf.DataFormat.TwosComplement)][System.ComponentModel.DefaultValue(google.protobuf.FileOptions.OptimizeMode.SPEED)]
    public google.protobuf.FileOptions.OptimizeMode optimize_for
    {
      get { return _optimize_for; }
      set { _optimize_for = value; }
    }
    private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _uninterpreted_option = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();
    [ProtoBuf.ProtoMember(999, Name=@"uninterpreted_option", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
    {
      get { return _uninterpreted_option; }
    }
  
    public enum OptimizeMode
    {
      SPEED = 1,
    CODE_SIZE = 2
    }
  
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"MessageOptions")]
    public partial class MessageOptions : ProtoBuf.IExtensible
    {
      public MessageOptions() {}
      

    private bool _message_set_wire_format =(bool)false;
    [ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"message_set_wire_format", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue((bool)false)]
    public bool message_set_wire_format
    {
      get { return _message_set_wire_format; }
      set { _message_set_wire_format = value; }
    }
    private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _uninterpreted_option = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();
    [ProtoBuf.ProtoMember(999, Name=@"uninterpreted_option", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
    {
      get { return _uninterpreted_option; }
    }
  
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"FieldOptions")]
    public partial class FieldOptions : ProtoBuf.IExtensible
    {
      public FieldOptions() {}
      

    private google.protobuf.FieldOptions.CType _ctype =google.protobuf.FieldOptions.CType.CORD;
    [ProtoBuf.ProtoMember(1, IsRequired = false, Name=@"ctype", DataFormat = ProtoBuf.DataFormat.TwosComplement)][System.ComponentModel.DefaultValue(google.protobuf.FieldOptions.CType.CORD)]
    public google.protobuf.FieldOptions.CType ctype
    {
      get { return _ctype; }
      set { _ctype = value; }
    }

    private bool _packed =default(bool);
    [ProtoBuf.ProtoMember(2, IsRequired = false, Name=@"packed", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue(default(bool))]
    public bool packed
    {
      get { return _packed; }
      set { _packed = value; }
    }

    private bool _deprecated =(bool)false;
    [ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"deprecated", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue((bool)false)]
    public bool deprecated
    {
      get { return _deprecated; }
      set { _deprecated = value; }
    }

    private string _experimental_map_key ="";
    [ProtoBuf.ProtoMember(9, IsRequired = false, Name=@"experimental_map_key", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string experimental_map_key
    {
      get { return _experimental_map_key; }
      set { _experimental_map_key = value; }
    }
    private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _uninterpreted_option = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();
    [ProtoBuf.ProtoMember(999, Name=@"uninterpreted_option", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
    {
      get { return _uninterpreted_option; }
    }
  
    public enum CType
    {
      CORD = 1,
    STRING_PIECE = 2
    }
  
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"EnumOptions")]
    public partial class EnumOptions : ProtoBuf.IExtensible
    {
      public EnumOptions() {}
      
    private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _uninterpreted_option = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();
    [ProtoBuf.ProtoMember(999, Name=@"uninterpreted_option", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
    {
      get { return _uninterpreted_option; }
    }
  
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"EnumValueOptions")]
    public partial class EnumValueOptions : ProtoBuf.IExtensible
    {
      public EnumValueOptions() {}
      
    private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _uninterpreted_option = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();
    [ProtoBuf.ProtoMember(999, Name=@"uninterpreted_option", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
    {
      get { return _uninterpreted_option; }
    }
  
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"ServiceOptions")]
    public partial class ServiceOptions : ProtoBuf.IExtensible
    {
      public ServiceOptions() {}
      
    private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _uninterpreted_option = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();
    [ProtoBuf.ProtoMember(999, Name=@"uninterpreted_option", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
    {
      get { return _uninterpreted_option; }
    }
  
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"MethodOptions")]
    public partial class MethodOptions : ProtoBuf.IExtensible
    {
      public MethodOptions() {}
      
    private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _uninterpreted_option = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();
    [ProtoBuf.ProtoMember(999, Name=@"uninterpreted_option", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
    {
      get { return _uninterpreted_option; }
    }
  
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"UninterpretedOption")]
    public partial class UninterpretedOption : ProtoBuf.IExtensible
    {
      public UninterpretedOption() {}
      
    private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption.NamePart> _name = new System.Collections.Generic.List<google.protobuf.UninterpretedOption.NamePart>();
    [ProtoBuf.ProtoMember(2, Name=@"name", DataFormat = ProtoBuf.DataFormat.Default)]
    public System.Collections.Generic.List<google.protobuf.UninterpretedOption.NamePart> name
    {
      get { return _name; }
    }
  

    private string _identifier_value ="";
    [ProtoBuf.ProtoMember(3, IsRequired = false, Name=@"identifier_value", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue("")]
    public string identifier_value
    {
      get { return _identifier_value; }
      set { _identifier_value = value; }
    }

    private ulong _positive_int_value =default(ulong);
    [ProtoBuf.ProtoMember(4, IsRequired = false, Name=@"positive_int_value", DataFormat = ProtoBuf.DataFormat.TwosComplement)][System.ComponentModel.DefaultValue(default(ulong))]
    public ulong positive_int_value
    {
      get { return _positive_int_value; }
      set { _positive_int_value = value; }
    }

    private long _negative_int_value =default(long);
    [ProtoBuf.ProtoMember(5, IsRequired = false, Name=@"negative_int_value", DataFormat = ProtoBuf.DataFormat.TwosComplement)][System.ComponentModel.DefaultValue(default(long))]
    public long negative_int_value
    {
      get { return _negative_int_value; }
      set { _negative_int_value = value; }
    }

    private double _double_value =default(double);
    [ProtoBuf.ProtoMember(6, IsRequired = false, Name=@"double_value", DataFormat = ProtoBuf.DataFormat.TwosComplement)][System.ComponentModel.DefaultValue(default(double))]
    public double double_value
    {
      get { return _double_value; }
      set { _double_value = value; }
    }

    private byte[] _string_value =null;
    [ProtoBuf.ProtoMember(7, IsRequired = false, Name=@"string_value", DataFormat = ProtoBuf.DataFormat.Default)][System.ComponentModel.DefaultValue(null)]
    public byte[] string_value
    {
      get { return _string_value; }
      set { _string_value = value; }
    }
    [System.Serializable, ProtoBuf.ProtoContract(Name=@"NamePart")]
    public partial class NamePart : ProtoBuf.IExtensible
    {
      public NamePart() {}
      
    private string _name_part;
    [ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"name_part", DataFormat = ProtoBuf.DataFormat.Default)]
    public string name_part
    {
      get { return _name_part; }
      set { _name_part = value; }
    }
    private bool _is_extension;
    [ProtoBuf.ProtoMember(2, IsRequired = true, Name=@"is_extension", DataFormat = ProtoBuf.DataFormat.Default)]
    public bool is_extension
    {
      get { return _is_extension; }
      set { _is_extension = value; }
    }
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
      private ProtoBuf.IExtension extensionObject;
      ProtoBuf.IExtension ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
        { return ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
    }
  
}