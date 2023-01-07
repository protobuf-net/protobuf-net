using ProtoBuf.Meta;
using System;
using System.IO;
using Xunit;

namespace ProtoBuf.Test.Issues;

public class Issue987
{
    [Fact]
    public void CanRoundTripClass_Root() => CanRoundTripFromRoot(new TestPacketClass { Message = "abc" }, "A2-06-05-0A-03-61-62-63");
    [Fact]
    public void CanRoundTripClass_Leaf() => CanRoundTripFromLeaf(new TestPacketClass { Message = "abc" }, "0A-03-61-62-63");

    [Fact]
    public void CanRoundTripStruct_Root() => CanRoundTripFromRoot(new TestPacketStruct { Message = "abc" }, "B0-06-05-0A-03-61-62-63");
    [Fact]
    public void CanRoundTripStruct_Leaf() => CanRoundTripFromLeaf(new TestPacketStruct { Message = "abc" }, "0A-03-61-62-63");

    private static void CanRoundTripFromRoot<T>(T packet, string expectedHex) where T : IPacket
    {
        Assert.Equal("abc", packet.MessageValue);
        var model = RuntimeTypeModel.Create();
        model.AutoCompile = false;
        using var ms = new MemoryStream();
        model.Serialize<IPacket>(ms, packet);
        if (!ms.TryGetBuffer(out var segment)) segment = new(ms.ToArray());
        var hex = BitConverter.ToString(segment.Array, segment.Offset, segment.Count);
        Assert.Equal(expectedHex, hex);

        ms.Position = 0;
        var clone = Assert.IsType<T>(model.Deserialize<IPacket>(ms));
        Assert.Equal("abc", clone.MessageValue);
    }

    private static void CanRoundTripFromLeaf<T>(T packet, string expectedHex) where T : IPacket
    {
        Assert.Equal("abc", packet.MessageValue);
        var model = RuntimeTypeModel.Create();
        model.AutoCompile = false;
        using var ms = new MemoryStream();
        model.Serialize<T>(ms, packet);
        if (!ms.TryGetBuffer(out var segment)) segment = new(ms.ToArray());
        var hex = BitConverter.ToString(segment.Array, segment.Offset, segment.Count);
        Assert.Equal(expectedHex, hex);

        ms.Position = 0;
        var clone = Assert.IsType<T>(model.Deserialize<T>(ms));
        Assert.Equal("abc", clone.MessageValue);
    }

    [ProtoContract]
    [ProtoInclude(100, typeof(TestPacketClass))]
    [ProtoInclude(101, typeof(TestPacketStruct))]
    public interface IPacket
    {
        string MessageValue { get; }
    }

    [ProtoContract] // (SkipConstructor = true)
    public struct TestPacketStruct : IPacket
    {
        [ProtoMember(1)]
        public string Message;

        string IPacket.MessageValue => Message;
    }

    [ProtoContract]
    public class TestPacketClass : IPacket
    {
        [ProtoMember(1)]
        public string Message;

        string IPacket.MessageValue => Message;
    }
}
