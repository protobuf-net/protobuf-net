using System.Collections.Generic;

namespace ProtoBuf.Internal.ProtoUnion
{
    internal sealed class ProtoUnionClassDescriptor
    {
        public string ClassName { get; set; }
        public string Namespace { get; set; }
        public ICollection<ProtoUnionField> UnionFields { get; set; }
    }
}