using System;
using System.Collections.Generic;
using System.Text;
using ProtoBuf.Property;

namespace ProtoBuf
{
    public static partial class Serializer
    {
        /// <summary>
        /// Global switches that change the behavior of protobuf-net
        /// </summary>
        public static class GlobalOptions
        {
            private static bool inferTagFromName;
            /// <summary>
            /// Global default for that
            /// enables/disables automatic tag generation based on the existing name / order
            /// of the defined members. See <seealso cref="ProtoContractAttribute.InferTagFromName"/>
            /// for usage and <b>important warning</b> / explanation.
            /// You must set the global default before attempting to serialize/deserialize any
            /// impacted type.
            /// </summary>
            public static bool InferTagFromName
            {
                get { return inferTagFromName; }
                set { inferTagFromName = value; }
            }
        }
        
        internal static bool HasAddMethod(Type list, Type item)
        {
            return list.GetMethod("Add", new Type[] { item }) != null;
        }
    }
}
