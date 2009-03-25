
namespace ProtoBuf
{
    /// <summary>
    /// Represents the function to obtain the return value from an asynchronouse operation;
    /// comparable to Func&lt;object&gt;.
    /// </summary>
    public delegate object AsyncResult();
    /// <summary>
    /// Returns the required value from an instance; comparable to Func&lt;TEntity,TValue&gt;
    /// </summary>
    public delegate TValue Getter<TEntity, TValue>(TEntity instance);
    /// <summary>
    /// Assigns the required value to an instance; comparable to Action&lt;TEntity,TValue&gt;.
    /// </summary>
    public delegate void Setter<TEntity, TValue>(TEntity instance, TValue value);
}
