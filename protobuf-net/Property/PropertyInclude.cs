
//using System;
//using System.Collections.Generic;
//namespace ProtoBuf.Property
//{
//    internal sealed class PropertyInclude<TValue> : Property<TValue, TValue>
//        where TValue : class, new()
//    {
//        Property<TSource> primary;
//        KeyValuePair<Type, Property<TSource>>[] map;
//        public override IEnumerable<Property<TSource>> GetCompatibleReaders()
//        {
//            foreach (Property<TSource> alt in primary.GetCompatibleReaders())
//            {
//                yield return alt;
//            }
//            for(int i = 0 ; i < map.Length ; i++) {
//                // yield the primary subclass reader
//                yield return map[i].Value;

//                // yield any alternative subclass readers
//                foreach (Property<TSource> alt in map[i].Value.GetCompatibleReaders())
//                {
//                    yield return alt;
//                }
//            }
//        }

//        public override string DefinedType
//        {
//            get { return primary.DefinedType; }
//        }

//        public override WireType WireType {
//            get { return primary.WireType; }
//        }

//        protected override void OnBeforeInit(int tag, ref DataFormat format)
//        {
//            primary = PropertyFactory.CreatePassThru<TValue>(tag, ref format);
//            base.OnBeforeInit(tag, ref format);
//        }
//        public override int Serialize(TSource source, SerializationContext context)
//        {
//            TValue value = GetValue(source);
//            if (value == null) return 0;
//            Type type = value.GetType();
//            for (int i = 0; i < map.Length; i++)
//            {
//                if (ReferenceEquals(map[i].Key, type)) return map[i].Value.Serialize(source, context);
//            }
//            throw new ProtoException(string.Format("Cannot serialize {0}; no suitable serializer found. Consider adding ProtoIncludeAttribute to declare this known-type", type));
//        }

//        public override TValue DeserializeImpl(TSource source, SerializationContext context)
//        {
//            // for this version to be called, we must be deserializing the base-class;
//            // use the first map:
//            return (TValue) primary.DeserializeImplWeak(source, context);
//        }
//    }
//}
