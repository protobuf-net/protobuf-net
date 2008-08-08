using System;
using System.Reflection;

namespace ProtoBuf.Property
{
    internal class PropertyArray<TSource, TValue> : Property<TSource, TValue[]>
    {
        private Property<TValue, TValue> innerProperty;

        protected override void OnBeforeInit(MemberInfo member, bool overrideIsGroup)
        {
            innerProperty = PropertyFactory.CreatePassThru<TValue>(member, overrideIsGroup);
            base.OnBeforeInit(member, overrideIsGroup);
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
            TValue value = innerProperty.DeserializeImpl(default(TValue), context);
            TValue[] arr = GetValue(source);
            if (arr == null)
            {
                arr = new TValue[1];
                arr[0] = value;
            }
            else
            {
                int len = arr.Length;
                TValue[] newArr = new TValue[len + 1];
                Array.Copy(arr, newArr, len);
                newArr[len] = value;
                arr = newArr;
            }
            return arr;
        }
    }
}
