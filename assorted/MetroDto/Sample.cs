[global::ProtoBuf.ProtoContract(Name = @"StatusHistPair")]
public partial class StatusHistPair : global::ProtoBuf.IExtensible
{
    public StatusHistPair() { }

    private int _date;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name = @"date", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int date
    {
        get { return _date; }
        set { _date = value; }
    }
    private readonly global::System.Collections.Generic.List<MemStatus> _values = new global::System.Collections.Generic.List<MemStatus>();
    [global::ProtoBuf.ProtoMember(2, Name = @"values", DataFormat = global::ProtoBuf.DataFormat.TwosComplement, IsPacked = true)]
    public global::System.Collections.Generic.List<MemStatus> values
    {
        get { return _values; }
    }

    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing) { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
}

[global::ProtoBuf.ProtoContract(Name = @"DiffHistPair")]
public partial class DiffHistPair : global::ProtoBuf.IExtensible
{
    public DiffHistPair() { }

    private int _date;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name = @"date", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int date
    {
        get { return _date; }
        set { _date = value; }
    }
    private readonly global::System.Collections.Generic.List<float> _values = new global::System.Collections.Generic.List<float>();
    [global::ProtoBuf.ProtoMember(2, Name = @"values", DataFormat = global::ProtoBuf.DataFormat.FixedSize, IsPacked = true)]
    public global::System.Collections.Generic.List<float> values
    {
        get { return _values; }
    }

    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing) { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
}

[global::ProtoBuf.ProtoContract(Name = @"SM2Stats")]
public partial class SM2Stats : global::ProtoBuf.IExtensible
{
    public SM2Stats() { }

    private float _easiness;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name = @"easiness", DataFormat = global::ProtoBuf.DataFormat.FixedSize)]
    public float easiness
    {
        get { return _easiness; }
        set { _easiness = value; }
    }
    private int _acqreps;
    [global::ProtoBuf.ProtoMember(2, IsRequired = true, Name = @"acqreps", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int acqreps
    {
        get { return _acqreps; }
        set { _acqreps = value; }
    }
    private int _retreps;
    [global::ProtoBuf.ProtoMember(3, IsRequired = true, Name = @"retreps", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int retreps
    {
        get { return _retreps; }
        set { _retreps = value; }
    }
    private int _lapses;
    [global::ProtoBuf.ProtoMember(4, IsRequired = true, Name = @"lapses", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int lapses
    {
        get { return _lapses; }
        set { _lapses = value; }
    }
    private int _acqrepssincelapse;
    [global::ProtoBuf.ProtoMember(5, IsRequired = true, Name = @"acqrepssincelapse", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int acqrepssincelapse
    {
        get { return _acqrepssincelapse; }
        set { _acqrepssincelapse = value; }
    }
    private int _retrepssincelapse;
    [global::ProtoBuf.ProtoMember(6, IsRequired = true, Name = @"retrepssincelapse", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int retrepssincelapse
    {
        get { return _retrepssincelapse; }
        set { _retrepssincelapse = value; }
    }
    private int _sm2reps;
    [global::ProtoBuf.ProtoMember(7, IsRequired = true, Name = @"sm2reps", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int sm2reps
    {
        get { return _sm2reps; }
        set { _sm2reps = value; }
    }
    private int _lastrep;
    [global::ProtoBuf.ProtoMember(8, IsRequired = true, Name = @"lastrep", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int lastrep
    {
        get { return _lastrep; }
        set { _lastrep = value; }
    }
    private int _nextrep;
    [global::ProtoBuf.ProtoMember(9, IsRequired = true, Name = @"nextrep", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public int nextrep
    {
        get { return _nextrep; }
        set { _nextrep = value; }
    }
    private Grade _grade;
    [global::ProtoBuf.ProtoMember(10, IsRequired = true, Name = @"grade", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public Grade grade
    {
        get { return _grade; }
        set { _grade = value; }
    }
    private MemStatus _memstatus;
    [global::ProtoBuf.ProtoMember(11, IsRequired = true, Name = @"memstatus", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public MemStatus memstatus
    {
        get { return _memstatus; }
        set { _memstatus = value; }
    }
    private readonly global::System.Collections.Generic.List<StatusHistPair> _statushistory = new global::System.Collections.Generic.List<StatusHistPair>();
    [global::ProtoBuf.ProtoMember(12, Name = @"statushistory", DataFormat = global::ProtoBuf.DataFormat.Group)]
    public global::System.Collections.Generic.List<StatusHistPair> statushistory
    {
        get { return _statushistory; }
    }

    private readonly global::System.Collections.Generic.List<DiffHistPair> _difficultyhistory = new global::System.Collections.Generic.List<DiffHistPair>();
    [global::ProtoBuf.ProtoMember(13, Name = @"difficultyhistory", DataFormat = global::ProtoBuf.DataFormat.Group)]
    public global::System.Collections.Generic.List<DiffHistPair> difficultyhistory
    {
        get { return _difficultyhistory; }
    }
    private long _createdon;
    [global::ProtoBuf.ProtoMember(14, IsRequired = false, Name = @"createdon", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public long createdon
    {
        get { return _createdon; }
        set { _createdon = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing) { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
}

[global::ProtoBuf.ProtoContract(Name = @"MemStatus")]
public enum MemStatus
{

    [global::ProtoBuf.ProtoEnum(Name = @"Memorized", Value = 0)]
    Memorized = 0,

    [global::ProtoBuf.ProtoEnum(Name = @"Lapsed", Value = 1)]
    Lapsed = 1,

    [global::ProtoBuf.ProtoEnum(Name = @"InAcquisition", Value = 2)]
    InAcquisition = 2,

    [global::ProtoBuf.ProtoEnum(Name = @"NotSeen", Value = 3)]
    NotSeen = 3,

    [global::ProtoBuf.ProtoEnum(Name = @"None", Value = 4)]
    None = 4
}

[global::ProtoBuf.ProtoContract(Name = @"Grade")]
public enum Grade
{

    [global::ProtoBuf.ProtoEnum(Name = @"Zero", Value = 0)]
    Zero = 0,

    [global::ProtoBuf.ProtoEnum(Name = @"One", Value = 1)]
    One = 1,

    [global::ProtoBuf.ProtoEnum(Name = @"Two", Value = 2)]
    Two = 2,

    [global::ProtoBuf.ProtoEnum(Name = @"Three", Value = 3)]
    Three = 3,

    [global::ProtoBuf.ProtoEnum(Name = @"Four", Value = 4)]
    Four = 4,

    [global::ProtoBuf.ProtoEnum(Name = @"Five", Value = 5)]
    Five = 5
}