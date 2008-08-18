using System;
using System.Reflection;
using System.Collections.Generic;

namespace ProtoBuf.Property
{
    internal class PropertyArray<TSource, TValue> : Property<TSource, TValue[]>
    {
        public override IEnumerable<Property<TSource>> GetCompatibleReaders()
        {
            foreach (Property<TValue> alt in innerProperty.GetCompatibleReaders())
            {
                yield return CreateAlternative<PropertyArray<TSource, TValue>>(alt.DataFormat);
            }
        }

        private Property<TValue, TValue> innerProperty;

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
            TValue[] arr = GetValue(source);
            if (arr == null || arr.Length == 0) return 0;
            int total = 0;
            for(int i = 0 ; i < arr.Length ; i++)
            {
                total += innerProperty.Serialize(arr[i], context);
            }
            return total;
        }
        public override TValue[] DeserializeImpl(TSource source, SerializationContext context)
        {
            TValue[] arr = GetValue(source);
            TValue value = innerProperty.DeserializeImpl(default(TValue), context);
            if (context.TryPeekFieldPrefix(FieldPrefix))
            {
                List<TValue> list = new List<TValue>();
                list.Add(value);
                do
                {
                    list.Add(innerProperty.DeserializeImpl(default(TValue), context));
                } while (context.TryPeekFieldPrefix(FieldPrefix));
                int index = Resize(ref arr, list.Count);
                list.CopyTo(arr, index);
                arr = list.ToArray();
            }
            else
            {
                Resize(ref arr, 1);
                arr[arr.Length - 1] = value;
            }
            return arr;
        }
        private static int Resize(ref TValue[] array, int delta) {
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
    }
}
