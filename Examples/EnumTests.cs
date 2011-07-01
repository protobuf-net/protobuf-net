using System;
using System.ComponentModel;
using System.IO;
using NUnit.Framework;
using ProtoBuf;
using System.Runtime.Serialization;

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

    [TestFixture]
    public class EnumTests
    {

        [Test, Ignore("GetProto not implemented yet")]
        public void EnumGeneration()
        {
            string proto = Serializer.GetProto<EnumFoo>();
            Assert.AreEqual(@"package Examples.DesignIdeas;

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


        [Test, Ignore("GetProto not implemented yet")]
        public void TestNonNullValues()
        {
            string proto = Serializer.GetProto<NonNullValues>();
            Assert.AreEqual(@"package Examples.DesignIdeas;

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

        [Test, Ignore("GetProto not implemented yet")]
        public void TestNullValues()
        {
            string proto = Serializer.GetProto<NullValues>();
            Assert.AreEqual(@"package Examples.DesignIdeas;

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

        [Test, ExpectedException(typeof(ProtoException))]
        public void TestConflictingKeys()
        {
            Serializer.Serialize(Stream.Null, new TypeDuffKeys { Value = HasConflictingKeys.Foo });
        }

        [Test, ExpectedException(typeof(ProtoException))]
        public void TestConflictingValues()
        {
            Serializer.Serialize(Stream.Null, new TypeDuffValues { Value = HasConflictingValues.Foo });
        }

        [Test]
        public void TestEnumNameValueMapped()
        {
            CheckValue(SomeEnum.ChangeBoth, 0x08, 92);
        }


        [Test]
        public void TestFlagsEnum()
        {
            var orig = new TypeWithFlags { Foo = TypeWithFlags.FlagsEnum.A | TypeWithFlags.FlagsEnum.B };
            var clone = Serializer.DeepClone(orig);
            Assert.AreEqual(orig.Foo, clone.Foo);
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

        [Test]
        public void TestNulalbleEnumNameValueMapped()
        {
            var orig = new EnumNullableFoo { Bar = SomeEnum.ChangeBoth };
            var clone = Serializer.DeepClone(orig);
            Assert.AreEqual(orig.Bar, clone.Bar);
        }
        [Test]
        public void TestEnumNameMapped() {
            CheckValue(SomeEnum.ChangeName, 0x08, 03);
        }
        [Test]
        public void TestEnumValueMapped() {
            CheckValue(SomeEnum.ChangeValue, 0x08, 19);
        }
        [Test]
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
                Assert.IsTrue(Program.ArraysEqual(buffer, expected), "Byte mismatch");

                EnumFoo clone = Serializer.Deserialize<EnumFoo>(ms);
                Assert.AreEqual(val, clone.Bar);
            }
        }

        [Test]
        public void TestNegEnum()
        {
            TestNegEnum(NegEnum.A);
            TestNegEnum(NegEnum.B);
            TestNegEnum(NegEnum.C);
        }
        [Test, ExpectedException(typeof(ProtoException))]
        public void TestNegEnumnotDefinedNeg()
        {
            TestNegEnum((NegEnum)(-2));
        }
        [Test, ExpectedException(typeof(ProtoException))]
        public void TestNegEnumnotDefinedPos()
        {
            TestNegEnum((NegEnum) 2);
        }
        [Test]
        public void ShouldBeAbleToSerializeExactDuplicatedEnumValues()
        {
            var obj = new HasDuplicatedEnumProp { Value = NastDuplicates.B };
            var clone = Serializer.DeepClone(obj);
            Assert.AreEqual(NastDuplicates.A, clone.Value);
            Assert.AreEqual(NastDuplicates.B, clone.Value);
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

        private static void TestNegEnum(NegEnum value)
        {
            NegEnumType obj = new NegEnumType { Value = value },
                clone = Serializer.DeepClone(obj);
            Assert.AreEqual(obj.Value, clone.Value, value.ToString());
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

        [Test]
        public void RoundTripTopLevelContract()
        {
            EnumMarkedContract value = EnumMarkedContract.C;
            Assert.IsTrue(Program.CheckBytes(value, 8, 3));
            Assert.AreEqual(value, Serializer.DeepClone(value));
        }

        [Test]
        public void RoundTripTopLevelNullableContract()
        {
            EnumMarkedContract? value = EnumMarkedContract.C;
            Assert.IsTrue(Program.CheckBytes(value, 8, 3));
            Assert.AreEqual(value, Serializer.DeepClone(value));
        }
        [Test]
        public void RoundTripTopLevelNullableContractNull()
        {
            EnumMarkedContract? value = null;
            Assert.AreEqual(value, Serializer.DeepClone(value));
        }
        [Test]
        public void RoundTripTopLevelNoContract()
        {
            EnumNoContract value = EnumNoContract.C;
            Assert.IsTrue(Program.CheckBytes(value, 8, 3));
            Assert.AreEqual(value, Serializer.DeepClone(value));
        }

        [Test]
        public void RoundTripTopLevelNullableNoContract()
        {
            EnumNoContract? value = EnumNoContract.C;
            Assert.IsTrue(Program.CheckBytes(value, 8, 3));
            Assert.AreEqual(value, Serializer.DeepClone(value));
        }
        [Test]
        public void RoundTripTopLevelNullableNoContractNull()
        {
            EnumNoContract? value = null;
            Assert.AreEqual(value, Serializer.DeepClone(value));
        }

    }
}
