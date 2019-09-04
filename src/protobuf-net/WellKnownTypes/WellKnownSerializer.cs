//namespace ProtoBuf.WellKnownTypes
//{
//    internal sealed class WellKnownSerializer :
//        IProtoSerializer<Empty>,
//        IProtoSerializer<Struct>,
//        IProtoSerializer<ListValue>,
//        IProtoSerializer<Value>,
//        IProtoSerializer<DoubleValue>,
//        IProtoSerializer<FloatValue>,
//        IProtoSerializer<Int64Value>,
//        IProtoSerializer<UInt64Value>,
//        IProtoSerializer<Int32Value>,
//        IProtoSerializer<UInt32Value>,
//        IProtoSerializer<StringValue>,
//        IProtoSerializer<BoolValue>,
//        IProtoSerializer<BytesValue>
//    {
//        private WellKnownSerializer() { }
//        public static readonly WellKnownSerializer Instance = new WellKnownSerializer();

//        void IProtoSerializer<Empty>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, Empty obj)
//        { }

//        Empty IProtoSerializer<Empty>.Deserialize(ProtoReader reader, ref ProtoReader.State state, Empty value)
//        {
//            while (reader.ReadFieldHeader(ref state) != 0) reader.SkipField(ref state);
//            return value;
//        }

//        void IProtoSerializer<DoubleValue>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, DoubleValue value)
//        {
//            var val = value.Value;
//            if (val != 0)
//            {
//                ProtoWriter.WriteFieldHeader(1, WireType.Fixed64, writer, ref state);
//                ProtoWriter.WriteDouble(val, writer, ref state);
//            }
//        }

//        DoubleValue IProtoSerializer<DoubleValue>.Deserialize(ProtoReader reader, ref ProtoReader.State state, DoubleValue value)
//        {
//            int field;
//            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//            {
//                switch (field)
//                {
//                    case 1: value = reader.ReadDouble(ref state); break;
//                    default: reader.SkipField(ref state); break;
//                }
//            }
//            return value;
//        }

//        void IProtoSerializer<FloatValue>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, FloatValue value)
//        {
//            var val = value.Value;
//            if (val != 0)
//            {
//                ProtoWriter.WriteFieldHeader(1, WireType.Fixed32, writer, ref state);
//                ProtoWriter.WriteSingle(val, writer, ref state);
//            }
//        }

//        FloatValue IProtoSerializer<FloatValue>.Deserialize(ProtoReader reader, ref ProtoReader.State state, FloatValue value)
//        {
//            int field;
//            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//            {
//                switch (field)
//                {
//                    case 1: value = reader.ReadSingle(ref state); break;
//                    default: reader.SkipField(ref state); break;
//                }
//            }
//            return value;
//        }

//        void IProtoSerializer<BoolValue>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, BoolValue value)
//        {
//            var val = value.Value;
//            if (val)
//            {
//                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
//                ProtoWriter.WriteBoolean(val, writer, ref state);
//            }
//        }

//        BoolValue IProtoSerializer<BoolValue>.Deserialize(ProtoReader reader, ref ProtoReader.State state, BoolValue value)
//        {
//            int field;
//            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//            {
//                switch (field)
//                {
//                    case 1: value = reader.ReadBoolean(ref state); break;
//                    default: reader.SkipField(ref state); break;
//                }
//            }
//            return value;
//        }

//        void IProtoSerializer<Int32Value>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, Int32Value value)
//        {
//            var val = value.Value;
//            if (val != 0)
//            {
//                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
//                ProtoWriter.WriteInt32(val, writer, ref state);
//            }
//        }

//        Int32Value IProtoSerializer<Int32Value>.Deserialize(ProtoReader reader, ref ProtoReader.State state, Int32Value value)
//        {
//            int field;
//            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//            {
//                switch (field)
//                {
//                    case 1: value = reader.ReadInt32(ref state); break;
//                    default: reader.SkipField(ref state); break;
//                }
//            }
//            return value;
//        }

//        void IProtoSerializer<UInt32Value>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, UInt32Value value)
//        {
//            var val = value.Value;
//            if (val != 0)
//            {
//                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
//                ProtoWriter.WriteUInt32(val, writer, ref state);
//            }
//        }

//        UInt32Value IProtoSerializer<UInt32Value>.Deserialize(ProtoReader reader, ref ProtoReader.State state, UInt32Value value)
//        {
//            int field;
//            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//            {
//                switch (field)
//                {
//                    case 1: value = reader.ReadUInt32(ref state); break;
//                    default: reader.SkipField(ref state); break;
//                }
//            }
//            return value;
//        }

//        void IProtoSerializer<Int64Value>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, Int64Value value)
//        {
//            var val = value.Value;
//            if (val != 0)
//            {
//                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
//                ProtoWriter.WriteInt64(val, writer, ref state);
//            }
//        }

