using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf.Property
{
    internal class PropertyEnumerable<TSource, TList, TValue> : Property<TSource, TList>
        where TList : IEnumerable<TValue>
    {
        public override IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            foreach (Property<TValue> alt in innerProperty.GetCompatibleReaders())
            {
                yield return CreateAlternative<PropertyEnumerable<TSource, TList, TValue>>(alt.DataFormat);
            }
            if (PropertyFactory.CanPack(innerProperty.WireType)) yield return CreateAlternative<PropertyPackedEnumerable<TSource, TList, TValue>>(innerProperty.DataFormat);
        }

        private Property<TValue, TValue> innerProperty;
        private Setter<TList, TValue> add;

        protected override void OnBeforeInit(int tag, ref DataFormat format)
        {
            innerProperty = PropertyFactory.CreatePassThru<TValue>(tag, ref format);
            MethodInfo addMethod = PropertyFactory.GetAddMethod(typeof(TList), typeof(TValue));
#if CF2
            add = delegate(TList source, TValue value) { addMethod.Invoke(source, new object[] { value }); };
#else
            add = (Setter<TList, TValue>)Delegate.CreateDelegate(typeof(Setter<TList, TValue>), null, addMethod);
#endif
            base.OnBeforeInit(tag, ref format);
        }
        public override WireType WireType
        {
            get { return innerProperty.WireType; }
        }

        public override string DefinedType
        {
            get { return innerProperty.DefinedType; }
        }
        public override bool IsRepeated {get {return true;}}

        public override int Serialize(TSource source, SerializationContext context)
        {
            TList list = GetValue(source);
            if (list == null) return 0;
            int total = 0;
            foreach (TValue value in list)
            {
                total += innerProperty.Serialize(value, context);
            }
            return total;
        }
        public override TList DeserializeImpl(TSource source, SerializationContext context)
        {
            return DeserializeImpl(source, context, false);
        }
        public override void Deserialize(TSource source, SerializationContext context)
        {
            DeserializeImpl(source, context, true);
        }
        private TList DeserializeImpl(TSource source, SerializationContext context, bool canSetValue)
        {
            TList list = GetValue(source);
            bool set = list == null;
            if (set)
            {
                list = ObjectFactory<TList>.Create();
            }
            do
            {
                add(list, innerProperty.DeserializeImpl(default(TValue), context));
            } while (context.TryPeekFieldPrefix(FieldPrefix));

            if (set && canSetValue) SetValue(source, list);
            return list;
        }


    }
}
