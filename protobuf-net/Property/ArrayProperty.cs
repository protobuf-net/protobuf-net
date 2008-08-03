using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf
{
    internal sealed class ArrayProperty<TEntity, TValue> : MultiProperty<TEntity, TValue[], TValue>
        where TEntity : class, new()
    {
        public ArrayProperty(PropertyInfo property)
            : base(property)
        { }

        protected override bool HasValue(TValue[] arr)
        {
            return arr != null && arr.Length > 0;
        }

        /// <summary>
        /// Simple blit version; probably optimise later...
        /// </summary>
        protected override bool AddItem(ref TValue[] arr, TValue value)
        {
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
            return true;
        }
    }
}
