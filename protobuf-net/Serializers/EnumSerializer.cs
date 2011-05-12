#if !NO_RUNTIME
using System;
#if FEAT_COMPILER
using System.Reflection.Emit;
using ProtoBuf.Meta;

#endif


namespace ProtoBuf.Serializers
{

    sealed class EnumSerializer : IProtoSerializer
    {
        public struct EnumPair
        {
            public readonly Enum Value; // note that this is boxing, but I'll live with it
            public readonly int WireValue;
            public EnumPair(int wireValue, Enum value)
            {
                WireValue = wireValue;
                Value = value;
            }
        } 
        private readonly Type enumType; 
        private readonly EnumPair[] map;
        public EnumSerializer(Type enumType, EnumPair[] map)
        {
            if (enumType == null) throw new ArgumentNullException("enumType");
            this.enumType = enumType;
            this.map = map;
            if (map != null)
            {
                for (int i = 1; i < map.Length; i++)
                for (int j = 0 ; j < i ; j++)
                {
                    if (map[i].WireValue == map[j].WireValue && !Equals(map[i].Value,map[j].Value))
                    {
                        throw new ProtoException("Multiple enums with wire-value " + map[i].WireValue);
                    }
                    if (Equals(map[i].Value, map[j].Value) && map[i].WireValue != map[j].WireValue)
                    {
                        throw new ProtoException("Multiple enums with deserialized-value " + map[i].WireValue);
                    }
                }

            }
        }
        private TypeCode GetTypeCode() {
            return Type.GetTypeCode(Enum.GetUnderlyingType(enumType));
        }

        private int EnumToWire(object value)
        {
            checked
            {
                switch (GetTypeCode())
                { // unbox then convert to int
                    case TypeCode.Byte: return (int)(byte)value;
                    case TypeCode.SByte: return (int)(sbyte)value;
                    case TypeCode.Int16: return (int)(short)value;
                    case TypeCode.Int32: return (int)value;
                    case TypeCode.Int64: return (int)(long)value;
                    case TypeCode.UInt16: return (int)(ushort)value;
                    case TypeCode.UInt32: return (int)(uint)value;
                    case TypeCode.UInt64: return (int)(ulong)value;
                    default: throw new InvalidOperationException();
                }
            }
        }
        private object WireToEnum(int value)
        {
            checked
            {
                switch (GetTypeCode())
                { // convert from int then box 
                    case TypeCode.Byte: return Enum.ToObject(enumType, (byte)value);
                    case TypeCode.SByte: return Enum.ToObject(enumType, (sbyte)value);
                    case TypeCode.Int16: return Enum.ToObject(enumType, (short)value);
                    case TypeCode.Int32: return Enum.ToObject(enumType, value);
                    case TypeCode.Int64: return Enum.ToObject(enumType, (long)value);
                    case TypeCode.UInt16: return Enum.ToObject(enumType, (ushort)value);
                    case TypeCode.UInt32: return Enum.ToObject(enumType, (uint)value);
                    case TypeCode.UInt64: return Enum.ToObject(enumType, (ulong)value);
                    default: throw new InvalidOperationException();
                }
            }
        }

