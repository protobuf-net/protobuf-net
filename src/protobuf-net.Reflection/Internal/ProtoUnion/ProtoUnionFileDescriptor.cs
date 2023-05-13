using System.Collections.Generic;

namespace ProtoBuf.Internal.ProtoUnion
{
    internal sealed class ProtoUnionFileDescriptor
    {
        public string Filename { get; set; }
        public string Class { get; set; }
        public string Namespace { get; set; }
        public ICollection<ProtoUnionField> UnionFields { get; set; }
    }
}