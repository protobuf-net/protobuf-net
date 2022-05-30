namespace ProtoBuf.Nano;

/// <summary>
/// Describes the capabilities for parsing and formatting protobuf messages
/// </summary>
public interface INanoSerializer<T>
{
    /// <summary>
    /// Parse a message
    /// </summary>
    T Read(ref Reader reader);
    /// <summary>
    /// Measure a message
    /// </summary>
    long Measure(in T value);
    /// <summary>
    /// Write a message
    /// </summary>
    void Write(in T value, ref Writer writer);
}
