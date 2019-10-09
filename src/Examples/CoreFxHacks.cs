using ProtoBuf.Meta;
using System;
using System.IO;
using System.Reflection;

#if COREFX
namespace ProtoBuf
{
    static class CoreFxHacks
    {
#pragma warning disable IDE0060
        public static byte[] GetBuffer(this MemoryStream ms)
        {
            if (ms.TryGetBuffer(out var segment) && segment.Offset == 0) return segment.Array;

            return ms.ToArray();
        }
        public static TypeModel Compile(this RuntimeTypeModel model, string x, string y)
        {
            return model.Compile();
        }
        public static bool IsSubclassOf(this Type x, Type y)
        {
            return x.GetTypeInfo().IsSubclassOf(y);
        }
        public static void Close(this Stream s) { }
#pragma warning restore IDE0060
    }
}
#endif