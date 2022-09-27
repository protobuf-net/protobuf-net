using System;
using System.Runtime.CompilerServices;

namespace ProtoBuf.Nano;

/// <summary>
/// Raw API for parsing protobuf data
/// </summary>
public ref partial struct Reader
{
    public uint ReadTag() => throw new NotImplementedException();
    public bool TryReadTag(uint tag) => throw new NotImplementedException();

    public string ReadString() => throw new NotImplementedException();
    public byte[] ReadBytes() => throw new NotImplementedException();

    public void Skip(uint tag) => throw new NotImplementedException();

    public uint ReadFixed32UInt32() => throw new NotImplementedException();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int ReadFixed32Int32() => unchecked((int)ReadFixed32UInt32());
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public long ReadFixed32Int64() => (long)ReadFixed32Int32();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public ulong ReadFixed32UInt64() => (ulong)ReadFixed32UInt32();

    public uint ReadVarintUInt32() => throw new NotImplementedException();
    public ulong ReadVarintUInt64() => throw new NotImplementedException();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int ReadVarintInt64() => unchecked((int)ReadVarintUInt64());
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int ReadVarintInt32() => unchecked((int)ReadVarintUInt32());


    public ulong ReadFixed64UInt64() => throw new NotImplementedException();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public uint ReadFixed64UInt32() => checked((uint)ReadFixed64UInt64());
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public long ReadFixed64Int64() => unchecked((long)ReadFixed64UInt64());
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public int ReadFixed64Int32() => checked((int)unchecked((long)ReadFixed64UInt64()));


    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool ReadVarintBoolean() => ReadVarintUInt32() != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool ReadFixed32Boolean() => ReadFixed32UInt32() != 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public bool ReadFixed64Boolean() => ReadFixed64UInt32() != 0;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadFixed32Single()
    {
        var val = ReadFixed32UInt32();
        return Unsafe.As<uint, float>(ref val);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadFixed64Double()
    {
        var val = ReadFixed64UInt64();
        return Unsafe.As<ulong, double>(ref val);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public double ReadFixed32Double() => (double)ReadFixed32Single();
    [MethodImpl(MethodImplOptions.AggressiveInlining)] public float ReadFixed64Single() => (float)ReadFixed64Double();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadVarintInt32Signed()
    {
        const int Int32Msb = 1 << 31;

        int value = unchecked((int)ReadVarintUInt32());
        return (-(value & 0x01)) ^ ((value >> 1) & ~Int32Msb);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadVarintInt64Signed()
    {
        const long Int64Msb = 1L << 63;

        long value = unchecked((long)ReadVarintUInt64());
        return (-(value & 0x01L)) ^ ((value >> 1) & ~Int64Msb);
    }

    uint _lastGroup;

    public ulong Position => throw new NotImplementedException();

    public void PushGroup(uint tag)
    {
        var group = tag >> 3;
        if ((tag & 7) != 4 || group <= 0) throw new ArgumentOutOfRangeException(nameof(tag));
        if (_lastGroup != 0) throw new InvalidOperationException($"Group {_lastGroup} was already being terminated, while terminating {group}");
        _lastGroup = group;
    }
    public void PopGroup(uint group)
    {
        if (group < 0) throw new ArgumentOutOfRangeException(nameof(group));
        if (_lastGroup != group) throw new InvalidOperationException($"While terminating group {group}, group {_lastGroup} was found instead");
        _lastGroup = 0;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ThrowUnhandledWireType(uint tag)
    => throw new NotSupportedException($"Field {tag >> 3} was not expected with wire-type {tag & 7}; this may indicate a tooling error - please report as an issue");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ThrowNetObjectProxy()
        => throw new NotSupportedException("dynamic types/reference-tracking is not supported");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ThrowUnhandledTag(uint tag)
        => throw new InvalidOperationException($"Field {tag >> 3} was not expected with wire-type {tag & 7}; this may indicate a tooling error - please report as an issue");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ThrowInvalidPackedLength(uint tag, ulong len)
    => throw new InvalidOperationException($"Field {tag >> 3} has invalid packed-length {len}, which is an incomplete number of elements");

    public ulong ConstrainByLengthPrefix() => throw new InvalidOperationException();

    public void Unconstrain(ulong oldEnd) => throw new NotImplementedException();
}
