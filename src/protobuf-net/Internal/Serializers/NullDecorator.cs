#if FEAT_NULL_LIST_ITEMS
using System;

namespace ProtoBuf.Internal.Serializers
{
    internal sealed class NullDecorator : ProtoDecoratorBase
    {
        public const int Tag = 1;
        public NullDecorator(IRuntimeProtoSerializerNode tail) : base(tail)
        {
            if (!tail.ReturnsValue)
                throw new NotSupportedException("NullDecorator only supports implementations that return values");

            Type tailType = tail.ExpectedType;
            if (tailType.IsValueType)
            {
                ExpectedType = typeof(Nullable<>).MakeGenericType(tailType);
            }
            else
            {
                ExpectedType = tailType;
            }
        }

        public override Type ExpectedType { get; }

        public override bool ReturnsValue => true;

        public override bool RequiresOldValue => true;

        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using Compiler.Local oldValue = ctx.GetLocalWithValue(ExpectedType, valueFrom);
            using Compiler.Local token = new Compiler.Local(ctx, typeof(SubItemToken));
            using Compiler.Local field = new Compiler.Local(ctx, typeof(int));
            ctx.LoadState();
            ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.StartSubItem), Type.EmptyTypes));
            ctx.StoreValue(token);

            Compiler.CodeLabel next = ctx.DefineLabel(), processField = ctx.DefineLabel(), end = ctx.DefineLabel();

            ctx.MarkLabel(next);

            ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadFieldHeader), typeof(int));
            ctx.CopyValue();
            ctx.StoreValue(field);
            ctx.LoadValue(Tag); // = 1 - process
            ctx.BranchIfEqual(processField, true);
            ctx.LoadValue(field);
            ctx.LoadValue(1); // < 1 - exit
            ctx.BranchIfLess(end, false);

            // default: skip
            ctx.LoadState();
            ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.SkipField), Type.EmptyTypes));
            ctx.Branch(next, true);

            // process
            ctx.MarkLabel(processField);
            if (Tail.RequiresOldValue)
            {
                if (ExpectedType.IsValueType)
                {
                    ctx.LoadAddress(oldValue, ExpectedType);
                    ctx.EmitCall(ExpectedType.GetMethod("GetValueOrDefault", Type.EmptyTypes));
                }
                else
                {
                    ctx.LoadValue(oldValue);
                }
            }
            Tail.EmitRead(ctx, null);
            // note we demanded always returns a value
            if (ExpectedType.IsValueType)
            {
                ctx.EmitCtor(ExpectedType, Tail.ExpectedType); // re-nullable<T> it
            }
            ctx.StoreValue(oldValue);
            ctx.Branch(next, false);

            // outro
            ctx.MarkLabel(end);

            ctx.LoadState();
            ctx.LoadValue(token);
            ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.EndSubItem),
                new[] { typeof(SubItemToken) }));
            ctx.LoadValue(oldValue); // load the old value
        }
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (Tail is TagDecorator td && Tail.ExpectedType == this.ExpectedType && td.CanEmitDirectWrite())
            {
                td.EmitDirectWrite(ctx, valueFrom);
            }
            else
            {
                using Compiler.Local valOrNull = ctx.GetLocalWithValue(ExpectedType, valueFrom);
                using Compiler.Local token = new Compiler.Local(ctx, typeof(SubItemToken));
                ctx.LoadState();
                ctx.LoadNullRef();
                ctx.EmitCall(typeof(ProtoWriter.State).GetMethod(nameof(ProtoWriter.State.StartSubItem), new Type[] { typeof(object) }));
                ctx.StoreValue(token);

                if (ExpectedType.IsValueType)
                {
                    ctx.LoadAddress(valOrNull, ExpectedType);
                    ctx.LoadValue(ExpectedType.GetProperty("HasValue"));
                }
                else
                {
                    ctx.LoadValue(valOrNull);
                }
                Compiler.CodeLabel @end = ctx.DefineLabel();
                ctx.BranchIfFalse(@end, false);
                if (ExpectedType.IsValueType)
                {
                    ctx.LoadAddress(valOrNull, ExpectedType);
                    ctx.EmitCall(ExpectedType.GetMethod("GetValueOrDefault", Type.EmptyTypes));
                }
                else
                {
                    ctx.LoadValue(valOrNull);
                }
                Tail.EmitWrite(ctx, null);

                ctx.MarkLabel(@end);

                ctx.EmitStateBasedWrite(nameof(ProtoWriter.State.EndSubItem), token);
            }
        }

        public override object Read(ref ProtoReader.State state, object value)
        {
            SubItemToken tok = state.StartSubItem();
            int field;
            while ((field = state.ReadFieldHeader()) > 0)
            {
                if (field == Tag)
                {
                    value = Tail.Read(ref state, value);
                }
                else
                {
                    state.SkipField();
                }
            }
            state.EndSubItem(tok);
            return value;
        }

        public override void Write(ref ProtoWriter.State state, object value)
        {
            SubItemToken token = state.StartSubItem(null);
            if (value is object)
            {
                Tail.Write(ref state, value);
            }
            state.EndSubItem(token);
        }
    }
}
#endif