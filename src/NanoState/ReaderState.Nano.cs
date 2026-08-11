using System;

namespace ProtoBuf.Nano;

// The NEW surface - hand-written, beside the generated compatibility floor. The existing API is
// reimplemented over these primitives; the generator emits against them directly.
public ref partial struct ReaderState
{
    /// <summary>
    /// Reads the next field tag as its raw wire value - the tag varint as-is, field number and
    /// wire type still joined - or 0 at the end of the current message. Generated code dispatches
    /// on compile-time constants (for example <c>case (2 &lt;&lt; 3) | (int)WireType.String:</c>),
    /// so no decomposition happens and no state is written; the legacy
    /// <c>ReadFieldHeader()</c>/<c>WireType</c> pair becomes a shift-and-mask veneer over this.
    /// </summary>
    public uint ReadTag()
        => throw new NotImplementedException();

    /// <summary>
    /// Consumes the next tag only if it is exactly <paramref name="tag"/> - the fields-in-order
    /// fast path: a serializer that just read field n speculates that field n+1 comes next and
    /// skips the dispatch entirely when it is right.
    /// </summary>
    public bool TryReadTag(uint tag)
        => throw new NotImplementedException();

    /// <summary>
    /// Skips the field whose raw tag was just read - the untyped counterpart of the legacy
    /// <c>SkipField()</c>, taking the wire type from the tag's low bits rather than from state.
    /// </summary>
    public void SkipTag(uint tag)
        => throw new NotImplementedException();
}
