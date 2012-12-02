#if !(CF || SILVERLIGHT)
using System;
using System.Collections.Generic;

namespace ProtoBuf.Caching
{
    /// <summary>
    /// A comparer (for dictionary use) that can compare segments of buffers; the
    /// intent being to avoid having to encode/decode strings
    /// </summary>
    /// <remarks>It is the responsibility of the consuming code not to mutate
    /// the byte[] in a dictionary</remarks>
    internal sealed class ByteSegmentComparer : IEqualityComparer<ArraySegment<byte>>
    {
        bool IEqualityComparer<ArraySegment<byte>>.Equals(ArraySegment<byte> x, ArraySegment<byte> y)
        {
            if (x.Count != y.Count) return false;
            byte[] xBuf = x.Array, yBuf = y.Array;
            int xOffset = x.Offset, yOffset = y.Offset, xMax = xOffset + x.Count;
            while (xOffset < xMax)
            {
                if (xBuf[xOffset++] != yBuf[yOffset++]) return false;
            }
            return true;
        }

        int IEqualityComparer<ArraySegment<byte>>.GetHashCode(ArraySegment<byte> segment)
        {
            byte[] buffer = segment.Array;
            int result = -1623343517;
            int offset = segment.Offset, max = offset + segment.Count;
            while (offset < max)
            {
                result = (-1521134295 * result) + (int)buffer[offset++];
            }
            return result;
        }
    }
}
#endif