#if !NO_RUNTIME
using System;
using ProtoBuf.Meta;
using System.Reflection.Emit;


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
            // write inheritance first
            Type actualType = value.GetType();
            if (actualType != forType)
            {
                for (int i = 0; i < serializers.Length; i++)
                {
                    IProtoSerializer ser = serializers[i];
                    if (ser.ExpectedType != forType && actualType.IsAssignableFrom(ser.ExpectedType))
                    {
                        ser.Write(value, dest);
                        break;
                    }
                }
            }

            // write all actual fields
            for (int i = 0; i < serializers.Length; i++)
            {
                IProtoSerializer ser = serializers[i];
                if(ser.ExpectedType == forType) ser.Write(value, dest);
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
                        IProtoSerializer ser = serializers[i];
                        if (value == null && ser.ExpectedType == forType) value = CreateInstance();
                        if (ser.ReturnsValue) {
                            value = ser.Read(value, source);
                        } else { // pop
                            ser.Read(value, source);
                        }
                        
                        lastFieldIndex = i;
                        lastFieldNumber = fieldNumber;
                        fieldHandled = true;
                        break;
                    }
                }
                if (!fieldHandled)
                {
                    if (value == null) value = CreateInstance();
                    source.SkipField();
                }
            }
            return value ?? CreateInstance();
        }

        object CreateInstance()
        {
            return Activator.CreateInstance(forType);
        }

        bool IProtoSerializer.RequiresOldValue { get { return true; } }
        bool IProtoSerializer.ReturnsValue { get { return false; } } // updates field directly
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type expected = ExpectedType;
            using (Compiler.Local loc = ctx.GetLocalWithValue(expected, valueFrom))
            {
                Compiler.CodeLabel startFields = ctx.DefineLabel();
                for (int i = 0; i < serializers.Length; i++)
                {
                    IProtoSerializer ser = serializers[i];
                    if (ser.ExpectedType != forType)
                    {
                        Compiler.CodeLabel ifMatch = ctx.DefineLabel(), nextTest = ctx.DefineLabel();
                        ctx.LoadValue(loc);
                        ctx.TryCast(ser.ExpectedType);
                        ctx.CopyValue();
                        ctx.BranchIfTrue(ifMatch, true);
                        ctx.DiscardValue();
                        ctx.Branch(nextTest, true);
                        ctx.MarkLabel(ifMatch);
                        ser.EmitWrite(ctx, null);
                        ctx.Branch(startFields, false);
                        ctx.MarkLabel(nextTest);
                    }
                }
                ctx.MarkLabel(startFields);                
                for (int i = 0; i < serializers.Length; i++)
                {
                    IProtoSerializer ser = serializers[i];
                    if(ser.ExpectedType == forType) ser.EmitWrite(ctx, loc);
                }
            }
        }
        class Group
        {
            public readonly int First;
            public readonly BasicList Items;
            public Group(int first)
            {
                this.First = first;
                this.Items = new BasicList();
            }
        }
        BasicList GetContiguousGroups()
        {
            BasicList outer = new BasicList();
            Group group = null;
            int lastIndex = 0;
            for (int i = 0; i < fieldNumbers.Length; i++)
            {
                if (fieldNumbers[i] != lastIndex + 1) { group = null; }
                if (group == null)
                {
                    group = new Group(fieldNumbers[i]);
                    outer.Add(group);
                }
                lastIndex = fieldNumbers[i];
                group.Items.Add(serializers[i]);
            }
            return outer;
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type expected = ExpectedType;
            Helpers.DebugAssert(valueFrom != null);

            using (Compiler.Local loc = ctx.GetLocalWithValue(expected, valueFrom))
            using (Compiler.Local fieldNumber = new Compiler.Local(ctx, typeof(int)))
            {
                Compiler.CodeLabel @continue = ctx.DefineLabel(), processField = ctx.DefineLabel();
                ctx.Branch(@continue, false);

                ctx.MarkLabel(processField);
                foreach (Group group in GetContiguousGroups())
                {
                    Compiler.CodeLabel tryNextField = ctx.DefineLabel();
                    int groupItemCount = group.Items.Count;
                    if (groupItemCount == 1)
                    {
                        // discreet group; use an equality test
                        ctx.LoadValue(fieldNumber);
                        ctx.LoadValue(group.First);
                        Compiler.CodeLabel processThisField = ctx.DefineLabel();
                        ctx.BranchIfEqual(processThisField, true);
                        ctx.Branch(tryNextField, false);
                        WriteFieldHandler(ctx, expected, loc, processThisField, @continue, (IProtoSerializer)group.Items[0]);
                    }
                    else
                    {   // implement as a jump-table-based switch
                        ctx.LoadValue(fieldNumber);
                        ctx.LoadValue(group.First);
                        ctx.Subtract(); // jump-tables are zero-based
                        Compiler.CodeLabel[] jmp = new Compiler.CodeLabel[groupItemCount];
                        for (int i = 0; i < groupItemCount; i++) {
                            jmp[i] = ctx.DefineLabel();
                        }
                        ctx.Switch(jmp);
                        // write the default...
                        ctx.Branch(tryNextField, false);
                        for (int i = 0; i < groupItemCount; i++)
                        {
                            WriteFieldHandler(ctx, expected, loc, jmp[i], @continue, (IProtoSerializer)group.Items[i]);
                        }
                    }
                    ctx.MarkLabel(tryNextField);
                }

                CreateIfNull(ctx, expected, loc);
                ctx.LoadReaderWriter();
                ctx.EmitCall(typeof(ProtoReader).GetMethod("SkipField"));
                
                ctx.MarkLabel(@continue);
                ctx.EmitBasicRead("ReadFieldHeader", typeof(int));
                ctx.CopyValue();
                ctx.StoreValue(fieldNumber);
                ctx.LoadValue(0);
                ctx.BranchIfGreater(processField, false);

                CreateIfNull(ctx, expected, loc);
            }
        }

        private void WriteFieldHandler(
            Compiler.CompilerContext ctx, Type expected, Compiler.Local loc,
            Compiler.CodeLabel handler, Compiler.CodeLabel @continue, IProtoSerializer serializer)
        {
            ctx.MarkLabel(handler);
            if (serializer.ExpectedType == forType) {
                CreateIfNull(ctx, expected, loc);
                serializer.EmitRead(ctx, loc);
            }
            else {
                ctx.LoadValue(loc);
                ctx.Cast(serializer.ExpectedType);
                serializer.EmitRead(ctx, null);                
            }
            
            if (serializer.ReturnsValue)
            {   // update the variable
                ctx.StoreValue(loc);
            }
            ctx.Branch(@continue, false); // "continue"
        }
        

        private static void CreateIfNull(Compiler.CompilerContext ctx, Type type, Compiler.Local storage)
        {
            Helpers.DebugAssert(storage != null);
            if (!type.IsValueType)
            {
                Compiler.CodeLabel afterNullCheck = ctx.DefineLabel(),
                    needToCreate = ctx.DefineLabel();
                ctx.LoadValue(storage);
                ctx.BranchIfTrue(afterNullCheck, true);
                ctx.EmitCtor(type);
                ctx.StoreValue(storage);                
                ctx.MarkLabel(afterNullCheck);
            }
        }
#endif
    }

}
#endif