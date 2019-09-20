using System;
using System.Diagnostics;
using ProtoBuf.Internal;
using ProtoBuf.Meta;

namespace ProtoBuf.Serializers
{
    internal sealed class EnumSerializer : IRuntimeProtoSerializerNode
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
            Type type = Nullable.GetUnderlyingType(ExpectedType) ?? ExpectedType;
            return Helpers.GetTypeCode(type);
        }

        public Type ExpectedType { get; }

        bool IRuntimeProtoSerializerNode.RequiresOldValue => false;

        bool IRuntimeProtoSerializerNode.ReturnsValue => true;

        private int EnumToWire(object value)
        {
            unchecked
            {
                return (GetTypeCode()) switch
                { // unbox then convert to int
                    ProtoTypeCode.Byte => (int)(byte)value,
                    ProtoTypeCode.SByte => (int)(sbyte)value,
                    ProtoTypeCode.Int16 => (int)(short)value,
                    ProtoTypeCode.Int32 => (int)value,
                    ProtoTypeCode.Int64 => (int)(long)value,
                    ProtoTypeCode.UInt16 => (int)(ushort)value,
                    ProtoTypeCode.UInt32 => (int)(uint)value,
                    ProtoTypeCode.UInt64 => (int)(ulong)value,
                    _ => throw new InvalidOperationException(),
                };
            }
        }

        private object WireToEnum(int value)
        {
            unchecked
            {
                return (GetTypeCode()) switch
                { // convert from int then box 
                    ProtoTypeCode.Byte => Enum.ToObject(ExpectedType, (byte)value),
                    ProtoTypeCode.SByte => Enum.ToObject(ExpectedType, (sbyte)value),
                    ProtoTypeCode.Int16 => Enum.ToObject(ExpectedType, (short)value),
                    ProtoTypeCode.Int32 => Enum.ToObject(ExpectedType, value),
                    ProtoTypeCode.Int64 => Enum.ToObject(ExpectedType, (long)value),
                    ProtoTypeCode.UInt16 => Enum.ToObject(ExpectedType, (ushort)value),
                    ProtoTypeCode.UInt32 => Enum.ToObject(ExpectedType, (uint)value),
                    ProtoTypeCode.UInt64 => Enum.ToObject(ExpectedType, (ulong)value),
                    _ => throw new InvalidOperationException(),
                };
            }
        }

        public object Read(ProtoReader source, ref ProtoReader.State state, object value)
        {
            Debug.Assert(value == null); // since replaces
            int wireValue = state.ReadInt32();
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
            state.ThrowEnumException(ExpectedType, wireValue);
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

        void IRuntimeProtoSerializerNode.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ProtoTypeCode typeCode = GetTypeCode();
            if (map == null)
            {
                ctx.LoadValue(valueFrom);
                ctx.ConvertToInt32(typeCode, false);
                ctx.EmitBasicWrite("WriteInt32", null, this);
            }
            else
            {
                using Compiler.Local loc = ctx.GetLocalWithValue(ExpectedType, valueFrom);
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
                    ctx.EmitBasicWrite("WriteInt32", null, this);
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

        void IRuntimeProtoSerializerNode.EmitRead(Compiler.CompilerContext ctx, Compiler.Local entity)
        {
            ProtoTypeCode typeCode = GetTypeCode();
            if (map == null)
            {
                ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadInt32), typeof(int));
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
                using Compiler.Local result = new Compiler.Local(ctx, ExpectedType);
                using Compiler.Local wireValue = new Compiler.Local(ctx, typeof(int));
                ctx.EmitStateBasedRead(nameof(ProtoReader.State.ReadInt32), typeof(int));
                ctx.StoreValue(wireValue);
                Compiler.CodeLabel @continue = ctx.DefineLabel();
                foreach (var group in BasicList.GetContiguousGroups(wireValues, values))
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
                ctx.LoadState();
                ctx.LoadValue(ExpectedType);
                ctx.LoadValue(wireValue);
                ctx.EmitCall(typeof(ProtoReader.State).GetMethod(nameof(ProtoReader.State.ThrowEnumException),
                    new[] { typeof(Type), typeof(int) }));
                ctx.MarkLabel(@continue);
                ctx.LoadValue(result);
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
    }
}