
using System.Reflection;
namespace ProtoBuf
{
    internal sealed class StructProperty<TEntity, TValue> : SimpleProperty<TEntity, TValue>
        where TEntity : class, new()
        where TValue : struct
    {
        public StructProperty(PropertyInfo property)
            : base(property)
        { }

        protected override bool HasValue(TValue value)
        {
            return true;
        }
    }
}
