using System;
using System.Collections.Generic;

namespace ProtoBuf.Property
{
    internal class PropertyPackedArray<TSource, TValue> : Property<TSource, TValue[]>, ILengthProperty<TValue[]>
    {
        public override IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            foreach (Property<TValue> alt in innerProperty.GetCompatibleReaders())
            {
                yield return CreateAlternative<PropertyArray<TSource, TValue>>(alt.DataFormat);
            }
            yield return CreateAlternative<PropertyArray<TSource, TValue>>(innerProperty.DataFormat);
        }

        private Property<TValue, TValue> innerProperty;

        protected override void OnBeforeInit(int tag, ref DataFormat format)
        {
            innerProperty = PropertyFactory.CreatePassThru<TValue>(tag, ref format);
            PropertyFactory.VerifyCanPack(innerProperty.WireType);
            base.OnBeforeInit(tag, ref format);
        }
        protected override void OnAfterInit()
        {
            base.OnAfterInit();
            innerProperty.SuppressPrefix = true;
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
            TValue[] arr = GetValue(source);
            if (arr == null) return 0;
            if (arr.Length == 0)
            {
                int len = WritePrefix(context) + 1;
                context.WriteByte(0);
                return len;
            }
            return WritePrefix(context) + context.WriteLengthPrefixed(arr, 0, this);
        }
        public override TValue[] DeserializeImpl(TSource source, SerializationContext context)
        {
            TValue[] arr = GetValue(source);

            long restore = context.LimitByLengthPrefix();
            List<TValue> list = new List<TValue>();
            while (context.Position < context.MaxReadPosition)
            {
                list.Add(innerProperty.DeserializeImpl(default(TValue), context));
            }
            // restore the max-pos
            context.MaxReadPosition = restore;

            int index = Resize(ref arr, list.Count);
            list.CopyTo(arr, index);

            return arr;
        }
        private static int Resize(ref TValue[] array, int delta)
        {
            if (array == null)
            {
                array = new TValue[delta];
                return 0;
            }
            else
            {
                int len = array.Length;
                TValue[] newArr = new TValue[len + delta];
                Array.Copy(array, newArr, len);
                array = newArr;
                return len;
            }
        }
        int ILengthProperty<TValue[]>.Serialize(TValue[] arr, SerializationContext context)
        {
            int total = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                total += innerProperty.Serialize(arr[i], context);
            }
            return total;
        }
    }
}
