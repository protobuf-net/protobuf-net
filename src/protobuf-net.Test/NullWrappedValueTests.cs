using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test
{
    public class NullWrappedValueTests
    {
        private readonly ITestOutputHelper _log;
        public NullWrappedValueTests(ITestOutputHelper log)
            => _log = log;

        private void Log(string message)
            => _log?.WriteLine(message);

        [Fact]
        public void ExistingListsBehaviour()
        {
            using var ms = new MemoryStream(new byte[] { 0x08, 0x00, 0x08, 0x01, 0x08, 0x02 });
            var clone = Serializer.Deserialize<Foo>(ms);
            if (!ms.TryGetBuffer(out var buffer)) buffer = new ArraySegment<byte>(ms.ToArray());
            Assert.Equal("08-00-08-01-08-02", BitConverter.ToString(buffer.Array, buffer.Offset, buffer.Count));
        }

        [DataContract]
        public class Foo
        {
            [DataMember(Order = 1)]
            public List<int?> Items { get; } = new List<int?>();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CanApplySimpleConfiguration(bool configured)
        {
            var model = RuntimeTypeModel.Create();
            if (configured)
            {
                model.AfterApplyDefaultBehaviour += (sender, e) =>
                {
                    // configure all Nullable<T> properties as wrapped
                    foreach (var field in e.MetaType.GetFields())
                    {
                        if (Nullable.GetUnderlyingType(field.MemberType) is object)
                        {
                            field.NullWrappedValue = true;
                        }
                    }
                };
            }
            var mt = model[typeof(SomeTypeWithNullables)];
            Assert.False(mt[1].NullWrappedValue);
            Assert.Equal(configured, mt[2].NullWrappedValue);
            Assert.False(mt[3].NullWrappedValue);
            Assert.Equal(configured, mt[4].NullWrappedValue);
            Assert.False(mt[5].NullWrappedValue);
        }

        [DataContract]
        public class SomeTypeWithNullables
        {
            [DataMember(Order = 1)]
            public int Int32 { get; set; }

            [DataMember(Order = 2)]
            public int? WrappedInt32 { get; set; }

            [DataMember(Order = 3)]
            public float Single { get; set; }

            [DataMember(Order = 4)]
            public float? WrappedSingle { get; set; }

            [DataMember(Order = 5)]
            public string String { get; set; }
        }

        [Theory]
        [InlineData(null, "")] // nothing to write
        [InlineData(0, "08-00")] // explicit zero
        [InlineData(1, "08-01")] // explicit one
        [InlineData(2, "08-02")] // explicit two
        public void ValidateBasicNullableInt32(int? value, string hex) =>
            AssertModel(new BasicNullableInt32 { Value = value }, hex, obj => {
                Assert.Equal(value, obj.Value);
            });

        [ProtoContract]
        public class BasicNullableInt32
        {
            [ProtoMember(1)]
            public int? Value { get; set; }
        }

        [Theory]
        [InlineData(null, "")] // nothing to write
        [InlineData(0, "0A-00")] // we want to emulate wrappers.proto, which uses zero-default
        [InlineData(1, "0A-02-08-01")] // non-zero: fine to write
        [InlineData(2, "0A-02-08-02")] // non-zero: fine to write
        public void ValidateWrappedNullableInt32(int? value, string hex) =>
            AssertModel(new WrappedNullableInt32 { Value = value }, hex, obj => {
                Assert.Equal(value, obj.Value);
            });

        [ProtoContract]
        public class WrappedNullableInt32
        {
            [ProtoMember(1)]
            [NullWrappedValue]
            public int? Value { get; set; }
        }

        [Theory]
        [InlineData(null, "")] // nothing to write
        [InlineData(0, "0B-0C")] // use zero-default rules, as per ValidateWrappedNullableInt32 (even though not really wrappers.proto)
        [InlineData(1, "0B-08-01-0C")] // non-zero: fine to write
        [InlineData(2, "0B-08-02-0C")] // non-zero: fine to write
        public void ValidateWrappedGroupedNullableInt32(int? value, string hex) =>
            AssertModel(new WrappedGroupedNullableInt32 { Value = value }, hex, obj => {
                Assert.Equal(value, obj.Value);
            });

        [ProtoContract]
        public class WrappedGroupedNullableInt32
        {
            [ProtoMember(1)]
            [NullWrappedValue(AsGroup = true)]
            public int? Value { get; set; }
        }

        private void AssertModel<T>(T value, string expectedHex, Action<T> assert, Action<RuntimeTypeModel> prepare = null, [CallerMemberName] string name = null)
        {
            var runtimeModel = RuntimeTypeModel.Create();
            runtimeModel.AutoCompile = false;
            prepare?.Invoke(runtimeModel);
            _ = runtimeModel[typeof(T)]; // make sure we touch T

            Execute(runtimeModel, "runtime"); // runtime-only
            runtimeModel.CompileInPlace(); // compile-in-place
            Execute(runtimeModel, nameof(runtimeModel.CompileInPlace)); // runtime-only
            Execute(runtimeModel.Compile(), nameof(runtimeModel.Compile)); // in-memory compile
            Execute(PEVerify.CompileAndVerify(runtimeModel, name), nameof(PEVerify.CompileAndVerify)); // dll compile

            void Execute(TypeModel serializeModel, string scenario)
            {
                try
                {
                    try
                    {
                        assert?.Invoke(value);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Test assertion failed on original input: " + ex.Message, ex);
                    }
                    using var ms = new MemoryStream();
                    serializeModel.Serialize<T>(ms, value);
                    if (!ms.TryGetBuffer(out var segment))
                        segment = new ArraySegment<byte>(ms.ToArray());
                    var actualHex = BitConverter.ToString(segment.Array, segment.Offset, segment.Count);
                    Assert.Equal(expectedHex, actualHex);

                    ms.Position = 0;
                    var clone = serializeModel.Deserialize<T>(ms);
                    assert?.Invoke(clone);
                }
                catch (Exception ex)
                {
                    Log($"{scenario}: {ex.Message}");
                    ex.Data?.Add("scenario", scenario);
                    throw;
                }
            }
        }
    }
}
