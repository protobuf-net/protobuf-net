
// see descriptor.proto
namespace google.protobuf
{

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class FileDescriptorSet
    {

        private readonly System.Collections.Generic.List<google.protobuf.FileDescriptorProto> _ID0EU = new System.Collections.Generic.List<google.protobuf.FileDescriptorProto>();

        [ProtoBuf.ProtoMember(1)]
        public System.Collections.Generic.List<google.protobuf.FileDescriptorProto> file
        {
            get { return _ID0EU; }
            set
            { // setter needed for XmlSerializer
                _ID0EU.Clear();
                if (value != null)
                {
                    _ID0EU.AddRange(value);
                }
            }
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class FileDescriptorProto
    {

        private string _ID0EJB = "";

        [ProtoBuf.ProtoMember(1)]
        [System.ComponentModel.DefaultValue("")]
        public string name
        {
            get { return _ID0EJB; }
            set { _ID0EJB = value; }
        }

        private string _ID0ESB = "";

        [ProtoBuf.ProtoMember(2)]
        [System.ComponentModel.DefaultValue("")]
        public string package
        {
            get { return _ID0ESB; }
            set { _ID0ESB = value; }
        }

        private readonly System.Collections.Generic.List<string> _ID0E2B = new System.Collections.Generic.List<string>();

        [ProtoBuf.ProtoMember(3)]
        public System.Collections.Generic.List<string> dependency
        {
            get { return _ID0E2B; }
            set
            { // setter needed for XmlSerializer
                _ID0E2B.Clear();
                if (value != null)
                {
                    _ID0E2B.AddRange(value);
                }
            }
        }

        private readonly System.Collections.Generic.List<google.protobuf.DescriptorProto> _ID0EGC = new System.Collections.Generic.List<google.protobuf.DescriptorProto>();

        [ProtoBuf.ProtoMember(4)]
        public System.Collections.Generic.List<google.protobuf.DescriptorProto> message_type
        {
            get { return _ID0EGC; }
            set
            { // setter needed for XmlSerializer
                _ID0EGC.Clear();
                if (value != null)
                {
                    _ID0EGC.AddRange(value);
                }
            }
        }

        private readonly System.Collections.Generic.List<google.protobuf.EnumDescriptorProto> _ID0ETC = new System.Collections.Generic.List<google.protobuf.EnumDescriptorProto>();

        [ProtoBuf.ProtoMember(5)]
        public System.Collections.Generic.List<google.protobuf.EnumDescriptorProto> enum_type
        {
            get { return _ID0ETC; }
            set
            { // setter needed for XmlSerializer
                _ID0ETC.Clear();
                if (value != null)
                {
                    _ID0ETC.AddRange(value);
                }
            }
        }

        private readonly System.Collections.Generic.List<google.protobuf.ServiceDescriptorProto> _ID0EAD = new System.Collections.Generic.List<google.protobuf.ServiceDescriptorProto>();

        [ProtoBuf.ProtoMember(6)]
        public System.Collections.Generic.List<google.protobuf.ServiceDescriptorProto> service
        {
            get { return _ID0EAD; }
            set
            { // setter needed for XmlSerializer
                _ID0EAD.Clear();
                if (value != null)
                {
                    _ID0EAD.AddRange(value);
                }
            }
        }

        private readonly System.Collections.Generic.List<google.protobuf.FieldDescriptorProto> _ID0END = new System.Collections.Generic.List<google.protobuf.FieldDescriptorProto>();

        [ProtoBuf.ProtoMember(7)]
        public System.Collections.Generic.List<google.protobuf.FieldDescriptorProto> extension
        {
            get { return _ID0END; }
            set
            { // setter needed for XmlSerializer
                _ID0END.Clear();
                if (value != null)
                {
                    _ID0END.AddRange(value);
                }
            }
        }

        private google.protobuf.FileOptions _ID0E1D = null;

        [ProtoBuf.ProtoMember(8)]
        [System.ComponentModel.DefaultValue(null)]
        public google.protobuf.FileOptions options
        {
            get { return _ID0E1D; }
            set { _ID0E1D = value; }
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class DescriptorProto
    {

        private string _ID0ENE = "";

        [ProtoBuf.ProtoMember(1)]
        [System.ComponentModel.DefaultValue("")]
        public string name
        {
            get { return _ID0ENE; }
            set { _ID0ENE = value; }
        }

        private readonly System.Collections.Generic.List<google.protobuf.FieldDescriptorProto> _ID0EWE = new System.Collections.Generic.List<google.protobuf.FieldDescriptorProto>();

        [ProtoBuf.ProtoMember(2)]
        public System.Collections.Generic.List<google.protobuf.FieldDescriptorProto> field
        {
            get { return _ID0EWE; }
            set
            { // setter needed for XmlSerializer
                _ID0EWE.Clear();
                if (value != null)
                {
                    _ID0EWE.AddRange(value);
                }
            }
        }

        private readonly System.Collections.Generic.List<google.protobuf.FieldDescriptorProto> _ID0EDF = new System.Collections.Generic.List<google.protobuf.FieldDescriptorProto>();

        [ProtoBuf.ProtoMember(6)]
        public System.Collections.Generic.List<google.protobuf.FieldDescriptorProto> extension
        {
            get { return _ID0EDF; }
            set
            { // setter needed for XmlSerializer
                _ID0EDF.Clear();
                if (value != null)
                {
                    _ID0EDF.AddRange(value);
                }
            }
        }

        private readonly System.Collections.Generic.List<google.protobuf.DescriptorProto> _ID0EQF = new System.Collections.Generic.List<google.protobuf.DescriptorProto>();

        [ProtoBuf.ProtoMember(3)]
        public System.Collections.Generic.List<google.protobuf.DescriptorProto> nested_type
        {
            get { return _ID0EQF; }
            set
            { // setter needed for XmlSerializer
                _ID0EQF.Clear();
                if (value != null)
                {
                    _ID0EQF.AddRange(value);
                }
            }
        }

        private readonly System.Collections.Generic.List<google.protobuf.EnumDescriptorProto> _ID0E4F = new System.Collections.Generic.List<google.protobuf.EnumDescriptorProto>();

        [ProtoBuf.ProtoMember(4)]
        public System.Collections.Generic.List<google.protobuf.EnumDescriptorProto> enum_type
        {
            get { return _ID0E4F; }
            set
            { // setter needed for XmlSerializer
                _ID0E4F.Clear();
                if (value != null)
                {
                    _ID0E4F.AddRange(value);
                }
            }
        }

        private readonly System.Collections.Generic.List<google.protobuf.DescriptorProto.ExtensionRange> _ID0EKG = new System.Collections.Generic.List<google.protobuf.DescriptorProto.ExtensionRange>();

        [ProtoBuf.ProtoMember(5)]
        public System.Collections.Generic.List<google.protobuf.DescriptorProto.ExtensionRange> extension_range
        {
            get { return _ID0EKG; }
            set
            { // setter needed for XmlSerializer
                _ID0EKG.Clear();
                if (value != null)
                {
                    _ID0EKG.AddRange(value);
                }
            }
        }

        private google.protobuf.MessageOptions _ID0EXG = null;

        [ProtoBuf.ProtoMember(7)]
        [System.ComponentModel.DefaultValue(null)]
        public google.protobuf.MessageOptions options
        {
            get { return _ID0EXG; }
            set { _ID0EXG = value; }
        }

        [System.Serializable, ProtoBuf.ProtoContract]
        public partial class ExtensionRange
        {

            private int _ID0ELH = default(int);

            [ProtoBuf.ProtoMember(1)]
            [System.ComponentModel.DefaultValue(default(int))]
            public int start
            {
                get { return _ID0ELH; }
                set { _ID0ELH = value; }
            }

            private int _ID0ESH = default(int);

            [ProtoBuf.ProtoMember(2)]
            [System.ComponentModel.DefaultValue(default(int))]
            public int end
            {
                get { return _ID0ESH; }
                set { _ID0ESH = value; }
            }

        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class FieldDescriptorProto
    {

        private string _ID0EEAAC = "";

        [ProtoBuf.ProtoMember(1)]
        [System.ComponentModel.DefaultValue("")]
        public string name
        {
            get { return _ID0EEAAC; }
            set { _ID0EEAAC = value; }
        }

        private int _ID0ENAAC = default(int);

        [ProtoBuf.ProtoMember(3)]
        [System.ComponentModel.DefaultValue(default(int))]
        public int number
        {
            get { return _ID0ENAAC; }
            set { _ID0ENAAC = value; }
        }

        private google.protobuf.FieldDescriptorProto.Label _ID0EUAAC = google.protobuf.FieldDescriptorProto.Label.LABEL_OPTIONAL;

        [ProtoBuf.ProtoMember(4)]
        [System.ComponentModel.DefaultValue(google.protobuf.FieldDescriptorProto.Label.LABEL_OPTIONAL)]
        public google.protobuf.FieldDescriptorProto.Label label
        {
            get { return _ID0EUAAC; }
            set { _ID0EUAAC = value; }
        }

        private google.protobuf.FieldDescriptorProto.Type _ID0E6AAC = google.protobuf.FieldDescriptorProto.Type.TYPE_DOUBLE;

        [ProtoBuf.ProtoMember(5)]
        [System.ComponentModel.DefaultValue(google.protobuf.FieldDescriptorProto.Type.TYPE_DOUBLE)]
        public google.protobuf.FieldDescriptorProto.Type type
        {
            get { return _ID0E6AAC; }
            set { _ID0E6AAC = value; }
        }

        private string _ID0EKBAC = "";

        [ProtoBuf.ProtoMember(6)]
        [System.ComponentModel.DefaultValue("")]
        public string type_name
        {
            get { return _ID0EKBAC; }
            set { _ID0EKBAC = value; }
        }

        private string _ID0ETBAC = "";

        [ProtoBuf.ProtoMember(2)]
        [System.ComponentModel.DefaultValue("")]
        public string extendee
        {
            get { return _ID0ETBAC; }
            set { _ID0ETBAC = value; }
        }

        private string _ID0E3BAC = "";

        [ProtoBuf.ProtoMember(7)]
        [System.ComponentModel.DefaultValue("")]
        public string default_value
        {
            get { return _ID0E3BAC; }
            set { _ID0E3BAC = value; }
        }

        private google.protobuf.FieldOptions _ID0EFCAC = null;

        [ProtoBuf.ProtoMember(8)]
        [System.ComponentModel.DefaultValue(null)]
        public google.protobuf.FieldOptions options
        {
            get { return _ID0EFCAC; }
            set { _ID0EFCAC = value; }
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

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class EnumDescriptorProto
    {

        private string _ID0E5HAC = "";

        [ProtoBuf.ProtoMember(1)]
        [System.ComponentModel.DefaultValue("")]
        public string name
        {
            get { return _ID0E5HAC; }
            set { _ID0E5HAC = value; }
        }

        private readonly System.Collections.Generic.List<google.protobuf.EnumValueDescriptorProto> _ID0EHIAC = new System.Collections.Generic.List<google.protobuf.EnumValueDescriptorProto>();

        [ProtoBuf.ProtoMember(2)]
        public System.Collections.Generic.List<google.protobuf.EnumValueDescriptorProto> value
        {
            get { return _ID0EHIAC; }
            set
            { // setter needed for XmlSerializer
                _ID0EHIAC.Clear();
                if (value != null)
                {
                    _ID0EHIAC.AddRange(value);
                }
            }
        }

        private google.protobuf.EnumOptions _ID0EUIAC = null;

        [ProtoBuf.ProtoMember(3)]
        [System.ComponentModel.DefaultValue(null)]
        public google.protobuf.EnumOptions options
        {
            get { return _ID0EUIAC; }
            set { _ID0EUIAC = value; }
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class EnumValueDescriptorProto
    {

        private string _ID0EHJAC = "";

        [ProtoBuf.ProtoMember(1)]
        [System.ComponentModel.DefaultValue("")]
        public string name
        {
            get { return _ID0EHJAC; }
            set { _ID0EHJAC = value; }
        }

        private int _ID0EQJAC = default(int);

        [ProtoBuf.ProtoMember(2)]
        [System.ComponentModel.DefaultValue(default(int))]
        public int number
        {
            get { return _ID0EQJAC; }
            set { _ID0EQJAC = value; }
        }

        private google.protobuf.EnumValueOptions _ID0EXJAC = null;

        [ProtoBuf.ProtoMember(3)]
        [System.ComponentModel.DefaultValue(null)]
        public google.protobuf.EnumValueOptions options
        {
            get { return _ID0EXJAC; }
            set { _ID0EXJAC = value; }
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class ServiceDescriptorProto
    {

        private string _ID0EKKAC = "";

        [ProtoBuf.ProtoMember(1)]
        [System.ComponentModel.DefaultValue("")]
        public string name
        {
            get { return _ID0EKKAC; }
            set { _ID0EKKAC = value; }
        }

        private readonly System.Collections.Generic.List<google.protobuf.MethodDescriptorProto> _ID0ETKAC = new System.Collections.Generic.List<google.protobuf.MethodDescriptorProto>();

        [ProtoBuf.ProtoMember(2)]
        public System.Collections.Generic.List<google.protobuf.MethodDescriptorProto> method
        {
            get { return _ID0ETKAC; }
            set
            { // setter needed for XmlSerializer
                _ID0ETKAC.Clear();
                if (value != null)
                {
                    _ID0ETKAC.AddRange(value);
                }
            }
        }

        private google.protobuf.ServiceOptions _ID0EALAC = null;

        [ProtoBuf.ProtoMember(3)]
        [System.ComponentModel.DefaultValue(null)]
        public google.protobuf.ServiceOptions options
        {
            get { return _ID0EALAC; }
            set { _ID0EALAC = value; }
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class MethodDescriptorProto
    {

        private string _ID0ETLAC = "";

        [ProtoBuf.ProtoMember(1)]
        [System.ComponentModel.DefaultValue("")]
        public string name
        {
            get { return _ID0ETLAC; }
            set { _ID0ETLAC = value; }
        }

        private string _ID0E3LAC = "";

        [ProtoBuf.ProtoMember(2)]
        [System.ComponentModel.DefaultValue("")]
        public string input_type
        {
            get { return _ID0E3LAC; }
            set { _ID0E3LAC = value; }
        }

        private string _ID0EFMAC = "";

        [ProtoBuf.ProtoMember(3)]
        [System.ComponentModel.DefaultValue("")]
        public string output_type
        {
            get { return _ID0EFMAC; }
            set { _ID0EFMAC = value; }
        }

        private google.protobuf.MethodOptions _ID0EOMAC = null;

        [ProtoBuf.ProtoMember(4)]
        [System.ComponentModel.DefaultValue(null)]
        public google.protobuf.MethodOptions options
        {
            get { return _ID0EOMAC; }
            set { _ID0EOMAC = value; }
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class FileOptions
    {

        private string _ID0EBNAC = "";

        [ProtoBuf.ProtoMember(1)]
        [System.ComponentModel.DefaultValue("")]
        public string java_package
        {
            get { return _ID0EBNAC; }
            set { _ID0EBNAC = value; }
        }

        private string _ID0EKNAC = "";

        [ProtoBuf.ProtoMember(8)]
        [System.ComponentModel.DefaultValue("")]
        public string java_outer_classname
        {
            get { return _ID0EKNAC; }
            set { _ID0EKNAC = value; }
        }

        private bool _ID0ETNAC = false;

        [ProtoBuf.ProtoMember(10)]
        [System.ComponentModel.DefaultValue(false)]
        public bool java_multiple_files
        {
            get { return _ID0ETNAC; }
            set { _ID0ETNAC = value; }
        }

        private google.protobuf.FileOptions.OptimizeMode _ID0E5NAC = google.protobuf.FileOptions.OptimizeMode.CODE_SIZE;

        [ProtoBuf.ProtoMember(9)]
        [System.ComponentModel.DefaultValue(google.protobuf.FileOptions.OptimizeMode.CODE_SIZE)]
        public google.protobuf.FileOptions.OptimizeMode optimize_for
        {
            get { return _ID0E5NAC; }
            set { _ID0E5NAC = value; }
        }

        private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _ID0ELOAC = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();

        [ProtoBuf.ProtoMember(999)]
        public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
        {
            get { return _ID0ELOAC; }
            set
            { // setter needed for XmlSerializer
                _ID0ELOAC.Clear();
                if (value != null)
                {
                    _ID0ELOAC.AddRange(value);
                }
            }
        }

        public enum OptimizeMode
        {
            SPEED = 1,
            CODE_SIZE = 2
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class MessageOptions
    {

        private bool _ID0EEAAE = false;

        [ProtoBuf.ProtoMember(1)]
        [System.ComponentModel.DefaultValue(false)]
        public bool message_set_wire_format
        {
            get { return _ID0EEAAE; }
            set { _ID0EEAAE = value; }
        }

        private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _ID0EPAAE = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();

        [ProtoBuf.ProtoMember(999)]
        public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
        {
            get { return _ID0EPAAE; }
            set
            { // setter needed for XmlSerializer
                _ID0EPAAE.Clear();
                if (value != null)
                {
                    _ID0EPAAE.AddRange(value);
                }
            }
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class FieldOptions
    {

        private google.protobuf.FieldOptions.CType _ID0EOBAE = google.protobuf.FieldOptions.CType.CORD;

        [ProtoBuf.ProtoMember(1)]
        [System.ComponentModel.DefaultValue(google.protobuf.FieldOptions.CType.CORD)]
        public google.protobuf.FieldOptions.CType ctype
        {
            get { return _ID0EOBAE; }
            set { _ID0EOBAE = value; }
        }

        private string _ID0EZBAE = "";

        [ProtoBuf.ProtoMember(9)]
        [System.ComponentModel.DefaultValue("")]
        public string experimental_map_key
        {
            get { return _ID0EZBAE; }
            set { _ID0EZBAE = value; }
        }

        private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _ID0ECCAE = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();

        [ProtoBuf.ProtoMember(999)]
        public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
        {
            get { return _ID0ECCAE; }
            set
            { // setter needed for XmlSerializer
                _ID0ECCAE.Clear();
                if (value != null)
                {
                    _ID0ECCAE.AddRange(value);
                }
            }
        }

        public enum CType
        {
            CORD = 1,
            STRING_PIECE = 2
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class EnumOptions
    {

        private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _ID0E1DAE = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();

        [ProtoBuf.ProtoMember(999)]
        public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
        {
            get { return _ID0E1DAE; }
            set
            { // setter needed for XmlSerializer
                _ID0E1DAE.Clear();
                if (value != null)
                {
                    _ID0E1DAE.AddRange(value);
                }
            }
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class EnumValueOptions
    {

        private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _ID0EZEAE = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();

        [ProtoBuf.ProtoMember(999)]
        public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
        {
            get { return _ID0EZEAE; }
            set
            { // setter needed for XmlSerializer
                _ID0EZEAE.Clear();
                if (value != null)
                {
                    _ID0EZEAE.AddRange(value);
                }
            }
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class ServiceOptions
    {

        private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _ID0EYFAE = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();

        [ProtoBuf.ProtoMember(999)]
        public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
        {
            get { return _ID0EYFAE; }
            set
            { // setter needed for XmlSerializer
                _ID0EYFAE.Clear();
                if (value != null)
                {
                    _ID0EYFAE.AddRange(value);
                }
            }
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class MethodOptions
    {

        private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption> _ID0EXGAE = new System.Collections.Generic.List<google.protobuf.UninterpretedOption>();

        [ProtoBuf.ProtoMember(999)]
        public System.Collections.Generic.List<google.protobuf.UninterpretedOption> uninterpreted_option
        {
            get { return _ID0EXGAE; }
            set
            { // setter needed for XmlSerializer
                _ID0EXGAE.Clear();
                if (value != null)
                {
                    _ID0EXGAE.AddRange(value);
                }
            }
        }

    }

    [System.Serializable, ProtoBuf.ProtoContract]
    public partial class UninterpretedOption
    {

        private readonly System.Collections.Generic.List<google.protobuf.UninterpretedOption.NamePart> _ID0EWHAE = new System.Collections.Generic.List<google.protobuf.UninterpretedOption.NamePart>();

        [ProtoBuf.ProtoMember(2)]
        public System.Collections.Generic.List<google.protobuf.UninterpretedOption.NamePart> name
        {
            get { return _ID0EWHAE; }
            set
            { // setter needed for XmlSerializer
                _ID0EWHAE.Clear();
                if (value != null)
                {
                    _ID0EWHAE.AddRange(value);
                }
            }
        }

        private string _ID0EDIAE = "";

        [ProtoBuf.ProtoMember(3)]
        [System.ComponentModel.DefaultValue("")]
        public string identifier_value
        {
            get { return _ID0EDIAE; }
            set { _ID0EDIAE = value; }
        }

        private ulong _ID0EMIAE = default(ulong);

        [ProtoBuf.ProtoMember(4)]
        [System.ComponentModel.DefaultValue(default(ulong))]
        public ulong positive_int_value
        {
            get { return _ID0EMIAE; }
            set { _ID0EMIAE = value; }
        }

        private long _ID0EVIAE = default(long);

        [ProtoBuf.ProtoMember(5)]
        [System.ComponentModel.DefaultValue(default(long))]
        public long negative_int_value
        {
            get { return _ID0EVIAE; }
            set { _ID0EVIAE = value; }
        }

        private double _ID0E5IAE = default(double);

        [ProtoBuf.ProtoMember(6)]
        [System.ComponentModel.DefaultValue(default(double))]
        public double double_value
        {
            get { return _ID0E5IAE; }
            set { _ID0E5IAE = value; }
        }

        private byte[] _ID0EHJAE = null;

        [ProtoBuf.ProtoMember(7)]
        [System.ComponentModel.DefaultValue(null)]
        public byte[] string_value
        {
            get { return _ID0EHJAE; }
            set { _ID0EHJAE = value; }
        }

        [System.Serializable, ProtoBuf.ProtoContract]
        public partial class NamePart
        {

            private string _ID0EZJAE;

            [ProtoBuf.ProtoMember(1)]
            public string name_part
            {
                get { return _ID0EZJAE; }
                set { _ID0EZJAE = value; }
            }

            private bool _ID0EEKAE;

            [ProtoBuf.ProtoMember(2)]
            public bool is_extension
            {
                get { return _ID0EEKAE; }
                set { _ID0EEKAE = value; }
            }

        }

    }

}
