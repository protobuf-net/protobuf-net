using System;
using System.ComponentModel;

namespace ProtoBuf.WellKnownTypes
{

    [ProtoContract(Name = ".google.protobuf.DoubleValue")]
    public readonly struct DoubleValue
    {
        public static IProtoSerializer<DoubleValue> Serializer => WellKnownSerializer.Instance;

        [ProtoMember(1, Name = @"value")]
        public double Value { get; }

        public DoubleValue(double value) => Value = value;

        public static implicit operator DoubleValue (double value) => new DoubleValue(value);
        public static implicit operator double (DoubleValue value) => value.Value;
    }

    [ProtoContract(Name = ".google.protobuf.FloatValue")]
    public readonly struct FloatValue
    {
        public static IProtoSerializer<FloatValue> Serializer => WellKnownSerializer.Instance;

        [ProtoMember(1, Name = @"value")]
        public float Value { get; }

        public FloatValue(float value) => Value = value;

        public static implicit operator FloatValue(float value) => new FloatValue(value);
        public static implicit operator float(FloatValue value) => value.Value;
    }

    [ProtoContract(Name = ".google.protobuf.Int64Value")]
    public readonly struct Int64Value
    {
        public static IProtoSerializer<Int64Value> Serializer => WellKnownSerializer.Instance;

        [ProtoMember(1, Name = @"value")]
        public long Value { get; }

        public Int64Value(long value) => Value = value;

        public static implicit operator Int64Value(long value) => new Int64Value(value);
        public static implicit operator long(Int64Value value) => value.Value;
    }

    [ProtoContract(Name = ".google.protobuf.UInt64Value")]
    public readonly struct UInt64Value
    {
        public static IProtoSerializer<UInt64Value> Serializer => WellKnownSerializer.Instance;

        [ProtoMember(1, Name = @"value")]
        public ulong Value { get; }

        public UInt64Value(ulong value) => Value = value;

        public static implicit operator UInt64Value(ulong value) => new UInt64Value(value);
        public static implicit operator ulong(UInt64Value value) => value.Value;
    }

    [ProtoContract(Name = ".google.protobuf.Int32Value")]
    public readonly struct Int32Value
    {
        public static IProtoSerializer<Int32Value> Serializer => WellKnownSerializer.Instance;

        [ProtoMember(1, Name = @"value")]
        public int Value { get; }

        public Int32Value(int value) => Value = value;

        public static implicit operator Int32Value(int value) => new Int32Value(value);
        public static implicit operator int(Int32Value value) => value.Value;
    }

    [ProtoContract(Name = ".google.protobuf.UInt32Value")]
    public readonly struct UInt32Value
    {
        public static IProtoSerializer<UInt32Value> Serializer => WellKnownSerializer.Instance;

        [ProtoMember(1, Name = @"value")]
        public uint Value { get; }

        public UInt32Value(uint value) => Value = value;

        public static implicit operator UInt32Value(uint value) => new UInt32Value(value);
        public static implicit operator uint(UInt32Value value) => value.Value;
    }

    [ProtoContract(Name = ".google.protobuf.BoolValue")]
    public readonly struct BoolValue
    {
        public static IProtoSerializer<BoolValue> Serializer => WellKnownSerializer.Instance;

        [ProtoMember(1, Name = @"value")]
        public bool Value { get; }

        public BoolValue(bool value) => Value = value;

        public static implicit operator BoolValue(bool value) => new BoolValue(value);
        public static implicit operator bool(BoolValue value) => value.Value;
    }

    [ProtoContract(Name = ".google.protobuf.StringValue")]
    public readonly struct StringValue
    {
        public static IProtoSerializer<StringValue> Serializer => WellKnownSerializer.Instance;

        [ProtoMember(1, Name = @"value")]
        [DefaultValue("")]
        public string Value => _value ?? "";

        private readonly string _value;
        public StringValue(string value) => _value = value;

        public static implicit operator StringValue(string value) => new StringValue(value);
        public static implicit operator string(StringValue value) => value.Value;
    }

    [ProtoContract(Name = ".google.protobuf.BytesValue")]
    public readonly struct BytesValue
    {
        public static IProtoSerializer<BytesValue> Serializer => WellKnownSerializer.Instance;

        [ProtoMember(1, Name = @"value")]
        public ArraySegment<byte> Value { get; }

        public BytesValue(ArraySegment<byte> value) => Value = value;
        public BytesValue(byte[] value) => Value = new ArraySegment<byte>(value);

        public static implicit operator BytesValue(ArraySegment<byte> value) => new BytesValue(value);
        public static implicit operator BytesValue(byte[] value) => new BytesValue(value);
    }

}
