using System.Collections.Generic;
using System.ComponentModel;
namespace ProtoBuf.WellKnownTypes
{

    [ProtoContract(Name = ".google.protobuf.Struct")]
    public sealed class Struct
    {
        public static IProtoSerializer<Struct> Serializer => WellKnownSerializer.Instance;

        [ProtoMember(1, Name = @"fields")]
        [ProtoMap]
        public Dictionary<string, Value> Fields => _fields  ?? (_fields = new Dictionary<string, Value>());

        public bool ShouldSerializeFields() => _fields != null;

        private Dictionary<string, Value> _fields;
    }

    [ProtoContract(Name = ".google.protobuf.Value")]
    public readonly struct Value
    {
        internal int Discriminator => _value.Discriminator;

        public static IProtoSerializer<Value> Serializer => WellKnownSerializer.Instance;

        private readonly DiscriminatedUnion64Object _value;
        private Value(DiscriminatedUnion64Object value) => _value = value;
        public static Value Null { get; } = new Value(NullValue.NullValue);

        [ProtoMember(1, Name = @"null_value")]
        public NullValue NullValue
        {
            get { return _value.Is(1) ? ((NullValue)_value.Int32) : default; }
        }

        internal Value(NullValue value) : this(new DiscriminatedUnion64Object(1, (int)value)) { }
        public bool ShouldSerializeNullValue() => _value.Is(1);

        [ProtoMember(2, Name = @"number_value")]
        public double NumberValue
        {
            get { return _value.Is(2) ? _value.Double : default; }
        }

        public Value(double value) : this(new DiscriminatedUnion64Object(2, value)) { }
        public static implicit operator Value(double value) => new Value(value);
        public bool ShouldSerializeNumberValue() => _value.Is(2);

        [ProtoMember(3, Name = @"string_value")]
        [DefaultValue("")]
        public string StringValue
        {
            get { return _value.Is(3) ? ((string)_value.Object) : ""; }
        }

        public Value(string value) : this(new DiscriminatedUnion64Object(3, value)) { }
        public static implicit operator Value(string value) => new Value(value);
        public bool ShouldSerializeStringValue() => _value.Is(3);

        [ProtoMember(4, Name = @"bool_value")]
        public bool BoolValue
        {
            get { return _value.Is(4) ? _value.Boolean : default; }
        }

        public Value(bool value) : this(new DiscriminatedUnion64Object(4, value)) { }
        public static implicit operator Value(bool value) => new Value(value);
        public bool ShouldSerializeBoolValue() => _value.Is(4);

        [ProtoMember(5, Name = @"struct_value")]
        public Struct StructValue
        {
            get { return _value.Is(5) ? ((Struct)_value.Object) : default; }
        }

        public Value(Struct value) : this(new DiscriminatedUnion64Object(5, value)) { }
        public static implicit operator Value(Struct value) => new Value(value);
        public bool ShouldSerializeStructValue() => _value.Is(5);

        [ProtoMember(6, Name = @"list_value")]
        public ListValue ListValue
        {
            get { return _value.Is(6) ? ((ListValue)_value.Object) : default; }
        }
        public Value(ListValue value) : this(new DiscriminatedUnion64Object(6, value)) { }
        public static implicit operator Value(ListValue value) => new Value(value);
        public bool ShouldSerializeListValue() => _value.Is(6);
    }

    [ProtoContract(Name = ".google.protobuf.ListValue")]
    public sealed class ListValue
    {
        public static IProtoSerializer<ListValue> Serializer => WellKnownSerializer.Instance;

        [ProtoMember(1, Name = @"values")]
        public List<Value> Values => _values ?? (_values = new List<Value>());

        public bool ShouldSerializeValues() => _values != null;

        private List<Value> _values;

    }

    [ProtoContract(Name = ".google.protobuf.NullValue")]
    public enum NullValue
    {
        [ProtoEnum(Name = @"NULL_VALUE")]
        NullValue = 0,
    }

}
