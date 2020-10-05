using ProtoBuf.Meta;
using System.ComponentModel;
using Xunit;

#if !PLAT_INIT_ONLY
namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit // yeah, don't do this!
    {
    }
}
#endif

namespace ProtoBuf.Test
{
    public class InitOnlyPropertyTests
    {
        [Fact]
        public void CanRoundTripInitOnlyProperties()
        {
            var model = RuntimeTypeModel.Create();
            model.AutoCompile = false;

            Test(model); // reflection
            model.CompileInPlace();
            Test(model); // in-place compile by method
            Test(model.Compile()); // full compile to in-memory assembly
            // (note: no point trying full disk compile unless new modreq exist in netfx)

            static void Test(TypeModel model)
            {
                var obj = new HazInitOnly { Id = 42 };
                var clone = model.DeepClone(obj);
                Assert.NotSame(obj, clone);
                Assert.Equal(42, clone.Id);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ReflectionViaGetSetMethod(bool nonPublic)
        {
            var obj = new HazInitOnly { Id = 42 };
            obj.GetType().GetProperty(nameof(obj.Id)).GetSetMethod(nonPublic).Invoke(obj, new object[] { 13 });
            Assert.Equal(13, obj.Id);
        }

        [Fact]
        public void ReflectionViaGetSetValue()
        {
            var obj = new HazInitOnly { Id = 42 };
            obj.GetType().GetProperty(nameof(obj.Id)).SetValue(obj, 13);
            Assert.Equal(13, obj.Id);
        }


        [ProtoContract]
        public class HazInitOnly
        {
            [ProtoMember(1)]
            public int Id { get; init; }
        }
    }
}