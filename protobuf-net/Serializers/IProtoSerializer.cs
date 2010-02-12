using System;


namespace ProtoBuf.Serializers
{
    interface IProtoSerializer
    {
        /// <summary>
        /// The type that this serializer is intended to work for.
        /// </summary>
        Type ExpectedType { get; }
        /// <summary>
        /// Perform (at runtime) the steps necessary to serialize this data.
        /// </summary>
        /// <param name="value">The value to be serialized.</param>
        /// <param name="dest">The writer entity that is accumulating the output data.</param>
        void Write(object value, ProtoWriter dest);

#if FEAT_COMPILER
        /// <summary>Emit the IL necessary to perform the given actions
        /// to serialize this data.
        /// </summary>
        /// <param name="ctx">Details and utilities for the method being generated.</param>
        /// <param name="valueFrom">The source of the data to work against;
        /// If the value is only needed once, then LoadValue is sufficient. If
        /// the value is needed multiple times, then note that this might
        /// mean "the top of the stack", in which case you should create your
        /// own copy - GetLocalWithValue.</param>
        void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom);
#endif
    }
}
