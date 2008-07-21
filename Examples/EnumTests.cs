using ProtoBuf;
using System.IO;
using System;
using NUnit.Framework;

namespace Examples.DesignIdeas
{
    /// <summary>
    /// would like to be able to specify custom values for enums;
    /// implementation note: some kind of map: Dictionary<TValue, long>?
    /// note: how to handle -ves? (ArgumentOutOfRangeException?)
    /// note: how to handle flags? (NotSupportedException? at least for now?
    ///             could later use a bitmap sweep?)
    /// </summary>
    enum SomeEnum
    {
        [ProtoEnum(Name="FOO")]
        ChangeName = 3,

        [ProtoEnum(Value = 19)]
        ChangeValue = 5,

        [ProtoEnum(Name="BAR", Value=92)]
        ChangeBoth = 7,
        
        LeaveAlone = 22
    }
    [ProtoContract]
    class EnumFoo
    {
        [ProtoMember(1)]
        public SomeEnum Bar { get; set; }
    }
    [TestFixture]
    public class EnumTests
    {
        [Test]
        public void TestEnumNameValueMapped()
        {
            CheckValue(SomeEnum.ChangeBoth, 0x08, 92);
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
    }
}
