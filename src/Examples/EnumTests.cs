using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.ComponentModel;
using Xunit;

namespace Examples.DesignIdeas
{
    [ProtoContract]
    class EnumNullableFoo
    {
        public EnumNullableFoo() { Bar = NegEnum.B; }
        [ProtoMember(1), DefaultValue(NegEnum.B)]
        public NegEnum? Bar { get; set; }
    }

    [ProtoContract]
    class EnumFoo
    {
        public EnumFoo() { Bar = NegEnum.B; }
        [ProtoMember(1), DefaultValue(NegEnum.B)]
        public NegEnum Bar { get; set; }
    }

    [ProtoContract(Name = "blah")]
    enum NegEnum
    {
        A = -1, B = 0, C = 1
    }
    [ProtoContract]
    class NegEnumType
    {
        [ProtoMember(1)]
        public NegEnum Value { get; set; }
    }

    [ProtoContract]
    class NonNullValues
    {
        [ProtoMember(1), DefaultValue(NegEnum.B)]
        NegEnum Foo { get; set; }
        [ProtoMember(2)]
        bool Bar { get; set; }
    }
    [ProtoContract]
    class NullValues
    {
        [ProtoMember(1), DefaultValue(NegEnum.B)]
        NegEnum? Foo { get; set; }
        [ProtoMember(2)]
        bool? Bar { get; set; }
    }

    
    public class EnumTests
    {

