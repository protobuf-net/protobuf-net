using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.ComponentModel;

namespace AotFixtures.Enums;

// every underlying type C# permits for an enum. Note char is NOT one of them (CS1008), though the
// CLR itself allows it - so a char-based enum can only arrive from a non-C# assembly.
public enum AsSByte : sbyte { Zero = 0, Neg = -3, Max = sbyte.MaxValue }
public enum AsByte : byte { Zero = 0, Some = 200, Max = byte.MaxValue }
public enum AsInt16 : short { Zero = 0, Neg = -300, Max = short.MaxValue }
public enum AsUInt16 : ushort { Zero = 0, Some = 40000, Max = ushort.MaxValue }
public enum AsInt32 : int { Zero = 0, Neg = -70000, Max = int.MaxValue }
public enum AsUInt32 : uint { Zero = 0, Some = 3000000000, Max = uint.MaxValue }
public enum AsInt64 : long { Zero = 0, Neg = -5000000000, Max = long.MaxValue }
public enum AsUInt64 : ulong { Zero = 0, Some = 10000000000000000000, Max = ulong.MaxValue }

[Flags]
public enum Flagged { None = 0, A = 1, B = 2, C = 4, AB = A | B }

[ProtoContract]
public class WithEnums
{
    [ProtoMember(1)] public AsSByte SByteEnum { get; set; }
    [ProtoMember(2)] public AsByte ByteEnum { get; set; }
    [ProtoMember(3)] public AsInt16 Int16Enum { get; set; }
    [ProtoMember(4)] public AsUInt16 UInt16Enum { get; set; }
    [ProtoMember(5)] public AsInt32 Int32Enum { get; set; }
    [ProtoMember(6)] public AsUInt32 UInt32Enum { get; set; }
    [ProtoMember(7)] public AsInt64 Int64Enum { get; set; }
    [ProtoMember(8)] public AsUInt64 UInt64Enum { get; set; }

    [ProtoMember(9)] public Flagged Flags { get; set; }

    // nullable enum: presence rather than value decides
    [ProtoMember(10)] public AsInt32? MaybeEnum { get; set; }

    // declared default on an enum, and on a nullable enum; the initialisers are required, since
    // [DefaultValue] affects writing only and is otherwise lossy across a round-trip
    [ProtoMember(11), DefaultValue(AsInt32.Neg)] public AsInt32 EnumWithDefault { get; set; } = AsInt32.Neg;
    [ProtoMember(12), DefaultValue(Flagged.AB)] public Flagged? MaybeFlagsWithDefault { get; set; } = Flagged.AB;

    // char is not an enum base, but is an adjacent scalar gap
    [ProtoMember(13)] public char Character { get; set; }
    [ProtoMember(14)] public char? MaybeCharacter { get; set; }
}

public static class EnumsSamples
{
    public static object[] Values =>
    [
        new WithEnums(),
        new WithEnums { SByteEnum = AsSByte.Neg, ByteEnum = AsByte.Some, Int16Enum = AsInt16.Neg },
        new WithEnums { UInt16Enum = AsUInt16.Some, Int32Enum = AsInt32.Neg, UInt32Enum = AsUInt32.Some },
        new WithEnums { Int64Enum = AsInt64.Neg, UInt64Enum = AsUInt64.Some },
        new WithEnums { SByteEnum = AsSByte.Max, ByteEnum = AsByte.Max, Int16Enum = AsInt16.Max,
                        UInt16Enum = AsUInt16.Max, Int32Enum = AsInt32.Max, UInt32Enum = AsUInt32.Max,
                        Int64Enum = AsInt64.Max, UInt64Enum = AsUInt64.Max },
        new WithEnums { Flags = Flagged.AB },
        new WithEnums { Flags = Flagged.A | Flagged.C },     // a combination with no declared name
        new WithEnums { MaybeEnum = AsInt32.Zero },          // present but zero: written
        new WithEnums { MaybeEnum = AsInt32.Neg },
        new WithEnums { EnumWithDefault = AsInt32.Neg },     // at declared default: not written
        new WithEnums { EnumWithDefault = AsInt32.Zero },    // not the declared default: written
        new WithEnums { MaybeFlagsWithDefault = Flagged.AB },
        new WithEnums { MaybeFlagsWithDefault = Flagged.None },
        new WithEnums { Character = 'a', MaybeCharacter = '\0' },
        new WithEnums { Character = '中', MaybeCharacter = char.MaxValue },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(WithEnums))]
public partial class EnumsModel : TypeModel
{
}
