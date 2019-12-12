using ProtoBuf.Meta;
using ProtoBuf.Serializers;
using ProtoBuf.unittest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ProtoBuf.Test.Issues
{
    public class Issue598
    {
        [Fact]
        public void RoundTripCustomSerializerScalar()
        {
            var data = new Item { ViaSerializer = 12345, Description = "abc", ViaSurrogate = 678910 };
            var ms = new MemoryStream();
            Serializer.Serialize(ms, data);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("08-B9-60-12-03-61-62-63", hex);
            ms.Position = 0;
            var clone = Serializer.Deserialize<Item>(ms);
            Assert.Equal(12345, (long)clone.ViaSerializer);
            Assert.Equal("abc", clone.Description);
            Assert.Equal(678910, (long)clone.ViaSurrogate);
        }

        [Fact]
        public void VerifyIL()
        {
            var model = RuntimeTypeModel.Create();
            model.Add<NewHtmlStringViaSerializer>();
            model.Add<NewHtmlStringViaSurrogate>();
            model.Add<Item>();
            PEVerify.CompileAndVerify(model, "NewHtmlStringModel", deleteOnSuccess: false);
        }

        [Fact]
        public void ProtoBuf_Empty_Propagation_Serializer()
        {
            Assert.Equal(CustomHtmlStringWithSerializer.Empty, RoundTrip<CustomHtmlStringWithSerializer, CustomHtmlStringWithSerializer>(CustomHtmlStringWithSerializer.Empty));
            Assert.Equal(string.Empty, RoundTrip<CustomHtmlStringWithSerializer, string>(CustomHtmlStringWithSerializer.Empty));
            Assert.Equal(CustomHtmlStringWithSerializer.Empty, RoundTrip<string, CustomHtmlStringWithSerializer>(string.Empty));
        }

        [Fact]
        public void ProtoBuf_Empty_Propagation_Surrogate()
        {
            Assert.Equal(CustomHtmlStringWithSurrogate.Empty, RoundTrip<CustomHtmlStringWithSurrogate, CustomHtmlStringWithSurrogate>(CustomHtmlStringWithSurrogate.Empty));
            Assert.Equal(string.Empty, RoundTrip<CustomHtmlStringWithSurrogate, string>(CustomHtmlStringWithSurrogate.Empty));
            Assert.Equal(CustomHtmlStringWithSurrogate.Empty, RoundTrip<string, CustomHtmlStringWithSurrogate>(string.Empty));
        }

        [Fact]
        public void ProtoBuf_Null_Propagation_Serializer()
        {
            // remember that protobuf has no nulls, so at the root, null and empty are the same thing; make sure
            // the tests are comparing like-for-like
            var expectation = RoundTrip<string, string>(null);
            Assert.Equal(expectation, RoundTrip<CustomHtmlStringWithSerializer, CustomHtmlStringWithSerializer>(null)?.ToHtmlString());
            Assert.Equal(expectation, RoundTrip<string, CustomHtmlStringWithSerializer>(null)?.ToHtmlString());
            Assert.Equal(expectation, RoundTrip<CustomHtmlStringWithSerializer, string>(null));
        }
        [Fact]
        public void ProtoBuf_Null_Propagation_Surrogate()
        {
            // remember that protobuf has no nulls, so at the root, null and empty are the same thing; make sure
            // the tests are comparing like-for-like
            var expectation = RoundTrip<string, string>(null);
            Assert.Equal(expectation, RoundTrip<CustomHtmlStringWithSurrogate, CustomHtmlStringWithSurrogate>(null)?.ToHtmlString());
            Assert.Equal(expectation, RoundTrip<string, CustomHtmlStringWithSurrogate>(null)?.ToHtmlString());
            Assert.Equal(expectation, RoundTrip<CustomHtmlStringWithSurrogate, string>(null));
        }

        [Fact]
        public void ProtoBuf_Nested_Html_To_Html_Serializer()
        {
            var original = CustomHtmlStringWithSerializer.Create("bar");

            var cloned = RoundTrip<NewHtmlStringViaSerializer, NewHtmlStringViaSerializer>(new NewHtmlStringViaSerializer
            {
                HtmlString = original,
                HtmlList = null,
            });

            Assert.Null(cloned.HtmlList);
            Assert.Equal(original, cloned.HtmlString);
        }

        [Fact]
        public void ProtoBuf_Nested_Html_To_Html_Surrogate()
        {
            var original = CustomHtmlStringWithSurrogate.Create("bar");

            var cloned = RoundTrip<NewHtmlStringViaSurrogate, NewHtmlStringViaSurrogate>(new NewHtmlStringViaSurrogate
            {
                HtmlString = original,
                HtmlList = null,
            });

            Assert.Null(cloned.HtmlList);
            Assert.Equal(original, cloned.HtmlString);
        }

        [Fact]
        public void ProtoBuf_Nested_String_To_Html_Serializer()
        {
            var original = "bar";

            var cloned = RoundTrip<OldHtmlString, NewHtmlStringViaSerializer>(new OldHtmlString
            {
                HtmlString = original,
                HtmlList = null,
            });

            Assert.Null(cloned.HtmlList);
            Assert.Equal(original, cloned.HtmlString.ToHtmlString());
        }


        [Fact]
        public void ProtoBuf_Nested_String_To_Html_Surrogate()
        {
            var original = "bar";

            var cloned = RoundTrip<OldHtmlString, NewHtmlStringViaSurrogate>(new OldHtmlString
            {
                HtmlString = original,
                HtmlList = null,
            });

            Assert.Null(cloned.HtmlList);
            Assert.Equal(original, cloned.HtmlString.ToHtmlString());
        }

        [Fact]
        public void ProtoBuf_Nested_Html_To_String_Serializer()
        {
            var original = CustomHtmlStringWithSerializer.Create("bar");

            var cloned = RoundTrip<NewHtmlStringViaSerializer, OldHtmlString>(new NewHtmlStringViaSerializer
            {
                HtmlString = original,
                HtmlList = null,
            });

            Assert.Null(cloned.HtmlList);
            Assert.Equal(original.ToHtmlString(), cloned.HtmlString);
        }

        [Fact]
        public void ProtoBuf_Nested_Html_To_String_Surrogate()
        {
            var original = CustomHtmlStringWithSurrogate.Create("bar");

            var cloned = RoundTrip<NewHtmlStringViaSurrogate, OldHtmlString>(new NewHtmlStringViaSurrogate
            {
                HtmlString = original,
                HtmlList = null,
            });

            Assert.Null(cloned.HtmlList);
            Assert.Equal(original.ToHtmlString(), cloned.HtmlString);
        }

        [Fact]
        public void ProtoBuf_NestedList_Html_To_Html_Serializer()
        {
            var original = CustomHtmlStringWithSerializer.Create("baz");

            var cloned = RoundTrip<NewHtmlStringViaSerializer, NewHtmlStringViaSerializer>(new NewHtmlStringViaSerializer
            {
                HtmlString = null,
                HtmlList = new List<CustomHtmlStringWithSerializer> { original },
            });

            Assert.Null(cloned.HtmlString);
            Assert.Equal(original, cloned.HtmlList.Single());
        }

        [Fact]
        public void ProtoBuf_NestedList_Html_To_Html_Surrogate()
        {
            var original = CustomHtmlStringWithSurrogate.Create("baz");

            var cloned = RoundTrip<NewHtmlStringViaSurrogate, NewHtmlStringViaSurrogate>(new NewHtmlStringViaSurrogate
            {
                HtmlString = null,
                HtmlList = new List<CustomHtmlStringWithSurrogate> { original },
            });

            Assert.Null(cloned.HtmlString);
            Assert.Equal(original, cloned.HtmlList.Single());
        }

        [Fact]
        public void ProtoBuf_NestedList_String_To_Html_Serializer()
        {
            var original = "baz";

            var cloned = RoundTrip<OldHtmlString, NewHtmlStringViaSerializer>(new OldHtmlString
            {
                HtmlString = null,
                HtmlList = new List<string> { original },
            });

            Assert.Null(cloned.HtmlString);
            Assert.Equal(original, cloned.HtmlList.Single().ToHtmlString());
        }

        [Fact]
        public void ProtoBuf_NestedList_String_To_Html_Surrogate()
        {
            var original = "baz";

            var cloned = RoundTrip<OldHtmlString, NewHtmlStringViaSurrogate>(new OldHtmlString
            {
                HtmlString = null,
                HtmlList = new List<string> { original },
            });

            Assert.Null(cloned.HtmlString);
            Assert.Equal(original, cloned.HtmlList.Single().ToHtmlString());
        }

        [Fact]
        public void ProtoBuf_NestedList_Html_To_String_Serializer()
        {
            var original = CustomHtmlStringWithSerializer.Create("baz");

            var cloned = RoundTrip<NewHtmlStringViaSerializer, OldHtmlString>(new NewHtmlStringViaSerializer
            {
                HtmlString = null,
                HtmlList = new List<CustomHtmlStringWithSerializer> { original },
            });

            Assert.Null(cloned.HtmlString);
            Assert.Equal(original.ToHtmlString(), cloned.HtmlList.Single());
        }

        [Fact]
        public void ProtoBuf_NestedList_Html_To_String_Surrogate()
        {
            var original = CustomHtmlStringWithSurrogate.Create("baz");

            var cloned = RoundTrip<NewHtmlStringViaSurrogate, OldHtmlString>(new NewHtmlStringViaSurrogate
            {
                HtmlString = null,
                HtmlList = new List<CustomHtmlStringWithSurrogate> { original },
            });

            Assert.Null(cloned.HtmlString);
            Assert.Equal(original.ToHtmlString(), cloned.HtmlList.Single());
        }

        private static TRead RoundTrip<TWrite, TRead>(TWrite instance)
        {
            using var memory = new MemoryStream();
            ProtoBuf.Serializer.Serialize(memory, instance);
            memory.Flush();
            memory.Position = 0;
            return ProtoBuf.Serializer.Deserialize<TRead>(memory);
        }

        [ProtoContract(Serializer = typeof(Serializer))]
        public readonly struct CustomTypeWithSerializer : IEquatable<CustomTypeWithSerializer>
        {
            public class Serializer : ISerializer<CustomTypeWithSerializer>
            {
                public SerializerFeatures Features
                    => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;

                public CustomTypeWithSerializer Read(ref ProtoReader.State state, CustomTypeWithSerializer value)
                    => state.ReadInt64();

                public void Write(ref ProtoWriter.State state, CustomTypeWithSerializer value)
                    => state.WriteInt64(value);
            }
            public static implicit operator long(CustomTypeWithSerializer d) => d.value;
            public static implicit operator CustomTypeWithSerializer(long d) => new CustomTypeWithSerializer(d);
            private readonly long value;
            public override string ToString()
                => $"custom: {value}";
            public CustomTypeWithSerializer(long value)
                => this.value = value;

            public override int GetHashCode() => value.GetHashCode();
            public override bool Equals(object obj)
                => obj is CustomTypeWithSerializer ct && Equals(ct);
            public bool Equals(CustomTypeWithSerializer other)
                => other.value == value;
        }

        [ProtoContract(Surrogate = typeof(long))]
        public readonly struct CustomTypeWithSurrogate : IEquatable<CustomTypeWithSurrogate>
        {
            public static implicit operator long(CustomTypeWithSurrogate d) => d.value;
            public static implicit operator CustomTypeWithSurrogate(long d) => new CustomTypeWithSurrogate(d);
            private readonly long value;
            public override string ToString()
                => $"custom: {value}";
            public CustomTypeWithSurrogate(long value)
                => this.value = value;

            public override int GetHashCode() => value.GetHashCode();
            public override bool Equals(object obj)
                => obj is CustomTypeWithSurrogate ct && Equals(ct);
            public bool Equals(CustomTypeWithSurrogate other)
                => other.value == value;
        }

        [ProtoContract]
        public class Item
        {
            [ProtoMember(1)]
            public CustomTypeWithSerializer ViaSerializer { get; set; }

            [ProtoMember(2)]
            public string Description { get; set; }

            [ProtoMember(3)]
            public CustomTypeWithSurrogate ViaSurrogate { get; set; }
        }

        [ProtoContract]
        public class NewHtmlStringViaSerializer
        {
            [ProtoMember(1)]
            public CustomHtmlStringWithSerializer HtmlString { get; set; }
            [ProtoMember(2)]
            public List<CustomHtmlStringWithSerializer> HtmlList { get; set; }
        }

        [ProtoContract]
        public class NewHtmlStringViaSurrogate
        {
            [ProtoMember(1)]
            public CustomHtmlStringWithSurrogate HtmlString { get; set; }
            [ProtoMember(2)]
            public List<CustomHtmlStringWithSurrogate> HtmlList { get; set; }
        }

        [ProtoContract]
        public class OldHtmlString
        {
            [ProtoMember(1)]
            public string HtmlString { get; set; }
            [ProtoMember(2)]
            public List<string> HtmlList { get; set; }

        }

        [ProtoContract(Serializer = typeof(ProtoBufSerializer))]
        public class CustomHtmlStringWithSerializer
        {
            public override int GetHashCode()
                => _value?.GetHashCode() ?? 0;
            public override bool Equals(object obj)
                => obj is CustomHtmlStringWithSerializer other && other._value == _value;

            public static CustomHtmlStringWithSerializer Empty { get; } = new CustomHtmlStringWithSerializer("");
            private readonly string _value;
            private CustomHtmlStringWithSerializer(string value) => _value = value;
            public static CustomHtmlStringWithSerializer Create(string value)
            {
                if (value == null) return null;
                if (value == "") return Empty;
                return new CustomHtmlStringWithSerializer(value);
            }

            public override string ToString() => _value;
            public string ToHtmlString() => _value;
            public class ProtoBufSerializer : ISerializer<CustomHtmlStringWithSerializer>, IFactory<CustomHtmlStringWithSerializer>
            {

                SerializerFeatures ISerializer<CustomHtmlStringWithSerializer>.Features { get; } = SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeString;

                // NOTE
                // If the factory implementation returns null it tries to call the Activator anyway!
                // https://github.com/protobuf-net/protobuf-net/blob/17a0ec14307e0926bd067472b3ef80a221851b95/src/protobuf-net.Core/Meta/TypeModel.cs#L1178-L1180
                //
                // This is only used when we deserialize a root object, and it has no content.
                CustomHtmlStringWithSerializer IFactory<CustomHtmlStringWithSerializer>.Create(ISerializationContext context) => Empty;

                CustomHtmlStringWithSerializer ISerializer<CustomHtmlStringWithSerializer>.Read(ref ProtoReader.State state, CustomHtmlStringWithSerializer value)
                    => state.ReadString() is string str ? Create(str) : null;

                void ISerializer<CustomHtmlStringWithSerializer>.Write(ref ProtoWriter.State state, CustomHtmlStringWithSerializer value)
                {
                    var str = value?._value;
                    if (str != null)
                    {
                        state.WriteString(str);
                    }
                }
            }

        }
        [ProtoContract(Serializer = typeof(string))]
        public class CustomHtmlStringWithSurrogate
        {
            public static explicit operator string(CustomHtmlStringWithSurrogate d) => d?._value;
            public static explicit operator CustomHtmlStringWithSurrogate(string d) => Create(d);
            public override int GetHashCode()
                => _value?.GetHashCode() ?? 0;
            public override bool Equals(object obj)
                => obj is CustomHtmlStringWithSurrogate other && other._value == _value;

            public static CustomHtmlStringWithSurrogate Empty { get; } = new CustomHtmlStringWithSurrogate("");
            private readonly string _value;
            private CustomHtmlStringWithSurrogate(string value) => _value = value;
            public static CustomHtmlStringWithSurrogate Create(string value)
            {
                if (value == null) return null;
                if (value == "") return Empty;
                return new CustomHtmlStringWithSurrogate(value);
            }

            public override string ToString() => _value;
            public string ToHtmlString() => _value;

        }
    }
}
