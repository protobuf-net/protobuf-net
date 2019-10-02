using System;
using System.Text;

namespace ProtoBuf
{
    /// <summary>
    /// Not yet implemented
    /// </summary>
    public sealed class StringMap
    {
        //        // TODO: 
        //        public bool TryGetValue(string value, out ReadOnlyMemory<byte> bytes)
        //        {
        //            bytes = default;
        //            return false;
        //        }
        //        public bool TryGetValue(ReadOnlyMemory<byte> bytes, out string value)
        //        {
        //            value = default;
        //            return false;
        //        }

        //        public string GetValue(ReadOnlyMemory<byte> bytes)
        //        {
        //            if (bytes.IsEmpty) return "";
        //            if (TryGetValue(bytes, out var s)) return s;
        //#if PLAT_SPAN_OVERLOADS
        //            return ProtoWriter.UTF8.GetString(bytes.Span);
        //#else
        //            unsafe
        //            {
        //                fixed (byte* ptr = bytes.Span)
        //                {
        //                    return ProtoWriter.UTF8.GetString(ptr, bytes.Length);
        //                }
        //            }
        //#endif
        //        }
    }
}
