using System.Reflection;
using System;

namespace ProtoBuf
{
    internal interface IProperty<TEntity>
    {
        string Name { get; }
        DataFormat DataFormat { get; }
        bool IsRequired { get; }
        int Tag { get; }
#if !CF
        string Description { get; }
#endif
        object DefaultValue { get; }

        void Deserialize(TEntity instance, SerializationContext context);
        int Serialize(TEntity instance, SerializationContext context);
        string DefinedType { get; }
        WireType WireType { get; }
        Type PropertyType { get; }
        bool IsRepeated { get; }

        int GetLength(TEntity instance, SerializationContext context);
    }

    /// <summary>
    /// Additional support for properties that can handle grouped (rather than length-prefixed) data (entities)
    /// </summary>
    internal interface IGroupProperty<T> : IProperty<T>
    {
        void DeserializeGroup(T instance, SerializationContext context);
    }
}
