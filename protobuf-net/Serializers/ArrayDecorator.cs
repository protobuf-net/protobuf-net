#if !NO_RUNTIME
using System;
using System.Collections;
using ProtoBuf.Compiler;
using ProtoBuf.Meta;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    sealed class ArrayDecorator : ProtoDecoratorBase
    {
        private readonly int packedFieldNumber;
        private readonly WireType packedWireType;
        public ArrayDecorator(IProtoSerializer tail, int packedFieldNumber, WireType packedWireType)
            : base(tail)
        {
            Helpers.DebugAssert(Tail.ExpectedType != typeof(byte), "Should have used BlobSerializer");
            this.packedWireType = WireType.None;
            if (packedFieldNumber != 0)
            {
                if (packedFieldNumber < 0) throw new ArgumentOutOfRangeException("packedFieldNumber");
                switch (packedWireType)
                {
                    case WireType.Fixed32:
                    case WireType.Fixed64:
                    case WireType.SignedVariant:
                    case WireType.Variant:
                        break;
                    default:
                        throw new ArgumentException("Packed buffers are not supported for wire-type: " + packedWireType, "packedFieldNumber");
                }
                this.packedFieldNumber = packedFieldNumber;
                this.packedWireType = packedWireType;
            }
            this.itemType = Tail.ExpectedType;
            this.arrayType = Helpers.MakeArrayType(itemType);
        }
        readonly Type arrayType, itemType; // this is, for example, typeof(int[])
        public override Type ExpectedType { get { return arrayType; } }
        public override bool RequiresOldValue { get { return true; } }
        public override bool ReturnsValue { get { return true; } }
#if FEAT_COMPILER
        protected override void EmitWrite(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            // int i and T[] arr
            using (Compiler.Local arr = ctx.GetLocalWithValue(arrayType, valueFrom))
            using (Compiler.Local i = new ProtoBuf.Compiler.Local(ctx, typeof(int)))
            {
                if (packedFieldNumber > 0)
                {
                    Compiler.CodeLabel allDone = ctx.DefineLabel();
                    ctx.LoadLength(arr, false);
                    ctx.BranchIfFalse(allDone, false);

                    ctx.LoadValue(packedFieldNumber);
                    ctx.LoadValue((int)WireType.String);
                    ctx.LoadReaderWriter();
                    ctx.EmitCall(typeof(ProtoWriter).GetMethod("WriteFieldHeader"));

                    ctx.LoadValue(arr);
                    ctx.LoadReaderWriter();
                    ctx.EmitCall(typeof(ProtoWriter).GetMethod("StartSubItem"));
                    using (Compiler.Local token = new Compiler.Local(ctx, typeof(SubItemToken))) { 
                        ctx.StoreValue(token);

                        ctx.LoadValue(packedFieldNumber);
                        ctx.LoadReaderWriter();
                        ctx.EmitCall(typeof(ProtoWriter).GetMethod("SetPackedField"));

                        EmitWriteArrayLoop(ctx, i, arr);

                        ctx.LoadValue(token);
                        ctx.LoadReaderWriter();
                        ctx.EmitCall(typeof(ProtoWriter).GetMethod("EndSubItem"));
                    }
                    ctx.MarkLabel(allDone);
                }
                else
                {
                    EmitWriteArrayLoop(ctx, i, arr);    
                }
            }
        }

        private void EmitWriteArrayLoop(CompilerContext ctx, Local i, Local arr)
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
            ctx.LoadLength(arr, false);
            ctx.BranchIfLess(processItem, false);
        }
