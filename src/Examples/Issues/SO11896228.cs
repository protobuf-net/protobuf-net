using System.IO;
using Xunit;
using ProtoBuf;
using ProtoBuf.Meta;

namespace Examples.Issues
{
    
    public class SO11896228
    {
        [Fact]
        public void AnonymousTypesCanRoundTrip()
        {
            var obj = new {X = 123, Y = "abc"};
            Assert.True(Program.CheckBytes(obj, new byte[] { 0x08, 0x7B, 0x12, 0x03, 0x61, 0x62, 0x63 }));
            var clone = Serializer.DeepClone(obj);
            Assert.NotSame(clone, obj);
            Assert.Equal(123, clone.X);
            Assert.Equal("abc", clone.Y);
        }

        static AnonEquiv ChangeToEquiv<T>(T value)
        {
            return Serializer.ChangeType<T, AnonEquiv>(value);
        }

        [Fact]
        public void AnonymousTypesAreEquivalent_Auto()
        {
            var obj = new { X = 123, Y = "abc" };
            Assert.True(Program.CheckBytes(obj, new byte[] { 0x08, 0x7B, 0x12, 0x03, 0x61, 0x62, 0x63 }));
            var clone = ChangeToEquiv(obj);
            Assert.NotSame(clone, obj);
            Assert.Equal(123, clone.X);
            Assert.Equal("abc", clone.Y);
        }

        [Fact]
        public void AnonymousTypesAreEquivalent_Manual()
        {
            var obj = new { X = 123, Y = "abc" };
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;
            TestAnonTypeEquiv(model, obj, "Runtime");
            model.CompileInPlace();
            TestAnonTypeEquiv(model, obj, "CompileInPlace");
        }


#pragma warning disable IDE0060
        private static void TestAnonTypeEquiv(TypeModel model, object obj, string caption)
#pragma warning restore IDE0060
        {
            AnonEquiv clone;
            byte[] expected = new byte[] {0x08, 0x7B, 0x12, 0x03, 0x61, 0x62, 0x63};
            Assert.True(Program.CheckBytes(obj, model, expected)); //, caption);
            using (var ms = new MemoryStream())
            {
#pragma warning disable CS0618
                model.Serialize(ms, obj);
#pragma warning restore CS0618
                Assert.Equal(expected.Length, ms.Length);
                Assert.Equal(Program.GetByteString(expected), Program.GetByteString(ms.ToArray())); //, caption);
                ms.Position = 0;
#pragma warning disable CS0618
                clone = (AnonEquiv) model.Deserialize(ms, null, typeof (AnonEquiv));
#pragma warning restore CS0618
            }
            Assert.NotSame(clone, obj); //, caption);
            Assert.Equal(123, clone.X); //, caption);
            Assert.Equal("abc", clone.Y); //, caption);
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
