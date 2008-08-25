
//using System;
//using System.Collections.Generic;
//namespace ProtoBuf.Property
//{
//    internal sealed class PropertyInclude<TSource, TBase> : Property<TSource, TBase>
//        where TBase : class, new()
//    {
//        KeyValuePair<Type, Property<TBase, TBase>>[] map;
//        public override IEnumerable<Property<TSource>> GetCompatibleReaders()
//        {
//            for (int i = 0; i < map.Length; i++)
//            {
//                if (map[i].Value == null) continue;
//                if (i > 0)
//                {
//                    // yield the primary subclass reader
//                    yield return CreateSlave(map[i].Value);
//                }
//                // yield any alternative subclass readers
//                foreach (Property<TBase, TBase> alt in map[i].Value.GetCompatibleReaders())
//                {
//                    yield return CreateSlave(alt);
//                }
//            }
//        }

//        public override string DefinedType
//        {
//            get { return map[0].Value.DefinedType; }
//        }

//        public override WireType WireType
//        {
//            get { return map[0].Value.WireType; }
//        }

//        protected override void OnBeforeInit(int tag, ref DataFormat format)
//        {
//            map = new KeyValuePair<Type, Property<TBase, TBase>>[] {
//                new KeyValuePair<Type, Property<TBase, TBase>>(
//                    typeof(TBase),
//                    PropertyFactory.CreatePassThru<TBase>(tag, ref format)
//                )
//            };
//            base.OnBeforeInit(tag, ref format);
//        }
//        public override int Serialize(TSource source, SerializationContext context)
//        {
//            TBase value = GetValue(source);
//            if (value == null) return 0;
//            Type type = value.GetType();
//            for (int i = 0; i < map.Length; i++)
//            {
//                if (ReferenceEquals(map[i].Key, type)) return map[i].Value.Serialize(value, context);
//            }
//            throw new ProtoException(string.Format("Cannot serialize {0}; no suitable serializer found. Consider adding ProtoIncludeAttribute to declare this known-type", type));
//        }

//        public override TBase DeserializeImpl(TSource source, SerializationContext context)
//        {
//            // for this version to be called, we must be deserializing the base-class;
//            // use the first map:
//            return map[0].Value.DeserializeImpl(GetValue(source), context);
//        }
//    }

    
//}
