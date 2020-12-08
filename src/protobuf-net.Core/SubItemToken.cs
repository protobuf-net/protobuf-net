
using System;
using System.Runtime.InteropServices;

namespace ProtoBuf
{
    /// <summary>
    /// Used to hold particulars relating to nested objects. This is opaque to the caller - simply
    /// give back the token you are given at the end of an object.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
#pragma warning disable CA2231 // Overload operator equals on overriding value type Equals
    public readonly struct SubItemToken
#pragma warning restore CA2231 // Overload operator equals on overriding value type Equals
    {
        /// <summary>
        /// See object.ToString()
        /// </summary>
        public override string ToString()
        {
            if (value64 < 0) return $"Group {-value64}";
            if (value64 == long.MaxValue) return "Message (restores to end when ended)";
            return $"Message (restores to value64 when ended)";
        }

        // note: can't really display value64 - it is usually confusing, since
        // it is the *restore* value (previous), not the *current* value

        /// <summary>
        /// See object.GetHashCode()
        /// </summary>
        public override int GetHashCode() => value64.GetHashCode();
        /// <summary>
        /// See object.Equals()
        /// </summary>
        public override bool Equals(object obj) => obj is SubItemToken tok && tok.value64 == value64;
        internal readonly long value64;

        internal SubItemToken(long value) => value64 = value;
    }
}
