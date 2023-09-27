using ProtoBuf.Meta;
using ProtoBuf.unittest;
using System;
using System.IO;
using Xunit;

#if NET6_0_OR_GREATER
namespace ProtoBuf.Test
{
    public class DateTimeOnlyTests
    {
        [Fact]
        public void SchemaFlat()
        {
            var model = RuntimeTypeModel.Create();
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;
import ""google/protobuf/duration.proto"";

message SomeType {
   int32 Date = 1;
   .google.protobuf.Duration Time = 2;
}
", model.GetSchema(typeof(SomeType)), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void SchemaArrays()
        {
            var model = RuntimeTypeModel.Create();
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;
import ""google/protobuf/duration.proto"";

message SomeTypeWithArrays {
   repeated int32 Dates = 1;
   repeated .google.protobuf.Duration Times = 2;
}
", model.GetSchema(typeof(SomeTypeWithArrays)), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void CanRoundtripSimpleAndArrays()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            model.Add<SomeType>();
            model.Add<SomeTypeWithArrays>();

            ExecuteSimple(model);
            ExecuteArrays(model);

            model.CompileInPlace();
            ExecuteSimple(model);
            ExecuteArrays(model);

            var compiled = model.Compile();
            ExecuteSimple(compiled);
            ExecuteArrays(compiled);

            compiled = PEVerify.CompileAndVerify(model);
            ExecuteSimple(compiled);
            ExecuteArrays(compiled);
        }

        void ExecuteSimple(TypeModel model)
        {
            var obj = new SomeType {
                Date = new DateOnly(2023, 09, 27),
                Time = new TimeOnly(11, 55, 32, 904),
            };

            using var ms = new MemoryStream();
            model.Serialize(ms, obj);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("08-E5-8B-2D-12-0A-08-B4-CF-02-10-80-E4-87-AF-03", hex);
            /*
        Field #1: 08 Varint Value = 738789, Hex = E5-8B-2D                  == 2023-09-27
        Field #2: 12 String Length = 10, Hex = 0A, UTF8 = "���䇯"
            Field #1: 08 Varint Value = 42932, Hex = B4-CF-02               == 11:55:32
            Field #2: 10 Varint Value = 904000000, Hex = 80-E4-87-AF-03     == .904s
             */


            ms.Position = 0;
            var clone = model.Deserialize<SomeType>(ms);
            Assert.NotNull(clone);
            Assert.NotSame(obj, clone);
            Assert.Equal(obj.Date, clone.Date);
            Assert.Equal(obj.Time, clone.Time);
        }

        void ExecuteArrays(TypeModel model)
        {
            var obj = new SomeTypeWithArrays
            {
                Dates = new[] { new DateOnly(2023, 09, 27) },
                Times = new[] { new TimeOnly(11, 55, 32, 904) },
            };

            using var ms = new MemoryStream();
            model.Serialize(ms, obj);
            var hex = BitConverter.ToString(ms.GetBuffer(), 0, (int)ms.Length);
            Assert.Equal("08-E5-8B-2D-12-0A-08-B4-CF-02-10-80-E4-87-AF-03", hex);
            /*
        Field #1: 08 Varint Value = 738789, Hex = E5-8B-2D                  == 2023-09-27
        Field #2: 12 String Length = 10, Hex = 0A, UTF8 = "���䇯"
            Field #1: 08 Varint Value = 42932, Hex = B4-CF-02               == 11:55:32
            Field #2: 10 Varint Value = 904000000, Hex = 80-E4-87-AF-03     == .904s
             */


            ms.Position = 0;
            var clone = model.Deserialize<SomeTypeWithArrays>(ms);
            Assert.NotNull(clone);
            Assert.NotSame(obj, clone);
            Assert.Equal(obj.Dates, clone.Dates);
            Assert.Equal(obj.Times, clone.Times);
        }

        [ProtoContract]
        public class SomeType
        {
            [ProtoMember(1)]
            public DateOnly Date { get; set; }

            [ProtoMember(2)]
            public TimeOnly Time { get; set; }
        }

        [ProtoContract]
        public class SomeTypeWithArrays
        {
            [ProtoMember(1)]
            public DateOnly[] Dates { get; set; }

            [ProtoMember(2)]
            public TimeOnly[] Times { get; set; }
        }
    }
}

#endif