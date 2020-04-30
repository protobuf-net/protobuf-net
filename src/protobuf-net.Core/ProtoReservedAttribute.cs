using System;

namespace ProtoBuf
{
    /// <summary>
    /// Indicates a reserved field or range
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, AllowMultiple = true, Inherited = false)]
    public sealed class ProtoReservedAttribute : Attribute
    {
        /// <summary>
        /// The start of a numeric field range
        /// </summary>
        public int From { get; }
        /// <summary>
        /// The end of a numeric field range
        /// </summary>
        public int To { get; }
        /// <summary>
        /// A named field reservation
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Creates a new instance of a single number field reservation
        /// </summary>
        public ProtoReservedAttribute(int field, string comment = null) : this(field, field, comment) { }
        /// <summary>
        /// Creates a new instance of a range number field reservation
        /// </summary>
        public ProtoReservedAttribute(int from, int to, string comment = null)
        {
            From = from;
            To = to;
            Comment = comment;
        }
        /// <summary>
        /// Records a comment explaining this reservation
        /// </summary>
        public string Comment { get; }
        /// <summary>
        /// Creates a new instance of a named field reservation
        /// </summary>
        public ProtoReservedAttribute(string field, string comment = null)
        {
            Name = field;
            Comment = comment;
        }

        internal void Verify()
        {
            const string Message = "Invalid reservation definition";
            if (string.IsNullOrWhiteSpace(Name))
            {
                if (From <= 0) throw new ArgumentOutOfRangeException(nameof(From), Message);
                if (To < From) throw new ArgumentOutOfRangeException(nameof(To), Message);
            }
        }
    }
}
