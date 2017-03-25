using NUnit.Framework;
using ProtoBuf;
using System.ComponentModel;
using System.IO;

namespace Examples
{
    [TestFixture]
    public class OptionalData
    {
        [Test]
        public void TestImplicitDefaultZero()
        {
            Test<ImplicitDefaultZero>(0F, 0);
            Test<ImplicitDefaultZero>(3F, 5);
            Test<ImplicitDefaultZero>(5F, 5);
        }
        [Test]
        public void TestExplicitDefaultZero()
        {
            Test<ExplicitDefaultZero>(0F, 0);
            Test<ExplicitDefaultZero>(3F, 5);
            Test<ExplicitDefaultZero>(5F, 5);
        }
        [Test]
        public void TestExplicitDefaultFive()
        {
            Test<ExplicitDefaultFive>(0F, 5);
            Test<ExplicitDefaultFive>(3F, 5);
            Test<ExplicitDefaultFive>(5F, 0);
        }
        [Test]
        public void ExplicitDefaultFivePrivateField()
        {
            Test<ExplicitDefaultFivePrivateField>(0F, 5);
            Test<ExplicitDefaultFivePrivateField>(3F, 5);
            Test<ExplicitDefaultFivePrivateField>(5F, 0);
        }
        [Test]
        public void TestRequiredImplicitZero()
        {
            Test<RequiredImplicitZero>(0F,5);
            Test<RequiredImplicitZero>(3F, 5);
            Test<RequiredImplicitZero>(5F, 5);
        }
        [Test]
        public void TestRequiredExplicitZero()
        {
            Test<RequiredExplicitZero>(0F, 5);
            Test<RequiredExplicitZero>(3F, 5);
            Test<RequiredExplicitZero>(5F, 5);
        }
        [Test]
        public void TestRequiredExplicitFive()
        {
            Test<RequiredExplicitFive>(0F, 5);
            Test<RequiredExplicitFive>(3F, 5);
            Test<RequiredExplicitFive>(5F, 5);
        }


        static void Test<T>(float value, int expectedSize) where T : class, IOptionalData, new()
        {
            T orig = new T { Value = value }, clone = Serializer.DeepClone(orig);
            Assert.AreEqual(value, orig.Value, "Original");
            Assert.AreNotSame(orig, clone, "Different objects");
            Assert.AreEqual(value, clone.Value, "Clone");

            using(var ms = new MemoryStream()) {
                Serializer.Serialize(ms, orig);
                Assert.AreEqual(expectedSize, ms.Length, "Length");
            }
        }
    }

    


    interface IOptionalData
    {
        float Value { get; set; }
    }
    [ProtoContract]
    class ImplicitDefaultZero : IOptionalData
    {
        [ProtoMember(1)]
        public float Value { get; set; }
    }
    [ProtoContract]
    class ExplicitDefaultZero : IOptionalData
    {
        [ProtoMember(1), DefaultValue(0F)]
        public float Value { get; set; }
    }
    [ProtoContract]
    class RequiredImplicitZero : IOptionalData
    {
        [ProtoMember(1, IsRequired = true)]
        public float Value { get; set; }
    }
    [ProtoContract]
    class RequiredExplicitZero : IOptionalData
    {
        [ProtoMember(1, IsRequired = true), DefaultValue(0F)]
        public float Value { get; set; }
    }
    [ProtoContract]
    class ExplicitDefaultFive : IOptionalData
    {
        public ExplicitDefaultFive() { Value = 5F; }
        [ProtoMember(1), DefaultValue(5F)]
        public float Value { get; set; }
    }
    [ProtoContract]
    class ExplicitDefaultFivePrivateField : IOptionalData
    {
        public ExplicitDefaultFivePrivateField() { value = 5F; }
        [ProtoMember(1), DefaultValue(5F)]
        private float value;

        float IOptionalData.Value
        {
            get { return value; }
            set { this.value = value; }
        }
    }
    
    [ProtoContract]
    class RequiredExplicitFive : IOptionalData
    {
        public RequiredExplicitFive() {Value = 5F; }
        [ProtoMember(1, IsRequired = true), DefaultValue(5F)]
        public float Value { get; set; }
    }
}
