using ProtoBuf.Meta;
using System;
using Xunit;
namespace ProtoBuf.Test.Issues
{
    public class Issue633
    {
        [Theory]
        [InlineData(typeof(HasReservedDistinctField), "Field 31 is reserved and cannot be used for data member 'B' (iz 31).")]
        [InlineData(typeof(HasReservedDistinctFieldEnum), "Field 31 is reserved and cannot be used for enum value 'B' (iz 31).")]
        [InlineData(typeof(HasReservedDistinctFieldSubType), "Field 31 is reserved and cannot be used for sub-type 'ProtoBuf.Test.Issues.Issue633+HasReservedDistinctFieldSubType+B' (iz 31).")]
        [InlineData(typeof(HasReservedRangeField), "Field 32 is reserved and cannot be used for data member 'B' (iz 32).")]
        [InlineData(typeof(HasReservedRangeFieldEnum), "Field 32 is reserved and cannot be used for enum value 'B' (iz 32).")]
        [InlineData(typeof(HasReservedRangeFieldSubType), "Field 32 is reserved and cannot be used for sub-type 'ProtoBuf.Test.Issues.Issue633+HasReservedRangeFieldSubType+B' (iz 32).")]
        [InlineData(typeof(HasReservedNameField), "Field 'B' is reserved and cannot be used for data member 33 (iz B).")]
        [InlineData(typeof(HasReservedNameFieldEnum), "Field 'B' is reserved and cannot be used for enum value 33 (iz B).")]
        [InlineData(typeof(HasReservedNameFieldSubType), "Field 'B' is reserved and cannot be used for sub-type 33 (iz B).")]
        public void DetectReservedFields(Type type, string expected)
        {
            var model = RuntimeTypeModel.Create();
            model.Add(type, true);
            var ex = Assert.Throws<InvalidOperationException>(() => model.CompileInPlace());
            Assert.Equal(expected, ex.Message);
        }

        [Fact]
        public void WriteReservationsInProto()
        {
            var model = RuntimeTypeModel.Create();
            model.Add<HazValidReservations>();
            var schema = model.GetSchema(typeof(HazValidReservations), ProtoSyntax.Proto3);
            Assert.Equal(@"syntax = ""proto3"";
package ProtoBuf.Test.Issues;

message B {
}
message HazValidReservations {
   HazValidReservationsEnum value = 10;
   oneof subtype {
      B B = 42;
   }
   reserved 1; /* simple */
   reserved 2 to 5; /* range */
   reserved ""foo""; /* named */
}
enum HazValidReservationsEnum {
   Z = 0;
   A = 10;
   reserved 1; /* simple */
   reserved 2 to 5; /* range */
   reserved ""foo""; /* named */
}
", schema, ignoreLineEndingDifferences: true);
        }


        [ProtoContract]
        [ProtoReserved(1, "simple")]
        [ProtoReserved(2, 5, "range")]
        [ProtoReserved("foo", "named")]
        [ProtoInclude(42, typeof(B))]
        class HazValidReservations
        {
            [ProtoMember(10)]
            public HazValidReservationsEnum value { get; set; }

            [ProtoReserved(1, "simple")]
            [ProtoReserved(2, 5, "range")]
            [ProtoReserved("foo", "named")]
            public enum HazValidReservationsEnum
            {
                Z = 0,
                A = 10,
            }

            [ProtoContract]
            public class B : HazValidReservations { }
        }

        [ProtoContract]
        [ProtoReserved(31, "iz 31")]
        public class HasReservedDistinctField
        {
            [ProtoMember(30)]
            public int A { get; set; }
            [ProtoMember(31)]
            public int B { get; set; }
            [ProtoMember(32)]
            public int C { get; set; }
        }

        [ProtoContract]
        [ProtoReserved(20, 40, "iz 32")]
        public class HasReservedRangeField
        {
            [ProtoMember(1)]
            public int A { get; set; }
            [ProtoMember(32)]
            public int B { get; set; }
            [ProtoMember(3)]
            public int C { get; set; }
        }

        [ProtoContract]
        [ProtoReserved("B", "iz B")]
        public class HasReservedNameField
        {
            [ProtoMember(1)]
            public int A { get; set; }
            [ProtoMember(33)]
            public int B { get; set; }
            [ProtoMember(3)]
            public int C { get; set; }
        }

        [ProtoContract]
        [ProtoInclude(31, typeof(B))]
        [ProtoReserved(31, "iz 31")]
        public class HasReservedDistinctFieldSubType
        {
            [ProtoMember(30)]
            public int A { get; set; }
            [ProtoMember(32)]
            public int C { get; set; }
            [ProtoContract]
            public class B : HasReservedDistinctFieldSubType { }
        }

        [ProtoContract]
        [ProtoInclude(32, typeof(B))]
        [ProtoReserved(20, 40, "iz 32")]
        public class HasReservedRangeFieldSubType
        {
            [ProtoMember(1)]
            public int A { get; set; }
            [ProtoMember(3)]
            public int C { get; set; }
            [ProtoContract]
            public class B : HasReservedRangeFieldSubType { }
        }

        [ProtoContract]
        [ProtoInclude(33, typeof(B))]
        [ProtoReserved("B", "iz B")]
        public class HasReservedNameFieldSubType
        {
            [ProtoMember(1)]
            public int A { get; set; }
            [ProtoMember(3)]
            public int C { get; set; }
            [ProtoContract]
            public class B : HasReservedNameFieldSubType { }
        }

        [ProtoReserved(31, "iz 31")]
        public enum HasReservedDistinctFieldEnum
        {
            A = 30,
            B = 31,
            C = 32,
        }

        [ProtoReserved(20, 40, "iz 32")]
        public enum HasReservedRangeFieldEnum
        {
            A = 1,
            B = 32,
            C = 3,
        }

        [ProtoReserved("B", "iz B")]
        public enum HasReservedNameFieldEnum
        {
            A = 1,
            B = 33,
            C = 3,
        }
    }
}