        [Fact]
        public void EnumGeneration()
        {

            string proto = Serializer.GetProto<EnumFoo>(ProtoSyntax.Proto2);

            Assert.Equal(@"syntax = ""proto2"";
package Examples.DesignIdeas;

message EnumFoo {
   optional blah Bar = 1 [default = B];
}
enum blah {
   B = 0;
   A = -1;
   C = 1;
}
", proto, ignoreLineEndingDifferences: true);
        }


        [Fact]
        public void TestNonNullValues()
        {
            var model = RuntimeTypeModel.Create();
            model.UseImplicitZeroDefaults = false;

            string proto = model.GetSchema(typeof (NonNullValues), ProtoSyntax.Proto2);

            Assert.Equal(@"syntax = ""proto2"";
package Examples.DesignIdeas;

message NonNullValues {
   optional blah Foo = 1 [default = B];
   optional bool Bar = 2;
}
enum blah {
   B = 0;
   A = -1;
   C = 1;
}
", proto, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void TestOutOfRangeValues()
        {
            var model = RuntimeTypeModel.Create();
            model.UseImplicitZeroDefaults = false;

            string proto = model.GetSchema(typeof(HazOutOfRange), ProtoSyntax.Proto3);

            Assert.Equal(@"syntax = ""proto3"";
package Examples.DesignIdeas;

message HazOutOfRange {
   int64 OutOfRange = 1; // declared as invalid enum: OutOfRangeEnum
   InRangeEnum InRange = 2;
}
enum InRangeEnum {
   ZERO = 0; // proto3 requires a zero value as the first item (it can be named anything)
   A = 1;
   B = 4;
   C = 2147483647;
   E = -2147483647;
}
/* for context only
enum OutOfRangeEnum {
   ZERO = 0; // proto3 requires a zero value as the first item (it can be named anything)
   A = 1;
   B = 4;
   C = 2147483647;
   // D = 2147483648; // note: enums should be valid 32-bit integers
   E = -2147483647;
   // F = -2147483649; // note: enums should be valid 32-bit integers
}
*/
", proto, ignoreLineEndingDifferences: true);
        }

        public enum InRangeEnum : long
        {
            A = 1,
            B = 4,
            C = int.MaxValue,
            E = -int.MaxValue,
        }

        public enum OutOfRangeEnum : long
        {
            A = 1,
            B = 4,
            C = int.MaxValue,
            D = ((long)int.MaxValue) + 1,
            E = -int.MaxValue,
            F = ((long)int.MinValue) -1,
        }

        [ProtoContract]
        public class HazOutOfRange
        {
            [ProtoMember(1)]
            public OutOfRangeEnum OutOfRange { get; set; }

            [ProtoMember(2)]
            public InRangeEnum InRange { get; set; }
        }

        [Fact]
        public void TestNullValues()
        {

            string proto = Serializer.GetProto<NullValues>(ProtoSyntax.Proto2);

            Assert.Equal(@"syntax = ""proto2"";
package Examples.DesignIdeas;

message NullValues {
   optional blah Foo = 1 [default = B];
   optional bool Bar = 2;
}
enum blah {
   B = 0;
   A = -1;
   C = 1;
}
", proto, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void TestFlagsEnum()
        {
            var orig = new TypeWithFlags { Foo = TypeWithFlags.FlagsEnum.A | TypeWithFlags.FlagsEnum.B };
            var clone = Serializer.DeepClone(orig);
            Assert.Equal(orig.Foo, clone.Foo);
        }

        [ProtoContract]
        class TypeWithFlags
        {
            [Flags]
            public enum FlagsEnum
            {
                None = 0, A = 1, B = 2, C = 4
            }
            [ProtoMember(1)]
            public FlagsEnum Foo { get; set; }
        }

        [Fact]
        public void TestNegEnum()
        {
            TestNegEnumImpl(NegEnum.A);
            TestNegEnumImpl(NegEnum.B);
            TestNegEnumImpl(NegEnum.C);
        }
        [Fact]
        public void TestNegEnumnotDefinedNeg()
        {
            TestNegEnumImpl((NegEnum)(-2));
        }
        [Fact]
        public void TestNegEnumnotDefinedPos()
        {
            TestNegEnumImpl((NegEnum)2);
        }
        [Fact]
        public void ShouldBeAbleToSerializeExactDuplicatedEnumValues()
        {
            var obj = new HasDuplicatedEnumProp { Value = NastDuplicates.B };
            var clone = Serializer.DeepClone(obj);
            Assert.Equal(NastDuplicates.A, clone.Value);
            Assert.Equal(NastDuplicates.B, clone.Value);
        }
        [ProtoContract]
        class HasDuplicatedEnumProp
        {
            [ProtoMember(1)]
            public NastDuplicates Value { get; set; }
        }
        enum NastDuplicates
        {
            None = 0,
            A = 1,
            B = 1
        }

        private static void TestNegEnumImpl(NegEnum value)
        {
            NegEnumType obj = new NegEnumType { Value = value },
                clone = Serializer.DeepClone(obj);
            Assert.Equal(obj.Value, clone.Value); //, value.ToString());
        }


        [ProtoContract]
        enum EnumMarkedContract : ushort
        {
            None = 0, A, B, C, D
        }
        enum EnumNoContract : ushort
        {
            None = 0, A, B, C, D
        }

        [Fact]
        public void RoundTripTopLevelContract()
        {
            EnumMarkedContract value = EnumMarkedContract.C;
            Assert.True(Program.CheckBytes(value, 8, 3));
            Assert.Equal(value, Serializer.DeepClone(value));
        }

        [Fact]
        public void RoundTripTopLevelNullableContract()
        {
            EnumMarkedContract? value = EnumMarkedContract.C;
            Assert.True(Program.CheckBytes(value, 8, 3));
            Assert.Equal(value, Serializer.DeepClone(value));
        }
        [Fact]
        public void RoundTripTopLevelNullableContractNull()
        {
            EnumMarkedContract? value = null;
            Assert.Equal(value, Serializer.DeepClone(value));
        }
        [Fact]
        public void RoundTripTopLevelNoContract()
        {
            EnumNoContract value = EnumNoContract.C;
            Assert.True(Program.CheckBytes(value, 8, 3));
            Assert.Equal(value, Serializer.DeepClone(value));
        }

        [Fact]
        public void RoundTripTopLevelNullableNoContract()
        {
            EnumNoContract? value = EnumNoContract.C;
            Assert.True(Program.CheckBytes(value, 8, 3));
            Assert.Equal(value, Serializer.DeepClone(value));
        }
        [Fact]
        public void RoundTripTopLevelNullableNoContractNull()
        {
            EnumNoContract? value = null;
            Assert.Equal(value, Serializer.DeepClone(value));
        }

    }
}
