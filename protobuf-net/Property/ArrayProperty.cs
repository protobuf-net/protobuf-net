using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf
{
    internal sealed class ArrayProperty<TEntity, TValue> : PropertyBase<TEntity, TValue[]>, IGroupProperty<TEntity>
        where TEntity : class, new()
    {
        public ArrayProperty(PropertyInfo property)
            : base(property)
        {
            serializer = GetSerializer<TValue>(property);
        }

        private readonly ISerializer<TValue> serializer;

        public override string DefinedType { get { return serializer.DefinedType; } }
        public override WireType WireType { get { return serializer.WireType; } }

        public override int Serialize(TEntity instance, SerializationContext context)
        { // write all items in a contiguous block
            TValue[] arr = GetValue(instance);
            int total = 0;
            if (arr != null && arr.Length > 0)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    total += Serialize(arr[i], serializer, context);
                }
            }

            return total;
        }
        public override int GetLength(TEntity instance, SerializationContext context)
        {
            TValue[] arr = GetValue(instance);
            int total = 0;
            if (arr != null && arr.Length > 0)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    total += GetLength(arr[i], serializer, context);
                }
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
            AddItem(instance, serializer.Deserialize(default(TValue), context));
        }

        public void DeserializeGroup(TEntity instance, SerializationContext context)
        {
            // the list could be of anything... need to check if the serializer
            // supports group usage (i.e. entities)
            IGroupSerializer<TValue> groupSerializer = serializer as IGroupSerializer<TValue>;
            if (groupSerializer == null)
            {
                throw new ProtoException("Cannot treat property as a group: " + Name);
            }
            // read a single item
            AddItem(instance, groupSerializer.DeserializeGroup(default(TValue), context));
        }
    }
}
