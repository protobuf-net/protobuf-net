using System.Reflection;

namespace ProtoBuf
{
    interface IProperty<TEntity>
    {
        string Name { get; }
        DataFormat DataFormat { get; }
        bool IsRequired { get; }
        int Tag { get; }
        string Description { get; }
        object DefaultValue { get; }

        void Deserialize(TEntity instance, SerializationContext context);
        int Serialize(TEntity instance, SerializationContext context);
        string DefinedType { get; }
        WireType WireType { get; }

        int GetLength(TEntity instance, SerializationContext context);
    }
}
