using ProtoBuf.Internal;
using ProtoBuf.Meta;
using ProtoBuf.Test.TestCompatibilityLevel;
using System;
using System.Runtime.CompilerServices;
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
            Assert.Equal(type, TypeCompatibilityHelper.GetLevel(typeof(T)));

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
                Assert.Equal(expected, field.EffectiveCompatibilityLevel);
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
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types
import ""google/protobuf/timestamp.proto"";
import ""google/protobuf/duration.proto"";

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
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types
import ""google/protobuf/timestamp.proto"";
import ""google/protobuf/duration.proto"";

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
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types
import ""google/protobuf/timestamp.proto"";
import ""google/protobuf/duration.proto"";

message InheritedDerivedDetermines {
   int32 Int32 = 1;
   .google.protobuf.Timestamp DateTime = 2;
   .google.protobuf.Duration TimeSpan = 3;
   .bcl.Guid Guid = 4; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   .bcl.Decimal Decimal = 5;
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
import ""protobuf-net/bcl.proto""; // schema for protobuf-net's handling of core .NET types
import ""google/protobuf/timestamp.proto"";
import ""google/protobuf/duration.proto"";

message AllDefaultWithModuleLevel {
   int32 Int32 = 1;
   .google.protobuf.Timestamp DateTime = 2;
   .google.protobuf.Duration TimeSpan = 3;
   .bcl.Guid Guid = 4; // default value could not be applied: 00000000-0000-0000-0000-000000000000
   .bcl.Decimal Decimal = 5;
}");

        [Fact]
        public void TestAllDefaultWithModuleLevel_Clean() => Test<AllDefaultWithModuleLevel_Clean>(
    CompatibilityLevel.Level300, CompatibilityLevel.Level300, CompatibilityLevel.Level300,
    CompatibilityLevel.Level300, default, default, @"
syntax = ""proto3"";
package ProtoBuf.Test.TestCompatibilityLevel;
import ""google/protobuf/timestamp.proto"";
import ""google/protobuf/duration.proto"";

message AllDefaultWithModuleLevel_Clean {
   int32 Int32 = 1;
   .google.protobuf.Timestamp DateTime = 2;
   .google.protobuf.Duration TimeSpan = 3;
}");

        [Fact]
        public void AllKnownLevelsAreValid()
        {
            foreach(CompatibilityLevel level in Enum.GetValues(typeof(CompatibilityLevel)))
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
    }
}
