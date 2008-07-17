using System.Reflection;

namespace ProtoBuf
{
    interface IProperty<TEntity>
    {
        int Tag { get; }
        PropertyInfo Property { get; }
        void Deserialize(TEntity instance, SerializationContext context);
        int Serialize(TEntity instance, SerializationContext context);
        string DefinedType { get; }
        WireType WireType { get; }

        int GetLength(TEntity instance, SerializationContext context);
    }
}
