namespace ProtoBuf.Internal.ProtoUnion
{
    /// <summary>
    /// Contains metadata for the discriminated union member.
    /// </summary>
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

        /// <summary>
        /// Represents type of discriminated union property in terms of size and referenc'ability.
        /// Is used for determining inner <see cref="DiscriminatedUnionType"/> of the corresponding union.
        /// </summary>
        public enum PropertyUnionType
        {
            /// <summary>
            /// Type of property is of 32 bit size
            /// </summary>
            Is32,

            /// <summary>
            /// Type of property is of 64 bit size
            /// </summary>
            Is64,

            /// <summary>
            /// Type of property is of 128 bit size
            /// </summary>
            Is128,

            /// <summary>
            /// Property is of reference type
            /// </summary>
            Reference
        }
    }
}