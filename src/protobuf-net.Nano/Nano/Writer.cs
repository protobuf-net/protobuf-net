using System;

namespace ProtoBuf.Nano;

/// <summary>
/// Raw API for formatting protobuf data
/// </summary>
public ref partial struct Writer
{
    public static uint MeasureVarint(uint value) => throw new NotImplementedException();
    public static uint MeasureWithLengthPrefix(string value) => throw new NotImplementedException();

    public static uint MeasureWithLengthPrefix(ulong value) => throw new NotImplementedException();

    public void WriteVarint(ulong value) => throw new NotImplementedException();
    public void WriteWithLengthPrefix(string value) => throw new NotImplementedException();
    public void WriteWithLengthPrefix(byte[] value) => throw new NotImplementedException();
}
