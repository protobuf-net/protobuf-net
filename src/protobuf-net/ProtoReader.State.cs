using System;

namespace ProtoBuf
{
    public partial class ProtoReader
    {
        /// <summary>
        /// Holds state used by the deserializer
        /// </summary>
        public ref struct State
        {
            internal static readonly Type ByRefType = typeof(State).MakeByRefType();
            internal static readonly Type[] ByRefTypeArray = new[] { ByRefType };
            internal SolidState Solidify() => default;
        }

        internal struct SolidState
        {
            internal State Liquify() => default;
        }
    }
}
