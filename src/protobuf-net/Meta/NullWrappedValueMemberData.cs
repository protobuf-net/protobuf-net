using System.Collections.Generic;

namespace ProtoBuf.Meta
{
    /// <remarks>Uniquessness of <see cref="NullWrappedValueMemberData"/> is based upon schemaTypeName data only</remarks>
    internal struct NullWrappedValueMemberData
    {
        public ValueMember ValueMember { get; }
        public string OriginalSchemaTypeName { get; }
        public string WrappedSchemaTypeName => "Wrapped" + OriginalSchemaTypeName;        

        public NullWrappedValueMemberData(ValueMember valueMember, string originalSchemaTypeName)
        {
            ValueMember = valueMember;
            OriginalSchemaTypeName = originalSchemaTypeName;
        }

        public override bool Equals(object obj)
        {
            return obj is NullWrappedValueMemberData data &&
                   OriginalSchemaTypeName == data.OriginalSchemaTypeName;
        }

        public override int GetHashCode()
        {
            return 693692870 + EqualityComparer<string>.Default.GetHashCode(OriginalSchemaTypeName);
        }
    }
}
