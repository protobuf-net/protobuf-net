using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization;

namespace ProtoBuf
{
    /// <summary>
    /// This class acts as an internal wrapper allowing us to do a dynamic
    /// methodinfo invoke; an't put into Serializer as don't want on public
    /// API; can't put into Serializer&lt;T&gt; since we need to invoke
    /// accross classes, which isn't allowed in Silverlight)
    /// </summary>
    internal static class ExtensibleUtil
    {
        public static IEnumerable<TValue> GetExtendedValues<TValue>(IExtensible instance, int tag, DataFormat format, bool singleton)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            return (IEnumerable<TValue>)typeof(ExtensibleUtil)
                .GetMethod("GetExtendedValuesTyped")
                .MakeGenericMethod(instance.GetType(), typeof(TValue))
                .Invoke(null, new object[] { instance, tag, format, singleton });
        }
        public static IEnumerable<TValue> GetExtendedValuesTyped<TSource, TValue>(TSource instance, int tag, DataFormat format, bool singleton)
            where TSource : class, IExtensible, new()
        {
            Serializer<TSource>.CheckTagNotInUse(tag);

            ISerializer<TValue> serializer = SerializerCache<TValue>.GetSerializer(format);
            Stream stream = instance.BeginQuery();
            TValue lastValue = default(TValue);
            bool hasValue = false;
            try
            {
                SerializationContext ctx = new SerializationContext(stream);
                int token;
                while (TwosComplementSerializer.TryReadInt32(ctx, out token))
                {
                    WireType wireType;
                    int readTag;
                    Serializer<TSource>.ParseFieldToken(token, out wireType, out readTag);

                    if (readTag == tag)
                    {
                        if (wireType != serializer.WireType)
                        {
                            throw new SerializationException(
                                string.Format("Wire-type mismatch; expected {0}, received {1}",
                                    serializer.WireType, wireType));
                        }
                        TValue value = serializer.Deserialize(lastValue, ctx);
                        hasValue = true;
                        if (singleton)
                        {
                            // merge with later values before returning
                            lastValue = value;
                        }
                        else
                        {
                            // return immediately; no merge
                            yield return value;
                        }
                    }
                    else
                    {
                        // skip all other tags
                        Serializer<TSource>.SkipData(ctx, wireType);
                    }
                }
            }
            finally
            {
                instance.EndQuery(stream);
            }
            if (singleton && hasValue)
            {
                yield return lastValue;
            }
        }

        public static void AppendExtendValue<TValue>(IExtensible instance, int tag, DataFormat format, object value)
        {
            if (instance == null) throw new ArgumentNullException("instance");
            typeof(ExtensibleUtil)
                .GetMethod("AppendExtendValueTyped")
                .MakeGenericMethod(instance.GetType(), typeof(TValue))
                .Invoke(null, new object[] { instance, tag, format, value});
        }
        public static void AppendExtendValueTyped<TSource, TValue>(TSource instance, int tag, DataFormat format, TValue value)
            where TSource : class, IExtensible, new()
        {
            Serializer<TSource>.CheckTagNotInUse(tag);

            ISerializer<TValue> serializer = SerializerCache<TValue>.GetSerializer(format);

            SerializationContext nullCtx = new SerializationContext(Stream.Null);
            int len = serializer.GetLength(value, nullCtx);
            if (len > 0)
            {
                Stream stream = instance.BeginAppend();
                try
                {
                    SerializationContext ctx = new SerializationContext(stream);
                    Serializer.WriteFieldToken(tag, serializer.WireType, ctx);
                    serializer.Serialize(value, ctx);
                    instance.EndAppend(stream, true);
                }
                catch
                {
                    instance.EndAppend(stream, false);
                    throw;
                }
            }
        }
    }
}
