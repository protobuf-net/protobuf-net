using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf.Property;

namespace ProtoBuf
{
    internal abstract class SerializerProxy<T>
    {
        public static readonly SerializerProxy<T> Default;

        static bool HasAdd(Type list, Type item)
        {
            return list.GetMethod("Add", new Type[] { item }) != null;
        }
        static SerializerProxy()
        {
            Type itemType;
            bool enumOnly;
            if (typeof(T).IsValueType) { } // not handling that...
            if (Serializer.IsEntityType(typeof(T)))
            {
                Default = (SerializerProxy<T>)typeof(SerializerProxy<T>).GetMethod("MakeItem")
                    .MakeGenericMethod(typeof(T)).Invoke(null, null);
            }
            else if ((itemType = PropertyFactory.GetListType(typeof(T), out enumOnly)) != null
                && (!enumOnly || HasAdd(typeof(T), itemType)) &&  Serializer.IsEntityType(itemType))
            {
                Default = (SerializerProxy<T>)typeof(SerializerProxy<T>).GetMethod("MakeList")
                    .MakeGenericMethod(typeof(T), itemType).Invoke(null, null);
            }

            if(Default == null)
            {
                throw new InvalidOperationException("Only concrete data-contract classes (and lists/arrays of such) can be processed");
            }
        }
        public abstract int Serialize(T instance, Stream destination);
        public abstract void Deserialize(ref T instance, Stream source);

        public static SerializerProxy<TItem> MakeItem<TItem>()
            where TItem : class, T, new()
        {
            return new SerializerItemProxy<TItem>();
        }
        public static SerializerProxy<TList> MakeList<TList,TItem>()
            where TList : class, T, IEnumerable<TItem>
            where TItem : class, new()
        {
            return new SerializerListProxy<TList,TItem>();
        }
    }

    sealed class SerializerItemProxy<TItem> : SerializerProxy<TItem> where TItem : class, new()
    {
        public override int Serialize(TItem instance, Stream destination)
        {
            return Serializer<TItem>.Serialize(instance, destination);
        }
        public override void Deserialize(ref TItem instance, Stream source)
        {
            Serializer<TItem>.Deserialize(ref instance, source);
        }
    }

    sealed class SerializerListProxy<TList, TItem> : SerializerProxy<TList>
        where TList : class, IEnumerable<TItem>
        where TItem : class, new()
    {
        public override int Serialize(TList instance, Stream destination)
        {
            return Serializer<ListWrapper>.Serialize(new ListWrapper(instance), destination);
        }
        public override void Deserialize(ref TList instance, Stream source)
        {
            ListWrapper wrapper = null;
            Serializer<ListWrapper>.Deserialize(ref wrapper, source);
            if (wrapper != null) instance = wrapper.List;
        }

        [ProtoContract]
        sealed class ListWrapper
        {
            public ListWrapper() { }
            public ListWrapper(TList list) { this.list = list; }
            private TList list;
            [ProtoMember(1)]
            public TList List
            {
                get { return list; }
                set { list = value; }
            }
        }
    }
}
