#nullable enable
// #define USE_SPANS
using ProtoBuf;
using ProtoBuf.Nano;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Benchmark.Nano.HandWrittenNoPool;

//static class WireTypes
//{
//    public const int Varint = 0;
//    public const int Fixed64 = 1;
//    public const int LengthDelimited = 2;
//    public const int StartGroup = 3;
//    public const int EndGroup = 4;
//    public const int Fixed32 = 5;
//}

internal sealed class HCSerializer : INanoSerializer<ForwardRequest>, INanoSerializer<ForwardResponse> // , INanoSerializer<HCForwardPerItemRequest>, INanoSerializer<HCForwardPerItemResponse>
{
    public static HCSerializer Instance { get; } = new HCSerializer();
    private HCSerializer() { }

    ForwardRequest INanoSerializer<ForwardRequest>.Read(ref Reader reader)
    {
        throw new NotImplementedException();
    }

    long INanoSerializer<ForwardRequest>.Measure(in ForwardRequest value)
    {
        throw new NotImplementedException();
    }

    void INanoSerializer<ForwardRequest>.Write(in ForwardRequest value, ref Writer writer)
    {
        throw new NotImplementedException();
    }

    ForwardResponse INanoSerializer<ForwardResponse>.Read(ref Reader reader)
    {
        throw new NotImplementedException();
    }

    long INanoSerializer<ForwardResponse>.Measure(in ForwardResponse value)
    {
        throw new NotImplementedException();
    }

