namespace ProtoBuf.Internal
{
    /// <summary>
    /// Implemented by a context that exists to COUNT bytes rather than to emit them, so that a
    /// consumer callback can tell a measuring pass from the real write.
    /// </summary>
    /// <remarks>
    /// <see cref="ProtoWriter.IsMeasuring(ISerializationContext)"/> asks "is this a measuring
    /// pass", not "is this one particular writer", and the two answers now come from different
    /// places: the classic backend measures by <b>being</b> a counting writer, so it overrides
    /// <c>ProtoWriter.IsMeasuringPass</c>; the generated raw path measures arithmetically with no
    /// writer at all, so it wraps the real context in something carrying this marker. Anything
    /// added later should pick whichever of the two it actually is rather than being bolted onto
    /// a type test.
    /// </remarks>
    internal interface IMeasuringPassContext { }
}
