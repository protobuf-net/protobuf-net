using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ProtoBuf.Nano.Internal;

internal static class PlatformShims
{
#if !NETCOREAPP3_1_OR_GREATER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetByteCount(this UTF8Encoding encoding, ReadOnlySpan<char> value)
    {
        fixed (char* cPtr = value)
        {
            return encoding.GetByteCount(cPtr, value.Length);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetBytes(this UTF8Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
    {
        fixed (char* cPtr = chars)
        fixed (byte* bPtr = bytes)
        {
            return encoding.GetBytes(cPtr, chars.Length, bPtr, bytes.Length);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetChars(this UTF8Encoding encoding, ReadOnlySpan<byte> bytes, Span<char> chars)
    {
        fixed (byte* bPtr = bytes)
        fixed (char* cPtr = chars)
        {
            return encoding.GetChars(bPtr, bytes.Length, cPtr, chars.Length);
        }
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetCharCount(this UTF8Encoding encoding, ReadOnlySpan<byte> bytes)
    {
        fixed (byte* bPtr = bytes)
        {
            return encoding.GetCharCount(bPtr, bytes.Length);
        }
    }
#endif
}
