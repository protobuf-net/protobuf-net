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

message SomeType {
   int32 Date = 1;
   int64 Time = 2;
}
", model.GetSchema(typeof(SomeType)), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void SchemaArrays()
        {
            var model = RuntimeTypeModel.Create();
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test;

message SomeTypeWithArrays {
   repeated int32 Dates = 1;
   repeated int64 Times = 2;
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
            Assert.Equal("08-E5-8B-2D-10-80-85-85-B0-BF-0C", hex);
            /*
            Field #1: 08 Varint Value = 738789, Hex = E5-8B-2D
            Field #2: 10 Varint Value = 429329040000, Hex = 80-85-85-B0-BF-0C
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
            Assert.Equal("08-E5-8B-2D-10-80-85-85-B0-BF-0C", hex);
            // same payload

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