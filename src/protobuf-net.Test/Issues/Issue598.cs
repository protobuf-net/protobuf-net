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
            var data = new Item { CustomId = 12345, Description = "abc" };
            var ms = new MemoryStream();
            Serializer.Serialize(ms, data);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("08-B9-60-12-03-61-62-63", hex);
            ms.Position = 0;
            var clone = Serializer.Deserialize<Item>(ms);
            Assert.Equal(12345, (long)clone.CustomId);
            Assert.Equal("abc", clone.Description);
        }

        [Fact]
        public void VerifyIL()
        {
            var model = RuntimeTypeModel.Create();
            model.Add<NewHtmlString>();
            model.Add<Item>();
            PEVerify.CompileAndVerify(model, "NewHtmlStringModel");
        }

        [Fact]
        public void ProtoBuf_Empty_Propagation()
        {
            Assert.Equal(CustomHtmlString.Empty, RoundTrip<CustomHtmlString, CustomHtmlString>(CustomHtmlString.Empty));
            Assert.Equal(string.Empty, RoundTrip<CustomHtmlString, string>(CustomHtmlString.Empty));
            Assert.Equal(CustomHtmlString.Empty, RoundTrip<string, CustomHtmlString>(string.Empty));
        }

        [Fact]
        public void ProtoBuf_Null_Propagation()
        {
            // For some reason deserializing a root null string returns an empty string
            // this makes sure that the tests break if this ever changes / our impl breaks because of this.
            var expectation = RoundTrip<string, string>(null);

            Assert.Equal(expectation, RoundTrip<CustomHtmlString, CustomHtmlString>(null)?.ToHtmlString());
            Assert.Equal(expectation, RoundTrip<string, CustomHtmlString>(null)?.ToHtmlString());
            Assert.Equal(expectation, RoundTrip<CustomHtmlString, string>(null));
        }

        [Fact]
        public void ProtoBuf_Nested_Html_To_Html()
        {
            var original = CustomHtmlString.Create("bar");

            var cloned = RoundTrip<NewHtmlString, NewHtmlString>(new NewHtmlString
            {
                HtmlString = original,
                HtmlList = null,
            });

            Assert.Null(cloned.HtmlList);
            Assert.Equal(original, cloned.HtmlString);
        }

        [Fact]
        public void ProtoBuf_Nested_String_To_Html()
        {
            var original = "bar";

            var cloned = RoundTrip<OldHtmlString, NewHtmlString>(new OldHtmlString
            {
                HtmlString = original,
                HtmlList = null,
            });

            Assert.Null(cloned.HtmlList);
            Assert.Equal(original, cloned.HtmlString.ToHtmlString());
        }

        [Fact]
        public void ProtoBuf_Nested_Html_To_String()
        {
            var original = CustomHtmlString.Create("bar");

            var cloned = RoundTrip<NewHtmlString, OldHtmlString>(new NewHtmlString
            {
                HtmlString = original,
                HtmlList = null,
            });

            Assert.Null(cloned.HtmlList);
            Assert.Equal(original.ToHtmlString(), cloned.HtmlString);
        }

        [Fact]
        public void ProtoBuf_NestedList_Html_To_Html()
        {
            var original = CustomHtmlString.Create("baz");

            var cloned = RoundTrip<NewHtmlString, NewHtmlString>(new NewHtmlString
            {
                HtmlString = null,
                HtmlList = new List<CustomHtmlString> { original },
            });

            Assert.Null(cloned.HtmlString);
            Assert.Equal(original, cloned.HtmlList.Single());
        }

        [Fact]
        public void ProtoBuf_NestedList_String_To_Html()
        {
            var original = "baz";

            var cloned = RoundTrip<OldHtmlString, NewHtmlString>(new OldHtmlString
            {
                HtmlString = null,
                HtmlList = new List<string> { original },
            });

            Assert.Null(cloned.HtmlString);
            Assert.Equal(original, cloned.HtmlList.Single().ToHtmlString());
        }

        [Fact]
        public void ProtoBuf_NestedList_Html_To_String()
        {
            var original = CustomHtmlString.Create("baz");

            var cloned = RoundTrip<NewHtmlString, OldHtmlString>(new NewHtmlString
            {
                HtmlString = null,
                HtmlList = new List<CustomHtmlString> { original },
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
        public readonly struct CustomType : IEquatable<CustomType>
        {
            public class Serializer : ISerializer<CustomType>
            {
                public SerializerFeatures Features
                    => SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeVarint;

                public CustomType Read(ref ProtoReader.State state, CustomType value)
                    => state.ReadInt64();

                public void Write(ref ProtoWriter.State state, CustomType value)
                    => state.WriteInt64(value);
            }
            public static implicit operator long(CustomType d) => d.value;
            public static implicit operator CustomType(long d) => new CustomType(d);
            private readonly long value;
            public override string ToString()
                => $"custom: {value}";
            public CustomType(long value)
                => this.value = value;

            public override int GetHashCode() => value.GetHashCode();
            public override bool Equals(object obj)
                => obj is CustomType ct && Equals(ct);
            public bool Equals(CustomType other)
                => other.value == value;
        }
        [ProtoContract]
        public class Item
        {
            [ProtoMember(1)]
            public CustomType CustomId { get; set; }

            [ProtoMember(2)]
            public string Description { get; set; }
        }

        [ProtoContract]
        public class NewHtmlString
        {
            [ProtoMember(1)]
            public CustomHtmlString HtmlString { get; set; }
            [ProtoMember(2)]
            public List<CustomHtmlString> HtmlList { get; set; }
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
        public class CustomHtmlString
        {
            public override int GetHashCode()
                => _value?.GetHashCode() ?? 0;
            public override bool Equals(object obj)
                => obj is CustomHtmlString other && other._value == _value;

            public static CustomHtmlString Empty { get; } = new CustomHtmlString("");
            private readonly string _value;
            private CustomHtmlString(string value) => _value = value;
            public static CustomHtmlString Create(string value) {
                if (value == null) return null;
                if (value == "") return Empty;
                return new CustomHtmlString(value);
            }

            public override string ToString() => _value;
            public string ToHtmlString() => _value;
            public class ProtoBufSerializer : ISerializer<CustomHtmlString>, IFactory<CustomHtmlString>
            {
                
                SerializerFeatures ISerializer<CustomHtmlString>.Features { get; } = SerializerFeatures.CategoryScalar | SerializerFeatures.WireTypeString;

                // NOTE
                // If the factory implementation returns null it tries to call the Activator anyway!
                // https://github.com/protobuf-net/protobuf-net/blob/17a0ec14307e0926bd067472b3ef80a221851b95/src/protobuf-net.Core/Meta/TypeModel.cs#L1178-L1180
                //
                // This is only used when we deserialize a root object, and it has no content.
                CustomHtmlString IFactory<CustomHtmlString>.Create(ISerializationContext context) => Empty;

                CustomHtmlString ISerializer<CustomHtmlString>.Read(ref ProtoReader.State state, CustomHtmlString value)
                    => state.ReadString() is string str ? Create(str) : null;

                void ISerializer<CustomHtmlString>.Write(ref ProtoWriter.State state, CustomHtmlString value)
                {
                    var str = value?._value;
                    if (str != null)
                    {
                        state.WriteString(str);
                    }
                }
            }
        }
    }
}
