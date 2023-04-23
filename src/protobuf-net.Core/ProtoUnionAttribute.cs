using System;

namespace ProtoBuf
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ProtoUnionAttribute : Attribute
    {
        public ProtoUnionAttribute(Type type, string unionName, int fieldNumber, string memberName)
        {
        }
    }
}
