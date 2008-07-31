
using System.Reflection;
namespace ProtoBuf
{
    internal sealed class ClassProperty<TEntity, TValue> : SimpleProperty<TEntity, TValue>
        where TEntity : class, new()
        where TValue : class
    {
        public ClassProperty(PropertyInfo property)
            : base(property)
        { }

        protected override bool HasValue(TValue value)
        {
            return value != null;
        }
    }
}
