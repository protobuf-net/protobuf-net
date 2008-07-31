using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf
{
    internal sealed class ArrayProperty<TEntity, TValue> : PropertyBase<TEntity, TValue[], TValue>
        where TEntity : class, new()
    {
        public ArrayProperty(PropertyInfo property)
            : base(property)
        {
        }
        protected override bool HasValue(TValue[] arr)
        {
            return arr != null && arr.Length > 0;
        }
        public override bool IsRepeated { get { return true; } }

        public override int Serialize(TValue[] arr, SerializationContext context)
        { // write all items in a contiguous block
            int total = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                total += SerializeValue(arr[i], context);
            }
            return total;
        }
        protected override int GetLengthImpl(TValue[] arr, SerializationContext context)
        {
            int total = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                total += GetValueLength(arr[i], context);
            }
            return total;
        }

        /// <summary>
        /// Simple blit version; probably optimise later...
        /// </summary>
        private void AddItem(TEntity instance, TValue value)
        {
            TValue[] arr = GetValue(instance);
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
            SetValue(instance, arr);
        }
        public override void Deserialize(TEntity instance, SerializationContext context)
        {   // read a single item
            AddItem(instance, ValueSerializer.Deserialize(default(TValue), context));
        }

        public override void DeserializeGroup(TEntity instance, SerializationContext context)
        {
            // read a single item
            AddItem(instance, GroupSerializer.DeserializeGroup(default(TValue), context));
        }
    }
}
