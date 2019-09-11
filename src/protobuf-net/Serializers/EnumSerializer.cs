#if !NO_RUNTIME
using System;
using ProtoBuf.Meta;
using System.Reflection;

namespace ProtoBuf.Serializers
{
    internal sealed class EnumSerializer : IProtoSerializer
    {
        public readonly struct EnumPair
        {
            public readonly object RawValue; // note that this is boxing, but I'll live with it
            public readonly Enum TypedValue; // note that this is boxing, but I'll live with it
            public readonly int WireValue;
            public EnumPair(int wireValue, object raw, Type type)
            {
                WireValue = wireValue;
                RawValue = raw;
                TypedValue = (Enum)Enum.ToObject(type, raw);
            }
        }
        private readonly EnumPair[] map;
        public EnumSerializer(Type enumType, EnumPair[] map)
        {
            ExpectedType = enumType ?? throw new ArgumentNullException(nameof(enumType));
            this.map = map;
            if (map != null)
            {
                for (int i = 1; i < map.Length; i++)
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (map[i].WireValue == map[j].WireValue && !Equals(map[i].RawValue, map[j].RawValue))
                        {
                            throw new ProtoException("Multiple enums with wire-value " + map[i].WireValue.ToString());
                        }
                        if (Equals(map[i].RawValue, map[j].RawValue) && map[i].WireValue != map[j].WireValue)
                        {
                            throw new ProtoException("Multiple enums with deserialized-value " + map[i].RawValue);
                        }
                    }
                }
            }
        }

        private ProtoTypeCode GetTypeCode()
        {
            Type type = Helpers.GetUnderlyingType(ExpectedType) ?? ExpectedType;
            return Helpers.GetTypeCode(type);
        }

        public Type ExpectedType { get; }

        bool IProtoSerializer.RequiresOldValue => false;

        bool IProtoSerializer.ReturnsValue => true;

        private int EnumToWire(object value)
        {
            unchecked
            {
                switch (GetTypeCode())
                { // unbox then convert to int
                    case ProtoTypeCode.Byte: return (int)(byte)value;
                    case ProtoTypeCode.SByte: return (int)(sbyte)value;
                    case ProtoTypeCode.Int16: return (int)(short)value;
                    case ProtoTypeCode.Int32: return (int)value;
                    case ProtoTypeCode.Int64: return (int)(long)value;
                    case ProtoTypeCode.UInt16: return (int)(ushort)value;
                    case ProtoTypeCode.UInt32: return (int)(uint)value;
                    case ProtoTypeCode.UInt64: return (int)(ulong)value;
                    default: throw new InvalidOperationException();
                }
            }
        }

        private object WireToEnum(int value)
        {
            unchecked
            {
                switch (GetTypeCode())
                { // convert from int then box 
                    case ProtoTypeCode.Byte: return Enum.ToObject(ExpectedType, (byte)value);
                    case ProtoTypeCode.SByte: return Enum.ToObject(ExpectedType, (sbyte)value);
                    case ProtoTypeCode.Int16: return Enum.ToObject(ExpectedType, (short)value);
                    case ProtoTypeCode.Int32: return Enum.ToObject(ExpectedType, value);
                    case ProtoTypeCode.Int64: return Enum.ToObject(ExpectedType, (long)value);
                    case ProtoTypeCode.UInt16: return Enum.ToObject(ExpectedType, (ushort)value);
                    case ProtoTypeCode.UInt32: return Enum.ToObject(ExpectedType, (uint)value);
                    case ProtoTypeCode.UInt64: return Enum.ToObject(ExpectedType, (ulong)value);
                    default: throw new InvalidOperationException();
                }
            }
        }

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Helpers.DebugAssert(value == null); // since replaces
            int wireValue = source.ReadInt32(ref state);
            if (map == null)
            {
                return WireToEnum(wireValue);
            }
            for (int i = 0; i < map.Length; i++)
            {
                if (map[i].WireValue == wireValue)
                {
                    return map[i].TypedValue;
                }
            }
            source.ThrowEnumException(ref state, ExpectedType, wireValue);
            return null; // to make compiler happy
        }

        public void Write(ProtoWriter dest, ref ProtoWriter.State state, object value)
        {
            if (map == null)
            {
                ProtoWriter.WriteInt32(EnumToWire(value), dest, ref state);
            }
            else
            {
                for (int i = 0; i < map.Length; i++)
                {
                    if (object.Equals(map[i].TypedValue, value))
                    {
                        ProtoWriter.WriteInt32(map[i].WireValue, dest, ref state);
                        return;
                    }
                }
                ProtoWriter.ThrowEnumException(dest, value);
            }
        }

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ProtoTypeCode typeCode = GetTypeCode();
            if (map == null)
            {
                ctx.LoadValue(valueFrom);
                ctx.ConvertToInt32(typeCode, false);
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
                        WriteEnumValue(ctx, typeCode, map[i].RawValue);
                        ctx.BranchIfEqual(processThisValue, true);
                        ctx.Branch(tryNextValue, true);
                        ctx.MarkLabel(processThisValue);
                        ctx.LoadValue(map[i].WireValue);
                        ctx.EmitBasicWrite("WriteInt32", null);
                        ctx.Branch(@continue, false);
                        ctx.MarkLabel(tryNextValue);
                    }
                    ctx.LoadWriter(false);
                    ctx.LoadValue(loc);
                    ctx.CastToObject(ExpectedType);
                    ctx.EmitCall(typeof(ProtoWriter).GetMethod("ThrowEnumException"));
                    ctx.MarkLabel(@continue);
                }
            }
        }

        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ProtoTypeCode typeCode = GetTypeCode();
            if (map == null)
            {
                ctx.EmitBasicRead("ReadInt32", typeof(int));
                ctx.ConvertFromInt32(typeCode, false);
            }
            else
            {
                int[] wireValues = new int[map.Length];
                object[] values = new object[map.Length];
                for (int i = 0; i < map.Length; i++)
                {
                    wireValues[i] = map[i].WireValue;
                    values[i] = map[i].RawValue;
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
                            for (int i = 0; i < groupItemCount; i++)
                            {
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
                    ctx.LoadReader(true);
                    ctx.LoadValue(ExpectedType);
                    ctx.LoadValue(wireValue);
                    ctx.EmitCall(typeof(ProtoReader).GetMethod("ThrowEnumException",
                        new[] { Compiler.ReaderUtil.ByRefStateType, typeof(Type), typeof(int) }));
                    ctx.MarkLabel(@continue);
                    ctx.LoadValue(result);
                }
            }
        }
        private static void WriteEnumValue(Compiler.CompilerContext ctx, ProtoTypeCode typeCode, object value)
        {
            switch (typeCode)
            {
                case ProtoTypeCode.Byte: ctx.LoadValue((int)(byte)value); break;
                case ProtoTypeCode.SByte: ctx.LoadValue((int)(sbyte)value); break;
                case ProtoTypeCode.Int16: ctx.LoadValue((int)(short)value); break;
                case ProtoTypeCode.Int32: ctx.LoadValue((int)(int)value); break;
                case ProtoTypeCode.Int64: ctx.LoadValue((long)(long)value); break;
                case ProtoTypeCode.UInt16: ctx.LoadValue((int)(ushort)value); break;
                case ProtoTypeCode.UInt32: ctx.LoadValue((int)(uint)value); break;
                case ProtoTypeCode.UInt64: ctx.LoadValue((long)(ulong)value); break;
                default: throw new InvalidOperationException();
            }
        }
        private static void WriteEnumValue(Compiler.CompilerContext ctx, ProtoTypeCode typeCode, Compiler.CodeLabel handler, Compiler.CodeLabel @continue, object value, Compiler.Local local)
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