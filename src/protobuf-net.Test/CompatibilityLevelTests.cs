using ProtoBuf.Internal;
using ProtoBuf.Meta;
using ProtoBuf.Test.TestCompatibilityLevel;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test
{
    public class CompatibilityLevelTests
    {
        private readonly ITestOutputHelper _log;
        private void Log(string message) => _log?.WriteLine(message);
        public CompatibilityLevelTests(ITestOutputHelper log) => _log = log;

        void Test<T>(CompatibilityLevel type, CompatibilityLevel Int32,
            CompatibilityLevel DateTime, CompatibilityLevel TimeSpan,
            CompatibilityLevel Guid, CompatibilityLevel Decimal,
            string expectedSchema)
        {
            Assert.Equal(type, TypeCompatibilityHelper.GetTypeCompatibilityLevel(typeof(T), default));

            var tm = RuntimeTypeModel.Create();
            var metaType = tm.Add<T>();
            Assert.Equal(type, metaType.CompatibilityLevel);
            if (Int32 != default) AssertField<int>(1, nameof(Int32), Int32);
            if (DateTime != default) AssertField<DateTime>(2, nameof(DateTime), DateTime);
            if (TimeSpan != default) AssertField<TimeSpan>(3, nameof(TimeSpan), TimeSpan);
            if (Guid != default) AssertField<Guid>(4, nameof(Guid), Guid);
            if (Decimal != default) AssertField<decimal>(5, nameof(Decimal), Decimal);

            var actualSchema = tm.GetSchema(typeof(T), ProtoSyntax.Proto3);
            Log(actualSchema);
            Assert.Equal(expectedSchema.Trim(), actualSchema.Trim(), ignoreWhiteSpaceDifferences: true, ignoreLineEndingDifferences: true);

            void AssertField<TField>(int fieldNumber, string name, CompatibilityLevel expected)
            {
                var field = metaType[fieldNumber];
                Assert.Equal(fieldNumber, field.FieldNumber);
                Assert.Equal(name, field.Name);
                Assert.Equal(typeof(TField), field.MemberType);
                Assert.Equal(expected, ValueMember.GetEffectiveCompatibilityLevel(field.CompatibilityLevel, field.DataFormat));
            }
        }

        [Fact]
        public void TestAllDefault() => Test<AllDefault>(
            CompatibilityLevel.Level200, CompatibilityLevel.Level200, CompatibilityLevel.Level200,
            CompatibilityLevel.Level200, CompatibilityLevel.Level200, CompatibilityLevel.Level200, @"
syntax = ""proto3"";
package ProtoBuf.Test;
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types

message AllDefault {
   int32 Int32 = 1;
   .bcl.DateTime DateTime = 2;
   .bcl.TimeSpan TimeSpan = 3;
   .bcl.Guid Guid = 4; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   .bcl.Decimal Decimal = 5;
}");

        [ProtoContract]
        public class AllDefault
        {
            [ProtoMember(1)]
            public int Int32 { get; set; }
            [ProtoMember(2)]
            public DateTime DateTime { get; set; }
            [ProtoMember(3)]
            public TimeSpan TimeSpan { get; set; }
            [ProtoMember(4)]
            public Guid Guid { get; set; }
            [ProtoMember(5)]
            public decimal Decimal { get; set; }
        }

        [Fact]
        public void TestExplicitNotSpecified() => Test<ExplicitNotSpecified>(
            CompatibilityLevel.Level200, CompatibilityLevel.Level200, CompatibilityLevel.Level200,
            CompatibilityLevel.Level200, CompatibilityLevel.Level200, CompatibilityLevel.Level200, @"
syntax = ""proto3"";
package ProtoBuf.Test;
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types

message ExplicitNotSpecified {
   int32 Int32 = 1;
   .bcl.DateTime DateTime = 2;
   .bcl.TimeSpan TimeSpan = 3;
   .bcl.Guid Guid = 4; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   .bcl.Decimal Decimal = 5;
}");

        [CompatibilityLevel(CompatibilityLevel.NotSpecified)]
        [ProtoContract]
        public class ExplicitNotSpecified
        {
            [ProtoMember(1)]
            public int Int32 { get; set; }
            [ProtoMember(2)]
            public DateTime DateTime { get; set; }
            [ProtoMember(3)]
            public TimeSpan TimeSpan { get; set; }
            [ProtoMember(4)]
            public Guid Guid { get; set; }
            [ProtoMember(5)]
            public decimal Decimal { get; set; }
        }

        [Fact]
        public void TestExplicitLevel200() => Test<ExplicitLevel200>(
            CompatibilityLevel.Level200, CompatibilityLevel.Level200, CompatibilityLevel.Level200,
            CompatibilityLevel.Level200, CompatibilityLevel.Level200, CompatibilityLevel.Level200, @"
syntax = ""proto3"";
package ProtoBuf.Test;
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types

message ExplicitLevel200 {
   int32 Int32 = 1;
   .bcl.DateTime DateTime = 2;
   .bcl.TimeSpan TimeSpan = 3;
   .bcl.Guid Guid = 4; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   .bcl.Decimal Decimal = 5;
}");

        [CompatibilityLevel(CompatibilityLevel.Level200)]
        [ProtoContract]
        public class ExplicitLevel200
        {
            [ProtoMember(1)]
            public int Int32 { get; set; }
            [ProtoMember(2)]
            public DateTime DateTime { get; set; }
            [ProtoMember(3)]
            public TimeSpan TimeSpan { get; set; }
            [ProtoMember(4)]
            public Guid Guid { get; set; }
            [ProtoMember(5)]
            public decimal Decimal { get; set; }
        }

        [Fact]
        public void TestDataFormatWellKnown() => Test<DataFormatWellKnown>(
            CompatibilityLevel.Level200, CompatibilityLevel.Level240, CompatibilityLevel.Level240,
            CompatibilityLevel.Level240, CompatibilityLevel.Level240, CompatibilityLevel.Level240, @"
syntax = ""proto3"";
package ProtoBuf.Test;
import ""google/protobuf/duration.proto"";
import ""google/protobuf/timestamp.proto"";
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types

message DataFormatWellKnown {
   int32 Int32 = 1;
   .google.protobuf.Timestamp DateTime = 2;
   .google.protobuf.Duration TimeSpan = 3;
   .bcl.Guid Guid = 4; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   .bcl.Decimal Decimal = 5;
}");

        [ProtoContract]
        public class DataFormatWellKnown
        {
#pragma warning disable CS0618
            [ProtoMember(1, DataFormat = DataFormat.WellKnown)]
            public int Int32 { get; set; }
            [ProtoMember(2, DataFormat = DataFormat.WellKnown)]
            public DateTime DateTime { get; set; }
            [ProtoMember(3, DataFormat = DataFormat.WellKnown)]
            public TimeSpan TimeSpan { get; set; }
            [ProtoMember(4, DataFormat = DataFormat.WellKnown)]
            public Guid Guid { get; set; }
            [ProtoMember(5, DataFormat = DataFormat.WellKnown)]
            public decimal Decimal { get; set; }
#pragma warning restore CS0618
        }

        [Fact]
        public void TestInheritedAllDefault() => Test<InheritedAllDefault>(
            CompatibilityLevel.Level200, CompatibilityLevel.Level200, CompatibilityLevel.Level200,
            CompatibilityLevel.Level200, CompatibilityLevel.Level200, CompatibilityLevel.Level200, @"
syntax = ""proto3"";
package ProtoBuf.Test;
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types

message BaseAllDefault {
   oneof subtype {
      InheritedAllDefault InheritedAllDefault = 1;
   }
}
message InheritedAllDefault {
   int32 Int32 = 1;
   .bcl.DateTime DateTime = 2;
   .bcl.TimeSpan TimeSpan = 3;
   .bcl.Guid Guid = 4; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   .bcl.Decimal Decimal = 5;
}");

        [ProtoContract, ProtoInclude(1, typeof(InheritedAllDefault))]
        public class BaseAllDefault { }
        [ProtoContract]
        public class InheritedAllDefault : BaseAllDefault
        {
            [ProtoMember(1)]
            public int Int32 { get; set; }
            [ProtoMember(2)]
            public DateTime DateTime { get; set; }
            [ProtoMember(3)]
            public TimeSpan TimeSpan { get; set; }
            [ProtoMember(4)]
            public Guid Guid { get; set; }
            [ProtoMember(5)]
            public decimal Decimal { get; set; }
        }

        [Fact]
        public void TestInheritedBaseDetermines() => Test<InheritedBaseDetermines>(
            CompatibilityLevel.Level240, CompatibilityLevel.Level240, CompatibilityLevel.Level240,
            CompatibilityLevel.Level240, CompatibilityLevel.Level240, CompatibilityLevel.Level240, @"
syntax = ""proto3"";
package ProtoBuf.Test;
import ""google/protobuf/duration.proto"";
import ""google/protobuf/timestamp.proto"";
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types

message BaseBaseDetermines {
   oneof subtype {
      InheritedBaseDetermines InheritedBaseDetermines = 1;
   }
}
message InheritedBaseDetermines {
   int32 Int32 = 1;
   .google.protobuf.Timestamp DateTime = 2;
   .google.protobuf.Duration TimeSpan = 3;
   .bcl.Guid Guid = 4; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   .bcl.Decimal Decimal = 5;
}");

        [CompatibilityLevel(CompatibilityLevel.Level240)]
        [ProtoContract, ProtoInclude(1, typeof(InheritedBaseDetermines))]
        public class BaseBaseDetermines { }
        [ProtoContract]
        public class InheritedBaseDetermines : BaseBaseDetermines
        {
            [ProtoMember(1)]
            public int Int32 { get; set; }
            [ProtoMember(2)]
            public DateTime DateTime { get; set; }
            [ProtoMember(3)]
            public TimeSpan TimeSpan { get; set; }
            [ProtoMember(4)]
            public Guid Guid { get; set; }
            [ProtoMember(5)]
            public decimal Decimal { get; set; }
        }

        [Fact]
        public void TestInheritedDerivedDetermines() => Test<InheritedDerivedDetermines>(
            CompatibilityLevel.Level300, CompatibilityLevel.Level300, CompatibilityLevel.Level300,
            CompatibilityLevel.Level300, CompatibilityLevel.Level300, CompatibilityLevel.Level300, @"
syntax = ""proto3"";
package ProtoBuf.Test;
import ""google/protobuf/duration.proto"";
import ""google/protobuf/timestamp.proto"";

message InheritedDerivedDetermines {
   int32 Int32 = 1;
   .google.protobuf.Timestamp DateTime = 2;
   .google.protobuf.Duration TimeSpan = 3;
   string Guid = 4; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   string Decimal = 5;
}");

        [CompatibilityLevel(CompatibilityLevel.Level240)]
        [ProtoContract, ProtoInclude(1, typeof(InheritedBaseDetermines))]
        public class BaseDerivedDetermines { }
        [ProtoContract]
        [CompatibilityLevel(CompatibilityLevel.Level300)]
        public class InheritedDerivedDetermines : BaseDerivedDetermines
        {
            [ProtoMember(1)]
            public int Int32 { get; set; }
            [ProtoMember(2)]
            public DateTime DateTime { get; set; }
            [ProtoMember(3)]
            public TimeSpan TimeSpan { get; set; }
            [ProtoMember(4)]
            public Guid Guid { get; set; }
            [ProtoMember(5)]
            public decimal Decimal { get; set; }
        }


        [Fact]
        public void TestAllDefaultWithModuleLevel() => Test<AllDefaultWithModuleLevel>(
            CompatibilityLevel.Level300, CompatibilityLevel.Level300, CompatibilityLevel.Level300,
            CompatibilityLevel.Level300, CompatibilityLevel.Level300, CompatibilityLevel.Level300, @"
syntax = ""proto3"";
package ProtoBuf.Test.TestCompatibilityLevel;
import ""google/protobuf/duration.proto"";
import ""google/protobuf/timestamp.proto"";

message AllDefaultWithModuleLevel {
   int32 Int32 = 1;
   .google.protobuf.Timestamp DateTime = 2;
   .google.protobuf.Duration TimeSpan = 3;
   string Guid = 4; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   string Decimal = 5;
}");

        [Fact]
        public void AllKnownLevelsAreValid()
        {
            foreach (CompatibilityLevel level in Enum.GetValues(typeof(CompatibilityLevel)))
            {
                CompatibilityLevelAttribute.AssertValid(level);
            }
        }

        [Theory]
        [InlineData(42)]
        [InlineData(-1)]
        [InlineData(int.MaxValue)]
        [InlineData(int.MinValue)]
        [InlineData(205)]
        public void InvalidLevelsAreDetected(int value)
        {
            var ex = Assert.Throws<ArgumentOutOfRangeException>("compatibilityLevel", () =>
                CompatibilityLevelAttribute.AssertValid((CompatibilityLevel)value));
            Assert.StartsWith($"Compatiblity level '{value}' is not recognized.", ex.Message);
        }

        [Fact]
        public void InvalidLevelDetectedAtType()
            => Assert.Throws<ArgumentOutOfRangeException>("compatibilityLevel", () => RuntimeTypeModel.Create().Add<InvalidCompatibilityLevelForType>());

        [CompatibilityLevel((CompatibilityLevel)42)]
        [ProtoContract]
        public class InvalidCompatibilityLevelForType
        {
            [ProtoMember(1)]
            public int Value { get; set; }
        }

        [Fact]
        public void InvalidLevelDetectedAtMember()
            => Assert.Throws<ArgumentOutOfRangeException>("compatibilityLevel", () => RuntimeTypeModel.Create().Add<InvalidCompatibilityLevelForType>());

        [ProtoContract]
        public class InvalidCompatibilityLevelForMember
        {
            [ProtoMember(1)]
            [CompatibilityLevel((CompatibilityLevel)42)]
            public int Value { get; set; }
        }

        [Fact]
        public void RoundTripGuid300()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<HazGuid>();

            Check(model);
            model.CompileInPlace();
            Check(model);
            Check(model.Compile());

            static void Check(TypeModel model)
            {
                var guid = Guid.Parse("9d058b10-c153-43a5-a18a-070492c41c22");
                var obj = new HazGuid { String = guid, Bytes = guid };
                using var ms = new MemoryStream();
                model.Serialize(ms, obj);
                var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                Assert.Equal("0A-24-39-64-30-35-38-62-31-30-2D-63-31-35-33-2D-34-33-61-35-2D-61-31-38-61-2D-30-37-30-34-39-32-63-34-31-63-32-32-12-10-9D-05-8B-10-C1-53-43-A5-A1-8A-07-04-92-C4-1C-22", hex);
                ms.Position = 0;
                var clone = model.Deserialize<HazGuid>(ms);
                Assert.Equal(guid, clone.String);
                Assert.Equal(guid, clone.Bytes);
            }
        }

        [Fact]
        public void Guid300Schema()
        {
            var model = RuntimeTypeModel.Create();
            model.Add<HazGuid>();
            var schema = model.GetSchema(typeof(HazGuid), ProtoSyntax.Proto3);
            Log(schema);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;

message HazGuid {
   string String = 1; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   bytes Bytes = 2; // default value could not be applied: 00000000-0000-0000-0000-000000000000
}
", schema, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void CheckExpectedGuidHex()
        {
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, new HazGuidRaw {
                String = "9d058b10-c153-43a5-a18a-070492c41c22",
                Bytes = new byte[] { 0x9d, 0x05, 0x8b, 0x10, 0xc1, 0x53, 0x43, 0xa5, 0xa1, 0x8a, 0x07, 0x04, 0x92, 0xc4, 0x1c, 0x22 }
            });
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Log(hex);
            Assert.Equal("0A-24-39-64-30-35-38-62-31-30-2D-63-31-35-33-2D-34-33-61-35-2D-61-31-38-61-2D-30-37-30-34-39-32-63-34-31-63-32-32-12-10-9D-05-8B-10-C1-53-43-A5-A1-8A-07-04-92-C4-1C-22", hex);
            /*
            0A = field 1, type String
            24 = length 36
            payload = 39-64-30-35-38-62-31-30-2D-63-31-35-33-2D-34-33-61-35-2D-61-31-38-61-2D-30-37-30-34-39-32-63-34-31-63-32-32
            UTF8: 9d058b10-c153-43a5-a18a-070492c41c22

            12 = field 2, type String
            10 = length 16
            payload = 9D-05-8B-10-C1-53-43-A5-A1-8A-07-04-92-C4-1C-22
            */
        }

        [ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
        public class HazGuid
        {
            [ProtoMember(1)]
            public Guid String { get; set; }

            [ProtoMember(2, DataFormat = DataFormat.FixedSize)]
            public Guid Bytes { get; set; }
        }

        [ProtoContract]
        public class HazGuidRaw
        {
            [ProtoMember(1)]
            public string String { get; set; }
            [ProtoMember(2)]
            public byte[] Bytes { get; set; }
        }


        [Fact]
        public void RoundTripDecimal300()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<HazDecimal>();

            Check(model);
            model.CompileInPlace();
            Check(model);
            Check(model.Compile());

            static void Check(TypeModel model)
            {
                var obj = new HazDecimal { Value = 123.450M };
                using var ms = new MemoryStream();
                model.Serialize(ms, obj);
                var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
                Assert.Equal("0A-07-31-32-33-2E-34-35-30", hex);
                ms.Position = 0;
                var clone = model.Deserialize<HazDecimal>(ms);
                Assert.Equal(123.450M, clone.Value);
            }
        }


        [Fact]
        public void Decimal300Schema()
        {
            var model = RuntimeTypeModel.Create();
            model.Add<HazDecimal>();
            var schema = model.GetSchema(typeof(HazDecimal), ProtoSyntax.Proto3);
            Log(schema);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;

message HazDecimal {
   string Value = 1;
}
", schema, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void CheckExpectedDecimalHex()
        {
            using var ms = new MemoryStream();
            Serializer.Serialize(ms, new HazDecimalRaw
            {
                Value = "123.450"
            });
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Log(hex);
            Assert.Equal("0A-07-31-32-33-2E-34-35-30", hex);
            /*
            0A = field 1, type String
            07 = length 7
            payload = 31-32-33-2E-34-35-30
            UTF8: 123.450
            */
        }

        [ProtoContract, CompatibilityLevel(CompatibilityLevel.Level300)]
        public class HazDecimal
        {
            [ProtoMember(1)]
            public decimal Value { get; set; }
        }

        [ProtoContract]
        public class HazDecimalRaw
        {
            [ProtoMember(1)]
            public string Value { get; set; }
        }


        [Theory]
        [InlineData(typeof(int), CompatibilityLevel.Level200, DataFormat.Default, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(DateTime), CompatibilityLevel.Level200, DataFormat.Default, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(TimeSpan), CompatibilityLevel.Level200, DataFormat.Default, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(Guid), CompatibilityLevel.Level200, DataFormat.Default, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(decimal), CompatibilityLevel.Level200, DataFormat.Default, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(CultureInfo), CompatibilityLevel.Level200, DataFormat.Default, null)]

#pragma warning disable CS0618
        [InlineData(typeof(int), CompatibilityLevel.Level200, DataFormat.WellKnown, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(DateTime), CompatibilityLevel.Level200, DataFormat.WellKnown, typeof(Level240DefaultSerializer))]
        [InlineData(typeof(TimeSpan), CompatibilityLevel.Level200, DataFormat.WellKnown, typeof(Level240DefaultSerializer))]
        [InlineData(typeof(Guid), CompatibilityLevel.Level200, DataFormat.WellKnown, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(decimal), CompatibilityLevel.Level200, DataFormat.WellKnown, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(CultureInfo), CompatibilityLevel.Level200, DataFormat.WellKnown, null)]
#pragma warning restore CS0618

        [InlineData(typeof(int), CompatibilityLevel.Level240, DataFormat.Default, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(DateTime), CompatibilityLevel.Level240, DataFormat.Default, typeof(Level240DefaultSerializer))]
        [InlineData(typeof(TimeSpan), CompatibilityLevel.Level240, DataFormat.Default, typeof(Level240DefaultSerializer))]
        [InlineData(typeof(Guid), CompatibilityLevel.Level240, DataFormat.Default, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(decimal), CompatibilityLevel.Level240, DataFormat.Default, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(CultureInfo), CompatibilityLevel.Level240, DataFormat.Default, null)]

        [InlineData(typeof(int), CompatibilityLevel.Level300, DataFormat.Default, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(DateTime), CompatibilityLevel.Level300, DataFormat.Default, typeof(Level300DefaultSerializer))]
        [InlineData(typeof(TimeSpan), CompatibilityLevel.Level300, DataFormat.Default, typeof(Level300DefaultSerializer))]
        [InlineData(typeof(Guid), CompatibilityLevel.Level300, DataFormat.Default, typeof(Level300DefaultSerializer))]
        [InlineData(typeof(decimal), CompatibilityLevel.Level300, DataFormat.Default, typeof(Level300DefaultSerializer))]
        [InlineData(typeof(CultureInfo), CompatibilityLevel.Level300, DataFormat.Default, null)]

        [InlineData(typeof(int), CompatibilityLevel.Level300, DataFormat.FixedSize, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(DateTime), CompatibilityLevel.Level300, DataFormat.FixedSize, typeof(Level300DefaultSerializer))]
        [InlineData(typeof(TimeSpan), CompatibilityLevel.Level300, DataFormat.FixedSize, typeof(Level300DefaultSerializer))]
        [InlineData(typeof(Guid), CompatibilityLevel.Level300, DataFormat.FixedSize, typeof(Level300FixedSerializer))]
        [InlineData(typeof(decimal), CompatibilityLevel.Level300, DataFormat.FixedSize, typeof(Level300DefaultSerializer))]
        [InlineData(typeof(CultureInfo), CompatibilityLevel.Level300, DataFormat.FixedSize, null)]

        [InlineData(typeof(int), OverHighLevel, DataFormat.FixedSize, typeof(PrimaryTypeProvider))]
        [InlineData(typeof(DateTime), OverHighLevel, DataFormat.FixedSize, typeof(Level300DefaultSerializer))]
        [InlineData(typeof(TimeSpan), OverHighLevel, DataFormat.FixedSize, typeof(Level300DefaultSerializer))]
        [InlineData(typeof(Guid), OverHighLevel, DataFormat.FixedSize, typeof(Level300FixedSerializer))]
        [InlineData(typeof(decimal), OverHighLevel, DataFormat.FixedSize, typeof(Level300DefaultSerializer))]
        [InlineData(typeof(CultureInfo), OverHighLevel, DataFormat.FixedSize, null)]
        public void ResolveInbuiltSerializerType(Type type, CompatibilityLevel compatibilityLevel, DataFormat dataFormat, Type expectedType)
            => s_Generic.MakeGenericMethod(type).Invoke(null, new object[] { ValueMember.GetEffectiveCompatibilityLevel(compatibilityLevel, dataFormat), dataFormat, expectedType });
        const CompatibilityLevel OverHighLevel = (CompatibilityLevel)21342345;

        private static readonly MethodInfo s_Generic = typeof(CompatibilityLevelTests).GetMethod(nameof(ResolveInbuiltSerializerTypeImpl), BindingFlags.Static | BindingFlags.NonPublic);

        private static void ResolveInbuiltSerializerTypeImpl<T>(CompatibilityLevel compatibilityLevel, DataFormat dataFormat, Type expectedType)
        {
            var serializer = TypeModel.GetInbuiltSerializer<T>(compatibilityLevel, dataFormat);
            if (expectedType is null)
            {
                Assert.Null(serializer);
            }
            else
            {
                Assert.IsType(expectedType, serializer);
            }
        }
    }
}
