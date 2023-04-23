using System;

namespace ProtoBuf
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ProtoUnionAttribute<T> : Attribute
    {
        public ProtoUnionAttribute(string unionName, int fieldNumber, string memberName)
        {
        }
    }
}
