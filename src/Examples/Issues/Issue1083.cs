using ProtoBuf.Serializers;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace ProtoBuf.Issues
{
    public class Issue1083
    {
        public enum SimpleEnum : int
        {
            Value0 = 0,
            Value1 = 1,
            //...
        }

        [ProtoContract]
        public class WithoutWrapping
        {

            [ProtoMember(1)]
            public List<SimpleEnum> List { get; } = new();
        }

        const string ExpectedHex = "08-01-08-00-08-01";

        [Fact]
        public void VerifyPayloadWithoutWrapping()
        {
            var hex = RoundTrip(new WithoutWrapping { List = { SimpleEnum.Value1, SimpleEnum.Value0, SimpleEnum.Value1 } }, out var clone);
            Assert.Equal(new[] { SimpleEnum.Value1, SimpleEnum.Value0, SimpleEnum.Value1 }, clone.List);
            Assert.Equal(ExpectedHex, hex);
        }

        [ProtoContract]
        public class WithWrapping
        {

            [ProtoMember(1)]
            public List<WrappingStruct> List { get; } = new();
        }

        [Fact]
        public void VerifyPayloadWithWrapping()
        {
            var hex = RoundTrip(new WithWrapping { List = { new(SimpleEnum.Value1), new(SimpleEnum.Value0), new(SimpleEnum.Value1) } }, out var clone);
            Assert.Equal(new WrappingStruct[] { new(SimpleEnum.Value1), new(SimpleEnum.Value0), new(SimpleEnum.Value1) }, clone.List);
            Assert.Equal(ExpectedHex, hex);
        }


        [ProtoContract(Serializer = typeof(WrappingStructSerializer))]
        public readonly struct WrappingStruct : IEquatable<WrappingStruct>
        {
            public WrappingStruct(SimpleEnum value) => Value = value;
            [ProtoMember(1)]
            public readonly SimpleEnum Value { get; }

            public override string ToString() => Value.ToString();
            public override int GetHashCode() => Value.GetHashCode();
            public override bool Equals(object obj) => obj is WrappingStruct other && Equals(other);
            public bool Equals(WrappingStruct other) => Value == other.Value;

            public sealed class WrappingStructSerializer : ISerializer<WrappingStruct>
            {
                SerializerFeatures ISerializer<WrappingStruct>.Features => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;

                WrappingStruct ISerializer<WrappingStruct>.Read(ref ProtoReader.State state, WrappingStruct value)
                    => new((SimpleEnum)state.ReadInt32());

                void ISerializer<WrappingStruct>.Write(ref ProtoWriter.State state, WrappingStruct value)
                    => state.WriteInt32((int)value.Value);
            }
        }


        static string RoundTrip<T>(T payload, out T clone)
        {
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, payload);
            ms.Position = 0;
            clone = Serializer.Deserialize<T>(ms);
            if (!ms.TryGetBuffer(out var buffer)) buffer = new(ms.ToArray());
            return BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count);
        }
    }
}
