namespace ProtoBuf.Meta
{
    internal class NullWrappedValueMemberData
    {
        private readonly string _originalSchemaTypeName;
        private readonly string _alternativeTypeName;

        public ValueMember ValueMember { get; }
        public bool ContainsSchemaTypeNameCollision { get; private set; } = false;

        public string SchemaTypeName => _originalSchemaTypeName;

        public string WrappedSchemaTypeName
            => !string.IsNullOrEmpty(_alternativeTypeName)
                ? "Wrapped" + _alternativeTypeName
                : "Wrapped" + _originalSchemaTypeName;        

        public NullWrappedValueMemberData(
            ValueMember valueMember,
            string typeName,
            string alternativeTypeName = null)
        {
            ValueMember = valueMember;
            _originalSchemaTypeName = typeName;
            _alternativeTypeName = alternativeTypeName;
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
            switch (_originalSchemaTypeName)
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
