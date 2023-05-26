namespace ProtoBuf.Internal.ProtoUnion
{
    public sealed class ProtoUnionField
    {
        /// <summary>
        /// Type as used in a code. For example 'int', 'bool', etc
        /// </summary>
        public string CSharpType { get; }
        /// <summary>
        /// Name of corresponding union usage from <see cref="DiscriminatedUnion32"/> or analogous discriminated union
        /// </summary>
        public string UnionUsageFieldName { get; }
        /// <summary>
        /// Specifies if property is reference\value and size (if is value type)
        /// </summary>
        public PropertyUnionType UnionType { get; }

        /// <summary>
        /// Name of a union specified by user
        /// </summary>
        public string UnionName { get; }
        /// <summary>
        /// Proto member number 
        /// </summary>
        public int FieldNumber { get; }
        /// <summary>
        /// Name of a generated property
        /// </summary>
        public string MemberName { get; }
        
        public ProtoUnionField(string unionName, int fieldNumber, string memberName, PropertyUnionType unionType, string unionUsageFieldName, string cSharpType)
        {
            MemberName = memberName;
            FieldNumber = fieldNumber;
            UnionName = unionName;
            UnionType = unionType;
            UnionUsageFieldName = unionUsageFieldName;
            CSharpType = cSharpType;
        }

        public enum PropertyUnionType
        {
            Is32,
            Is64,
            Is128,
            Reference
        }
    }
}