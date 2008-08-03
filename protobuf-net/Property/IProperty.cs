using System.Reflection;
using System;

namespace ProtoBuf
{
    internal interface IProperty<TEntity>
    {
        string Name { get; }
        DataFormat DataFormat { get; }
        bool IsRequired { get; }
        bool IsGroup { get; }
        bool CanBeGroup { get; }

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
    }
}
