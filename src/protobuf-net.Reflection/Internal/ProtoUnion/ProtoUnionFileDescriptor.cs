using System.Collections.Generic;

namespace ProtoBuf.Internal.ProtoUnion
{
    internal sealed record ProtoUnionFileDescriptor
    {
        public string Filename { get; }
        public string Class { get; }
        public string Namespace { get; }
        public ICollection<ProtoUnionField> UnionFields { get; }
        public IReadOnlyDictionary<string, DiscriminatedUnionType> UnionTypes { get; }

        public ProtoUnionFileDescriptor(
            string filename,
            string @class,
            string @namespace,
            ICollection<ProtoUnionField> unionFields,
            IReadOnlyDictionary<string, DiscriminatedUnionType> unionTypes)
        {
            Filename = filename;
            Class = @class;
            Namespace = @namespace;
            UnionFields = unionFields;
            UnionTypes = unionTypes;
        }
    }
}