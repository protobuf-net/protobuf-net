using System;
using System.Collections.Generic;
using System.Reflection;

namespace ProtoBuf.Property
{
    internal class PropertyEnumerable<TSource, TList, TValue> : Property<TSource, TList>
        where TList : IEnumerable<TValue>
    {
        private Property<TValue, TValue> innerProperty;
        private Setter<TList, TValue> add;

        protected override void OnBeforeInit(MemberInfo member, bool overrideIsGroup)
        {
            innerProperty = PropertyFactory.CreatePassThru<TValue>(member, overrideIsGroup);
            MethodInfo addMethod = PropertyFactory.GetAddMethod(typeof(TList), typeof(TValue));
#if CF2
            add = delegate(TList source, TValue value) { addMethod.Invoke(source, new object[] { value }); };
#else
            add = (Setter<TList, TValue>)Delegate.CreateDelegate(typeof(Setter<TList, TValue>), null, addMethod);
#endif
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
            TList list = GetValue(source);
            if (list == null) return 0;
            int total = 0;
            foreach (TValue value in list)
            {
                total += innerProperty.Serialize(value, context);
            }
            return total;
        }
        public override TList DeserializeImpl(TSource source, SerializationContext context)
        {
            throw new InvalidOperationException("DeserializeImpl should not be called for PropertyeEnumerable");
        }
        public override void Deserialize(TSource source, SerializationContext context)
        {
            TList list = GetValue(source);
            bool set = list == null;
            if (set) list = (TList)Activator.CreateInstance(typeof(TList));
            do
            {
                add(list, innerProperty.DeserializeImpl(default(TValue), context));
            } while (context.TryPeekFieldPrefix(FieldPrefix));

            if (set) SetValue(source, list);
        }


    }
}
