#nullable enable

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal enum CodeGenTypeRepresentation
{
    /// <summary>
    /// Raw type without any generic wrapper being used
    /// i.e. 'T SendAsync()'
    /// </summary>
    Raw,
    
    /// <summary>
    /// ValueTask wrapper over Raw type
    /// i.e. 'System.Threading.Tasks.ValueTask<T> SendAsync()'
    /// </summary>
    ValueTask,
    
    /// <summary>
    /// Task wrapper over Raw type
    /// i.e. 'System.Threading.Tasks.Task<T> SendAsync()'
    /// </summary>
    Task,
    
    /// <summary>
    /// IAsyncEnumerable wrapper over Raw type
    /// i.e. 'System.Collections.Generic.IAsyncEnumerable<T> SendAsync()'
    /// </summary>
    AsyncEnumerable
}
