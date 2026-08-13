using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

// NOTE: no .reference.cs yet - added on Linux, and AotRefGen is net472 so it could not be run.
// Nothing here is refused by ref-emit, so this fixture *should* have one. Differentially covered
// by AotConformanceTests - and unlike [ProtoSurrogate]/[ProtoSerializer] there is no replay: the
// runtime model honours [ProtoDataFormat] itself, so the differential asserts real JIT/AOT parity.
// Run AotRefGen on Windows and commit the result.
//
// Type-scoped deliberately: an assembly-scoped declaration would re-format every fixture in the
// linked assembly - the same trap AGENTS.md records for [module: CompatibilityLevel]. Assembly and
// module scope are proven by protobuf-net.TestDataFormat + the satellite tests instead.
namespace AotFixtures.FormatDefault;

[DataContract, CompatibilityLevel(CompatibilityLevel.Level300)]
[ProtoDataFormat(typeof(Guid), DataFormat.FixedSize)]
[ProtoDataFormat(typeof(int), DataFormat.ZigZag)]
public class Payment
{
    // WCF-style members: no per-member format is expressible here, which is the whole point
    [DataMember(Order = 1)] public Guid Id { get; set; }
    [DataMember(Order = 2)] public Guid? Correlation { get; set; }
    [DataMember(Order = 3)] public List<Guid> Batch { get; set; }
    [DataMember(Order = 4)] public int Amount { get; set; }
    // an explicit [ProtoMember] format beats the type default ([ProtoMember] mixes with the
    // [DataMember] family; it wins for its own member while [DataMember] supplies the rest)
    [ProtoMember(5, DataFormat = DataFormat.FixedSize)] public int Stated { get; set; }
    // a map value does not take the default: [ProtoMap(ValueFormat)] is the tool there
    [DataMember(Order = 6)] public Dictionary<int, Guid> ById { get; set; }
    // select-then-unwrap regression: a repeated *nullable* element must key the ambient default on
    // the unwrapped Guid, not on Guid? - selecting the element before unwrapping Nullable<T>
    [DataMember(Order = 7)] public List<Guid?> Certs { get; set; }
}

public static class FormatDefaultSamples
{
    public static object[] Values =>
    [
        new Payment(),
        new Payment { Id = Guid.Parse("c416e4af-455e-414c-948c-f27873263547"), Amount = -3 },
        new Payment
        {
            Correlation = Guid.Parse("11223344-5566-7788-99aa-bbccddeeff00"),
            Batch = [Guid.Parse("00112233-4455-6677-8899-aabbccddeeff")],
            Stated = -7,
            ById = new() { { 2, Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee") } },
            // elements are non-null: protobuf-net rejects a null element outright, so this exercises
            // the FixedSize default on the unwrapped Guid without also testing null-in-collection
            Certs = [Guid.Parse("55667788-99aa-bbcc-ddee-ff0011223344"), Guid.Empty],
        },
    ];
}

[ProtoModel]
[ProtoSerializable(typeof(Payment))]
public partial class FormatDefaultModel : TypeModel
{
}
