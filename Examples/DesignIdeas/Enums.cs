using ProtoBuf;

namespace Examples.DesignIdeas
{
    /// <summary>
    /// would like to be able to specify custom values for enums;
    /// implementation note: some kind of map: Dictionary<TValue, long>?
    /// note: how to handle -ves? (ArgumentOutOfRangeException?)
    /// note: how to handle flags? (NotSupportedException? at least for now?
    ///             could later use a bitmap sweep?)
    /// </summary>
    enum SomeEnum
    {
        [ProtoEnum(Name="FOO")]
        ChangeName = 3,

        [ProtoEnum(Value = 19)]
        ChangeValue = 5,

        [ProtoEnum(Name="BAR", Value=92)]
        ChangeBoth = 7,
        
        LeaveAlone = 22
    }
}
