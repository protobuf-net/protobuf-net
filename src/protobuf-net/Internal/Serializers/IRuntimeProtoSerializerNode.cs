using System;

namespace ProtoBuf.Internal.Serializers
{
    internal interface IRuntimeProtoSerializerNode
    {
        /// <summary>
        /// Does this represent a scalar type?
        /// </summary>
        bool IsScalar { get; }

        /// <summary>
        /// The type that this serializer is intended to work for.
        /// </summary>
        Type ExpectedType { get; }

        /// <summary>
        /// Perform the steps necessary to serialize this data.
        /// </summary>
        /// <param name="value">The value to be serialized.</param>
        /// <param name="state">Writer state</param>
        void Write(ref ProtoWriter.State state, object value);

        /// <summary>
        /// Perform the steps necessary to deserialize this data.
        /// </summary>
        /// <param name="value">The current value, if appropriate.</param>
        /// <param name="state">Reader state</param>
        /// <returns>The updated / replacement value.</returns>
        object Read(ref ProtoReader.State state, object value);

        /// <summary>
        /// Indicates whether a Read operation <em>replaces</em> the existing value, or
        /// <em>extends</em> the value. If false, the "value" parameter to Read is
        /// discarded, and should be passed in as null.
        /// </summary>
        bool RequiresOldValue { get; }
        /// <summary>
        /// Not all Read operations return a value (although most do); if false no
        /// value should be expected.
        /// </summary>
        bool ReturnsValue { get; }

        /// <summary>Emit the IL necessary to perform the given actions
        /// to serialize this data.
        /// </summary>
        /// <param name="ctx">Details and utilities for the method being generated.</param>
        /// <param name="valueFrom">The source of the data to work against;
        /// If the value is only needed once, then LoadValue is sufficient. If
        /// the value is needed multiple times, then note that a "null"
        /// means "the top of the stack", in which case you should create your
        /// own copy - GetLocalWithValue.</param>
        void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom);

        /// <summary>
        /// Emit the IL necessary to perform the given actions to deserialize this data.
        /// </summary>
        /// <param name="ctx">Details and utilities for the method being generated.</param>
        /// <param name="entity">For nested values, the instance holding the values; note
        /// that this is not always provided - a null means not supplied. Since this is always
        /// a variable or argument, it is not necessary to consume this value.</param>
        void EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity);
    }
}