
using System;
using ProtoBuf.ProtoBcl;
namespace ProtoBuf.Serializers
{
    internal sealed partial class BclSerializer
    {
        private BclSerializer() { }
        public static readonly BclSerializer Default = new BclSerializer();
        public bool CanBeGroup { get { return true; } }
        public WireType WireType { get { return WireType.String; } }

    }
}