//        Int64Value IProtoSerializer<Int64Value>.Deserialize(ProtoReader reader, ref ProtoReader.State state, Int64Value value)
//        {
//            int field;
//            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//            {
//                switch (field)
//                {
//                    case 1: value = reader.ReadInt64(ref state); break;
//                    default: reader.SkipField(ref state); break;
//                }
//            }
//            return value;
//        }

//        void IProtoSerializer<UInt64Value>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, UInt64Value value)
//        {
//            var val = value.Value;
//            if (val != 0)
//            {
//                ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
//                ProtoWriter.WriteUInt64(val, writer, ref state);
//            }
//        }

//        UInt64Value IProtoSerializer<UInt64Value>.Deserialize(ProtoReader reader, ref ProtoReader.State state, UInt64Value value)
//        {
//            int field;
//            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//            {
//                switch (field)
//                {
//                    case 1: value = reader.ReadUInt64(ref state); break;
//                    default: reader.SkipField(ref state); break;
//                }
//            }
//            return value;
//        }

//        void IProtoSerializer<StringValue>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, StringValue value)
//        {
//            var val = value.Value;
//            if (!string.IsNullOrEmpty(value))
//            {
//                ProtoWriter.WriteFieldHeader(1, WireType.String, writer, ref state);
//                ProtoWriter.WriteString(val, writer, ref state);
//            }
//        }

//        StringValue IProtoSerializer<StringValue>.Deserialize(ProtoReader reader, ref ProtoReader.State state, StringValue value)
//        {
//            int field;
//            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//            {
//                switch (field)
//                {
//                    case 1: value = reader.ReadString(ref state); break;
//                    default: reader.SkipField(ref state); break;
//                }
//            }
//            return value;
//        }

//        void IProtoSerializer<Struct>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, Struct value)
//        {
//            if (value == null || !value.ShouldSerializeFields()) return;

//            var map = value.Fields;
//            foreach (var pair in map)
//            {
//                ProtoWriter.WriteFieldHeader(1, WireType.String, writer, ref state);
//                var outer = ProtoWriter.StartSubItem(map, writer, ref state);

//                ProtoWriter.WriteFieldHeader(1, WireType.String, writer, ref state);
//                ProtoWriter.WriteString(pair.Key, writer, ref state);

//                ProtoWriter.WriteFieldHeader(2, WireType.String, writer, ref state);
//                var inner = ProtoWriter.StartSubItem(null, writer, ref state);
//                Value.Serializer.Serialize(writer, ref state, pair.Value);
//                ProtoWriter.EndSubItem(inner, writer, ref state);

//                ProtoWriter.EndSubItem(outer, writer, ref state);
//            }
//        }

//        Struct IProtoSerializer<Struct>.Deserialize(ProtoReader reader, ref ProtoReader.State state, Struct value)
//        {
//            if (value == null) value = new Struct();
//            int field;
//            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//            {
//                switch (field)
//                {
//                    case 1:
//                        var map = value.Fields;
//                        do
//                        {
//                            string key = default;
//                            Value val = default;
//                            var outer = ProtoReader.StartSubItem(reader, ref state);

//                            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//                            {
//                                switch (field)
//                                {
//                                    case 1:
//                                        key = reader.ReadString(ref state); break;
//                                    case 2:
//                                        var inner = ProtoReader.StartSubItem(reader, ref state);
//                                        val = Value.Serializer.Deserialize(reader, ref state, val);
//                                        ProtoReader.EndSubItem(inner, reader, ref state);
//                                        break;
//                                    default: reader.SkipField(ref state); break;
//                                }
//                            }
//                            ProtoReader.EndSubItem(outer, reader, ref state);
//                            map[key] = val;
//                        } while (reader.TryReadFieldHeader(ref state, field));
//                        break;
//                    default:
//                        reader.SkipField(ref state); break;
//                }
//            }
//            return value;
//        }

//        void IProtoSerializer<ListValue>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, ListValue value)
//        {
//            if (value == null || !value.ShouldSerializeValues()) return;

//            var list = value.Values;
//            foreach(var item in list)
//            {
//                ProtoWriter.WriteFieldHeader(2, WireType.String, writer, ref state);
//                var tok = ProtoWriter.StartSubItem(list, writer, ref state);
//                Value.Serializer.Serialize(writer, ref state, item);
//                ProtoWriter.EndSubItem(tok, writer, ref state);
//            }
//        }

