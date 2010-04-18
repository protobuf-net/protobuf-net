#if !NO_RUNTIME
using System;
using System.Reflection.Emit;



namespace ProtoBuf.Serializers
{
    sealed class EnumSerializer : IProtoSerializer
    {
        public struct EnumPair
        {
            public readonly Enum Value; // note that this is boxing, but I'll live with it
            public readonly int WireValue;
        }
        private readonly Type enumType; 
        private readonly EnumPair[] map;
        public EnumSerializer(Type enumType, EnumPair[] map)
        {
            if (enumType == null) throw new ArgumentNullException("enumType");
            this.enumType = enumType;
            this.map = map;
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
                    if (object.Equals(map[i].WireValue, value)) {
                        ProtoWriter.WriteInt32(map[i].WireValue, dest);
                        return;
                    }
                }
                throw new InvalidOperationException("Enum value not mapped:" + value.ToString());
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
            throw new InvalidOperationException("Enum value not mapped:" + wireValue.ToString());
        }
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (map == null)
            {
                ctx.LoadValue(valueFrom);
                ctx.ConvertToInt32(GetTypeCode());
                ctx.EmitBasicWrite("WriteInt32", null);
            }
            else
            {
                throw new NotImplementedException();
                //ctx.EmitBasicWrite("WriteInt32", valueFrom);
            }
            
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (map == null)
            {
                ctx.EmitBasicRead("ReadInt32", typeof(int));
                ctx.ConvertFromInt32(GetTypeCode());
            }
            else
            {
                throw new NotImplementedException();
                //ctx.EmitBasicRead("ReadInt32", ExpectedType);
            }
        }
#endif

    }
}
#endif