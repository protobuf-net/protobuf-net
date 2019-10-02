using ProtoBuf.Compiler;
using ProtoBuf.Internal;
using System;
using System.Collections.Generic;

namespace ProtoBuf.Serializers
{
    internal class MapDecorator<TDictionary, TKey, TValue> : ProtoDecoratorBase where TDictionary : class, IDictionary<TKey, TValue>
    {
        internal MapDecorator(
            int fieldNumber, SerializerFeatures features, SerializerFeatures keyFeatures, SerializerFeatures valueFeatures, bool overwriteList)
            : base(null)
        {
            _features = features;
            _keyFeatures = keyFeatures;
            _valueFeatures = valueFeatures;
            _fieldNumber = fieldNumber;
            if (overwriteList) _features |= SerializerFeatures.OptionOverwriteList;
        }
        private readonly int _fieldNumber;
        private readonly SerializerFeatures _features, _keyFeatures, _valueFeatures;

        public override Type ExpectedType => typeof(TDictionary);

        public override bool ReturnsValue => true;

        public override bool RequiresOldValue => (_features & SerializerFeatures.OptionOverwriteList) == 0;

        public override object Read(ref ProtoReader.State state, object value)
            =>  state.ReadMap<TDictionary, TKey, TValue>(_features, _keyFeatures, _valueFeatures, (TDictionary)value);

        public override void Write(ref ProtoWriter.State state, object value)
            => state.WriteMap(_fieldNumber, _features, _keyFeatures, _valueFeatures, (TDictionary)value);

        protected override void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            ThrowHelper.ThrowNotImplementedException();
        }
        protected override void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            ThrowHelper.ThrowNotImplementedException();
        }
    }
}