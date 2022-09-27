using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Nano;

/// <summary>
/// Raw API for formatting protobuf data
/// </summary>
public ref partial struct Writer
{
    public static uint MeasureVarint(int value) => throw new NotImplementedException();
    public static uint MeasureVarint(long value) => throw new NotImplementedException();
    public static uint MeasureVarint(uint value) => throw new NotImplementedException();
    public static uint MeasureVarint(ulong value) => throw new NotImplementedException();
    public static uint MeasureWithLengthPrefix(string value) => throw new NotImplementedException();
    public static uint MeasureWithLengthPrefix(byte[] value) => throw new NotImplementedException();
    public static ulong MeasureWithLengthPrefix(ulong value) => MeasureVarint(value) + value;

    public void WriteVarint(ulong value) => throw new NotImplementedException();
    public void WriteVarint(long value) => throw new NotImplementedException();
    public void WriteVarint(uint value) => throw new NotImplementedException();
    public void WriteVarint(int value) => throw new NotImplementedException();
    public void WriteVarint(bool value) => throw new NotImplementedException();

    public void WriteWithLengthPrefix(string value) => throw new NotImplementedException();
    public void WriteWithLengthPrefix(byte[] value) => throw new NotImplementedException();

    public void WriteFixed32(uint value) => throw new NotImplementedException();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void WriteFixed32(int value) => WriteFixed32(unchecked((uint)value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void WriteFixed32(float value) => WriteFixed32(Unsafe.As<float, uint>(ref value));

    public void WriteFixed64(ulong value) => throw new NotImplementedException();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void WriteFixed64(long value) => WriteFixed64(unchecked((ulong)value));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void WriteFixed64(double value) => WriteFixed64(Unsafe.As<double, ulong>(ref value));


    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void WriteVarintSigned(int value) => WriteVarint((uint)((value << 1) ^ (value >> 31)));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public void WriteVarintSigned(long value) => WriteVarint((ulong)((value << 1) ^ (value >> 63)));

    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint MeasureVarintSigned(int value) => MeasureVarint((uint)((value << 1) ^ (value >> 31)));
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public static uint MeasureVarintSigned(long value) => MeasureVarint((ulong)((value << 1) ^ (value >> 63)));
}
