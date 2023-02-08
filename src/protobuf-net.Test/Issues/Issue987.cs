using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ProtoBuf.Test.Issues;

public class Issue987
{
    [Theory]
    [InlineData("Runtime")]
    [InlineData("CompileInPlace")]
    [InlineData("Compile")]
    public void CanRoundTripClass_Root(string mode) => CanRoundTripFromRoot(new TestPacketClass { Message = "abc" }, "A2-06-05-0A-03-61-62-63", mode);

    [Theory]
    [InlineData("Runtime")]
    [InlineData("CompileInPlace")]
    [InlineData("Compile")]
    public void CanRoundTripClass_Leaf(string mode) => CanRoundTripFromLeaf(new TestPacketClass { Message = "abc" }, "0A-03-61-62-63", mode);

    [Theory]
    [InlineData("Runtime")]
    [InlineData("CompileInPlace")]
    [InlineData("Compile")]
    public void CanRoundTripStruct_Root(string mode) => CanRoundTripFromRoot(new TestPacketStruct { Message = "abc" }, "AA-06-05-0A-03-61-62-63", mode);

    [Theory]
    [InlineData("Runtime")]
    [InlineData("CompileInPlace")]
    [InlineData("Compile")]
    public void CanRoundTripStruct_Leaf(string mode) => CanRoundTripFromLeaf(new TestPacketStruct { Message = "abc" }, "0A-03-61-62-63", mode);

    [Fact]
    public void CheckModelConfig()
    {
        var types = RuntimeTypeModel.Default[typeof(IPacket)].GetSubtypes();

        var s = string.Join(",",
            from type in types
            orderby type.FieldNumber
            select $"{type.FieldNumber}:{type.DerivedType.Type.Name}");

        Assert.Equal("100:TestPacketClass,101:TestPacketStruct", s);
    }

    static TypeModel GetModel(string mode)
    {
        var setup = RuntimeTypeModel.Create();
        setup.AutoCompile = false;
        setup.Add(typeof(IPacket));
        setup.Add(typeof(TestPacketClass));
        setup.Add(typeof(TestPacketStruct));

        switch (mode)
        {
            case "Runtime":
                return setup;
            case "CompileInPlace":
                setup.CompileInPlace();
                return setup;
            case "Compile":
                setup.CompileAndVerify("Issue987_" + mode);
                return setup;
            default:
                throw new ArgumentException("Unknown mode: " + mode, nameof(mode));
        }
    }

    private static void CanRoundTripFromRoot<T>(T packet, string expectedHex, string mode) where T : IPacket
    {
        Assert.Equal("abc", packet.MessageValue);
        var model = GetModel(mode);

        using var ms = new MemoryStream();
        model.Serialize<IPacket>(ms, packet);
        if (!ms.TryGetBuffer(out var segment)) segment = new(ms.ToArray());
        var hex = BitConverter.ToString(segment.Array, segment.Offset, segment.Count);
        Assert.Equal(expectedHex, hex);

        ms.Position = 0;
        var clone = Assert.IsType<T>(model.Deserialize<IPacket>(ms));
        Assert.Equal("abc", clone.MessageValue);
    }

    private static void CanRoundTripFromLeaf<T>(T packet, string expectedHex, string mode) where T : IPacket
    {
        Assert.Equal("abc", packet.MessageValue);
        var model = GetModel(mode);

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
