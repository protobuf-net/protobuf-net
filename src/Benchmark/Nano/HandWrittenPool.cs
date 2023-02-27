﻿#nullable enable
// #define USE_SPANS
using ProtoBuf;
using ProtoBuf.Nano;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Benchmark.Nano.HandWrittenPool;

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

    ForwardRequest INanoSerializer<ForwardRequest>.Read(ref PrepReader reader)
    {
        throw new NotImplementedException();
    }

    long INanoSerializer<ForwardRequest>.Measure(in ForwardRequest value)
    {
        throw new NotImplementedException();
    }

    void INanoSerializer<ForwardRequest>.Write(in ForwardRequest value, ref PrepWriter writer)
    {
        throw new NotImplementedException();
    }

    ForwardResponse INanoSerializer<ForwardResponse>.Read(ref PrepReader reader)
    {
        throw new NotImplementedException();
    }

    long INanoSerializer<ForwardResponse>.Measure(in ForwardResponse value)
    {
        throw new NotImplementedException();
    }

    void INanoSerializer<ForwardResponse>.Write(in ForwardResponse value, ref PrepWriter writer)
    {
        throw new NotImplementedException();
    }
}

public sealed class ForwardRequest : IDisposable
{
    private ReadOnlyMemory<char> _traceId;
    private ReadOnlyMemory<ForwardPerItemRequest> _itemRequests;
    private ReadOnlyMemory<byte> _requestContextInfo;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ForwardRequest ReadSingle(ref PrepReader reader) => Merge(null, ref reader);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void WriteSingle(ForwardRequest value, ref PrepWriter writer)
    {
        if (!value._traceId.IsEmpty)
        {
            writer.WriteVarint((1 << 3) | (int)WireType.String);
            writer.WriteWithLengthPrefix(value._traceId);
        }
        if (!value._itemRequests.IsEmpty)
        {
            foreach (ref readonly var item in value._itemRequests.Span)
            {
                writer.WriteVarint((2 << 3) | (int)WireType.String);
                writer.WriteVarint(ForwardPerItemRequest.Measure(item));
                ForwardPerItemRequest.WriteSingle(in item, ref writer);
            }
        }
        if (!value._requestContextInfo.IsEmpty)
        {
            writer.WriteVarint((3 << 3) | (int)WireType.String);
            writer.WriteWithLengthPrefix(value._requestContextInfo);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ForwardRequest Merge(ForwardRequest? value, ref PrepReader reader)
    {
        value ??= new(default, default, default);
        uint tag;
        while ((tag = reader.ReadTag()) != 0)
        {
            switch (tag)
            {
                case (1 << 3) | (int)WireType.String:
                    value._traceId = reader.ReadRefCountedString(value._traceId);
                    break;
                case (2 << 3) | (int)WireType.String:
                    unsafe
                    {
                        value._itemRequests = reader.UnsafeAppendLengthPrefixed(value._itemRequests, &ForwardPerItemRequest.Merge, (2 << 3) | (int)WireType.String, 4000);
                    }
                    break;
                case (3 << 3) | (int)WireType.String:
                    reader.ReadRefCountedBytes(ref value._requestContextInfo);
                    break;
            }
        }
        return value;
    }


    public ReadOnlyMemory<char> TraceId => _traceId;
    public ReadOnlyMemory<ForwardPerItemRequest> ItemRequests => _itemRequests;
    public ReadOnlyMemory<byte> RequestContextInfo => _requestContextInfo;

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
        var writer = new PrepWriter(ctx.GetBufferWriter());
        WriteSingle(value, ref writer);
        writer.Dispose();
        ctx.Complete();
    }

    public static ForwardRequest ContextDeserialize(Grpc.Core.DeserializationContext ctx)
    {
        var ros = ctx.PayloadAsReadOnlySequence();
        if (!ros.IsSingleSegment) return Slow(ros);

        var reader = new PrepReader(ros.First);
        var value = ReadSingle(ref reader);
        reader.Dispose();
        return value;

        static ForwardRequest Slow(ReadOnlySequence<byte> payload)
        {
            var len = checked((int)payload.Length);
            var oversized = ArrayPool<byte>.Shared.Rent(len);
            payload.CopyTo(oversized);

            var reader = new PrepReader(oversized, 0, len);
            var value = ReadSingle(ref reader);
            reader.Dispose();
            ArrayPool<byte>.Shared.Return(oversized);
            return value;
        }
    }

    public void Dispose()
    {
        RefCountedMemory.Release(_traceId);
        RefCountedMemory.ReleaseAll(_itemRequests);
        RefCountedMemory.Release(_requestContextInfo);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Measure(ForwardRequest value)
    {
        ulong length = 0;
        if (!value._traceId.IsEmpty)
        {
            length += 1 + PrepWriter.MeasureWithLengthPrefix(value._traceId);
        }
        if (!value._itemRequests.IsEmpty)
        {
            length += 1 * (uint)value._itemRequests.Length;
            foreach (ref readonly var item in value._itemRequests.Span)
            {
                length += PrepWriter.MeasureWithLengthPrefix(ForwardPerItemRequest.Measure(item));
            }
        }
        if (!value._requestContextInfo.IsEmpty)
        {
            length += 1 + PrepWriter.MeasureWithLengthPrefix((uint)value._requestContextInfo.Length);
        }
        return length;
    }

    public ForwardRequest(ReadOnlyMemory<char> traceId, ReadOnlyMemory<ForwardPerItemRequest> itemRequests, ReadOnlyMemory<byte> requestContextInfo)
    {
        _traceId = traceId;
        _itemRequests = itemRequests;
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
public readonly struct ForwardPerItemRequest : IDisposable
{
    private readonly ReadOnlyMemory<byte> _itemId;
    private readonly ReadOnlyMemory<byte> _itemContext;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Merge(ref ForwardPerItemRequest value, ref PrepReader reader, bool reset)
    {
        if (reset) value = default;
        uint tag;
        while ((tag = reader.ReadTag()) != 0)
        {
            switch (tag)
            {
                case (1 << 3) | (int)WireType.String:
                    reader.ReadRefCountedBytes(ref Unsafe.AsRef(in value._itemId));
                    break;
                case (2 << 3) | (int)WireType.String:
                    reader.ReadRefCountedBytes(ref Unsafe.AsRef(in value._itemContext));
                    break;
            }
        }
    }
    public ForwardPerItemRequest(ReadOnlyMemory<byte> itemId, ReadOnlyMemory<byte> itemContext)
    {
        _itemId = itemId;
        _itemContext = itemContext;
    }

    public ReadOnlyMemory<byte> ItemId => _itemId;
    public ReadOnlyMemory<byte> ItemContext => _itemContext;

    public void Dispose()
    {
        RefCountedMemory.Release(_itemId);
        RefCountedMemory.Release(_itemContext);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Measure(in ForwardPerItemRequest value)
    {
        ulong length = 0;
        if (!value._itemId.IsEmpty)
        {
            length += 1 + PrepWriter.MeasureWithLengthPrefix((uint)value._itemId.Length);
        }
        if (!value._itemContext.IsEmpty)
        {
            length += 1 + PrepWriter.MeasureWithLengthPrefix((uint)value._itemContext.Length);
        }
        return length;
    }

    internal static void WriteSingle(in ForwardPerItemRequest value, ref PrepWriter writer)
    {
        if (!value._itemId.IsEmpty)
        {
            writer.WriteVarint((1 << 3) | (int)WireType.String);
            writer.WriteWithLengthPrefix(value._itemId);
        }
        if (!value._itemContext.IsEmpty)
        {
            writer.WriteVarint((2 << 3) | (int)WireType.String);
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
public readonly struct ForwardPerItemResponse : IDisposable
{
    private readonly float _result;
    private readonly ReadOnlyMemory<byte> _extraResult;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Merge(ref ForwardPerItemResponse value, ref PrepReader reader, bool reset)
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
                    reader.ReadRefCountedBytes(ref Unsafe.AsRef(in value._extraResult));
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
        if (!value._extraResult.IsEmpty)
        {
            length += 1 + PrepWriter.MeasureWithLengthPrefix((uint)value._extraResult.Length);
        }
        return length;
    }

    public ForwardPerItemResponse(float result, ReadOnlyMemory<byte> extraResult)
    {
        _result = result;
        _extraResult = extraResult;
    }

    public float Result => _result;
    public ReadOnlyMemory<byte> ExtraResult => _extraResult;

    public void Dispose()
    {
        RefCountedMemory.Release(_extraResult);
    }

    internal static void WriteSingle(in ForwardPerItemResponse value, ref PrepWriter writer)
    {
        if (value._result != 0)
        {
            writer.WriteVarint((1 << 3) | (int)WireType.Fixed32);
            writer.WriteSingle(value._result);
        }
        if (!value._extraResult.IsEmpty)
        {
            writer.WriteVarint((2 << 3) | (int)WireType.String);
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
public sealed class ForwardResponse : IDisposable
{
    private ReadOnlyMemory<ForwardPerItemResponse> _itemResponses;
    private long _routeLatencyInUs;
    private long _routeStartTimeInTicks;

    internal static ulong Measure(ForwardResponse value)
    {
        ulong length = 0;
        if (!value.ItemResponses.IsEmpty)
        {
            length += 1 * (uint)value.ItemResponses.Length;
            foreach (ref readonly var item in value._itemResponses.Span)
            {
                length += PrepWriter.MeasureWithLengthPrefix(ForwardPerItemResponse.Measure(item));
            }
        }
        if (value._routeLatencyInUs != 0)
        {
            length += 1 + PrepWriter.MeasureVarint64((ulong)value._routeLatencyInUs);
        }
        if (value._routeStartTimeInTicks != 0)
        {
            length += 1 + PrepWriter.MeasureVarint64((ulong)value._routeStartTimeInTicks);
        }
        return length;
    }

    internal static void WriteSingle(ForwardResponse value, ref PrepWriter writer)
    {
        if (!value.ItemResponses.IsEmpty)
        {
            foreach (ref readonly var item in value._itemResponses.Span)
            {
                writer.WriteVarint((1 << 3) | (int)WireType.String);
                writer.WriteVarint(ForwardPerItemResponse.Measure(item));
                ForwardPerItemResponse.WriteSingle(in item, ref writer);
            }
        }
        if (value._routeLatencyInUs != 0)
        {
            writer.WriteVarint((2 << 3) | (int)WireType.Varint);
            writer.WriteVarint((ulong)value._routeLatencyInUs);
        }
        if (value._routeStartTimeInTicks != 0)
        {
            writer.WriteVarint((3 << 3) | (int)WireType.Varint);
            writer.WriteVarint((ulong)value._routeStartTimeInTicks);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ForwardResponse ReadSingle(ref PrepReader reader) => Merge(null, ref reader);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ForwardResponse Merge(ForwardResponse? value, ref PrepReader reader)
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
                        value._itemResponses = reader.UnsafeAppendLengthPrefixed(value._itemResponses, &ForwardPerItemResponse.Merge, (1 << 3) | (int)WireType.String, 4000);
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

    public ForwardResponse(ReadOnlyMemory<ForwardPerItemResponse> itemResponses, long routeLatencyInUs, long routeStartTimeInTicks)
    {
        _itemResponses = itemResponses;
        _routeLatencyInUs = routeLatencyInUs;
        _routeStartTimeInTicks = routeStartTimeInTicks;
    }

    public ReadOnlyMemory<ForwardPerItemResponse> ItemResponses => _itemResponses;
    public long RouteLatencyInUs => _routeLatencyInUs;
    public long RouteStartTimeInTicks => _routeStartTimeInTicks;

    public static void ContextSerialize(ForwardResponse value, Grpc.Core.SerializationContext ctx)
    {
        var len = checked((int)Measure(value));
        ctx.SetPayloadLength(len);
        var writer = new PrepWriter(ctx.GetBufferWriter());
        WriteSingle(value, ref writer);
        writer.Dispose();
        ctx.Complete();
    }

    public static ForwardResponse ContextDeserialize(Grpc.Core.DeserializationContext ctx)
    {
        var ros = ctx.PayloadAsReadOnlySequence();
        if (!ros.IsSingleSegment) return Slow(ros);

        var reader = new PrepReader(ros.First);
        var value = ReadSingle(ref reader);
        reader.Dispose();
        return value;

        static ForwardResponse Slow(ReadOnlySequence<byte> payload)
        {
            var len = checked((int)payload.Length);
            var oversized = ArrayPool<byte>.Shared.Rent(len);
            payload.CopyTo(oversized);

            var reader = new PrepReader(oversized, 0, len);
            var value = ReadSingle(ref reader);
            reader.Dispose();
            ArrayPool<byte>.Shared.Return(oversized);
            return value;
        }
    }

    public void Dispose()
    {
        RefCountedMemory.ReleaseAll(_itemResponses);
    }
}
