#if !NO_RUNTIME
using System;


namespace ProtoBuf.Serializers
{
    sealed class TypeSerializer : IProtoSerializer
    {
        private readonly Type forType;
        public Type ExpectedType { get { return forType; } }
        private readonly IProtoSerializer[] serializers;
        private readonly int[] fieldNumbers;
        public TypeSerializer(Type forType, int[] fieldNumbers, IProtoSerializer[] serializers)
        {
            Helpers.DebugAssert(forType != null);
            Helpers.DebugAssert(fieldNumbers != null);
            Helpers.DebugAssert(serializers != null);
            Helpers.DebugAssert(fieldNumbers.Length == serializers.Length);
            Helpers.Sort(fieldNumbers, serializers);
            this.forType = forType;
            this.serializers = serializers;
            this.fieldNumbers = fieldNumbers;
#if !NO_GENERICS
            if (Nullable.GetUnderlyingType(forType) != null)
            {
                throw new ArgumentException("Cannot create a TypeSerializer for nullable types", "forType");
            }
#endif
        }
        
        public void Write(object value, ProtoWriter dest)
        {
            // write all suitable fields
            for (int i = 0; i < serializers.Length; i++)
            {
                serializers[i].Write(value, dest);
            }
        }

        public object Read(object value, ProtoReader source)
        {
            int fieldNumber, lastFieldNumber = 0, lastFieldIndex = 0;
            bool fieldHandled;
            while ((fieldNumber = source.ReadFieldHeader()) > 0)
            {
                fieldHandled = false;
                if (fieldNumber < lastFieldNumber)
                {
                    lastFieldNumber = lastFieldIndex = 0;
                }
                for (int i = lastFieldIndex; i < fieldNumbers.Length; i++)
                {
                    if (fieldNumbers[i] == fieldNumber)
                    {
                        if (value == null) value = Activator.CreateInstance(forType);
                        if (serializers[i].ReturnsValue) {
                            value = serializers[i].Read(value, source);
                        } else { // pop
                            serializers[i].Read(value, source);
                        }
                        
                        lastFieldIndex = i;
                        lastFieldNumber = fieldNumber;
                        fieldHandled = true;
                        break;
                    }
                }
                if (!fieldHandled)
                {
                    if (value == null) value = Activator.CreateInstance(forType);
                    source.SkipField();
                }
            }
            return value;
        }

        bool IProtoSerializer.RequiresOldValue { get { return true; } }
        bool IProtoSerializer.ReturnsValue { get { return false; } } // updates field directly
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type expected = ExpectedType;
            using (Compiler.Local loc = ctx.GetLocalWithValue(expected, valueFrom))
            {
                for (int i = 0; i < serializers.Length; i++)
                {
                    serializers[i].EmitWrite(ctx, loc);
                }
            }
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type expected = ExpectedType;
            Helpers.DebugAssert(valueFrom != null);
            using (Compiler.Local loc = ctx.GetLocalWithValue(expected, valueFrom))
            using (Compiler.Local fieldNumber = new Compiler.Local(ctx, typeof(int)))
            {
                Compiler.CodeLabel nextField = ctx.DefineLabel(), processField = ctx.DefineLabel();
                ctx.Branch(nextField);

                ctx.MarkLabel(processField);

                for (int i = 0; i < fieldNumbers.Length; i++)
                {
                    ctx.LoadValue(fieldNumber);
                    ctx.LoadValue(fieldNumbers[i]);
                    Compiler.CodeLabel processThisField = ctx.DefineLabel(),
                        tryNextField = ctx.DefineLabel();
                    ctx.BranchIfEqual(processThisField);
                    ctx.Branch(tryNextField);

                    ctx.MarkLabel(processThisField);

                    CreateIfNull(ctx, expected, loc);
                    serializers[i].EmitRead(ctx, loc);
                    if (serializers[i].ReturnsValue)
                    {   // update the variable
                        ctx.StoreValue(loc);
                    }
                    ctx.Branch(nextField);
                    ctx.MarkLabel(tryNextField);
                }

                CreateIfNull(ctx, expected, loc);
                ctx.LoadReaderWriter();
                ctx.EmitCall(typeof(ProtoReader).GetMethod("SkipField"));
                
                ctx.MarkLabel(nextField);
                ctx.EmitBasicRead("ReadFieldHeader", typeof(int));
                ctx.CopyValue();
                ctx.StoreValue(fieldNumber);
                ctx.LoadValue(0);
                ctx.BranchIfGreater(processField);

                //ctx.LoadValue(loc);
            }
        }

        private static void CreateIfNull(Compiler.CompilerContext ctx, Type type, Compiler.Local storage)
        {
            Helpers.DebugAssert(storage != null);
            if (!type.IsValueType)
            {
                Compiler.CodeLabel afterNullCheck = ctx.DefineLabel(),
                    needToCreate = ctx.DefineLabel();
                ctx.LoadValue(storage);
                ctx.LoadNull();
                ctx.BranchIfEqual(needToCreate);
                ctx.Branch(afterNullCheck);
                ctx.MarkLabel(needToCreate);
                ctx.EmitCtor(type);
                ctx.StoreValue(storage);
                ctx.MarkLabel(afterNullCheck);
            }
        }
#endif
    }

}
#endif