#endif
        public override void Write(object value, ProtoWriter dest)
        {
            IList arr = (IList)value;
            int len = arr.Count;
            if (packedFieldNumber > 0)
            {
                if (len != 0)
                {
                    ProtoWriter.WriteFieldHeader(packedFieldNumber, WireType.String, dest);
                    SubItemToken token = ProtoWriter.StartSubItem(value, dest);
                    ProtoWriter.SetPackedField(packedFieldNumber, dest);
                    for (int i = 0; i < len; i++)
                    {
                        object obj = arr[i];
                        if (obj == null) { throw new NullReferenceException(); }
                        Tail.Write(obj, dest);
                    }
                    ProtoWriter.EndSubItem(token, dest);
                }
            }
            else { 
                for (int i = 0; i < len; i++)
                {
                    object obj = arr[i];
                    if (obj == null) { throw new NullReferenceException(); }
                    Tail.Write(obj, dest);
                }
            }
        }
        public override object Read(object value, ProtoReader source)
        {
            int field = source.FieldNumber;
            BasicList list = new BasicList();
            if (packedWireType != WireType.None && source.WireType == WireType.String)
            {
                SubItemToken token = ProtoReader.StartSubItem(source);
                while (ProtoReader.HasSubValue(packedWireType, source))
                {
                    list.Add(Tail.Read(null, source));
                }
                ProtoReader.EndSubItem(token, source);
            }
            else
            { 
                do
                {
                    list.Add(Tail.Read(null, source));
                } while (source.TryReadFieldHeader(field));
            }
            int oldLen = (value == null ? 0 : ((Array)value).Length);
            Array result = Array.CreateInstance(itemType, oldLen + list.Count);
            if (oldLen != 0) ((Array)value).CopyTo(result, 0);
            list.CopyTo(result, oldLen);
            return result;
        }
#if FEAT_COMPILER
        protected override void EmitRead(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            Type listType;
#if NO_GENERICS
            listType = typeof(BasicList);
#else
            listType = typeof(System.Collections.Generic.List<>).MakeGenericType(itemType);
#endif
            using (Compiler.Local oldArr = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            using (Compiler.Local newArr = new Compiler.Local(ctx, ExpectedType))
            using (Compiler.Local list = new Compiler.Local(ctx, listType))
            {
                ctx.EmitCtor(listType);
                ctx.StoreValue(list);
                ListDecorator.EmitReadList(ctx, list, Tail, listType.GetMethod("Add"), packedWireType);

                // leave this "using" here, as it can share the "FieldNumber" local with EmitReadList
                using(Compiler.Local oldLen = new ProtoBuf.Compiler.Local(ctx, typeof(int))) {
                    ctx.LoadLength(oldArr, true);
                    ctx.CopyValue();
                    ctx.StoreValue(oldLen);
                    ctx.LoadAddress(list, listType);
                    ctx.LoadValue(listType.GetProperty("Count"));
                    ctx.Add();
                    ctx.CreateArray(itemType, null); // length is on the stack
                    ctx.StoreValue(newArr);

                    ctx.LoadValue(oldLen);
                    Compiler.CodeLabel nothingToCopy = ctx.DefineLabel();
                    ctx.BranchIfFalse(nothingToCopy, true);
                    ctx.LoadValue(oldArr);
                    ctx.LoadValue(newArr);
                    ctx.LoadValue(oldLen);
                    Type[] argTypes = new Type[] {typeof(Array), typeof(int)};
                    ctx.EmitCall(ExpectedType.GetMethod("CopyTo",argTypes));
                    ctx.MarkLabel(nothingToCopy);

                    ctx.LoadValue(list);
                    ctx.LoadValue(newArr);
                    ctx.LoadValue(oldLen);
                    argTypes[0] = ExpectedType; // // prefer: CopyTo(T[], int)
                    MethodInfo copyTo = listType.GetMethod("CopyTo", argTypes);
                    if (copyTo == null)
                    { // fallback: CopyTo(Array, int)
                        argTypes[1] = typeof(Array);
                        copyTo = listType.GetMethod("CopyTo", argTypes);
                    }
                    ctx.EmitCall(copyTo);
                }
                ctx.LoadValue(newArr);
            }


        }
#endif
    }
}
#endif