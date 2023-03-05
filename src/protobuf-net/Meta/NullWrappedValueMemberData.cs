using System;

namespace ProtoBuf.Meta
{
    internal class NullWrappedValueMemberData
    {
        private readonly string _originalSchemaTypeName;
        private readonly string _alternativeTypeName;
        private readonly bool _hasSchemaTypeNameCollision;
        private readonly ValueMember _valueMember;

        public NullWrappedValueMemberData(
            ValueMember valueMember,
            string originalSchemaTypeName,
            string alternativeTypeName = null,
            bool hasSchemaTypeNameCollision = false)
        {
            _originalSchemaTypeName = originalSchemaTypeName;
            _alternativeTypeName = alternativeTypeName;
            _hasSchemaTypeNameCollision = hasSchemaTypeNameCollision;
            _valueMember = valueMember;
        }

        public string SchemaTypeName => _originalSchemaTypeName;
        public string WrappedSchemaTypeName
        {
            get
            {
                var typeName = !string.IsNullOrEmpty(_alternativeTypeName)
                    ? _alternativeTypeName
                    : _originalSchemaTypeName;

                if (_valueMember.SupportNull) return "WrappedAsSupportNull" + typeName;
                if (_valueMember.NullWrappedValueGroup) return "WrappedAsGroup" + typeName;
                return "Wrapped" + typeName;
            }
        }

        /// <summary>
        /// Calculates, if valueMember has a schemaTypeName collision
        /// </summary>
        public bool HasSchemaTypeNameCollision => _hasSchemaTypeNameCollision && !HasKnownTypeSchema();

        /// <summary>
        /// Gets inner valueMember's item type
        /// </summary>
        public Type ItemType => _valueMember.ItemType;

        /// <summary>
        /// Requires `group` to be placed on original valueMember level
        /// </summary>
        public bool HasGroupModifier => _valueMember.RequiresGroupModifier;

        /// <summary>
        /// Identifies, if original schemaTypeName is a known .net type
        /// </summary>
        /// <returns>
        /// true, if original schemaTypeName is a string representation of known System.XXX type 
        /// </returns>
        private bool HasKnownTypeSchema() => _originalSchemaTypeName switch
        {
            "int32" or "uint32" or "int64" or "uint64" or "double" or "bool" or "string" => true,
            _ => false
        };
    }
}
