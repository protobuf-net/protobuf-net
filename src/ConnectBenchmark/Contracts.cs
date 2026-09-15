using System.Collections.Generic;
using ProtoBuf;

namespace ProtoBuf.ConnectBenchmark;

/// <summary>
/// A deliberately ordinary message: a few scalars, a string, some bytes, an enum, a nested message
/// and a small repeated field.
/// </summary>
/// <remarks>
/// Not the breadth fixture from the JSON differential. That one exists to walk every cell of the
/// mapping and is enormous; a benchmark wants something shaped like what people actually send, or the
/// numbers describe a message nobody has.
/// </remarks>
[ProtoContract]
public class Payload
{
    [ProtoMember(1)] public int Id { get; set; }
    [ProtoMember(2)] public long Ticks { get; set; }
    [ProtoMember(3)] public string Name { get; set; }
    [ProtoMember(4)] public bool Active { get; set; }
    [ProtoMember(5)] public double Score { get; set; }
    [ProtoMember(6)] public byte[] Blob { get; set; }
    [ProtoMember(7)] public Level Level { get; set; }
    [ProtoMember(8)] public Detail Detail { get; set; }
    [ProtoMember(9)] public List<string> Tags { get; set; } = new();
    [ProtoMember(10)] public List<int> Counts { get; set; } = new();
}

[ProtoContract]
public class Detail
{
    [ProtoMember(1)] public string Note { get; set; }
    [ProtoMember(2)] public int Weight { get; set; }
}

[ProtoContract]
public enum Level { None = 0, Low = 1, High = 2 }

[ProtoModel]
[ProtoSerializable(typeof(Payload))]
public partial class BenchModel : ProtoBuf.Meta.TypeModel { }
