using System;

namespace ProtoBuf
{
    /// <summary>
    /// Represents a member of a discriminated union.
    /// </summary>
    /// <typeparam name="T">type of discriminated union member.</typeparam>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class ProtoUnionAttribute<T> : Attribute
    {
        /// <summary>
        /// Declares a member of a discriminated union.
        /// </summary>
        /// <param name="unionName">name of discriminated union</param>
        /// <param name="fieldNumber">unique (per class) fieldNumber used for <see cref="ProtoMemberAttribute"> definition</param>
        /// <param name="memberName">name of discriminated union member.</param>
        public ProtoUnionAttribute(string unionName, int fieldNumber, string memberName)
        {
        }
    }
}
