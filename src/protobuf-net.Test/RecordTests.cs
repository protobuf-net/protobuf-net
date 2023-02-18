using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace ProtoBuf.Test;

public class RecordTests
{
    [Theory]
    [InlineData(typeof(ReadWriteClassRecord))]
    [InlineData(typeof(ReadWriteStructRecord))]
    [InlineData(typeof(ReadOnlyStructRecord))]
    public void TestSchema(Type type)
    {
        var schema = RuntimeTypeModel.Default.GetSchema(type);
        Assert.Equal($@"syntax = ""proto3"";
package ProtoBuf.Test;

message {type.Name} {{
   int32 Id = 1;
   string Name = 2;
}}
", schema, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void TestRoundTrip_ReadOnlyStructRecord() => TestRoundTrip(new ReadOnlyStructRecord(12, "abc"));

    [Fact]
    public void TestRoundTrip_ReadWriteStructRecord() => TestRoundTrip(new ReadWriteStructRecord(12, "abc"));

    [Fact]
    public void TestRoundTrip_ReadWriteClassRecord() => TestRoundTrip(new ReadWriteClassRecord(12, "abc"));

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Assertions", "xUnit2005:Do not use identity check on value type", Justification = "Ref-type check included")]
    private void TestRoundTrip<T>(T value) where T : IRecord
    {
        Assert.Equal(12, value.Id);
        Assert.Equal("abc", value.Name);

        using var ms = new MemoryStream();
        Serializer.Serialize(ms, value);
        if (!ms.TryGetBuffer(out var buffer)) buffer = new(ms.ToArray());
        var hex = BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count);
        Assert.Equal("08-0C-12-03-61-62-63", hex);

        ms.Position = 0;
        var clone = Serializer.Deserialize<T>(ms);
        if (!typeof(T).IsValueType)
        {
            Assert.NotSame(value, clone);
        }
        Assert.Equal(12, clone.Id);
        Assert.Equal("abc", clone.Name);
    }

    public interface IRecord
    {
        int Id { get; }
        string Name { get; }
    }

    record class ReadWriteClassRecord(int Id, string Name) : IRecord;
    record struct ReadWriteStructRecord(int Id, string Name) : IRecord;
    readonly record struct ReadOnlyStructRecord(int Id, string Name) : IRecord;
}
