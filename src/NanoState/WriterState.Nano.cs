using System;

namespace ProtoBuf.Nano;

// The NEW surface - hand-written, beside the generated compatibility floor. The existing API is
// reimplemented over these primitives; the generator emits against them directly.
public ref partial struct WriterState
{
    /// <summary>
    /// Writes a raw tag - the field number and wire type pre-joined as a compile-time constant in
    /// generated code, so the legacy <c>WriteFieldHeader(field, wireType)</c> join never happens
    /// at runtime.
    /// </summary>
    public void WriteTag(uint tag)
        => throw new NotImplementedException();

    /// <summary>
    /// Measures a varint - pure arithmetic, static, no state: the primitive that makes whole-graph
    /// measure ~10x faster than measuring by writing (see the v4 tables; lzcnt makes this
    /// effectively free).
    /// </summary>
    public static uint MeasureVarint(ulong value)
        => throw new NotImplementedException();
}
