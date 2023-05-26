using System.Collections.Generic;

namespace ProtoBuf.Internal.ProtoUnion
{
    internal sealed record ProtoUnionFileDescriptor(
        string Filename,
        string Class,
        string Namespace,
        string UnionName,
        DiscriminatedUnionType UnionType,
        ICollection<ProtoUnionField> UnionFields)
    {
        public string Filename { get; } = Filename;
        public string Class { get; } = Class;
        public string Namespace { get; } = Namespace;

        public string UnionName { get; } = UnionName;
        public DiscriminatedUnionType UnionType { get; } = UnionType;

        public ICollection<ProtoUnionField> UnionFields { get; } = UnionFields;
    }
}