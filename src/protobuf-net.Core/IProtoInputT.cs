using ProtoBuf.Internal;
using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf
{
    /// <summary>
    /// Represents the ability to deserialize values from an input of type <typeparamref name="TInput"/>
    /// </summary>
    /// <remarks>
    /// <typeparamref name="TInput"/> is the <em>transport</em> - <see cref="System.IO.Stream"/>,
    /// <c>ReadOnlySequence&lt;byte&gt;</c>, and so on - and nothing ever reflects over it, so it
    /// carries no <see cref="DynamicallyAccessedMembersAttribute"/>. It reads like a contract type
    /// parameter and is not one; the contract is <c>T</c> on <see cref="Deserialize"/>.
    /// </remarks>
    public interface IProtoInput<TInput>
    {
        /// <summary>
        /// Deserialize a value from the input
        /// </summary>
        T Deserialize<T>(TInput source, T value = default, object userState = null);
    }
}
