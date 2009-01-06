using System;
using System.Collections.Generic;
using System.IO;
using ProtoBuf.Property;
using System.Diagnostics;

namespace ProtoBuf
{
    internal abstract class SerializerProxy<T>
    {
        public static readonly SerializerProxy<T> Default;

        static SerializerProxy()
        {
            if (Serializer.IsEntityType(typeof(T)))
            {
                Default = (SerializerProxy<T>)typeof(SerializerProxy<T>).GetMethod("MakeItem")
                    .MakeGenericMethod(typeof(T)).Invoke(null, null);
            }
            else // see if our property-factory can handle this type; if it can, use a wrapper object
            {
                try
                {
                    DataFormat fmt = DataFormat.Default;
                    if (PropertyFactory.CreatePassThru<T>(1, ref fmt) != null)
                    {
                        Default = (SerializerProxy<T>)typeof(SerializerProxy<T>).GetMethod("MakeValue")
                            .MakeGenericMethod(typeof(T)).Invoke(null, null);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.ToString(), "SerializerProxy.cctor");
                    // nope, can't do it...
                }
            }
            if(Default == null)
            {
                throw new InvalidOperationException("Only data-contract classes (and lists/arrays of such) can be processed (error processing " + typeof(T).Name + ")");
            }
        }
        public abstract int Serialize(T instance, Stream destination);
        public abstract void Deserialize(ref T instance, Stream source);

        public static SerializerProxy<TItem> MakeItem<TItem>()
            where TItem : class, T
        {
            return new SerializerItemProxy<TItem>();
        }
        public static SerializerProxy<TValue> MakeValue<TValue>()
        {
            return new SerializerSimpleProxy<TValue>();
        }
    }

    sealed class SerializerItemProxy<TItem> : SerializerProxy<TItem> where TItem : class
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
    
    sealed class SerializerSimpleProxy<TValue> : SerializerProxy<TValue>
    {
        public override int Serialize(TValue value, Stream destination)
        {
            return Serializer<SimpleWrapper>.Serialize(new SimpleWrapper(value), destination);
        }
        public override void Deserialize(ref TValue value, Stream source)
        {
            SimpleWrapper wrapper = null;
            Serializer<SimpleWrapper>.Deserialize(ref wrapper, source);
            if (wrapper != null) value = wrapper.Value;
        }

        [ProtoContract]
        sealed class SimpleWrapper
        {
            public SimpleWrapper() { }
            public SimpleWrapper(TValue value) { this.value = value; }
            private TValue value;
            [ProtoMember(1)]
            public TValue Value
            {
                get { return this.value; }
                set { this.value = value; }
            }
        }
    }
}
