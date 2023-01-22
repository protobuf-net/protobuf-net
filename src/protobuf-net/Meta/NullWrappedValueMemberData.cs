namespace ProtoBuf.Meta
{
    internal class NullWrappedValueMemberData
    {
        public ValueMember ValueMember { get; }
        public string OriginalSchemaTypeName { get; }
        public string WrappedSchemaTypeName => "Wrapped" + OriginalSchemaTypeName;
        public bool ContainsSchemaTypeNameCollision { get; private set; } = false;

        public NullWrappedValueMemberData(ValueMember valueMember, string typeName)
        {
            ValueMember = valueMember;
            OriginalSchemaTypeName = typeName;
        }

        /// <summary>
        /// Requires `group` to be placed on original valueMember level
        /// </summary>
        public bool HasGroupModifier => ValueMember.SupportNull || ValueMember.NullWrappedValueGroup;

        public void SetContainsSchemaTypeNameCollision() => ContainsSchemaTypeNameCollision = true;

        /// <summary>
        /// Identifies, if <see cref="OriginalSchemaTypeName"/> is a known .net type
        /// </summary>
        /// <returns>
        /// true, if <see cref="OriginalSchemaTypeName"/> is a string representation of known System.XXX type 
        /// </returns>
        public bool HasKnownTypeSchema()
        {
            switch (OriginalSchemaTypeName)
            {
                case "int32":
                case "uint32":
                case "int64":
                case "uint64":
                case "double":
                case "bool":
                case "string":
                    return true;

                default: 
                    return false;
            }
        }
    }
}
