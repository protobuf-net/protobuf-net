using ProtoBuf;
using System.Collections.Generic;

namespace Benchmark.Nano.SimpleProtobufNet;

public readonly struct ForwardPerItemRequest
{
    public ForwardPerItemRequest(byte[] itemId, byte[] itemContext)
    {
        ItemId = itemId;
        ItemContext = itemContext;
    }

    public byte[] ItemId { get; }
    public byte[] ItemContext { get; }
}

public readonly struct ForwardPerItemResponse
{
    public ForwardPerItemResponse(float result, byte[] extraResult)
    {
        Result = result;
        ExtraResult = extraResult;
    }

    public float Result { get; }
    public byte[] ExtraResult { get; }
}

[ProtoContract]
public sealed class ForwardRequest
{
    [ProtoMember(1)]
    public string TraceId { get; set; }

    [ProtoMember(2)]
    public List<ForwardPerItemRequest> ItemRequests { get; } = new List<ForwardPerItemRequest>(4000);

    [ProtoMember(3)]
    public byte[] RequestContextInfo {get;set;}
}

[ProtoContract]
public sealed class ForwardResponse
{
    [ProtoMember(1)]
    public List<ForwardPerItemResponse> ItemResponses { get; } = new List<ForwardPerItemResponse>(4000);
    [ProtoMember(2)]
    public long RouteLatencyInUs { get; set; }
    [ProtoMember(3)]
    public long RouteStartTimeInTicks { get; set; }
}