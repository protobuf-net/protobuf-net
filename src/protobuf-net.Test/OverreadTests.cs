using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using Xunit;

namespace ProtoBuf.Test;

public class OverreadTests
{
    private static byte[] GetPayload(ulong length, int padding = 0, bool trim = true)
    {
        Span<byte> span = stackalloc byte[32];
        var len = ProtoWriter.State.WriteVarint64((1 << 3) | (int)WireType.String, span, 0);
        len += ProtoWriter.State.WriteVarint64(length, span, len);
        if (padding != 0) span.Slice(len, padding).Fill((byte)'a');
        if (trim) span = span.Slice(0, len + padding);
        return span.ToArray();
    }

    // we need the stream to not be obviously MemoryStream, which protobuf-net understands
    private static Stream ToStream(byte[] payload) => new BufferedStream(new MemoryStream(payload));

    private static ReadOnlySequence<byte> ToSequence(byte[] payload)
    {
        Segment first = new(payload.AsMemory(0, payload.Length / 2)),
            second = new(payload.AsMemory(payload.Length / 2), first);
        var seq = new ReadOnlySequence<byte>(
            first, 0, second, second.Memory.Length);
        Assert.Equal(payload.Length, seq.Length);
        Assert.False(seq.IsSingleSegment);
        return seq;
    }

    private sealed class Segment : ReadOnlySequenceSegment<byte>
    {
        public Segment(ReadOnlyMemory<byte> memory, Segment previous = null)
        {
            Memory = memory;
            if (previous is not null)
            {
                RunningIndex = previous.RunningIndex + previous.Memory.Length;
                previous.Next = this;
            }
        }
    }

    private const ulong Size1Gb = 1UL * 1024 * 1024 * 1024;

    [Fact]
    public void NormalBufferByteArray()
    {
        var payload = GetPayload(3, padding: 3);
        var obj = Serializer.Deserialize<WithByteArray>(payload);
        Assert.Equal("aaa"u8, obj.Value);
    }

    [Fact]
    public void OverreadBufferByteArray()
    {
        var payload = GetPayload(Size1Gb);
        var ex = Assert.Throws<EndOfStreamException>(() => Serializer.Deserialize<WithByteArray>(payload));
        Assert.True(ex.Data.Contains("defensive"));
    }

    [Fact]
    public void NormalSequenceByteArray()
    {
        var payload = ToSequence(GetPayload(3, padding: 3));
        var obj = Serializer.Deserialize<WithByteArray>(payload);
        Assert.Equal("aaa"u8, obj.Value);
    }

    [Fact]
    public void OverreadSequenceByteArray()
    {
        var payload = ToSequence(GetPayload(Size1Gb));
        var ex = Assert.Throws<EndOfStreamException>(() => Serializer.Deserialize<WithByteArray>(payload));
        Assert.True(ex.Data.Contains("defensive"));
    }

    [Fact]
    public void NormalStreamByteArray()
    {
        var payload = ToStream(GetPayload(3, padding: 3));
        var obj = Serializer.Deserialize<WithByteArray>(payload);
        Assert.Equal("aaa"u8, obj.Value);
    }

    [Fact]
    public void OverreadStreamByteArray()
    {
        var payload = ToStream(GetPayload(Size1Gb));
        var ex = Assert.Throws<EndOfStreamException>(() => Serializer.Deserialize<WithByteArray>(payload));
        Assert.True(ex.Data.Contains("defensive"));
    }

    [Fact]
    public void NormalBufferString()
    {
        var payload = GetPayload(3, padding: 3);
        var obj = Serializer.Deserialize<WithString>(payload);
        Assert.Equal("aaa", obj.Value);
    }

    [Fact]
    public void OverreadBufferString()
    {
        var payload = GetPayload(Size1Gb);
        var ex = Assert.Throws<EndOfStreamException>(() => Serializer.Deserialize<WithString>(payload));
        Assert.True(ex.Data.Contains("defensive"));
    }

    [Fact]
    public void NormalSequenceString()
    {
        var payload = ToSequence(GetPayload(3, padding: 3));
        var obj = Serializer.Deserialize<WithString>(payload);
        Assert.Equal("aaa", obj.Value);
    }

    [Fact]
    public void OverreadSequenceString()
    {
        var payload = ToSequence(GetPayload(Size1Gb));
        var ex = Assert.Throws<EndOfStreamException>(() => Serializer.Deserialize<WithString>(payload));
        Assert.True(ex.Data.Contains("defensive"));
    }

    [Fact]
    public void NormalStreamString()
    {
        var payload = ToStream(GetPayload(3, padding: 3));
        var obj = Serializer.Deserialize<WithString>(payload);
        Assert.Equal("aaa", obj.Value);
    }

    [Fact]
    public void OverreadStreamString()
    {
        var payload = ToStream(GetPayload(Size1Gb));
        var ex = Assert.Throws<EndOfStreamException>(() => Serializer.Deserialize<WithString>(payload));
        Assert.True(ex.Data.Contains("defensive"));
    }

    [Fact]
    public void NormalBufferInt32Array()
    {
        var payload = GetPayload(3, padding: 3);
        var obj = Serializer.Deserialize<WithInt32Array>(payload);
        Assert.Equal(new int[] { 'a', 'a', 'a' }, obj.Value);
    }

    [Fact]
    public void OverreadBufferInt32Array()
    {
        var payload = GetPayload(Size1Gb);
        var ex = Assert.Throws<EndOfStreamException>(() => Serializer.Deserialize<WithInt32Array>(payload));
        Assert.True(ex.Data.Contains("defensive"));
    }

    [Fact]
    public void NormalSequenceInt32Array()
    {
        var payload = ToSequence(GetPayload(3, padding: 3));
        var obj = Serializer.Deserialize<WithInt32Array>(payload);
        Assert.Equal(new int[] { 'a', 'a', 'a' }, obj.Value);
    }

    [Fact]
    public void OverreadSequenceInt32Array()
    {
        var payload = ToSequence(GetPayload(Size1Gb));
        var ex = Assert.Throws<EndOfStreamException>(() => Serializer.Deserialize<WithInt32Array>(payload));
        Assert.True(ex.Data.Contains("defensive"));
    }

    [Fact]
    public void NormalStreamInt32Array()
    {
        var payload = ToStream(GetPayload(3, padding: 3));
        var obj = Serializer.Deserialize<WithInt32Array>(payload);
        Assert.Equal(new int[] { 'a', 'a', 'a' }, obj.Value);
    }

    [Fact]
    public void OverreadStreamInt32Array()
    {
        var payload = ToStream(GetPayload(Size1Gb));
        var ex = Assert.Throws<EndOfStreamException>(() => Serializer.Deserialize<WithInt32Array>(payload));
        Assert.True(ex.Data.Contains("defensive"));
    }

    [ProtoContract]
    public class WithByteArray
    {
        [ProtoMember(1)] public byte[] Value { get; set; } = [];
    }

    [ProtoContract]
    public class WithString
    {
        [ProtoMember(1)] public string Value { get; set; } = "";
    }
    
    [ProtoContract]
    public class WithInt32Array
    {
        [ProtoMember(1)] public int[] Value { get; set; } = [];
    }
}