        public Type ExpectedType { get { return enumType; } }
        public void Write(object value, ProtoWriter dest)
        {
            if (map == null)
            {
                ProtoWriter.WriteInt32(EnumToWire(value), dest);
            }
            else
            {
                for (int i = 0; i < map.Length; i++) {
                    if (object.Equals(map[i].Value, value)) {
                        ProtoWriter.WriteInt32(map[i].WireValue, dest);
                        return;
                    }
                }
                ProtoWriter.ThrowEnumException(dest, value);
            }            
        }
        bool IProtoSerializer.RequiresOldValue { get { return false; } }
        bool IProtoSerializer.ReturnsValue { get { return true; } }
        public object Read(object value, ProtoReader source)
        {
            Helpers.DebugAssert(value == null); // since replaces
            int wireValue = source.ReadInt32();
            if(map == null) {
                return WireToEnum(wireValue);
            }
            for(int i = 0 ; i < map.Length ; i++) {
                if(map[i].WireValue == wireValue) {
                    return map[i].Value;
                }
            }
            source.ThrowEnumException(ExpectedType, wireValue);
            return null; // to make compiler happy
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            TypeCode typeCode = GetTypeCode();
            if (map == null)
            {
                ctx.LoadValue(valueFrom);
                ctx.ConvertToInt32(typeCode);
                ctx.EmitBasicWrite("WriteInt32", null);
            }
            else
            {
                using (Compiler.Local loc = ctx.GetLocalWithValue(ExpectedType, valueFrom))
                {
                    Compiler.CodeLabel @continue = ctx.DefineLabel();
                    for (int i = 0; i < map.Length; i++)
                    {
                        Compiler.CodeLabel tryNextValue = ctx.DefineLabel(), processThisValue = ctx.DefineLabel();
                        ctx.LoadValue(loc);
                        WriteEnumValue(ctx, typeCode, map[i].Value);
                        ctx.BranchIfEqual(processThisValue, true);
                        ctx.Branch(tryNextValue, true);
                        ctx.MarkLabel(processThisValue);
                        ctx.LoadValue(map[i].WireValue);
                        ctx.EmitBasicWrite("WriteInt32", null);
                        ctx.Branch(@continue, false);
                        ctx.MarkLabel(tryNextValue);
                    }
                    ctx.LoadReaderWriter();
                    ctx.LoadValue(loc);
                    ctx.CastToObject(ExpectedType);
                    ctx.EmitCall(typeof(ProtoWriter).GetMethod("ThrowEnumException"));
                    ctx.MarkLabel(@continue);
                }
            }
            
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            TypeCode typeCode = GetTypeCode();
            if (map == null)
            {
                ctx.EmitBasicRead("ReadInt32", typeof(int));
                ctx.ConvertFromInt32(typeCode);
            }
            else
            {
                int[] wireValues = new int[map.Length];
                object[] values = new object[map.Length];
                for (int i = 0; i < map.Length; i++)
                {
                    wireValues[i] = map[i].WireValue;
                    values[i] = map[i].Value;
                }
                using (Compiler.Local result = new Compiler.Local(ctx, ExpectedType))
                using (Compiler.Local wireValue = new Compiler.Local(ctx, typeof(int)))
                {
                    ctx.EmitBasicRead("ReadInt32", typeof(int));
                    ctx.StoreValue(wireValue);
                    Compiler.CodeLabel @continue = ctx.DefineLabel();
                    foreach (BasicList.Group group in BasicList.GetContiguousGroups(wireValues, values))
                    {
                        Compiler.CodeLabel tryNextGroup = ctx.DefineLabel();
                        int groupItemCount = group.Items.Count;
                        if (groupItemCount == 1)
                        {
                            // discreet group; use an equality test
                            ctx.LoadValue(wireValue);
                            ctx.LoadValue(group.First);
                            Compiler.CodeLabel processThisValue = ctx.DefineLabel();
                            ctx.BranchIfEqual(processThisValue, true);
                            ctx.Branch(tryNextGroup, false);
                            WriteEnumValue(ctx, typeCode, processThisValue, @continue, group.Items[0], @result);
                        }
                        else
                        {
                            // implement as a jump-table-based switch
                            ctx.LoadValue(wireValue);
                            ctx.LoadValue(group.First);
                            ctx.Subtract(); // jump-tables are zero-based
                            Compiler.CodeLabel[] jmp = new Compiler.CodeLabel[groupItemCount];
                            for (int i = 0; i < groupItemCount; i++) {
                                jmp[i] = ctx.DefineLabel();
                            }
                            ctx.Switch(jmp);
                            // write the default...
                            ctx.Branch(tryNextGroup, false);
                            for (int i = 0; i < groupItemCount; i++)
                            {
                                WriteEnumValue(ctx, typeCode, jmp[i], @continue, group.Items[i], @result);
                            }
                        }
                        ctx.MarkLabel(tryNextGroup);
                    }
                    // throw source.CreateEnumException(ExpectedType, wireValue);
                    ctx.LoadReaderWriter();
                    ctx.LoadValue(ExpectedType);
                    ctx.LoadValue(wireValue);
                    ctx.EmitCall(typeof(ProtoReader).GetMethod("ThrowEnumException"));
                    ctx.MarkLabel(@continue);
                    ctx.LoadValue(result);
                }
            }
        }
        private static void WriteEnumValue(Compiler.CompilerContext ctx, TypeCode typeCode, object value)
        {
            switch (typeCode)
            {
                case TypeCode.Byte: ctx.LoadValue((int)(byte)value); break;
                case TypeCode.SByte: ctx.LoadValue((int)(sbyte)value); break;
                case TypeCode.Int16: ctx.LoadValue((int)(short)value); break;
                case TypeCode.Int32: ctx.LoadValue((int)(int)value); break;
                case TypeCode.Int64: ctx.LoadValue((long)(long)value); break;
                case TypeCode.UInt16: ctx.LoadValue((int)(ushort)value); break;
                case TypeCode.UInt32: ctx.LoadValue((int)(uint)value); break;
                case TypeCode.UInt64: ctx.LoadValue((long)(ulong)value); break;
                default: throw new InvalidOperationException();
            }
        }
        private static void WriteEnumValue(Compiler.CompilerContext ctx, TypeCode typeCode, Compiler.CodeLabel handler, Compiler.CodeLabel @continue, object value, Compiler.Local local)
        {
            ctx.MarkLabel(handler);
            WriteEnumValue(ctx, typeCode, value);
            ctx.StoreValue(local);
            ctx.Branch(@continue, false); // "continue"
        }
#endif

    }
}
#endif