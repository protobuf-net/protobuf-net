using ProtoBuf;
using System.IO;
using System;

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
    [ProtoContract]
    class EnumFoo
    {
        [ProtoMember(1)]
        public SomeEnum Bar { get; set; }
    }

    public static class EnumTests
    {
        public static bool RunEnumTests()
        {
            bool pass =true;
            pass |= CheckValue(SomeEnum.ChangeBoth, 0x08, 92);
            pass |= CheckValue(SomeEnum.ChangeName, 0x08, 03);
            pass |= CheckValue(SomeEnum.ChangeValue, 0x08, 19);
            pass |= CheckValue(SomeEnum.LeaveAlone, 0x08, 22);
            return pass;
        }
        static bool CheckValue(SomeEnum val, params byte[] expected)
        {
            EnumFoo foo = new EnumFoo { Bar = val };
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, foo);
                ms.Position = 0;
                byte[] buffer = ms.ToArray();
                if (buffer.Length != expected.Length)
                {
                    Console.WriteLine("\tBuffer length mismatch: {0}", val);
                    return false;
                }
                for (int i = 0; i < buffer.Length; i++)
                {
                    if (buffer[i] != expected[i])
                    {
                        Console.WriteLine("\tBuffer content mismatch: {0}", val);
                        return false;
                    }
                }
                EnumFoo clone = Serializer.Deserialize<EnumFoo>(ms);
                if (clone.Bar != val)
                {
                    Console.WriteLine("\tValue mismatch: {0}", val);
                    return false;
                }
                return true;
            }
        }
    }
}
