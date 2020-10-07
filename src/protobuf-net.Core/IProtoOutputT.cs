using ProtoBuf.Internal;
using System.Diagnostics.CodeAnalysis;

namespace ProtoBuf
{
    /// <summary>
    /// Represents the ability to serialize values to an output of type <typeparamref name="TOutput"/>
    /// </summary>
    public interface IProtoOutput<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TOutput>
    {
        /// <summary>
        /// Serialize the provided value
        /// </summary>
        void Serialize<T>(TOutput destination, T value, object userState = null);
    }

    /// <summary>
    /// Represents the ability to serialize values to an output of type <typeparamref name="TOutput"/>
    /// with pre-computation of the length
    /// </summary>
    public interface IMeasuredProtoOutput<[DynamicallyAccessedMembers(DynamicAccess.ContractType)] TOutput> : IProtoOutput<TOutput>
    {
        /// <summary>
        /// Measure the length of a value in advance of serialization
        /// </summary>
        MeasureState<T> Measure<T>(T value, object userState = null);

        /// <summary>
        /// Serialize the previously measured value
        /// </summary>
        void Serialize<T>(MeasureState<T> measured, TOutput destination);
    }
}
