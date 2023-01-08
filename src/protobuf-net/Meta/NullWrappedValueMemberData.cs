namespace ProtoBuf.Meta
{
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
    }
}
