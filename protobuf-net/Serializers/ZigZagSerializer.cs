using System;
using System.Collections.Generic;
using System.Text;

namespace ProtoBuf
{
    internal sealed partial class ZigZagSerializer : ISerializer<int>, ISerializer<long>
    {
        public static readonly ZigZagSerializer Default = new ZigZagSerializer();
        private ZigZagSerializer() { }

        public WireType WireType { get { return WireType.Variant; } }
    }
}
