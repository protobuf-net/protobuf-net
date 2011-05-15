#if !NO_RUNTIME
using System;
using ProtoBuf.Meta;

namespace ProtoBuf
{
    public static partial class Serializer
    {
        /// <summary>
        /// Global switches that change the behavior of protobuf-net
        /// </summary>
        public static class GlobalOptions
        {
            /// <summary>
            /// <see cref="RuntimeTypeModel.InferTagFromNameDefault"/>
            /// </summary>
            [Obsolete("Please use RuntimeTypeModel.Default.InferTagFromNameDefault instead (or on a per-model basis)", false)]
            public static bool InferTagFromName
            {
                get { return RuntimeTypeModel.Default.InferTagFromNameDefault; }
                set { RuntimeTypeModel.Default.InferTagFromNameDefault = value; }
            }
        }
    }
}
#endif