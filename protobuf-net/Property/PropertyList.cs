using System;
using System.Collections.Generic;

namespace ProtoBuf.Property
{
    internal class PropertyList<TSource, TList, TValue> : Property<TSource, TList>
        where TList : ICollection<TValue>
    {
        private Property<TValue, TValue> innerProperty;

        public override IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            foreach(Property<TValue> alt in innerProperty.GetCompatibleReaders()) {
                yield return CreateAlternative<PropertyList<TSource, TList, TValue>>(alt.DataFormat);
            }
            if (PropertyFactory.CanPack(innerProperty.WireType)) yield return CreateAlternative<PropertyPackedList<TSource, TList, TValue>>(innerProperty.DataFormat);            
        }

        protected override void OnBeforeInit(int tag, ref DataFormat format)
        {
            innerProperty = PropertyFactory.CreatePassThru<TValue>(tag, ref format);
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
            if (list == null || list.Count == 0) return 0;
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
                if(typeof(TList) == typeof(List<TValue>) || typeof(TList) == typeof(IList<TValue>))
                {
                    list = (TList)(object)new List<TValue>(); // reasonable default for IList<T>, and faster for List<T>
                }
                else
                {
                    list = (TList)Activator.CreateInstance(typeof(TList));
                }
            }
            do
            {
                list.Add(innerProperty.DeserializeImpl(default(TValue), context));
            } while (context.TryPeekFieldPrefix(FieldPrefix));

            if (set && canSetValue) SetValue(source, list);
            return list;
        }
    }
}
