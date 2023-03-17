using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.ComponentModel;
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
        {
            _log = log;
            Log("Working folder: " + Directory.GetCurrentDirectory());
        }
        private void Log(string message)
            => _log?.WriteLine(message);

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

        [Theory]
        [InlineData(typeof(HazInvalidDefaultValue), "NullWrappedValue cannot be used with default values")]
        [InlineData(typeof(HazInvalidDataFormat), "NullWrappedValue can only be used with DataFormat.Default")]
        [InlineData(typeof(HazInvalidPacked), "NullWrappedValue cannot be used with packed values")]
        [InlineData(typeof(HazInvalidReqired), "NullWrappedValue cannot be used with required values")]
        [InlineData(typeof(HazInvalidNonNullableValue), "NullWrappedValue cannot be used with non-nullable values")]
        [InlineData(typeof(HazInvalidMessageValue), "NullWrappedValue can only be used with scalar types, or in a collection")]
        [InlineData(typeof(HazInvalidDecimal_Bcl), "NullWrappedValue can only be used with scalar types, or in a collection")]
        [InlineData(typeof(HazInvalidGuid_Bcl), "NullWrappedValue can only be used with scalar types, or in a collection")]
        [InlineData(typeof(HazInvalidGuid_Bytes), "NullWrappedValue can only be used with DataFormat.Default")]
        public void DetectInvalidConfiguration(Type type, string message)
        {
            var ex = Assert.Throws<NotSupportedException>(() =>
            {
                var model = RuntimeTypeModel.Create();
                _ = model[type];
                model.CompileInPlace();
            });
            Assert.Equal(message, ex.Message);
        }

        [Theory]
        [InlineData(typeof(HazValidDecimal_String))]
        [InlineData(typeof(HazValidGuid_String))]
        public void DetectValidConfiguration(Type type)
        {
            var model = RuntimeTypeModel.Create();
            _ = model[type];
            model.CompileInPlace();
        }

        [ProtoContract]
        public class HazInvalidDefaultValue
        {
            [ProtoMember(1)]
            [NullWrappedValue]
            [DefaultValue(42)]
            public int? Value { get; set; }
        }

        [ProtoContract]
        public class HazInvalidDataFormat
        {
            [ProtoMember(1, DataFormat = DataFormat.ZigZag)]
            [NullWrappedValue]
            public int? Value { get; set; }
        }

        [ProtoContract]
        public class HazInvalidPacked
        {
            [ProtoMember(1, IsPacked = true)]
            [NullWrappedValue]
            public int? Value { get; set; }
        }

        [ProtoContract]
        public class HazInvalidReqired
        {
            [ProtoMember(1, IsRequired = true)]
            [NullWrappedValue]
            public int? Value { get; set; }
        }

        [ProtoContract]
        public class HazInvalidNonNullableValue
        {
            [ProtoMember(1)]
            [NullWrappedValue]
            public int Value { get; set; }
        }

        [ProtoContract]
        public class HazInvalidMessageValue
        {
            [ProtoMember(1)]
            [NullWrappedValue]
            public SomeMessageType Value { get; set; }
        }

        [ProtoContract]
        public class HazInvalidDecimal_Bcl
        {
            [ProtoMember(1)]
            [NullWrappedValue]
            [CompatibilityLevel(CompatibilityLevel.Level240)]
            public decimal? Value { get; set; }
        }

        [ProtoContract]
        public class HazValidDecimal_String
        {
            [ProtoMember(1)]
            [NullWrappedValue]
            [CompatibilityLevel(CompatibilityLevel.Level300)]
            public decimal? Value { get; set; }
        }

        [ProtoContract]
        public class HazInvalidGuid_Bcl
        {
            [ProtoMember(1)]
            [NullWrappedValue]
            [CompatibilityLevel(CompatibilityLevel.Level240)]
            public Guid? Value { get; set; }
        }

        [ProtoContract]
        public class HazInvalidGuid_Bytes
        {
            [ProtoMember(1, DataFormat = DataFormat.FixedSize)]
            [NullWrappedValue]
            [CompatibilityLevel(CompatibilityLevel.Level300)]
            public Guid? Value { get; set; }
        }

        [ProtoContract]
        public class HazValidGuid_String
        {
            [ProtoMember(1)]
            [NullWrappedValue]
            [CompatibilityLevel(CompatibilityLevel.Level300)]
            public Guid? Value { get; set; }
        }

        [ProtoContract]
        public class SomeMessageType { }

        private void AssertModel<T>(T value, string expectedHex, Action<T> assert, Action<RuntimeTypeModel> prepare = null, [CallerMemberName] string name = null)
        {
            var runtimeModel = RuntimeTypeModel.Create();
            runtimeModel.AutoCompile = false;
            prepare?.Invoke(runtimeModel);
            _ = runtimeModel[typeof(T)]; // make sure we touch T

            Execute(() => runtimeModel, "runtime"); // runtime-only
            Execute(() =>
            {
                runtimeModel.CompileInPlace(); // compile-in-place
                return runtimeModel;
            }, nameof(runtimeModel.CompileInPlace)); // runtime-only
            Execute(() => runtimeModel.Compile(), nameof(runtimeModel.Compile)); // in-memory compile
            Execute(() => PEVerify.CompileAndVerify(runtimeModel, nameof(NullWrappedValueTests) + "_" + name), nameof(PEVerify.CompileAndVerify)); // dll compile

            void Execute(Func<TypeModel> factory, string scenario)
            {
                try
                {
                    var serializeModel = factory();
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
