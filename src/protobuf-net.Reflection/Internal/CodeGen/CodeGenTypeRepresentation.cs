#nullable enable

namespace ProtoBuf.Reflection.Internal.CodeGen;

internal enum CodeGenTypeRepresentation
{
    /// <summary>
    /// Raw type without any generic wrapper being used
    /// i.e. <code>T SendAsync()</code>
    /// </summary>
    Raw,

#pragma warning disable CS1574 // stop invalid cref (TFM-specific) complaining
    /// <summary>
    /// <see cref="System.Threading.Tasks.ValueTask"/> / <see cref="System.Threading.Tasks.ValueTask{T}"/> wrapper over Raw type
    /// i.e. <code>System.Threading.Tasks.ValueTask&lt;T&gt; SendAsync()</code>
    /// </summary>
    ValueTask,

    /// <summary>
    /// <see cref="System.Threading.Tasks.Task"/> / <see cref="System.Threading.Tasks.Task{T}"/> wrapper over Raw type
    /// i.e. <code>System.Threading.Tasks.Task&lt;T&gt; SendAsync()</code>
    /// </summary>
    Task,

    /// <summary>
    /// <see cref="System.Collections.Generic.IAsyncEnumerable{T}"/> wrapper over Raw type
    /// i.e. <code>System.Collections.Generic.IAsyncEnumerable&lt;T&gt; SendAsync()</code>
    /// </summary>
    AsyncEnumerable
#pragma warning restore CS1574 // stop invalid cref (TFM-specific) complaining
}