//        ListValue IProtoSerializer<ListValue>.Deserialize(ProtoReader reader, ref ProtoReader.State state, ListValue value)
//        {
//            if (value == null) value = new ListValue();
//            int field;
//            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//            {
//                switch (field)
//                {
//                    case 1:
//                        var list = value.Values;
//                        do
//                        {
//                            var inner = ProtoReader.StartSubItem(reader, ref state);
//                            list.Add(Value.Serializer.Deserialize(reader, ref state, default));
//                            ProtoReader.EndSubItem(inner, reader, ref state);
//                        } while (reader.TryReadFieldHeader(ref state, field));
//                        break;
//                    default: reader.SkipField(ref state); break;
//                }
//            }
//            return value;
//        }

//        void IProtoSerializer<BytesValue>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, BytesValue value)
//        {
//            ProtoWriter.WriteFieldHeader(1, WireType.String, writer, ref state);
//            if (value.TryGetArray(out var segment))
//            {
//                ProtoWriter.WriteBytes(segment.Array, segment.Offset, segment.Count, writer, ref state);
//            }
//            else
//            {
//#if PLAT_SPANS
//                ProtoWriter.WriteBytes(value.Payload, writer, ref state);
//#else
//                throw new System.InvalidOperationException();
//#endif
//            }

//        }

//        BytesValue IProtoSerializer<BytesValue>.Deserialize(ProtoReader reader, ref ProtoReader.State state, BytesValue value)
//        {
//            int field;
//            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//            {
//                switch (field)
//                {
//                    case 1:
//                        // this isn't especially clever or efficient, but since we don't usually
//                        // expect a blob to be in multiple chunks, I'm not too concerned
//                        value = value.Append(ProtoReader.AppendBytes(null, reader, ref state));
//                        break;
//                    default: reader.SkipField(ref state); break;
//                }
//            }
//            return value;
//        }

//        void IProtoSerializer<Value>.Serialize(ProtoWriter writer, ref ProtoWriter.State state, Value value)
//        {
//            switch(value.Discriminator)
//            {
//                case 1: // NullValue
//                    ProtoWriter.WriteFieldHeader(1, WireType.Variant, writer, ref state);
//                    ProtoWriter.WriteInt32((int)value.NullValue, writer, ref state);
//                    break;
//                case 2: // NumberValue
//                    ProtoWriter.WriteFieldHeader(2, WireType.Fixed64, writer, ref state);
//                    ProtoWriter.WriteDouble(value.NumberValue, writer, ref state);
//                    break;
//                case 3: // StringValue
//                    ProtoWriter.WriteFieldHeader(3, WireType.String, writer, ref state);
//                    ProtoWriter.WriteString(value.StringValue, writer, ref state);
//                    break;
//                case 4: // BoolValue
//                    ProtoWriter.WriteFieldHeader(4, WireType.Variant, writer, ref state);
//                    ProtoWriter.WriteBoolean(value.BoolValue, writer, ref state);
//                    break;
//                case 5: // StructValue
//                    var val = value.StructValue;
//                    var tok = ProtoWriter.StartSubItem(null, writer, ref state);
//                    Struct.Serializer.Serialize(writer, ref state, val);
//                    ProtoWriter.EndSubItem(tok, writer, ref state);
//                    break;
//                case 6: // ListValue
//                    var list = value.ListValue;
//                    tok = ProtoWriter.StartSubItem(list, writer, ref state);
//                    ListValue.Serializer.Serialize(writer, ref state, list);
//                    ProtoWriter.EndSubItem(tok, writer, ref state);
//                    break;
//            }
//        }

//        Value IProtoSerializer<Value>.Deserialize(ProtoReader reader, ref ProtoReader.State state, Value value)
//        {
//            int field;
//            while ((field = reader.ReadFieldHeader(ref state)) != 0)
//            {
//                switch(field)
//                {
//                    case 1: // NullValue
//                        value = new Value((NullValue)reader.ReadInt32(ref state));
//                        break;
//                    case 2: // NumberValue
//                        value = new Value(reader.ReadDouble(ref state));
//                        break;
//                    case 3: // StringValue
//                        value = new Value(reader.ReadString(ref state));
//                        break;
//                    case 4: // BoolValue
//                        value = new Value(reader.ReadBoolean(ref state));
//                        break;
//                    case 5: // StructValue
//                        var tok = ProtoReader.StartSubItem(reader, ref state);
//                        value = new Value(Struct.Serializer.Deserialize(reader, ref state, value.StructValue));
//                        ProtoReader.EndSubItem(tok, reader, ref state);
//                        break;
//                    case 6: // ListValue
//                        tok = ProtoReader.StartSubItem(reader, ref state);
//                        value = new Value(ListValue.Serializer.Deserialize(reader, ref state, value.ListValue));
//                        ProtoReader.EndSubItem(tok, reader, ref state);
//                        break;
//                    default:
//                        reader.SkipField(ref state);
//                        break;
//                }
//            }
//            return value;
//        }
//    }
//}
