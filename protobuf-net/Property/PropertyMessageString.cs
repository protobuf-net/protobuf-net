
namespace ProtoBuf.Property
{
    /// <summary>
    /// Serializes an entity using string (length-prefixed) syntax.
    /// The high number of type arguments is requird to support ancestral serialization;
    /// there are 2 use-cases:
    ///   direct: for example, a property (base is the highest contract ancestor; prop = actual = the property-type)
    ///   descendent: used internally to cascade inheritance; prop = base = the parent type, actual = the child type
    /// </summary>
    /// <typeparam name="TSource">The type declaring the member</typeparam>
    /// <typeparam name="TProperty">The defined member-type for accessing data</typeparam>
    /// <typeparam name="TEntityBase">The base-type to use when verifying / instantiating sub-type instances</typeparam>
    /// <typeparam name="TEntityActual">The type to use for serialization purposes</typeparam>
    internal sealed class PropertyMessageString<TSource, TProperty, TEntityBase, TEntityActual> : Property<TSource, TProperty>, ILengthProperty<TEntityActual>
        where TProperty : class, TEntityBase
        where TEntityBase : class
        where TEntityActual : class, TEntityBase
    {
        public override System.Collections.Generic.IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            yield return CreateAlternative<PropertyMessageGroup<TSource, TProperty, TEntityBase, TEntityActual>>(DataFormat.Group);
        }

        public override string DefinedType
        {
            get { return Serializer.GetDefinedTypeName<TEntityBase>(); }
        }

        public override WireType WireType { get { return WireType.String; } }

        protected override void OnAfterInit()
        {
            base.OnAfterInit();
            Serializer<TEntityActual>.Build();
        }

        public override int Serialize(TSource source, SerializationContext context)
        {
            TEntityActual value = (TEntityActual) (object) GetValue(source);
            if (value == null) return 0;

            return WritePrefix(context)
                + context.WriteLengthPrefixed(value, 0, this);
        }

        public override TProperty DeserializeImpl(TSource source, SerializationContext context)
        {
            TEntityActual value = Serializer<TEntityBase>.CheckSubType<TEntityActual>(GetValue(source));

            long restore = context.LimitByLengthPrefix();

            Serializer<TEntityActual>.Deserialize<TEntityActual>(ref value, context);
            // restore the max-pos
            context.MaxReadPosition = restore;

            return (TProperty) (object) value;
        }

        int ILengthProperty<TEntityActual>.Serialize(TEntityActual value, SerializationContext context)
        {
            return Serializer<TEntityActual>.Serialize(value, context);
        }
    }
}
