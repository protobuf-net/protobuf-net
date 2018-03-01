using System;
using System.ComponentModel;
using System.IO;
using Xunit;
using ProtoBuf;
using System.Runtime.Serialization;
using ProtoBuf.Meta;

namespace Examples.DesignIdeas
{
    /// <summary>
    /// would like to be able to specify custom values for enums;
    /// implementation note: some kind of map: Dictionary<TValue, long>?
    /// note: how to handle -ves? (ArgumentOutOfRangeException?)
    /// note: how to handle flags? (NotSupportedException? at least for now?
    ///             could later use a bitmap sweep?)
    /// </summary>
    [ProtoContract(Name="blah")]
    enum SomeEnum
    {
        [ProtoEnum(Name="FOO")]
        ChangeName = 3,

        [ProtoEnum(Value = 19)]
        ChangeValue = 5,

        [ProtoEnum(Name="BAR", Value=92)]
        ChangeBoth = 7,
        
        LeaveAlone = 22,


        Default = 2
    }
    [ProtoContract]
    class EnumFoo
    {
        public EnumFoo() { Bar = SomeEnum.Default; }
        [ProtoMember(1), DefaultValue(SomeEnum.Default)]
        public SomeEnum Bar { get; set; }
    }

    [ProtoContract]
    class EnumNullableFoo
    {
        public EnumNullableFoo() { Bar = SomeEnum.Default; }
        [ProtoMember(1), DefaultValue(SomeEnum.Default)]
        public SomeEnum? Bar { get; set; }
    }

    [ProtoContract(EnumPassthru = false)]
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
    public enum HasConflictingKeys
    {
        [ProtoEnum(Value = 1)]
        Foo = 0,
        [ProtoEnum(Value = 2)]
        Bar = 0
    }
    public enum HasConflictingValues
    {
        [ProtoEnum(Value=2)]
        Foo = 0,
        [ProtoEnum(Value = 2)]
        Bar = 1
    }
    [ProtoContract]
    class TypeDuffKeys
    {
        [ProtoMember(1)]
        public HasConflictingKeys Value {get;set;}
    }
    [ProtoContract]
    class TypeDuffValues
    {
        [ProtoMember(1)]
        public HasConflictingValues Value {get;set;}
    }

    [ProtoContract]
    class NonNullValues
    {
        [ProtoMember(1), DefaultValue(SomeEnum.Default)]
        SomeEnum Foo { get; set; }
        [ProtoMember(2)]
        bool Bar { get; set; }
    }
    [ProtoContract]
    class NullValues
    {
        [ProtoMember(1), DefaultValue(SomeEnum.Default)]
        SomeEnum? Foo { get; set; }
        [ProtoMember(2)]
        bool? Bar { get; set; }
    }

    
    public class EnumTests
    {

        [Fact]
        public void EnumGeneration()
        {

            string proto = Serializer.GetProto<EnumFoo>();

            Assert.Equal(@"syntax = ""proto2"";
package Examples.DesignIdeas;

message EnumFoo {
   optional blah Bar = 1 [default = Default];
}
enum blah {
   Default = 2;
   FOO = 3;
   ChangeValue = 19;
   LeaveAlone = 22;
   BAR = 92;
}
", proto);
        }


        [Fact]
        public void TestNonNullValues()
        {
            var model = TypeModel.Create();
            model.UseImplicitZeroDefaults = false;

            string proto = model.GetSchema(typeof (NonNullValues));

            Assert.Equal(@"syntax = ""proto2"";
package Examples.DesignIdeas;

message NonNullValues {
   optional blah Foo = 1 [default = Default];
   optional bool Bar = 2;
}
enum blah {
   Default = 2;
   FOO = 3;
   ChangeValue = 19;
   LeaveAlone = 22;
   BAR = 92;
}
", proto);
        }

        [Fact]
        public void TestNullValues()
        {

            string proto = Serializer.GetProto<NullValues>();

            Assert.Equal(@"syntax = ""proto2"";
package Examples.DesignIdeas;

message NullValues {
   optional blah Foo = 1 [default = Default];
   optional bool Bar = 2;
}
enum blah {
   Default = 2;
   FOO = 3;
   ChangeValue = 19;
   LeaveAlone = 22;
   BAR = 92;
}
", proto);
        }

        [Fact]
        public void TestConflictingKeys()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                Serializer.Serialize(Stream.Null, new TypeDuffKeys { Value = HasConflictingKeys.Foo });
            });
        }

        [Fact]
        public void TestConflictingValues()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                Serializer.Serialize(Stream.Null, new TypeDuffValues { Value = HasConflictingValues.Foo });
            });
        }

        [Fact]
        public void TestEnumNameValueMapped()
        {
            CheckValue(SomeEnum.ChangeBoth, 0x08, 92);
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
        public void TestNulalbleEnumNameValueMapped()
        {
            var orig = new EnumNullableFoo { Bar = SomeEnum.ChangeBoth };
            var clone = Serializer.DeepClone(orig);
            Assert.Equal(orig.Bar, clone.Bar);
        }
        [Fact]
        public void TestEnumNameMapped() {
            CheckValue(SomeEnum.ChangeName, 0x08, 03);
        }
        [Fact]
        public void TestEnumValueMapped() {
            CheckValue(SomeEnum.ChangeValue, 0x08, 19);
        }
        [Fact]
        public void TestEnumNoMap() {
            CheckValue(SomeEnum.LeaveAlone, 0x08, 22);
        }

        static void CheckValue(SomeEnum val, params byte[] expected)
        {
            EnumFoo foo = new EnumFoo { Bar = val };
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, foo);
                ms.Position = 0;
                byte[] buffer = ms.ToArray();
                Assert.True(Program.ArraysEqual(buffer, expected), "Byte mismatch");

                EnumFoo clone = Serializer.Deserialize<EnumFoo>(ms);
                Assert.Equal(val, clone.Bar);
            }
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
            Program.ExpectFailure<ProtoException>(() =>
            {
                TestNegEnumImpl((NegEnum)(-2));
            });
        }
        [Fact]
        public void TestNegEnumnotDefinedPos()
        {
            Program.ExpectFailure<ProtoException>(() =>
            {
                TestNegEnumImpl((NegEnum)2);
            });
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