    void INanoSerializer<ForwardResponse>.Write(in ForwardResponse value, ref Writer writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class ForwardRequest
{
    private string? _traceId;
    private readonly List<ForwardPerItemRequest> _itemRequests;
    private byte[]? _requestContextInfo;

    internal static readonly unsafe delegate*<ref Reader, ForwardRequest> UnsafeReader = &ReadSingle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ForwardRequest ReadSingle(ref Reader reader) => Merge(null, ref reader);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteSingle(ForwardRequest value, ref Writer writer)
    {
        if (value._traceId is { Length: > 0 })
        {
            writer.WriteTag((1 << 3) | (int)WireType.String);
            writer.WriteWithLengthPrefix(value._traceId);
        }
        if (value._itemRequests.Count > 0)
        {
#if NET5_0_OR_GREATER
            foreach (ref readonly var item in CollectionsMarshal.AsSpan(value._itemRequests))
#else
            foreach (var item in value._itemRequests)
#endif
            {
                writer.WriteTag((2 << 3) | (int)WireType.String);
                writer.WriteVarintUInt64(ForwardPerItemRequest.Measure(item));
                ForwardPerItemRequest.WriteSingle(in item, ref writer);
            }
        }
        if (value._requestContextInfo is { Length: > 0 })
        {
            writer.WriteTag((3 << 3) | (int)WireType.String);
            writer.WriteWithLengthPrefix(value._requestContextInfo);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ForwardRequest Merge(ForwardRequest? value, ref Reader reader)
    {
        value ??= new(default, default, default);
        uint tag;
        while ((tag = reader.ReadTag()) != 0)
        {
            switch (tag)
            {
                case (1 << 3) | (int)WireType.String:
                    value._traceId = reader.ReadString();
                    break;
                case (2 << 3) | (int)WireType.String:
                    unsafe
                    {
                        reader.UnsafeAppendLengthPrefixed(value._itemRequests, &ForwardPerItemRequest.Merge, (2 << 3) | (int)WireType.String);
                    }
                    break;
                case (3 << 3) | (int)WireType.String:
                    value._requestContextInfo = reader.ReadBytes();
                    break;
            }
        }
        return value;
    }


    public string? TraceId => _traceId;
    public List<ForwardPerItemRequest> ItemRequests => _itemRequests;
    public byte[]? RequestContextInfo => _requestContextInfo;

    //    public static readonly global::System.Action<T, grpc::SerializationContext> Serializer = (value, ctx) =>
    //    {
    //        global::ProtoBuf.IMeasuredProtoOutput<global::System.Buffers.IBufferWriter<byte>> measuredSerializer = CustomTypeModel.Instance;
    //        using var measured = measuredSerializer.Measure(value);
    //        int len = checked((int)measured.Length);
    //        ctx.SetPayloadLength(len);
    //        measuredSerializer.Serialize(measured, ctx.GetBufferWriter());
    //        ctx.Complete();
    //    };
    //    public static readonly global::System.Func<grpc::DeserializationContext, T> Deserializer = ctx =>
    //    {
    //        var buffer = ctx.PayloadAsReadOnlySequence();
    //        return CustomTypeModel.Instance.Deserialize<T>(buffer);
    //    };
    public static void ContextSerialize(ForwardRequest value, Grpc.Core.SerializationContext ctx)
    {
        var len = checked((int)Measure(value));
        ctx.SetPayloadLength(len);
        var writer = new Writer(ctx.GetBufferWriter());
        WriteSingle(value, ref writer);
        writer.Dispose();
        ctx.Complete();
    }

    public static ForwardRequest ContextDeserialize(Grpc.Core.DeserializationContext ctx)
    {
        var ros = ctx.PayloadAsReadOnlySequence();
        if (!ros.IsSingleSegment) return Slow(ros);

        var reader = new Reader(ros.First);
        var value = ReadSingle(ref reader);
        reader.Dispose();
        return value;

        static ForwardRequest Slow(ReadOnlySequence<byte> payload)
        {
            var len = checked((int)payload.Length);
            var oversized = ArrayPool<byte>.Shared.Rent(len);
            payload.CopyTo(oversized);

            var reader = new Reader(oversized, 0, len);
            var value = ReadSingle(ref reader);
            reader.Dispose();
            ArrayPool<byte>.Shared.Return(oversized);
            return value;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Measure(ForwardRequest value)
    {
        ulong length = 0;
        if (value._traceId is { Length: > 0 })
        {
            length += 1 + Writer.MeasureWithLengthPrefix(value._traceId);
        }
        if (value._itemRequests.Count > 0)
        {
            length += 1 * (uint)value._itemRequests.Count;
#if NET5_0_OR_GREATER
            foreach (ref readonly var item in CollectionsMarshal.AsSpan(value._itemRequests))
#else
            foreach (var item in value._itemRequests)
#endif
            {
                length += Writer.MeasureWithLengthPrefix(ForwardPerItemRequest.Measure(item));
            }
        }
        if (value._requestContextInfo is { Length: > 0 })
        {
            length += 1 + Writer.MeasureWithLengthPrefix((uint)value._requestContextInfo.Length);
        }
        return length;
    }

    public ForwardRequest(string? traceId, List<ForwardPerItemRequest>? itemRequests, byte[]? requestContextInfo)
    {
        _traceId = traceId;
        _itemRequests = itemRequests ?? new List<ForwardPerItemRequest>(3500);
        _requestContextInfo = requestContextInfo;
    }
}

/*
message ForwardPerItemRequest
{
  bytes itemId = 1;
  bytes itemContext = 2;
}
*/
public readonly struct ForwardPerItemRequest
{
    private readonly byte[] _itemId;
    private readonly byte[] _itemContext;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Merge(ref ForwardPerItemRequest value, ref Reader reader, bool reset)
    {
        if (reset) value = default;
        uint tag;
        while ((tag = reader.ReadTag()) != 0)
        {
            switch (tag)
            {
                case (1 << 3) | (int)WireType.String:
                    Unsafe.AsRef(in value._itemId) = reader.ReadBytes();
                    break;
                case (2 << 3) | (int)WireType.String:
                    Unsafe.AsRef(in value._itemContext) = reader.ReadBytes();
                    break;
            }
        }
    }
    public ForwardPerItemRequest(byte[] itemId, byte[] itemContext)
    {
        _itemId = itemId;
        _itemContext = itemContext;
    }

    public byte[] ItemId => _itemId;
    public byte[] ItemContext => _itemContext;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Measure(in ForwardPerItemRequest value)
    {
        ulong length = 0;
        if (value._itemId is { Length: > 0 })
        {
            length += 1 + Writer.MeasureWithLengthPrefix((uint)value._itemId.Length);
        }
        if (value._itemContext is { Length: > 0 })
        {
            length += 1 + Writer.MeasureWithLengthPrefix((uint)value._itemContext.Length);
        }
        return length;
    }

    internal static void WriteSingle(in ForwardPerItemRequest value, ref Writer writer)
    {
        if (value._itemId is { Length: > 0 })
        {
            writer.WriteTag((1 << 3) | (int)WireType.String);
            writer.WriteWithLengthPrefix(value._itemId);
        }
        if (value._itemContext is { Length:>0 })
        {
            writer.WriteTag((2 << 3) | (int)WireType.String);
            writer.WriteWithLengthPrefix(value._itemContext);
        }
    }
}

/*
message ForwardPerItemResponse {
  float result = 1;
  bytes extraResult = 2;
}
*/
public readonly struct ForwardPerItemResponse
{
    private readonly float _result;
    private readonly byte[] _extraResult;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Merge(ref ForwardPerItemResponse value, ref Reader reader, bool reset)
    {
        if (reset) value = default;
        uint tag;
        while ((tag = reader.ReadTag()) != 0)
        {
            switch (tag)
            {
                case (1 << 3) | (int)WireType.Fixed32:
                    Unsafe.AsRef(in value._result) = reader.ReadSingle();
                    break;
                case (2 << 3) | (int)WireType.String:
                    Unsafe.AsRef(in value._extraResult) = reader.ReadBytes();
                    break;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Measure(in ForwardPerItemResponse value)
    {
        ulong length = 0;
        if (value._result != 0)
        {
            length += 1 + 4;
        }
        if (value._extraResult is { Length: > 0 })
        {
            length += 1 + Writer.MeasureWithLengthPrefix((uint)value._extraResult.Length);
        }
        return length;
    }

    public ForwardPerItemResponse(float result, byte[] extraResult)
    {
        _result = result;
        _extraResult = extraResult;
    }

    public float Result => _result;
    public byte[] ExtraResult => _extraResult;

    internal static void WriteSingle(in ForwardPerItemResponse value, ref Writer writer)
    {
        if (value._result != 0)
        {
            writer.WriteTag((1 << 3) | (int)WireType.Fixed32);
            writer.WriteSingle(value._result);
        }
        if (value._extraResult is { Length: > 0 })
        {
            writer.WriteTag((2 << 3) | (int)WireType.String);
            writer.WriteWithLengthPrefix(value._extraResult);
        }
    }
}

/*
message ForwardResponse {
  repeated ForwardPerItemResponse itemResponses = 1;
  int64 routeLatencyInUs = 2;
  int64 routeStartTimeInTicks = 3;
}
*/
public sealed class ForwardResponse
{
    private readonly List<ForwardPerItemResponse> _itemResponses;
    private long _routeLatencyInUs;
    private long _routeStartTimeInTicks;

    internal static ulong Measure(ForwardResponse value)
    {
        ulong length = 0;
        if (value.ItemResponses.Count > 0)
        {
            length += 1 * (uint)value.ItemResponses.Count;
#if NET5_0_OR_GREATER
            foreach (ref readonly var item in CollectionsMarshal.AsSpan(value._itemResponses))
#else
            foreach (var item in value._itemResponses)
#endif
            {
                length += Writer.MeasureWithLengthPrefix(ForwardPerItemResponse.Measure(item));
            }
        }
        if (value._routeLatencyInUs != 0)
        {
            length += 1 + Writer.MeasureVarint64((ulong)value._routeLatencyInUs);
        }
        if (value._routeStartTimeInTicks != 0)
        {
            length += 1 + Writer.MeasureVarint64((ulong)value._routeStartTimeInTicks);
        }
        return length;
    }

    internal static void WriteSingle(ForwardResponse value, ref Writer writer)
    {
        if (value.ItemResponses.Count > 0)
        {
#if NET5_0_OR_GREATER
            foreach (ref readonly var item in CollectionsMarshal.AsSpan(value._itemResponses))
#else
            foreach (var item in value._itemResponses)
#endif
            {
                writer.WriteTag((1 << 3) | (int)WireType.String);
                writer.WriteVarintUInt64(ForwardPerItemResponse.Measure(item));
                ForwardPerItemResponse.WriteSingle(in item, ref writer);
            }
        }
        if (value._routeLatencyInUs != 0)
        {
            writer.WriteTag((2 << 3) | (int)WireType.Varint);
            writer.WriteVarintUInt64((ulong)value._routeLatencyInUs);
        }
        if (value._routeStartTimeInTicks != 0)
        {
            writer.WriteTag((3 << 3) | (int)WireType.Varint);
            writer.WriteVarintUInt64((ulong)value._routeStartTimeInTicks);
        }
    }

    internal static readonly unsafe delegate*<ref Reader, ForwardResponse> UnsafeReader = &ReadSingle;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ForwardResponse ReadSingle(ref Reader reader) => Merge(null, ref reader);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ForwardResponse Merge(ForwardResponse? value, ref Reader reader)
    {
        value ??= new(default, 0, 0);
        uint tag;
        while ((tag = reader.ReadTag()) != 0)
        {
            switch (tag)
            {
                case (1 << 3) | (int)WireType.String:
                    unsafe
                    {
                        reader.UnsafeAppendLengthPrefixed(value._itemResponses, &ForwardPerItemResponse.Merge, (1 << 3) | (int)WireType.String);
                    }
                    break;
                case (2 << 3) | (int)WireType.Varint:
                    value._routeLatencyInUs = reader.ReadVarintInt64();
                    break;
                case (3 << 3) | (int)WireType.Varint:
                    value._routeStartTimeInTicks = reader.ReadVarintInt64();
                    break;
            }
        }
        return value;
    }

    public ForwardResponse(List<ForwardPerItemResponse>? itemResponses, long routeLatencyInUs, long routeStartTimeInTicks)
    {
        _itemResponses = itemResponses ?? new List<ForwardPerItemResponse>(3500);
        _routeLatencyInUs = routeLatencyInUs;
        _routeStartTimeInTicks = routeStartTimeInTicks;
    }

    public List<ForwardPerItemResponse> ItemResponses => _itemResponses;
    public long RouteLatencyInUs => _routeLatencyInUs;
    public long RouteStartTimeInTicks => _routeStartTimeInTicks;

    public static void ContextSerialize(ForwardResponse value, Grpc.Core.SerializationContext ctx)
    {
        var len = checked((int)Measure(value));
        ctx.SetPayloadLength(len);
        var writer = new Writer(ctx.GetBufferWriter());
        WriteSingle(value, ref writer);
        writer.Dispose();
        ctx.Complete();
    }

    public static ForwardResponse ContextDeserialize(Grpc.Core.DeserializationContext ctx)
    {
        var ros = ctx.PayloadAsReadOnlySequence();
        if (!ros.IsSingleSegment) return Slow(ros);

        var reader = new Reader(ros.First);
        var value = ReadSingle(ref reader);
        reader.Dispose();
        return value;

        static ForwardResponse Slow(ReadOnlySequence<byte> payload)
        {
            var len = checked((int)payload.Length);
            var oversized = ArrayPool<byte>.Shared.Rent(len);
            payload.CopyTo(oversized);

            var reader = new Reader(oversized, 0, len);
            var value = ReadSingle(ref reader);
            reader.Dispose();
            ArrayPool<byte>.Shared.Return(oversized);
            return value;
        }
    }
}
