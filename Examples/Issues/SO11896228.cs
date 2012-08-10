using System.IO;
using NUnit.Framework;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    [TestFixture]
    public class SO11896228
    {
        [Test]
        public void AnonymousTypesCanRoundTrip()
        {
            var obj = new {X = 123, Y = "abc"};
            Assert.IsTrue(Program.CheckBytes(obj, new byte[] { 0x08, 0x7B, 0x12, 0x03, 0x61, 0x62, 0x63 }));
            var clone = Serializer.DeepClone(obj);
            Assert.AreNotSame(clone, obj);
            Assert.AreEqual(123, clone.X);
            Assert.AreEqual("abc", clone.Y);
        }

        static AnonEquiv ChangeToEquiv<T>(T value)
        {
            return Serializer.ChangeType<T, AnonEquiv>(value);
        }

        [Test]
        public void AnonymousTypesAreEquivalent_Auto()
        {
            var obj = new { X = 123, Y = "abc" };
            Assert.IsTrue(Program.CheckBytes(obj, new byte[] { 0x08, 0x7B, 0x12, 0x03, 0x61, 0x62, 0x63 }));
            var clone = ChangeToEquiv(obj);
            Assert.AreNotSame(clone, obj);
            Assert.AreEqual(123, clone.X);
            Assert.AreEqual("abc", clone.Y);
        }

        [Test]
        public void AnonymousTypesAreEquivalent_Manual()
        {
            var obj = new { X = 123, Y = "abc" };
            var model = TypeModel.Create();
            model.AutoCompile = false;
            TestAnonTypeEquiv(model, obj, "Runtime");
            model.CompileInPlace();
            TestAnonTypeEquiv(model, obj, "CompileInPlace");
        }

        private static void TestAnonTypeEquiv(TypeModel model, object obj, string caption)
        {
            AnonEquiv clone;
            byte[] expected = new byte[] {0x08, 0x7B, 0x12, 0x03, 0x61, 0x62, 0x63};
            Assert.IsTrue(Program.CheckBytes(obj, model, expected), caption);
            using (var ms = new MemoryStream())
            {
                model.Serialize(ms, obj);
                Assert.AreEqual(expected.Length, ms.Length);
                Assert.AreEqual(Program.GetByteString(expected), Program.GetByteString(ms.ToArray()), caption);
                ms.Position = 0;
                clone = (AnonEquiv) model.Deserialize(ms, null, typeof (AnonEquiv));
            }
            Assert.AreNotSame(clone, obj, caption);
            Assert.AreEqual(123, clone.X, caption);
            Assert.AreEqual("abc", clone.Y, caption);
        }

        [ProtoContract]
        public class AnonEquiv
        {
            [ProtoMember(1)]
            public int X { get; set; }
            [ProtoMember(2)]
            public string Y { get; set; }
        }
    }
}
