namespace ProtoBuf.WellKnownTypes
{
    internal sealed class WellKnownSerializer :
        IProtoSerializer<Empty>,
        IProtoSerializer<Struct>,
        IProtoSerializer<ListValue>,
        IProtoSerializer<Value>,
        IProtoSerializer<DoubleValue>,
        IProtoSerializer<FloatValue>,
        IProtoSerializer<Int64Value>,
        IProtoSerializer<UInt64Value>,
        IProtoSerializer<Int32Value>,
        IProtoSerializer<UInt32Value>,
        IProtoSerializer<StringValue>,
        IProtoSerializer<BoolValue>,
        IProtoSerializer<BytesValue>
    {
        private WellKnownSerializer() { }
        public static readonly WellKnownSerializer Instance = new WellKnownSerializer();

        void IProtoSerializer<Empty>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, Empty obj)
        { }

        Empty IProtoSerializer<Empty>.Deserialize(ProtoReader reader, ref ProtoReader.State state, Empty value)
        {
            while (reader.ReadFieldHeader(ref state) != 0) reader.SkipField(ref state);
            return value;
        }

        void IProtoSerializer<DoubleValue>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, DoubleValue value)
        {
            var val = value.Value;
            if (val != 0)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Fixed64, writer, ref state);
                ProtoWriter.WriteDouble(val, writer, ref state);
            }
        }

        DoubleValue IProtoSerializer<DoubleValue>.Deserialize(ProtoReader reader, ref ProtoReader.State state, DoubleValue value)
        {
            int field;
            while((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch(field)
                {
                    case 1: value = reader.ReadDouble(ref state); break;
                    default: reader.SkipField(ref state); break;
                }
            }
            return value;
        }

        void IProtoSerializer<FloatValue>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, FloatValue value)
        {
            var val = value.Value;
            if (val != 0)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Fixed32, writer, ref state);
                ProtoWriter.WriteSingle(val, writer, ref state);
            }
        }

        FloatValue IProtoSerializer<FloatValue>.Deserialize(ProtoReader reader, ref ProtoReader.State state, FloatValue value)
        {
            int field;
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 1: value = reader.ReadSingle(ref state); break;
                    default: reader.SkipField(ref state); break;
                }
            }
            return value;
        }

        void IProtoSerializer<BoolValue>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, BoolValue value)
        {
            var val = value.Value;
            if (val)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
                ProtoWriter.WriteBoolean(val, writer, ref state);
            }
        }

        BoolValue IProtoSerializer<BoolValue>.Deserialize(ProtoReader reader, ref ProtoReader.State state, BoolValue value)
        {
            int field;
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 1: value = reader.ReadBoolean(ref state); break;
                    default: reader.SkipField(ref state); break;
                }
            }
            return value;
        }

        void IProtoSerializer<Int32Value>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, Int32Value value)
        {
            var val = value.Value;
            if (val != 0)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
                ProtoWriter.WriteInt32(val, writer, ref state);
            }
        }

        Int32Value IProtoSerializer<Int32Value>.Deserialize(ProtoReader reader, ref ProtoReader.State state, Int32Value value)
        {
            int field;
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 1: value = reader.ReadInt32(ref state); break;
                    default: reader.SkipField(ref state); break;
                }
            }
            return value;
        }

        void IProtoSerializer<UInt32Value>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, UInt32Value value)
        {
            var val = value.Value;
            if (val != 0)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
                ProtoWriter.WriteUInt32(val, writer, ref state);
            }
        }

        UInt32Value IProtoSerializer<UInt32Value>.Deserialize(ProtoReader reader, ref ProtoReader.State state, UInt32Value value)
        {
            int field;
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 1: value = reader.ReadUInt32(ref state); break;
                    default: reader.SkipField(ref state); break;
                }
            }
            return value;
        }

        void IProtoSerializer<Int64Value>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, Int64Value value)
        {
            var val = value.Value;
            if (val != 0)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
                ProtoWriter.WriteInt64(val, writer, ref state);
            }
        }

        Int64Value IProtoSerializer<Int64Value>.Deserialize(ProtoReader reader, ref ProtoReader.State state, Int64Value value)
        {
            int field;
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 1: value = reader.ReadInt64(ref state); break;
                    default: reader.SkipField(ref state); break;
                }
            }
            return value;
        }

        void IProtoSerializer<UInt64Value>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, UInt64Value value)
        {
            var val = value.Value;
            if (val != 0)
            {
                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
                ProtoWriter.WriteUInt64(val, writer, ref state);
            }
        }

        UInt64Value IProtoSerializer<UInt64Value>.Deserialize(ProtoReader reader, ref ProtoReader.State state, UInt64Value value)
        {
            int field;
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 1: value = reader.ReadUInt64(ref state); break;
                    default: reader.SkipField(ref state); break;
                }
            }
            return value;
        }

        void IProtoSerializer<StringValue>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, StringValue value)
        {
            var val = value.Value;
            if (!string.IsNullOrEmpty(value))
            {
                ProtoWriter.WriteFieldHeader(1, WireType.String, writer, ref state);
                ProtoWriter.WriteString(val, writer, ref state);
            }
        }

        StringValue IProtoSerializer<StringValue>.Deserialize(ProtoReader reader, ref ProtoReader.State state, StringValue value)
        {
            int field;
            while ((field = reader.ReadFieldHeader(ref state)) != 0)
            {
                switch (field)
                {
                    case 1: value = reader.ReadString(ref state); break;
                    default: reader.SkipField(ref state); break;
                }
            }
            return value;
        }

        void IProtoSerializer<Struct>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, Struct value)
        {
            if (value == null) return;
            foreach(var item in value.Fields)
            {

            }
        }

        Struct IProtoSerializer<Struct>.Deserialize(ProtoReader reader, ref ProtoReader.State state, Struct value)
        {
            throw new System.NotImplementedException();
        }
    }
}
