using ProtoBuf.Meta;
using System;
using ProtoBuf.Compiler;
using System.Collections.Generic;

namespace ProtoBuf.Serializers
{
    class MapDecorator<TDictionary, TKey, TValue> : ProtoDecoratorBase where TDictionary : IDictionary<TKey,TValue>
    {
        private readonly Type concreteType;
        private readonly IProtoSerializer keyTail;
        private readonly int fieldNumber;
        private readonly WireType wireType;
        internal MapDecorator(TypeModel model, Type concreteType, IProtoSerializer keyTail, IProtoSerializer valueTail,
            int fieldNumber, WireType wireType, WireType keyWireType, WireType valueWireType)
            : base(new DefaultValueDecorator(model, DefaultValue, new TagDecorator(2, valueWireType, false, valueTail)))
        {
            this.wireType = wireType;
            this.keyTail = new DefaultValueDecorator(model, DefaultKey, new TagDecorator(1, keyWireType, false, keyTail));
            this.fieldNumber = fieldNumber;
            this.concreteType = concreteType ?? typeof(TDictionary);

            if (keyTail.RequiresOldValue) throw new InvalidOperationException("Key tail should not require the old value");
            if (!keyTail.ReturnsValue) throw new InvalidOperationException("Key tail should return a value");
            if (!valueTail.ReturnsValue) throw new InvalidOperationException("Value tail should return a value");
        }
        private static readonly TKey DefaultKey = (typeof(TKey) == typeof(string)) ? (TKey)(object)"" : default(TKey);
        private static readonly TValue DefaultValue = (typeof(TValue) == typeof(string)) ? (TValue)(object)"" : default(TValue);
        public override Type ExpectedType => typeof(TDictionary);

        public override bool ReturnsValue => true;

        public override bool RequiresOldValue => true;

        public override object Read(object untyped, ProtoReader source)
        {
            var typed = ((TDictionary)untyped);
            if(typed == null) typed = (TDictionary)Activator.CreateInstance(concreteType);
            do
            {
                var key = DefaultKey;
                var value = DefaultValue;
                SubItemToken token = ProtoReader.StartSubItem(source);
                int field;
                while((field = source.ReadFieldHeader()) > 0)
                {
                    switch(field)
                    {
                        case 1:
                            key = (TKey)keyTail.Read(null, source);
                            break;
                        case 2:
                            value = (TValue)Tail.Read(Tail.RequiresOldValue ? (object)value : null, source);
                            break;
                        default:
                            source.SkipField();
                            break;
                    }
                }

                ProtoReader.EndSubItem(token, source);
                typed[key] = value;
            } while (source.TryReadFieldHeader(fieldNumber));

            return typed;
        }

        private void ThrowNullKeyValue()
        {
            throw new NullReferenceException("Null key/value in map");
        }
        public override void Write(object untyped, ProtoWriter dest)
        {
            foreach(var pair in (TDictionary)untyped)
            {
                if (pair.Key == null || pair.Value == null) ThrowNullKeyValue();

                ProtoWriter.WriteFieldHeader(fieldNumber, wireType, dest);
                var token = ProtoWriter.StartSubItem(null, dest);
                keyTail.Write(pair.Key, dest);
                Tail.Write(pair.Value, dest);
                ProtoWriter.EndSubItem(token, dest);
            }
        }

        protected override void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            throw new NotImplementedException();
        }

        protected override void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            throw new NotImplementedException();
        }
    }
}
