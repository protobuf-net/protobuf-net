#if !NO_RUNTIME
using System;
using System.Collections;
using System.Reflection;
using ProtoBuf.Meta;
using System.Diagnostics;

namespace ProtoBuf.Serializers
{
    sealed class ArrayDecorator : ProtoDecoratorBase
    {        
        public ArrayDecorator(IProtoSerializer tail) : base(tail) {
            Helpers.DebugAssert(Tail.ExpectedType != typeof(byte), "Should have used BlobSerializer");
            this.itemType = Tail.ExpectedType;
            this.arrayType = itemType.MakeArrayType();
            
        }
        readonly Type arrayType, itemType; // this is, for example, typeof(int[])
        public override Type ExpectedType { get { return arrayType; } }
        public override bool RequiresOldValue { get { return true; } }
        public override bool ReturnsValue { get { return true; } }

        protected override void EmitRead(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
        }

        protected override void EmitWrite(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            // int i and T[] arr
            using (Compiler.Local arr = ctx.GetLocalWithValue(arrayType, valueFrom))
            using (Compiler.Local i = new ProtoBuf.Compiler.Local(ctx, typeof(int)))
            {
                // i = 0
                ctx.LoadValue(0);
                ctx.StoreValue(i);

                // range test is last (to minimise branches)
                Compiler.CodeLabel loopTest = ctx.DefineLabel(), processItem = ctx.DefineLabel();
                ctx.Branch(loopTest, false);
                ctx.MarkLabel(processItem);

                // {...}
                ctx.LoadArrayValue(arr, i);
                ctx.WriteNullCheckedTail(itemType, Tail, null);

                // i++
                ctx.LoadValue(i);
                ctx.LoadValue(1);
                ctx.Add();
                ctx.StoreValue(i);

                // i < arr.Length
                ctx.MarkLabel(loopTest);
                ctx.LoadValue(i);
                ctx.LoadLength(arr);
                ctx.BranchIfLess(processItem, false);
            }

        }
        public override void Write(object value, ProtoWriter dest)
        {
            IList arr = (IList)value;
            int len = arr.Count;
            for (int i = 0; i < len; i++)
            {
                object obj = arr[i];
                if (obj == null) { throw new NullReferenceException(); }
                Tail.Write(obj, dest);
            }
        }
        public override object Read(object value, ProtoReader source)
        {
            int field = source.FieldNumber;
            BasicList list = new BasicList();
            do
            {
                list.Add(Tail.Read(null, source));
            } while (source.TryReadFieldHeader(field));

            // we know that "list" is non-null and at least 1 long; not so sure about "arr"
            return list.Combine((Array)value, itemType);
        }

    }
}
#endif