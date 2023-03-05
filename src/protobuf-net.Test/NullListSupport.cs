using ProtoBuf.Meta;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ProtoBuf.Test
{
    public class NullListSupport
    {
        private readonly ITestOutputHelper _log;
        public NullListSupport(ITestOutputHelper log) => _log = log;
        public interface IHazLists
        {
            List<int> Int32List { get; set; }
            List<int?> NullableInt32List { get; set; }
            List<string> StringList { get; set; }
            List<SomePoco> PocoList { get; set; }
        }
        [ProtoContract]
        public class SomePoco
        {
            public SomePoco() { } // for serialization
            public SomePoco(int value) => Value = value;

            [ProtoMember(1)]
            public int Value { get; set; }
            public override int GetHashCode() => Value;
            public override bool Equals(object obj) => obj is SomePoco other && other.Value == Value;
        }

        [ProtoContract]
        public class BasicLists : IHazLists
        {
            [ProtoMember(1)]
            public List<int> Int32List { get; set; }
            [ProtoMember(2)]
            public List<int?> NullableInt32List { get; set; }
            [ProtoMember(3)]
            public List<string> StringList { get; set; }
            [ProtoMember(4)]
            public List<SomePoco> PocoList { get; set; }
        }

        [ProtoContract]
        public class BasicPackedLists : IHazLists
        {
            [ProtoMember(1, IsPacked = true)]
            public List<int> Int32List { get; set; }
            [ProtoMember(2, IsPacked = true)]
            public List<int?> NullableInt32List { get; set; }
            [ProtoMember(3)]
            public List<string> StringList { get; set; }
            [ProtoMember(4)]
            public List<SomePoco> PocoList { get; set; }
        }

        [ProtoContract]
        public class BasicGroupedLists : IHazLists
        {
            [ProtoMember(1)]
            public List<int> Int32List { get; set; }
            [ProtoMember(2)]
            public List<int?> NullableInt32List { get; set; }
            [ProtoMember(3)]
            public List<string> StringList { get; set; }
            [ProtoMember(4, DataFormat = DataFormat.Group)]
            public List<SomePoco> PocoList { get; set; }
        }

        [ProtoContract]
        public class BasicPackedGroupedLists : IHazLists
        {
            [ProtoMember(1, IsPacked = true)]
            public List<int> Int32List { get; set; }
            [ProtoMember(2, IsPacked = true)]
            public List<int?> NullableInt32List { get; set; }
            [ProtoMember(3)]
            public List<string> StringList { get; set; }
            [ProtoMember(4, DataFormat = DataFormat.Group)]
            public List<SomePoco> PocoList { get; set; }
        }

        public enum Scenario
        {
            Null,
            Empty,
            SingleNotNull,
            MultipleNotNull,
            SingleNull,
            MultipleWithNull,
        }

        public enum Outcome
        {
            Null,
            AllContents,
            SerializeFail,
        }

        [Fact]
        public void BasicLists_Schema() => CheckSchema<BasicLists>(false, @"syntax = ""proto3"";
package ProtoBuf.Test;

message BasicLists {
    repeated int32 Int32List = 1 [packed = false];
    repeated int32 NullableInt32List = 2;
    repeated string StringList = 3;
    repeated SomePoco PocoList = 4;
}
message SomePoco {
    int32 Value = 1;
}");

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.Null, "")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "08-2A")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "08-2A-08-11-08-09-08-04")]
        [InlineData(true, Scenario.Null, Outcome.Null, "")]
        [InlineData(true, Scenario.Empty, Outcome.Null, "")]
        [InlineData(true, Scenario.SingleNotNull, Outcome.AllContents, "0B-08-2A-0C")]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.AllContents, "0B-08-2A-0C-0B-08-11-0C-0B-08-09-0C-0B-08-04-0C")]
        public void BasicLists_Int32List(bool supportNull, Scenario scenario, Outcome outcome, string hex = null)
            => RunInt32List<BasicLists>(supportNull, scenario, outcome, hex);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.Null, "")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "10-2A")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "10-2A-10-11-10-09-10-04")]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Null, Outcome.Null, "")]
        [InlineData(true, Scenario.Empty, Outcome.Null, "")]
        [InlineData(true, Scenario.SingleNotNull, Outcome.AllContents, "13-08-2A-14")]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.AllContents, "13-08-2A-14-13-08-11-14-13-08-09-14-13-08-04-14")]
        [InlineData(true, Scenario.SingleNull, Outcome.AllContents, "13-14")]
        [InlineData(true, Scenario.MultipleWithNull, Outcome.AllContents, "13-08-2A-14-13-14-13-08-11-14-13-14")]
        public void BasicLists_NullableInt32List(bool supportNull, Scenario scenario, Outcome outcome, string hex = null)
            => RunNullableInt32List<BasicLists>(supportNull, scenario, outcome, hex);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.Null, "")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "1A-03-61-62-63")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "1A-03-61-62-63-1A-03-64-65-66-1A-03-67-68-69-1A-03-6A-6B-6C")]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Null, Outcome.Null, "")]
        [InlineData(true, Scenario.Empty, Outcome.Null, "")]
        [InlineData(true, Scenario.SingleNotNull, Outcome.AllContents, "1B-0A-03-61-62-63-1C")]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.AllContents, "1B-0A-03-61-62-63-1C-1B-0A-03-64-65-66-1C-1B-0A-03-67-68-69-1C-1B-0A-03-6A-6B-6C-1C")]
        [InlineData(true, Scenario.SingleNull, Outcome.AllContents, "1B-1C")]
        [InlineData(true, Scenario.MultipleWithNull, Outcome.AllContents, "1B-0A-03-61-62-63-1C-1B-1C-1B-0A-03-64-65-66-1C-1B-1C")]
        public void BasicLists_StringList(bool supportNull, Scenario scenario, Outcome outcome, string hex = null)
            => RunStringList<BasicLists>(supportNull, scenario, outcome, hex);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.Null, "")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "22-02-08-2A")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "22-02-08-2A-22-02-08-0C-22-02-08-09-22-02-08-04")]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail, "", true)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail, "", true)]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail, "", false)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail, "", false)]
        [InlineData(true, Scenario.Null, Outcome.Null, "")]
        [InlineData(true, Scenario.Empty, Outcome.Null, "")]
        [InlineData(true, Scenario.SingleNotNull, Outcome.AllContents, "23-0A-02-08-2A-24")]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.AllContents, "23-0A-02-08-2A-24-23-0A-02-08-0C-24-23-0A-02-08-09-24-23-0A-02-08-04-24")]
        [InlineData(true, Scenario.SingleNull, Outcome.AllContents, "23-24")]
        [InlineData(true, Scenario.MultipleWithNull, Outcome.AllContents, "23-0A-02-08-2A-24-23-24-23-0A-02-08-0C-24-23-24")]
        public void BasicLists_PocoList(bool supportNull, Scenario scenario, Outcome outcome, string hex = null, bool fullCompile = false)
            => RunPocoList<BasicLists>(supportNull, scenario, outcome, hex, fullCompile);

        [Fact]
        public void BasicPackedLists_Schema() => CheckSchema<BasicPackedLists>(false, @"syntax = ""proto3"";
package ProtoBuf.Test;

message BasicPackedLists {
    repeated int32 Int32List = 1;
    repeated int32 NullableInt32List = 2;
    repeated string StringList = 3;
    repeated SomePoco PocoList = 4;
}
message SomePoco {
    int32 Value = 1;
}");
        [Fact]
        public void BasicPackedLists_Schema_SupportNull() => CheckSchema<BasicPackedLists>(true, "SupportNull cannot be combined with IsPacked, NullWrappedValue or NullWrappedCollection", expectFailure: true);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.AllContents, "0A-00")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "08-2A")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "0A-04-2A-11-09-04")]
        [InlineData(true, Scenario.Null, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Empty, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.SerializeFail)]
        public void BasicPackedLists_Int32List(bool supportNull, Scenario scenario, Outcome outcome, string hex = null)
            => RunInt32List<BasicPackedLists>(supportNull, scenario, outcome, hex);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.AllContents, "12-00")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "10-2A")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "12-04-2A-11-09-04")]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Null, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Empty, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        public void BasicPackedLists_NullableInt32List(bool supportNull, Scenario scenario, Outcome outcome, string hex = null)
            => RunNullableInt32List<BasicPackedLists>(supportNull, scenario, outcome, hex);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.Null, "")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "1A-03-61-62-63")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "1A-03-61-62-63-1A-03-64-65-66-1A-03-67-68-69-1A-03-6A-6B-6C")]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Null, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Empty, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        public void BasicPackedLists_StringList(bool supportNull, Scenario scenario, Outcome outcome, string hex = null)
            => RunStringList<BasicPackedLists>(supportNull, scenario, outcome, hex);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.Null, "")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "22-02-08-2A")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "22-02-08-2A-22-02-08-0C-22-02-08-09-22-02-08-04")]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail, "", true)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail, "", true)]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail, "", false)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail, "", false)]
        [InlineData(true, Scenario.Null, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Empty, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        public void BasicPackedLists_PocoList(bool supportNull, Scenario scenario, Outcome outcome, string hex = null, bool fullCompile = false)
            => RunPocoList<BasicPackedLists>(supportNull, scenario, outcome, hex, fullCompile);

        [Fact]
        public void BasicGroupedLists_Schema() => CheckSchema<BasicGroupedLists>(false, @"syntax = ""proto3"";
package ProtoBuf.Test;

message BasicGroupedLists {
    repeated int32 Int32List = 1 [packed = false];
    repeated int32 NullableInt32List = 2;
    repeated string StringList = 3;
    repeated group SomePoco PocoList = 4;
}
message SomePoco {
    int32 Value = 1;
}");

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.Null, "")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "08-2A")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "08-2A-08-11-08-09-08-04")]
        [InlineData(true, Scenario.Null, Outcome.Null, "")]
        [InlineData(true, Scenario.Empty, Outcome.Null, "")]
        [InlineData(true, Scenario.SingleNotNull, Outcome.AllContents, "0B-08-2A-0C")]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.AllContents, "0B-08-2A-0C-0B-08-11-0C-0B-08-09-0C-0B-08-04-0C")]
        public void BasicGroupedLists_Int32List(bool supportNull, Scenario scenario, Outcome outcome, string hex = null)
            => RunInt32List<BasicGroupedLists>(supportNull, scenario, outcome, hex);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.Null, "")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "10-2A")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "10-2A-10-11-10-09-10-04")]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Null, Outcome.Null, "")]
        [InlineData(true, Scenario.Empty, Outcome.Null, "")]
        [InlineData(true, Scenario.SingleNotNull, Outcome.AllContents, "13-08-2A-14")]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.AllContents, "13-08-2A-14-13-08-11-14-13-08-09-14-13-08-04-14")]
        [InlineData(true, Scenario.SingleNull, Outcome.AllContents, "13-14")]
        [InlineData(true, Scenario.MultipleWithNull, Outcome.AllContents, "13-08-2A-14-13-14-13-08-11-14-13-14")]
        public void BasicGroupedLists_NullableInt32List(bool supportNull, Scenario scenario, Outcome outcome, string hex = null)
            => RunNullableInt32List<BasicGroupedLists>(supportNull, scenario, outcome, hex);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.Null, "")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "1A-03-61-62-63")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "1A-03-61-62-63-1A-03-64-65-66-1A-03-67-68-69-1A-03-6A-6B-6C")]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Null, Outcome.Null, "")]
        [InlineData(true, Scenario.Empty, Outcome.Null, "")]
        [InlineData(true, Scenario.SingleNotNull, Outcome.AllContents, "1B-0A-03-61-62-63-1C")]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.AllContents, "1B-0A-03-61-62-63-1C-1B-0A-03-64-65-66-1C-1B-0A-03-67-68-69-1C-1B-0A-03-6A-6B-6C-1C")]
        [InlineData(true, Scenario.SingleNull, Outcome.AllContents, "1B-1C")]
        [InlineData(true, Scenario.MultipleWithNull, Outcome.AllContents, "1B-0A-03-61-62-63-1C-1B-1C-1B-0A-03-64-65-66-1C-1B-1C")]
        public void BasicGroupedLists_StringList(bool supportNull, Scenario scenario, Outcome outcome, string hex = null)
            => RunStringList<BasicGroupedLists>(supportNull, scenario, outcome, hex);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.Null, "")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "23-08-2A-24")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "23-08-2A-24-23-08-0C-24-23-08-09-24-23-08-04-24")]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail, "", false)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail, "", false)]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail, "", true)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail, "", true)]
        [InlineData(true, Scenario.Null, Outcome.Null, "")]
        [InlineData(true, Scenario.Empty, Outcome.Null, "")]
        [InlineData(true, Scenario.SingleNotNull, Outcome.AllContents, "23-0B-08-2A-0C-24")]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.AllContents, "23-0B-08-2A-0C-24-23-0B-08-0C-0C-24-23-0B-08-09-0C-24-23-0B-08-04-0C-24")]
        [InlineData(true, Scenario.SingleNull, Outcome.AllContents, "23-24")]
        [InlineData(true, Scenario.MultipleWithNull, Outcome.AllContents, "23-0B-08-2A-0C-24-23-24-23-0B-08-0C-0C-24-23-24")]
        public void BasicGroupedLists_PocoList(bool supportNull, Scenario scenario, Outcome outcome, string hex = null, bool fullCompile = false)
            => RunPocoList<BasicGroupedLists>(supportNull, scenario, outcome, hex, fullCompile);

        [Fact]
        public void BasicPackedGroupedLists_Schema() => CheckSchema<BasicPackedGroupedLists>(false, @"syntax = ""proto3"";
package ProtoBuf.Test;

message BasicPackedGroupedLists {
    repeated int32 Int32List = 1;
    repeated int32 NullableInt32List = 2;
    repeated string StringList = 3;
    repeated group SomePoco PocoList = 4;
}
message SomePoco {
    int32 Value = 1;
}");
        [Fact]
        public void BasicPackedGroupedLists_Schema_SupportNull() => CheckSchema<BasicPackedGroupedLists>(true, "SupportNull cannot be combined with IsPacked, NullWrappedValue or NullWrappedCollection", expectFailure: true);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.AllContents, "0A-00")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "08-2A")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "0A-04-2A-11-09-04")]
        [InlineData(true, Scenario.Null, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Empty, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.SerializeFail)]
        public void BasicPackedGroupedLists_Int32List(bool supportNull, Scenario scenario, Outcome outcome, string hex = null)
            => RunInt32List<BasicPackedGroupedLists>(supportNull, scenario, outcome, hex);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.AllContents, "12-00")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "10-2A")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "12-04-2A-11-09-04")]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Null, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Empty, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        public void BasicPackedGroupedLists_NullableInt32List(bool supportNull, Scenario scenario, Outcome outcome, string hex = null)
            => RunNullableInt32List<BasicPackedGroupedLists>(supportNull, scenario, outcome, hex);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.Null, "")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "1A-03-61-62-63")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "1A-03-61-62-63-1A-03-64-65-66-1A-03-67-68-69-1A-03-6A-6B-6C")]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Null, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Empty, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        public void BasicPackedGroupedLists_StringList(bool supportNull, Scenario scenario, Outcome outcome, string hex = null)
            => RunStringList<BasicPackedGroupedLists>(supportNull, scenario, outcome, hex);

        [Theory]
        [InlineData(false, Scenario.Null, Outcome.Null, "")]
        [InlineData(false, Scenario.Empty, Outcome.Null, "")]
        [InlineData(false, Scenario.SingleNotNull, Outcome.AllContents, "23-08-2A-24")]
        [InlineData(false, Scenario.MultipleNotNull, Outcome.AllContents, "23-08-2A-24-23-08-0C-24-23-08-09-24-23-08-04-24")]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail, "", true)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail, "", true)]
        [InlineData(false, Scenario.SingleNull, Outcome.SerializeFail, "", false)]
        [InlineData(false, Scenario.MultipleWithNull, Outcome.SerializeFail, "", false)]
        [InlineData(true, Scenario.Null, Outcome.SerializeFail)]
        [InlineData(true, Scenario.Empty, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleNotNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.SingleNull, Outcome.SerializeFail)]
        [InlineData(true, Scenario.MultipleWithNull, Outcome.SerializeFail)]
        public void BasicPackedGroupedLists_PocoList(bool supportNull, Scenario scenario, Outcome outcome, string hex = null, bool fullCompile = false)
            => RunPocoList<BasicPackedGroupedLists>(supportNull, scenario, outcome, hex, fullCompile);

        private static RuntimeTypeModel GetModel<T>(bool supportNull)
        {
            var model = RuntimeTypeModel.Create();
            var mt = model.Add(typeof(T), true);
            if (supportNull)
            {
                foreach (var field in mt.GetFields())
                {
                    field.SupportNull = true;
                }
            }
            return model;
        }

        private void CheckSchema<T>(bool supportNull, string expected, bool expectFailure = false)
        {
            string actual;
            try
            {
                actual = GetModel<T>(supportNull).GetSchema(typeof(T), ProtoBuf.Meta.ProtoSyntax.Proto3);
            }
            catch (Exception ex) when (expectFailure)
            {
                _log?.WriteLine(ex.Message);
                Assert.Equal(expected, ex.Message);
                return;
            }
            Assert.False(expectFailure);
            _log?.WriteLine(actual);
            Assert.Equal(expected.Trim(), actual.Trim(), ignoreLineEndingDifferences: true, ignoreWhiteSpaceDifferences: true);
        }


        private static T Clone<T>(bool supportNull, T value, out string hex, bool fullCompile)
        {
            using var ms = new MemoryStream();
            var rtModel = GetModel<T>(supportNull);
            TypeModel model;
            if (fullCompile)
            {
                model = rtModel.Compile();
            }
            else
            {
                rtModel.CompileInPlace();
                model = rtModel;
            }
            model.Serialize(ms, value);
#if NET452
            var buffer = new ArraySegment<byte>(ms.ToArray());
#else
            if (!ms.TryGetBuffer(out var buffer)) buffer = new ArraySegment<byte>(ms.ToArray());
#endif
            hex = BitConverter.ToString(buffer.Array!, buffer.Offset, buffer.Count);
            ms.Position = 0;
            return (T)model.Deserialize(ms, null, typeof(T));
        }

        private void RunInt32List<T>(bool supportNull, Scenario scenario, Outcome outcome, string expectedHex, bool fullCompile = false) where T : class, IHazLists, new()
        {
            var orig = new T
            {
                Int32List = scenario switch
                {
                    Scenario.Null => null,
                    Scenario.Empty => new(),
                    Scenario.SingleNotNull => new() { 42 },
                    Scenario.MultipleNotNull => new() { 42, 17, 9, 4 },
                    _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
                }
            };
            T clone;
            string actualHex;
            try
            {
                clone = Clone(supportNull, orig, out actualHex, fullCompile);
            }
            catch (Exception ex) when (outcome is Outcome.SerializeFail)
            {
                _log?.WriteLine(ex.Message);
                return;
            }
            _log?.WriteLine(actualHex);
            CompareLists(orig.Int32List, clone.Int32List, outcome, 0);
            if (expectedHex is not null) Assert.Equal(expectedHex, actualHex);
        }

        private void RunNullableInt32List<T>(bool supportNull, Scenario scenario, Outcome outcome, string expectedHex, bool fullCompile = false) where T : class, IHazLists, new()
        {
            var orig = new T
            {
                NullableInt32List = scenario switch
                {
                    Scenario.Null => null,
                    Scenario.Empty => new(),
                    Scenario.SingleNotNull => new() { 42 },
                    Scenario.MultipleNotNull => new() { 42, 17, 9, 4 },
                    Scenario.SingleNull => new() { null },
                    Scenario.MultipleWithNull => new() { 42, null, 17, null },
                    _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
                }
            };
            T clone;
            string actualHex;
            try
            {
                clone = Clone(supportNull, orig, out actualHex, fullCompile);
            }
            catch (Exception ex) when (outcome is Outcome.SerializeFail)
            {
                _log?.WriteLine(ex.Message);
                return;
            }
            _log?.WriteLine(actualHex);
            CompareLists(orig.NullableInt32List, clone.NullableInt32List, outcome, null);
            if (expectedHex is not null) Assert.Equal(expectedHex, actualHex);
        }

        private void RunStringList<T>(bool supportNull, Scenario scenario, Outcome outcome, string expectedHex, bool fullCompile = false) where T : class, IHazLists, new()
        {
            var orig = new T
            {
                StringList = scenario switch
                {
                    Scenario.Null => null,
                    Scenario.Empty => new(),
                    Scenario.SingleNotNull => new() { "abc" },
                    Scenario.MultipleNotNull => new() { "abc", "def", "ghi", "jkl" },
                    Scenario.SingleNull => new() { null },
                    Scenario.MultipleWithNull => new() { "abc", null, "def", null },
                    _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
                }
            };
            T clone;
            string actualHex;
            try
            {
                clone = Clone(supportNull, orig, out actualHex, fullCompile);
            }
            catch (Exception ex) when (outcome is Outcome.SerializeFail)
            {
                _log?.WriteLine(ex.Message);
                return;
            }
            _log?.WriteLine(actualHex);
            CompareLists(orig.StringList, clone.StringList, outcome, "");
            if (expectedHex is not null) Assert.Equal(expectedHex, actualHex);
        }

        private void RunPocoList<T>(bool supportNull, Scenario scenario, Outcome outcome, string expectedHex, bool fullCompile = false) where T : class, IHazLists, new()
        {
            var orig = new T
            {
                PocoList = scenario switch
                {
                    Scenario.Null => null,
                    Scenario.Empty => new(),
                    Scenario.SingleNotNull => new() { new(42) },
                    Scenario.MultipleNotNull => new() { new(42), new(12), new(9), new(4) },
                    Scenario.SingleNull => new() { null },
                    Scenario.MultipleWithNull => new() { new(42), null, new(12), null },
                    _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
                }
            };
            T clone;
            string actualHex;
            try
            {
                clone = Clone(supportNull, orig, out actualHex, fullCompile);
            }
            catch (Exception ex) when (outcome is Outcome.SerializeFail)
            {
                _log?.WriteLine(ex.Message);
                return;
            }
            _log?.WriteLine(actualHex);
            CompareLists(orig.PocoList, clone.PocoList, outcome, new SomePoco());
            if (expectedHex is not null) Assert.Equal(expectedHex, actualHex);
        }
        static void CompareLists<T>(List<T> original, List<T> actual, Outcome outcome, T defaultValue)
        {
            switch (outcome)
            {
                case Outcome.Null:
                    Assert.Null(actual);
                    break;
                case Outcome.AllContents:
                    Assert.NotNull(actual);
                    Assert.NotSame(actual, original);
                    Assert.True(actual.SequenceEqual(original));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(outcome));
            }
        }


    }
}
