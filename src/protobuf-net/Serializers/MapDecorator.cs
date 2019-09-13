#pragma warning disable RCS1165
using System;
using ProtoBuf.Compiler;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ProtoBuf.Serializers
{
    internal class MapDecorator<TDictionary, TKey, TValue> : ProtoDecoratorBase where TDictionary : class, IDictionary<TKey, TValue>
    {
        private readonly Type concreteType;
        private readonly IProtoSerializer keyTail;
        private readonly int fieldNumber;
        private readonly WireType wireType;

        internal MapDecorator(Type concreteType, IProtoSerializer keyTail, IProtoSerializer valueTail,
            int fieldNumber, WireType wireType, WireType keyWireType, WireType valueWireType, bool overwriteList)
            : base(DefaultValue == null
                  ? (IProtoSerializer)new TagDecorator(2, valueWireType, false, valueTail)
                  : (IProtoSerializer)new DefaultValueDecorator(DefaultValue, new TagDecorator(2, valueWireType, false, valueTail)))
        {
            this.wireType = wireType;
            this.keyTail = new DefaultValueDecorator(DefaultKey, new TagDecorator(1, keyWireType, false, keyTail));
            this.fieldNumber = fieldNumber;
            this.concreteType = concreteType ?? typeof(TDictionary);

            if (keyTail.RequiresOldValue) throw new InvalidOperationException("Key tail should not require the old value");
            if (!keyTail.ReturnsValue) throw new InvalidOperationException("Key tail should return a value");
            if (!valueTail.ReturnsValue) throw new InvalidOperationException("Value tail should return a value");

            AppendToCollection = !overwriteList;
            _runtimeSerializer = new RuntimePairSerializer(this.keyTail, Tail);
        }

        private static readonly TKey DefaultKey = (typeof(TKey) == typeof(string)) ? (TKey)(object)"" : default;
        private static readonly TValue DefaultValue = (typeof(TValue) == typeof(string)) ? (TValue)(object)"" : default;
        public override Type ExpectedType => typeof(TDictionary);

        public override bool ReturnsValue => true;

        public override bool RequiresOldValue => AppendToCollection;

        private bool AppendToCollection { get; }

        public override object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            TDictionary typed = (AppendToCollection ? ((TDictionary)value) : null)
                ?? (TDictionary)Activator.CreateInstance(concreteType);
            do
            {
                var pair = new KeyValuePair<TKey, TValue>(DefaultKey, DefaultValue);
                pair = source.ReadSubItem<KeyValuePair<TKey, TValue>>(ref state, pair, _runtimeSerializer);
                typed[pair.Key] = pair.Value;
            } while (source.TryReadFieldHeader(ref state, fieldNumber));

            return typed;
        }

        public override void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            foreach (var pair in (TDictionary)value)
            {
                ProtoWriter.WriteFieldHeader(fieldNumber, wireType, dest, ref state);
                ProtoWriter.WriteSubItem<KeyValuePair<TKey, TValue>>(pair, dest, ref state, _runtimeSerializer);
            }
        }

        private readonly IProtoSerializer<KeyValuePair<TKey, TValue>, KeyValuePair<TKey, TValue>> _runtimeSerializer;

        sealed class RuntimePairSerializer : IProtoSerializer<KeyValuePair<TKey, TValue>, KeyValuePair<TKey, TValue>>
        {
            private readonly IProtoSerializer _keyTail, _valueTail;
            public RuntimePairSerializer(IProtoSerializer keyTail, IProtoSerializer valueTail)
            {
                _keyTail = keyTail;
                _valueTail = valueTail;
            }

            KeyValuePair<TKey, TValue> IProtoSerializer<KeyValuePair<TKey, TValue>, KeyValuePair<TKey, TValue>>.Deserialize(ProtoReader reader, ref ProtoReader.State state, KeyValuePair<TKey, TValue> pair)
            {
                var key = pair.Key;
                var value = pair.Value;
                int field;
                while ((field = reader.ReadFieldHeader(ref state)) > 0)
                {
                    switch (field)
                    {
                        case 1:
                            key = (TKey)_keyTail.Read(reader, ref state, null);
                            break;
                        case 2:
                            value = (TValue)_valueTail.Read(reader, ref state, _valueTail.RequiresOldValue ? (object)value : null);
                            break;
                        default:
                            reader.SkipField(ref state);
                            break;
                    }
                }
                return new KeyValuePair<TKey, TValue>(key, value);
            }

            void IProtoSerializer<KeyValuePair<TKey, TValue>, KeyValuePair<TKey, TValue>>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, KeyValuePair<TKey, TValue> pair)
            {
                if (pair.Key != null) _keyTail.Write(writer, ref state, pair.Key);
                if (pair.Value != null) _valueTail.Write(writer, ref state, pair.Value);
            }
        }

        private static readonly MethodInfo indexerSet = GetIndexerSetter();

        private static MethodInfo GetIndexerSetter()
        {
            foreach (var prop in typeof(TDictionary).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (prop.Name != "Item") continue;
                if (prop.PropertyType != typeof(TValue)) continue;

                var args = prop.GetIndexParameters();
                if (args == null || args.Length != 1) continue;

                if (args[0].ParameterType != typeof(TKey)) continue;
                var method = prop.GetSetMethod(true);
                if (method != null)
                {
                    return method;
                }
            }
            throw new InvalidOperationException("Unable to resolve indexer for map");
        }

        FieldInfo GetPairSerializer(CompilerContext ctx)
        {
            var scope = ctx.Scope;
            if (!scope.TryGetAdditionalSerializerInstance(this, out var result))
            {
                result = scope.DefineAdditionalSerializerInstance<KeyValuePair<TKey, TValue>>(this,
                    (key, il) => ((MapDecorator<TDictionary, TKey, TValue>)key).WritePairSerialize(il),
                    (key, il) => ((MapDecorator<TDictionary, TKey, TValue>)key).WritePairDeserialize(il));
            }
            return result;
        }
        void WritePairSerialize(ILGenerator il) { il.ThrowException(typeof(NotImplementedException)); }
        void WritePairDeserialize(ILGenerator il) { il.ThrowException(typeof(NotImplementedException)); }
        protected override void EmitWrite(CompilerContext ctx, Local valueFrom)
        {
            var pairSerializer = GetPairSerializer(ctx);

            Type itemType = typeof(KeyValuePair<TKey, TValue>);
            MethodInfo moveNext, current, getEnumerator = ListDecorator.GetEnumeratorInfo(
                ExpectedType, itemType, out moveNext, out current);
            Type enumeratorType = getEnumerator.ReturnType;

            MethodInfo key = itemType.GetProperty(nameof(KeyValuePair<TKey, TValue>.Key)).GetGetMethod(),
                @value = itemType.GetProperty(nameof(KeyValuePair<TKey, TValue>.Value)).GetGetMethod();

            using (Compiler.Local list = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            using (Compiler.Local iter = new Compiler.Local(ctx, enumeratorType))
            using (Compiler.Local token = new Compiler.Local(ctx, typeof(SubItemToken)))
            using (Compiler.Local kvp = new Compiler.Local(ctx, itemType))
            {
                ctx.LoadAddress(list, ExpectedType);
                ctx.EmitCall(getEnumerator, ExpectedType);
                ctx.StoreValue(iter);
                using (ctx.Using(iter))
                {
                    Compiler.CodeLabel body = ctx.DefineLabel(), next = ctx.DefineLabel();
                    ctx.Branch(next, false);

                    ctx.MarkLabel(body);

                    ctx.LoadAddress(iter, enumeratorType);
                    ctx.EmitCall(current, enumeratorType);

                    if (itemType != typeof(object) && current.ReturnType == typeof(object))
                    {
                        ctx.CastFromObject(itemType);
                    }
                    ctx.StoreValue(kvp);


                    // ProtoWriter.WriteFieldHeader(fieldNumber, wireType, dest, ref state);
                    ctx.LoadValue(fieldNumber);
                    ctx.LoadValue((int)wireType);
                    ctx.LoadWriter(true);
                    ctx.EmitCall(Compiler.WriterUtil.GetStaticMethod("WriteFieldHeader", this));

                    // ProtoWriter.WriteSubItem<KeyValuePair<TKey, TValue>>(pair, dest, ref state, _runtimeSerializer);
                    SubItemSerializer.EmitWriteSubItem<KeyValuePair<TKey, TValue>>(ctx, kvp, pairSerializer, false);

                    ctx.MarkLabel(@next);
                    ctx.LoadAddress(iter, enumeratorType);
                    ctx.EmitCall(moveNext, enumeratorType);
                    ctx.BranchIfTrue(body, false);
                }
            }
        }
        protected override void EmitRead(CompilerContext ctx, Local valueFrom)
        {
            var pairSerializer = GetPairSerializer(ctx);

            using (Compiler.Local list = AppendToCollection ? ctx.GetLocalWithValue(ExpectedType, valueFrom)
                : new Compiler.Local(ctx, typeof(TDictionary)))
            using (Compiler.Local token = new Compiler.Local(ctx, typeof(SubItemToken)))
            using (Compiler.Local key = new Compiler.Local(ctx, typeof(TKey)))
            using (Compiler.Local @value = new Compiler.Local(ctx, typeof(TValue)))
            using (Compiler.Local fieldNumber = new Compiler.Local(ctx, typeof(int)))
            {
                if (!AppendToCollection)
                { // always new
                    ctx.LoadNullRef();
                    ctx.StoreValue(list);
                }
                if (concreteType != null)
                {
                    ctx.LoadValue(list);
                    Compiler.CodeLabel notNull = ctx.DefineLabel();
                    ctx.BranchIfTrue(notNull, true);
                    ctx.EmitCtor(concreteType);
                    ctx.StoreValue(list);
                    ctx.MarkLabel(notNull);
                }

                var redoFromStart = ctx.DefineLabel();
                ctx.MarkLabel(redoFromStart);

                // key = default(TKey); value = default(TValue);
                if (typeof(TKey) == typeof(string))
                {
                    ctx.LoadValue("");
                    ctx.StoreValue(key);
                }
                else
                {
                    ctx.InitLocal(typeof(TKey), key);
                }
                if (typeof(TValue) == typeof(string))
                {
                    ctx.LoadValue("");
                    ctx.StoreValue(value);
                }
                else
                {
                    ctx.InitLocal(typeof(TValue), @value);
                }

                // token = ProtoReader.StartSubItem(reader);
                ctx.LoadReader(true);
                ctx.EmitCall(typeof(ProtoReader).GetMethod("StartSubItem", Compiler.ReaderUtil.ReaderStateTypeArray));
                ctx.StoreValue(token);

                Compiler.CodeLabel @continue = ctx.DefineLabel(), processField = ctx.DefineLabel();
                // while ...
                ctx.Branch(@continue, false);

                // switch(fieldNumber)
                ctx.MarkLabel(processField);
                ctx.LoadValue(fieldNumber);
                CodeLabel @default = ctx.DefineLabel(), one = ctx.DefineLabel(), two = ctx.DefineLabel();
                ctx.Switch(new[] { @default, one, two }); // zero based, hence explicit 0

                // case 0: default: reader.SkipField();
                ctx.MarkLabel(@default);
                ctx.LoadReader(true);
                ctx.EmitCall(typeof(ProtoReader).GetMethod("SkipField", Compiler.ReaderUtil.StateTypeArray));
                ctx.Branch(@continue, false);

                // case 1: key = ...
                ctx.MarkLabel(one);
                keyTail.EmitRead(ctx, null);
                ctx.StoreValue(key);
                ctx.Branch(@continue, false);

                // case 2: value = ...
                ctx.MarkLabel(two);
                Tail.EmitRead(ctx, Tail.RequiresOldValue ? @value : null);
                ctx.StoreValue(value);

                // (fieldNumber = reader.ReadFieldHeader()) > 0
                ctx.MarkLabel(@continue);
                ctx.EmitBasicRead("ReadFieldHeader", typeof(int));
                ctx.CopyValue();
                ctx.StoreValue(fieldNumber);
                ctx.LoadValue(0);
                ctx.BranchIfGreater(processField, false);

                // ProtoReader.EndSubItem(token, reader);
                ctx.LoadValue(token);
                ctx.LoadReader(true);
                ctx.EmitCall(typeof(ProtoReader).GetMethod("EndSubItem",
                    new[] { typeof(SubItemToken), typeof(ProtoReader), Compiler.ReaderUtil.ByRefStateType }));

                // list[key] = value;
                ctx.LoadAddress(list, ExpectedType);
                ctx.LoadValue(key);
                ctx.LoadValue(@value);
                ctx.EmitCall(indexerSet);

                // while reader.TryReadFieldReader(fieldNumber)
                ctx.LoadReader(true);
                ctx.LoadValue(this.fieldNumber);
                ctx.EmitCall(typeof(ProtoReader).GetMethod("TryReadFieldHeader",
                    new[] { Compiler.ReaderUtil.ByRefStateType, typeof(int) }));
                ctx.BranchIfTrue(redoFromStart, false);

                if (ReturnsValue)
                {
                    ctx.LoadValue(list);
                }
            }
        }
    }
}
#pragma warning restore RCS1165