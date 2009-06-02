using System;
using System.Collections.Generic;

namespace ProtoBuf.Property
{
    internal class PropertyPackedList<TSource, TList, TValue> : Property<TSource, TList>, ILengthProperty<TList>
        where TList : ICollection<TValue>
    {
        private Property<TValue, TValue> innerProperty;

        public override IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            foreach (Property<TValue> alt in innerProperty.GetCompatibleReaders()) {
                yield return CreateAlternative<PropertyList<TSource, TList, TValue>>(alt.DataFormat);
            }
            yield return CreateAlternative<PropertyList<TSource, TList, TValue>>(innerProperty.DataFormat);
        }

        protected override void OnBeforeInit(int tag, ref DataFormat format)
        {
            innerProperty = PropertyFactory.CreatePassThru<TValue>(tag, ref format);
            PropertyFactory.VerifyCanPack(innerProperty.WireType);
            base.OnBeforeInit(tag, ref format);
        }
        public override WireType WireType
        {
            get { return WireType.String; }
        }

        public override string DefinedType
        {
            get { return innerProperty.DefinedType; }
        }
        public override bool IsRepeated { get { return true; } }

        public override int Serialize(TSource source, SerializationContext context)
        {
            TList list = GetValue(source);
            if (list == null) return 0;
            if (list.Count == 0)
            {
                int len = WritePrefix(context) + 1;
                context.WriteByte(0);
                return len;
            }
            return WritePrefix(context) + context.WriteLengthPrefixed(list, 0, this);
        }
        public override TList DeserializeImpl(TSource source, SerializationContext context)
        {
            return DeserializeImpl(source, context, false);
        }
        public override void Deserialize(TSource source, SerializationContext context)
        {
            DeserializeImpl(source, context, true);
        }
        protected override void OnAfterInit()
        {
            base.OnAfterInit();
            innerProperty.SuppressPrefix = true;
        }
        private TList DeserializeImpl(TSource source, SerializationContext context, bool canSetValue)
        {
            TList list = GetValue(source);
            bool set = list == null;
            if (set) list = (TList)Activator.CreateInstance(typeof(TList));

            long restore = context.LimitByLengthPrefix();
            while (context.Position < context.MaxReadPosition)
            {
                list.Add(innerProperty.DeserializeImpl(default(TValue), context));
            }
            // restore the max-pos
            context.MaxReadPosition = restore;

            if (set && canSetValue) SetValue(source, list);
            return list;
        }

        int ILengthProperty<TList>.Serialize(TList list, SerializationContext context)
        {
            int total = 0;
            foreach (TValue value in list)
            {
                total += innerProperty.Serialize(value, context);
            }
            return total;
        }
    }
}
