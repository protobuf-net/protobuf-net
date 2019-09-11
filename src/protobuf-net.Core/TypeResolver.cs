using System;

namespace ProtoBuf
{
    /// <summary>
    /// Maps a field-number to a type
    /// </summary>
    public delegate Type TypeResolver(int fieldNumber);
}
